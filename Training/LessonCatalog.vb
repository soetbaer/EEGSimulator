Option Strict On

Imports NeuroSim.Simulation
Imports NeuroSim.UI

Namespace Training

    Public NotInheritable Class Lesson
        Public Property Title As String
        Public Property Blocks As New List(Of InfoBlock)
        Public Property Preset As SimSettings
        ''' <summary>Reiter der rechten Leiste, den die Lektion braucht: 0 = Einstellungen, 1 = Impedanzen, 2 = Signal.</summary>
        Public Property RightTab As Integer
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
                    InfoBlock.Para("Zum Vergleich: Eine AA-Batterie hat 1,5 Volt. Das ist rund 50.000-mal mehr als eine typische Alpha-Welle von etwa 30 µV. Deshalb sind EEG-Signale so empfindlich für Störungen."),
                    InfoBlock.Para("Jede Kurve ist ein Kanal: der Spannungsunterschied zwischen zwei Elektroden (z. B. P3-O1). Hier sehen Sie ein ruhiges Wach-EEG mit geschlossenen Augen. Über dem Hinterkopf (P3-O1, P4-O2) erkennt man den Alpha-Rhythmus mit etwa 10 Wellen pro Sekunde."),
                    InfoBlock.Bullet("Gezeigt wird ein Ausschnitt aus einer Längsreihe: die rechte Elektrodenkette oben, die linke darunter (Empfehlung der DGKN: rechts über links, vorne über hinten), dazu das EKG, das bei jeder Ableitung mitläuft. Eine vollständige Montage hat mindestens 18 Kanäle."),
                    InfoBlock.Bullet("Die kräftigen senkrechten Linien markieren je 1 Sekunde."),
                    InfoBlock.Bullet("Wie am echten Gerät gilt die EEG-Konvention 'negativ nach oben': Ist die erste Elektrode eines Kanals negativer als die zweite, schlägt die Kurve nach oben aus."),
                    InfoBlock.Bullet("Unten sehen Sie die Frequenz-Ansicht: Sie zeigt, wie viel von jeder Wellen-Geschwindigkeit im Signal steckt."),
                    InfoBlock.Tip("Frequenz = wie viele Wellen pro Sekunde. Die Einheit ist Hertz (Hz).  10 Hz = 10 Wellen pro Sekunde."),
                    InfoBlock.TryIt("Zählen Sie in P3-O1 die Wellen zwischen zwei Sekundenlinien. Klicken Sie dann links auf einen anderen Kanalnamen – die Frequenz-Ansicht zeigt jetzt diesen Kanal. Mit 'Abspielen' läuft die Schreiblinie wie am Gerät von links nach rechts und schreibt das EEG neu.")
                }})

            ' 2 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Langsam und schnell: die Frequenzbänder",
                .RightTab = 2,
                .Preset = Preset(0.3, 70, False, False, 7, 1, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Delta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein EEG ist immer eine Mischung aus Wellen unterschiedlicher Geschwindigkeit. Man teilt sie in Bänder ein:"),
                    InfoBlock.Bullet("Delta (unter 4 Hz): sehr langsam – z. B. Tiefschlaf oder an einer krankhaften Stelle (Herd)."),
                    InfoBlock.Bullet("Theta (4 bis unter 8 Hz): z. B. bei Müdigkeit; bei Kindern je nach Alter normal."),
                    InfoBlock.Bullet("Alpha (8–13 Hz): entspannt wach mit geschlossenen Augen."),
                    InfoBlock.Bullet("Beta (über 13 Hz, je nach Quelle bis 30 oder 40 Hz; meist 15–25 Hz): schnelle, niedrige Wellen, frontozentral betont; verstärkt z. B. durch Medikamente (Benzodiazepine)."),
                    InfoBlock.Tip("Bildhaft gesprochen: Langsame Wellen sind wie lange Meereswellen, schnelle wie das kleine Gekräusel auf dem Wasser. Ein Filter ist ein Sieb, das bestimmte Wellen zurückhält."),
                    InfoBlock.TryIt("Schalten Sie rechts im Reiter 'Signal' Delta, Theta, Alpha und Beta einzeln aus und wieder an. Beobachten Sie, wo in der Frequenz-Ansicht die farbigen Balken wachsen und schrumpfen.")
                }})

            ' 3 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der LowCut (FU): Sieb für langsame Wellen",
                .Preset = Preset(0, 70, False, False, 7, 0, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Sweat),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der LowCut heißt auch Hochpass, untere Grenzfrequenz (FU) oder Zeitkonstante (TC). Er lässt schnelle Wellen durch und dämpft sehr langsame."),
                    InfoBlock.Para("Wofür? Gegen langsame Störungen: Schwitzen, Bewegung der Elektrodenkabel, Atmung. Sie lassen die Grundlinie 'wandern'. Im Moment ist der LowCut AUS – sehen Sie, wie die Kurven auf und ab wandern?"),
                    InfoBlock.Tip("Zeitkonstante und Grenzfrequenz sind zwei Namen für dieselbe Einstellung:  TC 0,3 s entspricht 0,53 Hz,  TC 0,1 s entspricht 1,6 Hz.  Faustregel: KLEINERE Zeitkonstante = STÄRKERER Filter."),
                    InfoBlock.Para("Woher der Name kommt: Gibt man einen Spannungssprung (Eichsignal) auf den Verstärker, fällt die Kurve wieder zur Grundlinie zurück. Die Zeitkonstante ist die Zeit, nach der nur noch 37 % des Sprungs übrig sind."),
                    InfoBlock.Para("Ein Filter schneidet nicht wie mit der Schere ab. Genau an der Grenzfrequenz bleiben noch etwa 70 % der Wellenhöhe übrig – je langsamer die Welle, desto weniger kommt durch. Das zeigt die türkise Linie in der Frequenz-Ansicht."),
                    InfoBlock.TryIt("Stellen Sie den LowCut nacheinander auf TC 1,0 s, 0,3 s und 0,1 s. Schalten Sie dann 'Entfernten Anteil zeigen' ein: Rot ist genau das, was der Filter herausnimmt. Sie sehen: Bei TC 0,3 s bleibt ein Teil des Schwitzartefakts sichtbar – Schwitzen muss man an der Ursache bekämpfen (Raum kühlen, Haut trocknen).")
                }})

            ' 4 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Vorsicht: LowCut zu hoch",
                .Preset = Preset(0.03, 70, False, False, 7, 1, True, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Delta, SignalPart.Blinks),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein zu starker LowCut entfernt nicht nur Störungen, sondern auch echte langsame Hirnaktivität. In dieser Aufnahme steckt ein Delta-Herd links zentral (F3-C3 und C3-P3)."),
                    InfoBlock.Para("Eingestellt ist gerade TC 0,03 s (5,3 Hz). Grau unterlegt sehen Sie das ungefilterte Original."),
                    InfoBlock.Warning("Bei TC 0,03 s sind die Delta-Wellen fast verschwunden – ein krankhafter Befund kann so übersehen werden!"),
                    InfoBlock.Para("Auch das Augenblinzeln in Fp1-F3 und Fp2-F4 (großer Ausschlag nach unten) wird verformt: Es wird schmaler und bekommt danach einen Gegenschwinger nach oben."),
                    InfoBlock.Tip("Empfehlung der DGKN für die Routine-Ableitung: Hochpass höchstens 0,5 Hz, das entspricht TC 0,3 s. (Die US-Leitlinie der ACNS nennt höchstens 1 Hz.) Stärker filtern nur bewusst und vorübergehend – und im Protokoll vermerken."),
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
                    InfoBlock.Tip("Empfehlung von DGKN und ACNS: HiCut mindestens 70 Hz. Bei Muskelartefakten ist es immer besser, den Patienten zu entspannen (Mund leicht öffnen, Schultern locker), als stärker zu filtern.")
                }})

            ' 6 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Vorsicht: HiCut zu niedrig",
                .Preset = Preset(0.3, 15, False, False, 7, 1, True, False, SignalPart.Alpha, SignalPart.Spikes, SignalPart.Emg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Spikes (spitze Potentiale, Dauer 20 bis unter 70 Millisekunden) sind wichtige Hinweise, z. B. bei Epilepsie. Weil sie so steil sind, enthalten sie viele schnelle Anteile."),
                    InfoBlock.Warning("Bei einem HiCut von 15 Hz werden Spikes stumpf und kleiner – sie können dann wie harmlose Wellen aussehen."),
                    InfoBlock.Warning("Gefiltertes Muskelartefakt wird zu gleichmäßigeren Wellen, die wie Beta-Aktivität oder sogar wie steile Wellen aussehen können. Ein Filter macht aus einer Störung also nicht immer 'nichts' – manchmal macht er etwas Täuschendes daraus."),
                    InfoBlock.Para("Grau unterlegt sehen Sie wieder das Original zum Vergleich."),
                    InfoBlock.TryIt("Vergleichen Sie die Spikes in F3-C3 und C3-P3 bei 70 Hz und bei 15 Hz. Achten Sie auch auf die Phasenumkehr: in F3-C3 zeigt die Spitze nach unten, in C3-P3 nach oben – die Spitzen zeigen aufeinander zu. Die gemeinsame Elektrode C3 ist also am stärksten negativ: Dort liegt der Herd.")
                }})

            ' 7 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der 50-Hz-Filter (Netzfilter / Notch)",
                .Preset = Preset(0.3, 70, False, False, 7, 7, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum).WithImpedance("P4", 15),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Unser Stromnetz schwingt 50-mal pro Sekunde (50 Hz) und streut überall ein wenig in die Ableitung ein. Normalerweise unterdrückt der Verstärker diese Störung, weil sie an beiden Elektroden eines Kanals gleich groß ist. Liegt aber eine Elektrode schlecht an (hoher oder ungleicher Übergangswiderstand = Impedanz), gelingt das nicht mehr: Die Kurve wird dick und 'pelzig'."),
                    InfoBlock.Para("Hier hat die Elektrode P4 einen schlechten Kontakt (15 kΩ, siehe Kopfschema rechts): Betroffen sind C4-P4 und P4-O2 – also genau die beiden Kanäle, in denen P4 vorkommt. Mehr zu Impedanzen in der nächsten Lektion."),
                    InfoBlock.Para("Der 50-Hz-Filter ist ein 'Kerbfilter': Er schneidet gezielt einen schmalen Bereich um 50 Hz heraus und lässt den Rest in Ruhe. Der normale HiCut von 70 Hz hilft hier kaum – 50 Hz liegt ja noch darunter!"),
                    InfoBlock.Tip("Erst die Ursache beheben: Impedanz prüfen, Elektrode nachpräparieren, Kabelführung ändern, Störquellen suchen (Motor der Liege, Leuchtstofflampen, Geräte und Monitore in Patientennähe, defekter Schutzleiter)."),
                    InfoBlock.Warning("Der 50-Hz-Filter darf bei einer Standard-Ableitung nicht routinemäßig eingeschaltet sein: Er verdeckt Elektroden- und Kabeldefekte. Zulässig ist er allenfalls vorübergehend, z. B. auf der Intensivstation, wenn notwendige Geräte nicht abgeschaltet werden können."),
                    InfoBlock.TryIt("Schalten Sie den 50-Hz-Filter ein und aus. In der Frequenz-Ansicht sehen Sie die schmale Kerbe bei 50 Hz.")
                }})


            ' 8 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Impedanz: der Kontakt zur Kopfhaut",
                .RightTab = 1,
                .Preset = Preset(0.3, 70, False, False, 7, 2, False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum).WithImpedance("C3", 25),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Impedanz (Übergangswiderstand) beschreibt, wie gut der elektrische Kontakt zwischen Elektrode und Kopfhaut ist. Sie wird in Kiloohm (kΩ) gemessen. Niedrig heißt: guter Kontakt. Hornschicht, Hautfett, Haare unter der Elektrode oder eingetrocknete Paste erhöhen sie."),
                    InfoBlock.Para("Rechts sehen Sie jetzt im Reiter 'Impedanzen' das Kopfschema – wie bei der Impedanzmessung am Gerät. C3 hat hier 25 kΩ (rot), alle anderen Elektroden 2–3 kΩ."),
                    InfoBlock.Tip("Empfehlung: höchstens 5 kΩ, in Ausnahmefällen bis 10 kΩ, nicht unter 1 kΩ – und MÖGLICHST GLEICH, weil stark unterschiedliche Übergangswiderstände die Gleichtaktunterdrückung beeinträchtigen. Die Impedanzen werden zu Beginn und am Ende der Ableitung dokumentiert, bei Kindern auch zwischendurch wiederholt geprüft."),
                    InfoBlock.Para("Warum ist 'gleich' so wichtig? Der EEG-Verstärker misst immer den UNTERSCHIED zwischen zwei Elektroden. Die 50-Hz-Störung aus dem Stromnetz kommt an beiden Elektroden an. Ist der Kontakt beider Elektroden gleich gut, ist die Störung an beiden gleich groß – und fällt beim Abziehen heraus (Gleichtaktunterdrückung)."),
                    InfoBlock.Para("Wie bei einer Balkenwaage: Liegt auf beiden Seiten dasselbe Gewicht, bleibt sie im Gleichgewicht. Hat aber eine Elektrode 2 kΩ und die andere 25 kΩ, kommt die Störung ungleich stark an – der Unterschied bleibt als Netzbrumm in der Kurve stehen."),
                    InfoBlock.Para("Deshalb ist der Brumm genau in den zwei Kanälen zu sehen, in denen C3 vorkommt: F3-C3 und C3-P3. (Die zusätzlichen Sprünge und die unruhige Grundlinie besprechen wir in der nächsten Lektion.)"),
                    InfoBlock.TryIt("1. Klicken Sie C3 mit der rechten Maustaste herunter auf 2 kΩ: Der Brumm verschwindet – ganz ohne Filter. 2. Stellen Sie C3 wieder auf 25 kΩ und zusätzlich F3 auf 25 kΩ: In F3-C3 verschwindet der Brumm (beide gleich schlecht!), dafür brummt jetzt Fp1-F3. Gleich schlecht ist also keine Lösung – der Brumm wandert nur weiter, und die Kurven bleiben unruhig."),
                    InfoBlock.Warning("Hohe Impedanzen sind die häufigste Ursache für Netzbrumm. Der 50-Hz-Filter überdeckt das Problem nur.")
                }})

            ' 9 ---------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Schlechte Impedanz verfälscht das EEG",
                .RightTab = 1,
                .Preset = Preset(0.3, 70, False, False, 7, 5, False, False, SignalPart.Alpha, SignalPart.Beta).WithImpedance("F4", 50),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Netzbrumm ist nicht die einzige Folge. Hier ist die Netzeinstreuung ausgeschaltet, und F4 hat 50 kΩ – die Elektrode sitzt locker. Schauen Sie auf Fp2-F4 und F4-C4:"),
                    InfoBlock.Bullet("Unruhige Grundlinie: Die Kurve schwankt langsam hin und her. Studien zeigen, dass bei hoher Impedanz vor allem das langsame Rauschen (unter 5 Hz) zunimmt – besonders, wenn es warm ist und der Patient schwitzt."),
                    InfoBlock.Bullet("Elektroden-Pops: plötzliche, sehr steile Sprünge, die langsam zurücklaufen. Sie entstehen, wenn sich der Kontakt kurz ändert."),
                    InfoBlock.Warning("Verwechslungsgefahr! Ein Pop kann wie ein Spike aussehen, die langsame Unruhe wie ein Herdbefund. Das Erkennungszeichen: Die Störung betrifft nur EINE Elektrode. In den beiden Nachbarkanälen erscheint sie spiegelbildlich (F4 ist einmal zweite, einmal erste Elektrode), in allen anderen Kanälen ist nichts zu sehen. Ein echter Spike hat ein 'Feld': Er ist abgeschwächt auch in der Umgebung sichtbar (vergleichen Sie Lektion 6)."),
                    InfoBlock.Para("Was NICHT passiert: Das EEG wird durch hohe Impedanz nicht kleiner. Moderne Verstärker haben einen so hohen Eingangswiderstand (über 100 MΩ), dass selbst bei 50 kΩ weniger als 0,1 % der Spannung verloren gehen. Auch das reine Widerstandsrauschen ist winzig (unter 0,5 µV). Das 'Rauschen' kommt vom instabilen Kontakt und von eingestreuten Störungen."),
                    InfoBlock.Para("Zu niedrig ist auch falsch: Verläuft Elektrodenpaste zwischen zwei Nachbarelektroden, entsteht eine leitende Gelbrücke. Beide Elektroden messen dann fast dasselbe, und der Kanal dazwischen wird flach. Widerstände unter 100 Ω sind nicht zulässig, weil die Potentiale dann erheblich kleiner werden."),
                    InfoBlock.TryIt("1. Versuchen Sie, die Störung wegzufiltern (LowCut, HiCut, 50-Hz-Filter): Es gelingt nicht, ohne das EEG zu verändern. 2. Stellen Sie F4 auf 2 kΩ: sofort sauber. 3. Schalten Sie 'Gelbrücke F3–C3' ein und sehen Sie sich den Kanal F3-C3 an. (Die Filter finden Sie im Reiter 'Einstellungen'.)"),
                    InfoBlock.Tip("Impedanzprobleme lassen sich nicht wegfiltern. Elektrode neu setzen: Haare zur Seite, Haut mit Alkohol entfetten, abrasive Reinigungspaste sparsam verwenden, Elektrode mit ausreichend Kontaktgel oder Paste aufsetzen und fixieren – danach erneut messen und auf kleine Unterschiede zwischen den Elektroden achten.")
                }})

            ' 10 --------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Der EKG-Filter",
                .Preset = Preset(0.3, 70, False, False, 7, 3, False, False, SignalPart.Beta, SignalPart.Theta, SignalPart.Ecg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Das Herz erzeugt viel größere Spannungen als das Gehirn. Ein Teil davon erreicht die Kopfelektroden: Es entstehen kleine, regelmäßige Zacken im Takt des Herzschlags – das EKG-Artefakt. Vergleichen Sie mit dem EKG-Kanal ganz unten."),
                    InfoBlock.Para("Ein normaler Frequenzfilter hilft hier nicht: Die EKG-Zacke enthält Frequenzen, die auch im EEG vorkommen. Man würde das EEG gleich mit wegfiltern."),
                    InfoBlock.Para("Der EKG-Filter arbeitet deshalb anders – mit einer Schablone:"),
                    InfoBlock.Bullet("1. Im EKG-Kanal werden alle Herzschläge (R-Zacken) gefunden – die roten Dreiecke."),
                    InfoBlock.Bullet("2. Für jeden EEG-Kanal wird ausgerechnet, wie das Artefakt dort im Durchschnitt aussieht. Beim Mitteln vieler Herzschläge heben sich die echten Hirnwellen gegenseitig auf – übrig bleibt die typische Form des Artefakts (die Schablone)."),
                    InfoBlock.Bullet("3. Diese Schablone wird bei jedem Herzschlag abgezogen."),
                    InfoBlock.Tip("Voraussetzung ist ein sauberer EKG-Kanal! Ist das EKG gestört oder werden Herzschläge nicht erkannt, kann der Filter nicht richtig arbeiten."),
                    InfoBlock.TryIt("Schalten Sie den EKG-Filter ein und aus und schauen Sie auf P3-O1 und P4-O2. Mit 'Entfernten Anteil zeigen' sehen Sie in Rot, was abgezogen wird. Schalten Sie danach im Reiter 'Signal' Alpha dazu: Im kräftigen Alpha-Rhythmus fällt das Artefakt viel weniger auf – der Filter arbeitet trotzdem.")
                }})

            ' 11 --------------------------------------------------------------
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

            ' 12 --------------------------------------------------------------
            L.Add(New Lesson With {
                .Title = "Alles zusammen: Ihre Einstellung",
                .Preset = Preset(0, 0, False, False, 7, 0, False, False,
                                 SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Blinks, SignalPart.Sweat,
                                 SignalPart.Emg, SignalPart.Hum, SignalPart.Ecg).WithImpedance("P4", 15),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Im Alltag kommen Störungen selten allein. Hier sind fast alle typischen Artefakte gleichzeitig eingeschaltet – und alle Filter stehen auf AUS."),
                    InfoBlock.Para("Empfehlungen der DGKN für das Routine-EEG von Erwachsenen:"),
                    InfoBlock.Bullet("Impedanzen: höchstens 5 kΩ (Ausnahme bis 10 kΩ), nicht unter 1 kΩ, möglichst gleich"),
                    InfoBlock.Bullet("LowCut: höchstens 0,5 Hz, entspricht TC 0,3 s"),
                    InfoBlock.Bullet("HiCut: mindestens 70 Hz"),
                    InfoBlock.Bullet("50-Hz-Filter: nicht routinemäßig, nur vorübergehend im Ausnahmefall"),
                    InfoBlock.Bullet("EKG: wird immer mit aufgezeichnet; EKG-Filter nur bei Bedarf"),
                    InfoBlock.Bullet("Amplitude: 5–10 µV/mm, üblich 7 µV/mm (= 70 µV/cm). Bei Kindern gelten laut DGKN dieselben Geräte- und Darstellungseinstellungen wie bei Erwachsenen."),
                    InfoBlock.Para("Bei digitalen Geräten werden Filter, Verschaltung, Amplitude und Zeitachse rechnerisch angewendet und können bei der Auswertung nachträglich geändert werden. Bitte immer an die Standards Ihres Labors halten."),
                    InfoBlock.TryIt("Beheben Sie zuerst, was sich an der Ursache beheben lässt (Kopfschema im Reiter 'Impedanzen'!). Finden Sie dann die beste Einstellung, sodass Alpha gut erkennbar ist und die Störungen möglichst verschwinden. Prüfen Sie danach mit 'Rohsignal grau unterlegen', was Sie verändert haben."),
                    InfoBlock.Tip("Goldene Regel: So wenig filtern wie möglich, so viel wie nötig. Zuerst die Ursache beheben (Elektroden, Entspannung, Raum) – und jede Filteränderung dokumentieren.")
                }})

            Return L
        End Function

    End Module

End Namespace
