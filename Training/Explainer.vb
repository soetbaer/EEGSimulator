Option Strict On

Imports System.Drawing
Imports NeuroSim.Dsp
Imports NeuroSim.Simulation
Imports NeuroSim.UI

Namespace Training

    ''' <summary>Erzeugt die Live-Erklärung "Was passiert gerade?" passend zur aktuellen Einstellung.</summary>
    Public Module Explainer

        ''' <summary>
        ''' Misst am simulierten Baustein selbst, wie viel nach der Filterkette übrig bleibt (0 … 1).
        ''' Bei kurzen Einzelereignissen (Spikes, Blinzeln, EKG) zählt die Spitzenhöhe,
        ''' bei fortlaufender Aktivität der Effektivwert.
        ''' </summary>
        Public Function MeasurePassThrough(sim As EegSimulator, part As SignalPart, chain As FilterChain) As Double
            Dim raw = sim.PartSignal(part)
            Dim filt = CType(raw.Clone(), Double())
            chain.ProcessInPlace(filt)
            Dim usePeak = (part = SignalPart.Spikes OrElse part = SignalPart.Blinks OrElse part = SignalPart.Ecg)
            Dim start = CInt(5 * EegSimulator.SampleRate)       ' Einschwingen des Filters überspringen
            Dim a = 0.0, b = 0.0
            For i = start To raw.Length - 1
                If usePeak Then
                    a = Math.Max(a, Math.Abs(raw(i)))
                    b = Math.Max(b, Math.Abs(filt(i)))
                Else
                    a += raw(i) * raw(i)
                    b += filt(i) * filt(i)
                End If
            Next
            If a <= 0 Then Return 1.0
            Return If(usePeak, b / a, Math.Sqrt(b / a))
        End Function

        ''' <param name="ecgRemaining">Gemessener Rest des EKG-Artefakts nach dem EKG-Filter (0 … 1) oder Nothing.</param>
        Public Function Describe(s As SimSettings, chain As FilterChain, sim As EegSimulator,
                                 beatsDetected As Integer, ecgRemaining As Double?) As List(Of InfoBlock)
            Dim list As New List(Of InfoBlock)
            Dim has = Function(p As SignalPart) s.Parts.Contains(p)

            ' Messwerte für alle eingeschalteten Bausteine
            Dim pass As New Dictionary(Of SignalPart, Double)
            For Each p In s.Parts
                pass(p) = MeasurePassThrough(sim, p, chain)
            Next
            If has(SignalPart.Ecg) AndAlso s.EcgFilter AndAlso ecgRemaining.HasValue Then pass(SignalPart.Ecg) = ecgRemaining.Value

            list.Add(InfoBlock.Heading("Was passiert gerade?"))

            ' ---------- LowCut ----------
            If s.LowCutTc <= 0 Then
                list.Add(InfoBlock.Para("LowCut (FU) ist AUS: Auch extrem langsame Schwankungen werden voll angezeigt."))
                If has(SignalPart.Sweat) Then
                    list.Add(InfoBlock.Warning("Ohne LowCut wandert die Grundlinie durch das Schwitzartefakt weit nach oben und unten – die Kurven laufen ineinander."))
                End If
            Else
                Dim f = s.LowCutHz
                list.Add(InfoBlock.Para($"LowCut {Hz(f)} Hz (TC {Tc(s.LowCutTc)} s): Wellen, die langsamer sind als etwa {Hz(f)} Wellen pro Sekunde, werden abgeschwächt – je langsamer, desto stärker."))
                If f > 1.0 Then
                    list.Add(InfoBlock.Warning("Diese Einstellung liegt über den Empfehlungen für die Routine-Ableitung (DGKN: höchstens 0,5 Hz ≈ TC 0,3 s; Lehrbuch und ACNS: höchstens 1 Hz). Standard-Filter allenfalls vorübergehend verändern und dokumentieren."))
                End If
                If f >= 1.5 AndAlso (has(SignalPart.Delta) OrElse has(SignalPart.Blinks)) Then
                    list.Add(InfoBlock.Warning("Langsame Hirnwellen (Delta) werden deutlich kleiner – ein Herdbefund kann übersehen werden. Das Augenblinzeln wird schmaler und bekommt einen Gegenschwinger in die andere Richtung."))
                ElseIf has(SignalPart.Sweat) AndAlso pass(SignalPart.Sweat) > 0.5 Then
                    list.Add(InfoBlock.Tip("Das Schwitzartefakt ist noch deutlich sichtbar. Zuerst die Ursache angehen: Raum kühlen, Kopfhaut und Stirn trocknen. Ein stärkerer LowCut beruhigt die Grundlinie, dämpft aber auch echte langsame Hirnwellen."))
                End If
            End If

            ' ---------- HiCut ----------
            If s.HighCutHz <= 0 Then
                list.Add(InfoBlock.Para("HiCut (FO) ist AUS: Auch sehr schnelle Anteile werden voll angezeigt."))
                If has(SignalPart.Emg) Then list.Add(InfoBlock.Tip("Muskelartefakte sind jetzt ungefiltert in voller Stärke sichtbar."))
            Else
                list.Add(InfoBlock.Para($"HiCut {Hz(s.HighCutHz)} Hz: Wellen, die schneller sind als etwa {Hz(s.HighCutHz)} Wellen pro Sekunde, werden abgeschwächt – je schneller, desto stärker."))
                If s.HighCutHz < 70 Then
                    list.Add(InfoBlock.Warning("Diese Einstellung liegt unter den Empfehlungen für die Routine-Ableitung (DGKN und ACNS: mindestens 70 Hz). Nur vorübergehend zur Betrachtung verwenden und dokumentieren."))
                End If
                If s.HighCutHz <= 35 AndAlso has(SignalPart.Spikes) Then
                    list.Add(InfoBlock.Warning("Niedriger HiCut: Spikes werden abgerundet und kleiner – sie können dann wie harmlose Wellen aussehen."))
                End If
                If s.HighCutHz <= 35 AndAlso has(SignalPart.Emg) Then
                    list.Add(InfoBlock.Warning("Gefiltertes Muskelartefakt kann wie Beta-Aktivität oder wie steile Wellen aussehen! Besser: Patient entspannen lassen (Mund leicht öffnen, Stirn locker)."))
                End If
                If s.HighCutHz <= 20 AndAlso has(SignalPart.Beta) Then
                    list.Add(InfoBlock.Warning("Echte Beta-Aktivität wird bei diesem HiCut deutlich gedämpft."))
                End If
            End If

            ' ---------- Impedanzen ----------
            Dim z = CType(s.Impedances.Clone(), Double())
            If s.SaltBridge Then
                For Each e In EegSimulator.ChannelElectrodes(EegSimulator.SaltBridgeChannel) : z(e) = 0.5 : Next
            End If
            Dim worstCh = 0, worstDiff = 0.0
            For c = 0 To EegSimulator.ChannelElectrodes.Length - 1
                Dim d = Math.Abs(z(EegSimulator.ChannelElectrodes(c)(0)) - z(EegSimulator.ChannelElectrodes(c)(1)))
                If d > worstDiff Then worstDiff = d : worstCh = c
            Next
            Dim humUv = worstDiff * EegSimulator.HumMicrovoltPerKiloOhm
            Dim bad = Enumerable.Range(0, z.Length).Where(Function(i) z(i) > 10).ToList()
            Dim mid = Enumerable.Range(0, z.Length).Where(Function(i) z(i) > 5 AndAlso z(i) <= 10).ToList()
            Dim zLabel = Function(i As Integer) $"{EegSimulator.ElectrodeNames(i)} ({z(i).ToString("0", De)} kΩ)"

            If bad.Count > 0 Then
                list.Add(InfoBlock.Warning($"Impedanz zu hoch: {String.Join(", ", bad.Select(zLabel))}. Betroffen sind alle Kanäle mit dieser Elektrode: unruhige Grundlinie und plötzliche Sprünge (Elektroden-Pops), in Nachbarkanälen spiegelbildlich. Das lässt sich nicht wegfiltern – Elektrode neu setzen."))
                Dim zMax = bad.Max(Function(i) z(i))
                Dim lossPct = zMax / (100000.0 + zMax) * 100
                Dim thermalUv = Math.Sqrt(4 * 1.380649E-23 * 310 * zMax * 1000 * 70) * 1000000.0
                list.Add(InfoBlock.Para($"Zur Einordnung: Kleiner wird das EEG dadurch nicht – bei {zMax.ToString("0", De)} kΩ gehen an einem Verstärker mit 100 MΩ Eingangswiderstand nur etwa {lossPct.ToString("0.00", De)} % verloren. Auch das reine Widerstandsrauschen ist winzig (rechnerisch ca. {thermalUv.ToString("0.0", De)} µV). Das Problem ist der instabile, störanfällige Kontakt."))
            ElseIf mid.Count > 0 Then
                list.Add(InfoBlock.Tip($"Impedanz grenzwertig: {String.Join(", ", mid.Select(zLabel))}. Bis 10 kΩ ist noch zulässig, Ziel sind höchstens 5 kΩ und möglichst gleiche Werte an allen Elektroden."))
            End If
            If s.SaltBridge Then
                list.Add(InfoBlock.Warning("Gelbrücke zwischen F3 und C3: Elektrodenpaste verbindet beide Elektroden leitend. Sie messen fast dasselbe, der Kanal F3-C3 wird nahezu flach. Das kann wie eine echte Abflachung fehlgedeutet werden! Warnzeichen: Impedanz unter 1 kΩ."))
            End If

            ' ---------- 50 Hz ----------
            If s.Notch Then
                list.Add(InfoBlock.Para("50-Hz-Filter ist AN: Ein schmaler Bereich um 50 Hz wird gezielt herausgeschnitten, alles andere bleibt fast unverändert. Achtung: Bei einer Standard-Ableitung ist der routinemäßige Einsatz nicht zulässig, weil er Elektroden- und Kabeldefekte verdeckt."))
            End If
            If has(SignalPart.Hum) Then
                Dim chName = EegSimulator.ChannelNames(worstCh)
                If humUv < 6 Then
                    list.Add(InfoBlock.Good($"Netzeinstreuung ist vorhanden, aber kaum sichtbar: Die Impedanzen sind niedrig und fast gleich (größter Unterschied in einem Kanal: {worstDiff.ToString("0", De)} kΩ). Der Verstärker kann die Störung unterdrücken."))
                ElseIf s.Notch Then
                    list.Add(InfoBlock.Good($"Der Netzbrumm (am stärksten in {chName}) ist herausgefiltert. Die Ursache bleibt aber bestehen: {worstDiff.ToString("0", De)} kΩ Impedanz-Unterschied zwischen den beiden Elektroden dieses Kanals."))
                Else
                    list.Add(InfoBlock.Tip($"Netzbrumm sichtbar (dicke, 'pelzige' Kurve), am stärksten in {chName}: Die beiden Elektroden dieses Kanals unterscheiden sich um {worstDiff.ToString("0", De)} kΩ. Erst die Elektrode verbessern, dann ggf. den 50-Hz-Filter einschalten."))
                End If
            End If

            ' ---------- EKG ----------
            If s.EcgFilter Then
                If beatsDetected < 3 Then
                    list.Add(InfoBlock.Warning("EKG-Filter: Im EKG-Kanal wurden nicht genügend Herzschläge erkannt – der Filter kann nicht arbeiten."))
                Else
                    Dim bpm = beatsDetected / EegSimulator.Duration * 60
                    list.Add(InfoBlock.Para($"EKG-Filter ist AN: {beatsDetected} Herzschläge erkannt (rote Dreiecke, ca. {bpm:0} Schläge/min). Für jeden Kanal wird die gemittelte Artefakt-Schablone bei jedem Schlag abgezogen."))
                    If has(SignalPart.Ecg) Then
                        list.Add(InfoBlock.Good("Die EKG-Zacken in den EEG-Kanälen sind weitgehend entfernt – die Hirnaktivität bleibt erhalten."))
                    Else
                        list.Add(InfoBlock.Para("Im Moment ist kein EKG-Artefakt eingeschaltet – der Filter hat kaum etwas zu tun."))
                    End If
                End If
            ElseIf has(SignalPart.Ecg) Then
                list.Add(InfoBlock.Tip("EKG-Artefakt sichtbar: kleine, regelmäßige Zacken genau im Takt der R-Zacken im EKG-Kanal (unten). Der EKG-Filter kann sie entfernen."))
            End If

            ' ---------- Amplitude ----------
            list.Add(InfoBlock.Para($"Amplitude {SensLabel(s.Sensitivity)}: Eine Welle von {(s.Sensitivity * 10).ToString("0", De)} µV wird 1 cm hoch gezeichnet. (Die Amplitude verändert nur die Darstellung, nicht das Signal.)"))

            ' ---------- Balken: Was kommt durch? ----------
            Dim brain = PartCatalog.All.Where(Function(d) d.IsBrain AndAlso has(d.Part)).ToList()
            Dim arts = PartCatalog.All.Where(Function(d) Not d.IsBrain AndAlso has(d.Part)).ToList()
            If brain.Count > 0 OrElse arts.Count > 0 Then
                list.Add(InfoBlock.Divider())
                list.Add(InfoBlock.Heading("So viel kommt durch den Filter"))
                list.Add(InfoBlock.Para("Am simulierten Signal gemessen: So viel Prozent der Wellenhöhe bleiben nach dem Filtern übrig."))
            End If
            If brain.Count > 0 Then
                list.Add(InfoBlock.Subtitle("Hirnaktivität – soll erhalten bleiben"))
                For Each d In brain
                    Dim v = pass(d.Part)
                    Dim col = If(v >= 0.8, Theme.GoodAccent, If(v >= 0.5, Theme.TipAccent, Theme.WarnAccent))
                    list.Add(InfoBlock.Meter(d.Label, v, col))
                Next
            End If
            If arts.Count > 0 Then
                list.Add(InfoBlock.Subtitle("Artefakte – sollen möglichst weg"))
                For Each d In arts
                    Dim v = pass(d.Part)
                    Dim col = If(v <= 0.3, Theme.GoodAccent, If(v <= 0.7, Theme.TipAccent, Theme.WarnAccent))
                    list.Add(InfoBlock.Meter(d.Label, v, col))
                Next
            End If
            Return list
        End Function

    End Module

End Namespace
