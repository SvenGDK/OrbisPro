Imports System.IO

Public Class OrbisCDVDManager

    Public Shared GameTitle As String = ""
    Public Shared GameID As String = ""

    'Check the content of the disc
    Public Shared Function CheckCDVDContent(Optional DriveName As String = "") As (GameID As String, Platform As String)
        If File.Exists(DriveName + "\SYSTEM.CNF") Then 'Used on PS1 & PS2 discs

            If File.ReadAllLines(DriveName + "\SYSTEM.CNF")(0).Split("="c)(0).Trim() = "BOOT2" Then 'This is a PS2 disc (always?)
                GameID = File.ReadAllLines(DriveName + "\SYSTEM.CNF")(0).Split("="c)(1).Trim().Replace("cdrom0:\", "").Replace(";1", "").Replace(".", "").Replace("_", "-").Trim() 'Return as readable ID
                Return (GameID, "PS2")
            ElseIf File.ReadAllLines(DriveName + "\SYSTEM.CNF")(0).Split("="c)(0).Trim() = "BOOT" Then 'This is a PS1 disc (always?)
                GameID = File.ReadAllLines(DriveName + "\SYSTEM.CNF")(0).Split("="c)(1).Trim().Replace("cdrom:\", "").Replace(";1", "").Replace(".", "").Replace("_", "-").Trim() 'Return as readable ID
                Return (GameID, "PS1")
            Else
                Return ("", "")
            End If

        ElseIf File.Exists(DriveName + "\PARAM.SFO") Then 'Used on PS3 discs
            Return ("", "")
        Else
            'Check other kinds of files
            Dim CheckForCDAFile() As String = Directory.GetFiles(DriveName, "*.cda") 'This can be an audio disc and/or many more - this one currently reports as PC-Engine game

            If CheckForCDAFile.Length > 0 Then
                Return ("", "PCE")
            Else
                Return ("", "")
            End If

        End If

    End Function

    'Select the emulator and start the disc
    Public Shared Sub StartCDVD(Platform As String, DiscPath As String)

        'Select by reported platform
        Select Case Platform
            Case "PS1"

                'OrbisPro currently uses ePSXe as PS1 emulator as it supports disc booting in CLI
                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ePSXe\ePSXe.exe"
                Dim EmuArgs As String = "-nogui"

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

            Case "PS2"

                'Boot PCSX2 and use the portable mode
                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\pcsx2.exe"
                Dim EmuArgs As String = "--nogui --usecd --fullboot --portable"

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

            Case "PCE"

                'Mednafen seems to be the only emulator that supports physical discs (0.9.37.1)
                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\mednafen\mednafen.exe"
                Dim EmuArgs As String = "-physcd " + DiscPath.Replace("\", "")

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

        End Select

    End Sub

End Class
