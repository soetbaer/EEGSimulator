Überarbeitete Fassung nach fachlicher und technischer Prüfung.

## Neu
- Schreiblinie beim Abspielen: Die Seite bleibt stehen, die Linie schreibt die nächste Seite darüber, wie am EEG-Gerät.
- Die Tasten `+` und `−` ändern die Amplitude.
- Beim ersten Start fragt NeuroSim einmal, ob es nach neuen Versionen suchen darf. Ohne Zustimmung wird nichts abgefragt; mit Zustimmung höchstens einmal am Tag.
- Administratoren können die Update-Funktion abschalten (Datei `NeuroSim.noupdate` neben der EXE oder Registry-Wert, siehe README).

## Geändert
- Impedanz-Empfehlung: unter 10 kΩ und möglichst gleichmäßig; der Impedanztest soll mit aufgezeichnet werden.
- Spikes sind schärfer simuliert. Die Warnung zum HiCut nennt den gemessenen Höhenverlust.
- Lektion 4 zeigt offene Augen ohne Alpha; Alpha und Augenblinzeln sind in den Lektionen nicht mehr gemeinsam voreingestellt.
- Die Live-Erklärung zum LowCut und zum EKG-Filter richtet sich nach dem, was eingeschaltet ist, und nach dem gemessenen Ergebnis.
- Berufsbezeichnung: Medizinische Technologinnen und Technologen für Funktionsdiagnostik (MTF).

## Behoben
- Blättern liegt immer auf vollen 10 Sekunden; beim Abspielen wird die Aufnahme bis zum Ende gezeigt.
- Zeitanzeige im deutschen Zahlenformat.

## Installation
`NeuroSim.exe` herunterladen und starten. Keine Installation nötig, läuft auf Windows 10 und 11 (64 Bit). Wer Version 1.0.0 benutzt, kann im Info-Dialog (F1) über „Jetzt nach Updates suchen“ aktualisieren.

Die EXE ist nicht signiert. Zeigt Windows „Der Computer wurde durch Windows geschützt“, über **Weitere Informationen → Trotzdem ausführen** starten.

## Hinweis
NeuroSim dient ausschließlich der Aus-, Fort- und Weiterbildung. Die Kurven sind simuliert. NeuroSim ist kein Medizinprodukt. Lizenz: GPL-3.0-or-later.
