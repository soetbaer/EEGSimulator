Option Strict On

Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports NeuroSim.Dsp
Imports NeuroSim.Services
Imports NeuroSim.Simulation
Imports NeuroSim.Training

Namespace UI

    Public Class MainForm
        Inherits Form

        ' ---------- Daten ----------
        Private ReadOnly _sim As New EegSimulator()
        Private ReadOnly _lessons As List(Of Lesson) = LessonCatalog.Create()
        Private ReadOnly _edges As Double() = SpectrumAnalysis.LogEdges(SpectrumView.FMin, SpectrumView.FMax, 72)
        Private _settings As New SimSettings()
        Private _chain As FilterChain
        Private _raw As Double()()
        Private _filtered As Double()()
        Private _peaks As List(Of Integer)
        Private _ecgRemaining As Double?
        Private _lessonIndex As Integer
        Private _lessonMode As Boolean = True
        Private _suspend As Boolean

        ' ---------- Steuerelemente ----------
        Private ReadOnly _eeg As New EegView()
        Private ReadOnly _spectrum As New SpectrumView()
        Private ReadOnly _info As New InfoCard()
        Private ReadOnly _cmbLesson As New ComboBox()
        Private ReadOnly _lblProgress As New Label()
        Private ReadOnly _navPanel As New TableLayoutPanel()
        Private _btnPrev As Button
        Private _btnNext As Button
        Private _btnReset As Button
        Private _btnModeLessons As Button
        Private _btnModeFree As Button

        Private ReadOnly _cmbLowCut As New ComboBox()
        Private ReadOnly _cmbHiCut As New ComboBox()
        Private ReadOnly _tglNotch As New ToggleSwitch()
        Private ReadOnly _tglEcg As New ToggleSwitch()
        Private ReadOnly _tglRaw As New ToggleSwitch()
        Private ReadOnly _tglRemoved As New ToggleSwitch()
        Private ReadOnly _lblSens As New Label()
        Private _btnSensMinus As Button
        Private _btnSensPlus As Button
        Private ReadOnly _partToggles As New Dictionary(Of SignalPart, ToggleSwitch)
        Private ReadOnly _imp As New ImpedanceView()
        Private ReadOnly _tglBridge As New ToggleSwitch()
        Private ReadOnly _rightPages As New List(Of FlowLayoutPanel)
        Private ReadOnly _rightTabs As New List(Of Button)
        Private _impResetButton As Button

        Private _btnPlay As Button
        Private ReadOnly _lblTime As New Label()
        Private ReadOnly _tips As New ToolTip() With {.AutoPopDelay = 15000, .InitialDelay = 400}

        Private ReadOnly _timer As New System.Windows.Forms.Timer() With {.Interval = 30}
        Private ReadOnly _clock As New Stopwatch()
        Private _lastTick As Double
        Private _viewStart As Double

        Private Const WindowSec As Double = 10
        Private Const ControlWidth As Integer = 272

        Public Sub New()
            SuspendLayout()
            AutoScaleDimensions = New SizeF(96.0F, 96.0F)
            AutoScaleMode = AutoScaleMode.Dpi
            Text = $"{AppInfo.Name} – {AppInfo.Tagline}"
            Dim appIco = Theme.AppIcon()
            If appIco IsNot Nothing Then Icon = appIco
            Font = Theme.FontBase
            BackColor = Theme.AppBack
            Size = New Size(1500, 920)
            MinimumSize = New Size(1200, 760)
            StartPosition = FormStartPosition.CenterScreen
            WindowState = FormWindowState.Maximized
            KeyPreview = True

            Dim center = BuildCenter()
            Dim left = BuildLeft()
            Dim right = BuildRight()
            Dim header = BuildHeader()
            Controls.Add(center)
            Controls.Add(left)
            Controls.Add(right)
            Controls.Add(header)
            center.BringToFront()

            ResumeLayout(False)
            PerformLayout()

            ' Namen für Bildschirmleser
            _eeg.AccessibleName = "EEG-Kurven"
            _eeg.AccessibleDescription = "Simulierte EEG-Kurven, rechts über links, unten EKG. Klick auf einen Kanal wählt ihn für die Frequenz-Ansicht."
            _spectrum.AccessibleName = "Frequenz-Ansicht"
            _imp.AccessibleName = "Impedanzen der Elektroden"
            _info.AccessibleName = "Erklärung"
            AddHandler _timer.Tick, AddressOf OnTick
            AddHandler _eeg.ChannelClicked, AddressOf OnChannelClicked

            SetMode(lessonMode:=True)
        End Sub

        ' =====================================================================
        '  Aufbau der Oberfläche
        ' =====================================================================

        Private Function BuildHeader() As Control
            Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 64, .BackColor = Theme.HeaderBack}

            Dim logo As New LogoMark With {.Size = New Size(44, 64), .Location = New Point(14, 0)}
            Dim title As New Label With {.Text = AppInfo.Name, .Font = Theme.FontAppTitle, .ForeColor = Theme.HeaderText,
                                         .AutoSize = True, .Location = New Point(64, 8)}
            Dim sub1 As New Label With {.Text = "Der EEG-Simulator: Filter · Impedanzen · Artefakte verstehen",
                                        .Font = Theme.FontSmall, .ForeColor = Theme.HeaderMuted, .AutoSize = True, .Location = New Point(66, 38)}

            Dim modes As New FlowLayoutPanel With {.Dock = DockStyle.Right, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False,
                                                   .AutoSize = True, .Padding = New Padding(0, 14, 16, 0), .BackColor = Theme.HeaderBack}
            _btnModeLessons = MakeButton("Geführte Lektionen", Theme.Accent, Color.White)
            _btnModeLessons.Width = 180
            _btnModeFree = MakeButton("Freier Simulator", Theme.HeaderBack, Color.White)
            _btnModeFree.Width = 160
            AddHandler _btnModeLessons.Click, Sub() SetMode(True)
            AddHandler _btnModeFree.Click, Sub() SetMode(False)
            Dim btnInfo = MakeButton("Info", Theme.HeaderBack, Color.White)
            btnInfo.Width = 72
            btnInfo.Margin = New Padding(14, 3, 3, 3)
            btnInfo.FlatAppearance.BorderSize = 1
            btnInfo.FlatAppearance.BorderColor = Theme.HeaderMuted
            btnInfo.FlatAppearance.MouseOverBackColor = Theme.Blend(Theme.HeaderBack, Color.White, 0.15)
            btnInfo.AccessibleName = "Info über NeuroSim: Quellen, Haftungsausschluss, Lizenz, Updates"
            _tips.SetToolTip(btnInfo, "Über NeuroSim: Entwickler, Quellen, Haftungsausschluss, Lizenz und Updates (F1)")
            AddHandler btnInfo.Click, Sub() ShowInfo()
            modes.Controls.Add(_btnModeLessons)
            modes.Controls.Add(_btnModeFree)
            modes.Controls.Add(btnInfo)

            header.Controls.Add(modes)
            header.Controls.Add(logo)
            header.Controls.Add(title)
            header.Controls.Add(sub1)
            Return header
        End Function

        Private Function BuildLeft() As Control
            Dim left As New Panel With {.Dock = DockStyle.Left, .Width = 380, .BackColor = Theme.Card, .Padding = New Padding(0, 0, 1, 0)}

            ' Kapitelauswahl
            Dim top As New Panel With {.Dock = DockStyle.Top, .Height = 52, .Padding = New Padding(16, 12, 16, 6), .BackColor = Theme.Card}
            _cmbLesson.Dock = DockStyle.Fill
            _cmbLesson.DropDownStyle = ComboBoxStyle.DropDownList
            _cmbLesson.FlatStyle = FlatStyle.Standard
            _cmbLesson.Font = Theme.FontBold
            For i = 0 To _lessons.Count - 1
                _cmbLesson.Items.Add($"{i + 1}.  {_lessons(i).Title}")
            Next
            AddHandler _cmbLesson.SelectedIndexChanged, Sub()
                                                            If _suspend OrElse _cmbLesson.SelectedIndex < 0 Then Return
                                                            ApplyLesson(_cmbLesson.SelectedIndex)
                                                        End Sub
            top.Controls.Add(_cmbLesson)

            ' Navigation unten
            _navPanel.Dock = DockStyle.Bottom
            _navPanel.Height = 96
            _navPanel.ColumnCount = 3
            _navPanel.RowCount = 2
            _navPanel.Padding = New Padding(12, 6, 12, 12)
            _navPanel.BackColor = Theme.Card
            _navPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 36))
            _navPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 28))
            _navPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 36))
            _navPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            _navPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            _btnReset = MakeButton("Lektion zurücksetzen (Ausgangszustand)", Theme.Card, Theme.AccentDark)
            _btnReset.Font = Theme.FontSmallBold
            _btnReset.Dock = DockStyle.Fill
            AddHandler _btnReset.Click, Sub() ApplyLesson(_lessonIndex)
            _navPanel.Controls.Add(_btnReset, 0, 0)
            _navPanel.SetColumnSpan(_btnReset, 3)

            _btnPrev = MakeButton("◄  Zurück", Theme.AccentLight, Theme.AccentDark)
            _btnPrev.Dock = DockStyle.Fill
            AddHandler _btnPrev.Click, Sub() ApplyLesson(_lessonIndex - 1)
            _btnNext = MakeButton("Weiter  ►", Theme.Accent, Color.White)
            _btnNext.Dock = DockStyle.Fill
            AddHandler _btnNext.Click, Sub()
                                           If _lessonIndex >= _lessons.Count - 1 Then SetMode(False) Else ApplyLesson(_lessonIndex + 1)
                                       End Sub
            _lblProgress.Dock = DockStyle.Fill
            _lblProgress.TextAlign = ContentAlignment.MiddleCenter
            _lblProgress.ForeColor = Theme.TextMuted
            _lblProgress.Font = Theme.FontBold
            _navPanel.Controls.Add(_btnPrev, 0, 1)
            _navPanel.Controls.Add(_lblProgress, 1, 1)
            _navPanel.Controls.Add(_btnNext, 2, 1)

            Dim sep As New Panel With {.Dock = DockStyle.Bottom, .Height = 1, .BackColor = Theme.Border}

            _info.Dock = DockStyle.Fill
            left.Controls.Add(_info)
            left.Controls.Add(sep)
            left.Controls.Add(_navPanel)
            left.Controls.Add(top)
            _info.BringToFront()

            Dim border As New Panel With {.Dock = DockStyle.Right, .Width = 1, .BackColor = Theme.Border}
            left.Controls.Add(border)
            Return left
        End Function

        Private Function BuildRight() As Control
            Dim right As New Panel With {.Dock = DockStyle.Right, .Width = 316, .BackColor = Theme.Card}
            Dim border As New Panel With {.Dock = DockStyle.Left, .Width = 1, .BackColor = Theme.Border}

            ' Drei Reiter statt einer langen Liste: so passt jeder Bereich ohne Scrollen auf den Bildschirm
            For i = 0 To 2
                _rightPages.Add(New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False,
                                                         .AutoScroll = True, .Padding = New Padding(18, 0, 8, 16), .BackColor = Theme.Card, .Visible = (i = 0)})
            Next
            Dim tabBar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 52, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False,
                                                    .Padding = New Padding(18, 12, 0, 0), .BackColor = Theme.Card}
            Dim tabNames = {"Einstellungen", "Impedanzen", "Signal"}
            Dim tabWidths = {104, 92, 76}
            For i = 0 To 2
                Dim idx = i
                Dim tb = MakeButton(tabNames(i), Theme.AccentLight, Theme.AccentDark)
                tb.Size = New Size(tabWidths(i), 32)
                tb.Font = Theme.FontSmallBold
                tb.Margin = New Padding(0)
                AddHandler tb.Click, Sub() ShowRightTab(idx)
                _rightTabs.Add(tb)
                tabBar.Controls.Add(tb)
            Next
            Dim flow = _rightPages(0)

            ' ---- Filter ----
            AddSection(flow, "Filter")
            AddLabel(flow, "LowCut (FU)  ·  Zeitkonstante (TC)", "dämpft LANGSAME Wellen (z. B. Schwitzen)")
            SetupCombo(_cmbLowCut, LowCutOptions.Select(Function(t) LowCutLabel(t)))
            flow.Controls.Add(_cmbLowCut)
            _tips.SetToolTip(_cmbLowCut, "Hochpass: Wellen, die langsamer als die Grenzfrequenz sind, werden abgeschwächt." & vbCrLf &
                                         "Kleinere Zeitkonstante = stärkerer Filter.  TC = 1 / (2 · π · FU)")

            AddLabel(flow, "HiCut (FO)", "dämpft SCHNELLE Wellen (z. B. Muskel-EMG)")
            SetupCombo(_cmbHiCut, HighCutOptions.Select(Function(f) HighCutLabel(f)))
            flow.Controls.Add(_cmbHiCut)
            _tips.SetToolTip(_cmbHiCut, "Tiefpass: Wellen, die schneller als die Grenzfrequenz sind, werden abgeschwächt.")

            AddSpacer(flow, 8)
            SetupToggle(flow, _tglNotch, "50-Hz-Filter (Notch)", Color.Empty)
            _tips.SetToolTip(_tglNotch, "Kerbfilter: schneidet gezielt den Netzbrumm bei 50 Hz heraus.")
            SetupToggle(flow, _tglEcg, "EKG-Filter", Color.Empty)
            _tips.SetToolTip(_tglEcg, "Erkennt die Herzschläge im EKG-Kanal und zieht das gemittelte EKG-Artefakt aus jedem EEG-Kanal ab.")

            ' ---- Amplitude ----
            AddSection(flow, "Amplitude")
            AddLabel(flow, "Empfindlichkeit", "kleinere Zahl = größere Kurven")
            Dim sensRow As New FlowLayoutPanel With {.FlowDirection = FlowDirection.LeftToRight, .WrapContents = False,
                                                     .Size = New Size(ControlWidth, 40), .Margin = New Padding(0, 2, 0, 4), .BackColor = Theme.Card}
            _btnSensMinus = MakeButton("−", Theme.AccentLight, Theme.AccentDark)
            _btnSensMinus.Size = New Size(52, 36)
            _btnSensMinus.Font = Theme.FontHeading
            _btnSensPlus = MakeButton("+", Theme.AccentLight, Theme.AccentDark)
            _btnSensPlus.Size = New Size(52, 36)
            _btnSensPlus.Font = Theme.FontHeading
            _lblSens.AutoSize = False
            _lblSens.Size = New Size(ControlWidth - 52 * 2 - 18, 36)
            _lblSens.TextAlign = ContentAlignment.MiddleCenter
            _lblSens.Font = Theme.FontHeading
            _lblSens.ForeColor = Theme.TextDark
            _btnSensMinus.Margin = New Padding(0)
            _btnSensPlus.Margin = New Padding(0)
            _lblSens.Margin = New Padding(4, 0, 4, 0)
            sensRow.Controls.Add(_btnSensMinus)
            sensRow.Controls.Add(_lblSens)
            sensRow.Controls.Add(_btnSensPlus)
            flow.Controls.Add(sensRow)
            _tips.SetToolTip(_btnSensMinus, "Kurven kleiner (größere µV/mm-Zahl)")
            _tips.SetToolTip(_btnSensPlus, "Kurven größer (kleinere µV/mm-Zahl)")
            AddHandler _btnSensMinus.Click, Sub() StepSensitivity(+1)
            AddHandler _btnSensPlus.Click, Sub() StepSensitivity(-1)

            ' ---- Anzeige ----
            AddSection(flow, "Anzeige")
            SetupToggle(flow, _tglRaw, "Rohsignal grau unterlegen", Theme.TraceRaw)
            SetupToggle(flow, _tglRemoved, "Entfernten Anteil rot zeigen", Theme.Removed)

            ' ---- Impedanzen ----
            flow = _rightPages(1)
            AddSection(flow, "Impedanzen der Elektroden")
            flow.Controls.Add(New Label With {.Text = "Werte in kΩ. Klick: erhöhen, Rechtsklick: verringern. Tastatur: Pfeiltasten wählen die Elektrode, + und − ändern den Wert.",
                                              .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = False,
                                              .Size = New Size(ControlWidth, 52), .Margin = New Padding(0, 0, 0, 2)})
            _imp.Size = New Size(ControlWidth, 300)
            _imp.Margin = New Padding(0, 0, 0, 4)
            flow.Controls.Add(_imp)
            SetupToggle(flow, _tglBridge, "Gelbrücke F3–C3 (zu niedrig)", ImpedanceView.ColTooLow)
            _tips.SetToolTip(_tglBridge, "Elektrodenpaste verbindet F3 und C3 leitend: Der Kanal F3-C3 wird fast flach.")
            Dim btnImpReset = MakeButton("Alle Impedanzen wieder gut", Theme.AccentLight, Theme.AccentDark)
            btnImpReset.Size = New Size(ControlWidth, 32)
            btnImpReset.Font = Theme.FontSmallBold
            btnImpReset.Margin = New Padding(0, 6, 0, 0)
            _impResetButton = btnImpReset
            flow.Controls.Add(btnImpReset)
            AddHandler btnImpReset.Click, Sub()
                                              _settings.Impedances = CType(EegSimulator.DefaultImpedances.Clone(), Double())
                                              _settings.SaltBridge = False
                                              ApplySettingsToControls()
                                          End Sub
            AddHandler _imp.ValuesChanged, AddressOf OnControlChanged
            AddHandler _tglBridge.CheckedChanged, AddressOf OnControlChanged

            ' ---- Bausteine ----
            flow = _rightPages(2)
            AddSection(flow, "Signal-Bausteine")
            AddHint(flow, "Was steckt im simulierten EEG? Einzeln ein- und ausschalten.")
            AddSubLabel(flow, "Hirnaktivität")
            For Each d In PartCatalog.All.Where(Function(x) x.IsBrain)
                AddPartToggle(flow, d)
            Next
            AddSubLabel(flow, "Artefakte (Störungen)")
            For Each d In PartCatalog.All.Where(Function(x) Not x.IsBrain)
                AddPartToggle(flow, d)
            Next

            ' Ereignisse
            AddHandler _cmbLowCut.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler _cmbHiCut.SelectedIndexChanged, AddressOf OnControlChanged
            AddHandler _tglNotch.CheckedChanged, AddressOf OnControlChanged
            AddHandler _tglEcg.CheckedChanged, AddressOf OnControlChanged
            AddHandler _tglRaw.CheckedChanged, AddressOf OnControlChanged
            AddHandler _tglRemoved.CheckedChanged, AddressOf OnControlChanged

            For Each pg In _rightPages
                right.Controls.Add(pg)
            Next
            right.Controls.Add(tabBar)
            right.Controls.Add(border)
            ShowRightTab(0)
            Return right
        End Function

        Private Function BuildCenter() As Control
            Dim center As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(14, 10, 14, 14), .BackColor = Theme.AppBack}

            ' Werkzeugleiste
            Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 46, .FlowDirection = FlowDirection.LeftToRight,
                                                 .WrapContents = False, .BackColor = Theme.AppBack, .Padding = New Padding(0, 2, 0, 6)}
            Dim btnBack = MakeButton("◄◄  10 s", Theme.Card, Theme.TextDark)
            btnBack.Width = 96
            _btnPlay = MakeButton("►  Abspielen", Theme.Accent, Color.White)
            _btnPlay.Width = 140
            Dim btnFwd = MakeButton("10 s  ►►", Theme.Card, Theme.TextDark)
            btnFwd.Width = 96
            For Each b In {btnBack, _btnPlay, btnFwd}
                b.Height = 36
                b.Margin = New Padding(0, 0, 8, 0)
            Next
            btnBack.FlatAppearance.BorderSize = 1 : btnBack.FlatAppearance.BorderColor = Theme.Border
            btnFwd.FlatAppearance.BorderSize = 1 : btnFwd.FlatAppearance.BorderColor = Theme.Border
            AddHandler btnBack.Click, Sub() MoveView(-WindowSec)
            AddHandler btnFwd.Click, Sub() MoveView(WindowSec)
            AddHandler _btnPlay.Click, Sub() TogglePlay()
            _lblTime.AutoSize = True
            _lblTime.Margin = New Padding(10, 9, 0, 0)
            _lblTime.ForeColor = Theme.TextMuted
            _lblTime.Font = Theme.FontBold
            Dim lblHint As New Label With {.AutoSize = True, .Margin = New Padding(24, 10, 0, 0), .ForeColor = Theme.TextMuted, .Font = Theme.FontSmall,
                                           .Text = "Klick auf Kanalnamen: Frequenz-Ansicht"}
            bar.Controls.AddRange({btnBack, _btnPlay, btnFwd, _lblTime, lblHint})

            ' EEG-Karte
            Dim eegCard As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(1), .BackColor = Theme.Border}
            _eeg.Dock = DockStyle.Fill
            eegCard.Controls.Add(_eeg)

            ' Abstand + Spektrum-Karte
            Dim gap As New Panel With {.Dock = DockStyle.Bottom, .Height = 12, .BackColor = Theme.AppBack}
            Dim specCard As New Panel With {.Dock = DockStyle.Bottom, .Height = 250, .Padding = New Padding(1), .BackColor = Theme.Border}
            _spectrum.Dock = DockStyle.Fill
            specCard.Controls.Add(_spectrum)

            center.Controls.Add(eegCard)
            center.Controls.Add(gap)
            center.Controls.Add(specCard)
            center.Controls.Add(bar)
            eegCard.BringToFront()
            Return center
        End Function

        ' ---------- kleine Bau-Helfer ----------

        Private Shared Function MakeButton(text As String, back As Color, fore As Color) As Button
            Dim b As New Button With {
                .Text = text, .FlatStyle = FlatStyle.Flat, .BackColor = back, .ForeColor = fore,
                .Font = Theme.FontBold, .Cursor = Cursors.Hand, .Height = 36, .Width = 120,
                .UseVisualStyleBackColor = False, .TabStop = True}
            b.FlatAppearance.BorderSize = 0
            b.FlatAppearance.MouseOverBackColor = Theme.Blend(back, If(back.GetBrightness() > 0.6, Theme.Accent, Color.White), 0.15)
            b.FlatAppearance.MouseDownBackColor = Theme.Blend(back, Color.Black, 0.12)
            Return b
        End Function

        Private Shared Sub AddSection(flow As FlowLayoutPanel, text As String)
            Dim lbl As New Label With {.Text = text, .Font = Theme.FontHeading, .ForeColor = Theme.TextDark, .AutoSize = False,
                                       .Size = New Size(ControlWidth, 30), .TextAlign = ContentAlignment.BottomLeft, .Margin = New Padding(0, 16, 0, 0)}
            Dim line As New Panel With {.Size = New Size(ControlWidth, 1), .BackColor = Theme.Border, .Margin = New Padding(0, 3, 0, 6)}
            flow.Controls.Add(lbl)
            flow.Controls.Add(line)
        End Sub

        Private Shared Sub AddLabel(flow As FlowLayoutPanel, text As String, hint As String)
            flow.Controls.Add(New Label With {.Text = text, .Font = Theme.FontBold, .ForeColor = Theme.TextDark, .AutoSize = False,
                                              .Size = New Size(ControlWidth, 20), .Margin = New Padding(0, 6, 0, 0)})
            flow.Controls.Add(New Label With {.Text = hint, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = False,
                                              .Size = New Size(ControlWidth, 18), .Margin = New Padding(0, 0, 0, 3)})
        End Sub

        Private Shared Sub AddHint(flow As FlowLayoutPanel, text As String)
            flow.Controls.Add(New Label With {.Text = text, .Font = Theme.FontSmall, .ForeColor = Theme.TextMuted, .AutoSize = False,
                                              .Size = New Size(ControlWidth, 34), .Margin = New Padding(0, 0, 0, 2)})
        End Sub

        Private Shared Sub AddSubLabel(flow As FlowLayoutPanel, text As String)
            flow.Controls.Add(New Label With {.Text = text, .Font = Theme.FontSmallBold, .ForeColor = Theme.TextMuted, .AutoSize = False,
                                              .Size = New Size(ControlWidth, 20), .Margin = New Padding(0, 6, 0, 0)})
        End Sub

        Private Shared Sub AddSpacer(flow As FlowLayoutPanel, h As Integer)
            flow.Controls.Add(New Panel With {.Size = New Size(ControlWidth, h), .Margin = New Padding(0)})
        End Sub

        Private Shared Sub SetupCombo(cmb As ComboBox, items As IEnumerable(Of String))
            cmb.DropDownStyle = ComboBoxStyle.DropDownList
            cmb.FlatStyle = FlatStyle.Standard
            cmb.Font = Theme.FontBase
            cmb.Width = ControlWidth
            cmb.Margin = New Padding(0, 0, 0, 6)
            cmb.Items.AddRange(items.Cast(Of Object)().ToArray())
        End Sub

        Private Shared Sub SetupToggle(flow As FlowLayoutPanel, t As ToggleSwitch, text As String, dot As Color)
            t.Text = text
            t.DotColor = dot
            t.Size = New Size(ControlWidth, 30)
            t.Margin = New Padding(0, 1, 0, 1)
            flow.Controls.Add(t)
        End Sub

        Private Sub AddPartToggle(flow As FlowLayoutPanel, d As PartDescriptor)
            Dim t As New ToggleSwitch()
            SetupToggle(flow, t, d.Label, d.Color)
            t.OnColor = Theme.Teal
            _tips.SetToolTip(t, d.Hint)
            _partToggles(d.Part) = t
            AddHandler t.CheckedChanged, AddressOf OnControlChanged
        End Sub

        ' =====================================================================
        '  Modus & Lektionen
        ' =====================================================================

        Private Sub SetMode(lessonMode As Boolean)
            _lessonMode = lessonMode
            _btnModeLessons.BackColor = If(lessonMode, Theme.Accent, Theme.HeaderBack)
            _btnModeFree.BackColor = If(lessonMode, Theme.HeaderBack, Theme.Accent)
            ' wie ein Umschalter: der nicht gewählte Modus bleibt als umrandete Schaltfläche erkennbar
            For Each b In {_btnModeLessons, _btnModeFree}
                b.FlatAppearance.BorderColor = Theme.HeaderMuted
                b.FlatAppearance.BorderSize = If(b.BackColor = Theme.Accent, 0, 1)
            Next
            _btnModeLessons.FlatAppearance.MouseOverBackColor = Theme.Blend(_btnModeLessons.BackColor, Color.White, 0.15)
            _btnModeFree.FlatAppearance.MouseOverBackColor = Theme.Blend(_btnModeFree.BackColor, Color.White, 0.15)
            _navPanel.Visible = lessonMode
            _cmbLesson.Parent.Visible = lessonMode
            If lessonMode Then
                ApplyLesson(_lessonIndex)
            Else
                UpdateInfo(resetScroll:=True)
            End If
        End Sub

        ''' <summary>Zeigt einen Reiter der rechten Leiste: 0 = Einstellungen, 1 = Impedanzen, 2 = Signal.</summary>
        Private Sub ShowRightTab(index As Integer)
            For i = 0 To _rightPages.Count - 1
                _rightPages(i).Visible = (i = index)
                Dim sel = (i = index)
                _rightTabs(i).BackColor = If(sel, Theme.Accent, Theme.AccentLight)
                _rightTabs(i).ForeColor = If(sel, Color.White, Theme.AccentDark)
                _rightTabs(i).FlatAppearance.MouseOverBackColor = If(sel, Theme.Accent, Theme.Blend(Theme.AccentLight, Theme.Accent, 0.15))
                _rightTabs(i).AccessibleDescription = If(sel, "ausgewählt", "")
            Next
            _rightPages(index).BringToFront()
        End Sub

        Private Sub ApplyLesson(index As Integer)
            If index < 0 OrElse index >= _lessons.Count Then Return
            _lessonIndex = index
            _settings = _lessons(index).Preset.Clone()
            _suspend = True
            _cmbLesson.SelectedIndex = index
            _suspend = False
            _lblProgress.Text = $"{index + 1} / {_lessons.Count}"
            _btnPrev.Enabled = index > 0
            _btnNext.Text = If(index < _lessons.Count - 1, "Weiter  ►", "Frei üben  ►")
            ApplySettingsToControls()
            UpdateInfo(resetScroll:=True)
            ' rechte Leiste passend zur Lektion positionieren
            ShowRightTab(_lessons(index).RightTab)
        End Sub

        Private Sub ApplySettingsToControls()
            _suspend = True
            Try
                _cmbLowCut.SelectedIndex = Math.Max(0, Array.IndexOf(LowCutOptions, _settings.LowCutTc))
                _cmbHiCut.SelectedIndex = Math.Max(0, Array.IndexOf(HighCutOptions, _settings.HighCutHz))
                _tglNotch.Checked = _settings.Notch
                _tglEcg.Checked = _settings.EcgFilter
                _tglRaw.Checked = _settings.ShowRaw
                _tglRemoved.Checked = _settings.ShowRemoved
                _tglBridge.Checked = _settings.SaltBridge
                _imp.Values = _settings.Impedances
                _imp.SaltBridge = _settings.SaltBridge
                For Each kv In _partToggles
                    kv.Value.Checked = _settings.Parts.Contains(kv.Key)
                Next
            Finally
                _suspend = False
            End Try
            Recompute()
        End Sub

        Private Sub ReadSettingsFromControls()
            _settings.LowCutTc = LowCutOptions(Math.Max(0, _cmbLowCut.SelectedIndex))
            _settings.HighCutHz = HighCutOptions(Math.Max(0, _cmbHiCut.SelectedIndex))
            _settings.Notch = _tglNotch.Checked
            _settings.EcgFilter = _tglEcg.Checked
            _settings.ShowRaw = _tglRaw.Checked
            _settings.ShowRemoved = _tglRemoved.Checked
            _settings.Impedances = _imp.Values
            _settings.SaltBridge = _tglBridge.Checked
            _imp.SaltBridge = _settings.SaltBridge
            _settings.Parts.Clear()
            For Each kv In _partToggles
                If kv.Value.Checked Then _settings.Parts.Add(kv.Key)
            Next
        End Sub

        Private Sub OnControlChanged(sender As Object, e As EventArgs)
            If _suspend Then Return
            ReadSettingsFromControls()
            Recompute()
        End Sub

        Private Sub StepSensitivity(direction As Integer)
            Dim idx = Array.IndexOf(SensitivityOptions, _settings.Sensitivity)
            If idx < 0 Then idx = Array.IndexOf(SensitivityOptions, 7.0)
            idx = Math.Max(0, Math.Min(SensitivityOptions.Length - 1, idx + direction))
            _settings.Sensitivity = SensitivityOptions(idx)
            Recompute()
        End Sub

        Private Sub OnChannelClicked(sender As Object, ch As Integer)
            _settings.SelectedChannel = ch
            _eeg.SelectedChannel = ch
            _eeg.Invalidate()
            UpdateSpectrum()
        End Sub

        ' =====================================================================
        '  Berechnung
        ' =====================================================================

        Private Sub Recompute()
            Dim fs = EegSimulator.SampleRate
            _raw = _sim.Mix(_settings.Parts, _settings.Impedances, _settings.SaltBridge)
            _chain = FilterChain.Create(_settings.LowCutHz, _settings.HighCutHz, _settings.Notch, fs)

            _filtered = New Double(_raw.Length - 1)() {}
            For c = 0 To _raw.Length - 1
                _filtered(c) = CType(_raw(c).Clone(), Double())
                _chain.ProcessInPlace(_filtered(c))
            Next

            _peaks = Nothing
            _ecgRemaining = Nothing
            If _settings.EcgFilter Then
                _peaks = EcgArtifactRemover.DetectRPeaks(_raw(EegSimulator.EcgChannel), fs)
                Const MeasureChannel As Integer = 3     ' P3-O1: dort ist das EKG-Artefakt am stärksten
                Dim beforeRemoval = CType(_filtered(MeasureChannel).Clone(), Double())
                For c = 0 To _raw.Length - 1
                    If c <> EegSimulator.EcgChannel Then EcgArtifactRemover.Remove(_filtered(c), _peaks, fs)
                Next
                If _settings.Parts.Contains(SignalPart.Ecg) AndAlso _peaks.Count >= 3 Then
                    _ecgRemaining = MeasureEcgRemaining(beforeRemoval, _filtered(MeasureChannel), MeasureChannel)
                End If
            End If

            _lblSens.Text = SensLabel(_settings.Sensitivity)
            _btnSensPlus.Enabled = _settings.Sensitivity > SensitivityOptions.First()
            _btnSensMinus.Enabled = _settings.Sensitivity < SensitivityOptions.Last()

            _eeg.EcgChannel = EegSimulator.EcgChannel
            _eeg.Sensitivity = _settings.Sensitivity
            _eeg.WindowSeconds = WindowSec
            _eeg.StartSeconds = _viewStart
            _eeg.ShowRaw = _settings.ShowRaw
            _eeg.ShowRemoved = _settings.ShowRemoved
            _eeg.SelectedChannel = _settings.SelectedChannel
            _eeg.SetData(EegSimulator.ChannelNames, _raw, _filtered, fs, _peaks)
            UpdateTimeLabel()

            UpdateSpectrum()
            UpdateInfo(resetScroll:=False)
        End Sub

        ''' <summary>
        ''' Misst, wie viel vom EKG-Artefakt nach Filterkette + EKG-Filter übrig ist (0 … 1),
        ''' jeweils im Bereich ±30 ms um die tatsächlichen Herzschläge.
        ''' </summary>
        Private Function MeasureEcgRemaining(beforeRemoval As Double(), afterRemoval As Double(), channel As Integer) As Double
            Dim fs = EegSimulator.SampleRate
            Dim rawArt = _sim.PartSignal(SignalPart.Ecg, channel)
            Dim filtArt = CType(rawArt.Clone(), Double())
            _chain.ProcessInPlace(filtArt)
            Dim half = CInt(0.03 * fs)
            Dim a = 0.0, b = 0.0
            For Each p In _sim.TrueBeats
                For i = Math.Max(0, p - half) To Math.Min(rawArt.Length - 1, p + half)
                    Dim removed = beforeRemoval(i) - afterRemoval(i)
                    Dim rest = filtArt(i) - removed
                    a += rawArt(i) * rawArt(i)
                    b += rest * rest
                Next
            Next
            If a <= 0 Then Return 0
            Return Math.Min(1.0, Math.Sqrt(b / a))
        End Function

        Private Sub UpdateSpectrum()
            If _raw Is Nothing Then Return
            Dim ch = _settings.SelectedChannel
            Dim fs = EegSimulator.SampleRate
            Dim rawBars = SpectrumAnalysis.Bars(_raw(ch), fs, _edges)
            Dim filtBars = SpectrumAnalysis.Bars(_filtered(ch), fs, _edges)
            Dim chain = _chain
            _spectrum.ChannelName = EegSimulator.ChannelNames(ch)
            _spectrum.LowCutHz = _settings.LowCutHz
            _spectrum.HighCutHz = _settings.HighCutHz
            _spectrum.Notch = _settings.Notch
            _spectrum.SetData(_edges, rawBars, filtBars, Function(f As Double) chain.Magnitude(f))
        End Sub

        Private Sub UpdateInfo(resetScroll As Boolean)
            If _chain Is Nothing Then Return
            Dim blocks As New List(Of InfoBlock)
            If _lessonMode Then
                Dim les = _lessons(_lessonIndex)
                blocks.Add(InfoBlock.Subtitle($"Lektion {_lessonIndex + 1} von {_lessons.Count}"))
                blocks.Add(InfoBlock.Title(les.Title))
                blocks.AddRange(les.Blocks)
                blocks.Add(InfoBlock.Divider())
            Else
                blocks.Add(InfoBlock.Subtitle("Freier Simulator"))
                blocks.Add(InfoBlock.Title("Ausprobieren erwünscht!"))
                blocks.Add(InfoBlock.Para("Alle Regler sind frei. Stellen Sie rechts zusammen, was im EEG stecken soll, und probieren Sie die Filter aus. Hier erklärt die Anwendung laufend, was Ihre Einstellung bewirkt."))
            End If
            blocks.AddRange(Explainer.Describe(_settings, _chain, _sim, If(_peaks Is Nothing, 0, _peaks.Count), _ecgRemaining))
            _info.SetBlocks(blocks, resetScroll)
        End Sub

        ' =====================================================================
        '  Abspielen / Blättern
        ' =====================================================================

        Private Sub TogglePlay()
            If _timer.Enabled Then
                _timer.Stop()
                _clock.Stop()
                _btnPlay.Text = "►  Abspielen"
            Else
                _clock.Restart()
                _lastTick = 0
                _timer.Start()
                _btnPlay.Text = "■  Anhalten"
            End If
        End Sub

        Private Sub OnTick(sender As Object, e As EventArgs)
            Dim now = _clock.Elapsed.TotalSeconds
            Dim dt = now - _lastTick
            _lastTick = now
            _viewStart += dt
            If _viewStart > EegSimulator.Duration - WindowSec Then _viewStart = 0
            _eeg.StartSeconds = _viewStart
            _eeg.Invalidate()
            UpdateTimeLabel()
        End Sub

        Private Sub MoveView(delta As Double)
            _viewStart = Math.Max(0, Math.Min(EegSimulator.Duration - WindowSec, Math.Round(_viewStart + delta)))
            _eeg.StartSeconds = _viewStart
            _eeg.Invalidate()
            UpdateTimeLabel()
        End Sub

        Private Sub UpdateTimeLabel()
            _lblTime.Text = $"{_viewStart:0} – {_viewStart + WindowSec:0} s  von {EegSimulator.Duration:0} s"
        End Sub

        Private Sub ShowInfo()
            Using dlg As New AboutForm()
                dlg.ShowDialog(Me)
            End Using
        End Sub

        ''' <summary>Nach dem ersten Anzeigen: Reste eines Updates wegräumen und still nach einer neuen Version suchen.</summary>
        Protected Overrides Async Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            Updater.CleanupOldVersion()
            If Not AppSettings.AutoUpdateCheck Then Return
            Try
                Dim info = Await Updater.CheckAsync()
                If info IsNot Nothing AndAlso info.IsNewer AndAlso Not IsDisposed Then UpdatePrompt.ShowFor(Me, info)
            Catch
                ' kein Netz oder GitHub nicht erreichbar: beim Start nicht stören
            End Try
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.F1 Then
                ShowInfo()
                Return True
            End If
            ' Leertaste = Abspielen/Pause, Bild auf/ab = blättern (wenn kein Eingabefeld aktiv ist)
            If TypeOf ActiveControl Is ComboBox OrElse TypeOf ActiveControl Is ImpedanceView Then Return MyBase.ProcessCmdKey(msg, keyData)
            Select Case keyData
                Case Keys.PageDown, Keys.Right : MoveView(WindowSec) : Return True
                Case Keys.PageUp, Keys.Left : MoveView(-WindowSec) : Return True
                Case Keys.P : TogglePlay() : Return True
            End Select
            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            _timer.Stop()
            MyBase.OnFormClosed(e)
        End Sub

    End Class

End Namespace
