Option Strict On

Namespace Simulation

    ''' <summary>
    ''' Die 21 Elektroden des internationalen 10/20-Systems (19 Kopfelektroden und zwei Ohrelektroden)
    ''' mit ihrer Lage für das Kopfschema und für die Simulation.
    ''' Bezeichnungen wie in den Montage-Tabellen der DGKN-Empfehlung zu EEG-Ableiteprogrammen (2024):
    ''' T3, T5, T4, T6 entsprechen T7, P7, T8, P8 der neueren Nomenklatur.
    ''' </summary>
    Public Module Head

        Public ReadOnly Names As String() =
            {"Fp1", "Fp2", "F7", "F3", "Fz", "F4", "F8", "T3", "C3", "Cz", "C4", "T4", "T5", "P3", "Pz", "P4", "T6", "O1", "O2", "A1", "A2"}

        Public ReadOnly Property Count As Integer
            Get
                Return Names.Length
            End Get
        End Property

        ''' <summary>Lage von oben gesehen: X von −1 (links) bis +1 (rechts), Ohren außerhalb bei ±1,36.</summary>
        Public ReadOnly X As Double() =
            {-0.309, 0.309, -0.809, -0.4, 0, 0.4, 0.809, -1, -0.5, 0, 0.5, 1, -0.809, -0.4, 0, 0.4, 0.809, -0.309, 0.309, -1.36, 1.36}

        ''' <summary>Lage von oben gesehen: Y von +1 (vorne, Nase) bis −1 (hinten).</summary>
        Public ReadOnly Y As Double() =
            {0.951, 0.951, 0.588, 0.54, 0.5, 0.54, 0.588, 0, 0, 0, 0, 0, -0.588, -0.54, -0.5, -0.54, -0.588, -0.951, -0.951, -0.05, -0.05}

        ''' <summary>Höhe am Kopf: 1 = Scheitel (Cz), 0 = Höhe der Ohren. Nur für das EKG-Artefakt der Simulation.</summary>
        Public ReadOnly Height As Double() =
            {0.3, 0.3, 0.3, 0.7, 0.85, 0.7, 0.3, 0.3, 0.8, 1, 0.8, 0.3, 0.3, 0.7, 0.85, 0.7, 0.3, 0.3, 0.3, 0, 0}

        ''' <summary>Index einer Elektrode (Groß-/Kleinschreibung egal). Löst eine Ausnahme aus, wenn es sie nicht gibt.</summary>
        Public Function Index(name As String) As Integer
            For i = 0 To Names.Length - 1
                If String.Equals(Names(i), name, StringComparison.OrdinalIgnoreCase) Then Return i
            Next
            Throw New ArgumentException($"Unbekannte Elektrode: {name}")
        End Function

        ''' <summary>−1 = links, +1 = rechts, 0 = Mittellinie.</summary>
        Public Function Side(electrode As Integer) As Integer
            Return Math.Sign(X(electrode))
        End Function

        ''' <summary>Ohrelektrode der gleichen Seite (Mittellinie: Fz und Pz rechts, Cz links – wie in der DGKN-Tabelle).</summary>
        Public Function EarFor(electrode As Integer) As Integer
            Dim s = Side(electrode)
            If s < 0 Then Return Index("A1")
            If s > 0 Then Return Index("A2")
            Return If(Names(electrode) = "Cz", Index("A1"), Index("A2"))
        End Function

        ' Direkte Nachbarn jeder Kopfelektrode für die Quellenableitung (nach Hjorth).
        ' Welche Nachbarn und Gewichte ein Gerät benutzt, ist herstellerabhängig.
        Private ReadOnly NeighbourNames As New Dictionary(Of String, String()) From {
            {"Fp1", {"Fp2", "F7", "F3"}}, {"Fp2", {"Fp1", "F4", "F8"}},
            {"F7", {"Fp1", "F3", "T3"}}, {"F3", {"Fp1", "F7", "Fz", "C3"}}, {"Fz", {"F3", "F4", "Cz"}},
            {"F4", {"Fp2", "Fz", "F8", "C4"}}, {"F8", {"Fp2", "F4", "T4"}},
            {"T3", {"F7", "C3", "T5"}}, {"C3", {"F3", "T3", "Cz", "P3"}}, {"Cz", {"Fz", "C3", "C4", "Pz"}},
            {"C4", {"F4", "Cz", "T4", "P4"}}, {"T4", {"F8", "C4", "T6"}},
            {"T5", {"T3", "P3", "O1"}}, {"P3", {"C3", "T5", "Pz", "O1"}}, {"Pz", {"Cz", "P3", "P4"}},
            {"P4", {"C4", "Pz", "T6", "O2"}}, {"T6", {"T4", "P4", "O2"}},
            {"O1", {"T5", "P3", "O2"}}, {"O2", {"T6", "P4", "O1"}}}

        ''' <summary>Nachbarelektroden für die Quellenableitung (leer für die Ohren).</summary>
        Public Function Neighbours(electrode As Integer) As Integer()
            Dim n As String() = Nothing
            If Not NeighbourNames.TryGetValue(Names(electrode), n) Then Return Array.Empty(Of Integer)()
            Return n.Select(Function(s) Index(s)).ToArray()
        End Function

        ''' <summary>
        ''' Elektroden, die in die Durchschnittsreferenz eingehen: alle Kopfelektroden außer Fp1 und Fp2
        ''' (Augenartefakte) und ohne die Ohren – so nennt es die DGKN-Empfehlung als typisch.
        ''' </summary>
        Public ReadOnly Property AverageSet As Integer()
            Get
                Return Enumerable.Range(0, Names.Length).
                    Where(Function(i) Names(i) <> "Fp1" AndAlso Names(i) <> "Fp2" AndAlso Names(i) <> "A1" AndAlso Names(i) <> "A2").ToArray()
            End Get
        End Property

    End Module

End Namespace
