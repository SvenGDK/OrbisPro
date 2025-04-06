Imports OrbisPro.ProcessUtils
Imports System.IO
Imports DiscUtils.Iso9660
Imports Newtonsoft.Json

Public Class GameStarter

    Public Shared Function ReadISOFile(InputFile As String) As String
        Dim FileFor As String = ""
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
            Catch otherex As Exception
                InvalidISO = True
                Exit Try
            End Try
        End Using

        'Further checking if the ISO can't be read
        If InvalidISO Then
            'Check the ISO using DolphinTool
            Using DolphinTool As New Process()
                DolphinTool.StartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\DolphinTool.exe"
                DolphinTool.StartInfo.Arguments = "header -i """ + InputFile + """ -j"
                DolphinTool.StartInfo.RedirectStandardOutput = True
                DolphinTool.StartInfo.UseShellExecute = False
                DolphinTool.StartInfo.CreateNoWindow = True
                DolphinTool.Start()

                Dim OutputReader As StreamReader = DolphinTool.StandardOutput
                Dim ProcessOutput As String = OutputReader.ReadToEnd()

                If ProcessOutput.Length > 0 Then
                    Try
                        Dim ParamData As DolphinJSON = JsonConvert.DeserializeObject(Of DolphinJSON)(ProcessOutput)
                        If ParamData IsNot Nothing Then
                            FileFor = "GC"
                            Return "GC"
                        End If
                    Catch ex As Exception
                        Exit Try
                    End Try
                End If
            End Using
        Else
            'Valid ISO
            'Find a specific root folder to determine the console that the file is related to
            For Each FoundDirectory As String In RootDirectoryListing
                If FoundDirectory.Remove(0) = "PSP_GAME" Then
                    FileFor = "PSP"
                    Exit For
                ElseIf FoundDirectory.Remove(0) = "PS3_GAME" Then
                    FileFor = "PS3"
                    Exit For
                End If
            Next

            For Each FoundFile As String In RootFilesListing
                If FoundFile.Remove(0) = "SYSTEM.CNF" Then
                    FileFor = "PS2" 'could be also PS1, but PS1 games use the .BIN format instead of ISO
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
        Dim EmulatorLauncher As New Process() With {.StartInfo = EmulatorLauncherStartInfo, .EnableRaisingEvents = True}

        'Select by file extension
        Select Case Path.GetExtension(GamePath)
            Case ".bin"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ePSXe\ePSXe.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ePSXe\ePSXe.exe")
                EmulatorLauncherStartInfo.Arguments = "-nogui -loadbin """ + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gb"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gbc"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gba"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".cso"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".pbp"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gcm"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe")
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gcz"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe")
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".ciso"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe")
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".wbfs"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe")
                EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".vpk"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\Vita3K.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\Vita3K.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".gen"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".32X"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".sg"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".smd"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".iso"
                'Check the ISO file first
                Select Case ReadISOFile(GamePath)
                    Case "GC"
                        EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe"
                        EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\Dolphin.exe")
                        EmulatorLauncherStartInfo.Arguments = "-e """ + GamePath + """"
                        ActiveProcess = EmulatorLauncher
                        ActiveProcess.Start()
                    Case "PS2"
                        EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\pcsx2.exe"
                        EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\pcsx2.exe")
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --nogui --fullboot --portable"
                        ActiveProcess = EmulatorLauncher
                        ActiveProcess.Start()
                    Case "PS3"
                        EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
                        EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe")
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --no-gui"
                        ActiveProcess = EmulatorLauncher
                        ActiveProcess.Start()
                    Case "PSP"
                        EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe"
                        EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\PPSSPPWindows64.exe")
                        EmulatorLauncherStartInfo.Arguments = """" + GamePath + """"
                        ActiveProcess = EmulatorLauncher
                        ActiveProcess.Start()
                End Select
            Case ".BIN"
                EmulatorLauncherStartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
                EmulatorLauncherStartInfo.WorkingDirectory = Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe")
                EmulatorLauncherStartInfo.Arguments = """" + GamePath + """ --no-gui"
                ActiveProcess = EmulatorLauncher
                ActiveProcess.Start()
            Case ".exe"
                Dim NewExecutableProcess As New Process() With {.EnableRaisingEvents = True,
                    .StartInfo = New ProcessStartInfo() With {.FileName = GamePath, .WorkingDirectory = Path.GetDirectoryName(GamePath), .UseShellExecute = False}}
                ActiveProcess = NewExecutableProcess
                ActiveProcess.Start()
        End Select

    End Sub

    Public Shared Function GetGameAlias(ExecutableFileName As String) As String
        Dim AliasName As String

        If Path.GetFileName(ExecutableFileName) = "AC4BFMP.exe" Then
            AliasName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "AC4BFSP.exe" Then
            AliasName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACC.exe" Then
            AliasName = "ACRogue"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACS.exe" Then
            AliasName = "ACSyndicate"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACU.exe" Then
            AliasName = "ACUnity"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACMirage.exe" Then
            AliasName = "ACMirage"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOdyssey.exe" Then
            AliasName = "ACOdyssey"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOrigins.exe" Then
            AliasName = "ACOrigins"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACValhalla.exe" Then
            AliasName = "ACValhalla"
        ElseIf Path.GetFileName(ExecutableFileName) = "Audiosurf.exe" Then
            AliasName = "Audiosurf"
        ElseIf Path.GetFileName(ExecutableFileName) = "Avowed.exe" Then
            AliasName = "Avowed"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3.exe" Then
            AliasName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3_dx11.exe" Then
            AliasName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Blasphemous 2.exe" Then
            AliasName = "Blasphemous2"
        ElseIf Path.GetFileName(ExecutableFileName) = "Croc64.exe" Then
            AliasName = "Croc"
        ElseIf Path.GetFileName(ExecutableFileName) = "Cyberpunk2077.exe" Then
            AliasName = "Cyberpunk"
        ElseIf Path.GetFileName(ExecutableFileName) = "D2R.exe" Then
            AliasName = "Diablo2R"
        ElseIf Path.GetFileName(ExecutableFileName) = "DevilMayCry5.exe" Then
            AliasName = "DMC5"
        ElseIf Path.GetFileName(ExecutableFileName) = "Diablo IV.exe" Then
            AliasName = "DiabloIV"
        ElseIf Path.GetFileName(ExecutableFileName) = "eldenring.exe" Then
            AliasName = "EldenRing"
        ElseIf Path.GetFileName(ExecutableFileName) = "FarCry5.exe" Then
            AliasName = "FarCry5"
        ElseIf Path.GetFileName(ExecutableFileName) = "ffxvi.exe" Then
            AliasName = "FFXVI"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteLauncher.exe" Then
            AliasName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteClient-Win64-Shipping.exe" Then
            AliasName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "ForzaHorizon5.exe" Then
            AliasName = "ForzaHorizon5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GameApp_PcDx11_x64Final.exe" Then
            AliasName = "TeamSonicRacing"
        ElseIf Path.GetFileName(ExecutableFileName) = "GoW.exe" Then
            AliasName = "GoW"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA3.exe" Then
            AliasName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA5.exe" Then
            AliasName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAIV.exe" Then
            AliasName = "GTA4"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAV.exe" Then
            AliasName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta-vc.exe" Then
            AliasName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta_sa.exe" Then
            AliasName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAVLauncher.exe" Then
            AliasName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "Hearthstone.exe" Then
            AliasName = "Hearthstone"
        ElseIf Path.GetFileName(ExecutableFileName) = "HorizonForbiddenWest.exe" Then
            AliasName = "HorizonFW"
        ElseIf Path.GetFileName(ExecutableFileName) = "LibertyCity.exe" Then
            AliasName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP.exe" Then
            AliasName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP-Win64-Shipping.exe" Then
            AliasName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2.exe" Then
            AliasName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2-Win64-Shipping.exe" Then
            AliasName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "Palworld.exe" Then
            AliasName = "Palworld"
        ElseIf Path.GetFileName(ExecutableFileName) = "MaxPayne3.exe" Then
            AliasName = "MaxPayne3"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID.exe" Then
            AliasName = "MGS1"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID2.exe" Then
            AliasName = "MGS2"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID3.exe" Then
            AliasName = "MGS3"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvmgo.exe" Then
            AliasName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvtpp.exe" Then
            AliasName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "MilesMorales.exe" Then
            AliasName = "SpiderManMM"
        ElseIf Path.GetFileName(ExecutableFileName) = "NFS11.exe" Then
            AliasName = "NFSHotPursuit"
        ElseIf Path.GetFileName(ExecutableFileName) = "r5apex.exe" Then
            AliasName = "ApexLegends"
        ElseIf Path.GetFileName(ExecutableFileName) = "re4.exe" Then
            AliasName = "RE4N"
        ElseIf Path.GetFileName(ExecutableFileName) = "Resident Evil Village" Then
            AliasName = "REVillage"
        ElseIf Path.GetFileName(ExecutableFileName) = "RiftApart.exe" Then
            AliasName = "RatchetClankRA"
        ElseIf Path.GetFileName(ExecutableFileName) = "Ronin.exe" Then
            AliasName = "RoR"
        ElseIf Path.GetFileName(ExecutableFileName) = "ROTTR.exe" Then
            AliasName = "ROTombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "SanAndreas.exe" Then
            AliasName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "sekiro.exe" Then
            AliasName = "Sekiro"
        ElseIf Path.GetFileName(ExecutableFileName) = "SHProto.exe" Then
            AliasName = "SH2R"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicFrontiers.exe" Then
            AliasName = "SonicFrontiers"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicMania.exe" Then
            AliasName = "SonicMania"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicSuperstars.exe" Then
            AliasName = "SonicSuperstars"
        ElseIf Path.GetFileName(ExecutableFileName) = "SquirrelGun.exe" Then
            AliasName = "Swag"
        ElseIf Path.GetFileName(ExecutableFileName) = "TEKKEN 8.exe" Then
            AliasName = "Tekken8"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i.exe" Then
            AliasName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i-l.exe" Then
            AliasName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "THPS12.exe" Then
            AliasName = "TH1+2"
        ElseIf Path.GetFileName(ExecutableFileName) = "tomb123.exe" Then
            AliasName = "TR1-3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tomb Raider.exe" Then
            AliasName = "TombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tropico6.exe" Then
            AliasName = "Tropico6"
        ElseIf Path.GetFileName(ExecutableFileName) = "TslGame.exe" Then
            AliasName = "PUBG"
        ElseIf Path.GetFileName(ExecutableFileName) = "UnleashedRecomp.exe" Then
            AliasName = "SonicUnleashed"
        ElseIf Path.GetFileName(ExecutableFileName) = "ViceCity.exe" Then
            AliasName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.x64.exe" Then
            AliasName = "Warframe"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.exe" Then
            AliasName = "Warframe"
        ElseIf Path.GetFileName(ExecutableFileName) = "witcher3.exe" Then
            AliasName = "TW3"
        ElseIf Path.GetFileName(ExecutableFileName) = "WRCG.exe" Then
            AliasName = "WRCGen"


        ElseIf Path.GetFileName(ExecutableFileName) = "Battle.net Launcher.exe" Then
            AliasName = "BattleNet"
        ElseIf Path.GetFileName(ExecutableFileName) = "EALauncher.exe" Then
            AliasName = "EALauncher"
        ElseIf Path.GetFileName(ExecutableFileName) = "EpicGamesLauncher.exe" Then
            AliasName = "EpicGamesLauncher"
        ElseIf Path.GetFileName(ExecutableFileName) = "steam.exe" Then
            AliasName = "Steam"
        ElseIf Path.GetFileName(ExecutableFileName) = "UbisoftConnect.exe" Then
            AliasName = "UbisoftConnect"

        Else
            Return String.Empty
        End If

        Return AliasName
    End Function

    Public Shared Function CheckForExistingIconAsset(ExecutableFileName As String) As String
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\" + GetGameAlias(ExecutableFileName) + "_icon.jpg") Then
            Return FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\" + GetGameAlias(ExecutableFileName) + "_icon.jpg"
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function CheckForExistingBackgroundAsset(ExecutableFileName As String) As String
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameBackgrounds\" + GetGameAlias(ExecutableFileName) + "_BG.jpg") Then
            Return FileIO.FileSystem.CurrentDirectory + "\Assets\GameBackgrounds\" + GetGameAlias(ExecutableFileName) + "_BG.jpg"
        Else
            Return String.Empty
        End If
    End Function

End Class
