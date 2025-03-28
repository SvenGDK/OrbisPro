Imports LibVLCSharp.Shared
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Public Class SystemMediaPlayer

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private LastKeyboardKey As Key

    Private IsMediaPlayerVideoViewReady As Boolean = False
    Private WithEvents NewLibVLC As LibVLC
    Private WithEvents NewMediaPlayer As MediaPlayer

    Public Opener As String = ""
    Public VideoFile As String = String.Empty

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SystemMediaPlayer_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Wait until MediaPlayerVideoView is ready
        WaitTimer.Start()
    End Sub

    Private Async Sub SystemMediaPlayer_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            'ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub SystemMediaPlayer_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SystemMediaPlayer_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "Downloads"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.Downloads" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, Downloads).Activate()
                        CType(Win, Downloads).PauseInput = False
                        Exit For
                    End If
                Next
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, FileExplorer).Activate()
                        CType(Win, FileExplorer).PauseInput = False
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        CType(Win, MainWindow).PauseInput = False
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Media Player Loading"

    Private Sub MediaPlayerVideoView_Loaded(sender As Object, e As RoutedEventArgs) Handles MediaPlayerVideoView.Loaded
        'Init LibVLC after MediaPlayerVideoView is loaded
        NewLibVLC = New LibVLC(False, Nothing)
        NewMediaPlayer = New MediaPlayer(NewLibVLC)
        'Set the MediaPlayer
        MediaPlayerVideoView.MediaPlayer = NewMediaPlayer
        IsMediaPlayerVideoViewReady = True
    End Sub

    Private Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        If IsMediaPlayerVideoViewReady Then
            WaitTimer.Stop()

            If Not String.IsNullOrEmpty(VideoFile) Then
                PlayNewVideoFile(VideoFile)
            End If
        End If
    End Sub

#End Region

#Region "Input"

    Private Sub SystemMediaPlayer_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Select Case e.Key
                Case Key.A
                    PauseMedia()
                Case Key.C
                    StopMedia()
                Case Key.X
                    PauseMedia()
                Case Key.Left
                    RewindMedia(10000)
                Case Key.Right
                    FastForwardMedia(10000)
                Case Key.Escape
                    StopMedia()
                    BeginAnimation(OpacityProperty, ClosingAnimation)
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SystemMediaPlayer_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0
                Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    PauseMedia()
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    StopMedia()
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    PauseMedia()
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    RewindMedia(1000)
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    FastForwardMedia(1000)
                ElseIf MainGamepadButton_Back_Button_Pressed Then
                    StopMedia()
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Start_Button_Pressed Then

                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

#End Region

#Region "Media Player Options"

    Private Sub PlayNewVideoFile(InputVideoFile As String)
        Using NewMedia = New Media(NewLibVLC, New Uri(InputVideoFile, UriKind.RelativeOrAbsolute), Nothing)
            MediaPlayerVideoView.MediaPlayer.Play(NewMedia)
        End Using
    End Sub

    Private Sub PauseMedia()
        If MediaPlayerVideoView.MediaPlayer.IsPlaying Then
            MediaPlayerVideoView.MediaPlayer.SetPause(True)
        Else
            MediaPlayerVideoView.MediaPlayer.SetPause(False)
        End If
    End Sub

    Private Sub StopMedia()
        If MediaPlayerVideoView.MediaPlayer.IsPlaying Then
            MediaPlayerVideoView.MediaPlayer.Stop()
        End If
    End Sub

    Private Sub FastForwardMedia(ByValue As Integer)
        If MediaPlayerVideoView.MediaPlayer.IsPlaying Then
            Dim ForwardByValue As Long = ByValue
            'Only fast forward when staying below media duration
            If MediaPlayerVideoView.MediaPlayer.Time + ForwardByValue < MediaPlayerVideoView.MediaPlayer.Media.Duration Then
                MediaPlayerVideoView.MediaPlayer.Time = MediaPlayerVideoView.MediaPlayer.Time + ForwardByValue
            End If
        End If
    End Sub

    Private Sub RewindMedia(ByValue As Integer)
        If MediaPlayerVideoView.MediaPlayer.IsPlaying Then
            Dim RewindByValue As Long = ByValue
            'Do not rewind below 0
            If MediaPlayerVideoView.MediaPlayer.Time - RewindByValue >= 0 Then
                MediaPlayerVideoView.MediaPlayer.Time = MediaPlayerVideoView.MediaPlayer.Time - RewindByValue
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
            .Opener = "SystemMediaPlayer",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
