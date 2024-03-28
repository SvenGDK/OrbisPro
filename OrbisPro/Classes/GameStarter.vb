Imports OrbisPro.ProcessUtils
Imports OrbisPro.OrbisUtils
Imports System.IO
Imports DiscUtils.Iso9660
Imports Newtonsoft.Json

Public Class GameStarter

    Public Shared Function ReadISOFile(InputFile As String) As String
        Dim FileFor As String = String.Empty
        Dim InvalidISO As Boolean = False
        Dim RootFilesListing As New List(Of String)()
        Dim RootDirectoryListing As New List(Of String)()

        'Try to open the ISO file
        Using NewISOStream As FileStream = File.Open(InputFile, FileMode.Open)
            Try
                Dim NewCDReader As New CDReader(NewISOStream, True)

                For Each FileInISO As String In NewCDReader.GetFiles("\")
                    RootFilesListing.Add(FileInISO)
                Next

                For Each DirectoryInISO As String In NewCDReader.GetDirectories("\")
                    RootDirectoryListing.Add(DirectoryInISO)
                Next
            Catch ex As DiscUtils.InvalidFileSystemException
                'Not a default ISO9660 file
                InvalidISO = True
                Exit Try
            End Try
        End Using

        If InvalidISO Then
            'If the ISO file can't be opened then try to check the ISO using DolphinTool
            Using DolphinTool As New Process()
                DolphinTool.StartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\DolphinTool.exe"
                DolphinTool.StartInfo.Arguments = "header -i """ + InputFile + """ -j"
                DolphinTool.StartInfo.RedirectStandardOutput = True
                DolphinTool.StartInfo.UseShellExecute = False
                DolphinTool.StartInfo.CreateNoWindow = True
                DolphinTool.Start()

                Dim OutputReader As StreamReader = DolphinTool.StandardOutput
                Dim ProcessOutput As String = OutputReader.ReadToEnd()

                If ProcessOutput.Count > 0 Then
                    Dim ParamData = JsonConvert.DeserializeObject(Of DolphinJSON)(ProcessOutput)
                    If ParamData IsNot Nothing Then
                        Return "GC"
                    End If
                End If
            End Using
        Else
            'Find a specific root folder to determine the console that the file is related to
            For Each FoundDirectory As String In RootDirectoryListing
                Console.WriteLine(FoundDirectory)
                If FoundDirectory.Remove(0) = "PSP_GAME" Then
                    FileFor = "PSP"
                    Exit For
                ElseIf FoundDirectory.Remove(0) = "PS3_GAME" Then
                    FileFor = "PS3"
                    Exit For
                End If
            Next

            'Find a specific root file to determine the console that the file is related to
            For Each FoundFile As String In RootFilesListing
                Console.WriteLine(FoundFile)
                If FoundFile.Remove(0) = "SYSTEM.CNF" Then
                    FileFor = "PS2"
                    Exit For
                End If
            Next
        End If

        Return FileFor
    End Function

    Public Shared Function SupportedGameExtension(FilePath As String) As Boolean
        Select Case Path.GetExtension(FilePath)
            Case ".bin", ".gb", ".gbc", ".gba", ".cso", ".pbp", ".gcm", ".gcz", ".ciso", ".wbfs", ".vpk", ".gen", ".32X", ".sg", ".smd", ".BIN", ".exe"
                Return True
            Case ".iso"
                Select Case ReadISOFile(FilePath)
                    Case "GC", "PS2", "PS3", "PSP"
                        Return True
                    Case Else
                        Return False
                End Select
            Case Else
                Return False
        End Select
    End Function

    Public Shared Sub StartGame(GamePath As String)

        Dim EmulatorLauncherStartInfo As New ProcessStartInfo()
        Dim EmulatorLauncher As New Process() With {.StartInfo = EmulatorLauncherStartInfo}

        SuspendedThreads.Clear()

        'Select by file extension
        Select Case Path.GetExtension(GamePath)
            Case ".bin"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ePSXe\ePSXe.exe"
                EmulatorLauncherStartInfo.Arguments = "-nogui -loadbin """ + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gb"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gbc"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gba"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".cso"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".pbp"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gcm"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gcz"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".ciso"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".wbfs"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".vpk"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\Vita3K.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".gen"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".32X"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".sg"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".smd"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".iso"
                'Check the ISO file first
                Select Case ReadISOFile(GamePath)
                    Case "GC"
                        EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                        EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"

                        ProcessList.Add(EmulatorLauncher)
                        EmulatorLauncher.Start()
                    Case "PS2"
                        EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\pcsx2.exe"
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"

                        ProcessList.Add(EmulatorLauncher)
                        EmulatorLauncher.Start()
                    Case "PS3"
                        EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --no-gui"

                        ProcessList.Add(EmulatorLauncher)
                        EmulatorLauncher.Start()
                    Case "PSP"
                        EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"

                        ProcessList.Add(EmulatorLauncher)
                        EmulatorLauncher.Start()
                End Select
            Case ".BIN"
                EmulatorLauncherStartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --no-gui"

                ProcessList.Add(EmulatorLauncher)
                EmulatorLauncher.Start()
            Case ".exe"
                Dim NewExecutableProcess As New Process() With {.StartInfo = New ProcessStartInfo() With {.FileName = GamePath, .UseShellExecute = False}}
                ProcessList.Add(NewExecutableProcess)
                NewExecutableProcess.Start()
        End Select

    End Sub

End Class
