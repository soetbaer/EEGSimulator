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

    ''' <summary>Ergebnis einer Update-Prüfung.</summary>
    Public NotInheritable Class UpdateInfo
        Public Property Version As Version
        Public Property TagName As String = ""
        Public Property Notes As String = ""
        Public Property PageUrl As String = AppInfo.ReleasesUrl
        ''' <summary>Download-Adresse der EXE oder Nothing, wenn das Release keine EXE enthält.</summary>
        Public Property DownloadUrl As String
        ''' <summary>SHA-256-Prüfsumme der EXE (hex), sofern GitHub sie mitliefert.</summary>
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

        Private Const ApiUrl As String = "https://api.github.com/repos/" & AppInfo.RepoOwner & "/" & AppInfo.RepoName & "/releases/latest"
        ' Nur Downloads aus den Releases dieses Repositorys werden akzeptiert
        Private Const DownloadPrefix As String = AppInfo.RepoUrl & "/releases/download/"

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

        ''' <summary>Wertet die Antwort der GitHub-Schnittstelle aus (ohne Netzwerk, daher testbar).</summary>
        Public Shared Function ParseRelease(json As String, current As Version) As UpdateInfo
            Using doc = JsonDocument.Parse(json)
                Dim root = doc.RootElement
                Dim info As New UpdateInfo()
                Dim el As JsonElement
                If root.TryGetProperty("tag_name", el) AndAlso el.ValueKind = JsonValueKind.String Then info.TagName = el.GetString()
                info.Version = ParseVersion(info.TagName)
                If info.Version Is Nothing Then Return Nothing
                If root.TryGetProperty("body", el) AndAlso el.ValueKind = JsonValueKind.String Then info.Notes = el.GetString()
                If root.TryGetProperty("html_url", el) AndAlso el.ValueKind = JsonValueKind.String Then
                    Dim url = el.GetString()
                    If url.StartsWith(AppInfo.RepoUrl & "/", StringComparison.OrdinalIgnoreCase) Then info.PageUrl = url
                End If
                If root.TryGetProperty("assets", el) AndAlso el.ValueKind = JsonValueKind.Array Then
                    For Each a In el.EnumerateArray()
                        Dim nameEl As JsonElement, urlEl As JsonElement, digEl As JsonElement
                        If Not a.TryGetProperty("name", nameEl) OrElse Not a.TryGetProperty("browser_download_url", urlEl) Then Continue For
                        Dim name = nameEl.GetString(), url = urlEl.GetString()
                        If name Is Nothing OrElse url Is Nothing Then Continue For
                        If Not name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then Continue For
                        If Not url.StartsWith(DownloadPrefix, StringComparison.OrdinalIgnoreCase) Then Continue For
                        info.DownloadUrl = url
                        If a.TryGetProperty("digest", digEl) AndAlso digEl.ValueKind = JsonValueKind.String Then
                            Dim d = digEl.GetString()
                            If d.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) Then info.Sha256 = d.Substring(7).Trim().ToLowerInvariant()
                        End If
                        Exit For
                    Next
                End If
                Dim cur = New Version(current.Major, current.Minor, Math.Max(0, current.Build))
                info.IsNewer = info.Version > cur
                Return info
            End Using
        End Function

        Private Shared Function NewClient(timeoutSeconds As Integer) As HttpClient
            Dim c As New HttpClient With {.Timeout = TimeSpan.FromSeconds(timeoutSeconds)}
            c.DefaultRequestHeaders.UserAgent.ParseAdd("NeuroSim-Updater")
            c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json")
            Return c
        End Function

        ''' <summary>Fragt das neueste Release ab. Gibt Nothing zurück, wenn es noch kein Release gibt.</summary>
        Public Shared Async Function CheckAsync() As Task(Of UpdateInfo)
            Using client = NewClient(10)
                Using resp = Await client.GetAsync(ApiUrl)
                    If resp.StatusCode = Net.HttpStatusCode.NotFound Then Return Nothing
                    resp.EnsureSuccessStatusCode()
                    Dim json = Await resp.Content.ReadAsStringAsync()
                    Return ParseRelease(json, AppInfo.Version)
                End Using
            End Using
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
        ''' Danach muss die Anwendung beendet werden; die neue Version ist bereits gestartet.
        ''' </summary>
        Public Shared Async Function DownloadAndInstallAsync(info As UpdateInfo, progress As IProgress(Of Double)) As Task
            If info Is Nothing OrElse String.IsNullOrEmpty(info.DownloadUrl) Then Throw New InvalidOperationException("Das Release enthält keine EXE-Datei.")
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

            Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True})
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

        ''' <summary>Beim Start automatisch nach Updates suchen (Standard: ja).</summary>
        Public Property AutoUpdateCheck As Boolean
            Get
                Dim v As String = Nothing
                If Load().TryGetValue("AutoUpdateCheck", v) Then Return v <> "0"
                Return True
            End Get
            Set(value As Boolean)
                Dim d = Load()
                d("AutoUpdateCheck") = If(value, "1", "0")
                Save(d)
            End Set
        End Property

    End Module

End Namespace
