Option Strict On

Imports EegFilterTrainer.Dsp

Namespace Simulation

    ''' <summary>Bausteine, aus denen das simulierte EEG zusammengesetzt wird.</summary>
    Public Enum SignalPart
        Delta
        Theta
        Alpha
        Beta
        Spikes
        Blinks
        Sweat
        Emg
        Hum
        Ecg
    End Enum

    ''' <summary>
    ''' Alle Werte sind Potentialdifferenzen (Eingang 1 minus Eingang 2) in µV.
    ''' Die Anzeige folgt der EEG-Konvention "negativ nach oben".
    ''' Erzeugt ein reproduzierbares, simuliertes 8-Kanal-EEG (Längsreihe / "Doppelbanane")
    ''' plus EKG-Kanal. Jeder Baustein wird einmal vorberechnet und kann dann beliebig
    ''' ein- und ausgeschaltet werden.
    ''' </summary>
    Public NotInheritable Class EegSimulator

        Public Const SampleRate As Double = 500.0
        Public Const SampleCount As Integer = 32768          ' ≈ 65,5 s
        Public Const EcgChannel As Integer = 8

        Public Shared ReadOnly ChannelNames As String() =
            {"Fp1-F3", "F3-C3", "C3-P3", "P3-O1", "Fp2-F4", "F4-C4", "C4-P4", "P4-O2", "EKG"}

        ' Räumliche Verteilung (Gewicht pro Kanal) – Reihenfolge wie ChannelNames
        Private Shared ReadOnly Weights As New Dictionary(Of SignalPart, Double()) From {
            {SignalPart.Delta, {0.3, 1.0, -0.85, -0.2, 0.05, 0.1, 0.08, 0.05, 0}},      ' Herd links, Phasenumkehr bei C3
            {SignalPart.Theta, {0.3, 0.6, 0.7, 0.5, 0.3, 0.6, 0.7, 0.5, 0}},
            {SignalPart.Alpha, {0.1, 0.2, 0.5, 1.0, 0.1, 0.2, 0.55, 1.0, 0}},          ' okzipital betont
            {SignalPart.Beta, {0.8, 1.0, 0.7, 0.3, 0.8, 1.0, 0.7, 0.3, 0}},            ' frontozentral
            {SignalPart.Spikes, {0.15, 1.0, -0.9, -0.1, 0, 0.05, 0.05, 0, 0}},         ' oberflächennegativer Spike-Herd C3: F3-C3 positiv, C3-P3 negativ
            {SignalPart.Blinks, {1.0, 0.25, 0.05, 0, 1.0, 0.25, 0.05, 0, 0}},          ' Fp positiv (Bell-Phänomen) -> Ausschlag nach unten
            {SignalPart.Sweat, {1.0, 0.6, 0.5, 0.7, 0.9, 0.7, 0.4, 0.8, 0.1}},
            {SignalPart.Emg, {1.0, 0.5, 0.25, 0.3, 1.0, 0.55, 0.25, 0.35, 0.15}},       ' frontal
            {SignalPart.Hum, {0.25, 0.2, 0.25, 0.2, 0.25, 0.2, 1.0, 1.15, 0.3}},        ' schlechte Elektrode P4
            {SignalPart.Ecg, {0.15, 0.1, 0.35, 0.6, -0.15, -0.1, -0.35, -0.55, 0}}
        }

        Private ReadOnly _parts As New Dictionary(Of SignalPart, Double()())
        Private ReadOnly _background As Double()()
        Private ReadOnly _beats As New List(Of Integer)

        Public ReadOnly Property ChannelCount As Integer
            Get
                Return ChannelNames.Length
            End Get
        End Property

        Public Shared ReadOnly Property Duration As Double
            Get
                Return SampleCount / SampleRate
            End Get
        End Property

        ''' <summary>Tatsächliche Herzschlag-Zeitpunkte (Abtastwerte) – nur für Tests.</summary>
        Public ReadOnly Property TrueBeats As IReadOnlyList(Of Integer)
            Get
                Return _beats
            End Get
        End Property

        Public Sub New(Optional seed As Integer = 4711)
            Dim rng As New Random(seed)
            Dim fs = SampleRate

            ' ---------- Hintergrund (immer vorhanden) ----------
            _background = NewSet()
            For c = 0 To ChannelNames.Length - 1
                If c = EcgChannel Then
                    Dim w = WhiteNoise(rng)
                    For i = 0 To SampleCount - 1 : _background(c)(i) = 2.0 * w(i) : Next
                Else
                    Dim slow = BandNoise(rng, 0.5, 8)
                    Dim fast = WhiteNoise(rng)
                    For i = 0 To SampleCount - 1
                        _background(c)(i) = 3.5 * slow(i) + 0.8 * fast(i)
                    Next
                End If
            Next

            ' ---------- Delta: unregelmäßige langsame Wellen (Herd) ----------
            Dim delta(SampleCount - 1) As Double
            Dim dF = {0.9, 1.4, 2.1, 2.9}, dA = {1.0, 0.8, 0.55, 0.35}
            Dim dP = dF.Select(Function(f) rng.NextDouble() * 2 * Math.PI).ToArray()
            Dim dEnv = SmoothNoise(rng, 0.2)
            For i = 0 To SampleCount - 1
                Dim t = i / fs, v = 0.0
                For k = 0 To dF.Length - 1 : v += dA(k) * Math.Sin(2 * Math.PI * dF(k) * t + dP(k)) : Next
                delta(i) = v * (0.75 + 0.25 * Math.Tanh(dEnv(i)))
            Next
            Normalize(delta)
            _parts(SignalPart.Delta) = Spread(SignalPart.Delta, delta, delta, 40)

            ' ---------- Theta: zeitweise 6-Hz-Gruppen ----------
            Dim thL = Oscillation(rng, 6.0, 0.4, 0.25, intermittent:=True)
            Dim thR = Oscillation(rng, 6.0, 0.4, 0.25, intermittent:=True)
            _parts(SignalPart.Theta) = Spread(SignalPart.Theta, thL, thR, 28)

            ' ---------- Alpha: 10 Hz, an- und abschwellend ----------
            Dim alL(SampleCount - 1) As Double, alR(SampleCount - 1) As Double
            Dim fj = SmoothNoise(rng, 0.1), env = SmoothNoise(rng, 0.3)
            Dim ph = 0.0
            For i = 0 To SampleCount - 1
                ph += 2 * Math.PI * (10.0 + 0.25 * fj(i)) / fs
                Dim e = 0.6 + 0.4 * Math.Tanh(env(i))
                alL(i) = e * Math.Sin(ph)
                alR(i) = e * Math.Sin(ph + 0.4)
            Next
            _parts(SignalPart.Alpha) = Spread(SignalPart.Alpha, alL, alR, 50)

            ' ---------- Beta: 18–23 Hz, niedrig ----------
            Dim beL = Oscillation(rng, 18.0, 1.0, 1.0, False)
            Dim beL2 = Oscillation(rng, 23.0, 1.0, 1.0, False)
            Dim beR = Oscillation(rng, 18.5, 1.0, 1.0, False)
            Dim beR2 = Oscillation(rng, 22.0, 1.0, 1.0, False)
            For i = 0 To SampleCount - 1
                beL(i) = 0.6 * (beL(i) + beL2(i))
                beR(i) = 0.6 * (beR(i) + beR2(i))
            Next
            _parts(SignalPart.Beta) = Spread(SignalPart.Beta, beL, beR, 9)

            ' ---------- Spikes (Spike-Wave-Komplexe) ----------
            Dim sp(SampleCount - 1) As Double
            Dim ts = 1.6
            While ts < Duration - 1
                AddEvent(sp, ts, 0.6, Function(tau) Gauss(tau, 0, 0.011) - 0.3 * Gauss(tau, 0.035, 0.015) + 0.45 * Gauss(tau, 0.2, 0.07))
                ts += 1.7 + rng.NextDouble() * 2.3
            End While
            _parts(SignalPart.Spikes) = Spread(SignalPart.Spikes, sp, sp, 120)

            ' ---------- Augenblinzeln ----------
            Dim bl(SampleCount - 1) As Double
            Dim tb = 2.3
            While tb < Duration - 1
                AddEvent(bl, tb, 0.6, Function(tau) Gauss(tau, 0, 0.085) - 0.08 * Gauss(tau, 0.25, 0.1))
                tb += 3.5 + rng.NextDouble() * 3.5
            End While
            _parts(SignalPart.Blinks) = Spread(SignalPart.Blinks, bl, bl, 160)

            ' ---------- Schwitzartefakt: sehr langsame, unabhängige Schwankungen ----------
            Dim sw = NewSet()
            For c = 0 To ChannelNames.Length - 1
                Dim fq = {0.1, 0.22, 0.38}
                Dim amps = fq.Select(Function(f) 0.5 + rng.NextDouble() * 0.5).ToArray()
                Dim phs = fq.Select(Function(f) rng.NextDouble() * 2 * Math.PI).ToArray()
                Dim slow = SmoothNoise(rng, 0.3)
                Dim buf(SampleCount - 1) As Double
                For i = 0 To SampleCount - 1
                    Dim t = i / fs, v = 0.5 * slow(i)
                    For k = 0 To fq.Length - 1 : v += amps(k) * Math.Sin(2 * Math.PI * fq(k) * t + phs(k)) : Next
                    buf(i) = v
                Next
                Normalize(buf)
                Dim w = Weights(SignalPart.Sweat)(c) * 70
                For i = 0 To SampleCount - 1 : sw(c)(i) = w * buf(i) : Next
            Next
            _parts(SignalPart.Sweat) = sw

            ' ---------- Muskelartefakt (EMG): breitbandig schnell, schubweise ----------
            Dim em = NewSet()
            Dim tension = SmoothNoise(rng, 0.15)
            For c = 0 To ChannelNames.Length - 1
                Dim nz = BandNoise(rng, 25, 150)
                Dim w = Weights(SignalPart.Emg)(c) * 16
                For i = 0 To SampleCount - 1
                    Dim e = 0.15 + Math.Pow(Math.Max(0, tension(i) + 0.2), 1.4)
                    em(c)(i) = w * e * nz(i)
                Next
            Next
            _parts(SignalPart.Emg) = em

            ' ---------- 50-Hz-Netzbrumm ----------
            Dim hm = NewSet()
            Dim hmEnv = SmoothNoise(rng, 0.2)
            For c = 0 To ChannelNames.Length - 1
                Dim p0 = rng.NextDouble() * 0.6
                Dim w = Weights(SignalPart.Hum)(c) * 22
                For i = 0 To SampleCount - 1
                    hm(c)(i) = w * (1 + 0.1 * hmEnv(i)) * Math.Sin(2 * Math.PI * 50.0 * i / fs + p0)
                Next
            Next
            _parts(SignalPart.Hum) = hm

            ' ---------- Herzschlag: EKG-Kanal + EKG-Artefakt in den EEG-Kanälen ----------
            Dim beat(SampleCount - 1) As Double
            Dim tr = 0.35
            While tr < Duration
                _beats.Add(CInt(tr * fs))
                AddEvent(beat, tr, 0.6, AddressOf EcgShape)
                tr += 0.82 + 0.035 * Math.Sin(2 * Math.PI * tr / 4.5) + 0.03 * (rng.NextDouble() - 0.5)
            End While
            For i = 0 To SampleCount - 1 : _background(EcgChannel)(i) -= 800 * beat(i) : Next
            _parts(SignalPart.Ecg) = Spread(SignalPart.Ecg, beat, beat, 40)
        End Sub

        ''' <summary>
        ''' Signal eines einzelnen Bausteins in dem Kanal, in dem er am stärksten ist
        ''' (für die Messung "wie viel kommt durch den Filter").
        ''' </summary>
        Public Function PartSignal(part As SignalPart) As Double()
            Dim w = Weights(part)
            Dim best = 0
            For c = 1 To w.Length - 1
                If c <> EcgChannel AndAlso Math.Abs(w(c)) > Math.Abs(w(best)) Then best = c
            Next
            Return _parts(part)(best)
        End Function

        ''' <summary>Signal eines Bausteins in einem bestimmten Kanal.</summary>
        Public Function PartSignal(part As SignalPart, channel As Integer) As Double()
            Return _parts(part)(channel)
        End Function

        ''' <summary>Mischt Hintergrund + gewählte Bausteine zu einem Roh-EEG.</summary>
        Public Function Mix(parts As IEnumerable(Of SignalPart)) As Double()()
            Dim r = NewSet()
            For c = 0 To r.Length - 1
                Array.Copy(_background(c), r(c), SampleCount)
            Next
            For Each p In parts.Distinct()
                Dim s = _parts(p)
                For c = 0 To r.Length - 1
                    Dim src = s(c), dst = r(c)
                    For i = 0 To SampleCount - 1 : dst(i) += src(i) : Next
                Next
            Next
            Return r
        End Function

        ' ================= Hilfsfunktionen =================

        Public Shared Function EcgShape(tau As Double) As Double
            Return 0.12 * Gauss(tau, -0.18, 0.025) - 0.12 * Gauss(tau, -0.035, 0.01) +
                   1.0 * Gauss(tau, 0, 0.011) - 0.25 * Gauss(tau, 0.035, 0.011) +
                   0.3 * Gauss(tau, 0.26, 0.05)
        End Function

        Private Shared Function Spread(part As SignalPart, left As Double(), right As Double(), amplitude As Double) As Double()()
            Dim s = NewSet()
            Dim w = Weights(part)
            For c = 0 To ChannelNames.Length - 1
                Dim src = If(c >= 4 AndAlso c < 8, right, left)
                Dim f = w(c) * amplitude
                If f = 0 Then Continue For
                For i = 0 To SampleCount - 1 : s(c)(i) = f * src(i) : Next
            Next
            Return s
        End Function

        Private Shared Function NewSet() As Double()()
            Dim s = New Double(ChannelNames.Length - 1)() {}
            For c = 0 To s.Length - 1
                s(c) = New Double(SampleCount - 1) {}
            Next
            Return s
        End Function

        Private Shared Function Gauss(t As Double, mu As Double, sigma As Double) As Double
            Dim z = (t - mu) / sigma
            Return Math.Exp(-0.5 * z * z)
        End Function

        Private Shared Sub AddEvent(target As Double(), tSec As Double, halfWidth As Double, shape As Func(Of Double, Double))
            Dim c = CInt(tSec * SampleRate)
            Dim h = CInt(halfWidth * SampleRate)
            For i = Math.Max(0, c - h) To Math.Min(target.Length - 1, c + h)
                target(i) += shape((i - c) / SampleRate)
            Next
        End Sub

        Private Shared Function WhiteNoise(rng As Random) As Double()
            Dim x(SampleCount - 1) As Double
            For i = 0 To SampleCount - 1
                Dim u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble()
                x(i) = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)
            Next
            Return x
        End Function

        ''' <summary>Langsam veränderliches Zufallssignal (Standardabweichung 1).</summary>
        Private Shared Function SmoothNoise(rng As Random, cutoff As Double) As Double()
            Dim x = WhiteNoise(rng)
            Dim ch As New FilterChain(SampleRate)
            ch.Add(IirStage.LowPass2(cutoff, SampleRate))
            ch.Add(IirStage.LowPass2(cutoff, SampleRate))
            ch.ProcessInPlace(x)
            Normalize(x)
            Return x
        End Function

        Private Shared Function BandNoise(rng As Random, lo As Double, hi As Double) As Double()
            Dim x = WhiteNoise(rng)
            Dim ch As New FilterChain(SampleRate)
            ch.Add(IirStage.HighPass2(lo, SampleRate))
            ch.Add(IirStage.HighPass2(lo, SampleRate))
            ch.Add(IirStage.LowPass2(hi, SampleRate))
            ch.Add(IirStage.LowPass2(hi, SampleRate))
            ch.ProcessInPlace(x)
            Normalize(x)
            Return x
        End Function

        Private Shared Function Oscillation(rng As Random, f As Double, jitter As Double, envCutoff As Double, intermittent As Boolean) As Double()
            Dim fj = SmoothNoise(rng, 0.2), env = SmoothNoise(rng, envCutoff)
            Dim x(SampleCount - 1) As Double
            Dim ph = rng.NextDouble() * 2 * Math.PI
            For i = 0 To SampleCount - 1
                ph += 2 * Math.PI * (f + jitter * fj(i)) / SampleRate
                Dim e = If(intermittent, Math.Max(0, Math.Tanh(env(i) * 1.3)), 0.6 + 0.4 * Math.Tanh(env(i)))
                x(i) = e * Math.Sin(ph)
            Next
            Return x
        End Function

        Private Shared Sub Normalize(x As Double())
            Dim m = x.Average()
            Dim v = 0.0
            For i = 0 To x.Length - 1
                x(i) -= m
                v += x(i) * x(i)
            Next
            Dim sd = Math.Sqrt(v / x.Length)
            If sd > 0 Then
                For i = 0 To x.Length - 1 : x(i) /= sd : Next
            End If
        End Sub

    End Class

End Namespace
