Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace UI

    ''' <summary>
    ''' Flache Schaltfläche im Stil der Anwendung. Zeigt bei Tastaturbedienung einen deutlichen
    ''' Fokusrahmen (der gepunktete Windows-Rahmen ist auf flachen, farbigen Flächen kaum zu sehen).
    ''' </summary>
    Public Class FlatButton
        Inherits Button

        Public Sub New()
            FlatStyle = FlatStyle.Flat
            UseVisualStyleBackColor = False
            Cursor = Cursors.Hand
            TabStop = True
            FlatAppearance.BorderSize = 0
        End Sub

        ''' <summary>Färbt die Schaltfläche ein und setzt passende Farben für Maus darüber und gedrückt.</summary>
        Public Sub SetColors(back As Color, fore As Color)
            BackColor = back
            ForeColor = fore
            FlatAppearance.MouseOverBackColor = Theme.Blend(back, If(back.GetBrightness() > 0.6, Theme.Accent, Color.White), 0.15)
            FlatAppearance.MouseDownBackColor = Theme.Blend(back, Color.Black, 0.12)
        End Sub

        ''' <summary>Hauptaktion: gefüllt in der Akzentfarbe.</summary>
        Public Shared Function Primary(text As String) As FlatButton
            Dim b As New FlatButton With {.Text = text, .Font = Theme.FontBold, .Size = New Size(140, 36)}
            b.SetColors(Theme.Accent, Color.White)
            Return b
        End Function

        ''' <summary>Nebenaktion: hell getönt.</summary>
        Public Shared Function Secondary(text As String) As FlatButton
            Dim b As New FlatButton With {.Text = text, .Font = Theme.FontBold, .Size = New Size(140, 36)}
            b.SetColors(Theme.AccentLight, Theme.AccentDark)
            Return b
        End Function

        Protected Overrides Sub OnGotFocus(e As EventArgs)
            MyBase.OnGotFocus(e) : Invalidate()
        End Sub

        Protected Overrides Sub OnLostFocus(e As EventArgs)
            MyBase.OnLostFocus(e) : Invalidate()
        End Sub

        ' Der eingebaute gepunktete Rahmen wird durch den eigenen ersetzt
        Protected Overrides ReadOnly Property ShowFocusCues As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            If Not Focused OrElse Not MyBase.ShowFocusCues Then Return
            Dim sc = DeviceDpi / 96.0F
            Dim w = 2 * sc
            ' auf dunklen Flächen weiß, auf hellen in der Akzentfarbe
            Dim col = If(BackColor.GetBrightness() < 0.55, Color.White, Theme.Accent)
            Dim r As New RectangleF(w / 2 + 2 * sc, w / 2 + 2 * sc, Width - w - 4 * sc, Height - w - 4 * sc)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            Using p As New Pen(col, w), path = Theme.RoundRect(r, 3 * sc)
                e.Graphics.DrawPath(p, path)
            End Using
        End Sub

    End Class

End Namespace
