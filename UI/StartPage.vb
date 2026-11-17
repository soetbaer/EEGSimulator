Option Strict On

Imports System.Drawing
Imports System.Windows.Forms
Imports NeuroSim.Services

Namespace UI

    ''' <summary>
    ''' Startseite beim Programmstart: erklärt kurz, worum es geht, und bietet die beiden Modi an.
    ''' Hier wird auch die automatische Update-Suche ein- oder ausgeschaltet.
    ''' Liegt als Fläche über dem ganzen Fenster, bis ein Modus gewählt ist.
    ''' </summary>
    Public Class StartPage
        Inherits Panel

        ''' <summary>Der Benutzer hat einen Modus gewählt (True = Geführte Lektionen, False = Freier Simulator).</summary>
        Public Event ModeChosen(lessonMode As Boolean)

        Private Const CardWidth As Integer = 860
        Private Const Inner As Integer = CardWidth - 2 * 40

        Private ReadOnly _card As New Panel()
        Private ReadOnly _flow As New FlowLayoutPanel()
        Private ReadOnly _tglUpdate As New ToggleSwitch()
        Private ReadOnly _chkSkip As New CheckBox()
        Private _btnLessons As Button

        Public Sub New(lessonCount As Integer, chapterCount As Integer)
            ' Nicht angedockt, sondern über die ganze Fläche verankert: So liegt die Seite auch
            ' über Kopfzeile und Seitenleisten (angedockte Flächen teilen sich sonst den Platz).
            Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
            BackColor = Theme.AppBack
            AutoScroll = True
            Font = Theme.FontBase

            _card.BackColor = Theme.Card
            _card.Padding = New Padding(40, 32, 40, 28)
            _card.Width = CardWidth
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
            _flow.Location = New Point(40, 32)

            ' ---- Kopf: Logo, Name, Untertitel ----
            Dim head As New Panel With {.Size = New Size(Inner, 84), .Margin = New Padding(0, 0, 0, 10), .BackColor = Theme.Card}
            Dim logo As New LogoMark With {.BackColor = Theme.Card, .Size = New Size(76, 76), .Location = New Point(0, 4)}
            Dim title As New Label With {.Text = $"Willkommen bei {AppInfo.Name}", .Font = New Font("Segoe UI Semibold", 22.0F), .ForeColor = Theme.TextDark,
                                         .AutoSize = True, .Location = New Point(90, 4)}
            Dim tag As New Label With {.Text = AppInfo.Tagline, .Font = Theme.FontBase, .ForeColor = Theme.TextMuted,
                                       .AutoSize = True, .Location = New Point(93, 50)}
            head.Controls.AddRange({logo, title, tag})
            _flow.Controls.Add(head)

            ' ---- Worum geht es? ----
            AddPara($"{AppInfo.Name} ist eine Schulungsanwendung für MTF und weiteres Fachpersonal in der Neurophysiologie. " &
                    "Sie zeigt an simulierten EEG-Kurven, was auf dem Weg von der Elektrode bis auf den Bildschirm mit dem Signal passiert – " &
                    "und warum eine Einstellung die Kurven so verändert, wie sie es tut.")
            AddPara("Filter, Verschaltungen und Impedanzen versteht man am besten, wenn man sie selbst ausprobiert. " &
                    "Hier geht das ohne Risiko: Alle Kurven sind künstlich erzeugt, es gibt keine Patientendaten, " &
                    "und jede Einstellung lässt sich sofort wieder zurücknehmen.")

            AddHeading("Was Sie hier ausprobieren können")
            AddBullet("Verstärker und Impedanzen", "wie die Headbox Störungen unterdrückt – und warum das bei schlechten Impedanzen nicht mehr klappt.")
            AddBullet("Verschaltungen", "dasselbe EEG in Längsreihe, Querreihe, Ohr-, Vertex- und Durchschnittsreferenz oder als Quellenableitung.")
            AddBullet("Filter", "LowCut, HiCut, 50-Hz-Filter und EKG-Filter einstellen und in der Frequenz-Ansicht sehen, was sie entfernen.")
            AddBullet("Artefakte", "Blinzeln, Schwitzen, Muskelaktivität, Netzbrumm und EKG einzeln ein- und ausschalten.")

            ' ---- Die beiden Wege ----
            AddHeading("Wie möchten Sie beginnen?")
            Dim choices As New TableLayoutPanel With {.ColumnCount = 2, .RowCount = 2, .Size = New Size(Inner, 116), .Margin = New Padding(0, 4, 0, 6),
                                                      .BackColor = Theme.Card}
            choices.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            choices.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            choices.RowStyles.Add(New RowStyle(SizeType.Absolute, 56))
            choices.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            _btnLessons = MakeChoice("Geführte Lektionen", Theme.Accent, Color.White)
            _btnLessons.Margin = New Padding(0, 0, 10, 0)
            AddHandler _btnLessons.Click, Sub() RaiseEvent ModeChosen(True)
            Dim btnFree = MakeChoice("Freier Simulator", Theme.AccentLight, Theme.AccentDark)
            btnFree.Margin = New Padding(10, 0, 0, 0)
            AddHandler btnFree.Click, Sub() RaiseEvent ModeChosen(False)

            choices.Controls.Add(_btnLessons, 0, 0)
            choices.Controls.Add(btnFree, 1, 0)
            choices.Controls.Add(ChoiceHint($"{lessonCount} kurze Lektionen in {chapterCount} Kapiteln, Schritt für Schritt erklärt. Links lesen, rechts ausprobieren. Empfohlen für den Einstieg.",
                                            New Padding(2, 8, 12, 0)), 0, 1)
            choices.Controls.Add(ChoiceHint("Alle Regler, Verschaltungen und Signal-Bausteine sind frei. Die Anwendung erklärt laufend, was Ihre Einstellung bewirkt.",
                                            New Padding(12, 8, 2, 0)), 1, 1)
            _flow.Controls.Add(choices)

            ' ---- Einstellungen ----
            _flow.Controls.Add(New Panel With {.Size = New Size(Inner, 1), .BackColor = Theme.Border, .Margin = New Padding(0, 12, 0, 10)})

            Dim locked = Updater.DisabledByAdministrator
            _tglUpdate.Text = "Beim Start automatisch nach Updates suchen"
            _tglUpdate.Font = Theme.FontBold
            _tglUpdate.Size = New Size(Inner, 30)
            _tglUpdate.Margin = New Padding(0)
            _tglUpdate.Checked = AppSettings.AutoUpdateCheck AndAlso Not locked
            _tglUpdate.Enabled = Not locked
            AddHandler _tglUpdate.CheckedChanged, Sub() AppSettings.AutoUpdateCheck = _tglUpdate.Checked
            _flow.Controls.Add(_tglUpdate)
            _flow.Controls.Add(Hint(If(locked,
                "Die Update-Funktion wurde vom Administrator abgeschaltet.",
                "Dazu wird höchstens einmal am Tag die neueste Versionsnummer bei GitHub abgefragt. GitHub sieht dabei technisch bedingt die IP-Adresse dieses Rechners. Nutzungsdaten werden nicht übertragen."),
                New Padding(50, 0, 0, 10)))

            _chkSkip.Text = "Startseite beim nächsten Start nicht mehr anzeigen"
            _chkSkip.AutoSize = True
            _chkSkip.ForeColor = Theme.TextDark
            _chkSkip.Margin = New Padding(4, 2, 0, 0)
            _chkSkip.Checked = Not AppSettings.ShowStartPage
            AddHandler _chkSkip.CheckedChanged, Sub() AppSettings.ShowStartPage = Not _chkSkip.Checked
            _flow.Controls.Add(_chkSkip)
            _flow.Controls.Add(Hint("NeuroSim öffnet dann direkt Lektion 1. Wieder einschalten lässt sich die Startseite im Info-Dialog (F1).",
                                    New Padding(22, 0, 0, 0)))

            ' ---- Hinweis ----
            _flow.Controls.Add(New Label With {
                .Text = "Nur für die Aus-, Fort- und Weiterbildung. Die Kurven sind simuliert. NeuroSim ist kein Medizinprodukt.",
                .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = True, .MaximumSize = New Size(Inner, 0),
                .Margin = New Padding(0, 18, 0, 0)})

            _card.Controls.Add(_flow)
            Controls.Add(_card)
            AddHandler Me.Resize, Sub() CenterCard()
            AddHandler _card.SizeChanged, Sub() CenterCard()
        End Sub

        ''' <summary>Setzt den Fokus auf »Geführte Lektionen«, damit Enter direkt startet.</summary>
        Public Sub FocusDefault()
            _btnLessons.Focus()
        End Sub

        Private Sub CenterCard()
            Dim x = Math.Max(16, (ClientSize.Width - _card.Width) \ 2)
            Dim y = Math.Max(16, (ClientSize.Height - _card.Height) \ 2)
            Dim loc As New Point(x + AutoScrollPosition.X, y + AutoScrollPosition.Y)
            If _card.Location <> loc Then _card.Location = loc
            Dim min As New Size(_card.Width + 32, _card.Height + 32)
            If AutoScrollMinSize <> min Then AutoScrollMinSize = min
        End Sub

        Protected Overrides Sub OnLayout(e As LayoutEventArgs)
            MyBase.OnLayout(e)
            CenterCard()
        End Sub

        ' ---------- kleine Bau-Helfer ----------

        Private Sub AddPara(text As String)
            _flow.Controls.Add(New Label With {.Text = text, .Font = Theme.FontBase, .ForeColor = Theme.TextDark, .AutoSize = True,
                                               .MaximumSize = New Size(Inner, 0), .Margin = New Padding(0, 0, 0, 8)})
        End Sub

        Private Sub AddHeading(text As String)
            _flow.Controls.Add(New Label With {.Text = text, .Font = Theme.FontHeading, .ForeColor = Theme.TextDark, .AutoSize = True,
                                               .Margin = New Padding(0, 12, 0, 6)})
        End Sub

        Private Sub AddBullet(term As String, text As String)
            Dim row As New TableLayoutPanel With {.ColumnCount = 3, .RowCount = 1, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                                  .Margin = New Padding(4, 0, 0, 4), .BackColor = Theme.Card}
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 18))
            row.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 200))
            row.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            row.Controls.Add(New Label With {.Text = "•", .ForeColor = Theme.Accent, .Font = Theme.FontBold, .AutoSize = True, .Margin = New Padding(0)}, 0, 0)
            row.Controls.Add(New Label With {.Text = term, .ForeColor = Theme.TextDark, .Font = Theme.FontBold, .AutoSize = True, .Margin = New Padding(0)}, 1, 0)
            row.Controls.Add(New Label With {.Text = text, .ForeColor = Theme.TextDark, .Font = Theme.FontBase, .AutoSize = True,
                                             .MaximumSize = New Size(Inner - 4 - 18 - 200, 0), .Margin = New Padding(0)}, 2, 0)
            _flow.Controls.Add(row)
        End Sub

        Private Shared Function Hint(text As String, margin As Padding) As Label
            Return New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = True,
                                   .MaximumSize = New Size(Inner - margin.Left, 0), .Margin = margin}
        End Function

        Private Shared Function ChoiceHint(text As String, margin As Padding) As Label
            Return New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .Dock = DockStyle.Fill, .Margin = margin}
        End Function

        Private Shared Function MakeChoice(text As String, back As Color, fore As Color) As Button
            Dim b As New Button With {
                .Text = text, .FlatStyle = FlatStyle.Flat, .BackColor = back, .ForeColor = fore,
                .Font = Theme.FontHeading, .Cursor = Cursors.Hand, .Dock = DockStyle.Fill,
                .UseVisualStyleBackColor = False, .TabStop = True}
            b.FlatAppearance.BorderSize = If(back = Theme.Accent, 0, 1)
            b.FlatAppearance.BorderColor = Theme.Accent
            b.FlatAppearance.MouseOverBackColor = Theme.Blend(back, If(back.GetBrightness() > 0.6, Theme.Accent, Color.White), 0.15)
            b.FlatAppearance.MouseDownBackColor = Theme.Blend(back, Color.Black, 0.12)
            Return b
        End Function

    End Class

End Namespace
