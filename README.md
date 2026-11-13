# EEG-Filter verstehen

Interaktive Schulungsanwendung (VB.NET, WinForms, .NET 8) für MTA in der Neurologie.
Sie zeigt an simulierten EEG-Kurven, wie sich diese Einstellungen auswirken:
- **LowCut (FU, mit Zeitkonstante)**
- **HiCut (FO)**
- **Amplitude (Empfindlichkeit)**
- **50-Hz-Filter**
- **Impedanzen der Elektroden** (Netzbrumm, unruhige Grundlinie, Elektroden-Pops, Gelbrücke)
- **EKG-Filter**

Das didaktische Konzept steht in [KONZEPT.md](KONZEPT.md).

## Bedienung

- **Geführte Lektionen** (12 Kapitel): links lesen, rechts ausprobieren. Mit „Weiter“ geht es zum nächsten Kapitel. „Lektion zurücksetzen“ stellt den Ausgangszustand wieder her.
- **Freier Simulator**: Alle Regler und Signal-Bausteine sind frei wählbar.
- Ein Klick auf einen **Kanalnamen** wählt den Kanal für die Frequenz-Ansicht.
- Die rechte Leiste hat drei Reiter: **Einstellungen** (Filter, Amplitude, Anzeige), **Impedanzen** und **Signal**. Die Lektionen öffnen jeweils den passenden Reiter.
- Im **Kopfschema** (Reiter „Impedanzen“) ändert ein Klick auf eine Elektrode deren Impedanz (Rechtsklick: zurück). Per Tastatur: Pfeiltasten wählen die Elektrode, `+` und `−` ändern den Wert.
- **Abspielen** lässt das EEG wie am Gerät laufen. Tastatur:
  - `P` startet und stoppt die Wiedergabe.
  - `←` / `→` blättern um 10 s zurück bzw. vor.

## Bauen und Starten

Voraussetzung ist Visual Studio 2022 (Workload „.NET-Desktopentwicklung“) oder das .NET 8 SDK unter Windows.

```
dotnet run
```

Oder in Visual Studio `EegFilterTrainer.vbproj` öffnen und F5 drücken.

Eine einzelne EXE ohne .NET-Installation auf dem Schulungsrechner erzeugen:

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Die EXE liegt danach in `bin\Release\net8.0-windows\win-x64\publish\`.

## Projektstruktur

| Ordner | Inhalt |
|---|---|
| `Dsp/` | Filter (Hochpass/Tiefpass/Notch), FFT, EKG-Template-Subtraktion |
| `Simulation/` | Generator des simulierten EEG mit allen Bausteinen und Artefakten |
| `Training/` | Einstellungen, Lektionstexte (`LessonCatalog.vb`), Live-Erklärungen (`Explainer.vb`) |
| `UI/` | Hauptfenster, EEG-Ansicht, Frequenz-Ansicht, Kopfschema für Impedanzen, Schalter, Info-Karte, Farben (`Theme.vb`) |

## Anpassen

- **Texte der Lektionen**: `Training/LessonCatalog.vb`. Jede Lektion hat Texte und eine Ausgangseinstellung (`Preset`).
- **Auswahllisten** (Zeitkonstanten, HiCut-Werte, µV/mm): `Training/SimSettings.vb` → `FilterText`
- **Laborstandards** (z. B. andere Standardwerte): Preset von Lektion 12 bzw. `SimSettings`-Vorgaben
- **Farben/Schriften**: `UI/Theme.vb`

## Hinweise

- Die Signale sind simuliert und didaktisch vereinfacht. Sie ersetzen keine Geräte-Einweisung.
- Wie groß „1 mm“ am Bildschirm ist, hängt von der Windows-Skalierung ab.
