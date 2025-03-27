Imports System.Media
Imports CoreAudio
Imports OrbisPro.OrbisUtils

Public Class OrbisAudio

    Private Shared MainAudioDevice As MMDevice

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

        Dim NewSoundPlayer As SoundPlayer

        'Get the selected 'Navigation Audio Pack' to play the correct sound effect
        Dim AudioPack As String = MainConfigFile.IniReadValue("Audio", "Navigation Audio Pack")

        'Determine which sound to play - Some sound effects are still missing ...
        If Sound = Sounds.Start Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_start)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_start)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_start)
                    NewSoundPlayer.Play()
            End Select

        ElseIf Sound = Sounds.Message Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_message)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_message)
                    NewSoundPlayer.Play()
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.Move Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps2_move)
                    NewSoundPlayer.Play()
                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_tick)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_move)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_uimove)
                    NewSoundPlayer.Play()
            End Select

        ElseIf Sound = Sounds.Notification Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_notification)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_notification)
                    NewSoundPlayer.Play()
            End Select

        ElseIf Sound = Sounds.SelectItem Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps2_select)
                    NewSoundPlayer.Play()
                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_tick)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_select)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_uiselect)
                    NewSoundPlayer.Play()
            End Select

        ElseIf Sound = Sounds.Back Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps2_back)
                    NewSoundPlayer.Play()
                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_back)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_back)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_uiback)
                    NewSoundPlayer.Play()
            End Select

        ElseIf Sound = Sounds.Options Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps3_options)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_options)
                    NewSoundPlayer.Play()
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.CancelOptions Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"

                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_back2)
                    NewSoundPlayer.Play()
                Case "PS5"

            End Select

        ElseIf Sound = Sounds.Trophy Then

            Select Case AudioPack
                Case "PS1"

                Case "PS2"

                Case "PS3"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_trophy)
                    NewSoundPlayer.Play()
                Case "PS4"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps4_trophy)
                    NewSoundPlayer.Play()
                Case "PS5"
                    NewSoundPlayer = New SoundPlayer(My.Resources.ps5_trophy)
                    NewSoundPlayer.Play()
            End Select

        End If

    End Sub

    Public Shared Sub MasterVolumeUp()
        Dim NewDeviceEnumerator As New MMDeviceEnumerator(Guid.NewGuid())
        MainAudioDevice = NewDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
        Dim CurrentVolume As Integer = CInt(MainAudioDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100)
        If Not CurrentVolume + 1 >= 100 Then
            MainAudioDevice.AudioEndpointVolume.MasterVolumeLevelScalar += CSng(0.04)
        End If
    End Sub

    Public Shared Sub MasterVolumeDown()
        Dim NewDeviceEnumerator As New MMDeviceEnumerator(Guid.NewGuid())
        MainAudioDevice = NewDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
        Dim CurrentVolume As Integer = CInt(MainAudioDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100)

        If CurrentVolume - 1 >= 0 Then
            MainAudioDevice.AudioEndpointVolume.MasterVolumeLevelScalar -= CSng(0.04)
        End If
    End Sub

    Public Shared Sub MuteMasterVolume()
        Dim NewDeviceEnumerator As New MMDeviceEnumerator(Guid.NewGuid())
        MainAudioDevice = NewDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
        If MainAudioDevice.AudioEndpointVolume.Mute Then
            MainAudioDevice.AudioEndpointVolume.Mute = False
        Else
            MainAudioDevice.AudioEndpointVolume.Mute = True
        End If
    End Sub

    Public Shared Function GetCurrentMasterVolume() As Integer
        Dim NewDeviceEnumerator As New MMDeviceEnumerator(Guid.NewGuid())
        MainAudioDevice = NewDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
        Return CInt(MainAudioDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100)
    End Function

End Class
