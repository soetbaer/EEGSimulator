Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms
Imports EegFilterTrainer.Simulation
Imports EegFilterTrainer.Training

Namespace UI

    ''' <summary>
    ''' Kopfschema mit den Elektroden und ihren Impedanzen – wie die Impedanzanzeige am EEG-Gerät.
    ''' Linksklick auf eine Elektrode: nächsthöherer Wert, Rechtsklick: nächstniedrigerer Wert.
    ''' </summary>
    Public Class ImpedanceView
        Inherits Control

        Public Event ValuesChanged As EventHandler

        Private _values As Double() = CType(EegSimulator.DefaultImpedances.Clone(), Double())
        Private _saltBridge As Boolean
        Private _hover As Integer = -1

        ' Lage der Elektroden im Kopfschema (0 … 1), Reihenfolge wie EegSimulator.ElectrodeNames
        Private Shared ReadOnly Pos As PointF() = {
            New PointF(0.4F, 0.14F), New PointF(0.37F, 0.32F), New PointF(0.36F, 0.5F), New PointF(0.37F, 0.68F), New PointF(0.4F, 0.86F),
            New PointF(0.6F, 0.14F), New PointF(0.63F, 0.32F), New PointF(0.64F, 0.5F), New PointF(0.63F, 0.68F), New PointF(0.6F, 0.86F)}

        Public Shared ReadOnly ColGood As Color = ColorTranslator.FromHtml("#22995A")
        Public Shared ReadOnly ColOkay As Color = ColorTranslator.FromHtml("#E8A317")
        Public Shared ReadOnly ColBad As Color = ColorTranslator.FromHtml("#D64535")
        Public Shared ReadOnly ColTooLow As Color = ColorTranslator.FromHtml("#1E7FD8")

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw, True)
            BackColor = Theme.Card
            Size = New Size(272, 300)
        End Sub

        Public Property Values As Double()
            Get
                Return CType(_values.Clone(), Double())
            End Get
            Set(value As Double())
                _values = CType(value.Clone(), Double())
                Invalidate()
            End Set
        End Property

        Public Property SaltBridge As Boolean
            Get
                Return _saltBridge
            End Get
            Set(value As Boolean)
                _saltBridge = value
                Invalidate()
            End Set
        End Property

        ''' <summary>Ampelfarbe nach DGKN-Empfehlung (1–10 kΩ) und ACNS-Zielwert (5 kΩ).</summary>
        Public Shared Function ColorFor(kOhm As Double) As Color
            If kOhm < 1 Then Return ColTooLow
            If kOhm <= 5 Then Return ColGood
            If kOhm <= 10 Then Return ColOkay
            Return ColBad
        End Function

        Private ReadOnly Property Sc As Single
            Get
                Return DeviceDpi / 96.0F
            End Get
        End Property

        Private Function HeadRect() As RectangleF
            Dim legendH = 62 * Sc
            Dim h = Height - legendH - 14 * Sc
            Dim w = Math.Min(Width - 8 * Sc, h * 1.0F)
            Return New RectangleF((Width - w) / 2, 12 * Sc, w, h)
        End Function

        Private Function Center(i As Integer) As PointF
            Dim r = HeadRect()
            Return New PointF(r.Left + Pos(i).X * r.Width, r.Top + Pos(i).Y * r.Height)
        End Function

        Private Function HitTest(p As Point) As Integer
            Dim rad = 17 * Sc
            For i = 0 To Pos.Length - 1
                Dim c = Center(i)
                If (p.X - c.X) ^ 2 + (p.Y - c.Y) ^ 2 <= rad * rad Then Return i
            Next
            Return -1
        End Function

        Private Function IsBridged(i As Integer) As Boolean
            Return _saltBridge AndAlso EegSimulator.ChannelElectrodes(EegSimulator.SaltBridgeChannel).Contains(i)
        End Function

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            Dim h = HitTest(e.Location)
            If h <> _hover Then
                _hover = h
                Cursor = If(h >= 0, Cursors.Hand, Cursors.Default)
                Invalidate()
            End If
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            MyBase.OnMouseLeave(e)
            _hover = -1
            Invalidate()
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            Dim i = HitTest(e.Location)
            If i < 0 Then Return
            Dim idx = Array.IndexOf(ImpedanceOptions, _values(i))
            If idx < 0 Then idx = 0
            Dim n = ImpedanceOptions.Length
            idx = If(e.Button = MouseButtons.Right, (idx - 1 + n) Mod n, (idx + 1) Mod n)
            _values(i) = ImpedanceOptions(idx)
            Invalidate()
            RaiseEvent ValuesChanged(Me, EventArgs.Empty)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim r = HeadRect()

            ' Kopf von oben: Nase, Ohren, Umriss
            Using fill As New SolidBrush(Theme.LabelColumn), pen As New Pen(Theme.GridMajor, 2 * Sc)
                Dim nose = {New PointF(r.Left + r.Width / 2 - 10 * Sc, r.Top + 3 * Sc),
                            New PointF(r.Left + r.Width / 2, r.Top - 9 * Sc),
                            New PointF(r.Left + r.Width / 2 + 10 * Sc, r.Top + 3 * Sc)}
                g.FillPolygon(fill, nose)
                g.DrawLines(pen, nose)
                g.FillEllipse(fill, r.Left - 6 * Sc, r.Top + r.Height / 2 - 16 * Sc, 12 * Sc, 32 * Sc)
                g.DrawEllipse(pen, r.Left - 6 * Sc, r.Top + r.Height / 2 - 16 * Sc, 12 * Sc, 32 * Sc)
                g.FillEllipse(fill, r.Right - 6 * Sc, r.Top + r.Height / 2 - 16 * Sc, 12 * Sc, 32 * Sc)
                g.DrawEllipse(pen, r.Right - 6 * Sc, r.Top + r.Height / 2 - 16 * Sc, 12 * Sc, 32 * Sc)
                g.FillEllipse(fill, r)
                g.DrawEllipse(pen, r)
            End Using

            ' Ableitungsketten (Längsreihe) als dünne Linien
            Using pen As New Pen(Theme.GridMajor, 1.5F * Sc)
                For Each ch In EegSimulator.ChannelElectrodes
                    g.DrawLine(pen, Center(ch(0)), Center(ch(1)))
                Next
            End Using

            ' Gelbrücke
            If _saltBridge Then
                Dim ch = EegSimulator.ChannelElectrodes(EegSimulator.SaltBridgeChannel)
                Using pen As New Pen(Color.FromArgb(170, ColTooLow), 13 * Sc)
                    pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                    g.DrawLine(pen, Center(ch(0)), Center(ch(1)))
                End Using
            End If

            ' Elektroden
            Dim rad = 16 * Sc
            Dim de = CultureInfo.GetCultureInfo("de-DE")
            For i = 0 To Pos.Length - 1
                Dim c = Center(i)
                Dim v = If(IsBridged(i), 0.5, _values(i))
                Dim col = ColorFor(v)
                Dim rr = If(i = _hover, rad + 2 * Sc, rad)
                Using b As New SolidBrush(col), pen As New Pen(Color.White, 2 * Sc)
                    g.FillEllipse(b, c.X - rr, c.Y - rr, rr * 2, rr * 2)
                    g.DrawEllipse(pen, c.X - rr, c.Y - rr, rr * 2, rr * 2)
                End Using
                Dim txt = If(v < 1, "<1", v.ToString("0", de))
                TextRenderer.DrawText(g, txt, Theme.FontBold, New Rectangle(CInt(c.X - rad), CInt(c.Y - rad), CInt(rad * 2), CInt(rad * 2)),
                                      Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
                ' Name außen neben der Elektrode
                Dim name = EegSimulator.ElectrodeNames(i)
                Dim sz = TextRenderer.MeasureText(name, Theme.FontSmallBold)
                Dim leftSide = Pos(i).X < 0.5F
                Dim tx = If(leftSide, c.X - rad - 2 * Sc - sz.Width, c.X + rad + 2 * Sc)
                TextRenderer.DrawText(g, name, Theme.FontSmallBold, New Point(CInt(tx), CInt(c.Y - sz.Height / 2)), Theme.TextMuted)
            Next

            ' Legende
            Dim items = {("1–5 kΩ: gut", ColGood), ("bis 10 kΩ: zulässig", ColOkay),
                         ("über 10 kΩ: zu hoch", ColBad), ("unter 1 kΩ: zu niedrig", ColTooLow)}
            Dim y = Height - 58 * Sc
            Dim colW = Width / 2.0F
            For i = 0 To items.Length - 1
                Dim x = If(i Mod 2 = 0, 2 * Sc, colW)
                Dim yy = y + (i \ 2) * 22 * Sc
                Using b As New SolidBrush(items(i).Item2)
                    g.FillEllipse(b, x, yy + 4 * Sc, 10 * Sc, 10 * Sc)
                End Using
                TextRenderer.DrawText(g, items(i).Item1, Theme.FontSmall, New Point(CInt(x + 14 * Sc), CInt(yy)), Theme.TextDark)
            Next
        End Sub

    End Class

End Namespace
