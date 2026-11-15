# NeuroSim – Konzept: EEG-Filter und Impedanzen verständlich machen

Zielgruppe: Medizinische Technologinnen und Technologen für Funktionsdiagnostik (MTF, früher MTA-F), MFA und weiteres Fachpersonal in der Neurophysiologie, keine Techniker.
Grundsatz: **Erst sehen, dann verstehen.** Keine Formeln im Vordergrund, sondern Bilder, Vergleiche und sofortiges Ausprobieren.

## 1. Didaktischer Aufbau

| Schritt | Kernidee | Bild / Vergleich |
|---|---|---|
| Was ist ein EEG? | Winzige Spannungen (µV), jeder Kanal = Differenz zweier Elektroden | AA-Batterie hat ca. 50.000-mal mehr Spannung als eine Alpha-Welle von 30 µV |
| Frequenz | Hz = Wellen pro Sekunde | Wellen zwischen zwei Sekundenlinien zählen |
| Frequenzbänder | Delta, Theta, Alpha, Beta | Lange Meereswellen gegenüber kleinem Gekräusel |
| Headbox | Spannung gibt es nur zwischen zwei Punkten; jeder Kanal ist Eingang 1 minus Eingang 2 | Ein Kanal zerlegt: beide Eingänge einzeln, darunter ihr Unterschied |
| Differenzverstärker | Was an beiden Eingängen gleich ist (Gleichtakt), fällt heraus | Balkenwaage; 1000 µV Netzstörung an den Eingängen, sauberer Ausgang |
| Impedanz am Verstärker | Ungleiche Impedanzen machen aus Gleichtakt einen Unterschied, der wie EEG verstärkt wird | Eine Elektrode schlecht: Brumm am Ausgang; beide gleich schlecht: Brumm weg, Kurve unruhig |
| Verschaltung | Dasselbe EEG aus verschiedenen Blickwinkeln | Berg von verschiedenen Seiten fotografiert; Kopfschema mit Eingang 1 und 2 |
| Längs- und Querreihe | Nachbar gegen Nachbar; Phasenumkehr zeigt den Ort | Fadenkreuz aus zwei Richtungen |
| Ohr- und Vertexreferenz | Alle gegen einen Bezug: wahre Größe, aber kein Bezug ist stumm | Alpha an der Stirn (Ohr), Gegenausschläge in allen Kanälen (Cz) |
| Durchschnittsreferenz | Berechneter Bezug; große Potentiale verschieben ihn | Blinzeln erscheint umgekehrt in allen Kanälen |
| Quellenableitung | Elektrode gegen ihre Nachbarn: nur Örtliches bleibt | Spike nur noch in einem Kanal, Alpha stark verkleinert |
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

1. **EEG-Ansicht**: Kanäle der gewählten Verschaltung plus EKG (Ausschnitt der Längsreihe mit 8 Kanälen oder vollständige Programme mit 18 bis 20 Kanälen), 10 s je Seite, 1-s-Raster und Eichbalken. Optional:
   - grau: Rohsignal (vorher)
   - rot: vom Filter entfernter Anteil
   - rote Dreiecke: erkannte Herzschläge
2. **Frequenz-Ansicht** (logarithmische Achse 0,1–150 Hz) mit farbigen Bändern:
   - graue Balken: vorher
   - farbige Balken: nachher
   - türkise Linie: Durchlass des Filters
   - rote Schattierung: Sperrbereich

3. **Kopfschema der Verschaltung**: 21 Elektroden, Ketten der Verschaltung als Linien; für den gewählten Kanal Eingang 1 blau, Eingang 2 bzw. Bezug orange.
4. **Kopfschema der Impedanzen**: 21 Elektroden mit Ampelfarben, Werte per Klick oder Tastatur änderbar.

## 3. Simulation

- Abtastrate 500 Hz, 65,5 s Aufnahme, reproduzierbar (fester Zufallsstartwert)
- Gerechnet wird zuerst das Potential jeder der 21 Elektroden des 10/20-Systems (Fp1 … O2, A1, A2) gegenüber einem gedachten ruhigen Bezugspunkt. Jede Verschaltung bildet daraus ihre Kanäle als gewichtete Differenzen – so wie ein digitales Gerät alle Elektroden gleichzeitig aufzeichnet und die Verschaltung erst für die Anzeige rechnet. Die Anzeige folgt der Konvention „negativ nach oben“
- Jeder Baustein hat eine räumliche Verteilung (Gewicht je Elektrode) und einen oder mehrere Zeitverläufe; Schwitzen, Muskelartefakt, Grundrauschen, Kontakt-Unruhe und Pops entstehen an jeder Elektrode unabhängig
- Bausteine, einzeln zuschaltbar:

| Baustein | Eigenschaft |
|---|---|
| Delta-Herd | links zentral, am größten unter C3, Feld bis F3, P3, T3, Cz |
| Theta | diffus, zentral betont; je Seite gemeinsam plus örtlicher Anteil |
| Alpha | okzipital am größten, nach vorne abnehmend; auch die Ohren liegen im Alpha-Feld |
| Beta | frontozentral; je Seite gemeinsam plus örtlicher Anteil |
| Spike-Wave | oberflächennegativ, Herd C3 mit Phasenumkehr; Spitze etwa 16 ms breit (Halbwertsbreite), HiCut 15 Hz nimmt ihr rund 40 % der Höhe |
| Augenblinzeln | Fp positiv, Ausschlag nach unten; steht für „Augen offen“ und ist in den Lektionen nie zusammen mit Alpha voreingestellt |
| Schwitzartefakt | sehr langsam (0,1–0,4 Hz), je Elektrode unabhängig |
| EMG | schubweise, frontal und temporal |
| 50-Hz-Einstreuung | Stärke je Kanal folgt aus dem Impedanz-Unterschied der beiden Elektroden |
| EKG-Artefakt | umso größer, je tiefer die Elektrode sitzt (Ohren am größten), links stärker als rechts |
| Gleichtakt | 50 Hz (Beispiel 1000 µV) und langsames Wandern des Körperpotentials, an allen Elektroden gleich; nur an den einzeln gezeigten Verstärkereingängen sichtbar |

### Verschaltungen

Kanalfolgen nach der DGKN-Empfehlung zu EEG-Ableiteprogrammen (Oktober 2024), jeweils die „typische“ Variante; Elektrodennamen wie dort (T3/T4/T5/T6).

| Verschaltung | Rechnung je Kanal |
|---|---|
| Längsreihe (18 Kanäle), Querreihe (20 Kanäle), Ausschnitt (8 Kanäle) | Elektrode minus Nachbarelektrode |
| Ohrreferenz (19 Kanäle) | Elektrode minus Ohr der gleichen Seite (Fz und Pz gegen A2, Cz gegen A1) |
| Vertexreferenz (18 Kanäle) | Elektrode minus Cz |
| Durchschnittsreferenz (19 Kanäle) | Elektrode minus Mittelwert der Kopfelektroden ohne Fp1, Fp2 und Ohren |
| Quellenableitung (19 Kanäle) | Elektrode minus Mittelwert ihrer drei bis vier direkten Nachbarn (nach Hjorth, gleiche Gewichte) |
| Verstärker-Prinzip | P3 und O1 einzeln gegen den Bezugspunkt des Verstärkers (mit Gleichtakt, 1:20, ungefiltert), darunter P3-O1 |

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
| Kanalfolgen von Längsreihe, Querreihe, Ohr- und Vertexreferenz; bipolar und referenziell beurteilen; Average ohne Fp1/Fp2 und Ohren und „keine physikalische Referenz“; T3/T5/T4/T6 entsprechen T7/P7/T8/P8 | DGKN, Empfehlung zu EEG-Ableiteprogrammen (Fassung Oktober 2024, im Volltext geprüft) |
| Registrierung digital ohne zusätzliche Filter; Übergangswiderstände 1–10 kΩ, möglichst gleichmäßig, vor, nach und bei Artefakten während der Ableitung dokumentieren; Programme während der Ableitung wechseln | DGKN, Empfehlungen für die Durchführung von EEG-Ableitungen bei Erwachsenen (Fassung Oktober 2024, im Volltext geprüft) |
| Bipolar hebt umschriebene Befunde hervor, löscht Ausgebreitetes; Referenz zeigt Größe, kann „verschmutzt“ sein; Wahl von Ohr oder Cz je nach Ort des Befundes; Laplace für umschriebene Entladungen | Kutluay & Kalamangalam, J Clin Neurophysiol 2019 (im Volltext geprüft); ACNS Guideline 3 (2016, nur in Auszügen geprüft) |
| Quellenableitung: Elektrode gegen ihre Nachbarn | Hjorth, Electroencephalogr Clin Neurophysiol 1975 (nur über Sekundärquellen geprüft) |
| Gleichtaktunterdrückung mindestens 80 dB (10 000 : 1), moderne Geräte mindestens 100 dB; sie hängt vom Eingangswiderstand des Verstärkers und von der Elektrodenimpedanz ab; Aufzeichnung gegen eine Referenz außerhalb des 10/20-Systems (typischerweise CPz); jede gesetzte Elektrode soll in der Ableitemontage sichtbar sein | Baier, Kap. B-1.2 in Bischoff & Straube 2014 (im Volltext geprüft) |
| Alpha-Amplitude am einfachsten in Cz-Referenzableitung messen; Polarität okzipitaler Wellen gegen Cz leichter zu entscheiden als bipolar; Vertexwellen und K-Komplexe mit Phasenumkehr bei Cz | Rosenow, Kap. B-1.3, ebenda (im Volltext geprüft) |
| Ohrelektroden besonders störanfällig, Cz-Referenz weniger; Spannungsdifferenz wächst mit dem Elektrodenabstand | Kap. A-1, ebenda (im Volltext geprüft) |
| Gleichtaktunterdrückung hängt vom Verhältnis Elektrodenimpedanz zu Eingangswiderstand ab; bei hohen Impedanzen werden die Unterschiede zwischen den Elektroden größer; Verstärker messen jeden Eingang gegen die Erde | Kappenman & Luck, Psychophysiology 2010 (im Volltext geprüft) |
| Gleichtaktstörung bei geerdetem Körper im Millivolt-Bereich, sonst um 1 V; Gleichtaktunterdrückung mindestens 80 dB; Störströme erzeugen Spannungsabfall an den Elektroden | Usakli, Comput Intell Neurosci 2010 (im Volltext geprüft) |
| Weiterführend zur Störeinkopplung (nicht im Volltext geprüft) | Metting van Rijn et al., Med Biol Eng Comput 1990; Huhta & Webster 1973 |
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
| Gelbrücke F3–C3 | Beide Elektroden liegen fast auf demselben Potential (Mittelwert, 8 % des Unterschieds bleiben); wirkt in allen Kanälen mit F3 oder C3 | Lehrbuchbeschreibung der Salzbrücke (flacher Kanal) |

Empfehlung in den Lektionen: unter 10 kΩ und möglichst gleichmäßig, nicht unter 1 kΩ. Der Impedanztest soll vor und nach der Ableitung mit aufgezeichnet werden, bei Artefakten auch zwischendurch; die meisten Geräte speichern ihn automatisch in der Aufnahme.

Grenzwerte im Kopfschema: grün 1–5 kΩ, gelb bis 10 kΩ, rot über 10 kΩ, blau unter 1 kΩ (DGKN 2024: 1–10 kΩ, möglichst gleichmäßig; ACNS: Ziel 5 kΩ, bis 10 kΩ akzeptabel, nicht unter 100 Ω).

### Bewusste Vereinfachungen

- Die Stärke der Netzeinstreuung hängt in Wirklichkeit stark von der Umgebung ab. Der Wert von 2 µV pro kΩ ist ein Beispiel, kein Messwert.
- Die Schwellen, ab denen Unruhe (5 kΩ) und Pops (10 kΩ) einsetzen, sind didaktisch gewählt. In Wirklichkeit hängen Pops vom lockeren Sitz ab, nicht von einem festen Impedanzwert.
- Wie sich die Bausteine über die Elektroden verteilen, ist typisch gewählt, aber kein Kopfmodell: Es gibt keine Volumenleitung und keine Dipolrechnung.
- Die Gleichtaktstörung an den Verstärkereingängen (1000 µV Netz, langsames Wandern) ist ein Beispielwert. Der Verstärker selbst ist ideal gerechnet: Gleiches fällt vollständig heraus, der Rest der Störung entsteht nur über die Elektrodenimpedanzen.
- Die Quellenableitung benutzt gleiche Gewichte für die direkten Nachbarn; Geräte rechnen unterschiedlich.

- Die Flankensteilheit der Filter ist geräteabhängig. Hier: LowCut 6 dB/Oktave (wie ein RC-Glied), HiCut 12 dB/Oktave.
- Die Signale sind synthetisch. Amplituden und Verteilung sind typisch gewählt, bilden aber keinen einzelnen Patienten ab.
- Die Lektionen zu Filtern und Impedanzen zeigen der Übersicht halber nur einen Ausschnitt der Längsreihe (parasagittale Ketten rechts und links plus EKG). Die vollständigen Programme mit mindestens 18 Kanälen gibt es im Kapitel Verschaltungen und im freien Simulator.
- Alpha und Augenblinzeln lassen sich im freien Simulator gleichzeitig einschalten, obwohl das in einer echten Ableitung meist nicht zusammen vorkommt. Die Live-Erklärung weist darauf hin.
- Die Balken „So viel kommt durch“ werden am simulierten Baustein gemessen (Effektivwert, bei Spikes, Blinzeln und EKG die Spitzenhöhe).
