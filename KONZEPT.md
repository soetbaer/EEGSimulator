# NeuroSim – Konzept: EEG-Filter und Impedanzen verständlich machen

Zielgruppe: MTA/MFA in der Neurologie, keine Techniker.
Grundsatz: **Erst sehen, dann verstehen.** Keine Formeln im Vordergrund, sondern Bilder, Vergleiche und sofortiges Ausprobieren.

## 1. Didaktischer Aufbau

| Schritt | Kernidee | Bild / Vergleich |
|---|---|---|
| Was ist ein EEG? | Winzige Spannungen (µV), jeder Kanal = Differenz zweier Elektroden | AA-Batterie hat ca. 30.000-mal mehr Spannung als eine Alpha-Welle |
| Frequenz | Hz = Wellen pro Sekunde | Wellen zwischen zwei Sekundenlinien zählen |
| Frequenzbänder | Delta, Theta, Alpha, Beta | Lange Meereswellen gegenüber kleinem Gekräusel |
| Filter allgemein | Ein Filter ist ein **Sieb** | Rote Schattierung = „wird gedämpft“, türkise Linie = „kommt durch“ |
| LowCut (FU / TC) | Hält langsame Wellen zurück (Schwitzen, Drift) | Kurven „wandern“ ohne LowCut |
| LowCut zu hoch | Delta-Herd verschwindet, Blinzeln wird verformt | Direkter Vergleich mit grauem Original |
| HiCut (FO) | Hält schnelle Wellen zurück (EMG) | „Gekritzel“ wird ruhiger |
| HiCut zu niedrig | Spikes werden stumpf, EMG sieht wie Beta aus | „Ein Filter kann täuschen“ |
| 50-Hz-Filter | Kerbe genau bei 50 Hz | Ursache meist eine schlechte Elektrode |
| Impedanz | Kontakt zur Kopfhaut; nicht nur niedrig, sondern gleich | Balkenwaage: Nur gleiche Kontakte lassen die Störung herausfallen |
| Folgen schlechter Impedanz | Unruhe, Pops, Verwechslung mit Spike oder Herd; Gelbrücke | Störung nur an einer Elektrode, in Nachbarkanälen spiegelbildlich |
| EKG-Filter | Schablone mitteln und abziehen | Viele Herzschläge übereinanderlegen: Das EEG mittelt sich weg, das Artefakt bleibt |
| Amplitude | µV/mm, **kleinere Zahl = größere Kurven**, kein Filter! | Eichbalken, Frequenz-Ansicht ändert sich nicht |
| Alles zusammen | So wenig filtern wie möglich, so viel wie nötig | Freie Übung mit allen Artefakten |

Wiederkehrende Elemente in jeder Lektion:
- **Merke** (gelb): ein Satz zum Mitnehmen
- **Achtung** (rot): typische Fehlerquelle in der Praxis
- **Probieren Sie es aus** (blau): konkrete Aufgabe mit den Reglern
- **Was passiert gerade?**: Live-Erklärung passend zur aktuellen Einstellung, mit Balken „So viel kommt durch“ (Hirnaktivität soll bleiben, Artefakte sollen weg)

## 2. Visualisierung

1. **EEG-Ansicht**: 8 Kanäle (Längsreihe) plus EKG, 10 s je Seite, 1-s-Raster und Eichbalken. Optional:
   - grau: Rohsignal (vorher)
   - rot: vom Filter entfernter Anteil
   - rote Dreiecke: erkannte Herzschläge
2. **Frequenz-Ansicht** (logarithmische Achse 0,1–150 Hz) mit farbigen Bändern:
   - graue Balken: vorher
   - farbige Balken: nachher
   - türkise Linie: Durchlass des Filters
   - rote Schattierung: Sperrbereich

## 3. Simulation

- Abtastrate 500 Hz, 65,5 s Aufnahme, reproduzierbar (fester Zufallsstartwert)
- Alle Werte sind Potentialdifferenzen (Eingang 1 minus Eingang 2); die Anzeige folgt der Konvention „negativ nach oben“
- Bausteine, einzeln zuschaltbar:

| Baustein | Eigenschaft |
|---|---|
| Delta-Herd | links zentral, mit Phasenumkehr |
| Theta | – |
| Alpha | okzipital, an- und abschwellend |
| Beta | – |
| Spike-Wave | oberflächennegativ, Herd C3 mit Phasenumkehr |
| Augenblinzeln | Fp positiv, Ausschlag nach unten |
| Schwitzartefakt | sehr langsam (0,1–0,4 Hz), je Kanal unabhängig |
| EMG | schubweise, frontal |
| 50-Hz-Einstreuung | Stärke je Kanal folgt aus dem Impedanz-Unterschied der beiden Elektroden |
| EKG-Artefakt | – |

### Filter

Alle Filter arbeiten kausal, also wie in einem echten Gerät.

| Filter | Umsetzung |
|---|---|
| LowCut | Hochpass 1. Ordnung (RC-Glied), daher gilt exakt TC = 1/(2·π·FU) |
| HiCut | Butterworth-Tiefpass 2. Ordnung |
| 50 Hz | IIR-Kerbfilter (Q = 8) |
| EKG-Filter | Template-Subtraktion, siehe unten |

### Ablauf des EKG-Filters

1. Die R-Zacken werden im EKG-Kanal erkannt: Bandpass 8–25 Hz, Energie der Steigung, Schwelle, Refraktärzeit 250 ms.
2. Pro Kanal wird der Mittelwert über alle Herzschläge gebildet (−250 ms bis +450 ms).
3. Die Grundlinie wird entfernt. Eine statistische Schrumpfung sorgt dafür, dass kein EEG-Rauschen mit abgezogen wird.
4. Die Ränder werden weich ausgeblendet (Tukey-Fenster).
5. Die Schablone wird bei jedem Herzschlag abgezogen.

## 4. Fachliche Grundlagen und Quellen

Die Texte und die Simulation wurden gegen öffentlich zugängliche Leitlinien und Fachliteratur geprüft.

| Aussage in der Anwendung | Grundlage |
|---|---|
| LowCut höchstens 0,5 Hz (TC 0,3 s), HiCut mindestens 70 Hz, Empfindlichkeit 5–10 µV/mm | DGKN, Empfehlungen für die Durchführung von EEG-Ableitungen bei Erwachsenen (Fassung 2024) |
| Bei Kindern dieselben Geräte- und Darstellungseinstellungen wie bei Erwachsenen; Widerstände wiederholt prüfen; bei Neugeborenen unter 10 kΩ | DGKN, Empfehlungen für EEG-Ableitungen bei Kindern (Fassung Oktober 2024, im Volltext geprüft) |
| Anordnung rechts über links, anterior über posterior; EKG während der gesamten Ableitung; mindestens 18 Kanäle | DGKN, Empfehlung zu EEG-Ableiteprogrammen (Fassung Oktober 2024, im Volltext geprüft) |
| Standard 70 µV/cm, untere Grenzfrequenz 0,5–1 Hz, obere 70 Hz; 50-Hz-Filter bei Standard-Ableitung nicht routinemäßig (verdeckt Elektroden- und Kabeldefekte); Impedanz ≤ 5 kΩ, in Ausnahmen bis 10 kΩ, möglichst gleichmäßig; Filter und Verschaltung bei digitalen Geräten nachträglich änderbar | Baier, Kap. B-1.2 in Bischoff & Straube (Hrsg.), Leitlinien Klinische Neurophysiologie, Kohlhammer 2014 |
| Alpha 8–13 Hz, üblicherweise um 30 µV; Beta über 13 bis 40 Hz, meist 15–25 Hz, frontozentral; Theta 4–7 Hz | Rosenow, Kap. B-1.3, ebenda |
| Hauptkomponente epilepsietypischer Potentiale in der Regel negativ | Kap. B-1 (Befundung), ebenda |
| Widerstände unter 100 Ω nicht zulässig (Abnahme der Potentialamplituden); Übergangswiderstände 1–10 kΩ | Kap. A-1 (EEG beim irreversiblen Hirnfunktionsausfall), ebenda |
| Hautvorbereitung (entfetten, abrasive Paste sparsam, Kontaktgel), auf kleine Widerstandsdifferenz achten; typische 50-Hz-Störquellen | Merkblatt „Artefakte bei evozierten Potentialen“ (H. Krase) |
| LowCut höchstens 1 Hz (TC mindestens 0,16 s), HiCut mindestens 70 Hz; Netzfilter nur, wenn andere Maßnahmen versagen, weil er Spikes verändern kann | ACNS Guideline 1: Minimum Technical Requirements for Performing Clinical EEG (2016) |
| Spike: 20 bis unter 70 ms; Frequenzbänder | IFCN-Glossar (Kane et al., Clin Neurophysiol Pract 2017) |
| Polarität „negativ nach oben“; Phasenumkehr an der gemeinsamen Elektrode | Beniczky & Schomer, Epileptic Disord 2020; Lehrbuchstandard |
| Augenblinzeln: Fp wird positiv, Ausschlag nach unten in Fp1-F3/Fp2-F4 | Hornhaut-Netzhaut-Dipol und Bell-Phänomen; Lehrbuchstandard |
| Schwitzartefakt: sehr langsam, unter 0,5 Hz | Lehrbuchstandard (z. B. R.-EEG-T.-Lehrmaterial) |
| Muskelartefakt reicht von etwa 20 bis 300 Hz; gefiltert kann es Beta oder steile Wellen vortäuschen | Muthukumaraswamy, Front Hum Neurosci 2013; Goncharova et al., Clin Neurophysiol 2003 |
| EKG-Filter durch R-Zacken-getriggerte Mittelung und Subtraktion | Nakamura & Shibasaki, Electroencephalogr Clin Neurophysiol 1987; 66: 88–92 |

### Impedanzmodell

Störungen entstehen in der Simulation an der einzelnen Elektrode. Im Kanal erscheint die Differenz (Eingang 1 minus Eingang 2).

| Effekt | Modell | Grundlage |
|---|---|---|
| 50-Hz-Einstreuung | Störspannung je Elektrode = 2 µV pro kΩ (Beispielwert, entspricht etwa 2 nA Störstrom in der Zuleitung). Im Kanal bleibt der Unterschied der beiden Elektroden. | Ungleiche Impedanzen verschlechtern die Gleichtaktunterdrückung (ACNS Guideline 1; Huhta & Webster 1973) |
| Unruhige Grundlinie | Langsames Rauschen (0,2–3 Hz), ab 5 kΩ mit der Impedanz zunehmend | Kappenman & Luck, Psychophysiology 2010: mehr Rauschen unter 5 Hz bei hoher Impedanz |
| Elektroden-Pops | Steiler Sprung, Rücklauf mit 0,15 s, ab 10 kΩ; nur an einer Elektrode | Lehrbuchbeschreibung des Pop-Artefakts (steiler Anstieg, langsamer Abfall, kein Feld) |
| Kein Amplitudenverlust | Spannungsteiler Elektrode/Verstärker wird nur berechnet und angezeigt (bei 50 kΩ und 100 MΩ: 0,05 %) | Ferree et al., Clin Neurophysiol 2001; IFCN-Standard: Eingangswiderstand über 100 MΩ |
| Widerstandsrauschen | Wird nur berechnet und angezeigt (bei 25 kΩ und 70 Hz Bandbreite etwa 0,2 µV) | Physikalische Formel für thermisches Rauschen |
| Gelbrücke F3–C3 | Kanal F3-C3 behält 8 % der Amplitude | Lehrbuchbeschreibung der Salzbrücke (flacher Kanal) |

Grenzwerte im Kopfschema: grün 1–5 kΩ, gelb bis 10 kΩ, rot über 10 kΩ, blau unter 1 kΩ (DGKN 2024: 1–10 kΩ, möglichst gleichmäßig; ACNS: Ziel 5 kΩ, bis 10 kΩ akzeptabel, nicht unter 100 Ω).

### Bewusste Vereinfachungen

- Die Stärke der Netzeinstreuung hängt in Wirklichkeit stark von der Umgebung ab. Der Wert von 2 µV pro kΩ ist ein Beispiel, kein Messwert.
- Die Schwellen, ab denen Unruhe (5 kΩ) und Pops (10 kΩ) einsetzen, sind didaktisch gewählt. In Wirklichkeit hängen Pops vom lockeren Sitz ab, nicht von einem festen Impedanzwert.
- Die Gelbrücke wirkt nur auf den Kanal F3-C3. Die Nachbarkanäle bleiben unverändert.

- Die Flankensteilheit der Filter ist geräteabhängig. Hier: LowCut 6 dB/Oktave (wie ein RC-Glied), HiCut 12 dB/Oktave.
- Die Signale sind synthetisch. Amplituden und Verteilung sind typisch gewählt, bilden aber keinen einzelnen Patienten ab.
- Gezeigt wird nur ein Ausschnitt einer Längsreihe (parasagittale Ketten rechts und links plus EKG), keine vollständige Montage mit mindestens 18 Kanälen.
- Die Balken „So viel kommt durch“ werden am simulierten Baustein gemessen (Effektivwert, bei Spikes, Blinzeln und EKG die Spitzenhöhe).
