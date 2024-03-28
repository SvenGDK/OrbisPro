Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.Net
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports SharpDX.XInput
Imports System.Threading

Public Class SetupCheckUpdates

    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private WithEvents UpdateWarningTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 2)}
    Private WaitedFor As Integer = 0
    Public Opener As String = ""

    Private WithEvents UpdateClient As New WebClient()
    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupCheckUpdates_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        WaitTimer.Start()
    End Sub

    Private Async Sub SetupCheckUpdates_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        'Check for gamepads
        If GetAndSetGamepads() Then MainController = SharedController1
        If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
    End Sub

    Private Sub SetupCheckUpdates_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupCheckUpdates_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnimation_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed

        Select Case Opener
            Case "GeneralSettings"
                PlayBackgroundSound(Sounds.Back)
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
                        CType(Win, GeneralSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "MainWindow"
                PlayBackgroundSound(Sounds.Back)
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        CType(Win, MainWindow).PauseInput = False
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Input"

    Private Sub SetupCheckUpdates_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.C Then
            BeginAnimation(OpacityProperty, ClosingAnimation)
        End If
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons
            Dim AdditionalDelayAmount As Integer = 0

            If Not PauseInput Then

                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0

                If MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                End If

                AdditionalDelayAmount += 50

            Else
                AdditionalDelayAmount += 100
            End If

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

#End Region

    Private Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        WaitedFor += 1

        If WaitedFor = 2 Then
            WaitTimer.Stop()
            WaitedFor = 0

            CheckForUpdates()
        End If
    End Sub

    Private Sub UpdateWarningTimer_Tick(sender As Object, e As EventArgs) Handles UpdateWarningTimer.Tick
        WaitedFor += 1

        If WaitedFor = 2 Then
            UpdateWarningTimer.Stop()
            WaitedFor = 0

            Process.Start(My.Computer.FileSystem.CurrentDirectory + "\OrbisProUpdate.exe")
            Windows.Application.Current.Shutdown()
        End If
    End Sub

    Private Sub CheckForUpdates()
        If IsURLValid("http://89.58.2.209/orbispro.txt") Then
            Dim OrbisProInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Computer.FileSystem.CurrentDirectory + "\OrbisPro.exe")
            Dim CurrentOrbisProVersion As String = OrbisProInfo.FileVersion

            Dim VerCheckClient As New WebClient()
            Dim NewOrbisProVersion As String = VerCheckClient.DownloadString("http://89.58.2.209/orbispro.txt")

            If CurrentOrbisProVersion < NewOrbisProVersion Then
                TopLabel.Text = "An update is available and will be downloaded."

                LoadingIndicator.SpinDuration = 5
                DownloadUpdate()
            Else
                TopLabel.Text = "OrbisPro is up to date!"

                LoadingIndicator.Spin = False
                LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

                Select Case Opener
                    Case "MainWindow", "GeneralSettings"
                        BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                        BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    Case Else
                        ContinueSetup()
                End Select

            End If
        Else
            TopLabel.Text = "Could not check for updates. Setup will continue."

            LoadingIndicator.Spin = False
            LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

            Select Case Opener
                Case "MainWindow", "GeneralSettings"
                    BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                Case Else
                    ContinueSetup()
            End Select
        End If
    End Sub

    Private Sub DownloadUpdate()
        ProgressLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        Try
            UpdateClient.DownloadFileAsync(New Uri("http://89.58.2.209/OrbisProUpdate.exe"), My.Computer.FileSystem.CurrentDirectory + "\OrbisProUpdate.exe")
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub UpdateClient_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs) Handles UpdateClient.DownloadProgressChanged
        ProgressLabel.Text = "Progress: " + e.ProgressPercentage.ToString() + " %"
    End Sub

    Private Sub UpdateClient_DownloadFileCompleted(sender As Object, e As AsyncCompletedEventArgs) Handles UpdateClient.DownloadFileCompleted
        TopLabel.Text = "Update downloaded. OrbisPro will update now."
        LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        ProgressLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        UpdateWarningTimer.Start()
    End Sub

    Private Sub ContinueSetup()
        PlayBackgroundSound(Sounds.SelectItem)

        Dim NewGamesSetup As New SetupGames() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewGamesSetup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewGamesSetup.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
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
            .Opener = "SetupCheckUpdates",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
