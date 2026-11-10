# Konzept: EEG-Filter verständlich machen

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
| 50-Hz-Filter | Kerbe genau bei 50 Hz | Elektrode als „Antenne“, Ursache: Impedanz |
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
- Bausteine, einzeln zuschaltbar:

| Baustein | Eigenschaft |
|---|---|
| Delta-Herd | links zentral, mit Phasenumkehr |
| Theta | – |
| Alpha | okzipital, an- und abschwellend |
| Beta | – |
| Spike-Wave | Herd C3 |
| Augenblinzeln | – |
| Schwitzartefakt | – |
| EMG | schubweise, frontal |
| 50-Hz-Brumm | schlechte Elektrode P4 |
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
