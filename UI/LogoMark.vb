Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    ''' <summary>Kleines Logo: eine stilisierte EEG-Welle im Kreis.</summary>
    Public Class LogoMark
        Inherits Control

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.HeaderBack
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim d = Math.Min(Width, Height) - 8.0F
            Dim r As New RectangleF((Width - d) / 2, (Height - d) / 2, d, d)
            Using b As New SolidBrush(Theme.Blend(Theme.HeaderBack, Theme.Teal, 0.25))
                g.FillEllipse(b, r)
            End Using
            Dim pts As New List(Of PointF)
            For i = 0 To 40
                Dim t = i / 40.0
                Dim x = r.Left + d * 0.15F + CSng(t) * d * 0.7F
                Dim y = r.Top + d / 2 - CSng(Math.Sin(t * Math.PI * 4) * Math.Exp(-Math.Pow((t - 0.5) * 2.2, 2)) * d * 0.28)
                pts.Add(New PointF(x, y))
            Next
            Using p As New Pen(Theme.Teal, Math.Max(2.0F, d / 14))
                p.StartCap = LineCap.Round : p.EndCap = LineCap.Round : p.LineJoin = LineJoin.Round
                g.DrawLines(p, pts.ToArray())
            End Using
        End Sub

    End Class

End Namespace
