Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports NeuroSim.Simulation

Namespace UI

    ''' <summary>
    ''' Gemeinsame Geometrie für die Kopfschemata (Impedanzen, Verschaltung): Kopf von oben,
    ''' Nase oben, links ist links. Rechnet die Lage der Elektroden in Bildpunkte um.
    ''' </summary>
    Public NotInheritable Class HeadGeometry

        Public ReadOnly Property Center As PointF
        ''' <summary>Bildpunkte je Einheit der Elektrodenkoordinaten (Abstand Cz–T3).</summary>
        Public ReadOnly Property Unit As Single

        ''' <param name="area">Fläche, in die Kopf und Ohrelektroden passen sollen.</param>
        ''' <param name="margin">Platz rund um die äußersten Elektroden (für Kreise und Beschriftung).</param>
        Public Sub New(area As RectangleF, margin As Single)
            ' waagerecht reichen die Ohrelektroden bis ±1,36, senkrecht die Elektroden bis ±0,95 (plus Nase)
            Dim ux = (area.Width - 2 * margin) / 2.72F
            Dim uy = (area.Height - 2 * margin) / 2.1F
            Unit = Math.Max(10.0F, Math.Min(ux, uy))
            Center = New PointF(area.Left + area.Width / 2, area.Top + area.Height / 2 + 0.05F * Unit)
        End Sub

        Public Function Pos(electrode As Integer) As PointF
            Return New PointF(Center.X + CSng(Head.X(electrode)) * Unit, Center.Y - CSng(Head.Y(electrode)) * Unit)
        End Function

        ''' <summary>Zeichnet Kopfumriss, Nase und Ohren.</summary>
        Public Sub DrawHead(g As Graphics, sc As Single)
            Dim rx = 1.13F * Unit, ry = 1.1F * Unit
            Dim r As New RectangleF(Center.X - rx, Center.Y - ry, rx * 2, ry * 2)
            Using fill As New SolidBrush(Theme.LabelColumn), pen As New Pen(Theme.GridMajor, 2 * sc)
                Dim nose = {New PointF(Center.X - 9 * sc, r.Top + 3 * sc), New PointF(Center.X, r.Top - 9 * sc), New PointF(Center.X + 9 * sc, r.Top + 3 * sc)}
                g.FillPolygon(fill, nose)
                g.DrawLines(pen, nose)
                For Each sgn In {-1, 1}
                    Dim ex = Center.X + sgn * rx
                    g.FillEllipse(fill, ex - 6 * sc, Center.Y - 14 * sc, 12 * sc, 28 * sc)
                    g.DrawEllipse(pen, ex - 6 * sc, Center.Y - 14 * sc, 12 * sc, 28 * sc)
                Next
                g.FillEllipse(fill, r)
                g.DrawEllipse(pen, r)
            End Using
        End Sub

        ''' <summary>Elektrode unter dem Mauszeiger oder −1.</summary>
        Public Function HitTest(p As Point, radius As Single) As Integer
            For i = 0 To Head.Count - 1
                Dim c = Pos(i)
                If (p.X - c.X) ^ 2 + (p.Y - c.Y) ^ 2 <= radius * radius Then Return i
            Next
            Return -1
        End Function

        ''' <summary>Nächste Elektrode in einer Richtung (dx, dy in Kopfkoordinaten: +x rechts, +y vorne); sonst dieselbe.</summary>
        Public Shared Function NeighbourInDirection(electrode As Integer, dx As Double, dy As Double) As Integer
            Dim best = electrode, bestCost = Double.MaxValue
            For i = 0 To Head.Count - 1
                If i = electrode Then Continue For
                Dim vx = Head.X(i) - Head.X(electrode), vy = Head.Y(i) - Head.Y(electrode)
                Dim along = vx * dx + vy * dy
                If along <= 0.05 Then Continue For
                Dim across = Math.Abs(vx * dy - vy * dx)
                Dim cost = along + 2.5 * across       ' bevorzugt Elektroden, die möglichst genau in der Richtung liegen
                If cost < bestCost Then bestCost = cost : best = i
            Next
            Return best
        End Function

    End Class

End Namespace
