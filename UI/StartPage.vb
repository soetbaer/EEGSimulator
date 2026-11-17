Option Strict On

Imports System.Drawing
Imports System.Windows.Forms
Imports NeuroSim.Services

Namespace UI

    ''' <summary>
    ''' Startseite beim Programmstart: sagt in wenigen Zeilen, worum es geht, und bietet die beiden Modi an.
    ''' Hier wird auch die automatische Update-Suche ein- oder ausgeschaltet.
    ''' Liegt als Fläche über dem ganzen Fenster, bis ein Modus gewählt ist.
    ''' </summary>
    ''' <remarks>
    ''' Gestaltung: eine klare Hauptaktion (gefüllt) und eine Nebenaktion (getönt), Abstände im 8er-Raster,
    ''' alle Texte linksbündig an einer Kante, Schaltflächen mindestens 44 px hoch, beide Einstellungen als
    ''' Schalter, die den eingeschalteten Zustand beschreiben. Alle Texte wachsen in der Höhe mit, damit bei
    ''' großer Bildschirmskalierung nichts abgeschnitten wird.
    ''' </remarks>
    Public Class StartPage
        Inherits Panel

        ''' <summary>Der Benutzer hat einen Modus gewählt (True = Geführte Lektionen, False = Freier Simulator).</summary>
        Public Event ModeChosen(lessonMode As Boolean)

        Private Const CardWidth As Integer = 752
        Private Const PadX As Integer = 40
        Private Const Inner As Integer = CardWidth - 2 * PadX
        Private Const Gap As Integer = 16
        ' Text neben einem Schalter beginnt hier (Breite des Schalters + Abstand, siehe ToggleSwitch)
        Private Const ToggleTextIndent As Integer = 50

        Private ReadOnly _card As New Panel()
        Private ReadOnly _flow As New FlowLayoutPanel()
        Private ReadOnly _footer As New Label()
        Private ReadOnly _tglUpdate As New ToggleSwitch()
        Private ReadOnly _tglShow As New ToggleSwitch()
        Private ReadOnly _tips As New ToolTip() With {.AutoPopDelay = 15000, .InitialDelay = 400}
        Private _btnLessons As Button

        Public Sub New(lessonCount As Integer, chapterCount As Integer)
            ' Nicht angedockt, sondern über die ganze Fläche verankert: So liegt die Seite auch
            ' über Kopfzeile und Seitenleisten (angedockte Flächen teilen sich sonst den Platz).
            Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            BackColor = Theme.AppBack
            AutoScroll = True
            Font = Theme.FontBase

            _card.BackColor = Theme.Card
            _card.Padding = New Padding(PadX, 32, PadX, 32)
            _card.AutoSize = True
            _card.AutoSizeMode = AutoSizeMode.GrowAndShrink
            AddHandler _card.Paint, Sub(s, e)
                                        Using p As New Pen(Theme.Border)
                                            e.Graphics.DrawRectangle(p, 0, 0, _card.Width - 1, _card.Height - 1)
                                        End Using
                                    End Sub

            _flow.FlowDirection = FlowDirection.TopDown
            _flow.WrapContents = False
            _flow.AutoSize = True
            _flow.AutoSizeMode = AutoSizeMode.GrowAndShrink
            _flow.BackColor = Theme.Card
            _flow.Margin = New Padding(0)
            _flow.Location = New Point(PadX, 32)
            ' hält die Karte auf voller Breite, auch wenn kein Inhalt so breit ist
            _flow.Controls.Add(New Panel With {.Size = New Size(Inner, 0), .Margin = New Padding(0)})

            ' ---- Kopf: Logo, Name, eine Zeile Erklärung ----
            Dim head As New TableLayoutPanel With {.ColumnCount = 2, .RowCount = 2, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                                   .Margin = New Padding(0, 0, 0, 24), .BackColor = Theme.Card}
            head.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 80))
            head.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            Dim logo As New LogoMark With {.BackColor = Theme.Card, .Size = New Size(64, 64), .Margin = New Padding(0, 0, 16, 0)}
            head.Controls.Add(logo, 0, 0)
            head.SetRowSpan(logo, 2)
            head.Controls.Add(New Label With {.Text = $"Willkommen bei {AppInfo.Name}", .Font = New Font("Segoe UI Semibold", 22.0F), .ForeColor = Theme.TextDark,
                                              .AutoSize = True, .Margin = New Padding(0)}, 1, 0)
            head.Controls.Add(New Label With {
                .Text = "Lernen Sie an simulierten EEG-Kurven, wie Verstärker, Verschaltungen, Filter und Impedanzen das Signal verändern – ohne Risiko und ohne Patientendaten.",
                .Font = Theme.FontBase, .ForeColor = Theme.TextMuted, .AutoSize = True, .MaximumSize = New Size(Inner - 80, 0),
                .Margin = New Padding(2, 4, 0, 0)}, 1, 1)
            _flow.Controls.Add(head)

            ' ---- Was man ausprobieren kann: vier kurze Punkte in zwei Spalten ----
            Dim features = TwoColumns(New Padding(0, 0, 0, 32))
            features.Controls.Add(Feature("Verstärker und Impedanzen", "Wie die Headbox Störungen unterdrückt", left:=True), 0, 0)
            features.Controls.Add(Feature("Verschaltungen", "Dasselbe EEG in jeder Montage", left:=False), 1, 0)
            features.Controls.Add(Feature("Filter", "LowCut, HiCut, 50-Hz- und EKG-Filter", left:=True), 0, 1)
            features.Controls.Add(Feature("Artefakte", "Störungen einzeln ein- und ausschalten", left:=False), 1, 1)
            _flow.Controls.Add(features)

            ' ---- Die beiden Wege ----
            _flow.Controls.Add(New Label With {.Text = "Wie möchten Sie beginnen?", .Font = Theme.FontHeading, .ForeColor = Theme.TextDark,
                                               .AutoSize = True, .Margin = New Padding(0, 0, 0, 8)})
            Dim choices = TwoColumns(New Padding(0, 0, 0, 32))
            _btnLessons = MakeChoice("Geführte Lektionen", primary:=True)
            AddHandler _btnLessons.Click, Sub() RaiseEvent ModeChosen(True)
            Dim btnFree = MakeChoice("Freier Simulator", primary:=False)
            AddHandler btnFree.Click, Sub() RaiseEvent ModeChosen(False)
            choices.Controls.Add(Choice(_btnLessons, $"{lessonCount} Lektionen in {chapterCount} Kapiteln, Schritt für Schritt", left:=True), 0, 0)
            choices.Controls.Add(Choice(btnFree, "Alle Regler frei, mit laufender Erklärung", left:=False), 1, 0)
            _flow.Controls.Add(choices)

            ' ---- Einstellungen: zwei Schalter ----
            _flow.Controls.Add(New Panel With {.Size = New Size(Inner, 1), .BackColor = Theme.Border, .Margin = New Padding(0, 0, 0, Gap)})

            Dim locked = Updater.DisabledByAdministrator
            SetupToggle(_tglUpdate, "Beim Start nach Updates suchen")
            _tglUpdate.Checked = AppSettings.AutoUpdateCheck AndAlso Not locked
            _tglUpdate.Enabled = Not locked
            AddHandler _tglUpdate.CheckedChanged, Sub() AppSettings.AutoUpdateCheck = _tglUpdate.Checked
            _flow.Controls.Add(_tglUpdate)
            _flow.Controls.Add(ToggleHint(If(locked, "Vom Administrator abgeschaltet.",
                                                 "Höchstens einmal am Tag bei GitHub. Es werden keine Nutzungsdaten übertragen.")))
            _tips.SetToolTip(_tglUpdate, "Dazu wird höchstens einmal am Tag die neueste Versionsnummer bei GitHub abgefragt." & vbCrLf &
                                         "GitHub sieht dabei technisch bedingt die IP-Adresse dieses Rechners. Nutzungsdaten werden nicht übertragen.")

            SetupToggle(_tglShow, "Startseite beim Start anzeigen")
            _tglShow.Checked = AppSettings.ShowStartPage
            AddHandler _tglShow.CheckedChanged, Sub() AppSettings.ShowStartPage = _tglShow.Checked
            _flow.Controls.Add(_tglShow)
            Dim lastHint = ToggleHint("Aus: NeuroSim öffnet direkt Lektion 1. Wieder einschalten im Info-Dialog (F1).")
            lastHint.Margin = New Padding(ToggleTextIndent, 0, 0, 0)
            _flow.Controls.Add(lastHint)

            _card.Controls.Add(_flow)
            Controls.Add(_card)

            ' ---- Fußzeile unten links: Version und Hinweis ----
            _footer.Text = $"{AppInfo.Name} {AppInfo.VersionText}  ·  © {AppInfo.CopyrightYear} {AppInfo.Developer}  ·  Nur für die Aus- und Weiterbildung, kein Medizinprodukt"
            _footer.Font = Theme.FontSmall
            _footer.ForeColor = Theme.TextMuted
            _footer.BackColor = Theme.AppBack
            _footer.AutoSize = True
            Controls.Add(_footer)

            AddHandler Me.Resize, Sub() PlaceContent()
            AddHandler _card.SizeChanged, Sub() PlaceContent()
            AddHandler _footer.SizeChanged, Sub() PlaceContent()
        End Sub

        ''' <summary>Setzt den Fokus auf »Geführte Lektionen«, damit Enter direkt startet.</summary>
        Public Sub FocusDefault()
            _btnLessons.Focus()
        End Sub

        ''' <summary>Karte mittig, Fußzeile unten links; bei kleinem Fenster wird gescrollt.</summary>
        Private Sub PlaceContent()
            Dim margin = 16
            Dim footerSpace = _footer.Height + 2 * margin
            Dim x = Math.Max(margin, (ClientSize.Width - _card.Width) \ 2)
            Dim y = Math.Max(margin, (ClientSize.Height - footerSpace - _card.Height) \ 2)
            Dim loc As New Point(x + AutoScrollPosition.X, y + AutoScrollPosition.Y)
            If _card.Location <> loc Then _card.Location = loc
            Dim fy = Math.Max(y + _card.Height + margin, ClientSize.Height - _footer.Height - margin)
            Dim floc As New Point(margin + AutoScrollPosition.X, fy + AutoScrollPosition.Y)
            If _footer.Location <> floc Then _footer.Location = floc
            Dim min As New Size(Math.Max(_card.Width, _footer.Width) + 2 * margin, y + _card.Height + footerSpace)
            If AutoScrollMinSize <> min Then AutoScrollMinSize = min
        End Sub

        Protected Overrides Sub OnLayout(e As LayoutEventArgs)
            MyBase.OnLayout(e)
            PlaceContent()
        End Sub

        ' ---------- kleine Bau-Helfer ----------

        ''' <summary>Zwei gleich breite Spalten, Zeilen so hoch wie ihr Inhalt.</summary>
        Private Shared Function TwoColumns(margin As Padding) As TableLayoutPanel
            Dim t As New TableLayoutPanel With {.ColumnCount = 2, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                                .Margin = margin, .BackColor = Theme.Card}
            t.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, Inner \ 2))
            t.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, Inner - Inner \ 2))
            Return t
        End Function

        ''' <summary>Breite einer Spalte abzüglich des halben Spaltenabstands.</summary>
        Private Shared ReadOnly Property ColWidth As Integer
            Get
                Return Inner \ 2 - Gap \ 2
            End Get
        End Property

        Private Shared Function ColMargin(left As Boolean, bottom As Integer) As Padding
            Return If(left, New Padding(0, 0, Gap \ 2, bottom), New Padding(Gap \ 2, 0, 0, bottom))
        End Function

        Private Shared Function Feature(title As String, text As String, left As Boolean) As Control
            Dim f As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoSize = True,
                                               .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Margin = ColMargin(left, 12), .BackColor = Theme.Card}
            f.Controls.Add(New Label With {.Text = title, .Font = Theme.FontBold, .ForeColor = Theme.TextDark, .AutoSize = True,
                                           .MaximumSize = New Size(ColWidth, 0), .Margin = New Padding(0)})
            f.Controls.Add(New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = True,
                                           .MaximumSize = New Size(ColWidth, 0), .Margin = New Padding(0, 2, 0, 0)})
            Return f
        End Function

        Private Shared Function Choice(button As Button, text As String, left As Boolean) As Control
            Dim f As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoSize = True,
                                               .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Margin = ColMargin(left, 0), .BackColor = Theme.Card}
            button.Size = New Size(ColWidth, 48)
            button.Margin = New Padding(0)
            button.AccessibleDescription = text
            f.Controls.Add(button)
            f.Controls.Add(New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = True,
                                           .MaximumSize = New Size(ColWidth, 0), .Margin = New Padding(0, 8, 0, 0)})
            Return f
        End Function

        Private Shared Sub SetupToggle(t As ToggleSwitch, text As String)
            t.Text = text
            t.Font = Theme.FontBold
            t.Size = New Size(Inner, 32)
            t.Margin = New Padding(0)
        End Sub

        Private Shared Function ToggleHint(text As String) As Label
            Return New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = True,
                                   .MaximumSize = New Size(Inner - ToggleTextIndent, 0), .Margin = New Padding(ToggleTextIndent, 0, 0, Gap)}
        End Function

        Private Shared Function MakeChoice(text As String, primary As Boolean) As Button
            Dim back = If(primary, Theme.Accent, Theme.AccentLight)
            Dim b As New Button With {
                .Text = text, .FlatStyle = FlatStyle.Flat, .BackColor = back, .ForeColor = If(primary, Color.White, Theme.AccentDark),
                .Font = Theme.FontHeading, .Cursor = Cursors.Hand, .UseVisualStyleBackColor = False, .TabStop = True}
            ' Nebenaktion getönt ohne Rahmen, damit beide gleich groß und gleich ruhig wirken
            b.FlatAppearance.BorderSize = 0
            b.FlatAppearance.MouseOverBackColor = Theme.Blend(back, If(primary, Color.White, Theme.Accent), 0.15)
            b.FlatAppearance.MouseDownBackColor = Theme.Blend(back, Color.Black, 0.12)
            Return b
        End Function

    End Class

End Namespace
