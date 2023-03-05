Imports System.IO

Public Class GameStarter

    Public Shared Sub StartGame(GamePath As String)

        'Select by file extension
        Select Case Path.GetExtension(GamePath)
            Case ".bin"

                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\ePSXe\ePSXe.exe"
                Dim EmuArgs As String = "-nogui -loadbin """ + GamePath + """"

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

            Case ".iso"

                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\pcsx2.exe"
                Dim EmuArgs As String = """" + GamePath + """ --nogui --fullboot --portable"

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

            Case ".BIN"

                Dim Emu As String = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
                Dim EmuArgs As String = """" + GamePath + """ --no-gui"

                Using EmuLauncher As New Process()
                    EmuLauncher.StartInfo.FileName = Emu
                    EmuLauncher.StartInfo.Arguments = EmuArgs
                    EmuLauncher.Start()
                End Using

        End Select

    End Sub

End Class
