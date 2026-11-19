Option Strict On

Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text.Json

Namespace Services

    ''' <summary>Name, Version, Entwickler und Adressen der Anwendung an einer Stelle.</summary>
    Public Module AppInfo
        Public Const Name As String = "NeuroSim"
        Public Const Tagline As String = "Der EEG-Simulator: Filter, Impedanzen und Artefakte verstehen"
        Public Const Developer As String = "F. Soetbeer"
        Public Const CopyrightYear As String = "2026"
        Public Const LicenseName As String = "GNU General Public License, Version 3 oder später (GPL-3.0-or-later)"
        Public Const RepoOwner As String = "soetbaer"
        Public Const RepoName As String = "EEGSimulator"
        Public Const RepoUrl As String = "https://github.com/" & RepoOwner & "/" & RepoName
        Public Const ReleasesUrl As String = RepoUrl & "/releases"

        Public ReadOnly Property Version As Version
            Get
                Dim v = Assembly.GetExecutingAssembly().GetName().Version
                Return If(v, New Version(1, 0, 0))
            End Get
        End Property

        Public ReadOnly Property VersionText As String
            Get
                Dim v = Version
                Return $"{v.Major}.{v.Minor}.{Math.Max(0, v.Build)}"
            End Get
        End Property
    End Module

    ''' <summary>Art einer Zeile in den Versionshinweisen.</summary>
    Public Enum NoteKind
        ''' <summary>Überschrift einer Version (nur wenn mehrere Versionen übersprungen wurden).</summary>
        Version
        ''' <summary>Abschnitt wie »Neu«, »Geändert«, »Behoben«.</summary>
        Heading
        Bullet
        Text
    End Enum

    ''' <summary>Eine Zeile der Versionshinweise. Text kann **fett** markierte Stellen enthalten.</summary>
    Public NotInheritable Class NoteLine
        Public Property Kind As NoteKind
        Public Property Text As String = ""

        Public Sub New(kind As NoteKind, text As String)
            Me.Kind = kind
            Me.Text = text
        End Sub

        ''' <summary>Text ohne Fett-Markierung.</summary>
        Public ReadOnly Property PlainText As String
            Get
                Return Text.Replace("**", "")
            End Get
        End Property
    End Class

    ''' <summary>Ergebnis einer Update-Prüfung.</summary>
    Public NotInheritable Class UpdateInfo
        Public Property Version As Version
        Public Property TagName As String = ""
        Public Property Notes As String = ""
        ''' <summary>Versionshinweise aller neuen Versionen (neueste zuerst), leer wenn nicht abrufbar.</summary>
        Public Property NoteLines As New List(Of NoteLine)
        Public Property PageUrl As String = AppInfo.ReleasesUrl
        ''' <summary>Download-Adresse der EXE oder Nothing, wenn das Release keine EXE enthält.</summary>
        Public Property DownloadUrl As String
        ''' <summary>SHA-256-Prüfsumme der EXE (hex), sofern dem Release eine .sha256-Datei beiliegt.</summary>
        Public Property Sha256 As String
        Public Property IsNewer As Boolean

        Public ReadOnly Property VersionText As String
            Get
                Return $"{Version.Major}.{Version.Minor}.{Math.Max(0, Version.Build)}"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Update-Funktion: fragt das neueste Release bei GitHub ab und kann die neue EXE laden und einsetzen.
    ''' Es werden keine Nutzungsdaten gesendet; GitHub sieht bei der Abfrage lediglich die IP-Adresse des Rechners.
    ''' </summary>
    Public NotInheritable Class Updater

        ' Die normale Release-Seite leitet auf das neueste Release weiter. Anders als die
        ' Programmierschnittstelle von GitHub hat sie keine enge Abfragegrenze je IP-Adresse.
        Private Const LatestUrl As String = AppInfo.ReleasesUrl & "/latest"
        Private Const TagPrefix As String = AppInfo.ReleasesUrl & "/tag/"
        ' Nur Downloads aus den Releases dieses Repositorys werden akzeptiert
        Private Const DownloadPrefix As String = AppInfo.RepoUrl & "/releases/download/"
        Private Const ExeAssetName As String = "NeuroSim.exe"
        ' Versionshinweise (Release-Texte) kommen über die Programmierschnittstelle. Schlägt die
        ' Abfrage fehl (z. B. Abfragegrenze), zeigt der Dialog nur den Verweis auf die Release-Seite.
        Private Const ApiReleasesUrl As String = "https://api.github.com/repos/" & AppInfo.RepoOwner & "/" & AppInfo.RepoName & "/releases?per_page=30"
        ' Abschnitte der Release-Texte, die im Update-Dialog nichts nützen
        Private Shared ReadOnly SkippedSections As String() = {"Installation", "Hinweis"}
        ''' <summary>Liegt diese Datei neben der EXE, ist die Update-Funktion abgeschaltet.</summary>
        Public Const DisableFileName As String = "NeuroSim.noupdate"
        ''' <summary>Registry-Schlüssel (HKEY_LOCAL_MACHINE) mit dem Wert DisableUpdate = 1.</summary>
        Public Const PolicyKey As String = "Software\Policies\NeuroSim"

        Private Sub New()
        End Sub

        ''' <summary>Liest eine Versionsnummer aus einem Tag wie "v1.2.0" oder "1.2".</summary>
        Public Shared Function ParseVersion(tag As String) As Version
            If String.IsNullOrWhiteSpace(tag) Then Return Nothing
            Dim s = tag.Trim().TrimStart("v"c, "V"c)
            Dim n = 0
            While n < s.Length AndAlso (Char.IsDigit(s(n)) OrElse s(n) = "."c) : n += 1 : End While
            s = s.Substring(0, n).Trim("."c)
            If s.Length = 0 Then Return Nothing
            If Not s.Contains("."c) Then s &= ".0"
            Dim v As Version = Nothing
            If Not Version.TryParse(s, v) Then Return Nothing
            Return New Version(v.Major, v.Minor, Math.Max(0, v.Build))
        End Function

        ''' <summary>
        ''' Wertet die Weiterleitung von ".../releases/latest" aus (ohne Netzwerk, daher testbar).
        ''' Gibt Nothing zurück, wenn die Adresse nicht auf ein Release dieses Repositorys zeigt.
        ''' </summary>
        Public Shared Function ParseLatestLocation(location As String, current As Version) As UpdateInfo
            If String.IsNullOrWhiteSpace(location) Then Return Nothing
            If Not location.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase) Then Return Nothing
            Dim tag = Uri.UnescapeDataString(location.Substring(TagPrefix.Length)).Trim("/"c)
            If tag.Length = 0 OrElse tag.Contains("/"c) Then Return Nothing
            Dim v = ParseVersion(tag)
            If v Is Nothing Then Return Nothing
            Dim info As New UpdateInfo()
            info.TagName = tag
            info.Version = v
            info.PageUrl = TagPrefix & Uri.EscapeDataString(tag)
            info.DownloadUrl = DownloadPrefix & Uri.EscapeDataString(tag) & "/" & ExeAssetName
            Dim cur = New Version(current.Major, current.Minor, Math.Max(0, current.Build))
            info.IsNewer = v > cur
            Return info
        End Function

        ''' <summary>Liest die SHA-256-Prüfsumme (64 Hex-Zeichen am Anfang) aus einer .sha256-Datei oder gibt Nothing zurück.</summary>
        Public Shared Function ParseSha256(text As String) As String
            If text Is Nothing Then Return Nothing
            Dim t = text.Trim()
            If t.Length < 64 Then Return Nothing
            Dim h = t.Substring(0, 64).ToLowerInvariant()
            For Each ch In h
                If Not Uri.IsHexDigit(ch) Then Return Nothing
            Next
            Return h
        End Function

        ''' <summary>
        ''' Zerlegt einen Release-Text (Markdown) in Zeilen für den Update-Dialog.
        ''' Die Abschnitte »Installation« und »Hinweis« werden weggelassen.
        ''' </summary>
        Public Shared Function ParseReleaseBody(body As String) As List(Of NoteLine)
            Dim result As New List(Of NoteLine)
            If String.IsNullOrWhiteSpace(body) Then Return result
            Dim skipping = False
            For Each raw In body.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split(ControlChars.Lf)
                Dim line = raw.Trim().Replace("`", "")
                If line.Length = 0 Then Continue For
                If line.StartsWith("#") Then
                    Dim title = line.TrimStart("#"c).Trim().Replace("**", "")
                    skipping = SkippedSections.Any(Function(x) title.Equals(x, StringComparison.OrdinalIgnoreCase))
                    If Not skipping AndAlso title.Length > 0 Then result.Add(New NoteLine(NoteKind.Heading, title))
                    Continue For
                End If
                If skipping Then Continue For
                If line.StartsWith("- ") OrElse line.StartsWith("* ") OrElse line.StartsWith("+ ") Then
                    result.Add(New NoteLine(NoteKind.Bullet, line.Substring(2).Trim()))
                Else
                    result.Add(New NoteLine(NoteKind.Text, line))
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Liest die Antwort von GitHub (Liste der Releases als JSON) und sammelt die Versionshinweise
        ''' aller Versionen nach der installierten bis einschließlich der neuesten, neueste zuerst.
        ''' Ohne Netzwerk, daher testbar.
        ''' </summary>
        Public Shared Function BuildNoteLines(releasesJson As String, current As Version, latest As Version) As List(Of NoteLine)
            Dim result As New List(Of NoteLine)
            If String.IsNullOrWhiteSpace(releasesJson) OrElse current Is Nothing OrElse latest Is Nothing Then Return result
            Dim cur = New Version(current.Major, current.Minor, Math.Max(0, current.Build))
            Dim found As New List(Of (Version As Version, Lines As List(Of NoteLine)))
            Try
                Using doc = JsonDocument.Parse(releasesJson)
                    If doc.RootElement.ValueKind <> JsonValueKind.Array Then Return result
                    For Each rel In doc.RootElement.EnumerateArray()
                        If rel.ValueKind <> JsonValueKind.Object Then Continue For
                        If IsTrue(rel, "draft") OrElse IsTrue(rel, "prerelease") Then Continue For
                        Dim v = ParseVersion(GetString(rel, "tag_name"))
                        If v Is Nothing OrElse v <= cur OrElse v > latest Then Continue For
                        If found.Any(Function(f) f.Version = v) Then Continue For
                        Dim lines = ParseReleaseBody(GetString(rel, "body"))
                        If lines.Count > 0 Then found.Add((v, lines))
                    Next
                End Using
            Catch ex As JsonException
                Return result
            End Try
            found.Sort(Function(a, b) b.Version.CompareTo(a.Version))
            For Each f In found
                If found.Count > 1 Then result.Add(New NoteLine(NoteKind.Version, $"Version {f.Version.Major}.{f.Version.Minor}.{Math.Max(0, f.Version.Build)}"))
                result.AddRange(f.Lines)
            Next
            Return result
        End Function

        ''' <summary>Die Versionshinweise als einfacher Text.</summary>
        Public Shared Function NotesAsText(lines As IEnumerable(Of NoteLine)) As String
            Dim sb As New System.Text.StringBuilder()
            For Each l In lines
                Select Case l.Kind
                    Case NoteKind.Version, NoteKind.Heading
                        If sb.Length > 0 Then sb.AppendLine()
                        sb.AppendLine(l.PlainText)
                    Case NoteKind.Bullet
                        sb.AppendLine("• " & l.PlainText)
                    Case Else
                        sb.AppendLine(l.PlainText)
                End Select
            Next
            Return sb.ToString().TrimEnd()
        End Function

        Private Shared Function GetString(e As JsonElement, name As String) As String
            Dim p As JsonElement
            If e.TryGetProperty(name, p) AndAlso p.ValueKind = JsonValueKind.String Then Return p.GetString()
            Return Nothing
        End Function

        Private Shared Function IsTrue(e As JsonElement, name As String) As Boolean
            Dim p As JsonElement
            Return e.TryGetProperty(name, p) AndAlso p.ValueKind = JsonValueKind.True
        End Function

        ''' <summary>
        ''' Hat ein Administrator die Update-Funktion abgeschaltet?
        ''' Entweder durch die Datei "NeuroSim.noupdate" neben der EXE oder durch den Registry-Wert
        ''' HKEY_LOCAL_MACHINE\Software\Policies\NeuroSim, DisableUpdate = 1.
        ''' </summary>
        Public Shared ReadOnly Property DisabledByAdministrator As Boolean
            Get
                Try
                    Dim exe = ExePath
                    If Not String.IsNullOrEmpty(exe) AndAlso File.Exists(Path.Combine(Path.GetDirectoryName(exe), DisableFileName)) Then Return True
                Catch
                End Try
                Try
                    Using key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(PolicyKey)
                        If key IsNot Nothing Then
                            Dim value = key.GetValue("DisableUpdate")
                            If value IsNot Nothing AndAlso value.ToString() = "1" Then Return True
                        End If
                    End Using
                Catch
                End Try
                Return False
            End Get
        End Property

        Private Shared Function NewClient(timeoutSeconds As Integer, Optional followRedirects As Boolean = True) As HttpClient
            Dim handler As New HttpClientHandler With {.AllowAutoRedirect = followRedirects}
            Dim c As New HttpClient(handler) With {.Timeout = TimeSpan.FromSeconds(timeoutSeconds)}
            c.DefaultRequestHeaders.UserAgent.ParseAdd("NeuroSim-Updater")
            Return c
        End Function

        ''' <summary>Fragt das neueste Release ab. Gibt Nothing zurück, wenn es noch kein Release gibt.</summary>
        Public Shared Async Function CheckAsync() As Task(Of UpdateInfo)
            If DisabledByAdministrator Then Throw New InvalidOperationException("Die Update-Funktion wurde vom Administrator abgeschaltet.")
            Dim info As UpdateInfo = Nothing
            Using client = NewClient(10, followRedirects:=False)
                Using resp = Await client.GetAsync(LatestUrl)
                    Dim code = CInt(resp.StatusCode)
                    If code = 404 Then Return Nothing
                    Dim target = resp.Headers.Location
                    If code < 300 OrElse code >= 400 OrElse target Is Nothing Then Throw New HttpRequestException("Unerwartete Antwort von GitHub.")
                    If Not target.IsAbsoluteUri Then target = New Uri(New Uri(LatestUrl), target)
                    info = ParseLatestLocation(target.AbsoluteUri, AppInfo.Version)
                End Using
            End Using
            If info Is Nothing OrElse Not info.IsNewer Then Return info

            ' Prüfsumme, sofern dem Release eine Datei NeuroSim.exe.sha256 beiliegt
            Try
                Using client = NewClient(10)
                    Using resp = Await client.GetAsync(info.DownloadUrl & ".sha256")
                        If resp.IsSuccessStatusCode Then info.Sha256 = ParseSha256(Await resp.Content.ReadAsStringAsync())
                    End Using
                End Using
            Catch
            End Try

            ' Versionshinweise, auch der übersprungenen Versionen. Fehler sind hier unkritisch.
            Try
                Using client = NewClient(10)
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
                    Using resp = Await client.GetAsync(ApiReleasesUrl)
                        If resp.IsSuccessStatusCode Then
                            info.NoteLines = BuildNoteLines(Await resp.Content.ReadAsStringAsync(), AppInfo.Version, info.Version)
                            info.Notes = NotesAsText(info.NoteLines)
                        End If
                    End Using
                End Using
            Catch
            End Try
            Return info
        End Function

        Private Shared ReadOnly Property ExePath As String
            Get
                Return Environment.ProcessPath
            End Get
        End Property

        ''' <summary>Kann sich die Anwendung selbst ersetzen? (Nicht, wenn sie über "dotnet" gestartet wurde.)</summary>
        Public Shared ReadOnly Property CanSelfUpdate As Boolean
            Get
                Dim p = ExePath
                If String.IsNullOrEmpty(p) Then Return False
                Return Not Path.GetFileNameWithoutExtension(p).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            End Get
        End Property

        ''' <summary>
        ''' Lädt die neue EXE, prüft sie und setzt sie an die Stelle der laufenden EXE.
        ''' Die alte Datei bleibt als ".old" liegen und wird beim nächsten Start gelöscht.
        ''' Rückgabe True: Die neue Version ist gestartet, diese Instanz muss beendet werden.
        ''' Rückgabe False: Die neue Version ist installiert, ließ sich aber nicht starten.
        ''' </summary>
        Public Shared Async Function DownloadAndInstallAsync(info As UpdateInfo, progress As IProgress(Of Double)) As Task(Of Boolean)
            If info Is Nothing OrElse String.IsNullOrEmpty(info.DownloadUrl) Then Throw New InvalidOperationException("Das Release enthält keine EXE-Datei.")
            If DisabledByAdministrator Then Throw New InvalidOperationException("Die Update-Funktion wurde vom Administrator abgeschaltet.")
            If Not CanSelfUpdate Then Throw New InvalidOperationException("Die Anwendung läuft im Entwicklungsmodus und kann sich nicht selbst ersetzen.")
            Dim exe = ExePath
            Dim newPath = exe & ".new"
            Dim oldPath = exe & ".old"

            Using client = NewClient(600)
                Using resp = Await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead)
                    resp.EnsureSuccessStatusCode()
                    Dim total = resp.Content.Headers.ContentLength
                    Using src = Await resp.Content.ReadAsStreamAsync(), dst = New FileStream(newPath, FileMode.Create, FileAccess.Write, FileShare.None)
                        Dim buffer(81919) As Byte
                        Dim done As Long = 0
                        While True
                            Dim n = Await src.ReadAsync(buffer.AsMemory(0, buffer.Length))
                            If n <= 0 Then Exit While
                            Await dst.WriteAsync(buffer.AsMemory(0, n))
                            done += n
                            If progress IsNot Nothing AndAlso total.HasValue AndAlso total.Value > 0 Then progress.Report(done / CDbl(total.Value))
                        End While
                    End Using
                End Using
            End Using

            ' Prüfen: plausible Größe, Windows-Programm, ggf. Prüfsumme
            Try
                Dim fi As New FileInfo(newPath)
                If fi.Length < 1000000 Then Throw New InvalidDataException("Die heruntergeladene Datei ist unvollständig.")
                Using fs = File.OpenRead(newPath)
                    If fs.ReadByte() <> AscW("M"c) OrElse fs.ReadByte() <> AscW("Z"c) Then Throw New InvalidDataException("Die heruntergeladene Datei ist kein Windows-Programm.")
                    If Not String.IsNullOrEmpty(info.Sha256) Then
                        fs.Position = 0
                        Dim hash = Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant()
                        If hash <> info.Sha256 Then Throw New InvalidDataException("Die Prüfsumme der heruntergeladenen Datei stimmt nicht.")
                    End If
                End Using
            Catch
                TryDelete(newPath)
                Throw
            End Try

            ' Austauschen: Eine laufende EXE darf unter Windows umbenannt werden
            TryDelete(oldPath)
            File.Move(exe, oldPath)
            Try
                File.Move(newPath, exe)
            Catch
                File.Move(oldPath, exe)     ' zurückrollen
                TryDelete(newPath)
                Throw
            End Try

            Try
                Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True})
            Catch
                Return False
            End Try
            Return True
        End Function

        ''' <summary>Räumt Reste eines früheren Updates weg (beim Start aufrufen).</summary>
        Public Shared Sub CleanupOldVersion()
            Dim exe = ExePath
            If String.IsNullOrEmpty(exe) Then Return
            TryDelete(exe & ".old")
            TryDelete(exe & ".new")
        End Sub

        Private Shared Sub TryDelete(path As String)
            Try
                If File.Exists(path) Then File.Delete(path)
            Catch
            End Try
        End Sub

    End Class

    ''' <summary>Einfache, dauerhaft gespeicherte Einstellungen (%AppData%\NeuroSim\settings.txt).</summary>
    Public Module AppSettings

        Private ReadOnly FilePath As String =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.Name, "settings.txt")

        Private Function Load() As Dictionary(Of String, String)
            Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Try
                If File.Exists(FilePath) Then
                    For Each line In File.ReadAllLines(FilePath)
                        Dim i = line.IndexOf("="c)
                        If i > 0 Then d(line.Substring(0, i).Trim()) = line.Substring(i + 1).Trim()
                    Next
                End If
            Catch
            End Try
            Return d
        End Function

        Private Sub Save(d As Dictionary(Of String, String))
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath))
                File.WriteAllLines(FilePath, d.Select(Function(kv) kv.Key & "=" & kv.Value))
            Catch
            End Try
        End Sub

        ''' <summary>Beim Start automatisch nach Updates suchen (Standard: nein, bis der Benutzer es auf der Startseite oder im Info-Dialog einschaltet).</summary>
        Public Property AutoUpdateCheck As Boolean
            Get
                Dim v As String = Nothing
                If Load().TryGetValue("AutoUpdateCheck", v) Then Return v = "1"
                Return False
            End Get
            Set(value As Boolean)
                Dim d = Load()
                d("AutoUpdateCheck") = If(value, "1", "0")
                Save(d)
            End Set
        End Property

        ''' <summary>Startseite beim Programmstart zeigen (Standard: ja). Aus = NeuroSim öffnet direkt Lektion 1.</summary>
        Public Property ShowStartPage As Boolean
            Get
                Dim v As String = Nothing
                If Load().TryGetValue("ShowStartPage", v) Then Return v <> "0"
                Return True
            End Get
            Set(value As Boolean)
                Dim d = Load()
                d("ShowStartPage") = If(value, "1", "0")
                Save(d)
            End Set
        End Property

        Private Function Today() As String
            Return Date.Today.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture)
        End Function

        ''' <summary>Die automatische Suche läuft höchstens einmal am Tag.</summary>
        Public ReadOnly Property UpdateCheckDueToday As Boolean
            Get
                Dim v As String = Nothing
                Return Not (Load().TryGetValue("LastUpdateCheck", v) AndAlso v = Today())
            End Get
        End Property

        Public Sub MarkUpdateChecked()
            Dim d = Load()
            d("LastUpdateCheck") = Today()
            Save(d)
        End Sub

    End Module

End Namespace
