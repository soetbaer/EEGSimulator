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

            ' ---------- Verschaltung ----------
            Dim montage = s.Montage
            list.Add(InfoBlock.Para($"Verschaltung »{montage.Name}«: {montage.Summary}"))
            AddMontageHints(list, s, montage)

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
                    ' nur das nennen, was gerade eingeschaltet ist
                    Dim effects As New List(Of String)
                    If has(SignalPart.Delta) Then effects.Add("Langsame Hirnwellen (Delta) werden deutlich kleiner – ein Herdbefund kann übersehen werden.")
                    If has(SignalPart.Blinks) Then effects.Add("Das Augenblinzeln wird schmaler und bekommt einen Gegenschwinger in die andere Richtung.")
                    list.Add(InfoBlock.Warning(String.Join(" ", effects)))
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
                If has(SignalPart.Spikes) Then
                    ' Nur den HiCut messen, damit die Aussage nicht vom LowCut abhängt
                    Dim hiOnly = FilterChain.Create(0, s.HighCutHz, False, EegSimulator.SampleRate)
                    Dim spikeRest = MeasurePassThrough(sim, SignalPart.Spikes, hiOnly)
                    If spikeRest < 0.8 Then
                        list.Add(InfoBlock.Warning($"Niedriger HiCut: Spikes werden abgerundet und verlieren etwa {((1 - spikeRest) * 100).ToString("0", De)} % ihrer Höhe – sie können dann wie harmlose Wellen aussehen."))
                    End If
                End If
                If s.HighCutHz <= 35 AndAlso has(SignalPart.Emg) Then
                    list.Add(InfoBlock.Warning("Gefiltertes Muskelartefakt kann wie Beta-Aktivität oder wie steile Wellen aussehen! Besser: Patient entspannen lassen (Mund leicht öffnen, Stirn locker)."))
                End If
                If s.HighCutHz <= 20 AndAlso has(SignalPart.Beta) Then
                    list.Add(InfoBlock.Warning("Echte Beta-Aktivität wird bei diesem HiCut deutlich gedämpft."))
                End If
            End If

            ' ---------- Impedanzen ----------
            ' Im Kanal bleibt die gewichtete Differenz der Elektroden-Störspannungen übrig:
            ' bei zwei Elektroden der Impedanz-Unterschied, bei Durchschnitt/Quelle der Unterschied zum Mittelwert.
            Dim z = EegSimulator.EffectiveImpedances(s.Impedances, s.SaltBridge)
            Dim worstCh = -1, worstDiff = 0.0
            For c = 0 To montage.Channels.Count - 1
                If montage.Channels(c).Kind <> ChannelKind.Eeg Then Continue For
                Dim d = Math.Abs(montage.Channels(c).Combine(z))
                If worstCh < 0 OrElse d > worstDiff Then worstDiff = d : worstCh = c
            Next
            Dim humUv = worstDiff * EegSimulator.HumMicrovoltPerKiloOhm
            Dim bad = Enumerable.Range(0, z.Length).Where(Function(i) z(i) > 10).ToList()
            Dim mid = Enumerable.Range(0, z.Length).Where(Function(i) z(i) > 5 AndAlso z(i) <= 10).ToList()
            Dim zLabel = Function(i As Integer) $"{Head.Names(i)} ({z(i).ToString("0", De)} kΩ)"

            If bad.Count > 0 Then
                ' Wo die Störung einer einzelnen Elektrode erscheint, hängt von der Verschaltung ab
                Dim spread As String
                Select Case montage.Kind
                    Case MontageKind.Bipolar
                        spread = "Betroffen sind die Kanäle mit dieser Elektrode, in den beiden Nachbarkanälen einer Kette spiegelbildlich (einmal ist sie Eingang 2, einmal Eingang 1)."
                    Case MontageKind.Reference
                        spread = "Betroffen ist der Kanal dieser Elektrode, ohne Spiegelbild. Ist es die Bezugselektrode, erscheint die Störung umgekehrt in allen Kanälen dieses Bezugs."
                    Case MontageKind.Average
                        spread = "Die Störung erscheint voll im Kanal dieser Elektrode. Geht sie in den Durchschnitt ein, taucht die Störung außerdem klein und umgekehrt in allen anderen Kanälen auf."
                    Case MontageKind.Source
                        spread = "Die Störung erscheint voll im Kanal dieser Elektrode und kleiner, umgekehrt in den Kanälen ihrer Nachbarelektroden."
                    Case Else
                        spread = "Betroffen sind die Kanäle mit dieser Elektrode."
                End Select
                list.Add(InfoBlock.Warning($"Impedanz zu hoch: {String.Join(", ", bad.Select(zLabel))}. Folgen: unruhige Grundlinie und plötzliche Sprünge (Elektroden-Pops). {spread} Das lässt sich nicht wegfiltern – Elektrode neu setzen."))
                Dim zMax = bad.Max(Function(i) z(i))
                Dim lossPct = zMax / (100000.0 + zMax) * 100
                Dim thermalUv = Math.Sqrt(4 * 1.380649E-23 * 310 * zMax * 1000 * 70) * 1000000.0
                list.Add(InfoBlock.Para($"Zur Einordnung: Kleiner wird das EEG dadurch nicht – bei {zMax.ToString("0", De)} kΩ gehen an einem Verstärker mit 100 MΩ Eingangswiderstand nur etwa {lossPct.ToString("0.00", De)} % verloren. Auch das reine Widerstandsrauschen ist winzig (rechnerisch ca. {thermalUv.ToString("0.0", De)} µV). Das Problem ist der instabile, störanfällige Kontakt."))
            ElseIf mid.Count > 0 Then
                list.Add(InfoBlock.Tip($"Impedanz erhöht: {String.Join(", ", mid.Select(zLabel))}. Bis 10 kΩ ist noch zulässig – besser sind niedrigere und vor allem möglichst gleiche Werte an allen Elektroden."))
            End If
            If s.SaltBridge Then
                list.Add(InfoBlock.Warning("Gelbrücke zwischen F3 und C3: Elektrodenpaste verbindet beide Elektroden leitend. Sie messen fast dasselbe: Ein Kanal zwischen F3 und C3 wird nahezu flach, und in allen anderen Kanälen zeigen F3 und C3 dieselbe Kurve. Das kann wie eine echte Abflachung fehlgedeutet werden! Warnzeichen: Impedanz unter 1 kΩ."))
            End If

            ' ---------- 50 Hz ----------
            If s.Notch Then
                list.Add(InfoBlock.Para("50-Hz-Filter ist AN: Ein schmaler Bereich um 50 Hz wird gezielt herausgeschnitten, alles andere bleibt fast unverändert. Achtung: Bei einer Standard-Ableitung ist der routinemäßige Einsatz nicht zulässig, weil er Elektroden- und Kabeldefekte verdeckt."))
            End If
            If has(SignalPart.Hum) AndAlso worstCh >= 0 Then
                Dim worst = montage.Channels(worstCh)
                Dim chName = worst.Label
                Dim diffText = worstDiff.ToString("0", De)
                ' bei zwei Elektroden ist es ihr Unterschied, sonst der Abstand zum Mittelwert des Bezugs
                Dim cause = If(worst.Input2.Length = 1,
                               $"Die beiden Elektroden dieses Kanals unterscheiden sich um {diffText} kΩ",
                               $"Die Impedanz von {Head.Names(worst.Input1)} weicht um {diffText} kΩ vom Mittelwert ihres Bezugs ab")
                If humUv < 6 Then
                    If bad.Count = 0 Then
                        list.Add(InfoBlock.Good($"Netzeinstreuung ist vorhanden, aber kaum sichtbar: Die Impedanzen sind niedrig und fast gleich (größter Unterschied in einem Kanal: {diffText} kΩ). Der Verstärker kann die Störung unterdrücken."))
                    Else
                        ' gleich hohe Impedanzen: der Brumm hebt sich auf, die Elektroden bleiben trotzdem schlecht
                        list.Add(InfoBlock.Para($"Netzbrumm ist kaum sichtbar, weil die Elektroden innerhalb der Kanäle fast gleiche Impedanzen haben (größter Unterschied in einem Kanal: {diffText} kΩ). Die zu hohen Werte bleiben trotzdem ein Problem: Unruhe und Pops lassen sich so nicht beheben."))
                    End If
                ElseIf s.Notch Then
                    list.Add(InfoBlock.Good($"Der Netzbrumm (am stärksten in {chName}) ist herausgefiltert. Die Ursache bleibt aber bestehen: {cause}."))
                Else
                    list.Add(InfoBlock.Tip($"Netzbrumm sichtbar (dicke, 'pelzige' Kurve), am stärksten in {chName}: {cause}. Erst die Elektrode verbessern, dann ggf. den 50-Hz-Filter einschalten."))
                End If
            End If

            ' ---------- EKG ----------
            If s.EcgFilter Then
                If beatsDetected < 3 Then
                    list.Add(InfoBlock.Warning("EKG-Filter: Im EKG-Kanal wurden nicht genügend Herzschläge erkannt – der Filter kann nicht arbeiten."))
                Else
                    Dim bpm = beatsDetected / EegSimulator.Duration * 60
                    list.Add(InfoBlock.Para($"EKG-Filter ist AN: {beatsDetected} Herzschläge erkannt (rote Dreiecke, ca. {bpm:0} Schläge/min). Für jeden Kanal wird die gemittelte Artefakt-Schablone bei jedem Schlag abgezogen."))
                    If has(SignalPart.Ecg) AndAlso ecgRemaining.HasValue Then
                        ' Aussage nach dem gemessenen Rest des Artefakts im am stärksten betroffenen Kanal
                        Dim restPct = (ecgRemaining.Value * 100).ToString("0", De)
                        If ecgRemaining.Value <= 0.3 Then
                            list.Add(InfoBlock.Good($"Die EKG-Zacken in den EEG-Kanälen sind weitgehend entfernt (gemessener Rest etwa {restPct} % im am stärksten betroffenen Kanal) – die Hirnaktivität bleibt erhalten."))
                        ElseIf ecgRemaining.Value <= 0.6 Then
                            list.Add(InfoBlock.Tip($"Die EKG-Zacken sind kleiner geworden, aber noch erkennbar (gemessener Rest etwa {restPct} %). Starke andere Störungen erschweren es, die Schablone sauber zu bilden."))
                        Else
                            list.Add(InfoBlock.Warning($"Der EKG-Filter entfernt das Artefakt hier kaum (gemessener Rest etwa {restPct} %). Erst die anderen Störungen beheben."))
                        End If
                    ElseIf has(SignalPart.Ecg) Then
                        list.Add(InfoBlock.Para("Das EKG-Artefakt ist eingeschaltet; die Schablone wird bei jedem Herzschlag abgezogen."))
                    Else
                        list.Add(InfoBlock.Para("Im Moment ist kein EKG-Artefakt eingeschaltet – der Filter hat kaum etwas zu tun."))
                    End If
                End If
            ElseIf has(SignalPart.Ecg) Then
                list.Add(InfoBlock.Tip("EKG-Artefakt sichtbar: kleine, regelmäßige Zacken genau im Takt der R-Zacken im EKG-Kanal (unten). Der EKG-Filter kann sie entfernen."))
            End If

            ' ---------- Augen auf / zu ----------
            If has(SignalPart.Alpha) AndAlso has(SignalPart.Blinks) Then
                list.Add(InfoBlock.Para("Hinweis: Augenblinzeln tritt bei offenen Augen auf, der Alpha-Rhythmus vor allem bei geschlossenen. In einer echten Ableitung sieht man beides meist nicht gleichzeitig."))
            End If

            ' ---------- Amplitude ----------
            list.Add(InfoBlock.Para($"Amplitude {SensLabel(s.Sensitivity)}: Eine Welle von {(s.Sensitivity * 10).ToString("0", De)} µV wird 1 cm hoch gezeichnet. (Die Amplitude verändert nur die Darstellung der EEG-Kanäle, nicht das Signal. Das EKG hat eine eigene Größe: Klick auf die EKG-Kurve.)"))

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

        ''' <summary>Hinweise, die von der Verschaltung UND vom eingeschalteten Signal abhängen.</summary>
        Private Sub AddMontageHints(list As List(Of InfoBlock), s As SimSettings, montage As Montage)
            Dim has = Function(p As SignalPart) s.Parts.Contains(p)
            Dim focus = has(SignalPart.Spikes) OrElse has(SignalPart.Delta)
            Select Case montage.Id
                Case Montages.Longitudinal, Montages.Transverse, Montages.Excerpt
                    If focus Then
                        list.Add(InfoBlock.Tip("Bipolare Reihe: Der Herd unter C3 zeigt sich als Phasenumkehr – in den beiden Kanälen, in denen C3 vorkommt, schlagen die Kurven in entgegengesetzte Richtung aus."))
                    End If
                Case Montages.Ear
                    If has(SignalPart.Alpha) Then
                        list.Add(InfoBlock.Warning("Ohrreferenz: In dieser Simulation liegt am Ohr ein Teil des Alpha-Rhythmus an. Deshalb erscheint er klein auch in den vorderen Kanälen (z. B. Fp1-A1) – er stammt aus der Referenz, nicht von der Stirn."))
                    End If
                    If has(SignalPart.Ecg) Then
                        list.Add(InfoBlock.Warning("Ohrreferenz: Das EKG-Artefakt ist in dieser Simulation hier am größten, links stärker als rechts. In der Längsreihe ist es viel kleiner."))
                    End If
                    If focus Then
                        list.Add(InfoBlock.Tip("Referenzableitung: Der größte Ausschlag zeigt direkt die Elektrode über dem Herd (C3-A1). Eine Phasenumkehr gibt es hier nicht."))
                    End If
                Case Montages.Vertex
                    If focus Then
                        list.Add(InfoBlock.Warning("Vertexreferenz: Cz liegt im Feld des Herdes unter C3. C3-Cz erscheint deshalb kleiner, und in fast allen anderen Kanälen – auch rechts – taucht ein kleiner Ausschlag in Gegenrichtung auf. Er stammt aus der Referenz."))
                    End If
                Case Montages.Average
                    If has(SignalPart.Blinks) Then
                        list.Add(InfoBlock.Warning("Durchschnittsreferenz: Das große Augenblinzeln steckt über die frontalen Elektroden (F7, F3, Fz, F4, F8) im Durchschnitt. In allen Kanälen hinter der Stirn erscheint es dadurch umgekehrt, sogar okzipital."))
                    End If
                    If focus Then
                        list.Add(InfoBlock.Tip("Durchschnittsreferenz: Der Herd ist in C3-AV am größten. Weil er den Durchschnitt mitverschiebt, erscheint in den Kanälen abseits des Herdes ein kleiner Ausschlag in Gegenrichtung."))
                    End If
                Case Montages.Source
                    If has(SignalPart.Alpha) Then
                        list.Add(InfoBlock.Warning("Quellenableitung: Weit ausgebreitete Aktivität wie der Alpha-Rhythmus wird verkleinert, weil die Nachbarn Ähnliches messen."))
                    End If
                    If focus Then
                        list.Add(InfoBlock.Tip("Quellenableitung: Der Herd erscheint fast nur noch in C3-Q. In den Nachbarkanälen können kleine Ausschläge in Gegenrichtung entstehen."))
                    End If
                Case Montages.Amplifier
                    list.Add(InfoBlock.Para("Die beiden oberen Kurven sind die einzelnen Verstärkereingänge, gegen die Erdelektrode gemessen, 20-fach verkleinert und ungefiltert. Was an beiden gleich ist (Gleichtakt), fehlt in der dritten Kurve: Sie zeigt nur den Unterschied."))
            End Select
        End Sub

    End Module

End Namespace
