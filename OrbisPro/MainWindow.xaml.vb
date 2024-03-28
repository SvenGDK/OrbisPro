Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisCDVDManager
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports OrbisPro.ProcessUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports System.Threading

Class MainWindow

    'Used to show the current time
    Private WithEvents SystemTimer As DispatcherTimer

    'Hook the 'Home' key
    Private WithEvents GlobalKeyboardHook As New KeyboardHook()

    'Our background webbrowser to retrieve some game infos (covers, cd image, description, ...)
    Private WithEvents PSXDatacenterBrowser As New WebBrowser()
    Public GameDatabaseReturnedTitle As String
    Public SecondWebSearch As Boolean = False

    'Used to keep track of some variables
    Public CurrentMenu As String
    Public HomeAppsCount As Integer = 0
    Private IsDeviceInterface As Boolean = False

    Private LastKey As Key
    Public LastFocusedApp As Image
    Public StartedGameExecutable As String

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If Not String.IsNullOrEmpty(OrbisUser) Then
            UsernameLabel.Text = OrbisUser
        End If

        'Set background color & video (if set)
        OrbisGrid.Background = New SolidColorBrush(Colors.Black)
        SetBackground()

        'Version Banner
        NotificationsBanner.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)

        'Clock
        SystemClock.Text = Date.Now.ToString("HH:mm")
        SystemTimer = New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5)} 'Update every 5 seconds
        SystemTimer.Start()

        HomeAnimation()

        'Add default and custom applications
        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Apps\AppsList.txt") Then
            For Each LineWithApp As String In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                If Not LineWithApp.Split("="c)(1).Split(";"c).Count = 3 Then
                    If LineWithApp.StartsWith("App") Then
                        If App1.Tag Is Nothing Then
                            App1.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                            AppTitle.Text = LineWithApp.Split("="c)(1).Split(";"c)(0)
                        ElseIf App2.Tag Is Nothing Then
                            App2.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App3.Tag Is Nothing Then
                            App3.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App4.Tag Is Nothing Then
                            App4.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App5.Tag Is Nothing Then
                            App5.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App6.Tag Is Nothing Then
                            App6.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App7.Tag Is Nothing Then
                            App7.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        ElseIf App8.Tag Is Nothing Then
                            App8.Tag = New AppDetails() With {.AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        End If
                    End If
                Else
                    If LineWithApp.StartsWith("App") Then
                        AddNewApp(LineWithApp.Split("="c)(1).Split(";"c)(0), LineWithApp.Split("="c)(1).Split(";"c)(1))
                    End If
                End If
            Next
        End If

        'Add games
        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.txt") Then
            For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.txt")
                If Game.StartsWith("PS1Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PS2Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PS3Game") Then
                    'For PS3 games show the folder name
                    Dim PS3GameFolderName = Directory.GetParent(Game.Split("="c)(1).Split(";"c)(0))
                    AddNewApp(PS3GameFolderName.Parent.Parent.Name, Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PC") Then
                    AddNewApp(Game.Split(";"c)(1), Game.Split(";"c)(2))
                End If
            Next
        End If

        App1.Focus()
        CurrentMenu = "AppMenu"
    End Sub

    Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        'Add a global keyboard hook for the 'Home' key
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1

            GlobalKeyboardHook.Attach()
            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        GlobalKeyboardHook.Detach()
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
        UnregisterNotification()
    End Sub

#End Region

#Region "Device Broadcast"

    Private NotifyDeviceBroadcast As IntPtr

    Private Sub RegisterNotification(ByRef GUIDInfo As Guid)
        Dim NewDeviceInterface As New DEV_BROADCAST_DEVICEINTERFACE
        Dim NewDeviceInterfaceBuffer As IntPtr

        'Set the GUID
        NewDeviceInterface.dbcc_size = Marshal.SizeOf(NewDeviceInterface)
        NewDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE
        NewDeviceInterface.dbcc_reserved = 0
        NewDeviceInterface.dbcc_classguid = GUIDInfo

        'Allocate a buffer for the DLL call
        NewDeviceInterfaceBuffer = Marshal.AllocHGlobal(NewDeviceInterface.dbcc_size)

        'Copy NewDeviceInterfaceBuffer to buffer
        Marshal.StructureToPtr(NewDeviceInterface, NewDeviceInterfaceBuffer, True)

        'Register the device notification
        Dim MainWindowSource As HwndSource = HwndSource.FromHwnd(New WindowInteropHelper(Me).Handle)
        NotifyDeviceBroadcast = RegisterDeviceNotification(MainWindowSource.Handle, NewDeviceInterfaceBuffer, DEVICE_NOTIFY_WINDOW_HANDLE)
        MainWindowSource.AddHook(AddressOf HwndHandler)

        'Copy buffer to NewDeviceInterfaceBuffer
        Marshal.PtrToStructure(NewDeviceInterfaceBuffer, NewDeviceInterface)

        'Free buffer
        Marshal.FreeHGlobal(NewDeviceInterfaceBuffer)
    End Sub

    Private Sub UnregisterNotification()
        Dim ret As UInteger = UnregisterDeviceNotification(NotifyDeviceBroadcast)
    End Sub

    Protected Overrides Sub OnSourceInitialized(e As EventArgs)
        MyBase.OnSourceInitialized(e)
        Try
            Dim NewGUID As New Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED")
            RegisterNotification(NewGUID)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Function HwndHandler(hwnd As IntPtr, msg As Integer, wparam As IntPtr, lparam As IntPtr, ByRef handled As Boolean) As IntPtr
        If msg = WM_DEVICECHANGE Then
            Dim NotificationEventType = wparam.ToInt32()

            If NotificationEventType = DBT_DEVICEARRIVAL Then

                Dim NewHDR As New DEV_BROADCAST_HDR

                'Convert lparam to DEV_BROADCAST_HDR structure
                Marshal.PtrToStructure(lparam, NewHDR)

                If NewHDR.dbch_devicetype = DBT_DEVTYP_DEVICEINTERFACE Then
                    IsDeviceInterface = True 'This will prevent displaying the USB twice

#Region "Other unused"
                    'Dim NewDevInterface As New DEV_BROADCAST_DEVICEINTERFACE_1

                    ''Convert lparam to DEV_BROADCAST_DEVICEINTERFACE structure
                    'Marshal.PtrToStructure(lparam, NewDevInterface)

                    ''Get the device path from the broadcast message
                    'Dim DevicePath As String = NewDevInterface.dbcc_name

                    ''Remove null-terminated data from the string
                    'Dim Position As Integer = DevicePath.IndexOf(Chr(0))
                    'If Position <> -1 Then
                    '    DevicePath = DevicePath.Substring(0, Position)
                    'End If

                    'Dim rx As New Regex("_[0-9a-fA-F]+|\{[0-9a-fA-F\-]+\}")
                    'Dim matches As MatchCollection = rx.Matches(DevicePath)

                    'If matches.Count > 0 Then
                    '    'Dim USBVID As String = matches(0).Value
                    '    'Dim USBPID As String = matches(1).Value

                    '    Dispatcher.BeginInvoke(Sub()
                    '                               Dim USBInfo As String() = GetPartitionAndDriveLetter()
                    '                               PopupUSBDeviceNotification("USB " + USBInfo(1), "Drive can now be used.")
                    '                           End Sub)
                    'End If
#End Region

                ElseIf NewHDR.dbch_devicetype = DBT_DEVTYP_VOLUME Then

                    If IsDeviceInterface = False Then
                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = DirectCast(Marshal.PtrToStructure(lparam, GetType(DEV_BROADCAST_VOLUME)), DEV_BROADCAST_VOLUME)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        DiscCheck(DriveLetter)
                    Else
                        IsDeviceInterface = False

                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = DirectCast(Marshal.PtrToStructure(lparam, GetType(DEV_BROADCAST_VOLUME)), DEV_BROADCAST_VOLUME)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("USB " + DriveLetter, "Drive can now be used."))
                    End If

                End If

            ElseIf NotificationEventType = DBT_DEVICEREMOVECOMPLETE Then
                Dim NewHDR As New DEV_BROADCAST_HDR
                Marshal.PtrToStructure(lparam, NewHDR)

                If NewHDR.dbch_devicetype = DBT_DEVTYP_DEVICEINTERFACE Then
                    IsDeviceInterface = True
                ElseIf NewHDR.dbch_devicetype = DBT_DEVTYP_VOLUME Then
                    If IsDeviceInterface = False Then
                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = DirectCast(Marshal.PtrToStructure(lparam, GetType(DEV_BROADCAST_VOLUME)), DEV_BROADCAST_VOLUME)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("Disc " + DriveLetter, "Disc removed.", True))
                    Else
                        IsDeviceInterface = False

                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = DirectCast(Marshal.PtrToStructure(lparam, GetType(DEV_BROADCAST_VOLUME)), DEV_BROADCAST_VOLUME)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("USB " + DriveLetter, "Drive removed."))
                    End If
                End If
            End If

        End If

        handled = False
        Return IntPtr.Zero
    End Function

    Private Sub PopupDeviceNotification(NotificationTitle As String, NotificationMessage As String, Optional IsDisc As Boolean = False)
        If IsDisc Then
            Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, NotificationTitle, NotificationMessage, "/Icons/Media-CD-icon.png"))
        Else
            Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, NotificationTitle, NotificationMessage, "/Icons/usb-icon.png"))
        End If
    End Sub

    'Private Function GetPartitionAndDriveLetter() As String()
    '    Dim USBPartition As String = ""
    '    Dim USBLetter As String = ""

    '    For Each DiskDrive In New ManagementObjectSearcher("select * from Win32_DiskDrive where InterfaceType='USB'").Get()
    '        For Each DiskPartition As ManagementObject In New ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + DiskDrive("DeviceID").ToString + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get()
    '            USBPartition = DiskPartition("Name").ToString()
    '            For Each Disk In New ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + DiskPartition("DeviceID").ToString + "'} WHERE AssocClass =Win32_LogicalDiskToPartition").Get()
    '                USBLetter = Disk("Name").ToString()
    '            Next
    '        Next
    '    Next

    '    If Not String.IsNullOrEmpty(USBPartition) AndAlso Not String.IsNullOrEmpty(USBLetter) Then
    '        Return {USBPartition, USBLetter}
    '    Else
    '        Return {"", ""}
    '    End If
    'End Function

    Private Function GetDriveLetterFromMask(ByRef Unit As Integer) As Char
        Dim NewChar As Char = Nothing
        For i As Integer = 0 To 25
            If Unit = (2 ^ i) Then
                NewChar = Chr(Asc("A") + i)
            End If
        Next
        Return NewChar
    End Function

    Private Sub DiscCheck(DriveLetter As String)
        If CheckCDVDContent(DriveLetter).Platform = "PS2" Then
            'Handle PS2 disc
            DiscContentType = "PS2"
            DiscDriveName = DriveLetter
            DiscGameID = CheckCDVDContent(DriveLetter).GameID
            Dispatcher.BeginInvoke(Sub() PSXDatacenterBrowser.Navigate("https://psxdatacenter.com/psx2/games2/" + DiscGameID + ".html"))
        ElseIf CheckCDVDContent(DriveLetter).Platform = "PS1" Then
            'Handle PS1 disc
            DiscContentType = "PS1"
            DiscDriveName = DriveLetter
            DiscGameID = CheckCDVDContent(DriveLetter).GameID.ToUpper
            Dispatcher.BeginInvoke(Sub() PSXDatacenterBrowser.Navigate("https://psxdatacenter.com/plist.html"))
        ElseIf CheckCDVDContent(DriveLetter).Platform = "PCE" Then
            'Handle PC-Engine disc
            DiscContentType = "PCE"
            DiscDriveName = DriveLetter

            'The game id is a bit harder to retrieve and we got almost no infos about the disc ... it's compatible anyway so add it as default disc on the main menu
            'DiscContentType is the var here that chooses the right emulator afterwards
            Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, DriveLetter, "PC-Engine CD-ROM ready.", "/Icons/Media-CD-icon.png"))
            Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = DriveLetter,
                                                           .AppIconPath = "/Icons/Media-CD-icon.png",
                                                           .AppExecutable = "Start Disc",
                                                           .AppPath = DiscDriveName}))
        Else
            Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("Disc " + DriveLetter, "Disc can now be used.", True))
        End If
    End Sub

#End Region

#Region "Animations"

    'Distance App1 - App2 = 345 (330 image size + 15 space to next app)
    'Distance App2 - App3 = 210 (200 image size + 10 space To Next app)

    Private WithEvents GameStartAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(2))}

    'The animation when entering Home
    Private Sub HomeAnimation()
        Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(400))

        PlayBackgroundSound(Sounds.Start)

        Animate(App1, Canvas.LeftProperty, 461, 285, AnimDuration)
        Animate(App1, WidthProperty, 165, 330, AnimDuration)
        Animate(App1, HeightProperty, 165, 330, AnimDuration)

        Animate(App2, Canvas.LeftProperty, 631, 630, AnimDuration)
        Animate(App2, WidthProperty, 150, 200, AnimDuration)
        Animate(App2, HeightProperty, 150, 200, AnimDuration)

        Animate(App3, Canvas.LeftProperty, 786, 840, AnimDuration)
        Animate(App3, WidthProperty, 150, 200, AnimDuration)
        Animate(App3, HeightProperty, 150, 200, AnimDuration)

        Animate(App4, Canvas.LeftProperty, 941, 1050, AnimDuration)
        Animate(App4, WidthProperty, 150, 200, AnimDuration)
        Animate(App4, HeightProperty, 150, 200, AnimDuration)

        Animate(App5, Canvas.LeftProperty, 1096, 1260, AnimDuration)
        Animate(App5, WidthProperty, 150, 200, AnimDuration)
        Animate(App5, HeightProperty, 150, 200, AnimDuration)

        Animate(App6, Canvas.LeftProperty, 1251, 1470, AnimDuration)
        Animate(App6, WidthProperty, 150, 200, AnimDuration)
        Animate(App6, HeightProperty, 150, 200, AnimDuration)

        Animate(App7, Canvas.LeftProperty, 1406, 1680, AnimDuration)
        Animate(App7, WidthProperty, 150, 200, AnimDuration)
        Animate(App7, HeightProperty, 150, 200, AnimDuration)

        Animate(App8, Canvas.LeftProperty, 1561, 1890, AnimDuration)
        Animate(App8, WidthProperty, 150, 200, AnimDuration)
        Animate(App8, HeightProperty, 150, 200, AnimDuration)

        Animate(SelectedAppBorder, Canvas.LeftProperty, 456, 280, AnimDuration)
        Animate(SelectedAppBorder, WidthProperty, 175, 340, AnimDuration)
        Animate(SelectedAppBorder, HeightProperty, 175, 410, AnimDuration)

        AppTitle.Visibility = Visibility.Visible
        AppStartLabel.Visibility = Visibility.Visible
    End Sub

    'The animation when returning to Home
    Public Sub ReturnAnimation()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})

        If TypeOf FocusedItem Is Image Then
            Dim SelectedApp As Image = CType(FocusManager.GetFocusedElement(Me), Image)

            Dim PositionDuration = New Duration(TimeSpan.FromSeconds(1))
            Dim LongShowHideDuration = New Duration(TimeSpan.FromSeconds(1))

            'Top elements
            Animate(PlusBanner, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(NotificationsBannerB, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(NotificationsBanner, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(FriendsBanner, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(OnlineBanner, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(UsernameLabel, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(TrophyBanner, Canvas.TopProperty, -78, 78, PositionDuration)
            Animate(SystemClock, Canvas.TopProperty, -78, 78, PositionDuration)

            For Each App In OrbisGrid.Children
                If TypeOf App Is Image Then
                    Dim AppImage As Image = CType(App, Image)
                    If AppImage.Name.StartsWith("App") And AppImage.Name IsNot SelectedApp.Name Then
                        Animate(AppImage, OpacityProperty, 0, 1, PositionDuration)
                        Animate(AppImage, WidthProperty, 330, 200, PositionDuration)
                        Animate(AppImage, HeightProperty, 330, 200, PositionDuration)
                    ElseIf AppImage.Name Is SelectedApp.Name Then
                        Animate(AppImage, OpacityProperty, 0, 1, LongShowHideDuration)
                        Animate(AppImage, WidthProperty, 660, 330, LongShowHideDuration)
                        Animate(AppImage, HeightProperty, 660, 330, LongShowHideDuration)
                    End If
                End If
            Next

            Animate(BackgroundMedia, OpacityProperty, 0, 1, LongShowHideDuration)
            Animate(AppTitle, OpacityProperty, 0, 1, LongShowHideDuration)
            Animate(AppStartLabel, OpacityProperty, 0, 1, LongShowHideDuration)

            Animate(SelectedAppBorder, OpacityProperty, 0, 1, PositionDuration)
            SelectedAppBorder.Visibility = Visibility.Visible
        Else
            LastFocusedApp?.Focus()
        End If

    End Sub

    'Animation when starting a game
    Private Sub StartGameAnimation(SelectedApp As Image)

        'Get game infos
        Dim GameInfos As AppDetails = CType(SelectedApp.Tag, AppDetails)

        'Durations for the animations
        Dim PositionDuration = New Duration(TimeSpan.FromSeconds(1))
        Dim LongShowHideDuration = New Duration(TimeSpan.FromSeconds(2))

        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Animate top elements
        Animate(PlusBanner, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(NotificationsBannerB, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(NotificationsBanner, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(FriendsBanner, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(OnlineBanner, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(UsernameLabel, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(TrophyBanner, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(SystemClock, Canvas.TopProperty, 78, -78, PositionDuration)

        'Animate apps on the main menu
        For Each App In OrbisGrid.Children
            If TypeOf App Is Image Then
                Dim AppImage As Image = CType(App, Image)
                If AppImage.Name.StartsWith("App") And AppImage.Name IsNot SelectedApp.Name Then
                    Animate(AppImage, OpacityProperty, 1, 0, PositionDuration)
                    Animate(AppImage, WidthProperty, 200, 330, PositionDuration)
                    Animate(AppImage, HeightProperty, 200, 330, PositionDuration)
                ElseIf AppImage.Name Is SelectedApp.Name Then
                    Animate(AppImage, OpacityProperty, 1, 0, LongShowHideDuration)
                    Animate(AppImage, WidthProperty, 200, 660, LongShowHideDuration)
                    Animate(AppImage, HeightProperty, 200, 660, LongShowHideDuration)
                End If
            End If
        Next

        'Hide the background
        BackgroundMedia.BeginAnimation(OpacityProperty, GameStartAnimation)

        'Hide the game title, start label and border
        Animate(AppTitle, OpacityProperty, 1, 0, LongShowHideDuration)
        Animate(AppStartLabel, OpacityProperty, 1, 0, LongShowHideDuration)
        Animate(SelectedAppBorder, OpacityProperty, 1, 0, PositionDuration)
        SelectedAppBorder.Visibility = Visibility.Hidden

        'Start the game
        StartedGameExecutable = GameInfos.AppPath
        GameStarter.StartGame(GameInfos.AppPath)
    End Sub

    'Animation when starting an internal application
    Private Sub SimpleAppStartAnimation(SelectedApp As Image)

        'Get the details about the selected application
        Dim SelectedAppInfos As AppDetails = CType(SelectedApp.Tag, AppDetails)

        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Show the new window
        Select Case SelectedAppInfos.AppTitle
            Case "Web Browser"
                Dim NewSystemWebBrowser As New SystemWebBrowser() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewSystemWebBrowser.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewSystemWebBrowser.Show()
            Case "File Browser"
                Dim NewFileExplorer As New FileExplorer() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewFileExplorer.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewFileExplorer.Show()
            Case "Library"
                Dim NewGameLibrary As New GameLibrary() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewGameLibrary.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewGameLibrary.Show()
                NewGameLibrary.GameLibraryButton.Focus()
            Case "OrbisPro Update"
                Dim NewSetupCheckUpdates As New SetupCheckUpdates() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewSetupCheckUpdates.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewSetupCheckUpdates.Show()
            Case "Settings"
                Dim NewSettings As New GeneralSettings() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewSettings.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewSettings.Show()
            Case "Twitter"
                Dim NewSystemWebBrowser As New SystemWebBrowser() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow"}
                NewSystemWebBrowser.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewSystemWebBrowser.Show()
        End Select

    End Sub

    'Animation when adding a the disc application on the main menu
    Public Sub AddDiscToHome(DiscInfos As AppDetails)

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        DiscApp.Visibility = Visibility.Visible
        DiscApp.Source = New BitmapImage(New Uri(DiscInfos.AppIconPath, UriKind.RelativeOrAbsolute))
        DiscApp.Tag = DiscInfos
        IsDiscInserted = True

        'This will be necessary if the focused app is the DiscApp
        Dim FocusedImage As Image
        If TypeOf FocusedItem Is Image Then
            FocusedImage = CType(FocusedItem, Image)
        Else
            FocusedImage = Nothing
        End If

        Dim App1Position As Double = Canvas.GetLeft(App1)

        'Move every app
        For Each App In OrbisGrid.Children
            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then
                Dim TheApp As Image = CType(App, Image)
                'Exlude App1 (the DiscApp will always be shown after App1 atm)
                If Not TheApp.Name = "App1" Then
                    Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp), Canvas.GetLeft(TheApp) + 210, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If
        Next

        'Place the DiscApp on the main menu
        If Not FocusedImage.Name = "App1" Then
            Animate(DiscApp, Canvas.LeftProperty, 461, App1Position + 210, New Duration(TimeSpan.FromMilliseconds(100)))
        Else
            'Needs a different Left position because the size of App1 is bigger
            Animate(DiscApp, Canvas.LeftProperty, 461, App1Position + 345, New Duration(TimeSpan.FromMilliseconds(100)))
        End If

        Animate(DiscApp, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(100)))

        If Not FocusedImage.Name = "App1" Then
            'Re-position the selection border & labels
            Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) + 210, New Duration(TimeSpan.FromMilliseconds(100)))
            Animate(AppTitle, Canvas.LeftProperty, Canvas.GetLeft(AppTitle), Canvas.GetLeft(AppTitle) + 210, New Duration(TimeSpan.FromMilliseconds(100)))
            Animate(AppStartLabel, Canvas.LeftProperty, Canvas.GetLeft(AppStartLabel), Canvas.GetLeft(AppStartLabel) + 210, New Duration(TimeSpan.FromMilliseconds(100)))
        End If

    End Sub

    'Animation when removing the disc application on the main menu
    Public Sub RemoveDiscFromHome()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(100))

        'This will be necessary if the focused app is the DiscApp
        Dim FocusedImage As Image
        If TypeOf FocusedItem Is Image Then
            FocusedImage = CType(FocusedItem, Image)
        Else
            FocusedImage = Nothing
        End If

        'Move every app
        For Each App In OrbisGrid.Children
            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then
                Dim TheApp As Image = CType(App, Image)
                'Exlude App1
                If Not TheApp.Name = "App1" Then
                    Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp), Canvas.GetLeft(TheApp) - 210, AnimDuration)
                End If
            End If
        Next

        'Set the App1 size back if DiscApp was selected
        If FocusedImage.Name = "DiscApp" Then
            Animate(App1, WidthProperty, 200, 330, AnimDuration)
            Animate(App1, HeightProperty, 200, 330, AnimDuration)

            AppTitle.Text = CType(App1.Tag, AppDetails).AppTitle
            AppStartLabel.Text = CType(App1.Tag, AppDetails).AppExecutable
            App1.Focus()

            'Move every app back to the origin position 
            For Each App In OrbisGrid.Children
                If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then
                    Dim TheApp As Image = CType(App, Image)
                    Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp), Canvas.GetLeft(TheApp) + 210, AnimDuration)
                End If
            Next
        End If

        If Not FocusedImage.Name = "App1" And Not FocusedImage.Name = "DiscApp" Then
            'Re-position the selection border & labels
            Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) - 210, AnimDuration)
            Animate(AppTitle, Canvas.LeftProperty, Canvas.GetLeft(AppTitle), Canvas.GetLeft(AppTitle) - 210, AnimDuration)
            Animate(AppStartLabel, Canvas.LeftProperty, Canvas.GetLeft(AppStartLabel), Canvas.GetLeft(AppStartLabel) - 210, AnimDuration)
        ElseIf Not FocusedImage.Name = "App1" And FocusedImage.Name = "DiscApp" Then
            'Move every app from the right back to the left
            For Each App In OrbisGrid.Children
                If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then
                    Dim TheApp As Image = CType(App, Image)
                    'Exlude App1
                    If Not TheApp.Name = "App1" Then
                        Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp) - 210, Canvas.GetLeft(TheApp), AnimDuration)
                    End If
                End If
            Next
        End If

        'Remove the DiscApp on the main menu
        Animate(DiscApp, Canvas.LeftProperty, Canvas.GetLeft(DiscApp), 461, AnimDuration)
        Animate(DiscApp, HeightProperty, 330, 200, AnimDuration)
        Animate(DiscApp, WidthProperty, 330, 200, AnimDuration)
        Animate(DiscApp, OpacityProperty, 1, 0, AnimDuration)

        DiscApp.Source = Nothing
        DiscApp.Tag = Nothing
        IsDiscInserted = False
        DiscApp.Visibility = Visibility.Hidden
    End Sub

#Region "UI Browsing"

    Private Sub MoveAppsRight(SelectedApp As Image, NextApp As Image)

        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(80))
        PlayBackgroundSound(Sounds.Move)

        For Each App In OrbisGrid.Children

            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then

                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move + 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp), Canvas.GetLeft(TheApp) - 210, MoveDuration)
                End If

            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name = "DiscApp" Then

                Dim TheDiscApp As Image = CType(App, Image)

                If IsDiscInserted Then
                    'If the disc app is not the selected one and also not the next one, move + 210
                    If TheDiscApp.Name IsNot SelectedApp.Name AndAlso TheDiscApp.Name IsNot NextApp.Name Then
                        Animate(TheDiscApp, Canvas.LeftProperty, Canvas.GetLeft(TheDiscApp), Canvas.GetLeft(TheDiscApp) - 210, MoveDuration)
                    End If
                End If

            End If

        Next

        'The next application's animation
        Animate(NextApp, Canvas.LeftProperty, Canvas.GetLeft(NextApp), Canvas.GetLeft(NextApp) - 345, MoveDuration)
        Animate(NextApp, WidthProperty, 200, 330, MoveDuration)
        Animate(NextApp, HeightProperty, 200, 330, MoveDuration)

        'The selected application's animation
        Animate(SelectedApp, Canvas.LeftProperty, Canvas.GetLeft(SelectedApp), Canvas.GetLeft(SelectedApp) - 210, MoveDuration)
        Animate(SelectedApp, WidthProperty, 330, 200, MoveDuration)
        Animate(SelectedApp, HeightProperty, 330, 200, MoveDuration)

        'Selection border animation (reverts)
        Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) + 60, MoveDuration, True)
        Animate(SelectedAppBorder, HeightProperty, 410, 200, MoveDuration, True)

        'Set the selected application title and start label shown on the main menu and focus the app
        AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppExecutable

        NextApp.Focus()
    End Sub

    Private Sub MoveAppsLeft(SelectedApp As Image, NextApp As Image)

        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(80))
        PlayBackgroundSound(Sounds.Move)

        For Each App In OrbisGrid.Children

            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then

                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move + 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Animate(TheApp, Canvas.LeftProperty, Canvas.GetLeft(TheApp), Canvas.GetLeft(TheApp) + 210, MoveDuration)
                End If

            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name = "DiscApp" Then

                Dim TheDiscApp As Image = CType(App, Image)

                If IsDiscInserted Then
                    'If the disc app is not the selected one and also not the next one, move + 210
                    If TheDiscApp.Name IsNot SelectedApp.Name AndAlso TheDiscApp.Name IsNot NextApp.Name Then
                        Animate(TheDiscApp, Canvas.LeftProperty, Canvas.GetLeft(TheDiscApp), Canvas.GetLeft(TheDiscApp) + 210, MoveDuration)
                    End If
                End If

            End If

        Next

        Animate(NextApp, Canvas.LeftProperty, Canvas.GetLeft(NextApp), Canvas.GetLeft(NextApp) + 210, MoveDuration)
        Animate(NextApp, WidthProperty, 200, 330, MoveDuration)
        Animate(NextApp, HeightProperty, 200, 330, MoveDuration)

        Animate(SelectedApp, Canvas.LeftProperty, Canvas.GetLeft(SelectedApp), Canvas.GetLeft(SelectedApp) + 345, MoveDuration)
        Animate(SelectedApp, WidthProperty, 330, 200, MoveDuration)
        Animate(SelectedApp, HeightProperty, 330, 200, MoveDuration)

        Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) - 130, MoveDuration, True)
        Animate(SelectedAppBorder, HeightProperty, 410, 200, MoveDuration, True)

        AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppExecutable

        NextApp.Focus()
    End Sub

#End Region

#End Region

#Region "Input"

    'Keyboard input
    Private Sub MainWindow_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyDown

        If Not e.Key = LastKey Then

            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            If e.Key = Key.X Then
                If TypeOf FocusedItem Is Image Then

                    If String.IsNullOrEmpty(StartedGameExecutable) Then
                        Dim FocusedApp As Image = TryCast(FocusedItem, Image)
                        Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppPath

                        PauseInput = True

                        If String.IsNullOrEmpty(FocusedAppPath) Then
                            SimpleAppStartAnimation(FocusedApp)
                        Else
                            StartGameAnimation(FocusedApp)
                        End If
                    Else
                        Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png"))
                    End If

                End If
            ElseIf e.Key = Key.O Then
                PlayBackgroundSound(Sounds.Options)
                PauseInput = True

                Dim OpenWindowsManager As New OpenWindows() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "MainWindow"}

                If Not String.IsNullOrEmpty(StartedGameExecutable) Then
                    OpenWindowsManager.OtherProcess = StartedGameExecutable
                End If

                OpenWindowsManager.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                OpenWindowsManager.Show()
            ElseIf e.Key = Key.Left Then
                If TypeOf FocusedItem Is Image Then
                    Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                    Dim NextAppImage As Image

                    If App2.IsFocused And IsDiscInserted Then
                        NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                    ElseIf DiscApp.IsFocused Then
                        NextAppImage = CType(OrbisGrid.FindName("App1"), Image)
                    Else
                        Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                        If OrbisGrid.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                            NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image)
                        Else
                            NextAppImage = Nothing
                        End If
                    End If

                    'Do not move if there's no next app
                    If NextAppImage IsNot Nothing Then
                        MoveAppsLeft(SelectedApp, NextAppImage)

                        Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                        'Change background animation
                        If ConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
                            If Path.GetExtension(NextAppDetails.AppPath) = ".exe" Then
                                If Not String.IsNullOrEmpty(CheckForExistingBackgroundAsset(NextAppDetails.AppPath)) Then
                                    Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
                                    BackgroundImage.Source = New BitmapImage(New Uri(CheckForExistingBackgroundAsset(NextAppDetails.AppPath), UriKind.RelativeOrAbsolute))
                                    Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                End If
                            Else
                                If BackgroundMedia.Opacity = 0 Then
                                    BackgroundImage.Source = Nothing
                                    Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                End If
                            End If
                        End If
                    End If

                End If
            ElseIf e.Key = Key.Right Then
                If TypeOf FocusedItem Is Image Then
                    Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                    Dim NextAppImage As Image

                    If App1.IsFocused And IsDiscInserted Then
                        NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                    ElseIf DiscApp.IsFocused Then
                        NextAppImage = CType(OrbisGrid.FindName("App2"), Image)
                    Else
                        Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                        If OrbisGrid.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                            NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image)
                        Else
                            NextAppImage = Nothing
                        End If
                    End If

                    If NextAppImage IsNot Nothing Then
                        MoveAppsRight(SelectedApp, NextAppImage)

                        Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                        If ConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
                            'Change background animation
                            If Path.GetExtension(NextAppDetails.AppPath) = ".exe" Then
                                If Not String.IsNullOrEmpty(CheckForExistingBackgroundAsset(NextAppDetails.AppPath)) Then
                                    Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
                                    BackgroundImage.Source = New BitmapImage(New Uri(CheckForExistingBackgroundAsset(NextAppDetails.AppPath), UriKind.RelativeOrAbsolute))
                                    Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                End If
                            Else
                                If BackgroundMedia.Opacity = 0 Then
                                    BackgroundImage.Source = Nothing
                                    Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                End If
                            End If
                        End If
                    End If

                End If
            ElseIf e.Key = Key.F1 Then
                ReloadHome()
            End If
        Else
            e.Handled = True
        End If

        LastKey = e.Key

    End Sub

    Private Sub MainWindow_KeyUp(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyUp
        LastKey = Nothing
    End Sub

    'Get Home keyboard input
    Private Sub GlobalKeyboardHook_KeyDown(sender As Object, e As Winnster.Interop.LibHook.KeyPressEventArgs) Handles GlobalKeyboardHook.KeyDown
        If e.Key = VirtualKeyEnum.VK_HOME Then

            If PauseInput Then
                If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso SuspendedThreads.Count = 0 Then
                    Try
                        'Suspend the running game process
                        PauseProcessThread(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                    Catch ex As Exception
                        PauseInput = True
                        ExceptionDialog("System Error", ex.Message)
                    End Try
                End If

                'ShowProcess("OrbisPro") 'Re-activate OrbisPro and bring to front
                ReturnAnimation()
                Activate()
                Topmost = True
                Topmost = False
                Activate()
            Else
                If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso SuspendedThreads.Count > 0 Then
                    'Resume the game process and bring it to the front
                    PauseInput = True
                    ResumeProcessThreads()
                    ShowProcess(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                End If
            End If

        End If
    End Sub

    'Gamepad input
    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons
            Dim AdditionalDelayAmount As Integer = 0

            If Not PauseInput Then

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

                If MainGamepadButton_Back_Button_Pressed And MainGamepadButton_Start_Button_Pressed Then

                    If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso SuspendedThreads.Count > 0 Then
                        'Resume the game process and bring it to the front
                        PauseInput = True
                        ResumeProcessThreads()
                        ShowProcess(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                    End If

                    'Do not leave the buttons in a pressed state
                    MainGamepadButton_Back_Button_Pressed = False
                    MainGamepadButton_Start_Button_Pressed = False
                End If

                If MainGamepadButton_A_Button_Pressed Then
                    If TypeOf FocusedItem Is Image Then

                        If String.IsNullOrEmpty(StartedGameExecutable) Then
                            Dim FocusedApp As Image = TryCast(FocusedItem, Image)
                            Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppPath

                            PauseInput = True

                            If String.IsNullOrEmpty(FocusedAppPath) Then
                                SimpleAppStartAnimation(FocusedApp)
                            Else
                                StartGameAnimation(FocusedApp)
                            End If

                            LastFocusedApp = FocusedApp
                        Else
                            Await Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png"))
                            AdditionalDelayAmount += 25
                        End If

                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then

                ElseIf MainGamepadButton_Y_Button_Pressed Then

                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If App2.IsFocused And IsDiscInserted Then
                            NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                        ElseIf DiscApp.IsFocused Then
                            NextAppImage = CType(OrbisGrid.FindName("App1"), Image)
                        Else
                            Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                            If OrbisGrid.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        End If

                        'Do not move if there's no next app
                        If NextAppImage IsNot Nothing Then
                            MoveAppsLeft(SelectedApp, NextAppImage)

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                            'Change background animation
                            If ConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
                                If Path.GetExtension(NextAppDetails.AppPath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(CheckForExistingBackgroundAsset(NextAppDetails.AppPath)) Then
                                        AdditionalDelayAmount += 25
                                        Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
                                        BackgroundImage.Source = New BitmapImage(New Uri(CheckForExistingBackgroundAsset(NextAppDetails.AppPath), UriKind.RelativeOrAbsolute))
                                        Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                    End If
                                Else
                                    If BackgroundMedia.Opacity = 0 Then
                                        AdditionalDelayAmount += 25
                                        BackgroundImage.Source = Nothing
                                        Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                    End If
                                End If
                            End If

                        End If

                    End If
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If App1.IsFocused And IsDiscInserted Then
                            NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                        ElseIf DiscApp.IsFocused Then
                            NextAppImage = CType(OrbisGrid.FindName("App2"), Image)
                        Else
                            Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                            If OrbisGrid.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        End If

                        If NextAppImage IsNot Nothing Then
                            MoveAppsRight(SelectedApp, NextAppImage)

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                            'Change background animation
                            If ConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
                                If Path.GetExtension(NextAppDetails.AppPath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(CheckForExistingBackgroundAsset(NextAppDetails.AppPath)) Then
                                        AdditionalDelayAmount += 25
                                        Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
                                        BackgroundImage.Source = New BitmapImage(New Uri(CheckForExistingBackgroundAsset(NextAppDetails.AppPath), UriKind.RelativeOrAbsolute))
                                        Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                    End If
                                Else
                                    If BackgroundMedia.Opacity = 0 Then
                                        AdditionalDelayAmount += 25
                                        BackgroundImage.Source = Nothing
                                        Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                                    End If
                                End If
                            End If

                        End If

                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then

                ElseIf MainGamepadButton_DPad_Down_Pressed Then

                ElseIf MainGamepadButton_Back_Button_Pressed Then
                    PlayBackgroundSound(Sounds.Options)
                    PauseInput = True

                    Dim OpenWindowsManager As New OpenWindows() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "MainWindow"}

                    If Not String.IsNullOrEmpty(StartedGameExecutable) Then
                        OpenWindowsManager.OtherProcess = StartedGameExecutable
                    End If

                    OpenWindowsManager.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    OpenWindowsManager.Show()
                ElseIf MainGamepadButton_Start_Button_Pressed Then

                End If

                AdditionalDelayAmount += 40
            Else

                Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                If MainGamepadButton_Back_Button_Pressed And MainGamepadButton_Start_Button_Pressed Then

                    If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso SuspendedThreads.Count = 0 Then
                        Try
                            'Suspend the running game process
                            PauseProcessThread(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                        Catch ex As Exception
                            PauseInput = True
                            ExceptionDialog("System Error", ex.Message)
                        End Try
                    End If

                    'ShowProcess("OrbisPro") 'Re-activate OrbisPro and bring to front
                    ReturnAnimation()
                    Activate()
                    Topmost = True
                    Topmost = False
                    Activate()

                    'Do not leave the buttons in a pressed state
                    MainGamepadButton_Back_Button_Pressed = False
                    MainGamepadButton_Start_Button_Pressed = False

                End If

                AdditionalDelayAmount += 60
            End If

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

#End Region

    Private Sub PSXDatacenterBrowser_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) Handles PSXDatacenterBrowser.DocumentCompleted

        Dim CoverImgSrc As BitmapImage = Nothing

        If DiscContentType = "PS1" Then

            If SecondWebSearch = False Then
                'Load game properties site first
                Dim TableRows As HtmlElementCollection = PSXDatacenterBrowser.Document.GetElementsByTagName("tr")
                Dim TableGameIDs As New List(Of String)

                For Each Row As HtmlElement In TableRows
                    Try
                        Dim TableGameID As String = Row.GetElementsByTagName("td")(1).InnerText.Trim

                        If TableGameID.Contains(DiscGameID) Then
                            Dim GameURL As String = Row.GetElementsByTagName("td")(0).GetElementsByTagName("a")(0).GetAttribute("href")
                            SecondWebSearch = True

                            PSXDatacenterBrowser.Navigate(GameURL)
                        End If

                    Catch ex As ArgumentOutOfRangeException
                    End Try
                Next

            Else

                'Get the game infos
                Dim InfoRows As HtmlElementCollection = PSXDatacenterBrowser.Document.GetElementsByTagName("tr")

                'Game Title
                If InfoRows.Item(4).InnerText IsNot Nothing Then
                    GameDatabaseReturnedTitle = InfoRows.Item(4).InnerText.Split(New String() {"Official Title "}, StringSplitOptions.RemoveEmptyEntries)(0)
                Else
                    GameDatabaseReturnedTitle = ""
                End If

                'Game Cover
                If PSXDatacenterBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src") IsNot Nothing Then
                    CoverImgSrc = New BitmapImage(New Uri(PSXDatacenterBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src")))
                End If

                If CoverImgSrc IsNot Nothing Then
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, GameDatabaseReturnedTitle, "PS1 CD-ROM is now ready.", CoverImgSrc.UriSource.AbsoluteUri))
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                               .AppIconPath = CoverImgSrc.UriSource.AbsoluteUri,
                                                               .AppExecutable = "Start Disc",
                                                               .AppPath = DiscDriveName}))
                Else
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, GameDatabaseReturnedTitle, "PS1 CD-ROM is now ready.", "/Icons/Media-PS1-icon.png"))
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                               .AppIconPath = "/Icons/Media-PS1-icon.png",
                                                               .AppExecutable = "Start Disc",
                                                               .AppPath = DiscDriveName}))
                End If

                SecondWebSearch = False

            End If

        ElseIf DiscContentType = "PS2" Then

            If SecondWebSearch = False Then
                'Get the game infos
                Dim InfoRows As HtmlElementCollection = PSXDatacenterBrowser.Document.GetElementsByTagName("tr")

                'Game Title
                If InfoRows.Item(4).InnerText IsNot Nothing Then
                    GameDatabaseReturnedTitle = InfoRows.Item(4).InnerText.Split(New String() {"OFFICIAL TITLE "}, StringSplitOptions.RemoveEmptyEntries)(0)
                Else
                    GameDatabaseReturnedTitle = ""
                End If

                'Game Cover
                If PSXDatacenterBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src") IsNot Nothing Then
                    CoverImgSrc = New BitmapImage(New Uri(PSXDatacenterBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src")))
                End If

                If CoverImgSrc IsNot Nothing Then
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, GameDatabaseReturnedTitle, "PS2 DVD-ROM is now ready.", CoverImgSrc.UriSource.AbsoluteUri))
                Else
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, GameDatabaseReturnedTitle, "PS2 DVD-ROM is now ready.", "/Icons/Media-DVD-icon.png"))
                End If

                'Check if a PS2 disc image exists
                If PSXDatacenterBrowser.Document.GetElementById("table29").GetElementsByTagName("tr")(2).GetElementsByTagName("a")(0).GetAttribute("href") IsNot Nothing Then
                    Dim GameDiscImageSrc = PSXDatacenterBrowser.Document.GetElementById("table29").GetElementsByTagName("tr")(2).GetElementsByTagName("a")(0).GetAttribute("href")

                    'Do a second web browser navigation to get the game disc image
                    SecondWebSearch = True
                    PSXDatacenterBrowser.Navigate(GameDiscImageSrc)
                End If

            Else

                'Get the game disc image (if exists)
                If PSXDatacenterBrowser.Document.GetElementsByTagName("img")(2).GetAttribute("src") IsNot Nothing Then
                    Dim GameDiscImage = New BitmapImage(New Uri(PSXDatacenterBrowser.Document.GetElementsByTagName("img")(2).GetAttribute("src")))
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle, .AppIconPath = GameDiscImage.UriSource.AbsoluteUri, .AppExecutable = "Start"}))
                Else
                    'If it doesn't exists, add the previous found cover, if this one is also not present then add a default disc image
                    If CoverImgSrc IsNot Nothing Then
                        Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                                   .AppIconPath = CoverImgSrc.UriSource.AbsoluteUri,
                                                                   .AppExecutable = "Start",
                                                                   .AppPath = DiscDriveName}))
                    Else
                        Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                                   .AppIconPath = "/Icons/Media-DVD-icon.png",
                                                                   .AppExecutable = "Start",
                                                                   .AppPath = DiscDriveName}))
                    End If
                End If

                SecondWebSearch = False

            End If
        End If

    End Sub

    Private Sub Systimer_Tick(sender As Object, e As EventArgs) Handles SystemTimer.Tick
        SystemClock.Text = Date.Now.ToString("HH:mm")

        'Border Glowing
        SelectedAppBorder.BeginAnimation(OpacityProperty, SelectedBoxAnimation)
    End Sub

    Public Sub AddNewApp(AppTitle As String, AppFolderOrFile As String)

        'Get the count of displayed apps
        Dim InstalledAppsOnMenu As Integer = 0

        If HomeAppsCount = 0 Then
            For Each AppImage In OrbisGrid.Children
                If TypeOf AppImage Is Image Then
                    Dim Img As Image = CType(AppImage, Image)
                    If Img.Name.StartsWith("App") Then
                        InstalledAppsOnMenu += 1
                    End If
                End If
            Next
        Else
            'Speed up the next addition
            InstalledAppsOnMenu = HomeAppsCount
        End If

        'Declare the new app
        Dim NewAppImage As New Image With {
            .Height = 200,
            .Width = 200,
            .Stretch = Stretch.Uniform,
            .StretchDirection = StretchDirection.Both,
            .Name = "App" + (InstalledAppsOnMenu + 1).ToString, 'Here the new app get it's number, so the code can iterate through it
            .Focusable = True,
            .FocusVisualStyle = Nothing,
            .Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute)),
            .Tag = New AppDetails() With {.AppTitle = AppTitle, .AppExecutable = "Start", .AppPath = AppFolderOrFile}
        }

        'Set app image to executable icon or existing asset file (if exists)
        If Path.GetExtension(AppFolderOrFile) = ".exe" Then
            If Not String.IsNullOrEmpty(CheckForExistingIconAsset(AppFolderOrFile)) Then
                NewAppImage.Source = New BitmapImage(New Uri(CheckForExistingIconAsset(AppFolderOrFile), UriKind.RelativeOrAbsolute))
            Else
                NewAppImage.Source = GetExecutableIconAsImageSource(AppFolderOrFile)
            End If
        End If

        'Get the last app on the main menu
        Dim LastAppInMenu As Image = CType(OrbisGrid.FindName("App" + InstalledAppsOnMenu.ToString()), Image)
        HomeAppsCount = InstalledAppsOnMenu + 1

        'Set the position of the new app
        If LastAppInMenu.Name = "App8" Then
            'Move the first added game/app to the correct position
            'Case 1561 happens the very first time when the animation is probably finished but the canvas did not receive it's new location yet
            Select Case Canvas.GetLeft(App8)
                Case 1890
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 210)
                Case 1680
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 420)
                Case 1561
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 540)
                Case 1470
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 630)
                Case 1260
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 840)
                Case 1050
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 1050)
                Case 840
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 1260)
                Case 630
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 1470)
                Case 285
                    Canvas.SetLeft(NewAppImage, Canvas.GetLeft(App8) + 1805)
            End Select
        Else
            Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastAppInMenu) + 210)
        End If
        Canvas.SetTop(NewAppImage, Canvas.GetTop(LastAppInMenu))

        'Important, otherwise it can't find the new app
        RegisterName(NewAppImage.Name, NewAppImage)

        'Add the new app on the canvas
        OrbisGrid.Children.Add(NewAppImage)
    End Sub

    Private Sub GameStartAnimation_Completed(sender As Object, e As EventArgs) Handles GameStartAnimation.Completed
        Dim DiscInfos = CType(DiscApp.Tag, AppDetails)
        StartCDVD(DiscContentType, DiscInfos.AppPath)
    End Sub

    Public Sub SetBackground()
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
            .Opener = "MainWindow",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

    Public Sub ReloadHome()
        'Reset
        HomeAppsCount = 0

        'Get UIElements to remove
        Dim AppsToRemove As New List(Of UIElement)()
        For Each App In OrbisGrid.Children
            If TypeOf App Is Image Then

                Dim TheApp As Image = CType(App, Image)

                Select Case TheApp.Name
                    Case "App1", "App2", "App3", "App4", "App5", "App6", "App7", "App8"
                        Continue For
                    Case Else
                        'Remove only apps that have been added during runtime from the canvas
                        If TheApp.Name.StartsWith("App") Then
                            AppsToRemove.Add(TheApp)
                            UnregisterName(TheApp.Name)
                        End If
                End Select

            End If
        Next
        'Remove the UIElements from the canvas (removing while iterating will throw an exception)
        For Each AppToRemove In AppsToRemove
            OrbisGrid.Children.Remove(AppToRemove)
        Next

        HomeAnimation()

        'Reload custom applications & games
        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Apps\AppsList.txt") Then
            For Each LineWithApp As String In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                If LineWithApp.StartsWith("App") AndAlso LineWithApp.Split("="c)(1).Split(";"c).Count = 3 Then
                    AddNewApp(LineWithApp.Split("="c)(1).Split(";"c)(0), LineWithApp.Split("="c)(1).Split(";"c)(1))
                End If
            Next
        End If
        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.txt") Then
            For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.txt")
                If Game.StartsWith("PS1Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PS2Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PS3Game") Then
                    'For PS3 games show the folder name
                    Dim PS3GameFolderName = Directory.GetParent(Game.Split("="c)(1).Split(";"c)(0))
                    AddNewApp(PS3GameFolderName.Parent.Parent.Name, Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.StartsWith("PC") Then
                    AddNewApp(Game.Split(";"c)(1), Game.Split(";"c)(2))
                End If
            Next
        End If

        App1.Focus()
    End Sub

End Class
