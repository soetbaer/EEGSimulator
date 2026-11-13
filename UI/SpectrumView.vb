Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms

Namespace UI

    ''' <summary>
    ''' Frequenz-Ansicht: Balken zeigen, wie viel von jeder "Wellen-Geschwindigkeit" im Signal steckt
    ''' (grau = vorher, farbig = nach dem Filter). Die Linie zeigt den Durchlass des Filters.
    ''' </summary>
    Public Class SpectrumView
        Inherits Control

        Public Const FMin As Double = 0.1
        Public Const FMax As Double = 150.0

        Private Shared ReadOnly De As CultureInfo = CultureInfo.GetCultureInfo("de-DE")

        Private _edges As Double()
        Private _raw As Double()
        Private _filt As Double()
        Private _response As Func(Of Double, Double)

        Public Property LowCutHz As Double
        Public Property HighCutHz As Double
        Public Property Notch As Boolean
        Public Property ChannelName As String = ""

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.Card
        End Sub

        Public Sub SetData(edges As Double(), rawBars As Double(), filtBars As Double(), response As Func(Of Double, Double))
            _edges = edges : _raw = rawBars : _filt = filtBars : _response = response
            Invalidate()
        End Sub

        Private Shared Function L(f As Double) As Double
            Return Math.Log10(f)
        End Function

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(Theme.Card)
            Dim sc = DeviceDpi / 96.0F

            ' ---- Titel + Legende ----
            Dim title = $"Frequenz-Ansicht  ·  Kanal {ChannelName}"
            TextRenderer.DrawText(g, title, Theme.FontBold, New Point(CInt(12 * sc), CInt(8 * sc)), Theme.TextDark)
            Dim hint = "links = langsame Wellen  ·  rechts = schnelle Wellen"
            Dim tw = TextRenderer.MeasureText(title, Theme.FontBold).Width
            Dim legendLeft = DrawLegend(g, sc)
            Dim hx = CInt(12 * sc + tw + 14 * sc)
            If hx + TextRenderer.MeasureText(hint, Theme.FontSmall).Width < legendLeft - 10 * sc Then
                TextRenderer.DrawText(g, hint, Theme.FontSmall, New Point(hx, CInt(10 * sc)), Theme.TextMuted)
            End If

            If _edges Is Nothing OrElse _raw Is Nothing Then Return

            Dim stripTop = 34 * sc
            Dim stripH = 18 * sc
            Dim plot As New RectangleF(52 * sc, stripTop + stripH + 4 * sc, Width - 52 * sc - 18 * sc, Height - (stripTop + stripH + 4 * sc) - 26 * sc)
            If plot.Width < 50 OrElse plot.Height < 30 Then Return

            Dim lmin = L(FMin), lmax = L(FMax)
            Dim xOf = Function(f As Double) As Single
                          Return plot.Left + CSng((L(Math.Max(FMin, Math.Min(FMax, f))) - lmin) / (lmax - lmin) * plot.Width)
                      End Function
            Dim fOf = Function(x As Single) As Double
                          Return Math.Pow(10, lmin + (x - plot.Left) / plot.Width * (lmax - lmin))
                      End Function

            ' ---- Band-Streifen ----
            Dim bands = {("Drift", 0.1, 0.5, Theme.ColDrift), ("Delta", 0.5, 4.0, Theme.ColDelta), ("Theta", 4.0, 8.0, Theme.ColTheta),
                         ("Alpha", 8.0, 13.0, Theme.ColAlpha), ("Beta", 13.0, 30.0, Theme.ColBeta), ("schnell / Muskel", 30.0, 150.0, Theme.ColFast)}
            For Each bd In bands
                Dim x0 = xOf(bd.Item2), x1 = xOf(bd.Item3)
                Dim r As New RectangleF(x0, stripTop, x1 - x0 - 1, stripH)
                Using b As New SolidBrush(Color.FromArgb(40, bd.Item4))
                    g.FillRectangle(b, r)
                End Using
                Using b As New SolidBrush(bd.Item4)
                    g.FillRectangle(b, r.X, r.Bottom - 3 * sc, r.Width, 3 * sc)
                End Using
                TextRenderer.DrawText(g, bd.Item1, Theme.FontSmallBold, Rectangle.Round(r), Theme.Blend(bd.Item4, Color.Black, 0.55),
                                      TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
            Next

            ' ---- Plot-Hintergrund + Sperrbereich-Schattierung ----
            Using b As New SolidBrush(Theme.Paper)
                g.FillRectangle(b, plot)
            End Using
            If _response IsNot Nothing Then
                Dim stepPx = Math.Max(1.0F, 2 * sc)
                Dim x = plot.Left
                While x < plot.Right
                    Dim h = _response(fOf(x + stepPx / 2))
                    Dim a = CInt(Math.Max(0, Math.Min(1, 1 - h)) * 55)
                    If a > 1 Then
                        Using b As New SolidBrush(Color.FromArgb(a, Theme.Removed))
                            g.FillRectangle(b, x, plot.Top, stepPx + 0.5F, plot.Height)
                        End Using
                    End If
                    x += stepPx
                End While
            End If

            ' horizontale Hilfslinien (Durchlass in %)
            Using p As New Pen(Theme.GridMinor, 1)
                For Each pct In {0.25, 0.5, 0.75, 1.0}
                    Dim y = plot.Bottom - CSng(pct) * plot.Height
                    g.DrawLine(p, plot.Left, y, plot.Right, y)
                    TextRenderer.DrawText(g, $"{pct * 100:0} %", Theme.FontSmall,
                                          New Rectangle(0, CInt(y - 8 * sc), CInt(plot.Left - 6 * sc), CInt(16 * sc)), Theme.TextMuted,
                                          TextFormatFlags.Right Or TextFormatFlags.VerticalCenter)
                Next
            End Using

            ' ---- Balken ----
            Dim vmax = Math.Max(0.000001, _raw.Max())
            g.SmoothingMode = SmoothingMode.None
            For k = 0 To _raw.Length - 1
                Dim x0 = xOf(_edges(k)), x1 = xOf(_edges(k + 1))
                Dim bw = Math.Max(1.0F, x1 - x0 - 1.5F * sc)
                Dim hr = CSng(Math.Sqrt(_raw(k) / vmax)) * plot.Height * 0.94F
                Dim hf = CSng(Math.Sqrt(Math.Max(0, _filt(k)) / vmax)) * plot.Height * 0.94F
                Using b As New SolidBrush(Theme.SpectrumBefore)
                    g.FillRectangle(b, x0, plot.Bottom - hr, bw, hr)
                End Using
                Dim fc = Math.Sqrt(_edges(k) * _edges(k + 1))
                Using b As New SolidBrush(Color.FromArgb(225, Theme.BandColor(fc)))
                    g.FillRectangle(b, x0, plot.Bottom - hf, bw, hf)
                End Using
            Next

            ' ---- Filterkurve ----
            g.SmoothingMode = SmoothingMode.AntiAlias
            If _response IsNot Nothing Then
                Dim pts As New List(Of PointF)
                Dim x = plot.Left
                While x <= plot.Right
                    Dim h = Math.Max(0, Math.Min(1.05, _response(fOf(x))))
                    pts.Add(New PointF(x, plot.Bottom - CSng(h) * plot.Height))
                    x += 1
                End While
                Using p As New Pen(Color.FromArgb(235, Theme.Teal), 2.6F * sc)
                    p.LineJoin = LineJoin.Round
                    g.DrawLines(p, pts.ToArray())
                End Using
            End If

            ' ---- Markierungen FU / FO / 50 Hz ----
            If LowCutHz > 0 Then DrawMarker(g, plot, xOf(LowCutHz), $"FU {FormatHz(LowCutHz)} Hz", sc, True)
            If HighCutHz > 0 Then DrawMarker(g, plot, xOf(HighCutHz), $"FO {FormatHz(HighCutHz)} Hz", sc, False)
            If Notch Then DrawMarker(g, plot, xOf(50), "50 Hz", sc, HighCutHz > 0 AndAlso HighCutHz <= 70)

            ' ---- X-Achse ----
            Using p As New Pen(Theme.GridMajor, 1)
                g.DrawLine(p, plot.Left, plot.Bottom, plot.Right, plot.Bottom)
                For Each f In {0.1, 0.3, 0.5, 1.0, 2.0, 4.0, 8.0, 13.0, 20.0, 30.0, 50.0, 70.0, 150.0}
                    Dim x = xOf(f)
                    g.DrawLine(p, x, plot.Bottom, x, plot.Bottom + 4 * sc)
                    Dim lbl = If(f >= FMax, FormatHz(f) & " Hz", FormatHz(f))
                    Dim sz = TextRenderer.MeasureText(lbl, Theme.FontSmall)
                    Dim lx = If(f >= FMax, CInt(x - sz.Width + 4 * sc), CInt(x - sz.Width / 2))
                    TextRenderer.DrawText(g, lbl, Theme.FontSmall, New Point(lx, CInt(plot.Bottom + 5 * sc)), Theme.TextMuted)
                Next
            End Using
        End Sub

        Private Shared Sub DrawMarker(g As Graphics, plot As RectangleF, x As Single, text As String, sc As Single, labelLeft As Boolean)
            Using p As New Pen(Theme.TextDark, 1.2F * sc)
                p.DashStyle = DashStyle.Dash
                g.DrawLine(p, x, plot.Top, x, plot.Bottom)
            End Using
            Dim sz = TextRenderer.MeasureText(text, Theme.FontSmallBold)
            Dim tx = If(labelLeft, x - sz.Width - 4 * sc, x + 4 * sc)
            Dim r As New RectangleF(tx - 3 * sc, plot.Top + 4 * sc, sz.Width + 6 * sc, sz.Height + 2 * sc)
            Using b As New SolidBrush(Color.FromArgb(235, Color.White)), path = Theme.RoundRect(r, 4 * sc)
                g.FillPath(b, path)
            End Using
            TextRenderer.DrawText(g, text, Theme.FontSmallBold, New Point(CInt(tx), CInt(plot.Top + 5 * sc)), Theme.TextDark)
        End Sub

        ''' <summary>Zeichnet die Legende rechtsbündig und gibt deren linken Rand zurück.</summary>
        Private Function DrawLegend(g As Graphics, sc As Single) As Single
            Dim items = {("vorher", Theme.SpectrumBefore, 0), ("nach dem Filter", Theme.ColAlpha, 0), ("Durchlass des Filters (Skala links)", Theme.Teal, 1), ("wird gedämpft", Color.FromArgb(90, Theme.Removed), 0)}
            Dim x = Width - 12 * sc
            Dim y = 10 * sc
            For i = items.Length - 1 To 0 Step -1
                Dim it = items(i)
                Dim w = TextRenderer.MeasureText(it.Item1, Theme.FontSmall).Width
                x -= w
                TextRenderer.DrawText(g, it.Item1, Theme.FontSmall, New Point(CInt(x), CInt(y)), Theme.TextDark)
                x -= 18 * sc
                If it.Item3 = 1 Then
                    Using p As New Pen(it.Item2, 2.6F * sc)
                        g.DrawLine(p, x, y + 8 * sc, x + 14 * sc, y + 8 * sc)
                    End Using
                Else
                    Using b As New SolidBrush(it.Item2), p As New Pen(Theme.Border)
                        g.FillRectangle(b, x + 2 * sc, y + 3 * sc, 11 * sc, 11 * sc)
                        g.DrawRectangle(p, x + 2 * sc, y + 3 * sc, 11 * sc, 11 * sc)
                    End Using
                End If
                x -= 14 * sc
            Next
            Return x
        End Function

        Public Shared Function FormatHz(f As Double) As String
            If f < 1 Then Return f.ToString("0.0#", De)
            If f < 10 AndAlso Math.Abs(f - Math.Round(f)) > 0.05 Then Return f.ToString("0.0", De)
            Return f.ToString("0", De)
        End Function

    End Class

End Namespace
