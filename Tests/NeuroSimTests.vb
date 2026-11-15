Option Strict On

Imports NeuroSim.Dsp
Imports NeuroSim.Services
Imports NeuroSim.Simulation
Imports NeuroSim.Training
Imports Xunit

''' <summary>
''' Prüft die Zahlen, auf die sich die Lektionstexte stützen: Filter, Simulation, Update-Funktion.
''' </summary>
Public Class NeuroSimTests

    Private Const Fs As Double = EegSimulator.SampleRate
    Private Shared ReadOnly Sim As New EegSimulator()

    ' ---------- Filter ----------

    <Theory>
    <InlineData(2.0)>
    <InlineData(1.0)>
    <InlineData(0.3)>
    <InlineData(0.1)>
    <InlineData(0.03)>
    Public Sub LowCut_laesst_an_der_Grenzfrequenz_70_Prozent_durch(tc As Double)
        Dim s As New SimSettings With {.LowCutTc = tc}
        Dim chain = FilterChain.Create(s.LowCutHz, 0, False, Fs)
        Assert.InRange(chain.Magnitude(s.LowCutHz), 0.7, 0.715)
    End Sub

    <Fact>
    Public Sub Zeitkonstante_0_3_s_entspricht_0_53_Hz()
        Dim s As New SimSettings With {.LowCutTc = 0.3}
        Assert.InRange(s.LowCutHz, 0.525, 0.535)
    End Sub

    <Theory>
    <InlineData(100.0)>
    <InlineData(70.0)>
    <InlineData(35.0)>
    <InlineData(15.0)>
    Public Sub HiCut_laesst_an_der_Grenzfrequenz_70_Prozent_durch(f As Double)
        Dim chain = FilterChain.Create(0, f, False, Fs)
        Assert.InRange(chain.Magnitude(f), 0.7, 0.715)
    End Sub

    <Fact>
    Public Sub HiCut_70_Hz_hilft_kaum_gegen_50_Hz()
        Dim chain = FilterChain.Create(0, 70, False, Fs)
        Assert.True(chain.Magnitude(50) > 0.85)
    End Sub

    <Fact>
    Public Sub Kerbfilter_entfernt_50_Hz_und_schont_die_Umgebung()
        Dim chain = FilterChain.Create(0, 0, True, Fs)
        Assert.True(chain.Magnitude(50) < 0.01)
        Assert.True(chain.Magnitude(40) > 0.9)
        Assert.True(chain.Magnitude(10) > 0.99)
    End Sub

    ' ---------- Aussagen der Lektionen ----------

    <Fact>
    Public Sub HiCut_15_Hz_nimmt_dem_Spike_rund_40_Prozent_der_Hoehe()
        Dim rest = Explainer.MeasurePassThrough(Sim, SignalPart.Spikes, FilterChain.Create(0, 15, False, Fs))
        Assert.InRange(rest, 0.5, 0.68)
    End Sub

    <Fact>
    Public Sub HiCut_70_Hz_laesst_den_Spike_fast_unveraendert()
        Dim rest = Explainer.MeasurePassThrough(Sim, SignalPart.Spikes, FilterChain.Create(0, 70, False, Fs))
        Assert.True(rest > 0.9)
    End Sub

    <Fact>
    Public Sub LowCut_TC_0_03_s_laesst_vom_Delta_Herd_weniger_als_ein_Drittel()
        Dim s As New SimSettings With {.LowCutTc = 0.03}
        Dim rest = Explainer.MeasurePassThrough(Sim, SignalPart.Delta, FilterChain.Create(s.LowCutHz, 0, False, Fs))
        Assert.True(rest < 0.33)
    End Sub

    <Fact>
    Public Sub LowCut_TC_0_3_s_laesst_einen_Teil_des_Schwitzartefakts_stehen()
        Dim s As New SimSettings With {.LowCutTc = 0.3}
        Dim rest = Explainer.MeasurePassThrough(Sim, SignalPart.Sweat, FilterChain.Create(s.LowCutHz, 0, False, Fs))
        Assert.InRange(rest, 0.2, 0.7)
    End Sub

    ' ---------- Simulation ----------

    Private Shared Function MaxAbsDifference(a As Double(), b As Double()) As Double
        Dim m = 0.0
        For i = 0 To a.Length - 1
            m = Math.Max(m, Math.Abs(a(i) - b(i)))
        Next
        Return m
    End Function

    Private Shared Function MaxAbs(a As Double()) As Double
        Dim m = 0.0
        For i = CInt(5 * Fs) To a.Length - 1
            m = Math.Max(m, Math.Abs(a(i)))
        Next
        Return m
    End Function

    Private Shared Function Channel(montageId As String, label As String, parts As SignalPart(), Optional z As Double() = Nothing,
                                    Optional saltBridge As Boolean = False) As Double()
        Dim m = Montages.Get(montageId)
        Dim index = m.IndexOf(label)
        Assert.True(index >= 0, $"Kanal {label} fehlt in {montageId}")
        Return Sim.Mix(m, parts, z, saltBridge)(index)
    End Function

    Private Shared ReadOnly NothingOn As SignalPart() = Array.Empty(Of SignalPart)()

    <Fact>
    Public Sub Netzbrumm_haengt_vom_Impedanz_Unterschied_ab()
        Dim humOnly = New SignalPart() {SignalPart.Hum}

        ' gleiche Impedanzen: kein Brumm im Kanal
        Dim equal = Enumerable.Repeat(5.0, Head.Count).ToArray()
        Assert.True(MaxAbsDifference(Channel(Montages.Excerpt, "F3-C3", humOnly, equal), Channel(Montages.Excerpt, "F3-C3", NothingOn, equal)) < 0.000001)

        ' C3 schlecht: Brumm in F3-C3
        Dim unequal = CType(equal.Clone(), Double())
        unequal(Head.Index("C3")) = 25
        Assert.True(MaxAbsDifference(Channel(Montages.Excerpt, "F3-C3", humOnly, unequal), Channel(Montages.Excerpt, "F3-C3", NothingOn, unequal)) > 20)
    End Sub

    <Fact>
    Public Sub EKG_Filter_findet_die_Herzschlaege()
        Dim m = Montages.Get(Montages.Excerpt)
        Dim raw = Sim.Mix(m, New SignalPart() {SignalPart.Ecg})
        Dim peaks = EcgArtifactRemover.DetectRPeaks(raw(m.EcgChannel), Fs)
        Assert.InRange(peaks.Count, Sim.TrueBeats.Count - 2, Sim.TrueBeats.Count)
    End Sub

    ' ---------- Verschaltungen ----------

    <Fact>
    Public Sub Verschaltungen_haben_mindestens_18_Kanaele_und_das_EKG_zuletzt()
        For Each id In {Montages.Longitudinal, Montages.Transverse, Montages.Ear, Montages.Vertex, Montages.Average, Montages.Source}
            Dim m = Montages.Get(id)
            Assert.True(m.Channels.Count - 1 >= 18, id)
            Assert.Equal(m.Channels.Count - 1, m.EcgChannel)
        Next
    End Sub

    <Fact>
    Public Sub Laengsreihe_folgt_der_DGKN_Empfehlung()
        Dim labels = Montages.Get(Montages.Longitudinal).Channels.Select(Function(c) c.Label).ToArray()
        Assert.Equal({"Fp2-F8", "F8-T4", "T4-T6", "T6-O2", "Fp1-F7", "F7-T3", "T3-T5", "T5-O1",
                      "Fp2-F4", "F4-C4", "C4-P4", "P4-O2", "Fp1-F3", "F3-C3", "C3-P3", "P3-O1", "Fz-Cz", "Cz-Pz", "EKG"}, labels)
    End Sub

    <Fact>
    Public Sub Jeder_EEG_Kanal_ist_eine_Differenz()
        ' Die Gewichte jedes EEG-Kanals ergeben zusammen null: Was an allen Elektroden gleich ist, fällt heraus.
        For Each m In Montages.All
            For Each c In m.Channels
                If c.Kind = ChannelKind.Eeg Then Assert.True(Math.Abs(c.Weights.Sum()) < 0.000000001, $"{m.Id} {c.Label}")
            Next
        Next
    End Sub

    <Fact>
    Public Sub Bipolare_Reihe_zeigt_Phasenumkehr_bei_C3()
        Dim spikes = New SignalPart() {SignalPart.Spikes}
        Dim first = Array.IndexOf(Sim.PartSignal(SignalPart.Spikes), Sim.PartSignal(SignalPart.Spikes).Max())
        ' oberflächennegativer Herd unter C3: F3-C3 positiv (Ausschlag nach unten), C3-P3 negativ (nach oben)
        Assert.True(Channel(Montages.Longitudinal, "F3-C3", spikes)(first) > 80)
        Assert.True(Channel(Montages.Longitudinal, "C3-P3", spikes)(first) < -80)
        Assert.True(Channel(Montages.Transverse, "Cz-C3", spikes)(first) > 80)
        Assert.True(Channel(Montages.Transverse, "C3-T3", spikes)(first) < -80)
    End Sub

    <Fact>
    Public Sub Referenzableitung_zeigt_den_Herd_am_groessten_in_C3()
        Dim spikes = New SignalPart() {SignalPart.Spikes}
        Dim c3 = MaxAbs(Channel(Montages.Ear, "C3-A1", spikes))
        For Each other In {"F3-A1", "P3-A1", "T3-A1", "C4-A2"}
            Assert.True(c3 > 2 * MaxAbs(Channel(Montages.Ear, other, spikes)), other)
        Next
    End Sub

    <Fact>
    Public Sub Vertexreferenz_traegt_den_Herd_in_alle_Kanaele()
        ' Cz liegt im Feld des Herdes: rechts erscheint ein Ausschlag in Gegenrichtung, in der Ohrreferenz nicht
        Dim spikes = New SignalPart() {SignalPart.Spikes}
        Dim first = Array.IndexOf(Sim.PartSignal(SignalPart.Spikes), Sim.PartSignal(SignalPart.Spikes).Max())
        Assert.True(Channel(Montages.Vertex, "T4-Cz", spikes)(first) > 25)
        Assert.True(Math.Abs(Channel(Montages.Ear, "T4-A2", spikes)(first)) < 15)
    End Sub

    ''' <summary>Größter Ausschlag, den die Bausteine allein in einem Kanal erzeugen (ohne Grundrauschen).</summary>
    Private Shared Function Amplitude(montageId As String, label As String, parts As SignalPart()) As Double
        Return MaxAbsDifference(Channel(montageId, label, parts), Channel(montageId, label, NothingOn))
    End Function

    <Fact>
    Public Sub Quellenableitung_verkleinert_ausgebreitetes_Alpha()
        Dim alpha = New SignalPart() {SignalPart.Alpha}
        Assert.True(Amplitude(Montages.Source, "O1-Q", alpha) < 0.6 * Amplitude(Montages.Ear, "O1-A1", alpha))
        Assert.True(Amplitude(Montages.Source, "O1-Q", alpha) < 0.75 * Amplitude(Montages.Average, "O1-AV", alpha))
    End Sub

    <Fact>
    Public Sub Quellenableitung_zeigt_den_Herd_fast_nur_in_C3()
        Dim spikes = New SignalPart() {SignalPart.Spikes}
        Dim c3 = Amplitude(Montages.Source, "C3-Q", spikes)
        For Each other In {"F3-Q", "P3-Q", "T3-Q", "Cz-Q", "C4-Q"}
            Assert.True(Amplitude(Montages.Source, other, spikes) < 0.2 * c3, other)
        Next
    End Sub

    <Fact>
    Public Sub Ohrreferenz_holt_Alpha_in_die_Stirnkanaele()
        Dim alpha = New SignalPart() {SignalPart.Alpha}
        Assert.True(Amplitude(Montages.Ear, "Fp1-A1", alpha) > 3 * Amplitude(Montages.Longitudinal, "Fp1-F3", alpha))
    End Sub

    <Fact>
    Public Sub Durchschnittsreferenz_traegt_Blinzeln_in_alle_Kanaele()
        Dim blinks = New SignalPart() {SignalPart.Blinks}
        ' okzipital entsteht kein Blinzeln; in der Durchschnittsreferenz erscheint es dort trotzdem
        Assert.True(Amplitude(Montages.Average, "O1-AV", blinks) > 20)
        Assert.True(Amplitude(Montages.Ear, "O1-A1", blinks) < 20)
    End Sub

    <Fact>
    Public Sub EKG_Artefakt_ist_in_der_Ohrreferenz_groesser_als_in_der_Laengsreihe()
        Dim maxGain = Function(id As String) Montages.Get(id).Channels.Max(Function(c) Math.Abs(EegSimulator.EcgArtifactGain(c)))
        Assert.True(maxGain(Montages.Ear) > 2 * maxGain(Montages.Longitudinal))
    End Sub

    <Fact>
    Public Sub Gleichtakt_erscheint_nur_an_den_einzelnen_Eingaengen()
        Dim hum = New SignalPart() {SignalPart.Hum}
        Dim equal = Enumerable.Repeat(3.0, Head.Count).ToArray()
        ' am einzelnen Eingang rund 1000 µV Netzstörung, im Ausgang (Differenz) praktisch nichts
        Assert.True(MaxAbs(Channel(Montages.Amplifier, "P3 (1:20)", hum, equal)) > 800)
        Assert.True(MaxAbsDifference(Channel(Montages.Amplifier, "P3-O1", hum, equal), Channel(Montages.Amplifier, "P3-O1", NothingOn, equal)) < 0.000001)
    End Sub

    <Fact>
    Public Sub Gelbruecke_macht_den_Kanal_zwischen_den_Elektroden_flach()
        Dim parts = New SignalPart() {SignalPart.Alpha, SignalPart.Beta, SignalPart.Delta}
        Dim normal = MaxAbs(Channel(Montages.Longitudinal, "F3-C3", parts))
        Dim bridged = MaxAbs(Channel(Montages.Longitudinal, "F3-C3", parts, Nothing, True))
        Assert.True(bridged < 0.15 * normal)
    End Sub

    ' ---------- Lektionen ----------

    <Fact>
    Public Sub Lektionen_haben_gueltige_Voreinstellungen()
        Dim lessons = LessonCatalog.Create()
        Assert.Equal(23, lessons.Count)
        For Each les In lessons
            Assert.NotEqual("", les.Chapter)
            Assert.Contains(les.Preset.LowCutTc, LowCutOptions)
            Assert.Contains(les.Preset.HighCutHz, HighCutOptions)
            Assert.Contains(les.Preset.Sensitivity, SensitivityOptions)
            Assert.Equal(Head.Count, les.Preset.Impedances.Length)
            For Each z In les.Preset.Impedances
                Assert.Contains(z, ImpedanceOptions)
            Next
            Assert.Contains(les.Preset.MontageId, Montages.All.Select(Function(m) m.Id))
            Assert.InRange(les.Preset.SelectedChannel, 0, les.Preset.Montage.Channels.Count - 1)
            ' Alpha (Augen zu) und Augenblinzeln (Augen auf) nicht gemeinsam voreinstellen
            Assert.False(les.Preset.Parts.Contains(SignalPart.Alpha) AndAlso les.Preset.Parts.Contains(SignalPart.Blinks))
        Next
    End Sub

    ' ---------- Update-Funktion ----------

    <Fact>
    Public Sub Versionsnummer_wird_mit_und_ohne_v_gelesen()
        Assert.Equal(New Version(1, 2, 0), Updater.ParseVersion("v1.2.0"))
        Assert.Equal(New Version(1, 0, 1), Updater.ParseVersion("1.0.1"))
        Assert.Equal(New Version(2, 0, 0), Updater.ParseVersion("V2"))
        Assert.Null(Updater.ParseVersion("abc"))
    End Sub

    <Fact>
    Public Sub Neuestes_Release_wird_aus_der_Weiterleitung_gelesen()
        Dim info = Updater.ParseLatestLocation("https://github.com/soetbaer/EEGSimulator/releases/tag/1.0.1", New Version(1, 0, 0))
        Assert.True(info.IsNewer)
        Assert.Equal("https://github.com/soetbaer/EEGSimulator/releases/download/1.0.1/NeuroSim.exe", info.DownloadUrl)
        Assert.False(Updater.ParseLatestLocation("https://github.com/soetbaer/EEGSimulator/releases/tag/v1.0.1", New Version(1, 0, 1)).IsNewer)
    End Sub

    <Fact>
    Public Sub Weiterleitung_ohne_Release_oder_auf_fremde_Adresse_wird_abgelehnt()
        Assert.Null(Updater.ParseLatestLocation("https://github.com/soetbaer/EEGSimulator/releases", New Version(1, 0, 0)))
        Assert.Null(Updater.ParseLatestLocation("https://example.com/soetbaer/EEGSimulator/releases/tag/9.0.0", New Version(1, 0, 0)))
        Assert.Null(Updater.ParseLatestLocation("https://github.com/soetbaer/EEGSimulator/releases/tag/9.0.0/../../x", New Version(1, 0, 0)))
    End Sub

    <Fact>
    Public Sub Pruefsumme_wird_aus_der_sha256_Datei_gelesen()
        Dim hash = New String("a"c, 60) & "0F9b"
        Assert.Equal(hash.ToLowerInvariant(), Updater.ParseSha256(hash & "  NeuroSim.exe" & vbLf))
        Assert.Null(Updater.ParseSha256("keine Prüfsumme"))
        Assert.Null(Updater.ParseSha256(New String("z"c, 64)))
    End Sub

End Class
