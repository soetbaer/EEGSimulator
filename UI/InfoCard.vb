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
                        Dim h = Measure(g, b.Text, Theme.FontBase, w)
                        If draw Then TextRenderer.DrawText(g, b.Text, Theme.FontBase, New Rectangle(pad, y + oy, w, h), Theme.TextDark, Flags)
                        y += h + CInt(10 * sc)

                    Case BlockKind.Bullet
                        Dim ind = CInt(18 * sc)
                        Dim h = Measure(g, b.Text, Theme.FontBase, w - ind)
                        If draw Then
                            Dim lineH = TextRenderer.MeasureText(g, "X", Theme.FontBase).Height
                            Dim dd = 6 * sc
                            Using br As New SolidBrush(Theme.Accent)
                                g.FillEllipse(br, pad + 3 * sc, y + oy + (lineH - dd) / 2, dd, dd)
                            End Using
                            TextRenderer.DrawText(g, b.Text, Theme.FontBase, New Rectangle(pad + ind, y + oy, w - ind, h), Theme.TextDark, Flags)
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
                        Dim th = Measure(g, b.Text, Theme.FontBase, inner)
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
                            TextRenderer.DrawText(g, b.Text, Theme.FontBase, New Rectangle(tx, y + oy + bp + lh + CInt(3 * sc), inner, th), Theme.TextDark, Flags)
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
