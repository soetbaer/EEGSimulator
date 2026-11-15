Option Strict On

Imports NeuroSim.Dsp

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
    ''' Erzeugt ein reproduzierbares, simuliertes EEG an den 21 Elektroden des 10/20-Systems plus EKG.
    ''' Gerechnet wird zuerst das Potential jeder Elektrode (in µV, gegenüber einem gedachten ruhigen
    ''' Bezugspunkt). Jede Verschaltung (Montage) bildet daraus ihre Kanäle als Differenzen –
    ''' so wie ein digitales EEG-Gerät alle Elektroden gleichzeitig aufzeichnet und die Verschaltung
    ''' erst für die Anzeige berechnet.
    ''' Die Anzeige folgt der EEG-Konvention "negativ nach oben".
    ''' </summary>
    Public NotInheritable Class EegSimulator

        Public Const SampleRate As Double = 500.0
        Public Const SampleCount As Integer = 32768          ' ≈ 65,5 s

        ''' <summary>Gute, leicht unterschiedliche Ausgangswerte in kΩ (Reihenfolge wie Head.Names).</summary>
        Public Shared ReadOnly DefaultImpedances As Double() =
            {3, 2, 2, 3, 2, 2, 3, 2, 2, 3, 2, 2, 3, 2, 2, 3, 2, 3, 2, 2, 3}

        ''' <summary>
        ''' Beispielwert: so viel 50-Hz-Störspannung entsteht pro kΩ Elektrodenimpedanz (µV/kΩ).
        ''' Entspricht einem Störstrom von etwa 2 nA in der Elektrodenzuleitung (U = I · Z).
        ''' Im Kanal bleibt die DIFFERENZ der beteiligten Elektroden übrig – deshalb zählt der Impedanz-Unterschied.
        ''' Der tatsächliche Wert hängt stark von der Umgebung ab.
        ''' </summary>
        Public Const HumMicrovoltPerKiloOhm As Double = 2.0

        ''' <summary>
        ''' Beispielwert für die 50-Hz-Gleichtaktstörung am ganzen Körper (µV, Scheitelwert). Sie ist an allen
        ''' Elektroden gleich und erscheint deshalb in keinem EEG-Kanal, nur an den einzeln gezeigten Verstärkereingängen.
        ''' </summary>
        Public Const CommonModeHumMicrovolt As Double = 1000.0

        ''' <summary>Langsames Wandern des Körperpotentials (µV, Effektivwert), ebenfalls an allen Elektroden gleich.</summary>
        Public Const CommonModeDriftMicrovolt As Double = 400.0

        ''' <summary>Elektroden, die bei eingeschalteter Gelbrücke leitend verbunden sind.</summary>
        Public Shared ReadOnly SaltBridgeElectrodes As Integer() = {Head.Index("F3"), Head.Index("C3")}
        ''' <summary>Anteil des Spannungsunterschieds zwischen den beiden Elektroden, der trotz Gelbrücke bleibt.</summary>
        Public Const SaltBridgeRemaining As Double = 0.08
        Public Const SaltBridgeImpedance As Double = 0.5

        ''' <summary>Verstärkung der langsamen Kontakt-Unruhe (0 bei guter Impedanz bis 5 kΩ).</summary>
        Public Shared Function InstabilityGain(kOhm As Double) As Double
            Return Math.Min(2.5, Math.Max(0, kOhm - 5) / 20.0)
        End Function

        ''' <summary>Verstärkung der Elektroden-"Pops" (erst bei deutlich zu hoher Impedanz über 10 kΩ).</summary>
        Public Shared Function PopGain(kOhm As Double) As Double
            Return Math.Min(2.0, Math.Max(0, kOhm - 10) / 15.0)
        End Function

        ''' <summary>Impedanzen, wie sie die Simulation benutzt: bei Gelbrücke sind die beiden Elektroden niederohmig.</summary>
        Public Shared Function EffectiveImpedances(impedances As Double(), saltBridge As Boolean) As Double()
            Dim z = CType(If(impedances, DefaultImpedances).Clone(), Double())
            If saltBridge Then
                For Each e In SaltBridgeElectrodes : z(e) = SaltBridgeImpedance : Next
            End If
            Return z
        End Function

        ' ---------- Zeitverläufe ----------
        Private ReadOnly _background As Double()()      ' je Elektrode: Grundaktivität und Rauschen
        Private ReadOnly _delta As Double()
        Private ReadOnly _thetaSide As Double()()       ' gemeinsamer Anteil links / rechts
        Private ReadOnly _thetaLocal As Double()()      ' je Elektrode
        Private ReadOnly _alphaSide As Double()()
        Private ReadOnly _betaSide As Double()()
        Private ReadOnly _betaLocal As Double()()
        Private ReadOnly _spikes As Double()
        Private ReadOnly _blinks As Double()
        Private ReadOnly _sweat As Double()()           ' je Elektrode
        Private ReadOnly _emg As Double()()             ' je Elektrode
        Private ReadOnly _humBase As Double()
        Private ReadOnly _beat As Double()
        Private ReadOnly _ecgNoise As Double()
        Private ReadOnly _bodyDrift As Double()
        Private ReadOnly _wander As Double()()          ' je Elektrode: langsame Kontakt-Unruhe (µV bei Gain 1)
        Private ReadOnly _pops As Double()()            ' je Elektrode: Pops (µV bei Gain 1)
        Private ReadOnly _beats As New List(Of Integer)

        ' ---------- Räumliche Verteilung: Potential je Elektrode in µV (bei Zeitverlauf 1) ----------
        Private Shared ReadOnly TopoDelta As Double() = Topo(55,
            "C3", 1.0, "F3", 0.45, "P3", 0.5, "T3", 0.45, "Cz", 0.4, "Fz", 0.15, "Pz", 0.2, "F7", 0.2, "T5", 0.2,
            "Fp1", 0.1, "O1", 0.1, "A1", 0.15, "C4", 0.05, "Fp2", 0.03, "F4", 0.03, "P4", 0.03)
        ' oberflächennegativer Herd unter C3 mit kleinem Feld in der Umgebung
        Private Shared ReadOnly TopoSpikes As Double() = Topo(-160,
            "C3", 1.0, "F3", 0.25, "P3", 0.3, "T3", 0.3, "Cz", 0.3, "Fz", 0.08, "Pz", 0.1, "F7", 0.08, "T5", 0.1,
            "Fp1", 0.03, "O1", 0.03, "A1", 0.08, "C4", 0.03)
        ' Hornhaut-Netzhaut-Dipol und Bell-Phänomen: die Stirnelektroden werden positiv
        Private Shared ReadOnly TopoBlinks As Double() = Topo(245,
            "Fp1", 1.0, "Fp2", 1.0, "F7", 0.45, "F8", 0.45, "F3", 0.35, "F4", 0.35, "Fz", 0.4, "T3", 0.1, "T4", 0.1,
            "C3", 0.08, "C4", 0.08, "Cz", 0.08, "A1", 0.06, "A2", 0.06, "P3", 0.02, "P4", 0.02, "Pz", 0.02, "T5", 0.02, "T6", 0.02)
        ' Alpha: okzipital am größten; auch die Ohren liegen im Alpha-Feld (wichtig für die Ohrreferenz)
        Private Shared ReadOnly TopoAlpha As Double() = Topo(70,
            "O1", 1.0, "O2", 1.0, "T5", 0.6, "T6", 0.6, "Pz", 0.5, "P3", 0.45, "P4", 0.45,
            "T3", 0.25, "T4", 0.25, "C3", 0.25, "C4", 0.25, "Cz", 0.25, "A1", 0.2, "A2", 0.2,
            "F3", 0.07, "F4", 0.07, "Fz", 0.07, "F7", 0.07, "F8", 0.07, "Fp1", 0.03, "Fp2", 0.03)
        Private Shared ReadOnly TopoBeta As Double() = Topo(7,
            "F3", 1.0, "F4", 1.0, "C3", 1.0, "C4", 1.0, "Fz", 0.9, "Cz", 0.9, "Fp1", 0.7, "Fp2", 0.7, "F7", 0.7, "F8", 0.7,
            "P3", 0.5, "P4", 0.5, "Pz", 0.5, "T3", 0.5, "T4", 0.5, "T5", 0.35, "T6", 0.35, "O1", 0.3, "O2", 0.3, "A1", 0.25, "A2", 0.25)
        Private Shared ReadOnly TopoTheta As Double() = Topo(26,
            "C3", 1.0, "C4", 1.0, "Cz", 0.9, "T3", 0.8, "T4", 0.8, "P3", 0.8, "P4", 0.8, "Pz", 0.8, "F3", 0.7, "F4", 0.7, "Fz", 0.7,
            "T5", 0.7, "T6", 0.7, "F7", 0.55, "F8", 0.55, "O1", 0.5, "O2", 0.5, "A1", 0.5, "A2", 0.5, "Fp1", 0.4, "Fp2", 0.4)
        Private Shared ReadOnly TopoSweat As Double() = Topo(45,
            "Fp1", 1.0, "Fp2", 1.0, "F7", 0.9, "F8", 0.9, "F3", 0.8, "F4", 0.8, "Fz", 0.8, "T3", 0.8, "T4", 0.8, "O1", 0.8, "O2", 0.8,
            "T5", 0.7, "T6", 0.7, "C3", 0.6, "C4", 0.6, "Cz", 0.6, "P3", 0.6, "P4", 0.6, "Pz", 0.6, "A1", 0.5, "A2", 0.5)
        Private Shared ReadOnly TopoEmg As Double() = Topo(14,
            "Fp1", 1.0, "Fp2", 1.0, "F7", 1.0, "F8", 1.0, "T3", 0.9, "T4", 0.9, "A1", 0.6, "A2", 0.6, "F3", 0.5, "F4", 0.5,
            "T5", 0.5, "T6", 0.5, "O1", 0.35, "O2", 0.35, "Fz", 0.3, "C3", 0.2, "C4", 0.2, "P3", 0.2, "P4", 0.2, "Cz", 0.12, "Pz", 0.12)
        ' EKG-Artefakt: umso größer, je tiefer die Elektrode am Kopf sitzt (näher am Hals), links etwas stärker als rechts
        Private Shared ReadOnly TopoEcg As Double() =
            Enumerable.Range(0, Head.Count).Select(Function(e) 45.0 * (1.0 - Head.Height(e)) - 25.0 * Head.X(e)).ToArray()

        Private Shared Function Topo(amplitude As Double, ParamArray pairs As Object()) As Double()
            Dim t(Head.Count - 1) As Double
            For i = 0 To pairs.Length - 2 Step 2
                t(Head.Index(CStr(pairs(i)))) = amplitude * CDbl(pairs(i + 1))
            Next
            Return t
        End Function

        Public Shared ReadOnly Property Duration As Double
            Get
                Return SampleCount / SampleRate
            End Get
        End Property

        ''' <summary>Tatsächliche Herzschlag-Zeitpunkte (Abtastwerte) – für die Messung des EKG-Filters und für Tests.</summary>
        Public ReadOnly Property TrueBeats As IReadOnlyList(Of Integer)
            Get
                Return _beats
            End Get
        End Property

        Public Sub New(Optional seed As Integer = 4711)
            Dim rng As New Random(seed)
            Dim fs = SampleRate
            Dim n = Head.Count

            ' ---------- Hintergrund (immer vorhanden), an jeder Elektrode unabhängig ----------
            _background = PerElectrode(Function(e)
                                           Dim slow = BandNoise(rng, 0.5, 8)
                                           Dim fast = WhiteNoise(rng)
                                           Dim b(SampleCount - 1) As Double
                                           For i = 0 To SampleCount - 1 : b(i) = 2.5 * slow(i) + 0.55 * fast(i) : Next
                                           Return b
                                       End Function)
            _ecgNoise = WhiteNoise(rng)

            ' ---------- Delta: unregelmäßige langsame Wellen (Herd) ----------
            _delta = New Double(SampleCount - 1) {}
            Dim dF = {0.9, 1.4, 2.1, 2.9}, dA = {1.0, 0.8, 0.55, 0.35}
            Dim dP = dF.Select(Function(f) rng.NextDouble() * 2 * Math.PI).ToArray()
            Dim dEnv = SmoothNoise(rng, 0.2)
            For i = 0 To SampleCount - 1
                Dim t = i / fs, v = 0.0
                For k = 0 To dF.Length - 1 : v += dA(k) * Math.Sin(2 * Math.PI * dF(k) * t + dP(k)) : Next
                _delta(i) = v * (0.75 + 0.25 * Math.Tanh(dEnv(i)))
            Next
            Normalize(_delta)

            ' ---------- Theta: zeitweise 6-Hz-Gruppen, je Seite gemeinsam plus örtlicher Anteil ----------
            _thetaSide = {Oscillation(rng, 6.0, 0.4, 0.25, intermittent:=True), Oscillation(rng, 6.0, 0.4, 0.25, intermittent:=True)}
            _thetaLocal = PerElectrode(Function(e) Oscillation(rng, 6.0, 0.4, 0.25, intermittent:=True))

            ' ---------- Alpha: 10 Hz, an- und abschwellend ----------
            Dim alL(SampleCount - 1) As Double, alR(SampleCount - 1) As Double
            Dim fj = SmoothNoise(rng, 0.1), env = SmoothNoise(rng, 0.3)
            Dim ph = 0.0
            For i = 0 To SampleCount - 1
                ph += 2 * Math.PI * (10.0 + 0.25 * fj(i)) / fs
                Dim a = 0.6 + 0.4 * Math.Tanh(env(i))
                alL(i) = a * Math.Sin(ph)
                alR(i) = a * Math.Sin(ph + 0.4)
            Next
            _alphaSide = {alL, alR}

            ' ---------- Beta: 18–23 Hz, niedrig, je Seite gemeinsam plus örtlicher Anteil ----------
            _betaSide = {Oscillation(rng, 18.0, 1.0, 1.0, False), Oscillation(rng, 18.5, 1.0, 1.0, False)}
            _betaLocal = PerElectrode(Function(e) Oscillation(rng, 18.0 + 5.0 * rng.NextDouble(), 1.0, 1.0, False))

            ' ---------- Spikes (Spike-Wave-Komplexe) ----------
            _spikes = New Double(SampleCount - 1) {}
            Dim ts = 1.6
            While ts < Duration - 1
                AddEvent(_spikes, ts, 0.6, Function(tau) Gauss(tau, 0, 0.007) - 0.3 * Gauss(tau, 0.028, 0.012) + 0.45 * Gauss(tau, 0.2, 0.07))
                ts += 1.7 + rng.NextDouble() * 2.3
            End While

            ' ---------- Augenblinzeln ----------
            _blinks = New Double(SampleCount - 1) {}
            Dim tb = 2.3
            While tb < Duration - 1
                AddEvent(_blinks, tb, 0.6, Function(tau) Gauss(tau, 0, 0.085) - 0.08 * Gauss(tau, 0.25, 0.1))
                tb += 3.5 + rng.NextDouble() * 3.5
            End While

            ' ---------- Schwitzartefakt: sehr langsame, an jeder Elektrode unabhängige Schwankungen ----------
            _sweat = PerElectrode(Function(e)
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
                                      Return buf
                                  End Function)

            ' ---------- Muskelartefakt (EMG): breitbandig schnell, schubweise ----------
            Dim tension = SmoothNoise(rng, 0.15)
            _emg = PerElectrode(Function(e)
                                    Dim nz = BandNoise(rng, 25, 150)
                                    For i = 0 To SampleCount - 1
                                        nz(i) *= 0.15 + Math.Pow(Math.Max(0, tension(i) + 0.2), 1.4)
                                    Next
                                    Return nz
                                End Function)

            ' ---------- 50-Hz-Netzeinstreuung: Grundschwingung (Stärke je Elektrode folgt aus der Impedanz) ----------
            _humBase = New Double(SampleCount - 1) {}
            Dim hmEnv = SmoothNoise(rng, 0.2)
            For i = 0 To SampleCount - 1
                _humBase(i) = (1 + 0.08 * hmEnv(i)) * Math.Sin(2 * Math.PI * 50.0 * i / fs)
            Next

            ' ---------- Herzschlag: EKG-Kanal und EKG-Artefakt an den Elektroden ----------
            _beat = New Double(SampleCount - 1) {}
            Dim tr = 0.35
            While tr < Duration
                _beats.Add(CInt(tr * fs))
                AddEvent(_beat, tr, 0.6, AddressOf EcgShape)
                tr += 0.82 + 0.035 * Math.Sin(2 * Math.PI * tr / 4.5) + 0.03 * (rng.NextDouble() - 0.5)
            End While

            ' ---------- Körperpotential: wandert langsam, an allen Elektroden gleich (Gleichtakt) ----------
            _bodyDrift = BandNoise(rng, 0.08, 0.6)

            ' ---------- Kontakt-Unruhe und Pops je Elektrode (wirken nur bei hoher Impedanz) ----------
            Dim rngE As New Random(seed + 1)
            _wander = PerElectrode(Function(e)
                                       ' langsame Schwankungen, überwiegend unter 5 Hz, plus etwas breitbandiges Rauschen
                                       Dim slow = BandNoise(rngE, 0.2, 3)
                                       Dim white = WhiteNoise(rngE)
                                       Dim w(SampleCount - 1) As Double
                                       For i = 0 To SampleCount - 1 : w(i) = 8.0 * slow(i) + 1.5 * white(i) : Next
                                       Return w
                                   End Function)
            _pops = PerElectrode(Function(e)
                                     ' Pops: sehr steiler Sprung, langsames Zurücklaufen
                                     Dim pp(SampleCount - 1) As Double
                                     Dim tp = 1.0 + rngE.NextDouble() * 4
                                     While tp < Duration - 1
                                         Dim sign = If(rngE.NextDouble() < 0.5, -1.0, 1.0)
                                         Dim amp = sign * (60 + 60 * rngE.NextDouble())
                                         Dim i0 = CInt(tp * fs)
                                         For i = i0 To Math.Min(SampleCount - 1, i0 + CInt(1.2 * fs))
                                             Dim tau = (i - i0) / fs
                                             pp(i) += amp * (1 - Math.Exp(-tau / 0.004)) * Math.Exp(-tau / 0.15)
                                         Next
                                         tp += 2.5 + rngE.NextDouble() * 6
                                     End While
                                     Return pp
                                 End Function)
        End Sub

        Private Shared Function PerElectrode(make As Func(Of Integer, Double())) As Double()()
            Dim r = New Double(Head.Count - 1)() {}
            For e = 0 To Head.Count - 1
                r(e) = make(e)
            Next
            Return r
        End Function

        ''' <summary>
        ''' Typischer Zeitverlauf eines Bausteins (für die Messung "wie viel kommt durch den Filter").
        ''' Die Höhe spielt dabei keine Rolle, nur die Form.
        ''' </summary>
        Public Function PartSignal(part As SignalPart) As Double()
            Select Case part
                Case SignalPart.Delta : Return _delta
                Case SignalPart.Theta : Return _thetaSide(0)
                Case SignalPart.Alpha : Return _alphaSide(0)
                Case SignalPart.Beta : Return _betaLocal(Head.Index("F3"))
                Case SignalPart.Spikes : Return _spikes
                Case SignalPart.Blinks : Return _blinks
                Case SignalPart.Sweat : Return _sweat(Head.Index("Fp1"))
                Case SignalPart.Emg : Return _emg(Head.Index("Fp1"))
                Case SignalPart.Hum : Return _humBase
                Case Else : Return _beat
            End Select
        End Function

        ''' <summary>So groß ist das EKG-Artefakt in einem Kanal (µV je Einheit des Herzschlag-Verlaufs, mit Vorzeichen).</summary>
        Public Shared Function EcgArtifactGain(channel As Derivation) As Double
            If channel.Kind <> ChannelKind.Eeg Then Return 0
            Return channel.Combine(TopoEcg)
        End Function

        ''' <summary>EKG-Artefakt allein, so wie es in diesem Kanal erscheint.</summary>
        Public Function EcgArtifact(channel As Derivation) As Double()
            Dim g = EcgArtifactGain(channel)
            Dim a(SampleCount - 1) As Double
            For i = 0 To SampleCount - 1 : a(i) = g * _beat(i) : Next
            Return a
        End Function

        ''' <summary>
        ''' Potential jeder Elektrode (µV) aus Hintergrund und gewählten Bausteinen.
        ''' Die Elektrodenimpedanzen (kΩ, Reihenfolge wie Head.Names) bestimmen, wie stark
        ''' 50-Hz-Einstreuung, Kontakt-Unruhe und Pops an der einzelnen Elektrode entstehen.
        ''' </summary>
        Public Function ElectrodePotentials(parts As IEnumerable(Of SignalPart), Optional impedances As Double() = Nothing,
                                            Optional saltBridge As Boolean = False) As Double()()
            Dim partSet As New HashSet(Of SignalPart)(parts)
            Dim z = EffectiveImpedances(impedances, saltBridge)
            Dim v = New Double(Head.Count - 1)() {}

            For e = 0 To Head.Count - 1
                Dim dst = CType(_background(e).Clone(), Double())
                v(e) = dst
                Dim side = If(Head.Side(e) > 0, 1, 0)
                Dim midline = (Head.Side(e) = 0)

                If partSet.Contains(SignalPart.Delta) Then AddScaled(dst, _delta, TopoDelta(e))
                If partSet.Contains(SignalPart.Spikes) Then AddScaled(dst, _spikes, TopoSpikes(e))
                If partSet.Contains(SignalPart.Blinks) Then AddScaled(dst, _blinks, TopoBlinks(e))
                If partSet.Contains(SignalPart.Ecg) Then AddScaled(dst, _beat, TopoEcg(e))
                If partSet.Contains(SignalPart.Sweat) Then AddScaled(dst, _sweat(e), TopoSweat(e))
                If partSet.Contains(SignalPart.Emg) Then AddScaled(dst, _emg(e), TopoEmg(e))
                If partSet.Contains(SignalPart.Alpha) Then AddSided(dst, _alphaSide, side, midline, TopoAlpha(e))
                If partSet.Contains(SignalPart.Theta) Then
                    AddSided(dst, _thetaSide, side, midline, 0.6 * TopoTheta(e))
                    AddScaled(dst, _thetaLocal(e), 0.6 * TopoTheta(e))
                End If
                If partSet.Contains(SignalPart.Beta) Then
                    AddSided(dst, _betaSide, side, midline, 0.5 * TopoBeta(e))
                    AddScaled(dst, _betaLocal(e), 0.85 * TopoBeta(e))
                End If

                ' Störungen, die an der einzelnen Elektrode entstehen und von ihrer Impedanz abhängen
                If partSet.Contains(SignalPart.Hum) Then AddScaled(dst, _humBase, HumMicrovoltPerKiloOhm * z(e))
                AddScaled(dst, _wander(e), InstabilityGain(z(e)))
                AddScaled(dst, _pops(e), PopGain(z(e)))
            Next

            ' Gelbrücke: die beiden Elektroden sind leitend verbunden und liegen fast auf demselben Potential
            If saltBridge Then
                Dim a = v(SaltBridgeElectrodes(0)), b = v(SaltBridgeElectrodes(1))
                For i = 0 To SampleCount - 1
                    Dim mean = (a(i) + b(i)) / 2, halfDiff = (a(i) - b(i)) / 2 * SaltBridgeRemaining
                    a(i) = mean + halfDiff
                    b(i) = mean - halfDiff
                Next
            End If
            Return v
        End Function

        ''' <summary>Gleichtaktspannung am ganzen Körper (µV): an allen Elektroden gleich, in keinem EEG-Kanal sichtbar.</summary>
        Public Function CommonMode(parts As IEnumerable(Of SignalPart)) As Double()
            Dim hum = parts.Contains(SignalPart.Hum)
            Dim cm(SampleCount - 1) As Double
            For i = 0 To SampleCount - 1
                cm(i) = CommonModeDriftMicrovolt * _bodyDrift(i) + If(hum, CommonModeHumMicrovolt * _humBase(i), 0.0)
            Next
            Return cm
        End Function

        ''' <summary>Rohsignal aller Kanäle einer Verschaltung (ungefiltert), das EKG eingeschlossen.</summary>
        Public Function Mix(montage As Montage, parts As IEnumerable(Of SignalPart), Optional impedances As Double() = Nothing,
                            Optional saltBridge As Boolean = False) As Double()()
            Dim partList = parts.Distinct().ToList()
            Dim v = ElectrodePotentials(partList, impedances, saltBridge)
            Dim cm As Double() = Nothing
            Dim r = New Double(montage.Channels.Count - 1)() {}
            For c = 0 To montage.Channels.Count - 1
                Dim ch = montage.Channels(c)
                Dim dst(SampleCount - 1) As Double
                r(c) = dst
                If ch.Kind = ChannelKind.Ecg Then
                    FillEcgChannel(dst, partList)
                    Continue For
                End If
                For k = 0 To ch.Electrodes.Length - 1
                    AddScaled(dst, v(ch.Electrodes(k)), ch.Weights(k))
                Next
                If ch.Kind = ChannelKind.SingleInput Then
                    If cm Is Nothing Then cm = CommonMode(partList)
                    AddScaled(dst, cm, 1.0)
                End If
            Next
            Return r
        End Function

        Private Sub FillEcgChannel(dst As Double(), parts As List(Of SignalPart))
            Dim fp1 = Head.Index("Fp1")
            Dim hum = parts.Contains(SignalPart.Hum), sweat = parts.Contains(SignalPart.Sweat), emg = parts.Contains(SignalPart.Emg)
            For i = 0 To SampleCount - 1
                Dim x = 2.0 * _ecgNoise(i) - 800.0 * _beat(i)
                If hum Then x += 4.0 * _humBase(i)
                If sweat Then x += 7.0 * _sweat(fp1)(i)
                If emg Then x += 2.4 * _emg(fp1)(i)
                dst(i) = x
            Next
        End Sub

        ' ================= Hilfsfunktionen =================

        Private Shared Sub AddScaled(dst As Double(), src As Double(), factor As Double)
            If factor = 0 Then Return
            For i = 0 To dst.Length - 1 : dst(i) += factor * src(i) : Next
        End Sub

        ''' <summary>Linke Elektroden bekommen den linken Verlauf, rechte den rechten, die Mittellinie den Mittelwert.</summary>
        Private Shared Sub AddSided(dst As Double(), sides As Double()(), side As Integer, midline As Boolean, factor As Double)
            If factor = 0 Then Return
            If midline Then
                AddScaled(dst, sides(0), factor / 2)
                AddScaled(dst, sides(1), factor / 2)
            Else
                AddScaled(dst, sides(side), factor)
            End If
        End Sub

        Public Shared Function EcgShape(tau As Double) As Double
            Return 0.12 * Gauss(tau, -0.18, 0.025) - 0.12 * Gauss(tau, -0.035, 0.01) +
                   1.0 * Gauss(tau, 0, 0.011) - 0.25 * Gauss(tau, 0.035, 0.011) +
                   0.3 * Gauss(tau, 0.26, 0.05)
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
