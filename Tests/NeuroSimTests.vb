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

    <Fact>
    Public Sub Netzbrumm_haengt_vom_Impedanz_Unterschied_ab()
        Dim humOnly = New SignalPart() {SignalPart.Hum}
        Dim nothingOn = Array.Empty(Of SignalPart)()
        Const F3C3 As Integer = 1

        ' gleiche Impedanzen: kein Brumm im Kanal
        Dim equal = Enumerable.Repeat(5.0, EegSimulator.ElectrodeNames.Length).ToArray()
        Assert.True(MaxAbsDifference(Sim.Mix(humOnly, equal)(F3C3), Sim.Mix(nothingOn, equal)(F3C3)) < 0.000001)

        ' C3 schlecht: Brumm in F3-C3
        Dim unequal = CType(equal.Clone(), Double())
        unequal(Array.IndexOf(EegSimulator.ElectrodeNames, "C3")) = 25
        Assert.True(MaxAbsDifference(Sim.Mix(humOnly, unequal)(F3C3), Sim.Mix(nothingOn, unequal)(F3C3)) > 20)
    End Sub

    <Fact>
    Public Sub EKG_Filter_findet_die_Herzschlaege()
        Dim raw = Sim.Mix(New SignalPart() {SignalPart.Ecg})
        Dim peaks = EcgArtifactRemover.DetectRPeaks(raw(EegSimulator.EcgChannel), Fs)
        Assert.InRange(peaks.Count, Sim.TrueBeats.Count - 2, Sim.TrueBeats.Count)
    End Sub

    ' ---------- Lektionen ----------

    <Fact>
    Public Sub Zwoelf_Lektionen_mit_gueltigen_Voreinstellungen()
        Dim lessons = LessonCatalog.Create()
        Assert.Equal(12, lessons.Count)
        For Each les In lessons
            Assert.Contains(les.Preset.LowCutTc, LowCutOptions)
            Assert.Contains(les.Preset.HighCutHz, HighCutOptions)
            Assert.Contains(les.Preset.Sensitivity, SensitivityOptions)
            For Each z In les.Preset.Impedances
                Assert.Contains(z, ImpedanceOptions)
            Next
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
    Public Sub Release_mit_EXE_wird_als_neuer_erkannt()
        Dim json = "{""tag_name"":""1.0.1"",""assets"":[{""name"":""NeuroSim.exe""," &
                   """browser_download_url"":""https://github.com/soetbaer/EEGSimulator/releases/download/1.0.1/NeuroSim.exe""," &
                   """digest"":""sha256:ABCD""}]}"
        Dim info = Updater.ParseRelease(json, New Version(1, 0, 0))
        Assert.True(info.IsNewer)
        Assert.Equal("abcd", info.Sha256)
        Assert.NotNull(info.DownloadUrl)
        Assert.False(Updater.ParseRelease(json, New Version(1, 0, 1)).IsNewer)
    End Sub

    <Fact>
    Public Sub Download_von_fremder_Adresse_wird_abgelehnt()
        Dim json = "{""tag_name"":""v9.0.0"",""assets"":[{""name"":""NeuroSim.exe""," &
                   """browser_download_url"":""https://example.com/NeuroSim.exe""}]}"
        Dim info = Updater.ParseRelease(json, New Version(1, 0, 0))
        Assert.True(info.IsNewer)
        Assert.Null(info.DownloadUrl)
    End Sub

End Class
