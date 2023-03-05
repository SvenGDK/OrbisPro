Public Class OrbisAudio

    'Set the config file (currently fixed and should not be changed to prevent bugs)
    Public Shared ConfigFile As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\Config\Settings.ini")

    'List of possible sound effects
    Public Enum Sounds
        Start
        Trophy
        SelectItem
        Move
        Notification
        Back
        Options
        CancelOptions
    End Enum

    Public Shared Sub PlayBackgroundSound(Sound As Sounds)

        'Get the selected 'Navigation Audio Pack' to play the correct sound effect
        Dim AudioPack As String = ConfigFile.IniReadValue("Audio Settings", "Navigation Audio Pack")

        'Determine which sound to play - Some sound effects are still missing ...
        If Sound = Sounds.Start Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.start, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_start, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Move Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_move, AudioPlayMode.Background)
                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.move, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uimove, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Notification Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.notification, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_notification, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.SelectItem Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_select, AudioPlayMode.Background)
                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources._select, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uiselect, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Back Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_back, AudioPlayMode.Background)
                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.back, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uiback, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Options Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.options, AudioPlayMode.Background)
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.CancelOptions Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.back2, AudioPlayMode.Background)
                Case "PS5"

            End Select

        End If

    End Sub

End Class
