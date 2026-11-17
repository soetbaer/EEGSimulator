Option Strict On

Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports NeuroSim.Services

Namespace UI

    ''' <summary>Info-Dialog: Über NeuroSim (mit Update-Funktion), Quellen, Haftungsausschluss, Lizenz.</summary>
    Public Class AboutForm
        Inherits Form

        Private ReadOnly _tglAuto As New ToggleSwitch()
        Private ReadOnly _tglStart As New ToggleSwitch()
        Private ReadOnly _btnCheck As FlatButton = FlatButton.Secondary("Jetzt nach Updates suchen")
        Private ReadOnly _lblStatus As New Label()
        Private ReadOnly _pages As New List(Of Panel)
        Private ReadOnly _tabs As New List(Of FlatButton)
        Private _pageIndex As Integer

        Public Sub New()
            SuspendLayout()
            AutoScaleDimensions = New SizeF(96.0F, 96.0F)
            AutoScaleMode = AutoScaleMode.Dpi
            Text = $"Info über {AppInfo.Name}"
            Font = Theme.FontBase
            BackColor = Theme.Card
            ClientSize = New Size(760, 650)
            MinimumSize = New Size(640, 550)
            StartPosition = FormStartPosition.CenterParent
            ShowInTaskbar = False
            MinimizeBox = False
            MaximizeBox = False
            Dim ico = Theme.AppIcon()
            If ico IsNot Nothing Then Icon = ico

            ' Reiter im Stil des Hauptfensters (statt der Windows-Reiter)
            Dim titles = {"Über", "Quellen", "Haftungsausschluss", "Lizenz"}
            _pages.Add(BuildAboutPage())
            _pages.Add(TextPage(LegalTexts.Sources))
            _pages.Add(TextPage(LegalTexts.Disclaimer))
            _pages.Add(TextPage(LegalTexts.LicenseIntro & LegalTexts.LicenseText, monospace:=True))

            Dim tabBar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 52, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False,
                                                    .Padding = New Padding(24, 16, 24, 0), .BackColor = Theme.Card}
            For i = 0 To titles.Length - 1
                Dim idx = i
                Dim tb = FlatButton.Secondary(titles(i))
                tb.Font = Theme.FontSmallBold
                tb.AutoSize = True
                tb.AutoSizeMode = AutoSizeMode.GrowOnly
                tb.MinimumSize = New Size(72, 32)
                tb.Height = 32
                tb.Padding = New Padding(10, 0, 10, 0)
                tb.Margin = New Padding(0)
                tb.AccessibleRole = AccessibleRole.PageTab
                AddHandler tb.Click, Sub() ShowPage(idx)
                _tabs.Add(tb)
                tabBar.Controls.Add(tb)
            Next
            Dim tabLine As New Panel With {.Dock = DockStyle.Top, .Height = 1, .BackColor = Theme.Border}

            Dim bottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 64, .BackColor = Theme.Card}
            Dim bottomLine As New Panel With {.Dock = DockStyle.Top, .Height = 1, .BackColor = Theme.Border}
            Dim btnClose = FlatButton.Primary("Schließen")
            btnClose.Size = New Size(120, 36)
            btnClose.Anchor = AnchorStyles.Right Or AnchorStyles.Top
            btnClose.DialogResult = DialogResult.OK
            btnClose.Location = New Point(bottom.Width - btnClose.Width - 24, 14)
            bottom.Controls.Add(btnClose)
            bottom.Controls.Add(bottomLine)
            AcceptButton = btnClose
            CancelButton = btnClose

            Dim host As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.Card}
            For Each pg In _pages
                pg.Dock = DockStyle.Fill
                host.Controls.Add(pg)
            Next
            Controls.Add(host)
            Controls.Add(tabLine)
            Controls.Add(tabBar)
            Controls.Add(bottom)
            host.BringToFront()
            ShowPage(0)
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Private Sub ShowPage(index As Integer)
            _pageIndex = index
            For i = 0 To _pages.Count - 1
                Dim sel = (i = index)
                _pages(i).Visible = sel
                _tabs(i).SetColors(If(sel, Theme.Accent, Theme.AccentLight), If(sel, Color.White, Theme.AccentDark))
                _tabs(i).AccessibleDescription = If(sel, "ausgewählt", "")
            Next
        End Sub

        ''' <summary>Strg+Tab / Strg+Umschalt+Tab wechseln die Reiter wie bei Windows-Reitern.</summary>
        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = (Keys.Control Or Keys.Tab) Then
                ShowPage((_pageIndex + 1) Mod _pages.Count)
                Return True
            End If
            If keyData = (Keys.Control Or Keys.Shift Or Keys.Tab) Then
                ShowPage((_pageIndex + _pages.Count - 1) Mod _pages.Count)
                Return True
            End If
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Private Function BuildAboutPage() As Panel
            Dim page As New Panel With {.BackColor = Theme.Card, .AutoScroll = True}

            ' gemeinsame linke Kante für alle Inhalte: 24 px
            Dim logo As New LogoMark With {.BackColor = Theme.Card, .Size = New Size(88, 88), .Location = New Point(24, 24)}
            Dim name As New Label With {.Text = AppInfo.Name, .Font = New Font("Segoe UI Semibold", 22.0F), .ForeColor = Theme.TextDark,
                                        .AutoSize = True, .Location = New Point(128, 22)}
            Dim tag As New Label With {.Text = AppInfo.Tagline, .Font = Theme.FontBase, .ForeColor = Theme.TextMuted,
                                       .AutoSize = True, .Location = New Point(131, 64)}
            Dim ver As New Label With {.Text = $"Version {AppInfo.VersionText}", .Font = Theme.FontBold, .ForeColor = Theme.TextDark,
                                       .AutoSize = True, .Location = New Point(131, 88)}

            Dim about As New TextBox With {.Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .WordWrap = True, .BorderStyle = BorderStyle.None, .BackColor = Theme.Card,
                                           .ForeColor = Theme.TextDark, .Font = Theme.FontBase, .Text = LegalTexts.About, .TabStop = False,
                                           .Location = New Point(24, 128), .Size = New Size(400, 184)}

            Dim link As New LinkLabel With {.Text = "Quellcode und neue Versionen auf GitHub öffnen", .AutoSize = True, .Location = New Point(22, 316),
                                            .LinkColor = Theme.AccentDark, .ActiveLinkColor = Theme.Accent, .Font = Theme.FontBase,
                                            .LinkBehavior = LinkBehavior.HoverUnderline}
            AddHandler link.LinkClicked, Sub() OpenUrl(AppInfo.RepoUrl)

            ' ---- Einstellungen: Startseite und Updates ----
            Dim line As New Panel With {.Location = New Point(24, 352), .Size = New Size(400, 1), .BackColor = Theme.Border}
            Dim upd As New Label With {.Text = "Einstellungen", .Font = Theme.FontHeading, .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(22, 364)}

            ' Schalter wie auf der Startseite und im Hauptfenster
            _tglStart.Text = "Startseite beim Start anzeigen"
            _tglStart.Location = New Point(20, 396)
            _tglStart.Size = New Size(400, 32)
            _tglStart.Checked = AppSettings.ShowStartPage
            AddHandler _tglStart.CheckedChanged, Sub() AppSettings.ShowStartPage = _tglStart.Checked

            _tglAuto.Text = "Beim Start nach Updates suchen"
            _tglAuto.Location = New Point(20, 430)
            _tglAuto.Size = New Size(400, 32)
            Dim locked = Updater.DisabledByAdministrator
            _tglAuto.Checked = AppSettings.AutoUpdateCheck AndAlso Not locked
            _tglAuto.Enabled = Not locked
            AddHandler _tglAuto.CheckedChanged, Sub() AppSettings.AutoUpdateCheck = _tglAuto.Checked

            _btnCheck.Size = New Size(220, 36)
            _btnCheck.Location = New Point(24, 474)
            AddHandler _btnCheck.Click, AddressOf OnCheckClicked

            _lblStatus.AutoSize = False
            _lblStatus.Location = New Point(260, 470)
            _lblStatus.Size = New Size(300, 44)
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft
            _lblStatus.ForeColor = Theme.TextMuted
            _btnCheck.Enabled = Not locked
            _lblStatus.Text = If(locked, "Die Update-Funktion wurde vom Administrator abgeschaltet.",
                                 "Bei der Suche wird nur die neueste Versionsnummer bei GitHub abgefragt.")

            ' Breiten an die Seite anpassen
            Dim fit = Sub()
                          Dim w = Math.Max(200, page.ClientSize.Width - about.Left * 2)
                          about.Width = w
                          line.Width = w
                          _tglStart.Width = w
                          _tglAuto.Width = w
                          _lblStatus.Width = Math.Max(100, page.ClientSize.Width - _lblStatus.Left - about.Left)
                      End Sub
            AddHandler page.Resize, Sub() fit()
            AddHandler page.Layout, Sub() fit()

            page.Controls.AddRange({logo, name, tag, ver, about, link, line, upd, _tglStart, _tglAuto, _btnCheck, _lblStatus})
            Return page
        End Function

        Private Shared Function TextPage(text As String, Optional monospace As Boolean = False) As Panel
            Dim page As New Panel With {.BackColor = Theme.Card, .Padding = New Padding(24, 16, 8, 8)}
            Dim box As New TextBox With {.Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .Dock = DockStyle.Fill,
                                         .BorderStyle = BorderStyle.None, .BackColor = Theme.Card, .ForeColor = Theme.TextDark,
                                         .Font = If(monospace, New Font("Consolas", 9.5F), Theme.FontBase), .Text = text, .WordWrap = True}
            AddHandler page.VisibleChanged, Sub()
                                                If page.Visible Then box.Select(0, 0)
                                            End Sub
            page.Controls.Add(box)
            Return page
        End Function

        Private Async Sub OnCheckClicked(sender As Object, e As EventArgs)
            _btnCheck.Enabled = False
            _lblStatus.ForeColor = Theme.TextMuted
            _lblStatus.Text = "Suche nach Updates …"
            Try
                Dim info = Await Updater.CheckAsync()
                If info Is Nothing Then
                    _lblStatus.Text = "Es ist noch keine veröffentlichte Version vorhanden."
                ElseIf info.IsNewer Then
                    _lblStatus.ForeColor = Theme.GoodText
                    _lblStatus.Text = $"Version {info.VersionText} ist verfügbar."
                    UpdatePrompt.ShowFor(Me, info)
                Else
                    _lblStatus.ForeColor = Theme.GoodText
                    _lblStatus.Text = $"{AppInfo.Name} ist aktuell (Version {AppInfo.VersionText})."
                End If
            Catch ex As Exception
                _lblStatus.ForeColor = Theme.WarnText
                _lblStatus.Text = "Die Suche ist fehlgeschlagen (keine Internetverbindung oder GitHub nicht erreichbar)."
            Finally
                If Not IsDisposed Then _btnCheck.Enabled = True
            End Try
        End Sub

        Public Shared Sub OpenUrl(url As String)
            Try
                Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
            Catch
                MessageBox.Show("Die Seite konnte nicht geöffnet werden:" & Environment.NewLine & url, AppInfo.Name,
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

    End Class

    ''' <summary>Dialog "Neue Version verfügbar" mit Herunterladen und Installieren.</summary>
    Public Class UpdatePrompt
        Inherits Form

        Private ReadOnly _info As UpdateInfo
        Private ReadOnly _btnInstall As FlatButton = FlatButton.Primary("Jetzt aktualisieren")
        Private ReadOnly _btnPage As FlatButton = FlatButton.Secondary("Release-Seite öffnen")
        Private ReadOnly _btnLater As FlatButton = FlatButton.Secondary("Später")
        Private ReadOnly _bar As New ProgressBar()
        Private ReadOnly _lblState As New Label()

        ''' <summary>Zeigt den Dialog an. Nach erfolgreicher Installation wird die Anwendung beendet.</summary>
        Public Shared Sub ShowFor(owner As IWin32Window, info As UpdateInfo)
            Using dlg As New UpdatePrompt(info)
                dlg.ShowDialog(owner)
            End Using
        End Sub

        Private Sub New(info As UpdateInfo)
            _info = info
            SuspendLayout()
            AutoScaleDimensions = New SizeF(96.0F, 96.0F)
            AutoScaleMode = AutoScaleMode.Dpi
            Text = $"{AppInfo.Name} – Update verfügbar"
            Font = Theme.FontBase
            BackColor = Theme.Card
            ClientSize = New Size(600, 440)
            FormBorderStyle = FormBorderStyle.FixedDialog
            StartPosition = FormStartPosition.CenterParent
            ShowInTaskbar = False
            MinimizeBox = False
            MaximizeBox = False
            Dim ico = Theme.AppIcon()
            If ico IsNot Nothing Then Icon = ico

            Dim head As New Label With {.Text = $"{AppInfo.Name} {info.VersionText} ist verfügbar", .Font = Theme.FontTitle, .ForeColor = Theme.TextDark,
                                        .AutoSize = True, .Location = New Point(20, 18)}
            Dim subh As New Label With {.Text = $"Installiert ist Version {AppInfo.VersionText}.", .ForeColor = Theme.TextMuted,
                                        .AutoSize = True, .Location = New Point(22, 54)}
            Dim notes As New RichTextBox With {.ReadOnly = True, .ScrollBars = RichTextBoxScrollBars.Vertical, .Location = New Point(22, 82),
                                               .Size = New Size(556, 220), .BackColor = Theme.LabelColumn, .ForeColor = Theme.TextDark,
                                               .BorderStyle = BorderStyle.FixedSingle, .TabStop = False, .DetectUrls = False}
            If info.NoteLines IsNot Nothing AndAlso info.NoteLines.Count > 0 Then
                FillNotes(notes, info.NoteLines)
            Else
                notes.Text = "Die Versionshinweise konnten nicht geladen werden. Was sich geändert hat, steht auf der Release-Seite (Schaltfläche »Release-Seite öffnen«)."
            End If

            _lblState.Location = New Point(22, 310)
            _lblState.Size = New Size(556, 40)
            _lblState.ForeColor = Theme.TextMuted
            Dim canInstall = Not String.IsNullOrEmpty(info.DownloadUrl) AndAlso Updater.CanSelfUpdate
            _lblState.Text = If(canInstall,
                "»Jetzt aktualisieren« lädt die neue Version herunter, ersetzt das Programm und startet es neu.",
                "Diese Version kann nicht automatisch installiert werden. Bitte laden Sie sie von der Release-Seite herunter.")

            _bar.Location = New Point(22, 354)
            _bar.Size = New Size(556, 14)
            _bar.Visible = False

            _btnInstall.Size = New Size(170, 36)
            _btnInstall.Location = New Point(22, 386)
            _btnInstall.Enabled = canInstall
            If Not canInstall Then _btnInstall.SetColors(Theme.Border, Theme.TextMuted)
            AddHandler _btnInstall.Click, AddressOf OnInstall

            _btnPage.Size = New Size(180, 36)
            _btnPage.Location = New Point(202, 386)
            AddHandler _btnPage.Click, Sub() AboutForm.OpenUrl(_info.PageUrl)

            _btnLater.Size = New Size(110, 36)
            _btnLater.Location = New Point(468, 386)
            _btnLater.DialogResult = DialogResult.Cancel
            CancelButton = _btnLater
            AcceptButton = If(canInstall, _btnInstall, _btnPage)

            Controls.AddRange({head, subh, notes, _lblState, _bar, _btnInstall, _btnPage, _btnLater})
            ResumeLayout(False)
            PerformLayout()
        End Sub

        ''' <summary>Schreibt die Versionshinweise formatiert: Überschriften fett, Aufzählungen mit Punkt und hängendem Einzug.</summary>
        Private Shared Sub FillNotes(box As RichTextBox, lines As List(Of NoteLine))
            Dim bold As New Font(Theme.FontBase, FontStyle.Bold)
            Dim first = True
            For Each l In lines
                Dim isHead = l.Kind = NoteKind.Version OrElse l.Kind = NoteKind.Heading
                If Not first Then
                    box.AppendText(vbLf)
                    If isHead Then
                        ' Leerzeile vor Überschriften, ohne Aufzählungspunkt
                        box.SelectionStart = box.TextLength
                        box.SelectionBullet = False
                        box.SelectionIndent = 0
                        box.SelectionHangingIndent = 0
                        box.AppendText(vbLf)
                    End If
                End If
                first = False
                box.SelectionStart = box.TextLength
                box.SelectionLength = 0
                box.SelectionBullet = l.Kind = NoteKind.Bullet
                box.SelectionIndent = If(l.Kind = NoteKind.Bullet, 6, 0)
                box.SelectionHangingIndent = If(l.Kind = NoteKind.Bullet, 14, 0)
                Select Case l.Kind
                    Case NoteKind.Version
                        AppendRun(box, l.PlainText, Theme.FontHeading, Theme.Accent)
                    Case NoteKind.Heading
                        AppendRun(box, l.PlainText, bold, Theme.TextDark)
                    Case Else
                        ' **fett** markierte Stellen fett schreiben
                        Dim parts = l.Text.Split({"**"}, StringSplitOptions.None)
                        For i = 0 To parts.Length - 1
                            If parts(i).Length > 0 Then AppendRun(box, parts(i), If(i Mod 2 = 1, bold, Theme.FontBase), Theme.TextDark)
                        Next
                End Select
            Next
            box.SelectionStart = 0
            box.SelectionLength = 0
        End Sub

        Private Shared Sub AppendRun(box As RichTextBox, text As String, font As Font, color As Color)
            box.SelectionStart = box.TextLength
            box.SelectionLength = 0
            box.SelectionFont = font
            box.SelectionColor = color
            box.AppendText(text)
        End Sub

        Private Async Sub OnInstall(sender As Object, e As EventArgs)
            _btnInstall.Enabled = False
            _btnLater.Enabled = False
            _bar.Visible = True
            _lblState.ForeColor = Theme.TextMuted
            _lblState.Text = "Neue Version wird heruntergeladen …"
            Try
                Dim progress As New Progress(Of Double)(Sub(p) _bar.Value = CInt(Math.Max(0, Math.Min(1, p)) * 100))
                Dim started = Await Updater.DownloadAndInstallAsync(_info, progress)
                If started Then
                    ' Die neue Version läuft bereits – diese Instanz beenden
                    Application.Exit()
                Else
                    _bar.Visible = False
                    _lblState.ForeColor = Theme.WarnText
                    _lblState.Text = "Die neue Version ist installiert, konnte aber nicht gestartet werden. Bitte NeuroSim schließen und neu starten."
                    _btnLater.Enabled = True
                End If
            Catch ex As Exception
                _bar.Visible = False
                _lblState.ForeColor = Theme.WarnText
                _lblState.Text = "Das Update konnte nicht installiert werden (" & ex.Message & "). Bitte laden Sie die neue Version von der Release-Seite herunter."
                _btnLater.Enabled = True
                AcceptButton = _btnPage
            End Try
        End Sub

    End Class

End Namespace
