Option Strict On

Imports System.Drawing
Imports EegFilterTrainer.Dsp
Imports EegFilterTrainer.Simulation
Imports EegFilterTrainer.UI

Namespace Training

    ''' <summary>Erzeugt die Live-Erklärung "Was passiert gerade?" passend zur aktuellen Einstellung.</summary>
    Public Module Explainer

        Public Function Describe(s As SimSettings, chain As FilterChain, beatsDetected As Integer) As List(Of InfoBlock)
            Dim list As New List(Of InfoBlock)
            Dim has = Function(p As SignalPart) s.Parts.Contains(p)

            list.Add(InfoBlock.Heading("Was passiert gerade?"))

            ' ---------- LowCut ----------
            If s.LowCutTc <= 0 Then
                list.Add(InfoBlock.Para("LowCut (FU) ist AUS: Auch extrem langsame Schwankungen werden voll angezeigt."))
                If has(SignalPart.Sweat) Then
                    list.Add(InfoBlock.Warning("Ohne LowCut wandert die Grundlinie durch das Schwitzartefakt weit nach oben und unten – die Kurven laufen ineinander."))
                End If
            Else
                Dim f = s.LowCutHz
                list.Add(InfoBlock.Para($"LowCut {Hz(f)} Hz (TC {Tc(s.LowCutTc)} s): Wellen, die langsamer sind als etwa {Hz(f)} Wellen pro Sekunde, werden abgeschwächt."))
                If f >= 1.5 AndAlso (has(SignalPart.Delta) OrElse has(SignalPart.Blinks)) Then
                    list.Add(InfoBlock.Warning("Sehr starker LowCut! Langsame Hirnwellen (Delta) werden deutlich kleiner – ein Herdbefund kann übersehen werden. Das Augenblinzeln bekommt einen 'Unterschwinger'."))
                ElseIf f < 0.3 AndAlso has(SignalPart.Sweat) Then
                    list.Add(InfoBlock.Tip("Das Schwitzartefakt ist noch deutlich sichtbar. Eine kleinere Zeitkonstante (TC 0,3 s, vorübergehend 0,1 s) beruhigt die Grundlinie. Ursache beheben: Raum kühlen, Patient zudecken/abdecken prüfen."))
                End If
            End If

            ' ---------- HiCut ----------
            If s.HighCutHz <= 0 Then
                list.Add(InfoBlock.Para("HiCut (FO) ist AUS: Auch sehr schnelle Anteile werden voll angezeigt."))
                If has(SignalPart.Emg) Then list.Add(InfoBlock.Tip("Muskelartefakte sind jetzt ungefiltert in voller Stärke sichtbar."))
            Else
                list.Add(InfoBlock.Para($"HiCut {Hz(s.HighCutHz)} Hz: Wellen, die schneller sind als etwa {Hz(s.HighCutHz)} Wellen pro Sekunde, werden abgeschwächt."))
                If s.HighCutHz <= 30 AndAlso has(SignalPart.Spikes) Then
                    list.Add(InfoBlock.Warning("Niedriger HiCut: Spikes werden abgerundet und kleiner – sie können dann wie harmlose Wellen aussehen."))
                End If
                If s.HighCutHz <= 30 AndAlso has(SignalPart.Emg) Then
                    list.Add(InfoBlock.Warning("Weggefiltertes Muskelartefakt kann wie Beta-Aktivität aussehen! Besser: Patient entspannen lassen (Mund leicht öffnen, Stirn locker)."))
                End If
                If s.HighCutHz <= 20 AndAlso has(SignalPart.Beta) Then
                    list.Add(InfoBlock.Warning("Echte Beta-Aktivität wird bei diesem HiCut stark gedämpft."))
                End If
            End If

            ' ---------- 50 Hz ----------
            If s.Notch Then
                list.Add(InfoBlock.Para("50-Hz-Filter ist AN: Ein schmaler Bereich um 50 Hz wird gezielt herausgeschnitten, alles andere bleibt fast unverändert."))
                If has(SignalPart.Hum) Then
                    list.Add(InfoBlock.Good("Der Netzbrumm ist verschwunden. Trotzdem die Ursache prüfen: meist eine schlecht anliegende Elektrode (hier P4 – Impedanz messen!)."))
                End If
            ElseIf has(SignalPart.Hum) Then
                list.Add(InfoBlock.Tip("Netzbrumm sichtbar: dicke, 'pelzige' Kurven in C4-P4 und P4-O2 – beide Kanäle enthalten P4. Erst Elektrode prüfen, dann ggf. den 50-Hz-Filter einschalten."))
            End If

            ' ---------- EKG ----------
            If s.EcgFilter Then
                If beatsDetected < 3 Then
                    list.Add(InfoBlock.Warning("EKG-Filter: Im EKG-Kanal wurden nicht genügend Herzschläge erkannt – der Filter kann nicht arbeiten."))
                Else
                    Dim bpm = beatsDetected / EegSimulator.Duration * 60
                    list.Add(InfoBlock.Para($"EKG-Filter ist AN: {beatsDetected} Herzschläge erkannt (rote Dreiecke, ca. {bpm:0} Schläge/min). Für jeden Kanal wird die gemittelte Artefakt-Schablone bei jedem Schlag abgezogen."))
                    If has(SignalPart.Ecg) Then
                        list.Add(InfoBlock.Good("Die EKG-Zacken in den EEG-Kanälen sind entfernt – die Hirnaktivität bleibt erhalten."))
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
                list.Add(InfoBlock.Para("Wie viel Prozent der Wellenhöhe nach dem Filtern übrig bleibt:"))
            End If
            If brain.Count > 0 Then
                list.Add(InfoBlock.Subtitle("HIRNAKTIVITÄT – soll erhalten bleiben"))
                For Each d In brain
                    Dim v = chain.Magnitude(d.TypicalHz)
                    Dim col = If(v >= 0.8, Theme.GoodAccent, If(v >= 0.5, Theme.TipAccent, Theme.WarnAccent))
                    list.Add(InfoBlock.Meter($"{d.Label}", v, col))
                Next
            End If
            If arts.Count > 0 Then
                list.Add(InfoBlock.Subtitle("ARTEFAKTE – sollen möglichst weg"))
                For Each d In arts
                    Dim v As Double
                    If d.Part = SignalPart.Ecg Then
                        v = If(s.EcgFilter AndAlso beatsDetected >= 3, 0.1, chain.Magnitude(d.TypicalHz))
                    Else
                        v = chain.Magnitude(d.TypicalHz)
                    End If
                    Dim col = If(v <= 0.3, Theme.GoodAccent, If(v <= 0.7, Theme.TipAccent, Theme.WarnAccent))
                    Dim txt = If(d.Part = SignalPart.Ecg AndAlso s.EcgFilter AndAlso beatsDetected >= 3, "≈ 10 %", Nothing)
                    list.Add(InfoBlock.Meter(d.Label, v, col, txt))
                Next
            End If
            Return list
        End Function

    End Module

End Namespace
