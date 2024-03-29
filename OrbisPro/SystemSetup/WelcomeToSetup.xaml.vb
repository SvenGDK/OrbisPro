Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation

Public Class WelcomeToSetup

    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Dim Part2Done As Boolean = False

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub WelcomeToSetup_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If ConfigFile.IniReadValue("Setup", "Done") = "True" Then

            'Read config
            CheckDeviceModel()
            SetUserName()
            OrbisNotifications.NotificationDuration = CDbl(ConfigFile.IniReadValue("Notifications", "NotificationDuration"))
            If ConfigFile.IniReadValue("Notifications", "DisableNotifications") = "true" Then OrbisNotifications.DisablePopups = True

            'Open the main window
            Dim OrbisProMainWindow As New MainWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
            OrbisAnimations.Animate(OrbisProMainWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(800)))
            If ConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then
                OrbisProMainWindow.BackgroundMedia.IsMuted = True
            End If
            OrbisProMainWindow.Show()

            Close()
        Else

            If MsgBox("Welcome to the OrbisPro 0.1 Beta." +
                      vbCrLf +
                      vbCrLf +
                      "This application is still in development and you may encouter some bugs while using it." +
                      vbCrLf +
                      "The Beta is only optimized for the Full HD (1920x1080) screen resolution at 100% scaling." +
                      vbCrLf +
                      "By clicking OK you accept that OrbisPro checks your device model, installed games & applications." +
                      vbCrLf +
                      vbCrLf +
                      "This dialog will only be shown on the very first setup.") = MsgBoxResult.Ok Then

                'Check the device that uses OrbisPro and open a customized setup on specific devices
                Dim DeviceModel As String = CheckDeviceModel()

                If Not String.IsNullOrEmpty(DeviceModel) Then
                    Select Case DeviceModel
                        Case "ROG Ally RC71L_RC71L"
                            ConfigFile.IniWriteValue("System", "Background", "Blue Bubbles")
                            SetupAllyPart1()
                        Case Else
                            ConfigFile.IniWriteValue("System", "Background", "Orange/Red Gradient Waves")
                            SetBackground()
                            DefaultSetupPart1()
                    End Select
                Else
                    ConfigFile.IniWriteValue("System", "Background", "Orange/Red Gradient Waves")
                    SetBackground()
                    DefaultSetupPart1()
                End If

            End If

        End If
    End Sub

    Private Async Sub WelcomeToSetup_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub WelcomeToSetup_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub WelcomeToSetup_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        Select Case SharedDeviceModel
            Case DeviceModel.PC

            Case DeviceModel.ROGAlly
                'Change the background
                BackgroundMedia.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
                BackgroundMedia.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

                'Continue setup
                SetupAllyPart2()
            Case DeviceModel.SteamDeck

            Case DeviceModel.LegionGo

        End Select
    End Sub

    Private Sub ClosingAnimation_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

#Region "Default Setup"

    Private Sub DefaultSetupPart1()
        Dim TitleAndBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 284, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim InnerLabelsLeftAnim As New DoubleAnimation With {.From = 1920, .To = 444, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

        MainSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        SecondSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ROGDarkBox.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 0.7, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ROGCheckUpdatesLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCheckGamesLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCheckAppsLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCustomizeLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        'ROGSetupEmuLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        NextButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        NextButton.Focus()
    End Sub

    Private Sub DefaultSetupPart2()
        Dim NewUpdateChecker As New SetupCheckUpdates() With {.ShowActivated = True, .Left = Left, .Top = Top}
        NewUpdateChecker.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewUpdateChecker.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#End Region

#Region "Ally Setup"

    Private Sub SetupAllyPart1()
        BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\rog.mp4", UriKind.Absolute)
        BackgroundMedia.Play()
    End Sub

    Private Sub SetupAllyPart2()
        Dim TitleAndBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 284, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim InnerLabelsLeftAnim As New DoubleAnimation With {.From = 1920, .To = 444, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

        MainSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        SecondSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ROGDarkBox.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 0.7, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ROGCheckUpdatesLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCheckGamesLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCheckAppsLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ROGCustomizeLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        'ROGSetupEmuLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        NextButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        NextButton.Focus()
    End Sub

    Private Sub SetupAllyPart3()
        Dim NewUpdateChecker As New SetupCheckUpdates() With {.ShowActivated = True, .Left = Left, .Top = Top}
        NewUpdateChecker.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewUpdateChecker.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#End Region

#Region "Input"

    Private Sub SystemSetup_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        'The first pressed key/button (must) be HOME to continue
        If e.Key = Key.X Then
            PlayBackgroundSound(Sounds.SelectItem)

            Select Case SharedDeviceModel

                Case DeviceModel.ROGAlly
                    SetupAllyPart3()
                Case Else
                    DefaultSetupPart2()

            End Select
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

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                If MainGamepadButton_A_Button_Pressed Then
                    PlayBackgroundSound(Sounds.SelectItem)

                    Select Case SharedDeviceModel
                        Case DeviceModel.ROGAlly
                            SetupAllyPart3()
                        Case Else
                            DefaultSetupPart2()
                    End Select
                End If

                AdditionalDelayAmount += 45
            Else
                AdditionalDelayAmount += 500
            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

#End Region

    Public Sub SetBackground()
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

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "WelcomeToSetup",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
