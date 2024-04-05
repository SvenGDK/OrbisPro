﻿Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class CopyWindow

    Public WithEvents CopyWorker As New BackgroundWorker() With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}
    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Private LastKeyboardKey As Key
    Public CopyFrom As String
    Public CopyTo As String

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub CopyWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        If File.Exists(CopyTo) Then
            CopyDescriptionTextBlock.Text = "File already exists at destination." + vbCrLf + "Overwrite ?"
            CopyFromToTextBlock.Text = "From :" + vbCrLf + CopyFrom + vbCrLf + "To :" + vbCrLf + CopyTo
            CrossButtonLabel.Text = "Confirm"
        Else
            CopyDescriptionTextBlock.Text = "Copying, please wait ..."
            CopyFromToTextBlock.Text = "From :" + vbCrLf + CopyFrom + vbCrLf + "To :" + vbCrLf + CopyTo
            CopyStatusTextBlock.Text = "0 of " + CopyProgressBar.Maximum.ToString
            CopyWorker.RunWorkerAsync()
        End If
    End Sub

    Private Async Sub CopyWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub CopyWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub CopyWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed

        'Reload the files in the File Explorer
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.FileExplorer" Then
                Dim LastFileExplorerPath As String = CType(Win, FileExplorer).LastPath
                CType(Win, FileExplorer).OpenNewFolder(LastFileExplorerPath)
                Exit For
            End If
        Next

        Close()
    End Sub

#Region "CopyWorker Events"

    Private Sub CopyWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles CopyWorker.DoWork
        If e.Argument Is Nothing Then
            File.Copy(CopyFrom, CopyTo, True)
            CopyWorker.ReportProgress(1)
        End If
    End Sub

    Private Sub CopyWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles CopyWorker.ProgressChanged
        If Not Dispatcher.CheckAccess() Then
            Dispatcher.BeginInvoke(Sub() CopyStatusTextBlock.Text = "File " + e.ProgressPercentage.ToString + " of " + CopyProgressBar.Maximum.ToString)
        Else
            CopyStatusTextBlock.Text = e.ProgressPercentage.ToString + " of " + CopyProgressBar.Maximum.ToString
        End If

        CopyProgressBar.Value += 1
    End Sub

    Private Sub CopyWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles CopyWorker.RunWorkerCompleted
        WindowTitle.Text = "Done Copying"
        CopyDescriptionTextBlock.Text = "Copy completed." + vbCrLf + "Close with Cross (X) or Circle (O)."
        CrossButtonLabel.Text = "Close"
        BackLabel.Text = "Close"
    End Sub

#End Region

#Region "Input"

    Private Sub CopyWindow_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Select Case e.Key
                Case Key.C
                    If CopyDescriptionTextBlock.Text.Contains("already exists") Then
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If

                    If BackLabel.Text = "Close" Then
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    ElseIf BackLabel.Text = "Cancel" Then
                        CopyWorker.CancelAsync()
                    End If
                Case Key.X
                    If CopyDescriptionTextBlock.Text.Contains("already exists") Then
                        CopyWorker.RunWorkerAsync()
                        CrossButtonLabel.Text = "Hide"
                    End If

                    If CrossButtonLabel.Text = "Close" Then
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub CopyWindow_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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

            End If

            MainGamepadPreviousState = MainGamepadState

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            HideButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub SetBackground()
        'Set the background
        Select Case ConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
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

        'Set width & height
        If Not ConfigFile.IniReadValue("System", "DisplayScaling") = "AutoScaling" Then
            Dim SplittedValues As String() = ConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SystemCopyWindow, SystemCopyCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = ConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SystemCopyWindow, SystemCopyCanvas)
            End If
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
            .Opener = "CopyWindow",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
