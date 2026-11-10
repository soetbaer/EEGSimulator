Option Strict On

Imports System.Drawing
Imports System.Drawing.Drawing2D

Namespace UI

    ''' <summary>Farben und Schriften der Anwendung an einer Stelle.</summary>
    Public Module Theme

        ' Grundfarben
        Public ReadOnly HeaderBack As Color = ColorTranslator.FromHtml("#1B2838")
        Public ReadOnly HeaderText As Color = Color.White
        Public ReadOnly HeaderMuted As Color = ColorTranslator.FromHtml("#9FB3C8")
        Public ReadOnly AppBack As Color = ColorTranslator.FromHtml("#EEF2F6")
        Public ReadOnly Card As Color = Color.White
        Public ReadOnly Border As Color = ColorTranslator.FromHtml("#D9E1EA")
        Public ReadOnly TextDark As Color = ColorTranslator.FromHtml("#1F2D3D")
        Public ReadOnly TextMuted As Color = ColorTranslator.FromHtml("#6B7A8C")
        Public ReadOnly Accent As Color = ColorTranslator.FromHtml("#1E7FD8")
        Public ReadOnly AccentDark As Color = ColorTranslator.FromHtml("#155E9F")
        Public ReadOnly AccentLight As Color = ColorTranslator.FromHtml("#E3F0FC")
        Public ReadOnly Teal As Color = ColorTranslator.FromHtml("#0FA3A3")
        Public ReadOnly TrackOff As Color = ColorTranslator.FromHtml("#C5CFDA")

        ' EEG-Papier
        Public ReadOnly Paper As Color = ColorTranslator.FromHtml("#FFFFFF")
        Public ReadOnly LabelColumn As Color = ColorTranslator.FromHtml("#F6F8FB")
        Public ReadOnly GridMinor As Color = ColorTranslator.FromHtml("#EEF2F7")
        Public ReadOnly GridMajor As Color = ColorTranslator.FromHtml("#CBD6E2")
        Public ReadOnly Trace As Color = ColorTranslator.FromHtml("#14233A")
        Public ReadOnly TraceRaw As Color = ColorTranslator.FromHtml("#B9C4D0")
        Public ReadOnly Removed As Color = ColorTranslator.FromHtml("#E5533D")
        Public ReadOnly Selected As Color = ColorTranslator.FromHtml("#FFF6DD")
        Public ReadOnly SpectrumBefore As Color = ColorTranslator.FromHtml("#C3CCD8")

        ' Hinweis-Boxen
        Public ReadOnly TipBack As Color = ColorTranslator.FromHtml("#FFF7E0")
        Public ReadOnly TipAccent As Color = ColorTranslator.FromHtml("#E8A317")
        Public ReadOnly WarnBack As Color = ColorTranslator.FromHtml("#FDECEA")
        Public ReadOnly WarnAccent As Color = ColorTranslator.FromHtml("#D64535")
        Public ReadOnly GoodBack As Color = ColorTranslator.FromHtml("#E7F6EE")
        Public ReadOnly GoodAccent As Color = ColorTranslator.FromHtml("#22995A")
        Public ReadOnly TaskBack As Color = ColorTranslator.FromHtml("#E8F2FC")
        Public ReadOnly TaskAccent As Color = ColorTranslator.FromHtml("#1E7FD8")

        ' Frequenzbänder / Bausteine
        Public ReadOnly ColDrift As Color = ColorTranslator.FromHtml("#9C7A5B")
        Public ReadOnly ColDelta As Color = ColorTranslator.FromHtml("#5B6CFF")
        Public ReadOnly ColTheta As Color = ColorTranslator.FromHtml("#17A589")
        Public ReadOnly ColAlpha As Color = ColorTranslator.FromHtml("#F0A202")
        Public ReadOnly ColBeta As Color = ColorTranslator.FromHtml("#A85CD6")
        Public ReadOnly ColFast As Color = ColorTranslator.FromHtml("#E5533D")
        Public ReadOnly ColSpike As Color = ColorTranslator.FromHtml("#C2185B")
        Public ReadOnly ColBlink As Color = ColorTranslator.FromHtml("#6D8299")
        Public ReadOnly ColHum As Color = ColorTranslator.FromHtml("#EC407A")
        Public ReadOnly ColEcg As Color = ColorTranslator.FromHtml("#D0021B")

        ' Schriften
        Public ReadOnly FontBase As New Font("Segoe UI", 10.0F)
        Public ReadOnly FontBold As New Font("Segoe UI Semibold", 10.0F)
        Public ReadOnly FontSmall As New Font("Segoe UI", 8.75F)
        Public ReadOnly FontSmallBold As New Font("Segoe UI Semibold", 8.75F)
        Public ReadOnly FontSection As New Font("Segoe UI Semibold", 9.0F)
        Public ReadOnly FontHeading As New Font("Segoe UI Semibold", 12.0F)
        Public ReadOnly FontTitle As New Font("Segoe UI Semibold", 15.0F)
        Public ReadOnly FontAppTitle As New Font("Segoe UI Semibold", 16.0F)
        Public ReadOnly FontChannel As New Font("Segoe UI Semibold", 9.0F)

        Public Function RoundRect(r As RectangleF, radius As Single) As GraphicsPath
            Dim p As New GraphicsPath()
            Dim d = Math.Min(radius * 2, Math.Min(r.Width, r.Height))
            If d <= 0.5F Then
                p.AddRectangle(r)
                Return p
            End If
            p.AddArc(r.X, r.Y, d, d, 180, 90)
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
            p.CloseFigure()
            Return p
        End Function

        Public Function Blend(a As Color, b As Color, t As Double) As Color
            Return Color.FromArgb(
                CInt(a.R + (CInt(b.R) - a.R) * t),
                CInt(a.G + (CInt(b.G) - a.G) * t),
                CInt(a.B + (CInt(b.B) - a.B) * t))
        End Function

        ''' <summary>Farbe des EEG-Frequenzbandes zu einer Frequenz.</summary>
        Public Function BandColor(f As Double) As Color
            If f < 0.5 Then Return ColDrift
            If f < 4 Then Return ColDelta
            If f < 8 Then Return ColTheta
            If f < 13 Then Return ColAlpha
            If f < 30 Then Return ColBeta
            Return ColFast
        End Function

    End Module

End Namespace
