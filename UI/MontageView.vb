Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports NeuroSim.Simulation

Namespace UI

    ''' <summary>
    ''' Kopfschema der aktuellen Verschaltung: zeigt, welche Elektroden in den Kanälen miteinander
    ''' verglichen werden. Für den gewählten Kanal ist Eingang 1 blau und der Bezug (Eingang 2) orange markiert.
    ''' </summary>
    Public Class MontageView
        Inherits Control

        Public Shared ReadOnly ColInput1 As Color = ColorTranslator.FromHtml("#1A6FC0")
        Public Shared ReadOnly ColInput2 As Color = ColorTranslator.FromHtml("#E07B00")

        Private _montage As Montage
        Private _selected As Integer

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.Card
            Size = New Size(272, 300)
        End Sub

        Public Property Montage As Montage
            Get
                Return _montage
            End Get
            Set(value As Montage)
                _montage = value
                Invalidate()
            End Set
        End Property

        ''' <summary>Index des gewählten Kanals in der Verschaltung.</summary>
        Public Property SelectedChannel As Integer
            Get
                Return _selected
            End Get
            Set(value As Integer)
                _selected = value
                Invalidate()
            End Set
        End Property

        Private ReadOnly Property Sc As Single
            Get
                Return DeviceDpi / 96.0F
            End Get
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias
            If _montage Is Nothing Then Return

            Dim captionH = 44 * Sc
            Dim rad = 11 * Sc
            Dim geo As New HeadGeometry(New RectangleF(0, 0, Width, Height - captionH), rad + 4 * Sc)
            geo.DrawHead(g, Sc)

            Dim sel As Derivation = Nothing
            If _selected >= 0 AndAlso _selected < _montage.Channels.Count Then sel = _montage.Channels(_selected)
            If sel IsNot Nothing AndAlso sel.Kind = ChannelKind.Ecg Then sel = Nothing

            ' ---- alle Kanäle der Verschaltung als dünne Linien ----
            Using pen As New Pen(Theme.GridMajor, 1.5F * Sc)
                For Each ch In _montage.Channels
                    If ch.Kind <> ChannelKind.Eeg OrElse ch.Input2.Length <> 1 Then Continue For
                    g.DrawLine(pen, geo.Pos(ch.Input1), geo.Pos(ch.Input2(0)))
                Next
            End Using

            ' ---- gewählter Kanal: kräftige Linie(n) von Eingang 1 zum Bezug ----
            Dim isInput2 As New HashSet(Of Integer)
            If sel IsNot Nothing Then
                For Each r In sel.Input2
                    If r <> sel.Input1 Then isInput2.Add(r)
                Next
                Dim many = sel.Input2.Length > 1
                Using pen As New Pen(Color.FromArgb(If(many, 110, 230), ColInput2), If(many, 1.5F, 3.0F) * Sc)
                    For Each r In isInput2
                        g.DrawLine(pen, geo.Pos(sel.Input1), geo.Pos(r))
                    Next
                End Using
            End If

            ' ---- Elektroden ----
            For i = 0 To Head.Count - 1
                Dim c = geo.Pos(i)
                Dim fill = Theme.Card, border = Theme.GridMajor, text = Theme.TextMuted
                If sel IsNot Nothing AndAlso i = sel.Input1 Then
                    fill = ColInput1 : border = ColInput1 : text = Color.White
                ElseIf isInput2.Contains(i) Then
                    fill = If(sel.Input2.Length > 1, Theme.Blend(Theme.Card, ColInput2, 0.35), ColInput2)
                    border = ColInput2
                    text = If(sel.Input2.Length > 1, Theme.TextDark, Color.White)
                End If
                Using b As New SolidBrush(fill), pen As New Pen(border, 1.5F * Sc)
                    g.FillEllipse(b, c.X - rad, c.Y - rad, rad * 2, rad * 2)
                    g.DrawEllipse(pen, c.X - rad, c.Y - rad, rad * 2, rad * 2)
                End Using
                TextRenderer.DrawText(g, Head.Names(i), Theme.FontTiny, New Rectangle(CInt(c.X - rad - 2 * Sc), CInt(c.Y - rad), CInt(rad * 2 + 4 * Sc), CInt(rad * 2)),
                                      text, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
            Next

            ' ---- Beschriftung: gewählter Kanal in Worten ----
            Dim y = Height - captionH + 2 * Sc
            If sel Is Nothing Then
                TextRenderer.DrawText(g, "Klick auf einen Kanalnamen zeigt seine Eingänge.", Theme.FontSmall,
                                      New Rectangle(0, CInt(y), Width, CInt(captionH)), Theme.TextMuted, TextFormatFlags.WordBreak)
                Return
            End If
            Dim in1 = Head.Names(sel.Input1)
            Dim line2 As String
            If sel.Kind = ChannelKind.SingleInput Then
                line2 = "gegen die Erdelektrode"
            ElseIf sel.Input2.Length = 1 Then
                line2 = $"minus {Head.Names(sel.Input2(0))} (Eingang 2)"
            ElseIf _montage.Kind = MontageKind.Average Then
                line2 = $"minus Mittelwert aus {sel.Input2.Length} Elektroden"
            Else
                line2 = "minus Mittelwert aus " & String.Join(", ", sel.Input2.Select(Function(ix) Head.Names(ix)))
            End If
            Using b1 As New SolidBrush(ColInput1), b2 As New SolidBrush(ColInput2)
                g.FillEllipse(b1, 2 * Sc, y + 4 * Sc, 10 * Sc, 10 * Sc)
                g.FillEllipse(b2, 2 * Sc, y + 23 * Sc, 10 * Sc, 10 * Sc)
            End Using
            TextRenderer.DrawText(g, $"{in1} (Eingang 1)", Theme.FontSmallBold, New Point(CInt(16 * Sc), CInt(y)), Theme.TextDark)
            TextRenderer.DrawText(g, line2, Theme.FontSmall, New Point(CInt(16 * Sc), CInt(y + 19 * Sc)), Theme.TextDark)
        End Sub

    End Class

End Namespace
