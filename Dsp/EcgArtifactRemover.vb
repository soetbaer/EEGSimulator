Option Strict On

Namespace Dsp

    ''' <summary>
    ''' EKG-Filter nach dem Prinzip der Template-Subtraktion:
    ''' 1. R-Zacken im EKG-Kanal finden.
    ''' 2. Pro EEG-Kanal die Abschnitte um jede R-Zacke mitteln → "Schablone" (Template)
    '''    (die Hirnaktivität mittelt sich heraus, das herzschlag-gekoppelte Artefakt bleibt).
    ''' 3. Die Schablone bei jedem Herzschlag abziehen.
    ''' </summary>
    Public NotInheritable Class EcgArtifactRemover

        Public Const PreSeconds As Double = 0.25
        Public Const PostSeconds As Double = 0.45

        Private Sub New()
        End Sub

        Public Shared Function DetectRPeaks(ecg As Double(), fs As Double) As List(Of Integer)
            Dim peaks As New List(Of Integer)
            Dim n = ecg.Length
            If n < 10 Then Return peaks

            ' Bandpass 8–25 Hz: betont den steilen QRS-Komplex, unterdrückt Drift und Brumm
            Dim x = CType(ecg.Clone(), Double())
            Dim bp As New FilterChain(fs)
            bp.Add(IirStage.HighPass2(8, fs))
            bp.Add(IirStage.LowPass2(25, fs))
            bp.ProcessInPlace(x)

            ' Energie der Steigung, geglättet über 40 ms
            Dim e(n - 1) As Double
            For i = 1 To n - 2
                Dim d = x(i + 1) - x(i - 1)
                e(i) = d * d
            Next
            Dim win = Math.Max(1, CInt(0.04 * fs))
            Dim sm(n - 1) As Double
            Dim acc = 0.0
            For i = 0 To n - 1
                acc += e(i)
                If i >= win Then acc -= e(i - win)
                sm(i) = acc / win
            Next

            Dim sorted = CType(sm.Clone(), Double())
            Array.Sort(sorted)
            Dim thr = 0.3 * sorted(CInt(0.998 * (n - 1)))
            If thr <= 0 Then Return peaks

            Dim refractory = CInt(0.25 * fs)
            Dim search = CInt(0.08 * fs)
            Dim i2 = 0
            While i2 < n
                If sm(i2) > thr Then
                    Dim s = i2
                    While i2 < n AndAlso sm(i2) > thr : i2 += 1 : End While
                    Dim a = Math.Max(0, s - search), b = Math.Min(n - 1, i2 + search \ 2)
                    ' Lage der R-Zacke im Original-EKG (größter Betrag gegenüber lokalem Mittel)
                    Dim mean = 0.0
                    For k = a To b : mean += ecg(k) : Next
                    mean /= (b - a + 1)
                    Dim best = a, bestV = -1.0
                    For k = a To b
                        Dim v = Math.Abs(ecg(k) - mean)
                        If v > bestV Then bestV = v : best = k
                    Next
                    If peaks.Count = 0 OrElse best - peaks(peaks.Count - 1) > refractory Then peaks.Add(best)
                Else
                    i2 += 1
                End If
            End While
            Return peaks
        End Function

        ''' <summary>Berechnet die mittlere Artefakt-Schablone eines Kanals (oder Nothing).</summary>
        Public Shared Function BuildTemplate(signal As Double(), peaks As IList(Of Integer), fs As Double) As Double()
            Dim pre = CInt(PreSeconds * fs), post = CInt(PostSeconds * fs)
            Dim tplLen = pre + post + 1
            Dim tpl(tplLen - 1) As Double
            Dim sq(tplLen - 1) As Double
            Dim count = 0
            For Each p In peaks
                If p - pre < 0 OrElse p + post >= signal.Length Then Continue For
                For k = 0 To tplLen - 1
                    Dim v = signal(p - pre + k)
                    tpl(k) += v
                    sq(k) += v * v
                Next
                count += 1
            Next
            If count < 3 Then Return Nothing
            ' Standardfehler des Mittelwerts je Zeitpunkt (= Rest-Rauschen der Schablone)
            Dim se(tplLen - 1) As Double
            For k = 0 To tplLen - 1
                tpl(k) /= count
                Dim variance = Math.Max(0, sq(k) / count - tpl(k) * tpl(k))
                se(k) = Math.Sqrt(variance / count)
            Next

            ' Grundlinie (linearer Trend zwischen Anfang und Ende) entfernen
            Dim edge = Math.Max(1, tplLen \ 20)
            Dim m0 = 0.0, m1 = 0.0
            For k = 0 To edge - 1
                m0 += tpl(k) : m1 += tpl(tplLen - 1 - k)
            Next
            m0 /= edge : m1 /= edge
            For k = 0 To tplLen - 1
                tpl(k) -= m0 + (m1 - m0) * k / (tplLen - 1)
            Next

            ' Schrumpfung: Nur was deutlich über dem Rest-Rauschen liegt, gehört zum Artefakt.
            ' (Verhindert, dass gemitteltes EEG-Rauschen mit abgezogen wird.)
            Dim half = Math.Max(1, CInt(0.02 * fs))
            Dim shrunk(tplLen - 1) As Double
            For k = 0 To tplLen - 1
                Dim a = Math.Max(0, k - half), b = Math.Min(tplLen - 1, k + half)
                Dim pw = 0.0, nz = 0.0
                For j = a To b
                    pw += tpl(j) * tpl(j)
                    nz += se(j) * se(j)
                Next
                Dim noise = 4.0 * nz      ' ≈ (2·Standardfehler)²
                shrunk(k) = tpl(k) * pw / (pw + noise + 0.000000001)
            Next
            tpl = shrunk

            ' Ränder weich ausblenden (Tukey-Fenster), damit beim Abziehen keine Stufen entstehen
            Dim taperLen = CInt(tplLen * 0.15)
            For k = 0 To taperLen - 1
                Dim w = 0.5 - 0.5 * Math.Cos(Math.PI * k / taperLen)
                tpl(k) *= w
                tpl(tplLen - 1 - k) *= w
            Next
            Return tpl
        End Function

        ''' <summary>Zieht die Schablone bei jedem Herzschlag vom Signal ab.</summary>
        Public Shared Sub Remove(signal As Double(), peaks As IList(Of Integer), fs As Double)
            If peaks Is Nothing OrElse peaks.Count < 3 Then Return
            Dim tpl = BuildTemplate(signal, peaks, fs)
            If tpl Is Nothing Then Return
            Dim pre = CInt(PreSeconds * fs)
            For Each p In peaks
                For k = 0 To tpl.Length - 1
                    Dim idx = p - pre + k
                    If idx >= 0 AndAlso idx < signal.Length Then signal(idx) -= tpl(k)
                Next
            Next
        End Sub

    End Class

End Namespace
