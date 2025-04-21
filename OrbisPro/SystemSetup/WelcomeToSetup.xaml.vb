Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisNetwork
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisPowerUtils
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation

Public Class WelcomeToSetup

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private LastKeyboardKey As Key

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub WelcomeToSetup_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not MainConfigFile.IniReadValue("Setup", "Done") = "True" Then
            'Start first setup
            '
            'Set initial values
            MainConfigFile.IniWriteValue("System", "Background", "Orange/Red Gradient Waves")
            CheckDeviceModel()

            SetBackground()
            DefaultSetupPart1()
        Else
            'Read existing config
            CheckDeviceModel() 'Device identification
            OrbisNotifications.NotificationDuration = CDbl(MainConfigFile.IniReadValue("Notifications", "NotificationDuration")) 'Notification duration
            If MainConfigFile.IniReadValue("Notifications", "DisableNotifications") = "true" Then OrbisNotifications.DisablePopups = True 'Enable or disable notifications

            'Setup the main window
            Dim OrbisProMainWindow As New MainWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}

            'Mute background media if set
            If MainConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then OrbisProMainWindow.BackgroundMedia.IsMuted = True

            'Show a battery indicator if the device is using a battery as power source
            If Not IsMobileDevice() Then
                OrbisProMainWindow.BatteryIndicatorImage.Source = Nothing
                OrbisProMainWindow.BatteryPercentageTextBlock.Text = ""
            End If

            'Show a WiFi indicator with connected network name and signal strenght when WiFi is connected
            If Not IsWiFiRadioOn() Then
                OrbisProMainWindow.WiFiIndicatorImage.Source = Nothing
                OrbisProMainWindow.WiFiNetworkNameStrenghtTextBlock.Text = ""
            End If

            'Show the main window
            OrbisAnimations.Animate(OrbisProMainWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(800)))
            OrbisProMainWindow.Show()

            'Close setup loader
            Close()
        End If
    End Sub

    Private Async Sub WelcomeToSetup_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            If GetAndSetGamepads() Then MainController = SharedController1
            If SharedController1 IsNot Nothing Then
                Await ReadGamepadInputAsync(CTS.Token)
            End If
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

    Private Sub ClosingAnimation_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

#Region "Setup"

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

#Region "Input"

    Private Sub SystemSetup_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Select Case e.Key
                Case Key.X
                    PlayBackgroundSound(Sounds.SelectItem)
                    DefaultSetupPart2()
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub WelcomeToSetup_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                If MainGamepadButton_A_Button_Pressed Then
                    PlayBackgroundSound(Sounds.SelectItem)
                    DefaultSetupPart2()
                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub NextButton_Click(sender As Object, e As RoutedEventArgs) Handles NextButton.Click
        PlayBackgroundSound(Sounds.SelectItem)
        DefaultSetupPart2()
    End Sub

#End Region

#Region "Background"
    Private Sub SetBackground()
        'Set the background
        Select Case MainConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
            Case "Blue Bokeh Dust"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\Bluebokehdust.mp4", UriKind.Absolute)
            Case "Golden Dust"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\Goldendust.mp4", UriKind.Absolute)
            Case "Custom"
                BackgroundMedia.Source = New Uri(MainConfigFile.IniReadValue("System", "CustomBackgroundPath"), UriKind.Absolute)
            Case Else
                BackgroundMedia.Source = Nothing
        End Select

        'Play the background media if Source is not empty
        If BackgroundMedia.Source IsNot Nothing Then
            BackgroundMedia.Play()
        End If

        'Go to first second of the background video and pause it if BackgroundAnimation = False
        If MainConfigFile.IniReadValue("System", "BackgroundAnimation") = "false" Then
            BackgroundMedia.Position = New TimeSpan(0, 0, 1)
            BackgroundMedia.Pause()
        End If

        'Mute BackgroundMedia if BackgroundMusic = False
        If MainConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then
            BackgroundMedia.IsMuted = True
        End If

        'Set width & height
        If Not MainConfigFile.IniReadValue("System", "DisplayScaling") = "AutoScaling" Then
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(WelcomeSetupWindow, WelcomeSetupCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                OrbisDisplay.SetScaling(WelcomeSetupWindow, WelcomeSetupCanvas)
            End If
        End If
    End Sub

#End Region

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
