Option Strict On

Imports System.Numerics

Namespace Dsp

    ''' <summary>
    ''' Eine IIR-Filterstufe (bis 2. Ordnung, Direktform I).
    ''' Die Filter arbeiten "kausal", also wie ein echtes EEG-Gerät in Echtzeit:
    ''' jeder Ausgangswert hängt nur von aktuellen und vergangenen Werten ab.
    ''' </summary>
    Public NotInheritable Class IirStage

        Private ReadOnly _b0, _b1, _b2, _a1, _a2 As Double
        Private _x1, _x2, _y1, _y2 As Double

        Public ReadOnly Property Name As String

        Public Sub New(name As String, b0 As Double, b1 As Double, b2 As Double,
                       a0 As Double, a1 As Double, a2 As Double)
            Me.Name = name
            _b0 = b0 / a0 : _b1 = b1 / a0 : _b2 = b2 / a0
            _a1 = a1 / a0 : _a2 = a2 / a0
        End Sub

        ''' <summary>
        ''' Hochpass 1. Ordnung (entspricht dem klassischen RC-Glied im EEG-Verstärker).
        ''' Nur so gilt exakt: Zeitkonstante TC = 1 / (2·π·fc).
        ''' </summary>
        Public Shared Function HighPass1(fc As Double, fs As Double) As IirStage
            Dim k = Math.Tan(Math.PI * fc / fs)
            Return New IirStage("LowCut", 1.0, -1.0, 0.0, 1.0 + k, k - 1.0, 0.0)
        End Function

        ''' <summary>Butterworth-Tiefpass 2. Ordnung.</summary>
        Public Shared Function LowPass2(fc As Double, fs As Double) As IirStage
            Dim w0 = 2.0 * Math.PI * fc / fs
            Dim cs = Math.Cos(w0)
            Dim alpha = Math.Sin(w0) / (2.0 * Math.Sqrt(0.5))
            Return New IirStage("HiCut", (1.0 - cs) / 2.0, 1.0 - cs, (1.0 - cs) / 2.0,
                                1.0 + alpha, -2.0 * cs, 1.0 - alpha)
        End Function

        ''' <summary>Butterworth-Hochpass 2. Ordnung (intern für die Simulation / R-Zacken-Erkennung).</summary>
        Public Shared Function HighPass2(fc As Double, fs As Double) As IirStage
            Dim w0 = 2.0 * Math.PI * fc / fs
            Dim cs = Math.Cos(w0)
            Dim alpha = Math.Sin(w0) / (2.0 * Math.Sqrt(0.5))
            Return New IirStage("HP2", (1.0 + cs) / 2.0, -(1.0 + cs), (1.0 + cs) / 2.0,
                                1.0 + alpha, -2.0 * cs, 1.0 - alpha)
        End Function

        ''' <summary>Kerbfilter (Notch), z. B. 50 Hz.</summary>
        Public Shared Function Notch(f0 As Double, q As Double, fs As Double) As IirStage
            Dim w0 = 2.0 * Math.PI * f0 / fs
            Dim cs = Math.Cos(w0)
            Dim alpha = Math.Sin(w0) / (2.0 * q)
            Return New IirStage("Notch", 1.0, -2.0 * cs, 1.0, 1.0 + alpha, -2.0 * cs, 1.0 - alpha)
        End Function

        ''' <summary>Setzt den inneren Zustand so, als hätte schon immer der Wert <paramref name="first"/> angelegen.</summary>
        Public Sub Reset(first As Double)
            Dim dcGain = (_b0 + _b1 + _b2) / (1.0 + _a1 + _a2)
            _x1 = first : _x2 = first
            _y1 = first * dcGain : _y2 = _y1
        End Sub

        Public Function Process(x As Double) As Double
            Dim y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2
            _x2 = _x1 : _x1 = x
            _y2 = _y1 : _y1 = y
            Return y
        End Function

        ''' <summary>Betrag des Frequenzgangs (0 … 1) bei der Frequenz f.</summary>
        Public Function Magnitude(f As Double, fs As Double) As Double
            Dim w = 2.0 * Math.PI * f / fs
            Dim z1 = Complex.FromPolarCoordinates(1.0, -w)
            Dim z2 = Complex.FromPolarCoordinates(1.0, -2.0 * w)
            Dim num = New Complex(_b0, 0) + _b1 * z1 + _b2 * z2
            Dim den = New Complex(1.0, 0) + _a1 * z1 + _a2 * z2
            Return (num / den).Magnitude
        End Function

    End Class

    ''' <summary>Hintereinandergeschaltete Filterstufen.</summary>
    Public NotInheritable Class FilterChain

        Public ReadOnly Property Stages As New List(Of IirStage)
        Public ReadOnly Property SampleRate As Double

        Public Sub New(fs As Double)
            SampleRate = fs
        End Sub

        Public Sub Add(stage As IirStage)
            Stages.Add(stage)
        End Sub

        ''' <summary>Baut die EEG-Filterkette: LowCut (0 = aus), HiCut (0 = aus), optional 50-Hz-Notch.</summary>
        Public Shared Function Create(lowCutHz As Double, highCutHz As Double, notch50 As Boolean, fs As Double) As FilterChain
            Dim chain As New FilterChain(fs)
            If lowCutHz > 0 Then chain.Add(IirStage.HighPass1(lowCutHz, fs))
            If highCutHz > 0 Then chain.Add(IirStage.LowPass2(highCutHz, fs))
            If notch50 Then chain.Add(IirStage.Notch(50.0, 8.0, fs))
            Return chain
        End Function

        Public Sub ProcessInPlace(data As Double())
            If data Is Nothing OrElse data.Length = 0 Then Return
            For Each st In Stages
                st.Reset(data(0))
                For i = 0 To data.Length - 1
                    data(i) = st.Process(data(i))
                Next
            Next
        End Sub

        Public Function Magnitude(f As Double) As Double
            Dim m = 1.0
            For Each st In Stages
                m *= st.Magnitude(f, SampleRate)
            Next
            Return m
        End Function

    End Class

End Namespace
