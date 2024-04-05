Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation

Public Class SetupCustomize

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Private LastKeyboardKey As Key
    Private FocusedComboBox As ComboBox = Nothing

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupCustomize_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        UsernameTextBox.Focus()
    End Sub

    Private Async Sub SetupCustomize_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub SetupCustomize_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupCustomize_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

#End Region

#Region "Input"

    Private Sub SetupCustomize_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.A
                    FinishSetup()
                Case Key.C
                    If TypeOf FocusedItem Is ComboBox Then
                        Dim SelectedComboBox As ComboBox = CType(FocusedItem, ComboBox)

                        If SelectedComboBox.IsDropDownOpen Then
                            SelectedComboBox.IsDropDownOpen = False
                        End If

                        FocusedComboBox = SelectedComboBox
                    Else
                        ReturnToPreviousSetupStep()
                    End If
                Case Key.X
                    If TypeOf FocusedItem Is TextBox Then
                        ShowVirtualKeyboard()
                    ElseIf TypeOf FocusedItem Is ComboBox Then

                        Dim SelectedComboBox As ComboBox = CType(FocusedItem, ComboBox)

                        If Not SelectedComboBox.IsDropDownOpen Then
                            SelectedComboBox.IsDropDownOpen = True
                        End If

                        FocusedComboBox = SelectedComboBox

                    ElseIf TypeOf FocusedItem Is ComboBoxItem Then

                        'Close the DropDown and focus the ComboBox
                        If FocusedComboBox IsNot Nothing AndAlso FocusedComboBox.IsDropDownOpen = True Then
                            FocusedComboBox.IsDropDownOpen = False
                            FocusedComboBox.Focus()
                        End If

                    End If
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SetupCustomize_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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

                    If TypeOf FocusedItem Is TextBox Then
                        ShowVirtualKeyboard()
                    ElseIf TypeOf FocusedItem Is ComboBox Then

                        Dim SelectedComboBox As ComboBox = CType(FocusedItem, ComboBox)

                        If Not SelectedComboBox.IsDropDownOpen Then
                            SelectedComboBox.IsDropDownOpen = True
                        End If

                        FocusedComboBox = SelectedComboBox

                    ElseIf TypeOf FocusedItem Is ComboBoxItem Then

                        'Close the DropDown and focus the ComboBox
                        If FocusedComboBox IsNot Nothing AndAlso FocusedComboBox.IsDropDownOpen = True Then
                            FocusedComboBox.IsDropDownOpen = False
                            FocusedComboBox.Focus()
                        End If

                    End If

                ElseIf MainGamepadButton_B_Button_Pressed Then

                    If TypeOf FocusedItem Is ComboBox Then
                        Dim SelectedComboBox As ComboBox = CType(FocusedItem, ComboBox)

                        If SelectedComboBox.IsDropDownOpen Then
                            SelectedComboBox.IsDropDownOpen = False
                        End If

                        FocusedComboBox = SelectedComboBox
                    Else
                        ReturnToPreviousSetupStep()
                    End If

                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    FinishSetup()
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    'Navigate through the ComboBoxItems if IsDropDownOpen
                    If TypeOf FocusedItem Is ComboBoxItem Then
                        If FocusedComboBox IsNot Nothing AndAlso FocusedComboBox.IsDropDownOpen = True Then
                            Dim ItemsCount As Integer = FocusedComboBox.Items.Count
                            Dim SelectedIndex As Integer = FocusedComboBox.SelectedIndex

                            If SelectedIndex - 1 >= 0 Then
                                FocusedComboBox.SelectedIndex -= 1
                            End If
                        End If
                    End If

                    If UsernameTextBox.IsFocused Then
                        AudioPacksComboBox.Focus()
                    ElseIf AudioPacksComboBox.IsFocused Then
                        BackgroundsComboBox.Focus()
                    ElseIf BackgroundsComboBox.IsFocused Then
                        UsernameTextBox.Focus()
                    End If

                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    If TypeOf FocusedItem Is ComboBoxItem Then
                        If FocusedComboBox IsNot Nothing Then
                            Dim ItemsCount As Integer = FocusedComboBox.Items.Count
                            Dim SelectedIndex As Integer = FocusedComboBox.SelectedIndex

                            If Not SelectedIndex + 1 = ItemsCount Then
                                FocusedComboBox.SelectedIndex += 1
                            End If
                        End If
                    End If

                    If UsernameTextBox.IsFocused Then
                        BackgroundsComboBox.Focus()
                    ElseIf BackgroundsComboBox.IsFocused Then
                        AudioPacksComboBox.Focus()
                    ElseIf AudioPacksComboBox.IsFocused Then
                        UsernameTextBox.Focus()
                    End If

                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Selection Changes"

    Private Sub AudioPacksComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles AudioPacksComboBox.SelectionChanged
        If AudioPacksComboBox.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As ComboBoxItem = CType(e.RemovedItems(0), ComboBoxItem)
            Dim SelectedItem As ComboBoxItem = CType(e.AddedItems(0), ComboBoxItem)

            Select Case SelectedItem.Content.ToString()
                Case "PS2", "PS3", "PS4", "PS5"
                    ConfigFile.IniWriteValue("Audio", "Navigation Audio Pack", SelectedItem.Content.ToString())
            End Select
        End If
    End Sub

    Private Sub BackgroundsComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles BackgroundsComboBox.SelectionChanged
        If BackgroundsComboBox.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As ComboBoxItem = CType(e.RemovedItems(0), ComboBoxItem)
            Dim SelectedItem As ComboBoxItem = CType(e.AddedItems(0), ComboBoxItem)

            Select Case SelectedItem.Content.ToString()
                Case "Blue Bubbles"
                    BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
                    ConfigFile.IniWriteValue("System", "Background", "Blue Bubbles")
                Case "Orange/Red Gradient Waves"
                    BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
                    ConfigFile.IniWriteValue("System", "Background", "Orange/Red Gradient Waves")
                Case "PS2 Dots"
                    BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
                    ConfigFile.IniWriteValue("System", "Background", "PS2 Dots")
            End Select
        End If
    End Sub


#End Region

    Private Sub ReturnToPreviousSetupStep()
        PlayBackgroundSound(Sounds.Back)

        Dim NewSetupApps As New SetupApps() With {.ShowActivated = True, .Top = Top, .Left = Left, .Opacity = 0}
        NewSetupApps.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupApps.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub FinishSetup()
        'Save config
        If Not String.IsNullOrEmpty(UsernameTextBox.Text) Then
            ConfigFile.IniWriteValue("System", "Username", UsernameTextBox.Text)
        End If
        If BackgroundsComboBox.SelectedItem IsNot Nothing Then
            Select Case BackgroundsComboBox.SelectedValue.ToString()
                Case "Blue Bubbles"
                    ConfigFile.IniWriteValue("System", "Background", "Blue Bubbles")
                Case "Orange/Red Gradient Waves"
                    ConfigFile.IniWriteValue("System", "Background", "Orange/Red Gradient Waves")
                Case "PS2 Dots"
                    ConfigFile.IniWriteValue("System", "Background", "PS2 Dots")
            End Select
        End If
        If AudioPacksComboBox.SelectedItem IsNot Nothing Then
            ConfigFile.IniWriteValue("Audio", "Navigation Audio Pack", AudioPacksComboBox.Text)
        End If

        ConfigFile.IniWriteValue("Setup", "Done", "True")

        'Continue to Home
        Dim OrbisProMainWindow As New MainWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
        OrbisAnimations.Animate(OrbisProMainWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
        OrbisProMainWindow.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

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

                OrbisDisplay.SetScaling(SetupCustomizeWindow, SetupCustomizeCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = ConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SetupCustomizeWindow, SetupCustomizeCanvas)
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
            .Opener = "SetupCustomize",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
