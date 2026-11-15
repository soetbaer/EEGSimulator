Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.Windows.Forms
Imports NeuroSim.Simulation
Imports NeuroSim.Training

Namespace UI

    ''' <summary>
    ''' Kopfschema mit den 21 Elektroden des 10/20-Systems und ihren Impedanzen – wie die Impedanzanzeige am EEG-Gerät.
    ''' Linksklick auf eine Elektrode: nächsthöherer Wert, Rechtsklick: nächstniedrigerer Wert.
    ''' </summary>
    Public Class ImpedanceView
        Inherits Control

        Public Event ValuesChanged As EventHandler

        Private _values As Double() = CType(EegSimulator.DefaultImpedances.Clone(), Double())
        Private _saltBridge As Boolean
        Private _hover As Integer = -1
        Private _focusIdx As Integer        ' per Tastatur gewählte Elektrode

        Private _montage As Montage

        ''' <summary>Aktuelle Verschaltung: ihre bipolaren Ketten werden als dünne Linien eingezeichnet.</summary>
        Public Property Montage As Montage
            Get
                Return _montage
            End Get
            Set(value As Montage)
                _montage = value
                Invalidate()
            End Set
        End Property

        Public Shared ReadOnly ColGood As Color = ColorTranslator.FromHtml("#1B7F4B")
        Public Shared ReadOnly ColOkay As Color = ColorTranslator.FromHtml("#E8A317")
        Public Shared ReadOnly ColBad As Color = ColorTranslator.FromHtml("#C0392B")
        Public Shared ReadOnly ColTooLow As Color = ColorTranslator.FromHtml("#1A6FC0")

        Public Sub New()
            SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                     ControlStyles.UserPaint Or ControlStyles.ResizeRedraw Or ControlStyles.Selectable, True)
            BackColor = Theme.Card
            Size = New Size(272, 290)
            TabStop = True
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

        Private ReadOnly Property Radius As Single
            Get
                Return 13 * Sc
            End Get
        End Property

        Private Function Geometry() As HeadGeometry
            Dim legendH = 50 * Sc
            Return New HeadGeometry(New RectangleF(0, 0, Width, Height - legendH), Radius + 6 * Sc)
        End Function

        Private Function HitTest(p As Point) As Integer
            Return Geometry().HitTest(p, Radius + 2 * Sc)
        End Function

        Private Function IsBridged(i As Integer) As Boolean
            Return _saltBridge AndAlso EegSimulator.SaltBridgeElectrodes.Contains(i)
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
            Focus()
            Dim i = HitTest(e.Location)
            If i < 0 Then Return
            _focusIdx = i
            StepValue(i, If(e.Button = MouseButtons.Right, -1, 1), wrap:=True)
        End Sub

        Private Sub StepValue(i As Integer, direction As Integer, wrap As Boolean)
            Dim idx = Array.IndexOf(ImpedanceOptions, _values(i))
            If idx < 0 Then idx = 0
            Dim n = ImpedanceOptions.Length
            idx += direction
            If wrap Then idx = (idx + n) Mod n Else idx = Math.Max(0, Math.Min(n - 1, idx))
            If _values(i) = ImpedanceOptions(idx) Then Return
            _values(i) = ImpedanceOptions(idx)
            Invalidate()
            RaiseEvent ValuesChanged(Me, EventArgs.Empty)
        End Sub

        ''' <summary>Pfeiltasten sollen die Elektrode wechseln und nicht den Fokus weiterschieben.</summary>
        Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
            Select Case keyData And Keys.KeyCode
                Case Keys.Left, Keys.Right, Keys.Up, Keys.Down : Return True
            End Select
            Return MyBase.IsInputKey(keyData)
        End Function

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            Select Case e.KeyCode
                Case Keys.Up : _focusIdx = HeadGeometry.NeighbourInDirection(_focusIdx, 0, 1)
                Case Keys.Down : _focusIdx = HeadGeometry.NeighbourInDirection(_focusIdx, 0, -1)
                Case Keys.Left : _focusIdx = HeadGeometry.NeighbourInDirection(_focusIdx, -1, 0)
                Case Keys.Right : _focusIdx = HeadGeometry.NeighbourInDirection(_focusIdx, 1, 0)
                Case Keys.Add, Keys.Oemplus, Keys.Space, Keys.Enter : StepValue(_focusIdx, 1, wrap:=False)
                Case Keys.Subtract, Keys.OemMinus : StepValue(_focusIdx, -1, wrap:=False)
                Case Else : Return
            End Select
            e.Handled = True
            Invalidate()
        End Sub

        Protected Overrides Sub OnGotFocus(e As EventArgs)
            MyBase.OnGotFocus(e) : Invalidate()
        End Sub

        Protected Overrides Sub OnLostFocus(e As EventArgs)
            MyBase.OnLostFocus(e) : Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            Dim g = e.Graphics
            g.Clear(BackColor)
            g.SmoothingMode = SmoothingMode.AntiAlias
            Dim geo = Geometry()
            geo.DrawHead(g, Sc)

            ' Ketten der aktuellen Verschaltung als dünne Linien (nur Kanäle mit genau zwei Elektroden)
            If _montage IsNot Nothing AndAlso _montage.Kind = MontageKind.Bipolar Then
                Using pen As New Pen(Theme.GridMajor, 1.5F * Sc)
                    For Each ch In _montage.Channels
                        If ch.Kind = ChannelKind.Eeg AndAlso ch.Input2.Length = 1 Then g.DrawLine(pen, geo.Pos(ch.Input1), geo.Pos(ch.Input2(0)))
                    Next
                End Using
            End If

            ' Gelbrücke
            If _saltBridge Then
                Using pen As New Pen(Color.FromArgb(170, ColTooLow), 11 * Sc)
                    pen.StartCap = LineCap.Round : pen.EndCap = LineCap.Round
                    g.DrawLine(pen, geo.Pos(EegSimulator.SaltBridgeElectrodes(0)), geo.Pos(EegSimulator.SaltBridgeElectrodes(1)))
                End Using
            End If

            ' Elektroden: Wert im Kreis, Name darüber
            Dim rad = Radius
            Dim de = CultureInfo.GetCultureInfo("de-DE")
            For i = 0 To Head.Count - 1
                Dim c = geo.Pos(i)
                Dim v = If(IsBridged(i), EegSimulator.SaltBridgeImpedance, _values(i))
                Dim col = ColorFor(v)
                Dim rr = If(i = _hover, rad + 2 * Sc, rad)
                Using b As New SolidBrush(col), pen As New Pen(Color.White, 1.5F * Sc)
                    g.FillEllipse(b, c.X - rr, c.Y - rr, rr * 2, rr * 2)
                    g.DrawEllipse(pen, c.X - rr, c.Y - rr, rr * 2, rr * 2)
                End Using
                ' Fokusring für die Tastaturbedienung
                If Focused AndAlso i = _focusIdx Then
                    Using fpen As New Pen(Theme.TextDark, 2 * Sc)
                        g.DrawEllipse(fpen, c.X - rr - 3 * Sc, c.Y - rr - 3 * Sc, (rr + 3 * Sc) * 2, (rr + 3 * Sc) * 2)
                    End Using
                End If
                Dim txt = If(v < 1, "<1", v.ToString("0", de))
                ' auf Gelb ist weiße Schrift nicht lesbar -> dunkle Schrift
                Dim txtCol = If(col = ColOkay, ColorTranslator.FromHtml("#3A2A00"), Color.White)
                TextRenderer.DrawText(g, txt, Theme.FontSmallBold, New Rectangle(CInt(c.X - rad), CInt(c.Y - rad), CInt(rad * 2), CInt(rad * 2)),
                                      txtCol, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding)
                Dim name = Head.Names(i)
                Dim sz = TextRenderer.MeasureText(g, name, Theme.FontTiny, Size.Empty, TextFormatFlags.NoPadding)
                TextRenderer.DrawText(g, name, Theme.FontTiny, New Point(CInt(c.X - sz.Width / 2), CInt(c.Y - rad - sz.Height + 1 * Sc)), Theme.TextMuted,
                                      TextFormatFlags.NoPadding)
            Next

            ' Legende
            Dim items = {("1–5 kΩ: gut", ColGood), ("bis 10 kΩ: zulässig", ColOkay),
                         ("über 10 kΩ: zu hoch", ColBad), ("unter 1 kΩ: zu niedrig", ColTooLow)}
            Dim y = Height - 46 * Sc
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
