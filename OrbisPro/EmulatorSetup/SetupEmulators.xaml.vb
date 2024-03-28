﻿Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports SharpDX.XInput

Public Class SetupEmulators

    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Public Opener As String = ""

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupEmulators_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        SetupPS3Button.Focus()
    End Sub

    Private Async Sub SetupEmulators_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub SetupEmulators_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupEmulators_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "GeneralSettings"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
                        CType(Win, GeneralSettings).PauseInput = False
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Focus Changes"

    Private Sub SetupPS3Button_GotFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS3Button.GotFocus
        SetupPS3Button.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SetupPS3Button_LostFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS3Button.LostFocus
        SetupPS3Button.BorderBrush = Nothing
    End Sub

    Private Sub SetupPSVitaButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles SetupPSVitaButton.GotFocus
        SetupPSVitaButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SetupPSVitaButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles SetupPSVitaButton.LostFocus
        SetupPSVitaButton.BorderBrush = Nothing
    End Sub

#End Region

#Region "Input"

    Private Sub SetupEmulators_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.X Then
            If SetupPS3Button.IsFocused Then
                OpenPS3Setup()
            ElseIf SetupPSVitaButton.IsFocused Then
                OpenPSVitaSetup()
            End If
        ElseIf e.Key = Key.C Then
            BeginAnimation(OpacityProperty, ClosingAnimation)
        ElseIf e.Key = Key.Up Then
            MoveUp()
        ElseIf e.Key = Key.Down Then
            MoveDown()
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

                If MainGamepadButton_A_Button_Pressed Then
                    If SetupPS3Button.IsFocused Then
                        OpenPS3Setup()
                    ElseIf SetupPSVitaButton.IsFocused Then
                        OpenPSVitaSetup()
                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
                End If

                AdditionalDelayAmount += 50
            Else
                AdditionalDelayAmount += 500
            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            SelectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub MoveUp()
        PlayBackgroundSound(Sounds.Move)

        If SetupPS3Button.IsFocused Then
            SetupPSVitaButton.Focus()
        ElseIf SetupPSVitaButton.IsFocused Then
            SetupPS3Button.Focus()
        End If
    End Sub

    Private Sub MoveDown()
        PlayBackgroundSound(Sounds.Move)

        If SetupPS3Button.IsFocused Then
            SetupPSVitaButton.Focus()
        ElseIf SetupPSVitaButton.IsFocused Then
            SetupPS3Button.Focus()
        End If
    End Sub

    Public Sub OpenPS3Setup()
        PlayBackgroundSound(Sounds.SelectItem)

        PauseInput = True

        Dim NewPS3Setup As New SetupPS3() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "SetupEmulators"}
        NewPS3Setup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewPS3Setup.Show()
    End Sub

    Public Sub OpenPSVitaSetup()
        PlayBackgroundSound(Sounds.SelectItem)

        PauseInput = True

        Dim NewPSVitaSetup As New SetupPSVita() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "SetupEmulators"}
        NewPSVitaSetup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewPSVitaSetup.Show()
    End Sub

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

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "SetupEmulators",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
