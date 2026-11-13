Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    ''' <summary>Zeichnet die EEG-Kurven wie auf einem EEG-Monitor (mit Sekundenraster und Eichbalken).</summary>
    Public Class EegView
        Inherits Control

        Public Event ChannelClicked As EventHandler(Of Integer)

        Private _raw As Double()()
        Private _filtered As Double()()
        Private _names As String() = Array.Empty(Of String)()
        Private _peaks As IReadOnlyList(Of Integer)
        Private _fs As Double = 500

        Public Property EcgChannel As Integer = -1
        Public Property EcgScaleDivisor As Double = 10
        Public Property Sensitivity As Double = 7          ' µV/mm
        Public Property WindowSeconds As Double = 10
        Public Property StartSeconds As Double
        Public Property ShowRaw As Boolean
        Public Property ShowRemoved As Boolean
        Public Property SelectedChannel As Integer = 3

        ' ---- Schreiblinie (wie am EEG-Gerät): die Seite steht, eine Linie läuft von links nach rechts
        '      und schreibt die neue Kurve über die alte Seite ----
        ''' <summary>Schreiblinien-Darstellung aktiv (sonst: stehende Seite ab StartSeconds).</summary>
        Public Property SweepActive As Boolean
        ''' <summary>Beginn der Seite, die gerade geschrieben wird (s).</summary>
        Public Property PageStart As Double
        ''' <summary>Beginn der vorherigen Seite, die rechts der Schreiblinie noch steht (s); NaN = keine.</summary>
        Public Property OldPageStart As Double = Double.NaN
        ''' <summary>Position der Schreiblinie innerhalb der Seite (s).</summary>
        Public Property SweepPos As Double
        ''' <summary>Lücke vor der Schreiblinie, in der die alte Kurve schon gelöscht ist (s).</summary>
        Private Const SweepGap As Double = 0.3

        ''' <summary>Ein Kurvenabschnitt: Zeit "From" bis "To", gezeichnet relativ zum Seitenanfang "Origin".</summary>
        Private Structure Segment
            Public Origin As Double
            Public From As Double
            Public [To] As Double
        End Structure

        Private Function Segments() As List(Of Segment)
            Dim list As New List(Of Segment)
            If Not SweepActive Then
                list.Add(New Segment With {.Origin = StartSeconds, .From = StartSeconds, .To = StartSeconds + WindowSeconds})
            Else
                list.Add(New Segment With {.Origin = PageStart, .From = PageStart, .To = PageStart + SweepPos})
                If Not Double.IsNaN(OldPageStart) AndAlso SweepPos + SweepGap < WindowSeconds Then
                    list.Add(New Segment With {.Origin = OldPageStart, .From = OldPageStart + SweepPos + SweepGap, .To = OldPageStart + WindowSeconds})
                End If
            End If
            Return list
        End Function

        ''' <summary>
        ''' Reihenfolge der Kanäle von oben nach unten (Kanalindex je Zeile).
        ''' DGKN-Empfehlung zu Ableiteprogrammen: rechts über links, anterior über posterior, EKG zuletzt.
        ''' </summary>
        Public Property DisplayOrder As Integer() = {4, 5, 6, 7, 0, 1, 2, 3, 8}

        Private Function RowOf(channel As Integer) As Integer
            Dim r = Array.IndexOf(DisplayOrder, channel)
            Return If(r < 0, channel, r)
        End Function

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.Paper
        End Sub

        Public Sub SetData(names As String(), raw As Double()(), filtered As Double()(), fs As Double, peaks As IReadOnlyList(Of Integer))
            _names = names : _raw = raw : _filtered = filtered : _fs = fs : _peaks = peaks
            Invalidate()
        End Sub

        Private ReadOnly Property Sc As Single
            Get
                Return DeviceDpi / 96.0F
            End Get
        End Property

        Private Function PlotRect() As RectangleF
            Dim lw = 96 * Sc
            ' oben ein eigener Streifen für die Legende, damit sie keine Kurven verdeckt
            Return New RectangleF(lw, 28 * Sc, Width - lw - 78 * Sc, Height - 28 * Sc - 24 * Sc)
        End Function

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            Cursor = If(e.X < PlotRect().Left, Cursors.Hand, Cursors.Default)
        End Sub

        Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
            MyBase.OnMouseClick(e)
            If _filtered Is Nothing Then Return
            Dim plot = PlotRect()
            Dim rowH = plot.Height / _filtered.Length
            Dim row = CInt(Math.Floor((e.Y - plot.Top) / rowH))
            If row >= 0 AndAlso row < _filtered.Length Then RaiseEvent ChannelClicked(Me, DisplayOrder(row))
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(Theme.Paper)
            If _filtered Is Nothing Then Return

            Dim plot = PlotRect()
            If plot.Width < 40 OrElse plot.Height < 40 Then Return
            Dim nCh = _filtered.Length
            Dim rowH = plot.Height / nCh
            Dim secPx = plot.Width / CSng(WindowSeconds)
            Dim pxPerMm = DeviceDpi / 25.4F

            ' Kanal-Spalte
            Using b As New SolidBrush(Theme.LabelColumn)
                g.FillRectangle(b, 0, 0, plot.Left, Height)
            End Using
            If SelectedChannel >= 0 AndAlso SelectedChannel < nCh Then
                Using b As New SolidBrush(Theme.Selected)
                    g.FillRectangle(b, 0, plot.Top + rowH * RowOf(SelectedChannel), Width, rowH)
                End Using
            End If

            ' Raster: 0,2 s fein, 1 s kräftig
            Dim origin = If(SweepActive, PageStart, StartSeconds)
            Dim firstTick = CInt(Math.Ceiling(origin * 5 - 0.000001))
            Dim lastTick = CInt(Math.Floor((origin + WindowSeconds) * 5 + 0.000001))
            Using pMinor As New Pen(Theme.GridMinor, 1), pMajor As New Pen(Theme.GridMajor, 1)
                For k = firstTick To lastTick
                    Dim t = k / 5.0
                    Dim x = plot.Left + CSng((t - origin) * secPx)
                    Dim major = (k Mod 5 = 0)
                    g.DrawLine(If(major, pMajor, pMinor), x, plot.Top, x, plot.Bottom)
                    ' rechts der Schreiblinie steht noch die alte Seite: dort gilt deren Zeit
                    Dim labelTime = t
                    Dim showLabel = major
                    If SweepActive AndAlso t - origin > SweepPos + 0.000001 Then
                        If Double.IsNaN(OldPageStart) Then showLabel = False Else labelTime = OldPageStart + (t - origin)
                    End If
                    If showLabel Then
                        Dim lbl = $"{CInt(Math.Round(labelTime))} s"
                        Dim sz = TextRenderer.MeasureText(lbl, Theme.FontSmall)
                        TextRenderer.DrawText(g, lbl, Theme.FontSmall, New Point(CInt(x - sz.Width / 2), CInt(plot.Bottom + 4 * Sc)), Theme.TextMuted)
                    End If
                Next
                For c = 1 To nCh - 1
                    Dim y = plot.Top + rowH * c
                    g.DrawLine(pMinor, 0, y, Width, y)
                Next
            End Using

            ' Kanalnamen
            For c = 0 To nCh - 1
                Dim r As New Rectangle(CInt(10 * Sc), CInt(plot.Top + rowH * RowOf(c)), CInt(plot.Left - 14 * Sc), CInt(rowH))
                Dim txt = _names(c)
                Dim col = If(c = SelectedChannel, Theme.AccentDark, Theme.TextDark)
                If c = EcgChannel Then
                    Dim h1 = TextRenderer.MeasureText(txt, Theme.FontChannel).Height
                    Dim h2 = TextRenderer.MeasureText("X", Theme.FontSmall).Height
                    Dim y0 = r.Y + (r.Height - h1 - h2) \ 2
                    TextRenderer.DrawText(g, txt, Theme.FontChannel, New Point(r.X, y0), col)
                    TextRenderer.DrawText(g, $"Skala 1:{EcgScaleDivisor:0}", Theme.FontSmall, New Point(r.X, y0 + h1), Theme.TextMuted)
                Else
                    TextRenderer.DrawText(g, txt, Theme.FontChannel, r, col, TextFormatFlags.VerticalCenter Or TextFormatFlags.Left)
                End If
            Next
            Using pLine As New Pen(Theme.Border, 1)
                g.DrawLine(pLine, plot.Left, 0, plot.Left, Height)
            End Using

            ' Kurven
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim oldClip = g.Clip
            g.SetClip(New RectangleF(plot.Left + 1, plot.Top, plot.Width, plot.Height))
            Using pRaw As New Pen(Theme.TraceRaw, 1.4F * Sc),
                  pTrace As New Pen(Theme.Trace, 1.15F * Sc),
                  pRem As New Pen(Color.FromArgb(215, Theme.Removed), 1.15F * Sc)
                pRaw.LineJoin = LineJoin.Round : pTrace.LineJoin = LineJoin.Round : pRem.LineJoin = LineJoin.Round
                For c = 0 To nCh - 1
                    Dim cy = plot.Top + rowH * (RowOf(c) + 0.5F)
                    Dim pxPerUv = pxPerMm / CSng(Sensitivity)
                    If c = EcgChannel Then pxPerUv /= CSng(EcgScaleDivisor)
                    For Each seg In Segments()
                        If ShowRaw Then DrawTrace(g, _raw(c), Nothing, plot, cy, pxPerUv, pRaw, seg)
                        DrawTrace(g, _filtered(c), Nothing, plot, cy, pxPerUv, pTrace, seg)
                        If ShowRemoved Then DrawTrace(g, _raw(c), _filtered(c), plot, cy, pxPerUv, pRem, seg)
                    Next
                Next
            End Using

            ' erkannte Herzschläge (EKG-Filter)
            If _peaks IsNot Nothing AndAlso EcgChannel >= 0 Then
                Using b As New SolidBrush(Theme.ColEcg)
                    Dim yTop = plot.Top + rowH * RowOf(EcgChannel) + 3 * Sc
                    Dim s = 5 * Sc
                    For Each seg In Segments()
                        For Each p In _peaks
                            Dim t = p / _fs
                            If t < seg.From OrElse t > seg.To Then Continue For
                            Dim x = plot.Left + CSng((t - seg.Origin) * secPx)
                            g.FillPolygon(b, {New PointF(x - s, yTop), New PointF(x + s, yTop), New PointF(x, yTop + s * 1.4F)})
                        Next
                    Next
                End Using
            End If

            ' Schreiblinie
            If SweepActive Then
                Dim sx = plot.Left + CSng(SweepPos * secPx)
                Using glow As New Pen(Color.FromArgb(45, Theme.Accent), 7 * Sc), pen As New Pen(Theme.Accent, 2 * Sc)
                    g.DrawLine(glow, sx, plot.Top, sx, plot.Bottom)
                    g.DrawLine(pen, sx, plot.Top, sx, plot.Bottom)
                End Using
            End If
            g.Clip = oldClip

            DrawCalibration(g, plot, pxPerMm)
            DrawLegend(g, plot)
        End Sub

        ''' <summary>Zeichnet eine Kurve; bei mehr Abtastwerten als Pixeln mit Min/Max-Verdichtung.</summary>
        Private Sub DrawTrace(g As Graphics, a As Double(), b As Double(), plot As RectangleF, cy As Single, pxPerUv As Single, pen As Pen, seg As Segment)
            Dim n = a.Length
            Dim secPx = plot.Width / WindowSeconds
            If seg.To <= seg.From Then Return
            Dim i0 = Math.Max(0, CInt(Math.Floor(seg.From * _fs)))
            Dim i1 = Math.Min(n - 1, CInt(Math.Floor(seg.To * _fs)))
            If i1 <= i0 Then Return
            Dim limit = plot.Height * 3
            Dim yOf = Function(idx As Integer) As Single
                          Dim v = a(idx)
                          If b IsNot Nothing Then v -= b(idx)
                          Dim y = cy + CSng(v) * pxPerUv      ' EEG-Konvention: negativ nach oben
                          Return Math.Max(-limit, Math.Min(limit + plot.Bottom, y))
                      End Function
            Dim xOf = Function(idx As Integer) As Single
                          Return plot.Left + CSng((idx / _fs - seg.Origin) * secPx)
                      End Function

            Dim xStart = xOf(i0)
            Dim cols = Math.Max(1, CInt(Math.Ceiling(xOf(i1) - xStart)))
            Dim samplesPerPx = (i1 - i0) / CDbl(cols)
            Dim pts As New List(Of PointF)(cols * 2 + 4)
            If samplesPerPx <= 2.0 Then
                For i = i0 To i1
                    pts.Add(New PointF(xOf(i), yOf(i)))
                Next
            Else
                Dim idx = i0
                For col = 0 To cols - 1
                    Dim lastIdx = Math.Min(i1, i0 + CInt((col + 1) * samplesPerPx))
                    If lastIdx < idx Then Continue For
                    Dim minI = idx, maxI = idx
                    Dim minY = yOf(idx), maxY = minY
                    For i = idx + 1 To lastIdx
                        Dim y = yOf(i)
                        If y < minY Then minY = y : minI = i
                        If y > maxY Then maxY = y : maxI = i
                    Next
                    Dim x = xStart + col
                    If minI <= maxI Then
                        pts.Add(New PointF(x, minY)) : pts.Add(New PointF(x + 0.5F, maxY))
                    Else
                        pts.Add(New PointF(x, maxY)) : pts.Add(New PointF(x + 0.5F, minY))
                    End If
                    idx = lastIdx + 1
                    If idx > i1 Then Exit For
                Next
            End If
            If pts.Count >= 2 Then g.DrawLines(pen, pts.ToArray())
        End Sub

        Private Sub DrawCalibration(g As Graphics, plot As RectangleF, pxPerMm As Single)
            ' Eichbalken: wählt einen runden Wert, der etwa 8–15 mm hoch ist
            Dim candidates = {10, 20, 50, 70, 100, 200, 500, 1000}
            Dim uv = candidates.First(Function(v) v / Sensitivity >= 7 OrElse v = 1000)
            Dim h = CSng(uv / Sensitivity) * pxPerMm
            Dim x = plot.Right + 16 * Sc
            Dim yb = plot.Bottom - 8 * Sc
            Using p As New Pen(Theme.Trace, 2 * Sc)
                g.DrawLine(p, x, yb, x, yb - h)
                g.DrawLine(p, x - 4 * Sc, yb, x + 4 * Sc, yb)
                g.DrawLine(p, x - 4 * Sc, yb - h, x + 4 * Sc, yb - h)
            End Using
            TextRenderer.DrawText(g, $"{uv} µV", Theme.FontSmallBold, New Point(CInt(x + 8 * Sc), CInt(yb - h / 2 - 8 * Sc)), Theme.Trace)
            TextRenderer.DrawText(g, "Eichung", Theme.FontSmall, New Point(CInt(x - 8 * Sc), CInt(yb - h - 22 * Sc)), Theme.TextMuted)
            TextRenderer.DrawText(g, "neg. ▲", Theme.FontSmall, New Point(CInt(x - 8 * Sc), CInt(plot.Top + 2 * Sc)), Theme.TextMuted)
        End Sub

        Private Sub DrawLegend(g As Graphics, plot As RectangleF)
            Dim items As New List(Of (String, Color))
            items.Add(("EEG (gefiltert)", Theme.Trace))
            If ShowRaw Then items.Add(("Rohsignal (ungefiltert)", Theme.TraceRaw))
            If ShowRemoved Then items.Add(("vom Filter entfernt", Theme.Removed))
            If _peaks IsNot Nothing Then items.Add(("erkannter Herzschlag", Theme.ColEcg))

            Dim widths = items.Select(Function(it) TextRenderer.MeasureText(it.Item1, Theme.FontSmall).Width + CInt(28 * Sc)).ToList()
            Dim total = widths.Sum() + CInt(10 * Sc)
            Dim x = plot.Right - total
            Dim h = 20 * Sc
            Dim y = (plot.Top - h) / 2
            Using b As New SolidBrush(Color.FromArgb(230, Color.White)), p As New Pen(Theme.Border)
                Dim r As New RectangleF(x, y, total, h)
                Using path = Theme.RoundRect(r, 5 * Sc)
                    g.FillPath(b, path)
                    g.DrawPath(p, path)
                End Using
            End Using
            Dim cx = x + 8 * Sc
            For i = 0 To items.Count - 1
                Using pen As New Pen(items(i).Item2, 2.5F * Sc)
                    g.DrawLine(pen, cx, y + h / 2, cx + 14 * Sc, y + h / 2)
                End Using
                TextRenderer.DrawText(g, items(i).Item1, Theme.FontSmall,
                                      New Rectangle(CInt(cx + 18 * Sc), CInt(y), widths(i), CInt(h)), Theme.TextDark,
                                      TextFormatFlags.VerticalCenter Or TextFormatFlags.Left Or TextFormatFlags.NoPadding)
                cx += widths(i)
            Next
        End Sub

    End Class

End Namespace
