Option Strict On

Imports NeuroSim.Simulation
Imports NeuroSim.UI

Namespace Training

    Public NotInheritable Class Lesson
        ''' <summary>Kapitel, zu dem die Lektion gehört (für die Überschrift).</summary>
        Public Property Chapter As String = ""
        Public Property Title As String
        Public Property Blocks As New List(Of InfoBlock)
        Public Property Preset As SimSettings
        ''' <summary>Reiter der rechten Leiste, den die Lektion braucht (TabFilter, TabMontage, TabImpedance, TabSignal).</summary>
        Public Property RightTab As Integer

        Public Const TabFilter As Integer = 0
        Public Const TabMontage As Integer = 1
        Public Const TabImpedance As Integer = 2
        Public Const TabSignal As Integer = 3
    End Class

    ''' <summary>
    ''' Die geführten Lektionen, in der Reihenfolge des Signalwegs:
    ''' Grundlagen, Verstärker und Impedanz, Verschaltungen, Filter, Darstellung und Praxis.
    ''' Texte bewusst ohne Formeln und Fachchinesisch.
    ''' </summary>
    Public Module LessonCatalog

        Public Const ChapterBasics As String = "Grundlagen"
        Public Const ChapterAmplifier As String = "Verstärker und Impedanz"
        Public Const ChapterMontage As String = "Verschaltungen"
        Public Const ChapterFilter As String = "Filter"
        Public Const ChapterPractice As String = "Darstellung und Praxis"

        Private Function Preset(tc As Double, hiCut As Double, notch As Boolean, ecg As Boolean, sens As Double,
                                montageId As String, channelLabel As String, showRaw As Boolean, showRemoved As Boolean,
                                ParamArray parts As SignalPart()) As SimSettings
            Dim s As New SimSettings With {
                .LowCutTc = tc, .HighCutHz = hiCut, .Notch = notch, .EcgFilter = ecg, .Sensitivity = sens,
                .ShowRaw = showRaw, .ShowRemoved = showRemoved,
                .Parts = New HashSet(Of SignalPart)(parts)}
            Return s.WithMontage(montageId, channelLabel)
        End Function

        Public Function Create() As List(Of Lesson)
            Dim L As New List(Of Lesson)

            L.Add(New Lesson With {
                .Chapter = ChapterBasics,
                .Title = "Willkommen: Was zeigt ein EEG?",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Das EEG misst winzige elektrische Spannungen der Hirnrinde – nur wenige millionstel Volt (Mikrovolt, µV)."),
                    InfoBlock.Para("Zum Vergleich: Eine AA-Batterie hat 1,5 Volt. Das ist rund 50.000-mal mehr als eine typische Alpha-Welle von etwa 30 µV. Deshalb sind EEG-Signale so empfindlich für Störungen."),
                    InfoBlock.Para("Jede Kurve ist ein Kanal: der Spannungsunterschied zwischen zwei Elektroden (z. B. P3-O1). Hier sehen Sie ein ruhiges Wach-EEG mit geschlossenen Augen. Über dem Hinterkopf (P3-O1, P4-O2) erkennt man den Alpha-Rhythmus mit etwa 10 Wellen pro Sekunde."),
                    InfoBlock.Bullet("Gezeigt wird ein Ausschnitt aus einer Längsreihe: die rechte Elektrodenkette oben, die linke darunter (Empfehlung der DGKN: rechts über links, vorne über hinten), dazu das EKG, das bei jeder Ableitung mitläuft. Eine vollständige Montage hat mindestens 18 Kanäle."),
                    InfoBlock.Bullet("Die kräftigen senkrechten Linien markieren je 1 Sekunde."),
                    InfoBlock.Bullet("Wie am echten Gerät gilt die EEG-Konvention »negativ nach oben«: Ist die erste Elektrode eines Kanals negativer als die zweite, schlägt die Kurve nach oben aus."),
                    InfoBlock.Bullet("Unten sehen Sie die Frequenz-Ansicht: Sie zeigt, wie viel von jeder Wellen-Geschwindigkeit im Signal steckt."),
                    InfoBlock.Tip("Frequenz = wie viele Wellen pro Sekunde. Die Einheit ist Hertz (Hz).  10 Hz = 10 Wellen pro Sekunde."),
                    InfoBlock.TryIt("Zählen Sie in P3-O1 die Wellen zwischen zwei Sekundenlinien. Klicken Sie dann links auf einen anderen Kanalnamen – die Frequenz-Ansicht zeigt jetzt diesen Kanal. Mit »Abspielen« läuft die Schreiblinie wie am Gerät von links nach rechts und schreibt das EEG neu.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterBasics,
                .Title = "Langsam und schnell: die Frequenzbänder",
                .RightTab = Lesson.TabSignal,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "F3-C3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Delta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein EEG ist immer eine Mischung aus Wellen unterschiedlicher Geschwindigkeit. Man teilt sie in Bänder ein:"),
                    InfoBlock.Bullet("Delta (unter 4 Hz): sehr langsam – z. B. Tiefschlaf oder an einer krankhaften Stelle (Herd)."),
                    InfoBlock.Bullet("Theta (4 bis unter 8 Hz): z. B. bei Müdigkeit; bei Kindern je nach Alter normal."),
                    InfoBlock.Bullet("Alpha (8–13 Hz): entspannt wach mit geschlossenen Augen."),
                    InfoBlock.Bullet("Beta (über 13 Hz, je nach Quelle bis 30 oder 40 Hz; meist 15–25 Hz): schnelle, niedrige Wellen, frontozentral betont; verstärkt z. B. durch Medikamente (Benzodiazepine)."),
                    InfoBlock.Tip("Bildhaft gesprochen: Langsame Wellen sind wie lange Meereswellen, schnelle wie das kleine Gekräusel auf dem Wasser. Ein Filter ist ein Sieb, das bestimmte Wellen zurückhält."),
                    InfoBlock.TryIt("Schalten Sie rechts im Reiter »Signal« Delta, Theta, Alpha und Beta einzeln aus und wieder an. Beobachten Sie, wo in der Frequenz-Ansicht die farbigen Balken wachsen und schrumpfen.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterAmplifier,
                .Title = "Die Headbox: vom Kopf in den Computer",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Amplifier, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Headbox (Elektrodenanschlussbox) ist das Kästchen, in das die Elektrodenkabel gesteckt werden. Zusammen mit dem Verstärker und dem Analog-Digital-Wandler – je nach Gerät im selben Gehäuse oder getrennt davon – macht sie aus der winzigen Spannung am Kopf ein Signal, das der Computer anzeigen und speichern kann."),
                    InfoBlock.Para("Das Wichtigste zuerst: Eine Spannung kann man nie an **einem** Punkt messen, sondern immer nur **zwischen** zwei Punkten – so wie man eine Höhe immer von irgendwo aus misst. Jeder Verstärkerkanal hat deshalb zwei Eingänge und zeigt ihren Unterschied: Eingang 1 minus Eingang 2. Darum heißt er Differenzverstärker."),
                    InfoBlock.Para("Rechts ist ein einziger Kanal in seine Teile zerlegt: oben die Spannung an P3 (Eingang 1), darunter die an O1 (Eingang 2) – jeweils gegen die Erdelektrode gemessen und 20-fach verkleinert gezeichnet. Darunter steht, was der Verstärker daraus macht: P3 minus O1. Ein echtes Gerät zeigt die einzelnen Eingänge nicht an; hier dienen sie nur zur Erklärung."),
                    InfoBlock.Bullet("Beide Eingänge wandern gemeinsam langsam auf und ab. Das ist keine Hirnaktivität, sondern das Potential des ganzen Körpers – es ist an allen Elektroden gleich."),
                    InfoBlock.Bullet("Am Ausgang ist davon nichts zu sehen: Was an beiden Eingängen gleich ist, fällt beim Abziehen heraus. Übrig bleibt, was sich zwischen P3 und O1 unterscheidet – hier vor allem der Alpha-Rhythmus."),
                    InfoBlock.Tip("An der Headbox gibt es drei Arten von Anschlüssen: die Messelektroden (Fp1 bis O2 und die Ohren), die Referenz für die Aufzeichnung und die Erde (Neutralelektrode). Übliche Verstärker messen jeden Eingang zunächst gegen die Erde und bilden dann den Unterschied. Die Erde selbst erscheint deshalb in keinem Kanal."),
                    InfoBlock.Para("Digitale Geräte zeichnen alle Elektroden des 10/20-Systems gleichzeitig auf – ohne zusätzliche Filter. Jede Elektrode wird dabei gegen eine Referenzelektrode abgeleitet und digitalisiert. Damit sich später jede Verschaltung berechnen lässt, liegt diese Referenz außerhalb des 10/20-Systems, typischerweise bei CPz zwischen Cz und Pz. Welche Elektrode in der Anzeige mit welcher verglichen wird (die Verschaltung) und wie gefiltert wird, rechnet der Computer erst für die Darstellung aus. Deshalb lässt sich beides nachträglich ändern."),
                    InfoBlock.TryIt("Schalten Sie im Reiter »Signal« Alpha aus und wieder an. Am Ausgang (P3-O1) verschwindet und erscheint der Rhythmus – die beiden Eingänge wandern unverändert gemeinsam weiter.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterAmplifier,
                .Title = "Der Differenzverstärker: Störungen verschwinden",
                .RightTab = Lesson.TabSignal,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Amplifier, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der Körper wirkt wie eine Antenne: Stromleitungen, Lampen, Bildschirme und andere Geräte in der Umgebung erzeugen auf ihm eine Störspannung im Takt des Stromnetzes, also 50-mal pro Sekunde. Sie ist an den Eingängen riesig: Bei geerdetem Körper liegt sie im Millivolt-Bereich – in diesem Beispiel bei 1000 µV, rund 30-mal größer als eine Alpha-Welle."),
                    InfoBlock.Para("Sehen Sie sich die beiden Eingänge an: ein dickes 50-Hz-Band, vom EEG ist nichts zu erkennen. Trotzdem ist der Ausgang darunter sauber."),
                    InfoBlock.Para("Der Grund: Die Störung kommt an beiden Elektroden gleich groß und gleichzeitig an. Man nennt das Gleichtakt. Der Verstärker bildet den Unterschied – und Gleiches minus Gleiches ergibt null. Das EEG ist dagegen an P3 und O1 verschieden und bleibt übrig."),
                    InfoBlock.Tip("Wie bei einer Balkenwaage: Liegt auf beiden Seiten dasselbe Gewicht, bleibt sie im Gleichgewicht – egal wie schwer es ist. Sie zeigt nur den Unterschied."),
                    InfoBlock.Para("Wie gut ein Verstärker Gleiches unterdrückt, heißt Gleichtaktunterdrückung. Der Verstärker soll mindestens 80 dB erreichen – das heißt, ein Gleichtaktsignal wird mindestens 10.000-fach abgeschwächt. Aus 1000 µV Störung werden dann höchstens 0,1 µV. Moderne Geräte liegen bei mindestens 100.000 zu 1 (100 dB)."),
                    InfoBlock.Bullet("Was der Differenzverstärker **nicht** kann: Eine Störung, die nur **eine** Elektrode trifft (eine Elektrode bewegt sich, schlechter Kontakt, Elektroden-Pop), ist kein Gleichtakt. Sie bleibt voll erhalten. Auch Störungen aus dem Körper selbst wie Muskelaktivität oder Augenblinzeln fallen nicht heraus."),
                    InfoBlock.Bullet("Die Kehrseite: Auch echte Hirnaktivität, die an beiden Elektroden gleich ist, fällt heraus. Das wird im Kapitel »Verschaltungen« wichtig."),
                    InfoBlock.TryIt("Schalten Sie im Reiter »Signal« die 50-Hz-Einstreuung aus und wieder an. An den Eingängen ändert sich das Bild völlig, am Ausgang fast nichts. In der nächsten Lektion sehen Sie, was passiert, wenn eine der beiden Elektroden schlecht sitzt.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterAmplifier,
                .Title = "Warum niedrige und gleiche Impedanzen zählen",
                .RightTab = Lesson.TabImpedance,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Amplifier, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum).WithImpedance("O1", 25),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Jetzt sitzt O1 schlecht: 25 kΩ, während P3 mit 2 kΩ gut anliegt (siehe Kopfschema rechts). Die Störung in der Umgebung ist dieselbe wie in der vorigen Lektion – trotzdem erscheint am Ausgang jetzt Netzbrumm."),
                    InfoBlock.Para("Warum? Die Störung fällt nur heraus, wenn beide Eingänge gleich behandelt werden. Störströme aus der Umgebung erzeugen an jeder Elektrode einen Spannungsabfall – je höher ihre Impedanz, desto größer. Sind beide Impedanzen gleich, ist auch dieser Spannungsabfall gleich und fällt heraus. Sind sie ungleich, bleibt der Unterschied stehen."),
                    InfoBlock.Warning("Aus einer gemeinsamen Störung ist damit ein Unterschied zwischen den Eingängen geworden – und den behandelt der Verstärker wie EEG: Er wird genauso verstärkt, gefiltert und aufgezeichnet. Der Verstärker kann nicht wissen, was vom Gehirn kommt und was von der Steckdose."),
                    InfoBlock.Bullet("Zahlenbeispiel der Simulation: je kΩ entstehen etwa 2 µV Störspannung (ein Beispielwert, in Wirklichkeit hängt er stark von der Umgebung ab). 2 kΩ gegen 25 kΩ sind 23 kΩ Unterschied, also rund 46 µV Brumm – mehr als das Alpha."),
                    InfoBlock.Bullet("Wie gut ein Verstärker beide Eingänge gleich behandeln kann, hängt vom Verhältnis zwischen Elektrodenimpedanz und Eingangswiderstand des Verstärkers ab. Der Eingangswiderstand ist fest und sehr groß (Megaohm-Bereich oder höher). Die Elektrodenimpedanz schwankt dagegen von Person zu Person, von Elektrode zu Elektrode und mit der Zeit. Je höher die Impedanzen insgesamt sind, desto größer fallen auch die zufälligen Unterschiede zwischen den Elektroden aus – und desto schlechter wird die Gleichtaktunterdrückung."),
                    InfoBlock.Bullet("Auch die Erdelektrode zählt: Ist der Körper über sie mit dem Verstärker verbunden, liegt die Gleichtaktstörung im Millivolt-Bereich. Ohne diese Verbindung kann sie rund tausendmal größer sein."),
                    InfoBlock.Tip("Merke: niedrig **und** gleich. Gleich, weil nur Gleiches herausfällt. Niedrig, weil bei hohen Impedanzen auch die Unterschiede zwischen den Elektroden größer werden. Die DGKN verlangt 1 bis 10 kΩ und möglichst gleichmäßige Werte."),
                    InfoBlock.TryIt("1. Klicken Sie O1 mit der rechten Maustaste herunter auf 3 kΩ: Der Brumm am Ausgang verschwindet, obwohl die Eingänge weiter voller Störung sind. 2. Stellen Sie O1 wieder auf 25 kΩ und zusätzlich P3 auf 25 kΩ: Der Brumm verschwindet ebenfalls – beide sind jetzt gleich schlecht. Die Kurve bleibt aber unruhig und springt: Dazu mehr in den nächsten beiden Lektionen.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterAmplifier,
                .Title = "Impedanz: der Kontakt zur Kopfhaut",
                .RightTab = Lesson.TabImpedance,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "C3-P3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum).WithImpedance("C3", 25),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Impedanz (Übergangswiderstand) beschreibt, wie gut der elektrische Kontakt zwischen Elektrode und Kopfhaut ist. Sie wird in Kiloohm (kΩ) gemessen. Niedrig heißt: guter Kontakt. Hornschicht, Hautfett, Haare unter der Elektrode oder eingetrocknete Paste erhöhen sie."),
                    InfoBlock.Para("Rechts sehen Sie jetzt im Reiter »Impedanzen« das Kopfschema – wie bei der Impedanzmessung am Gerät. C3 hat hier 25 kΩ (rot), alle anderen Elektroden 2–3 kΩ."),
                    InfoBlock.Tip("Empfehlung: unter 10 kΩ und **möglichst gleichmäßig** (nicht unter 1 kΩ), weil stark unterschiedliche Übergangswiderstände die Gleichtaktunterdrückung beeinträchtigen."),
                    InfoBlock.Para("Der Impedanztest gehört zur Aufzeichnung: Er sollte vor und nach der Ableitung mit aufgezeichnet werden, bei Artefakten auch zwischendurch. Die meisten Geräte speichern die Messung automatisch in der Aufnahme. Bei Kindern werden die Impedanzen zwischendurch wiederholt geprüft."),
                    InfoBlock.Para("Warum »gleich« so wichtig ist, haben Sie in der vorigen Lektion am Verstärker gesehen: Nur was an beiden Eingängen gleich ankommt, fällt beim Abziehen heraus. Hat eine Elektrode 2 kΩ und die andere 25 kΩ, bleibt der Unterschied als Netzbrumm in der Kurve stehen."),
                    InfoBlock.Para("Deshalb ist der Brumm genau in den zwei Kanälen zu sehen, in denen C3 vorkommt: F3-C3 und C3-P3. (Die zusätzlichen Sprünge und die unruhige Grundlinie besprechen wir in der nächsten Lektion.)"),
                    InfoBlock.TryIt("1. Klicken Sie C3 mit der rechten Maustaste herunter auf 2 kΩ: Der Brumm verschwindet – ganz ohne Filter. 2. Stellen Sie C3 wieder auf 25 kΩ und zusätzlich F3 auf 25 kΩ: In F3-C3 verschwindet der Brumm (beide gleich schlecht!), dafür brummt jetzt Fp1-F3. Gleich schlecht ist also keine Lösung – der Brumm wandert nur weiter, und die Kurven bleiben unruhig."),
                    InfoBlock.Warning("Hohe Impedanzen sind die häufigste Ursache für Netzbrumm. Der 50-Hz-Filter überdeckt das Problem nur.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterAmplifier,
                .Title = "Schlechte Impedanz verfälscht das EEG",
                .RightTab = Lesson.TabImpedance,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "F4-C4", False, False, SignalPart.Alpha, SignalPart.Beta).WithImpedance("F4", 50),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Netzbrumm ist nicht die einzige Folge. Hier ist die Netzeinstreuung ausgeschaltet, und F4 hat 50 kΩ – die Elektrode sitzt locker. Schauen Sie auf Fp2-F4 und F4-C4:"),
                    InfoBlock.Bullet("Unruhige Grundlinie: Die Kurve schwankt langsam hin und her. Studien zeigen, dass bei hoher Impedanz vor allem das langsame Rauschen (unter 5 Hz) zunimmt – besonders, wenn es warm ist und der Patient schwitzt."),
                    InfoBlock.Bullet("Elektroden-Pops: plötzliche, sehr steile Sprünge, die langsam zurücklaufen. Sie entstehen, wenn sich der Kontakt kurz ändert."),
                    InfoBlock.Warning("Verwechslungsgefahr! Ein Pop kann wie ein Spike aussehen, die langsame Unruhe wie ein Herdbefund. Das Erkennungszeichen: Die Störung betrifft nur **eine** Elektrode. In den beiden Nachbarkanälen erscheint sie spiegelbildlich (F4 ist einmal zweite, einmal erste Elektrode), in allen anderen Kanälen ist nichts zu sehen. Ein echter Spike hat ein »Feld«: Er ist abgeschwächt auch in der Umgebung sichtbar (mehr dazu im Kapitel »Verschaltungen«)."),
                    InfoBlock.Para("Was **nicht** passiert: Das EEG wird durch hohe Impedanz nicht kleiner. Moderne Verstärker haben einen so hohen Eingangswiderstand (über 100 MΩ), dass selbst bei 50 kΩ weniger als 0,1 % der Spannung verloren gehen. Auch das reine Widerstandsrauschen ist winzig (unter 0,5 µV). Das »Rauschen« kommt vom instabilen Kontakt und von eingestreuten Störungen."),
                    InfoBlock.Para("Zu niedrig ist auch falsch: Verläuft Elektrodenpaste zwischen zwei Nachbarelektroden, entsteht eine leitende Gelbrücke. Beide Elektroden messen dann fast dasselbe, und der Kanal dazwischen wird flach. Widerstände unter 100 Ω sind nicht zulässig, weil die Potentiale dann erheblich kleiner werden."),
                    InfoBlock.TryIt("1. Versuchen Sie, die Störung wegzufiltern (LowCut, HiCut, 50-Hz-Filter): Es gelingt nicht, ohne das EEG zu verändern. 2. Stellen Sie F4 auf 2 kΩ: sofort sauber. 3. Schalten Sie »Gelbrücke F3–C3« ein und sehen Sie sich den Kanal F3-C3 an. (Die Filter finden Sie im Reiter »Filter«.)"),
                    InfoBlock.Tip("Impedanzprobleme lassen sich nicht wegfiltern. Elektrode neu setzen: Haare zur Seite, Haut mit Alkohol entfetten, abrasive Reinigungspaste sparsam verwenden, Elektrode mit ausreichend Kontaktgel oder Paste aufsetzen und fixieren – danach erneut messen und auf kleine Unterschiede zwischen den Elektroden achten.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Was ist eine Verschaltung?",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Longitudinal, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Headbox misst jede Elektrode einzeln. Eine Verschaltung (auch Montage oder Ableiteprogramm genannt) legt fest, welche Elektroden in jedem Kanal miteinander verglichen werden – und in welcher Reihenfolge die Kanäle untereinander stehen."),
                    InfoBlock.Para("Das EEG im Kopf ist dabei immer dasselbe. Die Verschaltung ist nur der Blickwinkel – wie bei einem Berg, den man von verschiedenen Seiten fotografiert: Jede Ansicht zeigt etwas anderes deutlich und verdeckt etwas anderes."),
                    InfoBlock.Para("Rechts im Reiter »Verschaltung« sehen Sie das Kopfschema mit den 21 Elektroden des 10/20-Systems. Die Linien zeigen, welche Elektroden verbunden sind. Klicken Sie links auf einen Kanalnamen: Eingang 1 wird blau markiert, Eingang 2 orange."),
                    InfoBlock.Bullet("Die Namen: Fp = frontopolar, F = frontal, C = zentral, P = parietal, O = okzipital, T = temporal, A = Ohr. Ungerade Zahlen liegen links, gerade rechts, z in der Mitte. T3/T5 und T4/T6 heißen in der neueren Bezeichnung T7/P7 und T8/P8."),
                    InfoBlock.Bullet("Es gibt zwei Familien: bipolare Reihen (Nachbar gegen Nachbar, z. B. Längsreihe und Querreihe) und Referenzableitungen (alle Elektroden gegen denselben Bezug, z. B. Ohr, Cz oder Durchschnitt)."),
                    InfoBlock.Bullet("Anordnung nach DGKN: rechts über links, vorne über hinten, mindestens 18 Kanäle. Das EKG läuft immer mit."),
                    InfoBlock.Tip("Jeder Befund muss in mindestens zwei Verschaltungen angesehen werden – einer bipolaren und einer Referenzableitung (DGKN). Erst der Wechsel des Blickwinkels zeigt, was echt ist."),
                    InfoBlock.Para("Weil digitale Geräte alle Elektroden gleichzeitig aufzeichnen, lässt sich die Verschaltung jederzeit wechseln, auch nachträglich bei der Auswertung. Laut DGKN müssen die Programme auch während der Ableitung gewechselt werden, damit alle Elektroden beurteilt werden können."),
                    InfoBlock.TryIt("Wählen Sie im Reiter »Verschaltung« nacheinander die anderen Verschaltungen. Beobachten Sie den Alpha-Rhythmus: Er sitzt immer hinten am Kopf – sieht aber jedes Mal anders aus.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Die Längsreihe",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Longitudinal, "F3-C3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("In der Längsreihe laufen Ketten von vorne nach hinten, und jede Elektrode wird mit der nächsten dahinter verglichen: Fp1-F3, F3-C3, C3-P3, P3-O1. So entstehen auf jeder Seite eine äußere (temporale) und eine innere (parasagittale) Kette, dazu die Mittellinie."),
                    InfoBlock.Para("Wozu? Benachbarte Elektroden sehen fast dieselbe weit ausgebreitete Aktivität und dieselben Störungen – das fällt beim Abziehen heraus. Übrig bleibt, was sich auf kurzer Strecke ändert. Eine bipolare Reihe zeigt also das Gefälle der Spannung und hebt kleine, umschriebene Befunde hervor."),
                    InfoBlock.Para("Das wichtigste Zeichen ist die Phasenumkehr. Liegt ein Herd unter einer Elektrode mitten in der Kette, ist sie im einen Kanal Eingang 2 und im nächsten Eingang 1. Die Ausschläge zeigen dann in entgegengesetzte Richtungen. Hier sitzen Spikes unter C3: In F3-C3 zeigt die Spitze nach unten, in C3-P3 nach oben – die Spitzen zeigen aufeinander zu, C3 ist also am stärksten negativ."),
                    InfoBlock.Warning("Grenzen: 1. Die Höhe des Ausschlags sagt nichts über die Größe der Spannung, nur über den Unterschied zwischen zwei Elektroden. Ist die Aktivität an beiden gleich groß, bleibt der Kanal flach. 2. Am Ende einer Kette (Fp1, O1) gibt es keine Phasenumkehr, ein Herd dort kann übersehen werden. 3. Weit ausgebreitete Aktivität wird unterschätzt."),
                    InfoBlock.Tip("Für den Seitenvergleich immer die gleichen Kanäle rechts und links ansehen, zum Beispiel F4-C4 und F3-C3."),
                    InfoBlock.TryIt("Suchen Sie die Phasenumkehr in der inneren Kette links (F3-C3 und C3-P3). Sehen Sie dann in die äußere Kette (F7-T3 und T3-T5): Dort gibt es eine kleinere Phasenumkehr bei T3 – das Feld des Herdes reicht bis dorthin. Rechts ist nichts zu sehen.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Die Querreihe",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Transverse, "Cz-C3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("In der Querreihe laufen die Ketten quer über den Kopf von rechts nach links, zum Beispiel A2-T4, T4-C4, C4-Cz, Cz-C3, C3-T3, T3-A1. Es gibt eine frontale, eine zentrale und eine parietale Reihe, dazu vorne und hinten je einen Bogen."),
                    InfoBlock.Para("Wozu, wenn es die Längsreihe gibt? Eine Kette ist nur in ihrer eigenen Richtung empfindlich. Die Längsreihe findet einen Herd auf der Linie vorne–hinten, die Querreihe auf der Linie rechts–links. Erst beide zusammen ergeben ein Fadenkreuz."),
                    InfoBlock.Para("Hier derselbe Spike-Herd wie in der vorigen Lektion: Jetzt liegt die Phasenumkehr zwischen Cz-C3 (Spitze nach unten) und C3-T3 (Spitze nach oben). C3 kommt in beiden Verschaltungen als gemeinsame Elektrode heraus – der Ort ist damit sicher."),
                    InfoBlock.Bullet("Sie verbindet die Mittellinie (Fz, Cz, Pz) mit beiden Seiten. Befunde nahe der Mittellinie, die in der Längsreihe unklar bleiben, können hier eine Phasenumkehr zeigen. Die Vertexwellen des Schlafs zum Beispiel haben ihre Phasenumkehr bei Cz."),
                    InfoBlock.Bullet("Jede Kette läuft über beide Seiten: Rechts und links stehen in derselben Reihe nebeneinander."),
                    InfoBlock.Warning("Auch hier gilt: Am Kettenende gibt es keine Phasenumkehr, und was an beiden Elektroden gleich ist, fällt heraus. Der Alpha-Rhythmus ist rechts und links ähnlich – deshalb ist er in O2-O1 nur etwa halb so groß wie an der einzelnen Elektrode."),
                    InfoBlock.TryIt("Suchen Sie die Phasenumkehr in der zentralen Reihe. Wechseln Sie dann im Reiter »Verschaltung« zurück zur Längsreihe und vergleichen Sie: In beiden ist C3 die gemeinsame Elektrode.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Referenzableitung zu den Ohren",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Ear, "C3-A1", False, False, SignalPart.Alpha, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("In einer Referenzableitung werden alle Elektroden mit derselben Bezugselektrode verglichen – hier mit dem Ohr der gleichen Seite: links A1, rechts A2. Die Idee: Der Bezug liegt abseits des Geschehens und ist möglichst ruhig. Dann zeigt jeder Kanal, was unter **seiner** Elektrode passiert."),
                    InfoBlock.Bullet("Vorteil 1: Die Höhe des Ausschlags entspricht der Größe der Spannung. Der größte Ausschlag gehört zur Elektrode über dem Herd – hier C3-A1. In F3, P3, T3 und Cz ist der Spike kleiner (das Feld) und zeigt ebenfalls nach oben (negativ). Eine Phasenumkehr gibt es nicht und braucht es nicht."),
                    InfoBlock.Bullet("Vorteil 2: Auch weit ausgebreitete Aktivität bleibt in voller Größe sichtbar. Form und Seitenvergleich lassen sich gut beurteilen."),
                    InfoBlock.Warning("Aber: Kein Bezug ist wirklich stumm – eine ruhige Referenz gibt es am Kopf nicht. Was am Ohr anliegt, erscheint umgekehrt in **allen** Kanälen dieser Seite. In dieser Simulation liegt am Ohr ein Teil des Alpha-Rhythmus an: Er taucht deshalb klein, aber regelmäßig auch in den Stirnkanälen auf (Fp1-A1, Fp2-A2), obwohl er dort kaum vorhanden ist."),
                    InfoBlock.Warning("Die Ohrelektroden gelten als besonders störanfällig. In dieser Simulation ist das EKG-Artefakt (hier noch ausgeschaltet) in der Ohrreferenz am größten und links stärker als rechts."),
                    InfoBlock.Tip("Faustregel: Zeigt sich dieselbe Welle fast gleich groß in allen Kanälen einer Seite, steckt sie wahrscheinlich in der Referenz. Dann den Bezug wechseln."),
                    InfoBlock.Para("Die Abstände zum Ohr sind sehr unterschiedlich (T3-A1 kurz, Fp1-A1 lang). Die DGKN weist darauf hin, dass das beachtet werden muss. Statt der Ohren können laut DGKN auch die Mastoid-Elektroden benutzt werden."),
                    InfoBlock.TryIt("1. Schalten Sie im Reiter »Signal« Alpha aus und wieder an und achten Sie auf Fp1-A1. 2. Schalten Sie das EKG-Artefakt ein und vergleichen Sie rechts und links. 3. Wechseln Sie zur Längsreihe: Das EKG-Artefakt ist dort viel kleiner.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Referenzableitung zu Cz (Vertex)",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Vertex, "C3-Cz", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Hier ist Cz der Bezug, der Scheitelpunkt in der Mitte des Kopfes. Jede andere Elektrode wird mit Cz verglichen."),
                    InfoBlock.Bullet("Vorteile: Cz liegt weit weg von den Schläfen und vom Hinterkopf. Befunde dort lassen sich gegen Cz gut darstellen – die Amplitude des Alpha-Rhythmus misst man am einfachsten in einer Cz-Referenzableitung. Ob eine Welle am Hinterkopf positiv oder weiter vorne negativ ist, lässt sich in bipolaren Reihen nicht immer leicht entscheiden; gegen Cz gelingt das meist sofort. Die Cz-Elektrode ist außerdem weniger störanfällig als die Ohrelektroden, und beide Seiten haben denselben Bezug."),
                    InfoBlock.Warning("Nachteile: Auch Cz ist nicht stumm. Liegt der Befund in der Nähe von Cz – zentral oder nahe der Mittellinie –, steckt er in der Referenz und erscheint in allen Kanälen. Im Schlaf liegen Vertexwellen und K-Komplexe genau bei Cz; bei ausgeprägter Schlafaktivität ist Cz deshalb keine gute Wahl. Elektroden nahe an Cz (C3, C4, Fz, Pz) haben außerdem nur einen kurzen Abstand zum Bezug – und je kleiner der Elektrodenabstand, desto kleiner die gemessene Spannungsdifferenz."),
                    InfoBlock.Para("Sehen Sie auf den Spike-Herd unter C3: Sein Feld reicht bis Cz. C3-Cz zeigt den Spike deshalb kleiner als die Ohrreferenz. Und in fast allen **anderen** Kanälen – auch auf der rechten Seite – erscheint ein kleiner Ausschlag in Gegenrichtung (nach unten). Dort ist kein Herd: Der Ausschlag steckt in der Referenz."),
                    InfoBlock.Tip("Faustregel: Den Bezug dort wählen, wo der Befund **nicht** ist. Bei einem Befund an der Schläfe oder am Hinterkopf ist Cz gut geeignet. Bei einem zentralen oder frontalen Befund und im Schlaf sind die Ohren besser."),
                    InfoBlock.TryIt("Vergleichen Sie C3-Cz mit T4-Cz und O2-Cz. Wechseln Sie dann zur Ohrreferenz: Die Gegenausschläge rechts verschwinden, und C3-A1 ist größer.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Die Durchschnittsreferenz (AV)",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Average, "C3-AV", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Statt einer einzelnen Elektrode dient hier der Mittelwert vieler Elektroden als Bezug: Kanal = Elektrode minus Durchschnitt. AV steht für Average. Diesen Durchschnitt gibt es nicht als Elektrode am Kopf – der Computer rechnet ihn aus."),
                    InfoBlock.Para("Die Idee: Aktivität, die nur an wenigen Elektroden anliegt, geht im Mittelwert fast unter. Der Bezug ist damit ziemlich ruhig und hängt nicht an einer einzelnen Elektrode. Amplituden lassen sich gut vergleichen, und der Herd ist dort, wo der Ausschlag am größten ist – hier C3-AV."),
                    InfoBlock.Bullet("Üblicherweise gehen nicht alle Elektroden in den Durchschnitt ein. Laut DGKN bleiben typischerweise Fp1 und Fp2 (wegen der Augenartefakte) und die Ohren draußen. So ist es auch hier gerechnet."),
                    InfoBlock.Warning("Große oder weit ausgebreitete Spannungen verschieben den Durchschnitt mit. Sie erscheinen dann kleiner und zusätzlich umgekehrt in den übrigen Kanälen. Hier sehen Sie zu jedem Spike einen kleinen Gegenausschlag in den Kanälen abseits des Herdes. Auch der Alpha-Rhythmus steckt im Durchschnitt und erscheint deshalb umgekehrt in den vorderen Kanälen, wo er kaum vorhanden ist. Bei Aktivität, die den ganzen Kopf erfasst, hilft die Durchschnittsreferenz meist nicht weiter."),
                    InfoBlock.Warning("Eine einzige schlechte Elektrode stört über den Durchschnitt **alle** Kanäle."),
                    InfoBlock.Tip("Die DGKN erlaubt die Durchschnittsreferenz, wenn den Auswertenden bewusst ist, dass sie keiner physikalischen Referenz entspricht."),
                    InfoBlock.TryIt("1. Schalten Sie im Reiter »Signal« das Augenblinzeln ein: Obwohl Fp1 und Fp2 nicht im Durchschnitt stecken, erscheint es über die frontalen Elektroden umgekehrt in allen Kanälen, bis nach okzipital. 2. Stellen Sie im Reiter »Impedanzen« T4 auf 50 kΩ: Die Pops dieser einen Elektrode tauchen klein in allen Kanälen auf.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Die Quellenableitung",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Source, "C3-Q", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Spikes),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Quellenableitung (nach Hjorth, auch Laplace-Ableitung genannt) vergleicht jede Elektrode mit dem Mittelwert ihrer direkten Nachbarn. C3 zum Beispiel mit dem Durchschnitt aus F3, T3, Cz und P3. In der Kanalliste steht dafür hier das Kürzel Q."),
                    InfoBlock.Para("Die Frage hinter dieser Verschaltung lautet: Was ist **hier** anders als ringsum? Alles Ausgebreitete und jede Störung in einem fernen Bezug fällt heraus. Übrig bleibt nur, was streng örtlich ist."),
                    InfoBlock.Bullet("Stärke: scharfe Ortsangabe, unabhängig von einer fernen Referenzelektrode. Sie eignet sich am besten für umschriebene Entladungen mit kleinem Feld. Der Spike erscheint hier fast nur noch in C3-Q."),
                    InfoBlock.Warning("Schwächen: 1. Weit ausgebreitete Aktivität wird verkleinert – der Alpha-Rhythmus über O1 ist hier nur noch etwa halb so groß wie in der Ohrreferenz. 2. Am Rand eines ausgedehnten Befundes kann fälschlich ein Herd erscheinen. 3. Um den Herd herum entstehen kleine Ausschläge in Gegenrichtung, hier zum Beispiel in T3-Q, F7-Q und T5-Q. 4. Die Elektroden am Rand des Kopfes haben in dieser Simulation nur drei Nachbarn, die alle auf einer Seite liegen."),
                    InfoBlock.Para("Die Kurven zeigen hier keine Spannung zwischen zwei Elektroden mehr, sondern eine berechnete Größe. Die Berechnung hängt von der jeweiligen Elektrode und ihren Nachbarn ab; hier sind es vereinfacht die direkten Nachbarn mit gleichem Gewicht. Die DGKN-Empfehlung führt die Quellenableitung nicht unter ihren Standardprogrammen auf – sie ist eine Ergänzung."),
                    InfoBlock.TryIt("Vergleichen Sie im Reiter »Verschaltung« nacheinander Ohrreferenz, Durchschnittsreferenz und Quellenableitung. Wie groß ist jeweils das Feld des Spikes um C3? Und was passiert mit dem Alpha-Rhythmus über O1 und O2?")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterMontage,
                .Title = "Welche Verschaltung wofür?",
                .RightTab = Lesson.TabMontage,
                .Preset = Preset(0.3, 70, False, False, 10, Montages.Longitudinal, "F3-C3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Delta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Keine Verschaltung ist die richtige. Jede beantwortet eine andere Frage und hat ihre eigene Schwäche:"),
                    InfoBlock.Bullet("Längsreihe: zum Durchsehen der Ableitung und zur Herdsuche von vorne nach hinten (Phasenumkehr). Schwäche: Kettenenden, ausgebreitete Aktivität."),
                    InfoBlock.Bullet("Querreihe: Herdsuche von rechts nach links, Befunde nahe der Mittellinie. Ergänzt die Längsreihe zum Fadenkreuz."),
                    InfoBlock.Bullet("Ohrreferenz: Größe und Form, ausgebreitete Aktivität, zentrale und frontale Befunde. Schwäche: Was am Ohr anliegt, steckt in allen Kanälen der Seite; die Ohrelektroden sind besonders störanfällig."),
                    InfoBlock.Bullet("Vertexreferenz (Cz): Befunde an Schläfe und Hinterkopf, Amplitude des Alpha-Rhythmus; weniger störanfällig als die Ohren. Schwäche: Befunde nahe Cz und Schlaf."),
                    InfoBlock.Bullet("Durchschnittsreferenz (AV): Überblick und Verteilung über den Kopf. Schwäche: große oder generalisierte Spannungen und Artefakte verschieben den Bezug."),
                    InfoBlock.Bullet("Quellenableitung: kleine, umschriebene Herde. Schwäche: Ausgebreitetes wird verkleinert; der Rand eines ausgedehnten Befundes kann wie ein Herd aussehen."),
                    InfoBlock.Tip("Darum verlangt die DGKN, dass die Programme Referenzschaltungen sowie bipolare Längs- und Querreihen enthalten und dass jeder Befund in mindestens zwei Verschaltungen – bipolar und referenziell – beurteilt wird. Erscheint in einer Referenzableitung unerwünschte Aktivität im Bezug, wird ein anderer Bezug gewählt und der Wechsel vermerkt."),
                    InfoBlock.Para("Für die Ableitung heißt das: Alle Verschaltungen setzen voraus, dass die Elektroden richtig ausgemessen sind und an der richtigen Buchse stecken. Ein falscher Abstand oder eine vertauschte Elektrode verfälscht jede Verschaltung. Beim Ableiten sollte jede gesetzte Elektrode in der gewählten Verschaltung sichtbar sein, damit Artefakte früh erkannt und beseitigt werden. Verschaltungen nach dem Standard des Labors benutzen; auf dem Bildschirm und im Ausdruck muss für jeden Kanal eindeutig erkennbar sein, welche Elektroden verschaltet sind (DGKN)."),
                    InfoBlock.TryIt("In dieser Aufnahme steckt ein Delta-Herd. Finden Sie die Elektrode über dem Herd in drei Verschaltungen: in der Längsreihe (Phasenumkehr), in der Querreihe (Phasenumkehr) und in der Ohrreferenz (größter Ausschlag). Kommen Sie dreimal auf dieselbe Elektrode?")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Der LowCut (FU): Sieb für langsame Wellen",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0, 70, False, False, 7, Montages.Excerpt, "Fp1-F3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Sweat),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der LowCut heißt auch Hochpass, untere Grenzfrequenz (FU) oder Zeitkonstante (TC). Er lässt schnelle Wellen durch und dämpft sehr langsame."),
                    InfoBlock.Para("Wofür? Gegen langsame Störungen: Schwitzen, Bewegung der Elektrodenkabel, Atmung. Sie lassen die Grundlinie »wandern«. Im Moment ist der LowCut **aus** – sehen Sie, wie die Kurven auf und ab wandern?"),
                    InfoBlock.Tip("Zeitkonstante und Grenzfrequenz sind zwei Namen für dieselbe Einstellung:  TC 0,3 s entspricht 0,53 Hz,  TC 0,1 s entspricht 1,6 Hz.  Faustregel: **kleinere** Zeitkonstante = **stärkerer** Filter."),
                    InfoBlock.Para("Woher der Name kommt: Gibt man einen Spannungssprung (Eichsignal) auf den Verstärker, fällt die Kurve wieder zur Grundlinie zurück. Die Zeitkonstante ist die Zeit, nach der nur noch 37 % des Sprungs übrig sind."),
                    InfoBlock.Para("Ein Filter schneidet nicht wie mit der Schere ab. Genau an der Grenzfrequenz bleiben noch etwa 70 % der Wellenhöhe übrig – je langsamer die Welle, desto weniger kommt durch. Das zeigt die türkise Linie in der Frequenz-Ansicht."),
                    InfoBlock.TryIt("Stellen Sie den LowCut nacheinander auf TC 1,0 s, 0,3 s und 0,1 s. Schalten Sie dann »Entfernten Anteil zeigen« ein: Rot ist genau das, was der Filter herausnimmt. Sie sehen: Bei TC 0,3 s bleibt ein Teil des Schwitzartefakts sichtbar – Schwitzen muss man an der Ursache bekämpfen (Raum kühlen, Haut trocknen).")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Vorsicht: LowCut zu hoch",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.03, 70, False, False, 7, Montages.Excerpt, "F3-C3", True, False, SignalPart.Beta, SignalPart.Delta, SignalPart.Blinks),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Ein zu starker LowCut entfernt nicht nur Störungen, sondern auch echte langsame Hirnaktivität. In dieser Aufnahme steckt ein Delta-Herd links zentral (F3-C3 und C3-P3)."),
                    InfoBlock.Para("Eingestellt ist gerade TC 0,03 s (5,3 Hz). Grau unterlegt sehen Sie das ungefilterte Original."),
                    InfoBlock.Warning("Bei TC 0,03 s sind die Delta-Wellen fast verschwunden – ein krankhafter Befund kann so übersehen werden!"),
                    InfoBlock.Para("Die Augen sind in dieser Aufnahme geöffnet: Deshalb fehlt der Alpha-Rhythmus, und man sieht Augenblinzeln."),
                    InfoBlock.Para("Auch das Augenblinzeln in Fp1-F3 und Fp2-F4 (großer Ausschlag nach unten) wird verformt: Es wird schmaler und bekommt danach einen Gegenschwinger nach oben."),
                    InfoBlock.Tip("Empfehlung der DGKN für die Routine-Ableitung: Hochpass höchstens 0,5 Hz, das entspricht TC 0,3 s. (Die US-Leitlinie der ACNS nennt höchstens 1 Hz.) Stärker filtern nur bewusst und vorübergehend – und im Protokoll vermerken."),
                    InfoBlock.TryIt("Wechseln Sie zwischen TC 0,3 s und TC 0,03 s und vergleichen Sie Kanal F3-C3. Achten Sie in der Frequenz-Ansicht auf die blauen Delta-Balken.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Der HiCut (FO): Sieb für schnelle Wellen",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 0, False, False, 7, Montages.Excerpt, "Fp1-F3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Emg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Der HiCut heißt auch Tiefpass oder obere Grenzfrequenz (FO). Er lässt langsame Wellen durch und dämpft schnelle."),
                    InfoBlock.Para("Typische Störung: Muskelartefakte (EMG) – z. B. durch Zähne zusammenbeißen, Stirnrunzeln oder allgemeine Anspannung. Sie erscheinen als »Gekritzel« mit sehr vielen schnellen Zacken, hier vor allem frontal (Fp1-F3, Fp2-F4)."),
                    InfoBlock.Para("Im Moment ist der HiCut **aus**."),
                    InfoBlock.TryIt("Stellen Sie den HiCut nacheinander auf 70 Hz, 35 Hz und 15 Hz. Beobachten Sie, wie das Muskelartefakt kleiner wird – und schauen Sie, was mit den roten Balken rechts in der Frequenz-Ansicht passiert."),
                    InfoBlock.Tip("Empfehlung von DGKN und ACNS: HiCut mindestens 70 Hz. Bei Muskelartefakten ist es immer besser, den Patienten zu entspannen (Mund leicht öffnen, Schultern locker), als stärker zu filtern.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Vorsicht: HiCut zu niedrig",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 15, False, False, 7, Montages.Excerpt, "F3-C3", True, False, SignalPart.Alpha, SignalPart.Spikes, SignalPart.Emg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Spikes (spitze Potentiale, Dauer 20 bis unter 70 Millisekunden) sind wichtige Hinweise, z. B. bei Epilepsie. Weil sie so steil sind, enthalten sie viele schnelle Anteile."),
                    InfoBlock.Warning("Bei einem HiCut von 15 Hz werden Spikes stumpfer und breiter und verlieren rund 40 % ihrer Höhe – sie können dann wie harmlose Wellen aussehen."),
                    InfoBlock.Warning("Gefiltertes Muskelartefakt wird zu gleichmäßigeren Wellen, die wie Beta-Aktivität oder sogar wie steile Wellen aussehen können. Ein Filter macht aus einer Störung also nicht immer »nichts« – manchmal macht er etwas Täuschendes daraus."),
                    InfoBlock.Para("Grau unterlegt sehen Sie wieder das Original zum Vergleich."),
                    InfoBlock.TryIt("Vergleichen Sie die Spikes in F3-C3 und C3-P3 bei 70 Hz und bei 15 Hz. Achten Sie auch auf die Phasenumkehr: in F3-C3 zeigt die Spitze nach unten, in C3-P3 nach oben – die Spitzen zeigen aufeinander zu. Die gemeinsame Elektrode C3 ist also am stärksten negativ: Dort liegt der Herd.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Der 50-Hz-Filter (Netzfilter / Notch)",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "P4-O2", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Hum).WithImpedance("P4", 15),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Unser Stromnetz schwingt 50-mal pro Sekunde (50 Hz) und streut überall ein wenig in die Ableitung ein. Normalerweise unterdrückt der Verstärker diese Störung, weil sie an beiden Elektroden eines Kanals gleich groß ist. Liegt aber eine Elektrode schlecht an (hoher oder ungleicher Übergangswiderstand = Impedanz), gelingt das nicht mehr: Die Kurve wird dick und »pelzig«."),
                    InfoBlock.Para("Hier hat die Elektrode P4 einen schlechten Kontakt (15 kΩ, siehe Kopfschema im Reiter »Impedanzen«): Betroffen sind C4-P4 und P4-O2 – also genau die beiden Kanäle, in denen P4 vorkommt. Warum das so ist, zeigt das Kapitel »Verstärker und Impedanz«."),
                    InfoBlock.Para("Der 50-Hz-Filter ist ein »Kerbfilter«: Er schneidet gezielt einen schmalen Bereich um 50 Hz heraus und lässt den Rest in Ruhe. Der normale HiCut von 70 Hz hilft hier kaum – 50 Hz liegt ja noch darunter!"),
                    InfoBlock.Tip("Erst die Ursache beheben: Impedanz prüfen, Elektrode nachpräparieren, Kabelführung ändern, Störquellen suchen (Motor der Liege, Leuchtstofflampen, Geräte und Monitore in Patientennähe, defekter Schutzleiter)."),
                    InfoBlock.Warning("Der 50-Hz-Filter darf bei einer Standard-Ableitung nicht routinemäßig eingeschaltet sein: Er verdeckt Elektroden- und Kabeldefekte. Zulässig ist er allenfalls vorübergehend, z. B. auf der Intensivstation, wenn notwendige Geräte nicht abgeschaltet werden können."),
                    InfoBlock.TryIt("Schalten Sie den 50-Hz-Filter ein und aus. In der Frequenz-Ansicht sehen Sie die schmale Kerbe bei 50 Hz.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterFilter,
                .Title = "Der EKG-Filter",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "P4-O2", False, False, SignalPart.Beta, SignalPart.Theta, SignalPart.Ecg),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Das Herz erzeugt viel größere Spannungen als das Gehirn. Ein Teil davon erreicht die Kopfelektroden: Es entstehen kleine, regelmäßige Zacken im Takt des Herzschlags – das EKG-Artefakt. Vergleichen Sie mit dem EKG-Kanal ganz unten."),
                    InfoBlock.Para("Ein normaler Frequenzfilter hilft hier nicht: Die EKG-Zacke enthält Frequenzen, die auch im EEG vorkommen. Man würde das EEG gleich mit wegfiltern."),
                    InfoBlock.Para("Der EKG-Filter arbeitet deshalb anders – mit einer Schablone:"),
                    InfoBlock.Bullet("1. Im EKG-Kanal werden alle Herzschläge (R-Zacken) gefunden – die roten Dreiecke."),
                    InfoBlock.Bullet("2. Für jeden EEG-Kanal wird ausgerechnet, wie das Artefakt dort im Durchschnitt aussieht. Beim Mitteln vieler Herzschläge heben sich die echten Hirnwellen gegenseitig auf – übrig bleibt die typische Form des Artefakts (die Schablone)."),
                    InfoBlock.Bullet("3. Diese Schablone wird bei jedem Herzschlag abgezogen."),
                    InfoBlock.Tip("Voraussetzung ist ein sauberer EKG-Kanal! Ist das EKG gestört oder werden Herzschläge nicht erkannt, kann der Filter nicht richtig arbeiten."),
                    InfoBlock.TryIt("Schalten Sie den EKG-Filter ein und aus und schauen Sie auf P3-O1 und P4-O2. Mit »Entfernten Anteil zeigen« sehen Sie in Rot, was abgezogen wird. Schalten Sie danach im Reiter »Signal« Alpha dazu: Im kräftigen Alpha-Rhythmus fällt das Artefakt viel weniger auf – der Filter arbeitet trotzdem.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterPractice,
                .Title = "Die Amplitude (Empfindlichkeit)",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0.3, 70, False, False, 7, Montages.Excerpt, "P3-O1", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Die Amplitude (Empfindlichkeit) bestimmt, wie groß die Kurven gezeichnet werden. Sie wird in µV/mm angegeben: wie viele Mikrovolt nötig sind, damit die Kurve 1 mm ausschlägt."),
                    InfoBlock.Tip("Umgekehrte Logik: **kleinere** Zahl = **größere** Kurven.  Bei 7 µV/mm ist eine 70-µV-Welle 1 cm hoch, bei 3 µV/mm schon etwa 2,3 cm."),
                    InfoBlock.Para("Wichtig: Die Amplitude ist **kein** Filter. Das Signal selbst bleibt gleich, nur die Darstellung ändert sich. Deshalb verändert sich die Frequenz-Ansicht nicht."),
                    InfoBlock.Warning("Zu klein dargestellt (große Zahl): niedrige Aktivität wird übersehen. Zu groß dargestellt: Kurven überlappen und werden unlesbar."),
                    InfoBlock.Para("Hinweis: Wie groß ein Millimeter am Bildschirm wirklich ist, hängt von den Monitor-Einstellungen ab."),
                    InfoBlock.Para("Die Amplitude gilt für die EEG-Kanäle. Das EKG ist viel größer und hat deshalb eine eigene Einstellung, die neben dem Kanalnamen steht (hier 70 µV/mm): Ein Klick auf die EKG-Kurve zeichnet sie größer, ein Rechtsklick kleiner."),
                    InfoBlock.TryIt("Ändern Sie die Amplitude mit den Schaltflächen – und + (oder den Tasten + und − auf der Tastatur) und beobachten Sie den Eichbalken unten rechts im EEG. Das EKG bleibt dabei gleich groß. Klicken Sie dann auf die EKG-Kurve.")
                }})

            L.Add(New Lesson With {
                .Chapter = ChapterPractice,
                .Title = "Alles zusammen: Ihre Einstellung",
                .RightTab = Lesson.TabFilter,
                .Preset = Preset(0, 0, False, False, 7, Montages.Excerpt, "Fp1-F3", False, False, SignalPart.Alpha, SignalPart.Beta, SignalPart.Theta, SignalPart.Sweat, SignalPart.Emg, SignalPart.Hum, SignalPart.Ecg).WithImpedance("P4", 15),
                .Blocks = New List(Of InfoBlock) From {
                    InfoBlock.Para("Im Alltag kommen Störungen selten allein. Hier sind fast alle typischen Artefakte gleichzeitig eingeschaltet – und alle Filter stehen auf **aus**."),
                    InfoBlock.Para("Empfehlungen der DGKN für das Routine-EEG von Erwachsenen:"),
                    InfoBlock.Bullet("Impedanzen: unter 10 kΩ und möglichst gleichmäßig, nicht unter 1 kΩ; Impedanztest mit aufzeichnen"),
                    InfoBlock.Bullet("LowCut: höchstens 0,5 Hz, entspricht TC 0,3 s"),
                    InfoBlock.Bullet("HiCut: mindestens 70 Hz"),
                    InfoBlock.Bullet("50-Hz-Filter: nicht routinemäßig, nur vorübergehend im Ausnahmefall"),
                    InfoBlock.Bullet("EKG: wird immer mit aufgezeichnet; EKG-Filter nur bei Bedarf"),
                    InfoBlock.Bullet("Verschaltung: mindestens 18 Kanäle; jeder Befund wird bipolar **und** in einer Referenzableitung betrachtet"),
                    InfoBlock.Bullet("Amplitude: 5–10 µV/mm, üblich 7 µV/mm (= 70 µV/cm). Bei Kindern gelten laut DGKN dieselben Geräte- und Darstellungseinstellungen wie bei Erwachsenen."),
                    InfoBlock.Para("Bei digitalen Geräten werden Filter, Verschaltung, Amplitude und Zeitachse rechnerisch angewendet und können bei der Auswertung nachträglich geändert werden. Bitte immer an die Standards Ihres Labors halten."),
                    InfoBlock.TryIt("Beheben Sie zuerst, was sich an der Ursache beheben lässt (Kopfschema im Reiter »Impedanzen«!). Finden Sie dann die beste Einstellung, sodass Alpha gut erkennbar ist und die Störungen möglichst verschwinden. Prüfen Sie danach mit »Rohsignal grau unterlegen«, was Sie verändert haben."),
                    InfoBlock.Tip("Goldene Regel: So wenig filtern wie möglich, so viel wie nötig. Zuerst die Ursache beheben (Elektroden, Entspannung, Raum) – und jede Filteränderung dokumentieren.")
                }})

            Return L
        End Function

    End Module

End Namespace
