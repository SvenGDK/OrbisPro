Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.ProcessUtils
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation

Public Class OpenWindows

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents SwitchAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private LastKeyboardKey As Key

    Public Opener As String
    Public OtherProcess As String

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub OpenWindows_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        WindowDescriptionTextBlock.Text = "Open windows & running executables." + vbCrLf + "Choose an application from the list and select an action."

        'Check for open windows of OrbisPro
        For Each Win In System.Windows.Application.Current.Windows()

            'Don't add this window
            If Win.ToString = "OrbisPro.OpenWindows" Then Continue For

            'Create the new item and add as ListViewItem
            Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = Win.ToString.Replace("OrbisPro.", ""),
                .IsItemSelected = Visibility.Hidden,
                .ItemSubDescription = "Window",
                .ItemIcon = New BitmapImage(New Uri("/Icons/OrbisProLogo.png", UriKind.RelativeOrAbsolute))}

            Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
            OpenWindowsListView.Items.Add(NewListViewItem)
        Next

        'Check for running processes with open windows
        Dim RunningProcesses = Process.GetProcesses()
        Dim FilteredProcesses = RunningProcesses.Where(Function(p) Not String.IsNullOrEmpty(p.MainWindowTitle))
        For Each FoundProcess As Process In FilteredProcesses
            If Not FoundProcess.MainWindowHandle = IntPtr.Zero Then
                Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = FoundProcess.ProcessName,
                    .IsItemSelected = Visibility.Hidden,
                    .ItemSubDescription = FoundProcess.MainWindowTitle,
                    .ItemIcon = GetExecutableIconAsBitmapImage(FoundProcess.MainModule.FileName),
                    .ItemProcessID = FoundProcess.Id,
                    .ItemProcessHwnd = FoundProcess.MainWindowHandle}

                Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
                OpenWindowsListView.Items.Add(NewListViewItem)
            End If
        Next

        RefocusFirstItem()
    End Sub

    Private Async Sub OpenWindows_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub OpenWindows_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub OpenWindows_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

#Region "Animations"

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
            Case "Downloads"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.Downloads" Then
                        CType(Win, Downloads).Activate()
                        Exit For
                    End If
                Next
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, FileExplorer).Activate()
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
                        CType(Win, MainWindow).Topmost = True
                        CType(Win, MainWindow).Topmost = False
                        CType(Win, MainWindow).Activate()
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
            Case "SetupGames"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupGames" Then
                        CType(Win, SetupGames).Activate()
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

    Private Sub SwitchAnimation_Completed(sender As Object, e As EventArgs) Handles SwitchAnimation.Completed
        PlayBackgroundSound(Sounds.Back)
        Close()
    End Sub

#End Region

#Region "Input"

    Private Sub OpenWindows_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.A
                    PlayBackgroundSound(Sounds.CancelOptions)

                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedListViewItem As ListViewItem = CType(OpenWindowsListView.SelectedItem, ListViewItem)
                        Dim CurrentSelectedItem As OpenWindowListViewItem = CType(SelectedListViewItem.Content, OpenWindowListViewItem)

                        If CurrentSelectedItem.ItemName = IO.Path.GetFileNameWithoutExtension(OtherProcess) AndAlso Not String.IsNullOrEmpty(OtherProcess) Then
                            'Find the process by name
                            Dim ProcessByName As Process() = Process.GetProcessesByName(IO.Path.GetFileNameWithoutExtension(OtherProcess))
                            If ProcessByName IsNot Nothing Then
                                Dim FoundProcess As Process = ProcessByName(0)

                                'Kill the process
                                FoundProcess.Kill()

                                'Let the main window know that the OtherProcess got killed
                                For Each Win In System.Windows.Application.Current.Windows()
                                    If Win.ToString = "OrbisPro.MainWindow" Then
                                        CType(Win, MainWindow).StartedGameExecutable = ""
                                        Exit For
                                    End If
                                Next

                                'Close this window
                                Close()
                            End If
                        Else
                            'Kill the selected unknown process
                            Dim ActiveProcessesByName As Process() = Process.GetProcessesByName(CurrentSelectedItem.ItemName)
                            If ActiveProcessesByName IsNot Nothing AndAlso ActiveProcessesByName.Length <> 0 Then
                                Dim ActiveProcess As Process = ActiveProcessesByName(0)

                                ActiveProcess.Kill()

                                OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)

                                RefocusFirstItem()
                            End If
                        End If
                    End If

                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X
                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim SelectedListViewItem As ListViewItem = CType(OpenWindowsListView.SelectedItem, ListViewItem)
                        Dim CurrentSelectedItem As OpenWindowListViewItem = CType(SelectedListViewItem.Content, OpenWindowListViewItem)

                        If CurrentSelectedItem.ItemName = "MainWindow" Then
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.MainWindow" Then
                                    CType(Win, MainWindow).Activate()
                                    Exit For
                                End If
                            Next
                        Else
                            If CurrentSelectedItem.ItemProcessID <> 0 Then
                                'Resume threads of the process if suspended
                                If SuspendedThreads IsNot Nothing Then
                                    ResumeProcessThreads()
                                End If
                                'Show the process window
                                ShowProcess(CurrentSelectedItem.ItemName)
                            End If
                        End If

                        BeginAnimation(OpacityProperty, SwitchAnimation)

                    End If
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub OpenWindows_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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

                Dim MainGamepadButton_RightThumbY_Up As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(32767)
                Dim MainGamepadButton_RightThumbY_Down As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(-32768)

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim SelectedListViewItem As ListViewItem = CType(OpenWindowsListView.SelectedItem, ListViewItem)
                        Dim CurrentSelectedItem As OpenWindowListViewItem = CType(SelectedListViewItem.Content, OpenWindowListViewItem)

                        If CurrentSelectedItem.ItemName = "MainWindow" Then
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.MainWindow" Then
                                    CType(Win, MainWindow).Activate()
                                    CType(Win, MainWindow).PauseInput = False
                                    Exit For
                                End If
                            Next
                        Else
                            If CurrentSelectedItem.ItemProcessID <> 0 Then
                                'Resume threads of the process if suspended
                                If SuspendedThreads IsNot Nothing Then
                                    ResumeProcessThreads()
                                End If
                                'Show the process window
                                ShowProcess(CurrentSelectedItem.ItemName)
                            End If
                        End If

                        BeginAnimation(OpacityProperty, SwitchAnimation)

                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    PlayBackgroundSound(Sounds.CancelOptions)

                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedListViewItem As ListViewItem = CType(OpenWindowsListView.SelectedItem, ListViewItem)
                        Dim CurrentSelectedItem As OpenWindowListViewItem = CType(SelectedListViewItem.Content, OpenWindowListViewItem)

                        If CurrentSelectedItem.ItemName = IO.Path.GetFileNameWithoutExtension(OtherProcess) AndAlso Not String.IsNullOrEmpty(OtherProcess) Then
                            'Find the process by name
                            Dim ProcessByName As Process() = Process.GetProcessesByName(IO.Path.GetFileNameWithoutExtension(OtherProcess))
                            If ProcessByName IsNot Nothing Then
                                Dim FoundProcess As Process = ProcessByName(0)

                                'Kill the process
                                FoundProcess.Kill()
                                Close()
                            End If
                        Else
                            'Kill the selected unknown process
                            Dim ActiveProcessesByName As Process() = Process.GetProcessesByName(CurrentSelectedItem.ItemName)
                            If ActiveProcessesByName IsNot Nothing AndAlso ActiveProcessesByName.Length <> 0 Then
                                Dim ActiveProcess As Process = ActiveProcessesByName(0)

                                ActiveProcess.Kill()

                                OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)

                                RefocusFirstItem()
                            End If
                        End If
                    End If

                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    'Get the ListView of the selected item
                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                    Dim SelectedIndex As Integer = OpenWindowsListView.SelectedIndex
                    Dim NextIndex As Integer = OpenWindowsListView.SelectedIndex - 1

                    If Not NextIndex <= -1 Then
                        OpenWindowsListView.SelectedIndex -= 1
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    'Get the ListView of the selected item
                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                    Dim SelectedIndex As Integer = OpenWindowsListView.SelectedIndex
                    Dim NextIndex As Integer = OpenWindowsListView.SelectedIndex + 1

                    If Not NextIndex = OpenWindowsListView.Items.Count Then
                        OpenWindowsListView.SelectedIndex += 1
                    End If
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                End If
            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            ActionButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
            SwitchButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub OpenWindowsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OpenWindowsListView.SelectionChanged
        If OpenWindowsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim RemovedItem As ListViewItem = CType(e.RemovedItems(0), ListViewItem)
            Dim AddedItem As ListViewItem = CType(e.AddedItems(0), ListViewItem)

            Dim PreviousItem As OpenWindowListViewItem = CType(RemovedItem.Content, OpenWindowListViewItem)
            Dim SelectedItem As OpenWindowListViewItem = CType(AddedItem.Content, OpenWindowListViewItem)

            SelectedItem.IsItemSelected = Visibility.Visible
            PreviousItem.IsItemSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(OpenWindowsListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(OpenWindowsListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

#Region "Navigation Helpers"

    Private Sub RefocusFirstItem()
        If OpenWindowsListView.Items.Count > 0 Then
            'Focus the first item
            Dim LastSelectedListViewItem As ListViewItem = CType(OpenWindowsListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            LastSelectedListViewItem.Focus()

            'Convert to OpenWindowListViewItem to set the border visibility on the first item
            Dim LastSelectedItem As OpenWindowListViewItem = CType(LastSelectedListViewItem.Content, OpenWindowListViewItem)
            LastSelectedItem.IsItemSelected = Visibility.Visible

            Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(OpenWindowsListView)
            OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(0)
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

                OrbisDisplay.SetScaling(OpenWindowsWindow, MainCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(OpenWindowsWindow, MainCanvas)
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
            .Opener = "OpenWindows",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
