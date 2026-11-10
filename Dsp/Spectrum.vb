Option Strict On

Namespace Dsp

    ''' <summary>Schnelle Fourier-Transformation (Radix-2) und Frequenzbalken für die Anzeige.</summary>
    Public Module SpectrumAnalysis

        Public Sub Fft(re As Double(), im As Double())
            Dim n = re.Length
            Dim j = 0
            For i = 1 To n - 1
                Dim bit = n >> 1
                While (j And bit) <> 0
                    j = j Xor bit
                    bit >>= 1
                End While
                j = j Xor bit
                If i < j Then
                    Dim t = re(i) : re(i) = re(j) : re(j) = t
                    t = im(i) : im(i) = im(j) : im(j) = t
                End If
            Next

            Dim size = 2
            While size <= n
                Dim half = size \ 2
                Dim ang = -2.0 * Math.PI / size
                Dim wr = Math.Cos(ang), wi = Math.Sin(ang)
                For start = 0 To n - 1 Step size
                    Dim cr = 1.0, ci = 0.0
                    For k = 0 To half - 1
                        Dim a = start + k, b = a + half
                        Dim tr = re(b) * cr - im(b) * ci
                        Dim ti = re(b) * ci + im(b) * cr
                        re(b) = re(a) - tr : im(b) = im(a) - ti
                        re(a) += tr : im(a) += ti
                        Dim ncr = cr * wr - ci * wi
                        ci = cr * wi + ci * wr
                        cr = ncr
                    Next
                Next
                size <<= 1
            End While
        End Sub

        ''' <summary>Amplitudenspektrum mit Hann-Fenster. Länge n/2+1, Auflösung fs/n.</summary>
        Public Function AmplitudeSpectrum(x As Double()) As Double()
            Dim n = 1
            While n * 2 <= x.Length : n *= 2 : End While
            Dim mean = 0.0
            For i = 0 To n - 1 : mean += x(i) : Next
            mean /= n

            Dim re(n - 1) As Double, im(n - 1) As Double
            Dim sumW = 0.0
            For i = 0 To n - 1
                Dim w = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (n - 1))
                re(i) = (x(i) - mean) * w
                sumW += w
            Next
            Fft(re, im)

            Dim amp(n \ 2) As Double
            For k = 0 To n \ 2
                amp(k) = 2.0 * Math.Sqrt(re(k) * re(k) + im(k) * im(k)) / sumW
            Next
            Return amp
        End Function

        ''' <summary>Logarithmisch verteilte Bandgrenzen.</summary>
        Public Function LogEdges(fMin As Double, fMax As Double, count As Integer) As Double()
            Dim e(count) As Double
            Dim l0 = Math.Log10(fMin), l1 = Math.Log10(fMax)
            For i = 0 To count
                e(i) = Math.Pow(10, l0 + (l1 - l0) * i / count)
            Next
            Return e
        End Function

        ''' <summary>Fasst das Spektrum in Balken zusammen (Wurzel der Leistung pro Balken).</summary>
        Public Function Bars(x As Double(), fs As Double, edges As Double()) As Double()
            Dim amp = AmplitudeSpectrum(x)
            Dim n = (amp.Length - 1) * 2
            Dim df = fs / n
            Dim result(edges.Length - 2) As Double
            For k = 0 To result.Length - 1
                Dim i0 = CInt(Math.Ceiling(edges(k) / df))
                Dim i1 = CInt(Math.Floor(edges(k + 1) / df))
                i1 = Math.Min(i1, amp.Length - 1)
                If i1 < i0 Then
                    Dim idx = CInt(Math.Round(Math.Sqrt(edges(k) * edges(k + 1)) / df))
                    result(k) = amp(Math.Max(0, Math.Min(amp.Length - 1, idx)))
                Else
                    Dim p = 0.0
                    For i = i0 To i1 : p += amp(i) * amp(i) : Next
                    result(k) = Math.Sqrt(p)
                End If
            Next
            Return result
        End Function

    End Module

End Namespace
