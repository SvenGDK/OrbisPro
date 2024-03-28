Imports OrbisPro.OrbisUtils

Public Class OrbisAudio

    'List of possible sound effects
    Public Enum Sounds
        Back
        CancelOptions
        Message
        Move
        Notification
        Options
        SelectItem
        Start
        Trophy
    End Enum

    Public Shared Sub PlayBackgroundSound(Sound As Sounds)

        'Get the selected 'Navigation Audio Pack' to play the correct sound effect
        Dim AudioPack As String = ConfigFile.IniReadValue("Audio", "Navigation Audio Pack")

        'Determine which sound to play - Some sound effects are still missing ...
        If Sound = Sounds.Start Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_start, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_start, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_start, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Message Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_message, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_message, AudioPlayMode.Background)
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.Move Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_move, AudioPlayMode.Background)
                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_tick, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_move, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uimove, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Notification Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_notification, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_notification, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.SelectItem Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_select, AudioPlayMode.Background)
                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_tick, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_select, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uiselect, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Back Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    My.Computer.Audio.Play(My.Resources.ps2_back, AudioPlayMode.Background)
                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_back, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_back, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_uiback, AudioPlayMode.Background)
            End Select

        ElseIf Sound = Sounds.Options Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps3_options, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_options, AudioPlayMode.Background)
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.CancelOptions Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_back2, AudioPlayMode.Background)
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.Trophy Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    My.Computer.Audio.Play(My.Resources.ps4_trophy, AudioPlayMode.Background)
                Case "PS4"
                    My.Computer.Audio.Play(My.Resources.ps4_trophy, AudioPlayMode.Background)
                Case "PS5"
                    My.Computer.Audio.Play(My.Resources.ps5_trophy, AudioPlayMode.Background)
            End Select

        End If

    End Sub

End Class
