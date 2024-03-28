﻿Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports SharpDX.XInput

Public Class SystemDialog

    Public Opener As String
    Public MessageTitle As String
    Public MessageDescription As String
    Public MessageType As SystemMessage

    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}

    Public SetupStep As Boolean = False

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SystemDialog_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Set title and message
        If Not String.IsNullOrEmpty(MessageTitle) Then
            SetupTitle.Text = MessageTitle
        End If
        If Not String.IsNullOrEmpty(MessageDescription) Then
            SystemDialogTextBlock.Text = MessageDescription
        End If
    End Sub

    Private Async Sub SystemDialog_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub SystemDialog_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

    Private Sub SystemDialog_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "BluetoothSettings"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.BluetoothSettings" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, BluetoothSettings).Activate()
                        CType(Win, BluetoothSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "Downloads"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.Downloads" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, Downloads).PauseInput = False
                        CType(Win, Downloads).Activate()
                        Exit For
                    End If
                Next
            Case "FileExplorer"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, FileExplorer).Activate()
                        CType(Win, FileExplorer).PauseInput = False
                        Exit For
                    End If
                Next
            Case "GameLibrary"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GameLibrary" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, GameLibrary).Activate()
                        CType(Win, GameLibrary).PauseInput = False
                        Exit For
                    End If
                Next
            Case "GeneralSettings"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, GeneralSettings).Activate()
                        CType(Win, GeneralSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        CType(Win, MainWindow).PauseInput = False
                        Exit For
                    End If
                Next
            Case "OpenWindows"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.OpenWindows" Then
                        CType(Win, OpenWindows).Activate()
                        CType(Win, OpenWindows).PauseInput = False
                        Exit For
                    End If
                Next
            Case "SetupApps"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupApps" Then
                        CType(Win, SetupApps).Activate()
                        Exit For
                    End If
                Next
            Case "SetupGames"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupGames" Then
                        CType(Win, SetupGames).Activate()
                        Exit For
                    End If
                Next
            Case "SetupPS3"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupPS3" Then

                        If SetupStep Then
                            CType(Win, SetupPS3).RequirementsRead = True
                            CType(Win, SetupPS3).RequirementsReadCheckBox.IsChecked = True
                            CType(Win, SetupPS3).ReadRequirementsButton.BorderBrush = Nothing
                            CType(Win, SetupPS3).DownloadFirmwareButton.Focus()
                        End If

                        CType(Win, SetupPS3).AdditionalPauseDelay = 100
                        CType(Win, SetupPS3).Activate()

                        Exit For
                    End If
                Next
            Case "SetupCustomize"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupCustomize" Then
                        CType(Win, SetupCustomize).Activate()
                        Exit For
                    End If
                Next
            Case "SystemMediaPlayer"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SystemMediaPlayer" Then
                        CType(Win, SystemMediaPlayer).Activate()
                        Exit For
                    End If
                Next
            Case "WelcomeToSetup"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.WelcomeToSetup" Then
                        CType(Win, WelcomeToSetup).Activate()
                        Exit For
                    End If
                Next
            Case "WifiSettings"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.WifiSettings" Then
                        CType(Win, WifiSettings).Activate()
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Input"

    Private Sub SystemDialog_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.O Then
            BeginAnimation(OpacityProperty, ClosingAnimation)
        End If
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim AdditionalDelayAmount As Integer = 0

            If Not PauseInput Then

                Dim MainGamepadState As State = MainController.GetState()
                Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then

                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then

                ElseIf MainGamepadButton_DPad_Left_Pressed Then

                ElseIf MainGamepadButton_DPad_Right_Pressed Then

                ElseIf MainGamepadButton_DPad_Up_Pressed Then

                ElseIf MainGamepadButton_DPad_Down_Pressed Then

                ElseIf MainGamepadButton_Start_Button_Pressed Then

                End If

                AdditionalDelayAmount += 45
            Else
                AdditionalDelayAmount += 500
            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub SetBackground()
        'Set the background
        Select Case ConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
            Case "Custom"
                BackgroundMedia.Source = New Uri(ConfigFile.IniReadValue("System", "CustomBackgroundPath"), UriKind.Absolute)
            Case Else
                BackgroundMedia.Source = Nothing
        End Select

        'Play the background media if Source is not empty
        If BackgroundMedia.Source IsNot Nothing Then
            BackgroundMedia.Play()
        End If

        'Go to first second of the background video and pause it if BackgroundAnimation = False
        If ConfigFile.IniReadValue("System", "BackgroundAnimation") = "false" Then
            BackgroundMedia.Position = New TimeSpan(0, 0, 1)
            BackgroundMedia.Pause()
        End If

        'Mute BackgroundMedia if BackgroundMusic = False
        If ConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then
            BackgroundMedia.IsMuted = True
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

End Class
