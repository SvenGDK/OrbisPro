Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class SystemDialog

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Private LastKeyboardKey As Key

    Public Opener As String
    Public MessageTitle As String
    Public MessageDescription As String
    Public MessageType As SystemMessage
    Public SetupStep As Boolean = False

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
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
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.BluetoothSettings" Then
                        CType(Win, BluetoothSettings).Activate()
                        Exit For
                    End If
                Next
            Case "CreateNewFolderDialog"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                        CType(Win, CreateNewFolderDialog).Activate()
                        Exit For
                    End If
                Next
            Case "Downloads"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.Downloads" Then
                        CType(Win, Downloads).Activate()
                        Exit For
                    End If
                Next
            Case "ExistingFolders"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.ExistingFolders" Then
                        CType(Win, ExistingFolders).Activate()
                        Exit For
                    End If
                Next
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        CType(Win, FileExplorer).Activate()
                        Exit For
                    End If
                Next
            Case "GamepadInputTester"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GamepadInputTester" Then
                        CType(Win, GamepadInputTester).Activate()
                        Exit For
                    End If
                Next
            Case "GameLibrary"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GameLibrary" Then
                        CType(Win, GameLibrary).Activate()
                        Exit For
                    End If
                Next
            Case "GeneralSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        Exit For
                    End If
                Next
            Case "OpenWindows"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.OpenWindows" Then
                        CType(Win, OpenWindows).Activate()
                        Exit For
                    End If
                Next
            Case "PKGInstaller"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.PKGInstaller" Then
                        CType(Win, PKGInstaller).Activate()
                        CType(Win, PKGInstaller).ContinuePKGInstallation()
                        Exit For
                    End If
                Next
            Case "SelectFolderContentDialog"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SelectFolderContentDialog" Then
                        CType(Win, SelectFolderContentDialog).Activate()
                        Exit For
                    End If
                Next
            Case "SetupApps"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupApps" Then
                        CType(Win, SetupApps).Activate()
                        Exit For
                    End If
                Next
            Case "SetupDrives"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupDrives" Then
                        CType(Win, SetupDrives).Activate()
                        Exit For
                    End If
                Next
            Case "SetupGames"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupGames" Then
                        CType(Win, SetupGames).Activate()
                        Exit For
                    End If
                Next
            Case "SetupPS3"
                For Each Win In System.Windows.Application.Current.Windows()
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
            Case "SetupPSVita"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupPSVita" Then

                        If SetupStep Then
                            CType(Win, SetupPSVita).RequirementsRead = True
                            CType(Win, SetupPSVita).RequirementsReadCheckBox.IsChecked = True
                            CType(Win, SetupPSVita).ReadRequirementsButton.BorderBrush = Nothing
                            CType(Win, SetupPSVita).DownloadFirmwareButton.Focus()
                        End If

                        CType(Win, SetupPSVita).AdditionalPauseDelay = 100
                        CType(Win, SetupPSVita).Activate()

                        Exit For
                    End If
                Next
            Case "SetupCustomize"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupCustomize" Then
                        CType(Win, SetupCustomize).Activate()
                        Exit For
                    End If
                Next
            Case "SystemImageViewer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SystemImageViewer" Then
                        CType(Win, SystemImageViewer).Activate()
                        Exit For
                    End If
                Next
            Case "SystemMediaPlayer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SystemMediaPlayer" Then
                        CType(Win, SystemMediaPlayer).Activate()
                        Exit For
                    End If
                Next
            Case "WelcomeToSetup"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.WelcomeToSetup" Then
                        CType(Win, WelcomeToSetup).Activate()
                        Exit For
                    End If
                Next
            Case "WifiSettings"
                For Each Win In System.Windows.Application.Current.Windows()
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
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Select Case e.Key
                Case Key.C

                    If Opener = "GameLibrary" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.GameLibrary" Then
                                CType(Win, GameLibrary).Confirmed = False
                                Exit For
                            End If
                        Next
                    ElseIf Opener = "PKGInstaller" Then
                        'Set InstallationAborted in the PKGInstaller
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.PKGInstaller" Then
                                CType(Win, PKGInstaller).InstallationAborted = True
                                Exit For
                            End If
                        Next
                    End If

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X

                    If Opener = "GameLibrary" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.GameLibrary" Then
                                CType(Win, GameLibrary).Confirmed = True
                                Exit For
                            End If
                        Next
                    ElseIf Opener = "PKGInstaller" Then
                        'Set InstallationConfirmed in the PKGInstaller
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.PKGInstaller" Then
                                CType(Win, PKGInstaller).InstallationConfirmed = True
                                Exit For
                            End If
                        Next
                    End If

                    BeginAnimation(OpacityProperty, ClosingAnimation)
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SystemDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    If Opener = "GameLibrary" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.GameLibrary" Then
                                CType(Win, GameLibrary).Confirmed = True
                                Exit For
                            End If
                        Next
                    ElseIf Opener = "PKGInstaller" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.PKGInstaller" Then
                                CType(Win, PKGInstaller).InstallationConfirmed = True
                                Exit For
                            End If
                        Next
                    End If

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    If Opener = "GameLibrary" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.GameLibrary" Then
                                CType(Win, GameLibrary).Confirmed = False
                                Exit For
                            End If
                        Next
                    ElseIf Opener = "PKGInstaller" Then
                        'Set confirmation in the Library
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.PKGInstaller" Then
                                CType(Win, PKGInstaller).InstallationAborted = True
                                Exit For
                            End If
                        Next
                    End If

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            CloseButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        CloseButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        ConfirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
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

                OrbisDisplay.SetScaling(SystemDialogWindow, SystemDialogCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SystemDialogWindow, SystemDialogCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

End Class
