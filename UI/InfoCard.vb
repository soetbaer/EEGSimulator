Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    Public Enum BlockKind
        Title
        Subtitle
        Heading
        Paragraph
        Bullet
        Tip
        Warning
        Good
        TryIt
        Meter
        Divider
    End Enum

    ''' <summary>Ein Baustein des Erklärtextes (Absatz, Merke-Box, Balken …).</summary>
    Public NotInheritable Class InfoBlock
        Public Property Kind As BlockKind
        Public Property Text As String = ""
        Public Property Value As Double
        Public Property Accent As Color = Color.Empty
        Public Property ValueText As String = ""

        Public Shared Function Title(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Title, .Text = t}
        End Function
        Public Shared Function Subtitle(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Subtitle, .Text = t}
        End Function
        Public Shared Function Heading(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Heading, .Text = t}
        End Function
        Public Shared Function Para(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Paragraph, .Text = t}
        End Function
        Public Shared Function Bullet(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Bullet, .Text = t}
        End Function
        Public Shared Function Tip(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Tip, .Text = t}
        End Function
        Public Shared Function Warning(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Warning, .Text = t}
        End Function
        Public Shared Function Good(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Good, .Text = t}
        End Function
        Public Shared Function TryIt(t As String) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.TryIt, .Text = t}
        End Function
        Public Shared Function Divider() As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Divider}
        End Function
        Public Shared Function Meter(label As String, value As Double, accent As Color, Optional valueText As String = Nothing) As InfoBlock
            Return New InfoBlock With {.Kind = BlockKind.Meter, .Text = label, .Value = value, .Accent = accent,
                                       .ValueText = If(valueText, $"{Math.Round(value * 100)} %")}
        End Function
    End Class

    ''' <summary>Scrollbare "Karte", die InfoBlocks ansprechend darstellt.</summary>
    Public Class InfoCard
        Inherits Panel

        Private _blocks As New List(Of InfoBlock)
        Private Const Flags As TextFormatFlags = TextFormatFlags.WordBreak Or TextFormatFlags.NoPadding Or TextFormatFlags.TextBoxControl

        Public Sub New()
            DoubleBuffered = True
            ResizeRedraw = True
            AutoScroll = True
            BackColor = Theme.Card
        End Sub

        Public Sub SetBlocks(blocks As IEnumerable(Of InfoBlock), resetScroll As Boolean)
            Dim pos = AutoScrollPosition
            _blocks = blocks.ToList()
            UpdateExtent()
            If resetScroll Then
                AutoScrollPosition = New Point(0, 0)
            Else
                AutoScrollPosition = New Point(-pos.X, -pos.Y)
            End If
            Invalidate()
        End Sub

        Private Sub UpdateExtent()
            If Not IsHandleCreated Then Return
            Using g = CreateGraphics()
                Dim h = LayoutBlocks(g, False)
                AutoScrollMinSize = New Size(0, h)
            End Using
        End Sub

        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            UpdateExtent()
        End Sub

        Protected Overrides Sub OnResize(e As EventArgs)
            MyBase.OnResize(e)
            UpdateExtent()
            Invalidate()
        End Sub

        Protected Overrides Sub OnScroll(se As ScrollEventArgs)
            MyBase.OnScroll(se)
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
            MyBase.OnMouseWheel(e)
            Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            LayoutBlocks(e.Graphics, True)
        End Sub

        Private Shared ReadOnly BoldFonts As New Dictionary(Of Font, Font)
        Private Const RunFlags As TextFormatFlags = TextFormatFlags.NoPadding Or TextFormatFlags.SingleLine Or TextFormatFlags.NoPrefix

        Private Shared Function BoldOf(f As Font) As Font
            Dim b As Font = Nothing
            If Not BoldFonts.TryGetValue(f, b) Then
                b = New Font(f, FontStyle.Bold)
                BoldFonts(f) = b
            End If
            Return b
        End Function

        ''' <summary>
        ''' Misst (und zeichnet optional) einen umbrechenden Text. Mit **…** markierte Stellen werden fett
        ''' geschrieben – zur Betonung statt Großbuchstaben. Gibt die Höhe zurück.
        ''' </summary>
        Private Shared Function RichText(g As Graphics, text As String, font As Font, x As Integer, y As Integer, width As Integer,
                                         color As Color, draw As Boolean) As Integer
            If String.IsNullOrEmpty(text) Then Return 0
            If Not text.Contains("**") Then
                Dim h0 = Measure(g, text, font, width)
                If draw Then TextRenderer.DrawText(g, text, font, New Rectangle(x, y, width, h0), color, Flags)
                Return h0
            End If
            Dim bold = BoldOf(font)
            ' Wörter aus Abschnitten (normal/fett); ein Wort kann den Stil wechseln, z. B. **nicht**:
            Dim words As New List(Of List(Of (Text As String, Bold As Boolean)))
            Dim word As New List(Of (Text As String, Bold As Boolean))
            Dim run As New System.Text.StringBuilder()
            Dim parts = text.Split({"**"}, StringSplitOptions.None)
            For pi = 0 To parts.Length - 1
                Dim isBold = (pi Mod 2 = 1)
                For Each ch In parts(pi)
                    If ch = " "c Then
                        If run.Length > 0 Then word.Add((run.ToString(), isBold)) : run.Clear()
                        If word.Count > 0 Then words.Add(word) : word = New List(Of (Text As String, Bold As Boolean))
                    Else
                        run.Append(ch)
                    End If
                Next
                If run.Length > 0 Then word.Add((run.ToString(), isBold)) : run.Clear()
            Next
            If word.Count > 0 Then words.Add(word)

            Dim lineH = Math.Max(TextRenderer.MeasureText(g, "Ag", font, Size.Empty, RunFlags).Height,
                                 TextRenderer.MeasureText(g, "Ag", bold, Size.Empty, RunFlags).Height)
            Dim spaceW = TextRenderer.MeasureText(g, "a a", font, Size.Empty, RunFlags).Width -
                         TextRenderer.MeasureText(g, "aa", font, Size.Empty, RunFlags).Width
            Dim cx = 0, line = 0
            For Each w In words
                Dim widths = w.Select(Function(r) TextRenderer.MeasureText(g, r.Text, If(r.Bold, bold, font), Size.Empty, RunFlags).Width).ToList()
                Dim ww = widths.Sum()
                If cx > 0 AndAlso cx + spaceW + ww > width Then
                    line += 1 : cx = 0
                ElseIf cx > 0 Then
                    cx += spaceW
                End If
                For i = 0 To w.Count - 1
                    If draw Then TextRenderer.DrawText(g, w(i).Text, If(w(i).Bold, bold, font), New Point(x + cx, y + line * lineH), color, RunFlags)
                    cx += widths(i)
                Next
            Next
            Return (line + 1) * lineH
        End Function

        Private Shared Function Measure(g As Graphics, text As String, font As Font, width As Integer) As Integer
            If String.IsNullOrEmpty(text) Then Return 0
            Return TextRenderer.MeasureText(g, text, font, New Size(Math.Max(10, width), Integer.MaxValue), Flags).Height
        End Function

        ''' <summary>Berechnet (und zeichnet optional) alle Blöcke. Gibt die Gesamthöhe zurück.</summary>
        Private Function LayoutBlocks(g As Graphics, draw As Boolean) As Integer
            Dim sc = DeviceDpi / 96.0F
            Dim pad = CInt(18 * sc)
            Dim w = Math.Max(60, Width - SystemInformation.VerticalScrollBarWidth - 2 * pad)
            Dim oy = If(draw, AutoScrollPosition.Y, 0)
            Dim y = pad

            For Each b In _blocks
                Select Case b.Kind
                    Case BlockKind.Title
                        Dim h = Measure(g, b.Text, Theme.FontTitle, w)
                        If draw Then TextRenderer.DrawText(g, b.Text, Theme.FontTitle, New Rectangle(pad, y + oy, w, h), Theme.TextDark, Flags)
                        y += h + CInt(4 * sc)

                    Case BlockKind.Subtitle
                        If y > pad Then y += CInt(8 * sc)
                        Dim h = Measure(g, b.Text, Theme.FontSmallBold, w)
                        If draw Then TextRenderer.DrawText(g, b.Text, Theme.FontSmallBold, New Rectangle(pad, y + oy, w, h), Theme.AccentDark, Flags)
                        y += h + CInt(12 * sc)

                    Case BlockKind.Heading
                        y += CInt(8 * sc)
                        Dim h = Measure(g, b.Text, Theme.FontHeading, w)
                        If draw Then TextRenderer.DrawText(g, b.Text, Theme.FontHeading, New Rectangle(pad, y + oy, w, h), Theme.AccentDark, Flags)
                        y += h + CInt(6 * sc)

                    Case BlockKind.Paragraph
                        Dim h = RichText(g, b.Text, Theme.FontBase, pad, y + oy, w, Theme.TextDark, draw)
                        y += h + CInt(10 * sc)

                    Case BlockKind.Bullet
                        Dim ind = CInt(18 * sc)
                        Dim h = RichText(g, b.Text, Theme.FontBase, pad + ind, y + oy, w - ind, Theme.TextDark, False)
                        If draw Then
                            Dim lineH = TextRenderer.MeasureText(g, "X", Theme.FontBase).Height
                            Dim dd = 6 * sc
                            Using br As New SolidBrush(Theme.Accent)
                                g.FillEllipse(br, pad + 3 * sc, y + oy + (lineH - dd) / 2, dd, dd)
                            End Using
                            RichText(g, b.Text, Theme.FontBase, pad + ind, y + oy, w - ind, Theme.TextDark, True)
                        End If
                        y += h + CInt(6 * sc)

                    Case BlockKind.Tip, BlockKind.Warning, BlockKind.Good, BlockKind.TryIt
                        Dim label As String, back As Color, acc As Color, labelCol As Color
                        Select Case b.Kind
                            Case BlockKind.Tip : label = "Merke" : back = Theme.TipBack : acc = Theme.TipAccent : labelCol = Theme.TipText
                            Case BlockKind.Warning : label = "Achtung" : back = Theme.WarnBack : acc = Theme.WarnAccent : labelCol = Theme.WarnText
                            Case BlockKind.Good : label = "Gut so" : back = Theme.GoodBack : acc = Theme.GoodAccent : labelCol = Theme.GoodText
                            Case Else : label = "Probieren Sie es aus" : back = Theme.TaskBack : acc = Theme.TaskAccent : labelCol = Theme.TaskText
                        End Select
                        Dim bp = CInt(12 * sc), stripe = CInt(5 * sc)
                        Dim inner = w - 2 * bp - stripe
                        Dim lh = Measure(g, label, Theme.FontBold, inner)
                        Dim th = RichText(g, b.Text, Theme.FontBase, 0, 0, inner, Theme.TextDark, False)
                        Dim bh = bp * 2 + lh + CInt(3 * sc) + th
                        If draw Then
                            Dim r As New RectangleF(pad, y + oy, w, bh)
                            Using p = Theme.RoundRect(r, 8 * sc), br As New SolidBrush(back)
                                g.FillPath(br, p)
                                Dim oldClip = g.Clip
                                g.SetClip(p, CombineMode.Intersect)
                                Using sb As New SolidBrush(acc)
                                    g.FillRectangle(sb, r.X, r.Y, stripe, r.Height)
                                End Using
                                g.Clip = oldClip
                            End Using
                            Dim tx = pad + stripe + bp
                            TextRenderer.DrawText(g, label, Theme.FontBold, New Rectangle(tx, y + oy + bp, inner, lh), labelCol, Flags)
                            RichText(g, b.Text, Theme.FontBase, tx, y + oy + bp + lh + CInt(3 * sc), inner, Theme.TextDark, True)
                        End If
                        y += bh + CInt(12 * sc)

                    Case BlockKind.Meter
                        Dim rowH = CInt(24 * sc)
                        Dim labelW = CInt(w * 0.5)
                        Dim pctW = CInt(48 * sc)
                        If draw Then
                            TextRenderer.DrawText(g, b.Text, Theme.FontSmall, New Rectangle(pad, y + oy, labelW, rowH), Theme.TextDark,
                                                  TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPadding)
                            Dim bx = pad + labelW + CInt(4 * sc)
                            Dim bw = w - labelW - pctW - CInt(8 * sc)
                            Dim barH = 10 * sc
                            Dim br0 As New RectangleF(bx, y + oy + (rowH - barH) / 2, bw, barH)
                            Using p = Theme.RoundRect(br0, barH / 2), tb As New SolidBrush(Theme.GridMinor)
                                g.FillPath(tb, p)
                            End Using
                            Dim v = CSng(Math.Max(0, Math.Min(1, b.Value)))
                            If v > 0.005 Then
                                Dim br1 As New RectangleF(br0.X, br0.Y, Math.Max(barH, br0.Width * v), barH)
                                Using p = Theme.RoundRect(br1, barH / 2), fb As New SolidBrush(If(b.Accent.IsEmpty, Theme.Accent, b.Accent))
                                    g.FillPath(fb, p)
                                End Using
                            End If
                            TextRenderer.DrawText(g, b.ValueText, Theme.FontSmallBold, New Rectangle(pad + w - pctW, y + oy, pctW, rowH), Theme.TextDark,
                                                  TextFormatFlags.VerticalCenter Or TextFormatFlags.Right Or TextFormatFlags.NoPadding)
                        End If
                        y += rowH

                    Case BlockKind.Divider
                        y += CInt(10 * sc)
                        If draw Then
                            Using pn As New Pen(Theme.Border, 1)
                                g.DrawLine(pn, pad, y + oy, pad + w, y + oy)
                            End Using
                        End If
                        y += CInt(12 * sc)
                End Select
            Next
            Return y + pad
        End Function

    End Class

End Namespace
