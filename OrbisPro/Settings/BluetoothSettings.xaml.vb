Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisBluetooth
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports InTheHand.Net.Bluetooth
Imports InTheHand.Net.Sockets

Public Class BluetoothSettings

    Public Opener As String = ""
    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents NewGlobalKeyboardHook As New OrbisKeyboardHook()
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private WithEvents BluetoothWorker As New BackgroundWorker() With {.WorkerReportsProgress = True}
    Private BluetoothWorkerAction As String
    Private WaitedFor As Integer = 0

    Private BTClient As New BluetoothClient()
    Private PINInputBox As New PSInputBox("Pairing PIN Code (optional) :")
    Private PairPIN As String = "0000"
    Private DeviceToHandle As BTDeviceOrServiceListViewItem
    Private PairingSuccess As Boolean = False
    Private UnpairingSuccess As Boolean = False
    Private ConnectionSuccess As Boolean = False
    Private DisonnectionSuccess As Boolean = False

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub BluetoothSettings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Add a PIN input field to the canvas
        Canvas.SetLeft(PINInputBox, 1925)
        Canvas.SetTop(PINInputBox, 1085)
        BTSettingsCanvas.Children.Add(PINInputBox)
    End Sub

    Private Async Sub BluetoothSettings_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            WaitTimer.Start()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub BluetoothSettings_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub BluetoothSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
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

    Private Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        WaitedFor += 1

        If WaitedFor = 1 Then
            WaitTimer.Stop()
            BluetoothWorkerAction = "Discover"
            BluetoothWorker.RunWorkerAsync("Discover")
        End If
    End Sub

#Region "Input"

    Private Sub BluetoothSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If e.Key = Key.T Then
            If Not BluetoothWorker.IsBusy Then
                BluetoothWorkerAction = "Discover"
                BluetoothWorker.RunWorkerAsync("Discover")
            End If

        ElseIf e.Key = Key.A Then
            If TypeOf FocusedItem Is ListViewItem Then
                Dim SelectedDevice As BTDeviceOrServiceListViewItem = CType(BluetoothDevicesListView.SelectedItem, BTDeviceOrServiceListViewItem)
                DeviceToHandle = SelectedDevice

                If ActionTextBlock.Text = "Pair" Then
                    If Not BluetoothWorker.IsBusy Then

                        'Show a PIN input field
                        Animate(PINInputBox, Canvas.TopProperty, 1085, 400, New Duration(TimeSpan.FromMilliseconds(500)))
                        Animate(PINInputBox, Canvas.LeftProperty, 1925, 500, New Duration(TimeSpan.FromMilliseconds(500)))
                        Animate(PINInputBox, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))

                        'Pause the window keyboard input and focus the PIN input field
                        PauseInput = True
                        PINInputBox.InputTextBox.Clear()
                        PINInputBox.InputTextBox.Focus()
                        ShowVirtualKeyboard()

                    End If
                Else
                    If Not BluetoothWorker.IsBusy Then
                        BluetoothWorkerAction = "Unpair"
                        BluetoothWorker.RunWorkerAsync("Unpair")
                    End If
                End If
            End If
        ElseIf e.Key = Key.S Then
            If TypeOf FocusedItem Is ListViewItem Then
                Dim SelectedDevice As BTDeviceOrServiceListViewItem = CType(BluetoothDevicesListView.SelectedItem, BTDeviceOrServiceListViewItem)
                DeviceToHandle = SelectedDevice

                If ActionTextBlock.Text = "Connect" Then
                    If Not BluetoothWorker.IsBusy Then
                        BluetoothWorkerAction = "Connect"
                        BluetoothWorker.RunWorkerAsync("Connect")
                    End If
                Else
                    If Not BluetoothWorker.IsBusy Then
                        BluetoothWorkerAction = "Disconnect"
                        BluetoothWorker.RunWorkerAsync("Disconnect")
                    End If
                End If
            End If
        ElseIf e.Key = Key.C Then
            BeginAnimation(OpacityProperty, ClosingAnimation)
        End If
    End Sub

    Private Sub NewGlobalKeyboardHook_KeyDown(Key As Forms.Keys) Handles NewGlobalKeyboardHook.KeyDown
        If PauseInput Then
            Select Case Key
                Case Forms.Keys.Return, Forms.Keys.Enter
                    'Remove the PIN input field
                    Animate(PINInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PINInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PINInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    If DeviceToHandle IsNot Nothing Then
                        PauseInput = False

                        PairPIN = PINInputBox.InputTextBox.Text
                        BluetoothWorkerAction = "Pair"
                        BluetoothWorker.RunWorkerAsync("Pair")

                        'Set the focus back on the previously selected BTDeviceOrServiceListViewItem
                        Dim LastSelectedListViewItem As ListViewItem = TryCast(BluetoothDevicesListView.ItemContainerGenerator.ContainerFromIndex(BluetoothDevicesListView.SelectedIndex), ListViewItem)
                        Dim LastSelectedItem As BTDeviceOrServiceListViewItem = TryCast(LastSelectedListViewItem.Content, BTDeviceOrServiceListViewItem)
                        If LastSelectedItem IsNot Nothing Then
                            LastSelectedItem.IsDeviceSelected = Visibility.Visible
                            LastSelectedListViewItem.Focus()
                        End If
                    End If

                    PauseInput = False
                Case Forms.Keys.Escape
                    'Remove the PIN input field
                    Animate(PINInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PINInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PINInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    'Set the focus back on the previously selected BTDeviceOrServiceListViewItem
                    Dim LastSelectedListViewItem As ListViewItem = TryCast(BluetoothDevicesListView.ItemContainerGenerator.ContainerFromIndex(BluetoothDevicesListView.SelectedIndex), ListViewItem)
                    Dim LastSelectedItem As BTDeviceOrServiceListViewItem = TryCast(LastSelectedListViewItem.Content, BTDeviceOrServiceListViewItem)
                    If LastSelectedItem IsNot Nothing Then
                        LastSelectedItem.IsDeviceSelected = Visibility.Visible
                        LastSelectedListViewItem.Focus()
                    End If
                    PauseInput = False
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
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                Dim MainGamepadButton_RightThumbY_Up As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(32767)
                Dim MainGamepadButton_RightThumbY_Down As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(-32768)

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    If Not BluetoothWorker.IsBusy Then
                        BluetoothWorkerAction = "Discover"
                        BluetoothWorker.RunWorkerAsync("Discover")
                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedDevice As BTDeviceOrServiceListViewItem = CType(BluetoothDevicesListView.SelectedItem, BTDeviceOrServiceListViewItem)
                        DeviceToHandle = SelectedDevice

                        If ActionTextBlock.Text = "Pair" Then
                            If Not BluetoothWorker.IsBusy Then

                                'Show a PIN input field
                                Animate(PINInputBox, Canvas.TopProperty, 1085, 400, New Duration(TimeSpan.FromMilliseconds(500)))
                                Animate(PINInputBox, Canvas.LeftProperty, 1925, 500, New Duration(TimeSpan.FromMilliseconds(500)))
                                Animate(PINInputBox, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))

                                'Pause the window keyboard input and focus the PIN input field
                                PauseInput = True
                                PINInputBox.InputTextBox.Clear()
                                PINInputBox.InputTextBox.Focus()
                                ShowVirtualKeyboard()

                            End If
                        Else
                            If Not BluetoothWorker.IsBusy Then
                                BluetoothWorkerAction = "Unpair"
                                BluetoothWorker.RunWorkerAsync("Unpair")
                            End If
                        End If
                    End If
                ElseIf MainGamepadButton_X_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedDevice As BTDeviceOrServiceListViewItem = CType(BluetoothDevicesListView.SelectedItem, BTDeviceOrServiceListViewItem)
                        DeviceToHandle = SelectedDevice

                        If ActionTextBlock.Text = "Connect" Then
                            If Not BluetoothWorker.IsBusy Then
                                BluetoothWorkerAction = "Connect"
                                BluetoothWorker.RunWorkerAsync("Connect")
                            End If
                        Else
                            If Not BluetoothWorker.IsBusy Then
                                BluetoothWorkerAction = "Disconnect"
                                BluetoothWorker.RunWorkerAsync("Disconnect")
                            End If
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
                    Dim SelectedIndex As Integer = BluetoothDevicesListView.SelectedIndex
                    Dim NextIndex As Integer = BluetoothDevicesListView.SelectedIndex - 1

                    If Not NextIndex <= -1 Then
                        BluetoothDevicesListView.SelectedIndex -= 1
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
                    Dim SelectedIndex As Integer = BluetoothDevicesListView.SelectedIndex
                    Dim NextIndex As Integer = BluetoothDevicesListView.SelectedIndex + 1

                    If Not NextIndex = BluetoothDevicesListView.Items.Count Then
                        BluetoothDevicesListView.SelectedIndex += 1
                    End If
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
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
            ActionButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
            Action2Button.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_x.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub BluetoothWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles BluetoothWorker.DoWork

        Select Case e.Argument.ToString()
            Case "Discover"
                Dim DiscoveredDevices As List(Of DiscoveredBluetoothDevice) = DiscoverBTDevices()
                For Each DiscoveredDevice As DiscoveredBluetoothDevice In DiscoveredDevices

                    Dim NewBTDeviceOrServiceLVItem As New BTDeviceOrServiceListViewItem() With {.DeviceTitle = DiscoveredDevice.FriendlyName, .DeviceAddress = DiscoveredDevice.Address}

                    'Check if the discovered device is already authenticated
                    If DiscoveredDevice.Authenticated Then
                        NewBTDeviceOrServiceLVItem.IsDevicePaired = True
                    Else
                        NewBTDeviceOrServiceLVItem.IsDevicePaired = False
                    End If
                    'Check if the discovered device is connected
                    If DiscoveredDevice.Connected Then
                        NewBTDeviceOrServiceLVItem.IsDeviceConnected = True
                    Else
                        NewBTDeviceOrServiceLVItem.IsDeviceConnected = False
                    End If

                    Dispatcher.BeginInvoke(Sub()

                                               'Set icon depending on type of device
                                               Select Case DiscoveredDevice.ClassOfDevice
                                                   Case DeviceClass.PeripheralGamepad
                                                       NewBTDeviceOrServiceLVItem.DeviceIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.Relative))
                                                   Case DeviceClass.AudioVideoHeadphones
                                                       NewBTDeviceOrServiceLVItem.DeviceIcon = New BitmapImage(New Uri("/Icons/HeadsetIcon.png", UriKind.Relative))
                                                   Case DeviceClass.PeripheralKeyboard
                                                       NewBTDeviceOrServiceLVItem.DeviceIcon = New BitmapImage(New Uri("/Icons/KeyboardIcon.png", UriKind.Relative))
                                                   Case DeviceClass.Smartphone
                                                       NewBTDeviceOrServiceLVItem.DeviceIcon = New BitmapImage(New Uri("/Icons/PhoneIcon.png", UriKind.Relative))
                                                   Case DeviceClass.AudioVideoDisplayLoudspeaker
                                                       NewBTDeviceOrServiceLVItem.DeviceIcon = New BitmapImage(New Uri("/Icons/TVIcon.png", UriKind.Relative))
                                                   Case Else
                                                       Console.WriteLine(DiscoveredDevice.ClassOfDevice.ToString())
                                               End Select

                                               NewBTDeviceOrServiceLVItem.IsDeviceSelected = Visibility.Hidden
                                               BluetoothDevicesListView.Items.Add(NewBTDeviceOrServiceLVItem)
                                           End Sub)

                Next
            Case "Pair"
                If PairBTDevice(DeviceToHandle, PairPIN) Then
                    PairingSuccess = True
                Else
                    PairingSuccess = False
                End If
            Case "Connect"
                BTClient.Connect(DeviceToHandle.DeviceAddress, BluetoothService.SerialPort)
                If BTClient.Connected Then
                    ConnectionSuccess = True
                Else
                    ConnectionSuccess = False
                End If
            Case "Disconnect"
                If BTClient.Connected Then
                    BTClient.Close()
                    If BTClient.Connected Then
                        DisonnectionSuccess = False
                    Else
                        DisonnectionSuccess = True
                    End If
                End If
            Case "Unpair"
                If UnpairBTDevice(DeviceToHandle) Then
                    UnpairingSuccess = True
                Else
                    UnpairingSuccess = False
                End If
        End Select

    End Sub

    Private Sub BluetoothWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles BluetoothWorker.RunWorkerCompleted

        Select Case BluetoothWorkerAction
            Case "Discover"
                'Hide scanning indicator
                ScanningIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

                If BluetoothDevicesListView.Items.Count > 0 Then

                    'Focus the first found device
                    Dim FirstListViewItem As ListViewItem = CType(BluetoothDevicesListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
                    FirstListViewItem.Focus()

                    'Convert to FirstListViewItem to control the item's customized properties
                    Dim FirstSelectedItem As BTDeviceOrServiceListViewItem = CType(FirstListViewItem.Content, BTDeviceOrServiceListViewItem)
                    FirstSelectedItem.IsDeviceSelected = Visibility.Visible 'Show the selection border

                    'Set button actions
                    If FirstSelectedItem.IsDevicePaired Then
                        ActionTextBlock.Text = "Unpair"
                    Else
                        ActionTextBlock.Text = "Pair"
                    End If
                    If FirstSelectedItem.IsDeviceConnected Then
                        Action2TextBlock.Text = "Disconnect"
                    Else
                        Action2TextBlock.Text = "Connect"
                    End If

                    'Show buttons and activate gamepad
                    BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    ActionButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    ActionTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    Action2Button.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                    Action2TextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
                End If

            Case "Pair"
                If PairingSuccess Then
                    DeviceToHandle.IsDevicePaired = True
                    ActionTextBlock.Text = "Unpair"
                End If
            Case "Connect"
                If ConnectionSuccess Then
                    DeviceToHandle.IsDeviceConnected = True
                    Action2TextBlock.Text = "Disconnect"
                End If
            Case "Disconnect"
                If DisonnectionSuccess Then
                    DeviceToHandle.IsDeviceConnected = False
                    Action2TextBlock.Text = "Connect"
                End If
            Case "Unpair"
                If UnpairingSuccess Then
                    DeviceToHandle.IsDevicePaired = False
                    ActionTextBlock.Text = "Pair"
                End If
        End Select

        'Refresh & Reset
        BluetoothDevicesListView.Items.Refresh()

        PairingSuccess = False
        UnpairingSuccess = False
        ConnectionSuccess = False
        DisonnectionSuccess = False
    End Sub

    Private Sub BluetoothDevicesListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles BluetoothDevicesListView.SelectionChanged
        If BluetoothDevicesListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As BTDeviceOrServiceListViewItem = CType(e.RemovedItems(0), BTDeviceOrServiceListViewItem)
            Dim SelectedItem As BTDeviceOrServiceListViewItem = CType(e.AddedItems(0), BTDeviceOrServiceListViewItem)

            SelectedItem.IsDeviceSelected = Visibility.Visible
            PreviousItem.IsDeviceSelected = Visibility.Hidden

            If SelectedItem.IsDevicePaired Then
                ActionTextBlock.Text = "Unpair"
            Else
                ActionTextBlock.Text = "Pair"
            End If

            If SelectedItem.IsDeviceConnected Then
                Action2TextBlock.Text = "Disconnect"
            Else
                Action2TextBlock.Text = "Connect"
            End If

        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(BluetoothDevicesListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(BluetoothDevicesListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
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
            .Opener = "BluetoothSettings",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
