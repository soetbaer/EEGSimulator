Option Strict On

Imports System.IO
Imports System.Reflection

Namespace Services

    ''' <summary>Texte für den Info-Dialog: Quellen, Haftungsausschluss, Lizenz.</summary>
    Public Module LegalTexts

        Private ReadOnly NL As String = Environment.NewLine

        Public ReadOnly Property About As String
            Get
                Return String.Join(NL, {
                    $"{AppInfo.Name} ist eine interaktive Schulungsanwendung für medizinisch-technische Assistenzberufe in der Neurologie. Sie zeigt an simulierten EEG-Kurven, wie sich LowCut, HiCut, Amplitude, 50-Hz-Filter, EKG-Filter und Elektrodenimpedanzen auswirken.",
                    "",
                    $"Entwickler: {AppInfo.Developer}",
                    $"Copyright © {AppInfo.CopyrightYear} {AppInfo.Developer}",
                    "",
                    $"{AppInfo.Name} ist freie Software und kostenlos. Jeder darf sie nutzen, weitergeben und verändern – unter den Bedingungen der {AppInfo.LicenseName}. Der Quellcode ist öffentlich:",
                    AppInfo.RepoUrl})
            End Get
        End Property

        Public ReadOnly Property Disclaimer As String
            Get
                Return String.Join(NL, {
                    "HAFTUNGSAUSSCHLUSS UND NUTZUNGSHINWEISE",
                    "",
                    "1. Zweck",
                    $"{AppInfo.Name} dient ausschließlich der Aus-, Fort- und Weiterbildung. Alle dargestellten Kurven sind künstlich erzeugt (simuliert) und didaktisch vereinfacht. Es werden keine Patientendaten verarbeitet.",
                    "",
                    "2. Kein Medizinprodukt",
                    $"{AppInfo.Name} ist kein Medizinprodukt im Sinne der Verordnung (EU) 2017/745 (MDR) und nicht für die Diagnose, Befundung, Therapie oder Überwachung von Patientinnen und Patienten bestimmt. Die Anwendung darf nicht als Grundlage für medizinische Entscheidungen verwendet werden.",
                    "",
                    "3. Kein Ersatz für Einweisung, Leitlinien und Fachaufsicht",
                    "Die Anwendung ersetzt weder die Einweisung in das jeweilige EEG-Gerät noch die Gebrauchsanweisung des Herstellers, die geltenden Leitlinien und Empfehlungen der Fachgesellschaften, hausinterne Standards oder die Anleitung und Aufsicht durch ärztliches und fachlich verantwortliches Personal. Filterverhalten, Grenzwerte und Bezeichnungen können sich von Gerät zu Gerät unterscheiden.",
                    "",
                    "4. Inhalte ohne Gewähr",
                    "Alle Inhalte wurden nach bestem Wissen und Gewissen anhand der im Reiter »Quellen« genannten Unterlagen zusammengestellt. Eine Gewähr für Richtigkeit, Vollständigkeit und Aktualität wird nicht übernommen. Fachliche Empfehlungen ändern sich; maßgeblich sind stets die aktuellen Originalquellen. Teile der Simulation beruhen auf Modellannahmen und Beispielwerten.",
                    "",
                    "5. Gewährleistung und Haftung",
                    $"{AppInfo.Name} wird unentgeltlich und »wie besehen« zur Verfügung gestellt, ohne jede ausdrückliche oder stillschweigende Gewährleistung, insbesondere ohne Zusicherung der Eignung für einen bestimmten Zweck. Die Nutzung erfolgt auf eigene Verantwortung. Eine Haftung des Entwicklers für Schäden, die aus der Nutzung oder Nichtnutzbarkeit der Anwendung oder aus dem Vertrauen auf ihre Inhalte entstehen, ist ausgeschlossen, soweit dies gesetzlich zulässig ist. Unberührt bleibt die Haftung für Vorsatz und grobe Fahrlässigkeit, für arglistig verschwiegene Mängel sowie nach zwingenden gesetzlichen Vorschriften. Ergänzend gelten die Abschnitte 15 bis 17 der GNU General Public License, Version 3 (siehe Reiter »Lizenz«).",
                    "",
                    "6. Quellen und Namensnennungen",
                    "Die Nennung von Fachgesellschaften, Leitlinien, Büchern und Autoren dient allein der Quellenangabe. Es besteht keine Verbindung zu den genannten Organisationen, Verlagen oder Personen, und die Anwendung wurde von ihnen weder geprüft noch empfohlen. Genannte Produkt- und Firmennamen können Marken ihrer jeweiligen Inhaber sein.",
                    "",
                    "7. Update-Funktion und Datenschutz",
                    $"{AppInfo.Name} speichert und überträgt keine personenbezogenen Daten und keine Nutzungsdaten. Bei der Suche nach Updates wird eine Anfrage an die Server von GitHub (GitHub, Inc.) gesendet; dabei wird technisch bedingt die IP-Adresse des Rechners an GitHub übermittelt. Die automatische Suche beim Start lässt sich im Reiter »Über« abschalten. Die einzige lokal gespeicherte Einstellung ist diese Auswahl."})
            End Get
        End Property

        Public ReadOnly Property Sources As String
            Get
                Return String.Join(NL, {
                    "QUELLEN",
                    "",
                    "Die Inhalte und die Simulation stützen sich auf folgende Unterlagen:",
                    "",
                    "Empfehlungen und Leitlinien",
                    "• Deutsche Gesellschaft für Klinische Neurophysiologie und Funktionelle Bildgebung (DGKN): Empfehlungen für die Durchführung von EEG-Ableitungen in Klinik und Praxis bei Erwachsenen. Überarbeitete Fassung, Oktober 2024.",
                    "• DGKN: Empfehlungen für die Durchführung von EEG-Ableitungen in Klinik und Praxis bei Kindern. Überarbeitete Fassung, Oktober 2024.",
                    "• DGKN: Empfehlung zu EEG-Ableiteprogrammen. Überarbeitete Fassung, Oktober 2024.",
                    "• American Clinical Neurophysiology Society (ACNS): Guideline 1 – Minimum Technical Requirements for Performing Clinical Electroencephalography. J Clin Neurophysiol 2016; 33: 303–307.",
                    "• Nuwer MR et al.: IFCN standards for digital recording of clinical EEG. Electroencephalogr Clin Neurophysiol 1998; 106: 259–261.",
                    "• Kane N et al.: A revised glossary of terms most commonly used by clinical electroencephalographers and updated proposal for the report format of the EEG findings. Clin Neurophysiol Pract 2017; 2: 170–185.",
                    "",
                    "Lehrbücher und Übersichten",
                    "• Bischoff C, Straube A (Hrsg.): Leitlinien Klinische Neurophysiologie. Kohlhammer, Stuttgart 2014 – insbesondere Baier H: Ableitung und technische Grundlagen; Rosenow F: Das normale EEG und seine Grundrhythmusvarianten bei Erwachsenen.",
                    "• Beniczky S, Schomer DL: Electroencephalography: basic biophysical and technological aspects important for clinical applications. Epileptic Disord 2020; 22.",
                    "",
                    "Einzelarbeiten",
                    "• Kappenman ES, Luck SJ: The effects of electrode impedance on data quality and statistical significance in ERP recordings. Psychophysiology 2010; 47: 888–904.",
                    "• Ferree TC, Luu P, Russell GS, Tucker DM: Scalp electrode impedance, infection risk, and EEG data quality. Clin Neurophysiol 2001; 112: 536–544.",
                    "• Nakamura M, Shibasaki H: Elimination of EKG artifacts from EEG records: a new method of non-cephalic referential EEG recording. Electroencephalogr Clin Neurophysiol 1987; 66: 88–92.",
                    "• Huhta JC, Webster JG: 60-Hz interference in electrocardiography. IEEE Trans Biomed Eng 1973; 20: 91–101.",
                    "• Muthukumaraswamy SD: High-frequency brain activity and muscle artifacts in MEG/EEG: a review and recommendations. Front Hum Neurosci 2013; 7: 138.",
                    "• Goncharova II et al.: EMG contamination of EEG: spectral and topographical characteristics. Clin Neurophysiol 2003; 114: 1580–1593.",
                    "",
                    "Weitere Unterlagen",
                    "• Krase H: Artefakte bei evozierten Potentialen (Merkblatt; Hautvorbereitung und technische Störquellen).",
                    "",
                    "Hinweise",
                    "• Die Stärke der Netzeinstreuung je Kiloohm, die Schwellen für Kontakt-Unruhe und Elektroden-Pops sowie die Wirkung der Gelbrücke sind Modellannahmen der Simulation und keine Messwerte.",
                    "• Die Flankensteilheit der Filter ist geräteabhängig. Simuliert werden ein Hochpass 1. Ordnung (LowCut), ein Tiefpass 2. Ordnung (HiCut) und ein Kerbfilter bei 50 Hz.",
                    "• Einzelheiten zum didaktischen Konzept und zum Simulationsmodell stehen in der Datei KONZEPT.md im Quellcode-Repository."})
            End Get
        End Property

        Public ReadOnly Property LicenseIntro As String
            Get
                Return String.Join(NL, {
                    $"{AppInfo.Name} – {AppInfo.Tagline}",
                    $"Copyright © {AppInfo.CopyrightYear} {AppInfo.Developer}",
                    "",
                    "Dieses Programm ist freie Software. Sie können es unter den Bedingungen der GNU General Public License, wie von der Free Software Foundation veröffentlicht, weitergeben und/oder verändern – entweder gemäß Version 3 der Lizenz oder (nach Ihrer Wahl) jeder späteren Version.",
                    "",
                    "Dieses Programm wird in der Hoffnung verbreitet, dass es nützlich ist, aber OHNE JEDE GEWÄHRLEISTUNG – sogar ohne die stillschweigende Gewährleistung der MARKTFÄHIGKEIT oder der EIGNUNG FÜR EINEN BESTIMMTEN ZWECK. Einzelheiten stehen in der GNU General Public License.",
                    "",
                    "Rechtlich maßgeblich ist der folgende englische Originaltext der Lizenz:",
                    "",
                    "────────────────────────────────────────",
                    ""})
            End Get
        End Property

        ''' <summary>Vollständiger Lizenztext (in die EXE eingebettet).</summary>
        Public ReadOnly Property LicenseText As String
            Get
                Try
                    Using s = Assembly.GetExecutingAssembly().GetManifestResourceStream("NeuroSim.LICENSE.txt")
                        If s IsNot Nothing Then
                            Using r As New StreamReader(s)
                                Return r.ReadToEnd().Replace(vbCrLf, vbLf).Replace(vbLf, NL)
                            End Using
                        End If
                    End Using
                Catch
                End Try
                Return "Der Lizenztext liegt dem Programm als Datei LICENSE bei und ist unter https://www.gnu.org/licenses/gpl-3.0.html abrufbar."
            End Get
        End Property

    End Module

End Namespace
