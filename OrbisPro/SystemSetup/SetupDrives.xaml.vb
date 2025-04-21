Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNotifications
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class SetupDrives

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents DrivesCollectionWorker As New BackgroundWorker() With {.WorkerReportsProgress = True}
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private WaitedFor As Integer = 0
    Private LastKeyboardKey As Key

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupDrives_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()
    End Sub

    Private Sub SetupDrives_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        WaitTimer.Start()

        'Check for gamepads
        If GetAndSetGamepads() Then MainController = SharedController1
        ChangeButtonLayout()
    End Sub

    Private Sub SetupDrives_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupDrives_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnimation_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

    Private Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        WaitedFor += 1

        If WaitedFor = 2 Then
            WaitTimer.Stop()
            DrivesCollectionWorker.RunWorkerAsync()
        End If
    End Sub

    Private Sub ContinueSetup()

        'Set the drives that will be used
        Dim DrivesToUse As New List(Of DriveListViewItem)()
        For Each Drive As DriveListViewItem In DrivesList.Items
            If Drive.UsedForScanning Then
                DrivesToUse.Add(Drive)
            End If
        Next
        UsedDriveList = DrivesToUse

        PlayBackgroundSound(Sounds.SelectItem)

        Dim NewGamesSetup As New SetupGames() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewGamesSetup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewGamesSetup.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub ReturnToPreviousSetupStep()
        PlayBackgroundSound(Sounds.Back)

        Dim NewSetupCheckUpdates As New SetupCheckUpdates() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewSetupCheckUpdates.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupCheckUpdates.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#Region "Drives Loader"

    Private Sub DrivesCollectionWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles DrivesCollectionWorker.DoWork

        For Each DiskDrive In DriveInfo.GetDrives()
            'Get active fixed and removable drives
            If DiskDrive.DriveType = DriveType.Removable And DiskDrive.IsReady Or DiskDrive.DriveType = DriveType.Fixed And DiskDrive.IsReady Then

                Dim NewDriveListViewItem As New DriveListViewItem()

                Select Case DiskDrive.DriveType
                    Case DriveType.Fixed
                        NewDriveListViewItem.DriveType = DriveType.Fixed
                        DrivesList.Dispatcher.BeginInvoke(Sub() NewDriveListViewItem.DriveIcon = New BitmapImage(New Uri("/Icons/Hard-Drive.png", UriKind.RelativeOrAbsolute)))
                    Case DriveType.Removable
                        NewDriveListViewItem.DriveType = DriveType.Removable
                        DrivesList.Dispatcher.BeginInvoke(Sub() NewDriveListViewItem.DriveIcon = New BitmapImage(New Uri("/Icons/USBDevice.png", UriKind.RelativeOrAbsolute)))
                End Select

                NewDriveListViewItem.DriveName = DiskDrive.Name
                NewDriveListViewItem.DriveFormat = DiskDrive.DriveFormat
                NewDriveListViewItem.DriveLeftSize = DiskDrive.AvailableFreeSpace
                NewDriveListViewItem.DriveTotalSize = DiskDrive.TotalSize
                NewDriveListViewItem.DriveTotalFreeSpace = DiskDrive.TotalFreeSpace
                NewDriveListViewItem.DriveVolumeLabel = DiskDrive.VolumeLabel
                NewDriveListViewItem.IsDriveSelected = Visibility.Hidden

                NewDriveListViewItem.DriveUsedSpace = (DiskDrive.TotalSize - DiskDrive.AvailableFreeSpace) / DiskDrive.TotalSize * 100
                NewDriveListViewItem.DriveFullNameText = DiskDrive.VolumeLabel + "(" + DiskDrive.Name + ")"
                NewDriveListViewItem.DriveSizeText = FormatSizeAsString(DiskDrive.AvailableFreeSpace) + " free of " + FormatSizeAsString(DiskDrive.TotalSize)

                DrivesList.Dispatcher.BeginInvoke(Sub() DrivesList.Items.Add(NewDriveListViewItem))
                Thread.Sleep(125)
            End If
        Next

    End Sub

    Private Async Sub DrivesCollectionWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles DrivesCollectionWorker.RunWorkerCompleted
        'Hide loading indicator
        FontAwesome.Sharp.Awesome.SetSpin(LoadingIndicator, False)
        LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        'Adjust top label
        TopLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100)), .AutoReverse = True})
        TopLabel.Text = "Select the drives that should be scanned for Games"

        'Show buttons and activate gamepad
        BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddDriveTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        EnterButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        ContinueTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        If DrivesList.Items.Count > 0 Then
            'Focus the first game
            Dim FirstListViewItem As ListViewItem = CType(DrivesList.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert to DriveListViewItem to control the item's customized properties
            Dim FirstSelectedItem As DriveListViewItem = CType(FirstListViewItem.Content, DriveListViewItem)
            FirstSelectedItem.IsDriveSelected = Visibility.Visible 'Show the selection border
        End If

        Try
            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

#End Region

#Region "Input"

    Private Sub SetupGames_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            Select Case e.Key
                Case Key.A
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.SelectItem)

                        'Add/Remove selected drive from scanning process
                        Dim SelectedDrive As DriveListViewItem = CType(DrivesList.SelectedItem, DriveListViewItem)

                        If SelectedDrive.DriveSelectionBorderBrush IsNot Brushes.LightBlue Then
                            SelectedDrive.DriveSelectionBorderBrush = Brushes.LightBlue
                            SelectedDrive.IsDriveSelected = Visibility.Visible
                            SelectedDrive.UsedForScanning = True

                            NotificationPopup(SetupDrivesCanvas, SelectedDrive.DriveFullNameText, "Selected for scanning", SelectedDrive.DriveIcon)
                        Else
                            SelectedDrive.DriveSelectionBorderBrush = Brushes.White
                            SelectedDrive.UsedForScanning = False

                            NotificationPopup(SetupDrivesCanvas, SelectedDrive.DriveFullNameText, "Removed from scanning", SelectedDrive.DriveIcon)
                        End If

                    End If
                Case Key.C
                    ReturnToPreviousSetupStep()
                Case Key.X
                    ContinueSetup()
            End Select

        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SetupGames_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                Dim MainGamepadButton_RightThumbY_Up As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(32767)
                Dim MainGamepadButton_RightThumbY_Down As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(-32768)

                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    ContinueSetup()
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    ReturnToPreviousSetupStep()
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.SelectItem)

                        'Add/Remove selected drive from scanning process
                        Dim SelectedDrive As DriveListViewItem = CType(DrivesList.SelectedItem, DriveListViewItem)

                        If SelectedDrive.DriveSelectionBorderBrush IsNot Brushes.LightBlue Then
                            SelectedDrive.DriveSelectionBorderBrush = Brushes.LightBlue
                            SelectedDrive.IsDriveSelected = Visibility.Visible
                            SelectedDrive.UsedForScanning = True

                            NotificationPopup(SetupDrivesCanvas, SelectedDrive.DriveFullNameText, "Selected for scanning", SelectedDrive.DriveIcon)
                        Else
                            SelectedDrive.DriveSelectionBorderBrush = Brushes.White
                            SelectedDrive.UsedForScanning = False

                            NotificationPopup(SetupDrivesCanvas, SelectedDrive.DriveFullNameText, "Removed from scanning", SelectedDrive.DriveIcon)
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = DrivesList.SelectedIndex
                        Dim NextIndex As Integer = DrivesList.SelectedIndex - 1

                        If Not NextIndex = -1 Then
                            DrivesList.SelectedIndex -= 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = DrivesList.SelectedIndex
                        Dim NextIndex As Integer = DrivesList.SelectedIndex + 1

                        If Not NextIndex = DrivesList.Items.Count Then
                            DrivesList.SelectedIndex += 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = DrivesList.SelectedIndex
                        Dim NextIndex As Integer = DrivesList.SelectedIndex - 4

                        If Not NextIndex = DrivesList.Items.Count Then
                            DrivesList.SelectedIndex -= 4
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = DrivesList.SelectedIndex
                        Dim NextIndex As Integer = DrivesList.SelectedIndex + 4

                        If Not NextIndex = DrivesList.Items.Count Then
                            DrivesList.SelectedIndex += 4
                        End If
                    End If
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            BackButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            AddButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Triangle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Triangle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Triangle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Triangle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_Y.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_Y.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_Y.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_y.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub DrivesList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DrivesList.SelectionChanged
        If DrivesList.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As DriveListViewItem = CType(e.RemovedItems(0), DriveListViewItem)
            Dim SelectedItem As DriveListViewItem = CType(e.AddedItems(0), DriveListViewItem)

            'Do not hide the border when the drive is selected for scanning
            If PreviousItem.DriveSelectionBorderBrush IsNot Brushes.LightBlue Then
                PreviousItem.IsDriveSelected = Visibility.Hidden
            End If

            SelectedItem.IsDriveSelected = Visibility.Visible
        End If
    End Sub

    Private Sub ScrollUp()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(DrivesList)
        Dim VerticalOffset As Double = GamesListViewScrollViewer.VerticalOffset
        GamesListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 70)
    End Sub

    Private Sub ScrollDown()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(DrivesList)
        Dim VerticalOffset As Double = GamesListViewScrollViewer.VerticalOffset
        GamesListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 70)
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

                OrbisDisplay.SetScaling(SetupDrivesWindow, SetupDrivesCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SetupDrivesWindow, SetupDrivesCanvas)
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
            .Opener = "SetupGames",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
