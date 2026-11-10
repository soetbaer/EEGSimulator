Option Strict On

Imports EegFilterTrainer.Simulation
Imports EegFilterTrainer.UI

Namespace Training

    Public NotInheritable Class Lesson
        Public Property Title As String
        Public Property Blocks As New List(Of InfoBlock)
        Public Property Preset As SimSettings
    End Class

    ''' <summary>Die geführten Lektionen. Texte bewusst ohne Formeln und Fachchinesisch.</summary>
    Public Module LessonCatalog

        Private Function Preset(tc As Double, hiCut As Double, notch As Boolean, ecg As Boolean, sens As Double,
                                channel As Integer, showRaw As Boolean, showRemoved As Boolean,
                                ParamArray parts As SignalPart()) As SimSettings
            Return New SimSettings With {
                .LowCutTc = tc, .HighCutHz = hiCut, .Notch = notch, .EcgFilter = ecg, .Sensitivity = sens,
                .SelectedChannel = channel, .ShowRaw = showRaw, .ShowRemoved = showRemoved,
                .Parts = New HashSet(Of SignalPart)(parts)}
        End Function

        Public Function Create() As List(Of Lesson)
            Dim L As New List(Of Lesson)

            ' 1 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Willkommen: Was zeigt ein EEG?",
                .Preset = Preset(0.3, 70, False, False, 7, 3, False, False, SignalPart.Alpha, SignalPart.Beta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Das EEG misst winzige elektrische Spannungen der Hirnrinde – nur wenige millionstel Volt (Mikrovolt, µV)."),
                    InfoBlock.Para("Zum Vergleich: Eine AA-Batterie hat 1,5 Volt. Das ist rund 30.000-mal mehr als eine typische Alpha-Welle. Deshalb sind EEG-Signale so empfindlich für Störungen."),
                    InfoBlock.Para("Jede Kurve ist ein Kanal: der Spannungsunterschied zwischen zwei Elektroden (z. B. P3-O1). Hier sehen Sie ein ruhiges Wach-EEG mit geschlossenen Augen. Über dem Hinterkopf (P3-O1, P4-O2) erkennt man den Alpha-Rhythmus mit etwa 10 Wellen pro Sekunde."),
                    InfoBlock.Bullet("Die kräftigen senkrechten Linien markieren je 1 Sekunde."),
                    InfoBlock.Bullet("Unten sehen Sie die Frequenz-Ansicht: Sie zeigt, wie viel von jeder Wellen-Geschwindigkeit im Signal steckt."),
                    InfoBlock.Tip("Frequenz = wie viele Wellen pro Sekunde. Die Einheit ist Hertz (Hz).  10 Hz = 10 Wellen pro Sekunde."),
                    InfoBlock.TryIt("Zählen Sie in P3-O1 die Wellen zwischen zwei Sekundenlinien. Klicken Sie dann links auf einen anderen Kanalnamen – die Frequenz-Ansicht zeigt jetzt diesen Kanal. Mit 'Abspielen' läuft das EEG wie am Gerät.")
                }})

            ' 2 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Langsam und schnell: die Frequenzbänder",
                .Preset = Preset(0.3, 70, False, False, 7, 1, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Delta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein EEG ist immer eine Mischung aus Wellen unterschiedlicher Geschwindigkeit. Man teilt sie in Bänder ein:"),
                    InfoBlock.Bullet("Delta (0,5–4 Hz): sehr langsam – z. B. Tiefschlaf oder an einer krankhaften Stelle (Herd)."),
                    InfoBlock.Bullet("Theta (4–8 Hz): Müdigkeit, bei Kindern normal."),
                    InfoBlock.Bullet("Alpha (8–13 Hz): entspannt wach mit geschlossenen Augen."),
                    InfoBlock.Bullet("Beta (13–30 Hz): aktiv, konzentriert, auch durch Medikamente (z. B. Benzodiazepine)."),
                    InfoBlock.Tip("Bildhaft gesprochen: Langsame Wellen sind wie lange Meereswellen, schnelle wie das kleine Gekräusel auf dem Wasser. Ein Filter ist ein Sieb, das bestimmte Wellen zurückhält."),
                    InfoBlock.TryIt("Schalten Sie rechts unter 'Signal-Bausteine' Delta, Theta, Alpha und Beta einzeln aus und wieder an. Beobachten Sie, wo in der Frequenz-Ansicht die farbigen Balken wachsen und schrumpfen.")
                }})

            ' 3 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der LowCut (FU): Sieb für langsame Wellen",
                .Preset = Preset(0, 70, False, False, 7, 0, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Sweat),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der LowCut heißt auch Hochpass, untere Grenzfrequenz (FU) oder Zeitkonstante (TC). Er lässt schnelle Wellen durch und dämpft sehr langsame."),
                    InfoBlock.Para("Wofür? Gegen langsame Störungen: Schwitzen, Bewegung der Elektrodenkabel, Atmung. Sie lassen die Grundlinie 'wandern'. Im Moment ist der LowCut AUS – sehen Sie, wie die Kurven auf und ab wandern?"),
                    InfoBlock.Tip("Zeitkonstante und Grenzfrequenz sind zwei Namen für dieselbe Einstellung:  TC 0,3 s entspricht 0,53 Hz,  TC 0,1 s entspricht 1,6 Hz.  Faustregel: KLEINERE Zeitkonstante = STÄRKERER Filter."),
                    InfoBlock.Para("Ein Filter schneidet nicht wie mit der Schere ab. Genau an der Grenzfrequenz bleiben noch etwa 70 % der Wellenhöhe übrig – je langsamer die Welle, desto weniger kommt durch. Das zeigt die türkise Linie in der Frequenz-Ansicht."),
                    InfoBlock.TryIt("Stellen Sie den LowCut nacheinander auf TC 1,0 s, 0,3 s und 0,1 s. Schalten Sie dann 'Entfernten Anteil zeigen' ein: Rot ist genau das, was der Filter herausnimmt.")
                }})

            ' 4 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Vorsicht: LowCut zu hoch",
                .Preset = Preset(0.03, 70, False, False, 7, 1, True, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Delta, SignalPart.Blinks),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein zu starker LowCut entfernt nicht nur Störungen, sondern auch echte langsame Hirnaktivität. In dieser Aufnahme steckt ein Delta-Herd links zentral (F3-C3 und C3-P3)."),
                    InfoBlock.Para("Eingestellt ist gerade TC 0,03 s (5,3 Hz). Grau unterlegt sehen Sie das ungefilterte Original."),
                    InfoBlock.Warning("Bei TC 0,03 s sind die Delta-Wellen fast verschwunden – ein krankhafter Befund kann so übersehen werden!"),
                    InfoBlock.Para("Auch das Augenblinzeln in Fp1-F3 und Fp2-F4 wird verformt: Es wird schmaler und bekommt danach einen 'Unterschwinger' nach unten."),
                    InfoBlock.Tip("Häufige Standardeinstellung im Routine-EEG: TC 0,3 s (0,53 Hz). Stärker filtern nur bewusst und vorübergehend – und im Protokoll vermerken."),
                    InfoBlock.TryIt("Wechseln Sie zwischen TC 0,3 s und TC 0,03 s und vergleichen Sie Kanal F3-C3. Achten Sie in der Frequenz-Ansicht auf die blauen Delta-Balken.")
                }})

            ' 5 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der HiCut (FO): Sieb für schnelle Wellen",
                .Preset = Preset(0.3, 0, False, False, 7, 0, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Emg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der HiCut heißt auch Tiefpass oder obere Grenzfrequenz (FO). Er lässt langsame Wellen durch und dämpft schnelle."),
                    InfoBlock.Para("Typische Störung: Muskelartefakte (EMG) – z. B. durch Zähne zusammenbeißen, Stirnrunzeln oder allgemeine Anspannung. Sie erscheinen als 'Gekritzel' mit sehr vielen schnellen Zacken, hier vor allem frontal (Fp1-F3, Fp2-F4)."),
                    InfoBlock.Para("Im Moment ist der HiCut AUS."),
                    InfoBlock.TryIt("Stellen Sie den HiCut nacheinander auf 70 Hz, 35 Hz und 15 Hz. Beobachten Sie, wie das Muskelartefakt kleiner wird – und schauen Sie, was mit den roten Balken rechts in der Frequenz-Ansicht passiert."),
                    InfoBlock.Tip("Häufiger Standard: 70 Hz. Bei Muskelartefakten ist es immer besser, den Patienten zu entspannen (Mund leicht öffnen, Schultern locker), als stärker zu filtern.")
                }})

            ' 6 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Vorsicht: HiCut zu niedrig",
                .Preset = Preset(0.3, 15, False, False, 7, 1, True, False, SignalPart.Alpha, SignalPart.Spikes, SignalPart.Emg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Spikes (spitze Potentiale) sind wichtige Hinweise, z. B. bei Epilepsie. Weil sie so steil sind, enthalten sie viele schnelle Anteile."),
                    InfoBlock.Warning("Bei einem HiCut von 15 Hz werden Spikes stumpf und kleiner – sie können dann wie harmlose Wellen aussehen."),
                    InfoBlock.Warning("Gefiltertes Muskelartefakt wird zu gleichmäßigeren Wellen, die wie Beta-Aktivität aussehen können. Ein Filter macht aus einer Störung also nicht immer 'nichts' – manchmal macht er etwas Täuschendes daraus."),
                    InfoBlock.Para("Grau unterlegt sehen Sie wieder das Original zum Vergleich."),
                    InfoBlock.TryIt("Vergleichen Sie die Spikes in F3-C3 und C3-P3 bei 70 Hz und bei 15 Hz. Achten Sie auch auf die Phasenumkehr: in F3-C3 nach oben, in C3-P3 nach unten – der Herd liegt bei C3.")
                }})

            ' 7 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der 50-Hz-Filter (Netzfilter / Notch)",
                .Preset = Preset(0.3, 70, False, False, 7, 7, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Unser Stromnetz schwingt 50-mal pro Sekunde (50 Hz). Liegt eine Elektrode schlecht an (hoher Übergangswiderstand = Impedanz), fängt sie diese Schwingung wie eine Antenne ein. Die Kurve wird dick und 'pelzig'."),
                    InfoBlock.Para("Hier hat die Elektrode P4 einen schlechten Kontakt: Betroffen sind C4-P4 und P4-O2 – also genau die beiden Kanäle, in denen P4 vorkommt."),
                    InfoBlock.Para("Der 50-Hz-Filter ist ein 'Kerbfilter': Er schneidet gezielt einen schmalen Bereich um 50 Hz heraus und lässt den Rest in Ruhe. Der normale HiCut von 70 Hz hilft hier kaum – 50 Hz liegt ja noch darunter!"),
                    InfoBlock.Tip("Erst die Ursache beheben: Impedanz prüfen, Elektrode nachpräparieren, Kabel und Geräte in der Nähe kontrollieren. Der Filter ist nur die zweitbeste Lösung."),
                    InfoBlock.TryIt("Schalten Sie den 50-Hz-Filter ein und aus. In der Frequenz-Ansicht sehen Sie die schmale Kerbe bei 50 Hz.")
                }})

            ' 8 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der EKG-Filter",
                .Preset = Preset(0.3, 70, False, False, 7, 3, False, False, SignalPart.Beta, SignalPart.Theta, SignalPart.Ecg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Das Herz erzeugt viel größere Spannungen als das Gehirn. Ein Teil davon erreicht die Kopfelektroden: Es entstehen kleine, regelmäßige Zacken im Takt des Herzschlags – das EKG-Artefakt. Vergleichen Sie mit dem EKG-Kanal ganz unten."),
                    InfoBlock.Para("Ein normaler Frequenzfilter hilft hier nicht: Die EKG-Zacke enthält dieselben Frequenzen wie das EEG. Man würde das EEG gleich mit wegfiltern."),
                    InfoBlock.Para("Der EKG-Filter arbeitet deshalb anders – mit einer Schablone:"),
                    InfoBlock.Bullet("1. Im EKG-Kanal werden alle Herzschläge (R-Zacken) gefunden – die roten Dreiecke."),
                    InfoBlock.Bullet("2. Für jeden EEG-Kanal wird ausgerechnet, wie das Artefakt dort im Durchschnitt aussieht. Beim Mitteln vieler Herzschläge heben sich die echten Hirnwellen gegenseitig auf – übrig bleibt die typische Form des Artefakts (die Schablone)."),
                    InfoBlock.Bullet("3. Diese Schablone wird bei jedem Herzschlag abgezogen."),
                    InfoBlock.Tip("Voraussetzung ist ein sauberer EKG-Kanal! Ist das EKG gestört oder werden Herzschläge nicht erkannt, kann der Filter nicht richtig arbeiten."),
                    InfoBlock.TryIt("Schalten Sie den EKG-Filter ein und aus und schauen Sie auf P3-O1 und P4-O2. Mit 'Entfernten Anteil zeigen' sehen Sie in Rot, was abgezogen wird. Schalten Sie danach Alpha dazu: Im kräftigen Alpha-Rhythmus fällt das Artefakt viel weniger auf – der Filter arbeitet trotzdem.")
                }})

            ' 9 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Die Amplitude (Empfindlichkeit)",
                .Preset = Preset(0.3, 70, False, False, 7, 3, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Amplitude (Empfindlichkeit) bestimmt, wie groß die Kurven gezeichnet werden. Sie wird in µV/mm angegeben: wie viele Mikrovolt nötig sind, damit die Kurve 1 mm ausschlägt."),
                    InfoBlock.Tip("Umgekehrte Logik: KLEINERE Zahl = GRÖSSERE Kurven.  Bei 7 µV/mm ist eine 70-µV-Welle 1 cm hoch, bei 3 µV/mm schon etwa 2,3 cm."),
                    InfoBlock.Para("Wichtig: Die Amplitude ist KEIN Filter. Das Signal selbst bleibt gleich, nur die Darstellung ändert sich. Deshalb verändert sich die Frequenz-Ansicht nicht."),
                    InfoBlock.Warning("Zu klein dargestellt (große Zahl): niedrige Aktivität wird übersehen. Zu groß dargestellt: Kurven überlappen und werden unlesbar."),
                    InfoBlock.Para("Hinweis: Wie groß ein Millimeter am Bildschirm wirklich ist, hängt von den Monitor-Einstellungen ab."),
                    InfoBlock.TryIt("Ändern Sie die Amplitude mit den Tasten – und + und beobachten Sie den Eichbalken unten rechts im EEG.")
                }})

            ' 10 --------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Alles zusammen: Ihre Einstellung",
                .Preset = Preset(0, 0, False, False, 7, 0, False, False,
                                 SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Blinks, SignalPart.Sweat,
                                 SignalPart.Emg, SignalPart.Hum, SignalPart.Ecg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Im Alltag kommen Störungen selten allein. Hier sind fast alle typischen Artefakte gleichzeitig eingeschaltet – und alle Filter stehen auf AUS."),
                    InfoBlock.Para("Häufig verwendete Einstellungen beim Routine-EEG von Erwachsenen:"),
                    InfoBlock.Bullet("LowCut: TC 0,3 s (0,53 Hz)"),
                    InfoBlock.Bullet("HiCut: 70 Hz"),
                    InfoBlock.Bullet("50-Hz-Filter und EKG-Filter: nur bei Bedarf"),
                    InfoBlock.Bullet("Amplitude: 7 µV/mm (bei Kindern oft 10–20 µV/mm)"),
                    InfoBlock.Para("Bitte immer an die Standards Ihres Labors halten."),
                    InfoBlock.TryIt("Finden Sie die beste Einstellung, sodass Alpha gut erkennbar ist und die Störungen möglichst verschwinden. Prüfen Sie danach mit 'Rohsignal grau unterlegen', was Sie verändert haben."),
                    InfoBlock.Tip("Goldene Regel: So wenig filtern wie möglich, so viel wie nötig. Zuerst die Ursache beheben (Elektroden, Entspannung, Raum) – und jede Filteränderung dokumentieren.")
                }})

            Return L
        End Function

    End Module

End Namespace
