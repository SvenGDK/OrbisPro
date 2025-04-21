Imports System.ComponentModel
Imports System.Text
Imports System.Threading
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class GamepadInputTester

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Private LastKeyboardKey As Key

    Public Opener As String
    Public MessageTitle As String
    Public MessageDescription As String
    Public MessageType As SystemMessage
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
            Case "GeneralSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
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

            If Not PauseInput Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0

                Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_L_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.LeftShoulder) <> 0
                Dim MainGamepadButton_R_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.RightShoulder) <> 0

                Dim MainGamepadButton_LeftThumb_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.LeftThumb) <> 0
                Dim MainGamepadButton_RightThumb_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.RightThumb) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                Dim MainGamepadButton_LeftThumbY As Short = MainGamepadState.Gamepad.LeftThumbY
                Dim MainGamepadButton_LeftThumbX As Short = MainGamepadState.Gamepad.LeftThumbX
                Dim MainGamepadButton_LeftThumbY_String As String = $"Left Thumb Y Value: {MainGamepadButton_LeftThumbY}"
                Dim MainGamepadButton_LeftThumbX_String As String = $"Left Thumb X Value: {MainGamepadButton_LeftThumbX}"

                Dim MainGamepadButton_RightThumbY As Short = MainGamepadState.Gamepad.RightThumbY
                Dim MainGamepadButton_RightThumbX As Short = MainGamepadState.Gamepad.RightThumbX
                Dim MainGamepadButton_RightThumbY_String As String = $"Right Thumb Y Value: {MainGamepadButton_RightThumbY}"
                Dim MainGamepadButton_RightThumbX_String As String = $"Right Thumb X Value: {MainGamepadButton_RightThumbX}"

                Dim MainGamepadButton_RightTrigger As Byte = MainGamepadState.Gamepad.RightTrigger
                Dim MainGamepadButton_RightTrigger_String As String = $"Right Trigger Value: {MainGamepadButton_RightTrigger}"

                Dim MainGamepadButton_LeftTrigger As Byte = MainGamepadState.Gamepad.LeftTrigger
                Dim MainGamepadButton_LeftTrigger_String As String = $"Left Trigger Value: {MainGamepadButton_LeftTrigger}"

                'Combo to close the Gamepad Input Tester
                If MainGamepadButton_X_Button_Pressed AndAlso MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                End If

                Dim NewStringBuilder As New StringBuilder()
                If MainGamepadButton_A_Button_Pressed Then
                    NewStringBuilder.AppendLine("Button A (Xbox) / X (PlayStation) pressed")
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    NewStringBuilder.AppendLine("Button B (Xbox) / O (PlayStation) pressed")
                ElseIf MainGamepadButton_X_Button_Pressed Then
                    NewStringBuilder.AppendLine("Button X (Xbox) / □ (PlayStation) pressed")
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    NewStringBuilder.AppendLine("Button Y (Xbox) / △ (PlayStation) pressed")
                ElseIf MainGamepadButton_Back_Button_Pressed Then
                    NewStringBuilder.AppendLine("Back Button (Xbox) / Select (PlayStation) pressed")
                ElseIf MainGamepadButton_Start_Button_Pressed Then
                    NewStringBuilder.AppendLine("Start Button pressed")
                ElseIf MainGamepadButton_L_Button_Pressed Then
                    NewStringBuilder.AppendLine("Left Shoulder Button pressed")
                ElseIf MainGamepadButton_R_Button_Pressed Then
                    NewStringBuilder.AppendLine("Right Shoulder Button pressed")
                ElseIf MainGamepadButton_LeftThumb_Button_Pressed Then
                    NewStringBuilder.AppendLine("Left Stick pressed")
                ElseIf MainGamepadButton_RightThumb_Button_Pressed Then
                    NewStringBuilder.AppendLine("Right Stick pressed")
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    NewStringBuilder.AppendLine("D-Pad UP pressed")
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    NewStringBuilder.AppendLine("D-Pad DOWN pressed")
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    NewStringBuilder.AppendLine("D-Pad LEFT pressed")
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    NewStringBuilder.AppendLine("D-Pad RIGHT pressed")
                End If

                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_LeftThumbY_String)
                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_LeftThumbX_String)

                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_RightThumbY_String)
                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_RightThumbX_String)

                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_RightTrigger_String)
                NewStringBuilder.AppendLine(vbCrLf + MainGamepadButton_LeftTrigger_String)

                ControllerReadingsTextBlock.Text = NewStringBuilder.ToString()

            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            CloseButton2.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            CloseButton1.Visibility = Visibility.Hidden
            ButtonComboTextBlock.Visibility = Visibility.Hidden
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_X.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_X.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_X.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        CloseButton1.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_x.png", UriKind.RelativeOrAbsolute))
                        CloseButton2.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
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

                OrbisDisplay.SetScaling(GamepadInputTesterWindow, GamepadInputTesterCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(GamepadInputTesterWindow, GamepadInputTesterCanvas)
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
