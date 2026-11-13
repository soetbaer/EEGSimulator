Option Strict On

Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports NeuroSim.Services

Namespace UI

    ''' <summary>Info-Dialog: Über NeuroSim (mit Update-Funktion), Quellen, Haftungsausschluss, Lizenz.</summary>
    Public Class AboutForm
        Inherits Form

        Private ReadOnly _chkAuto As New CheckBox()
        Private ReadOnly _btnCheck As New Button()
        Private ReadOnly _lblStatus As New Label()

        Public Sub New()
            SuspendLayout()
            AutoScaleDimensions = New SizeF(96.0F, 96.0F)
            AutoScaleMode = AutoScaleMode.Dpi
            Text = $"Info über {AppInfo.Name}"
            Font = Theme.FontBase
            BackColor = Theme.Card
            ClientSize = New Size(760, 620)
            MinimumSize = New Size(640, 520)
            StartPosition = FormStartPosition.CenterParent
            ShowInTaskbar = False
            MinimizeBox = False
            MaximizeBox = False
            Dim ico = Theme.AppIcon()
            If ico IsNot Nothing Then Icon = ico

            Dim tabs As New TabControl With {.Dock = DockStyle.Fill, .Font = Theme.FontBase, .Padding = New Point(14, 6)}
            tabs.TabPages.Add(BuildAboutPage())
            tabs.TabPages.Add(TextPage("Quellen", LegalTexts.Sources))
            tabs.TabPages.Add(TextPage("Haftungsausschluss", LegalTexts.Disclaimer))
            tabs.TabPages.Add(TextPage("Lizenz", LegalTexts.LicenseIntro & LegalTexts.LicenseText, monospace:=True))

            Dim bottom As New Panel With {.Dock = DockStyle.Bottom, .Height = 56, .BackColor = Theme.Card}
            Dim btnClose As New Button With {.Text = "Schließen", .Size = New Size(120, 34), .Anchor = AnchorStyles.Right Or AnchorStyles.Bottom,
                                             .DialogResult = DialogResult.OK, .Font = Theme.FontBase}
            btnClose.Location = New Point(bottom.Width - btnClose.Width - 16, 11)
            bottom.Controls.Add(btnClose)
            AcceptButton = btnClose
            CancelButton = btnClose

            Dim host As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(12, 12, 12, 0), .BackColor = Theme.Card}
            host.Controls.Add(tabs)
            Controls.Add(host)
            Controls.Add(bottom)
            host.BringToFront()
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Private Function BuildAboutPage() As TabPage
            Dim page As New TabPage("Über") With {.BackColor = Theme.Card, .Padding = New Padding(18)}

            Dim logo As New LogoMark With {.BackColor = Theme.Card, .Size = New Size(96, 96), .Location = New Point(22, 22)}
            Dim name As New Label With {.Text = AppInfo.Name, .Font = New Font("Segoe UI Semibold", 24.0F), .ForeColor = Theme.TextDark,
                                        .AutoSize = True, .Location = New Point(134, 20)}
            Dim tag As New Label With {.Text = AppInfo.Tagline, .Font = Theme.FontBase, .ForeColor = Theme.TextMuted,
                                       .AutoSize = True, .Location = New Point(138, 66)}
            Dim ver As New Label With {.Text = $"Version {AppInfo.VersionText}", .Font = Theme.FontBold, .ForeColor = Theme.TextDark,
                                       .AutoSize = True, .Location = New Point(138, 92)}

            Dim about As New TextBox With {.Multiline = True, .ReadOnly = True, .BorderStyle = BorderStyle.None, .BackColor = Theme.Card,
                                           .ForeColor = Theme.TextDark, .Font = Theme.FontBase, .Text = LegalTexts.About, .TabStop = False,
                                           .Location = New Point(24, 136), .Size = New Size(400, 190)}

            Dim link As New LinkLabel With {.Text = "Quellcode und neue Versionen auf GitHub öffnen", .AutoSize = True, .Location = New Point(22, 330),
                                            .LinkColor = Theme.AccentDark, .Font = Theme.FontBase}
            AddHandler link.LinkClicked, Sub() OpenUrl(AppInfo.RepoUrl)

            ' ---- Updates ----
            Dim line As New Panel With {.Location = New Point(24, 368), .Size = New Size(400, 1), .BackColor = Theme.Border}
            Dim upd As New Label With {.Text = "Updates", .Font = Theme.FontHeading, .ForeColor = Theme.TextDark, .AutoSize = True, .Location = New Point(20, 380)}

            _chkAuto.Text = "Beim Start automatisch nach Updates suchen"
            _chkAuto.AutoSize = True
            _chkAuto.Location = New Point(24, 414)
            _chkAuto.Checked = AppSettings.AutoUpdateCheck
            AddHandler _chkAuto.CheckedChanged, Sub() AppSettings.AutoUpdateCheck = _chkAuto.Checked

            _btnCheck.Text = "Jetzt nach Updates suchen"
            _btnCheck.Size = New Size(220, 34)
            _btnCheck.Location = New Point(24, 446)
            AddHandler _btnCheck.Click, AddressOf OnCheckClicked

            _lblStatus.AutoSize = False
            _lblStatus.Location = New Point(256, 446)
            _lblStatus.Size = New Size(300, 44)
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft
            _lblStatus.ForeColor = Theme.TextMuted
            _lblStatus.Text = "Bei der Suche wird nur die neueste Versionsnummer bei GitHub abgefragt."

            ' Breiten an die Seite anpassen (TabPages bekommen ihre Größe erst beim Anzeigen)
            Dim fit = Sub()
                          Dim w = Math.Max(200, page.ClientSize.Width - about.Left * 2)
                          about.Width = w
                          line.Width = w
                          _lblStatus.Width = Math.Max(100, page.ClientSize.Width - _lblStatus.Left - about.Left)
                      End Sub
            AddHandler page.Resize, Sub() fit()
            AddHandler page.Layout, Sub() fit()

            page.Controls.AddRange({logo, name, tag, ver, about, link, line, upd, _chkAuto, _btnCheck, _lblStatus})
            Return page
        End Function

        Private Shared Function TextPage(title As String, text As String, Optional monospace As Boolean = False) As TabPage
            Dim page As New TabPage(title) With {.BackColor = Theme.Card, .Padding = New Padding(10)}
            Dim box As New TextBox With {.Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .Dock = DockStyle.Fill,
                                         .BorderStyle = BorderStyle.None, .BackColor = Theme.Card, .ForeColor = Theme.TextDark,
                                         .Font = If(monospace, New Font("Consolas", 9.5F), Theme.FontBase), .Text = text, .WordWrap = True}
            AddHandler page.Enter, Sub() box.Select(0, 0)
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
        Private ReadOnly _btnInstall As New Button()
        Private ReadOnly _btnPage As New Button()
        Private ReadOnly _btnLater As New Button()
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
            Dim subh As New Label With {.Text = $"Installiert ist Version {AppInfo.VersionText}. Das ist neu:", .ForeColor = Theme.TextMuted,
                                        .AutoSize = True, .Location = New Point(22, 54)}
            Dim notes As New TextBox With {.Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .Location = New Point(22, 82),
                                           .Size = New Size(556, 220), .BackColor = Theme.LabelColumn, .ForeColor = Theme.TextDark,
                                           .BorderStyle = BorderStyle.FixedSingle, .TabStop = False,
                                           .Text = If(String.IsNullOrWhiteSpace(info.Notes), "(Keine Beschreibung vorhanden.)",
                                                      info.Notes.Replace(vbCrLf, vbLf).Replace(vbLf, Environment.NewLine))}

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

            _btnInstall.Text = "Jetzt aktualisieren"
            _btnInstall.Size = New Size(170, 36)
            _btnInstall.Location = New Point(22, 386)
            _btnInstall.Enabled = canInstall
            AddHandler _btnInstall.Click, AddressOf OnInstall

            _btnPage.Text = "Release-Seite öffnen"
            _btnPage.Size = New Size(180, 36)
            _btnPage.Location = New Point(202, 386)
            AddHandler _btnPage.Click, Sub() AboutForm.OpenUrl(_info.PageUrl)

            _btnLater.Text = "Später"
            _btnLater.Size = New Size(110, 36)
            _btnLater.Location = New Point(468, 386)
            _btnLater.DialogResult = DialogResult.Cancel
            CancelButton = _btnLater
            AcceptButton = If(canInstall, _btnInstall, _btnPage)

            Controls.AddRange({head, subh, notes, _lblState, _bar, _btnInstall, _btnPage, _btnLater})
            ResumeLayout(False)
            PerformLayout()
        End Sub

        Private Async Sub OnInstall(sender As Object, e As EventArgs)
            _btnInstall.Enabled = False
            _btnLater.Enabled = False
            _bar.Visible = True
            _lblState.ForeColor = Theme.TextMuted
            _lblState.Text = "Neue Version wird heruntergeladen …"
            Try
                Dim progress As New Progress(Of Double)(Sub(p) _bar.Value = CInt(Math.Max(0, Math.Min(1, p)) * 100))
                Await Updater.DownloadAndInstallAsync(_info, progress)
                ' Die neue Version läuft bereits – diese Instanz beenden
                Application.Exit()
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
