Option Strict On

Imports System.Drawing
Imports System.Globalization
Imports NeuroSim.Simulation
Imports NeuroSim.UI

Namespace Training

    ''' <summary>Alle Einstellungen, die der Benutzer verändern kann.</summary>
    Public NotInheritable Class SimSettings
        Public Property LowCutTc As Double = 0.3        ' Zeitkonstante in s; 0 = aus
        Public Property HighCutHz As Double = 70        ' 0 = aus
        Public Property Notch As Boolean
        Public Property EcgFilter As Boolean
        Public Property Sensitivity As Double = 7       ' µV/mm
        Public Property Parts As New HashSet(Of SignalPart)
        Public Property ShowRaw As Boolean
        Public Property ShowRemoved As Boolean
        Public Property SelectedChannel As Integer = 3
        ''' <summary>Elektrodenimpedanzen in kΩ (Reihenfolge wie EegSimulator.ElectrodeNames).</summary>
        Public Property Impedances As Double() = CType(EegSimulator.DefaultImpedances.Clone(), Double())
        ''' <summary>Gelbrücke zwischen F3 und C3 (zu niedrige Impedanz).</summary>
        Public Property SaltBridge As Boolean

        Public ReadOnly Property LowCutHz As Double
            Get
                Return If(LowCutTc > 0, 1.0 / (2.0 * Math.PI * LowCutTc), 0.0)
            End Get
        End Property

        ''' <summary>Setzt die Impedanz einer Elektrode (für Lektions-Voreinstellungen).</summary>
        Public Function WithImpedance(electrodeName As String, kOhm As Double) As SimSettings
            Impedances(Array.IndexOf(EegSimulator.ElectrodeNames, electrodeName)) = kOhm
            Return Me
        End Function

        Public Function Clone() As SimSettings
            Dim c = DirectCast(MemberwiseClone(), SimSettings)
            c.Parts = New HashSet(Of SignalPart)(Parts)
            c.Impedances = CType(Impedances.Clone(), Double())
            Return c
        End Function
    End Class

    ''' <summary>Auswahllisten und deutschsprachige Beschriftungen.</summary>
    Public Module FilterText

        Public ReadOnly De As CultureInfo = CultureInfo.GetCultureInfo("de-DE")

        ''' <summary>Zeitkonstanten (s). 0 = aus.</summary>
        Public ReadOnly LowCutOptions As Double() = {0, 2.0, 1.0, 0.3, 0.1, 0.03}
        ''' <summary>HiCut in Hz. 0 = aus.</summary>
        Public ReadOnly HighCutOptions As Double() = {0, 100, 70, 50, 35, 30, 20, 15}
        ''' <summary>Empfindlichkeit in µV/mm.</summary>
        Public ReadOnly SensitivityOptions As Double() = {2, 3, 5, 7, 10, 15, 20, 30, 50, 70, 100}
        ''' <summary>Wählbare Elektrodenimpedanzen in kΩ.</summary>
        Public ReadOnly ImpedanceOptions As Double() = {2, 3, 5, 10, 15, 25, 50}

        Public Function Hz(f As Double) As String
            If f < 1 Then Return f.ToString("0.00", De)
            If f < 10 Then Return f.ToString("0.0", De)
            Return f.ToString("0", De)
        End Function

        Public Function Tc(t As Double) As String
            Return t.ToString("0.0##", De)
        End Function

        Public Function LowCutLabel(tcSec As Double) As String
            If tcSec <= 0 Then Return "Aus  (kein LowCut)"
            Dim f = 1.0 / (2 * Math.PI * tcSec)
            Return $"{Hz(f)} Hz   ·   TC {Tc(tcSec)} s"
        End Function

        Public Function HighCutLabel(f As Double) As String
            If f <= 0 Then Return "Aus  (kein HiCut)"
            Return $"{Hz(f)} Hz"
        End Function

        Public Function SensLabel(s As Double) As String
            Return $"{s.ToString("0", De)} µV/mm"
        End Function

    End Module

    ''' <summary>Beschreibung eines Signal-Bausteins für Oberfläche und Erklärungen.</summary>
    Public NotInheritable Class PartDescriptor
        Public Property Part As SignalPart
        Public Property Label As String
        Public Property Color As Color
        Public Property IsBrain As Boolean
        Public Property TypicalHz As Double
        Public Property Hint As String
    End Class

    Public Module PartCatalog

        Public ReadOnly All As PartDescriptor() = {
            New PartDescriptor With {.Part = SignalPart.Delta, .Label = "Delta-Herd (1–3 Hz)", .Color = Theme.ColDelta, .IsBrain = True, .TypicalHz = 1.5,
                .Hint = "Langsame Wellen links zentral (F3-C3 / C3-P3) – z. B. bei einer Läsion."},
            New PartDescriptor With {.Part = SignalPart.Theta, .Label = "Theta (6 Hz)", .Color = Theme.ColTheta, .IsBrain = True, .TypicalHz = 6,
                .Hint = "Zwischenwellen, z. B. bei Müdigkeit."},
            New PartDescriptor With {.Part = SignalPart.Alpha, .Label = "Alpha (10 Hz)", .Color = Theme.ColAlpha, .IsBrain = True, .TypicalHz = 10,
                .Hint = "Grundrhythmus bei geschlossenen Augen, hinten betont."},
            New PartDescriptor With {.Part = SignalPart.Beta, .Label = "Beta (20 Hz)", .Color = Theme.ColBeta, .IsBrain = True, .TypicalHz = 20,
                .Hint = "Schnelle, niedrige Wellen, frontal betont."},
            New PartDescriptor With {.Part = SignalPart.Spikes, .Label = "Spikes (spitze Potentiale)", .Color = Theme.ColSpike, .IsBrain = True, .TypicalHz = 25,
                .Hint = "Oberflächennegative Spike-Wave-Komplexe mit Phasenumkehr bei C3."},
            New PartDescriptor With {.Part = SignalPart.Blinks, .Label = "Augenblinzeln", .Color = Theme.ColBlink, .IsBrain = False, .TypicalHz = 1.0,
                .Hint = "Große Ausschläge nach unten in Fp1-F3 und Fp2-F4 (Fp wird positiv)."},
            New PartDescriptor With {.Part = SignalPart.Sweat, .Label = "Schwitzartefakt / Drift", .Color = Theme.ColDrift, .IsBrain = False, .TypicalHz = 0.25,
                .Hint = "Sehr langsames Wandern der Grundlinie (unter 0,5 Hz)."},
            New PartDescriptor With {.Part = SignalPart.Emg, .Label = "Muskelartefakt (EMG)", .Color = Theme.ColFast, .IsBrain = False, .TypicalHz = 70,
                .Hint = "Schnelles 'Gekritzel' durch Muskelanspannung, frontal."},
            New PartDescriptor With {.Part = SignalPart.Hum, .Label = "50-Hz-Einstreuung (Netz)", .Color = Theme.ColHum, .IsBrain = False, .TypicalHz = 50,
                .Hint = "Netzstörung aus der Umgebung. Wie stark sie im EEG erscheint, hängt von den Impedanzen ab."},
            New PartDescriptor With {.Part = SignalPart.Ecg, .Label = "EKG-Artefakt", .Color = Theme.ColEcg, .IsBrain = False, .TypicalHz = 12,
                .Hint = "Kleine Zacken im Takt des Herzschlags."}
        }

        Public Function [Get](p As SignalPart) As PartDescriptor
            Return All.First(Function(d) d.Part = p)
        End Function

    End Module

End Namespace
