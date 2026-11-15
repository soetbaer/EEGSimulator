Option Strict On

Namespace Simulation

    Public Enum ChannelKind
        ''' <summary>Gewöhnlicher EEG-Kanal: gewichtete Differenz von Elektroden.</summary>
        Eeg
        ''' <summary>EKG-Kanal.</summary>
        Ecg
        ''' <summary>
        ''' Einzelner Verstärkereingang gegen den Bezugspunkt des Verstärkers – mit der Gleichtaktstörung,
        ''' die an allen Elektroden gleich ist. Nur zur Erklärung des Differenzverstärkers.
        ''' </summary>
        SingleInput
    End Enum

    Public Enum MontageKind
        Bipolar
        Reference
        Average
        Source
        Amplifier
    End Enum

    ''' <summary>Ein Kanal einer Verschaltung: Summe aus Gewicht × Elektrodenpotential.</summary>
    Public NotInheritable Class Derivation
        Public Property Label As String = ""
        ''' <summary>Kleine zweite Zeile unter dem Kanalnamen (z. B. "Skala 1:10").</summary>
        Public Property SubLabel As String = ""
        Public Property Kind As ChannelKind = ChannelKind.Eeg
        ''' <summary>Elektrode an Eingang 1 (−1 beim EKG).</summary>
        Public Property Input1 As Integer = -1
        ''' <summary>Elektrode(n), mit denen verglichen wird: eine bei bipolar/Referenz, mehrere bei Durchschnitt/Quelle.</summary>
        Public Property Input2 As Integer() = Array.Empty(Of Integer)()
        Public Property Electrodes As Integer() = Array.Empty(Of Integer)()
        Public Property Weights As Double() = Array.Empty(Of Double)()
        ''' <summary>Die Kurve wird um diesen Faktor verkleinert gezeichnet (EKG, Verstärkereingänge).</summary>
        Public Property ScaleDivisor As Double = 1

        ''' <summary>Gewichtete Summe eines Wertes je Elektrode (z. B. Impedanz oder Feldstärke eines Bausteins).</summary>
        Public Function Combine(perElectrode As Double()) As Double
            Dim s = 0.0
            For k = 0 To Electrodes.Length - 1
                s += Weights(k) * perElectrode(Electrodes(k))
            Next
            Return s
        End Function

        Public Shared Function Pair(in1 As String, in2 As String) As Derivation
            Dim a = Head.Index(in1), b = Head.Index(in2)
            Return New Derivation With {.Label = $"{in1}-{in2}", .Input1 = a, .Input2 = {b},
                                        .Electrodes = {a, b}, .Weights = {1.0, -1.0}}
        End Function

        ''' <summary>Elektrode gegen den Mittelwert mehrerer Elektroden.</summary>
        Public Shared Function AgainstMean(in1 As String, others As Integer(), suffix As String) As Derivation
            Dim a = Head.Index(in1)
            Dim el As New List(Of Integer) From {a}
            Dim w As New List(Of Double) From {1.0}
            For Each o In others
                Dim pos = el.IndexOf(o)
                If pos >= 0 Then
                    w(pos) -= 1.0 / others.Length          ' die Elektrode selbst gehört mit zum Durchschnitt
                Else
                    el.Add(o) : w.Add(-1.0 / others.Length)
                End If
            Next
            Return New Derivation With {.Label = $"{in1}-{suffix}", .Input1 = a, .Input2 = others,
                                        .Electrodes = el.ToArray(), .Weights = w.ToArray()}
        End Function

        Public Shared Function SingleInput(electrode As String, role As String) As Derivation
            Dim a = Head.Index(electrode)
            Return New Derivation With {.Label = $"{electrode} (1:{AmplifierInputScale:0})", .SubLabel = role, .Kind = ChannelKind.SingleInput,
                                        .Input1 = a, .Electrodes = {a}, .Weights = {1.0}, .ScaleDivisor = AmplifierInputScale}
        End Function

        Public Shared Function Ecg() As Derivation
            Return New Derivation With {.Label = "EKG", .SubLabel = $"Skala 1:{EcgScale:0}", .Kind = ChannelKind.Ecg, .ScaleDivisor = EcgScale}
        End Function

        Public Const EcgScale As Double = 10
        Public Const AmplifierInputScale As Double = 20
    End Class

    ''' <summary>Eine Verschaltung (Montage, Ableiteprogramm): Liste der Kanäle von oben nach unten, das EKG zuletzt.</summary>
    Public NotInheritable Class Montage
        Public Property Id As String = ""
        Public Property Name As String = ""
        Public Property Kind As MontageKind
        ''' <summary>Ein Satz für die Oberfläche: was diese Verschaltung tut.</summary>
        Public Property Summary As String = ""
        Public Property Channels As New List(Of Derivation)

        Public ReadOnly Property EcgChannel As Integer
            Get
                Return Channels.FindIndex(Function(c) c.Kind = ChannelKind.Ecg)
            End Get
        End Property

        ''' <summary>Index des Kanals mit dieser Beschriftung oder −1.</summary>
        Public Function IndexOf(label As String) As Integer
            Return Channels.FindIndex(Function(c) String.Equals(c.Label, label, StringComparison.OrdinalIgnoreCase))
        End Function
    End Class

    ''' <summary>
    ''' Die wählbaren Verschaltungen. Kanalfolgen der vollständigen Programme nach der
    ''' DGKN-Empfehlung zu EEG-Ableiteprogrammen (Oktober 2024), jeweils die "typische" Variante:
    ''' rechts über links, vorne über hinten, EKG zuletzt.
    ''' </summary>
    Public Module Montages

        Public Const Excerpt As String = "excerpt"
        Public Const Longitudinal As String = "long"
        Public Const Transverse As String = "trans"
        Public Const Ear As String = "ear"
        Public Const Vertex As String = "cz"
        Public Const Average As String = "av"
        Public Const Source As String = "source"
        Public Const Amplifier As String = "amp"

        ' Reihenfolge der Elektroden in den Referenzprogrammen (DGKN, typische Ohr- bzw. Vertexreferenz)
        Private ReadOnly ReferenceOrder As String() =
            {"Fp2", "F8", "T4", "T6", "Fp1", "F7", "T3", "T5", "F4", "C4", "P4", "O2", "F3", "C3", "P3", "O1", "Fz", "Cz", "Pz"}

        Private Function Build(id As String, name As String, kind As MontageKind, summary As String, channels As IEnumerable(Of Derivation)) As Montage
            Dim m As New Montage With {.Id = id, .Name = name, .Kind = kind, .Summary = summary}
            m.Channels.AddRange(channels)
            m.Channels.Add(Derivation.Ecg())
            Return m
        End Function

        Private Function Pairs(ParamArray labels As String()) As IEnumerable(Of Derivation)
            Return labels.Select(Function(l)
                                     Dim p = l.Split("-"c)
                                     Return Derivation.Pair(p(0), p(1))
                                 End Function).ToList()
        End Function

        Public ReadOnly All As Montage() = {
            Build(Excerpt, "Längsreihe – Ausschnitt (8 Kanäle)", MontageKind.Bipolar,
                  "Ausschnitt der Längsreihe: die beiden parasagittalen Ketten, jede Elektrode gegen die nächste dahinter.",
                  Pairs("Fp2-F4", "F4-C4", "C4-P4", "P4-O2", "Fp1-F3", "F3-C3", "C3-P3", "P3-O1")),
            Build(Longitudinal, "Längsreihe", MontageKind.Bipolar,
                  "Bipolare Ketten von vorne nach hinten: jede Elektrode gegen die nächste dahinter.",
                  Pairs("Fp2-F8", "F8-T4", "T4-T6", "T6-O2", "Fp1-F7", "F7-T3", "T3-T5", "T5-O1",
                        "Fp2-F4", "F4-C4", "C4-P4", "P4-O2", "Fp1-F3", "F3-C3", "C3-P3", "P3-O1", "Fz-Cz", "Cz-Pz")),
            Build(Transverse, "Querreihe", MontageKind.Bipolar,
                  "Bipolare Ketten quer über den Kopf von rechts nach links: jede Elektrode gegen die nächste daneben.",
                  Pairs("F8-Fp2", "Fp2-Fp1", "Fp1-F7", "F8-F4", "F4-Fz", "Fz-F3", "F3-F7",
                        "A2-T4", "T4-C4", "C4-Cz", "Cz-C3", "C3-T3", "T3-A1",
                        "T6-P4", "P4-Pz", "Pz-P3", "P3-T5", "T6-O2", "O2-O1", "O1-T5")),
            Build(Ear, "Ohrreferenz (A1/A2)", MontageKind.Reference,
                  "Referenzableitung: jede Elektrode gegen das Ohr der gleichen Seite.",
                  ReferenceOrder.Where(Function(e) True).
                      Select(Function(e) Derivation.Pair(e, Head.Names(Head.EarFor(Head.Index(e))))).ToList()),
            Build(Vertex, "Vertexreferenz (Cz)", MontageKind.Reference,
                  "Referenzableitung: jede Elektrode gegen Cz, den Scheitelpunkt.",
                  ReferenceOrder.Where(Function(e) e <> "Cz").Select(Function(e) Derivation.Pair(e, "Cz")).ToList()),
            Build(Average, "Durchschnittsreferenz (AV)", MontageKind.Average,
                  "Jede Elektrode gegen den berechneten Mittelwert der Kopfelektroden (ohne Fp1, Fp2 und Ohren).",
                  ReferenceOrder.Select(Function(e) Derivation.AgainstMean(e, Head.AverageSet, "AV")).ToList()),
            Build(Source, "Quellenableitung", MontageKind.Source,
                  "Jede Elektrode gegen den Mittelwert ihrer direkten Nachbarn (nach Hjorth).",
                  ReferenceOrder.Select(Function(e) Derivation.AgainstMean(e, Head.Neighbours(Head.Index(e)), "Q")).ToList()),
            Build(Amplifier, "Verstärker-Prinzip (P3, O1)", MontageKind.Amplifier,
                  "Ein Kanal zerlegt: die beiden Eingänge einzeln (20-fach verkleinert) und darunter ihr Unterschied.",
                  {Derivation.SingleInput("P3", "Eingang 1"), Derivation.SingleInput("O1", "Eingang 2"), Derivation.Pair("P3", "O1")})
        }

        Public Function [Get](id As String) As Montage
            Dim m = All.FirstOrDefault(Function(x) x.Id = id)
            Return If(m, All(0))
        End Function

    End Module

End Namespace
