Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    ''' <summary>Moderner Ein/Aus-Schalter mit optionalem Farbpunkt.</summary>
    Public Class ToggleSwitch
        Inherits Control

        Private _checked As Boolean
        Private _hover As Boolean

        Public Event CheckedChanged As EventHandler

        Public Property DotColor As Color = Color.Empty
        Public Property OnColor As Color = Theme.Accent

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw Or ControlStyles.Selectable, True)
            Cursor = Cursors.Hand
            Font = Theme.FontBase
            ForeColor = Theme.TextDark
            BackColor = Theme.Card
            Size = New Size(270, 28)
            TabStop = True
            AccessibleRole = AccessibleRole.CheckButton
        End Sub

        Public Property Checked As Boolean
            Get
                Return _checked
            End Get
            Set(value As Boolean)
                If value = _checked Then Return
                _checked = value
                Invalidate()
                RaiseEvent CheckedChanged(Me, EventArgs.Empty)
            End Set
        End Property

        Protected Overrides Sub OnClick(e As EventArgs)
            MyBase.OnClick(e)
            Focus()
            Checked = Not Checked
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            If e.KeyCode = Keys.Space Then Checked = Not Checked
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            MyBase.OnMouseEnter(e) : _hover = True : Invalidate()
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e) : _hover = False : Invalidate()
        End Sub

        Protected Overrides Sub OnTextChanged(e As EventArgs)
            MyBase.OnTextChanged(e) : AccessibleName = Text : Invalidate()
        End Sub

        Protected Overrides Sub OnGotFocus(e As EventArgs)
            MyBase.OnGotFocus(e) : Invalidate()
        End Sub

        Protected Overrides Sub OnLostFocus(e As EventArgs)
            MyBase.OnLostFocus(e) : Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(If(_hover, Theme.Blend(BackColor, Theme.AccentLight, 0.6), BackColor))
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim sc = DeviceDpi / 96.0F

            Dim track As New RectangleF(2 * sc, (Height - 20 * sc) / 2, 38 * sc, 20 * sc)
            Dim trackCol = If(_checked, If(Enabled, OnColor, Theme.TrackOff), Theme.TrackOff)
            Using p = Theme.RoundRect(track, 10 * sc), b As New SolidBrush(trackCol)
                g.FillPath(b, p)
            End Using
            Dim d = 16 * sc
            Dim kx = If(_checked, track.Right - 2 * sc - d, track.Left + 2 * sc)
            Using b As New SolidBrush(Color.White)
                g.FillEllipse(b, kx, track.Top + 2 * sc, d, d)
            End Using

            Dim x = track.Right + 10 * sc
            If Not DotColor.IsEmpty Then
                Dim dd = 10 * sc
                Using b As New SolidBrush(DotColor)
                    g.FillEllipse(b, x, (Height - dd) / 2, dd, dd)
                End Using
                x += dd + 7 * sc
            End If

            Dim r As New Rectangle(CInt(x), 0, Math.Max(1, Width - CInt(x)), Height)
            TextRenderer.DrawText(g, Text, Font, r, If(Enabled, ForeColor, Theme.TextMuted),
                                  TextFormatFlags.VerticalCenter Or TextFormatFlags.Left Or TextFormatFlags.EndEllipsis)

            If Focused AndAlso ShowFocusCues Then
                ControlPaint.DrawFocusRectangle(g, New Rectangle(0, 0, Width, Height))
            End If
        End Sub

    End Class

End Namespace
