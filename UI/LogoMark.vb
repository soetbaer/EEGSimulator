Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    ''' <summary>
    ''' NeuroSim-Logo: ein "N", das aus einer EEG-Kurve gezeichnet ist (wie Assets/logo.svg).
    ''' </summary>
    Public Class LogoMark
        Inherits Control

        ' Punkte der Kurve im 256er-Raster der SVG-Vorlage
        Private Shared ReadOnly Shape As PointF() = {
            New PointF(30, 128), New PointF(66, 128), New PointF(82, 186), New PointF(96, 70),
            New PointF(160, 186), New PointF(174, 70), New PointF(190, 128), New PointF(226, 128)}

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.HeaderBack
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            DrawLogo(g, New RectangleF(0, 0, Width, Height))
        End Sub

        ''' <summary>Zeichnet das Logo quadratisch und zentriert in das angegebene Rechteck.</summary>
        Public Shared Sub DrawLogo(g As Graphics, bounds As RectangleF)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim d = Math.Min(bounds.Width, bounds.Height)
            If d < 4 Then Return
            Dim ox = bounds.Left + (bounds.Width - d) / 2
            Dim oy = bounds.Top + (bounds.Height - d) / 2
            Dim k = d / 256.0F

            Dim tile As New RectangleF(ox + 8 * k, oy + 8 * k, 240 * k, 240 * k)
            Using path = Theme.RoundRect(tile, 56 * k),
                  bg As New LinearGradientBrush(tile, ColorTranslator.FromHtml("#24405C"), ColorTranslator.FromHtml("#14202E"), 45.0F)
                g.FillPath(bg, path)
            End Using

            Dim pts = Shape.Select(Function(p) New PointF(ox + p.X * k, oy + p.Y * k)).ToArray()
            Using wave As New LinearGradientBrush(New RectangleF(ox + 20 * k, oy, 216 * k, d),
                                                  ColorTranslator.FromHtml("#2EE6D6"), ColorTranslator.FromHtml("#4AA8FF"), 0.0F),
                  pen As New Pen(wave, Math.Max(1.5F, 19 * k))
                pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round : pen.LineJoin = LineJoin.Round
                g.DrawLines(pen, pts)
            End Using
        End Sub

    End Class

End Namespace
