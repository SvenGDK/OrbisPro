Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports SharpDX.XInput
Imports System.Threading
Imports System.Windows.Threading

Public Class SetupPS3

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents PS3Emulator As Process
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private LastKeyboardKey As Key

    Public Opener As String = ""
    Public RequirementsRead As Boolean = False
    Public FirmwareDownloadCompleted As Boolean = False

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True
    Public AdditionalPauseDelay As Integer = 0

#Region "Window Events"

    Private Sub SetupPS3Config_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        ReadRequirementsButton.Focus()
    End Sub

    Private Async Sub SetupPS3_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub SetupPS3_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupPS3_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "GeneralSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
                        CType(Win, GeneralSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "SetupEmulators"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupEmulators" Then
                        CType(Win, SetupEmulators).Activate()
                        CType(Win, SetupEmulators).PauseInput = False
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

    Private Sub ReadRequirements()
        'Pause input on this window
        PauseInput = True

        'Open requirements window
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = Top,
            .Left = Left,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "SetupPS3",
            .MessageTitle = "RPCS3 System Requirements",
            .MessageDescription = "CPU Requirements" + vbCrLf + vbCrLf + "- AMD 6 cores and 12 threads or more" + vbCrLf + "- Intel 6 cores and 12 threads, 8 cores or more" + vbCrLf + vbCrLf +
        "GPU Requirements" + vbCrLf + vbCrLf + "- AMD or NVIDIA - Vulkan compatible with latest driver" + vbCrLf + vbCrLf +
        "RAM Requirements" + vbCrLf + vbCrLf + "- 8 GB Dual-Channel RAM or more"}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

    Private Sub DownloadPS3Firmware()
        'Pause input on this window
        PauseInput = True

        Dim DownloadsWindow As New Downloads() With {.ShowActivated = True, .Top = Top, .Left = Left, .Opacity = 0, .PS3SetupDownload = True, .Opener = "SetupPS3"}
        DownloadsWindow.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        DownloadsWindow.Show()
    End Sub

    Private Sub InstallFirmware()
        Try
            PS3Emulator = New Process
            PS3Emulator.StartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"

            If Not MainConfigFile.IniReadValue("Network", "DownloadPath") = "Default" Then
                PS3Emulator.StartInfo.Arguments = "--installfw """ + MainConfigFile.IniReadValue("Network", "DownloadPath") + "\PS3UPDAT.PUP"""
            Else
                PS3Emulator.StartInfo.Arguments = "--installfw """ + FileIO.FileSystem.CurrentDirectory + "\System\Downloads\PS3UPDAT.PUP"""
            End If

            PS3Emulator.StartInfo.WorkingDirectory = IO.Path.GetDirectoryName(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe")
            PS3Emulator.StartInfo.UseShellExecute = False
            PS3Emulator.EnableRaisingEvents = True
            PS3Emulator.Start()
            PS3Emulator.WaitForExit()
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub PS3Emulator_Exited(sender As Object, e As EventArgs) Handles PS3Emulator.Exited
        'Reactivate this window
        PlayBackgroundSound(Sounds.Back)
        Activate()

        PS3Emulator = Nothing

        'Continue setup
        InstallFirmwareButton.BorderBrush = Nothing
        FirmwareInstalledCheckBox.IsChecked = True
        SelectBackupsButton.Focus()
    End Sub

    Private Sub OpenFileExplorer()
        Dim NewExplorerWindow As New FileExplorer() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opacity = 0}
        Animate(NewExplorerWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
        NewExplorerWindow.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#Region "Focus Changes"

    Private Sub ReadRequirementsButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles ReadRequirementsButton.GotFocus
        ReadRequirementsButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub ReadRequirementsButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles ReadRequirementsButton.LostFocus
        ReadRequirementsButton.BorderBrush = Nothing
    End Sub

    Private Sub DownloadFirmwareButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles DownloadFirmwareButton.GotFocus
        DownloadFirmwareButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub DownloadFirmwareButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles DownloadFirmwareButton.LostFocus
        DownloadFirmwareButton.BorderBrush = Nothing
    End Sub

    Private Sub InstallFirmwareButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles InstallFirmwareButton.GotFocus
        InstallFirmwareButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub InstallFirmwareButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles InstallFirmwareButton.LostFocus
        InstallFirmwareButton.BorderBrush = Nothing
    End Sub

    Private Sub SelectBackupsButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles SelectBackupsButton.GotFocus
        SelectBackupsButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SelectBackupsButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles SelectBackupsButton.LostFocus
        SelectBackupsButton.BorderBrush = Nothing
    End Sub

#End Region

#Region "Input"

    Private Sub SetupPS3_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X
                    PlayBackgroundSound(Sounds.SelectItem)

                    If TypeOf FocusedItem Is Button Then
                        If ReadRequirementsButton.IsFocused Then
                            ReadRequirements()
                        ElseIf DownloadFirmwareButton.IsFocused Then
                            DownloadPS3Firmware()
                        ElseIf InstallFirmwareButton.IsFocused Then
                            InstallFirmware()
                        ElseIf SelectBackupsButton.IsFocused Then
                            OpenFileExplorer()
                        End If
                    End If
                Case Key.Up
                    MoveUp()
                Case Key.Down
                    MoveDown()
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SetupPS3_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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
                    PlayBackgroundSound(Sounds.SelectItem)

                    If TypeOf FocusedItem Is Button Then

                        If ReadRequirementsButton.IsFocused Then
                            ReadRequirements()
                        ElseIf DownloadFirmwareButton.IsFocused Then
                            DownloadPS3Firmware()
                        ElseIf InstallFirmwareButton.IsFocused Then
                            InstallFirmware()
                        ElseIf SelectBackupsButton.IsFocused Then
                            OpenFileExplorer()
                        End If

                    End If

                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            If AdditionalPauseDelay = 0 Then
                Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
            Else
                Await Task.Delay(SharedController1PollingRate + AdditionalPauseDelay, CancellationToken.None)
                AdditionalPauseDelay = 0
            End If
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            SelectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub MoveUp()
        PlayBackgroundSound(Sounds.Move)

        If ReadRequirementsButton.IsFocused Then
            SelectBackupsButton.Focus()
        ElseIf DownloadFirmwareButton.IsFocused Then
            ReadRequirementsButton.Focus()
        ElseIf InstallFirmwareButton.IsFocused Then
            DownloadFirmwareButton.Focus()
        ElseIf SelectBackupsButton.IsFocused Then
            InstallFirmwareButton.Focus()
        End If
    End Sub

    Private Sub MoveDown()
        PlayBackgroundSound(Sounds.Move)

        If ReadRequirementsButton.IsFocused Then
            DownloadFirmwareButton.Focus()
        ElseIf DownloadFirmwareButton.IsFocused Then
            InstallFirmwareButton.Focus()
        ElseIf InstallFirmwareButton.IsFocused Then
            SelectBackupsButton.Focus()
        ElseIf SelectBackupsButton.IsFocused Then
            ReadRequirementsButton.Focus()
        End If
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

                OrbisDisplay.SetScaling(PS3SetupWindow, PS3SetupCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(PS3SetupWindow, PS3SetupCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "SetupPS3",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
