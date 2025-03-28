#Region "Orbis Imports"
Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisCDVDManager
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNetwork
Imports OrbisPro.OrbisPowerUtils
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports OrbisPro.ProcessUtils
#End Region

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

    'Used for current time, border glowing, battery & WiFi info
    Private WithEvents ClockTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5)}
    Private WithEvents SystemTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 45)}

    'Hook the 'Home' key
    Private WithEvents NewGlobalKeyboardHook As New OrbisKeyboardHook()

    'Background WebBrowser to retrieve some game infos (covers, cd image, description, ...)
    Private WithEvents GameDataBrowser As New WebBrowser()
    Public GameDatabaseReturnedTitle As String
    Public SecondWebSearch As Boolean = False

    'Used to keep track of some variables
    Public CurrentMenu As String
    Public HomeAppsCount As Integer = 0
    Private IsDeviceInterface As Boolean = False
    Private LastKeyboardKey As Key
    Public LastFocusedApp As Image
    Public StartedGameExecutable As String
    Private DidAnimate As Boolean = False

    'Animations
    Dim WithEvents LastHomeAnimation As New DoubleAnimation With {.From = 175, .To = 410, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Dim WithEvents LastHomeRestoreAnimation As New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Dim WithEvents LastUIMoveAnimation As New DoubleAnimation With {.From = 410, .To = 200, .Duration = New Duration(TimeSpan.FromMilliseconds(100)), .AutoReverse = True}

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If Not String.IsNullOrEmpty(MainConfigFile.IniReadValue("System", "Username")) Then
            UsernameTextBlock.Text = MainConfigFile.IniReadValue("System", "Username")
        End If

        'Set background color & video (if set)
        MainCanvas.Background = New SolidColorBrush(Colors.Black)
        SetBackground()

        'Version Banner
        NotificationBannerTextBlock.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)

        'Clock
        SystemClock.Text = Date.Now.ToString("HH:mm")
        ClockTimer.Start()

        HomeAnimation()

        'Add default and custom applications
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt") Then
            For Each LineWithApp As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                If Not LineWithApp.Split("="c)(1).Split(";"c).Length = 3 Then
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
        If File.Exists(GameLibraryPath) Then
            For Each Game In File.ReadAllLines(GameLibraryPath)
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
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
            SystemTimer.Start()
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
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
                        Volume = Marshal.PtrToStructure(Of DEV_BROADCAST_VOLUME)(lparam)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        DiscCheck(DriveLetter)
                    Else
                        IsDeviceInterface = False

                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = Marshal.PtrToStructure(Of DEV_BROADCAST_VOLUME)(lparam)
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
                        Volume = Marshal.PtrToStructure(Of DEV_BROADCAST_VOLUME)(lparam)
                        Dim DriveLetter As String = GetDriveLetterFromMask(Volume.dbcv_unitmask) & ":\"

                        Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("Disc " + DriveLetter, "Disc removed.", True))
                    Else
                        IsDeviceInterface = False

                        Dim Volume As DEV_BROADCAST_VOLUME
                        Volume = Marshal.PtrToStructure(Of DEV_BROADCAST_VOLUME)(lparam)
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
            OrbisNotifications.NotificationPopup(MainCanvas, NotificationTitle, NotificationMessage, "/Icons/Media-CD-icon.png")
        Else
            OrbisNotifications.NotificationPopup(MainCanvas, NotificationTitle, NotificationMessage, "/Icons/usb-icon.png")
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
            Dispatcher.BeginInvoke(Sub() GameDataBrowser.Navigate("https://psxdatacenter.com/psx2/games2/" + DiscGameID + ".html"))
        ElseIf CheckCDVDContent(DriveLetter).Platform = "PS1" Then
            'Handle PS1 disc
            DiscContentType = "PS1"
            DiscDriveName = DriveLetter
            DiscGameID = CheckCDVDContent(DriveLetter).GameID.ToUpper
            Dispatcher.BeginInvoke(Sub() GameDataBrowser.Navigate("https://psxdatacenter.com/plist.html"))
        ElseIf CheckCDVDContent(DriveLetter).Platform = "PCE" Then
            'Handle PC-Engine disc
            DiscContentType = "PCE"
            DiscDriveName = DriveLetter

            'The game id is a bit harder to retrieve and we got almost no infos about the disc ... it's compatible anyway so add it as default disc on the main menu
            'DiscContentType is the var here that chooses the right emulator afterwards
            OrbisNotifications.NotificationPopup(MainCanvas, DriveLetter, "PC-Engine CD-ROM ready.", "/Icons/Media-CD-icon.png")
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

    'Animation shown when entering Home
    Private Sub HomeAnimation()
        Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(500))

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
        SelectedAppBorder.BeginAnimation(HeightProperty, LastHomeAnimation)

        AppTitle.Visibility = Visibility.Visible
        AppStartLabel.Visibility = Visibility.Visible
    End Sub

    'Animation shown when returning to Home
    Public Sub ReturnAnimation()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        SetBackground()

        NotificationBannerTextBlock.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)
        ClockTimer.Start()
        SystemTimer.Start()

        If Dispatcher.CheckAccess() Then
            BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        Else
            Dispatcher.BeginInvoke(Sub() BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}))
        End If

        If TypeOf FocusedItem Is Image Then
            Dim SelectedApp As Image = CType(FocusManager.GetFocusedElement(Me), Image)
            Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(500))

            'Top elements
            Animate(NotificationImage, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(NotificationBannerTextBlock, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(FriendsBannerImage, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(OnlineIndicatorImage, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(UsernameTextBlock, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(WiFiIndicatorImage, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(BatteryIndicatorImage, Canvas.TopProperty, -78, 78, AnimDuration)
            Animate(SystemClock, Canvas.TopProperty, -78, 78, AnimDuration)

            For Each App In MainCanvas.Children
                If TypeOf App Is Image Then
                    Dim AppImage As Image = CType(App, Image)
                    If AppImage.Name.StartsWith("App") And AppImage.Name IsNot SelectedApp.Name Then
                        Animate(AppImage, OpacityProperty, 0, 1, AnimDuration)
                        Animate(AppImage, WidthProperty, 330, 200, AnimDuration)
                        Animate(AppImage, HeightProperty, 330, 200, AnimDuration)
                    ElseIf AppImage.Name Is SelectedApp.Name Then
                        Animate(AppImage, OpacityProperty, 0, 1, AnimDuration)
                        Animate(AppImage, WidthProperty, 660, 330, AnimDuration)
                        Animate(AppImage, HeightProperty, 660, 330, AnimDuration)
                    End If
                End If
            Next

            Animate(BackgroundMedia, OpacityProperty, 0, 1, AnimDuration)
            Animate(AppTitle, OpacityProperty, 0, 1, AnimDuration)
            Animate(AppStartLabel, OpacityProperty, 0, 1, AnimDuration)

            SelectedAppBorder.BeginAnimation(OpacityProperty, LastHomeRestoreAnimation)
            SelectedAppBorder.Visibility = Visibility.Visible
        Else
            LastFocusedApp?.Focus()
        End If
    End Sub

    'Animation shown when starting a game
    Private Sub StartGameAnimation(SelectedApp As Image)

        'Get game infos
        Dim GameInfos As AppDetails = CType(SelectedApp.Tag, AppDetails)

        'Durations for the animations
        Dim PositionDuration = New Duration(TimeSpan.FromSeconds(1))
        Dim LongShowHideDuration = New Duration(TimeSpan.FromSeconds(2))

        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Animate top elements
        Animate(NotificationImage, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(NotificationBannerTextBlock, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(FriendsBannerImage, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(OnlineIndicatorImage, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(UsernameTextBlock, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(WiFiIndicatorImage, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(BatteryIndicatorImage, Canvas.TopProperty, 78, -78, PositionDuration)
        Animate(SystemClock, Canvas.TopProperty, 78, -78, PositionDuration)

        'Animate apps on the main menu
        For Each App In MainCanvas.Children
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
        ReduceUsage()
        GameStarter.StartGame(GameInfos.AppPath)
    End Sub

    'Animation shown when starting an internal application
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

    'Animation shown when adding a the disc application on the main menu
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
        For Each App In MainCanvas.Children
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

    'Animation shown when removing the disc application on the main menu
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
        For Each App In MainCanvas.Children
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
            For Each App In MainCanvas.Children
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
            For Each App In MainCanvas.Children
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

    'Animation shown when adding a new app on the main menu
    Public Sub AddNewApp(AppTitle As String, AppFolderOrFile As String)

        'Get the count of displayed apps
        Dim InstalledAppsOnMenu As Integer = 0

        If HomeAppsCount = 0 Then
            For Each AppImage In MainCanvas.Children
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
        Dim LastAppInMenu As Image = CType(MainCanvas.FindName("App" + InstalledAppsOnMenu.ToString()), Image)
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
        MainCanvas.Children.Add(NewAppImage)
    End Sub

    'Animation shown when reloading Home
    Public Sub ReloadHome()
        'Reset
        HomeAppsCount = 0

        'Get UIElements to remove
        Dim AppsToRemove As New List(Of UIElement)()
        For Each App In MainCanvas.Children
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
            MainCanvas.Children.Remove(AppToRemove)
        Next

        'Restore window and background (if hidden)
        If Width = 0 Then
            NotificationBannerTextBlock.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)
            ClockTimer.Start()
            SystemTimer.Start()
        End If

        SetBackground()

        'Reload custom applications & games
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt") Then
            For Each LineWithApp As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                If LineWithApp.StartsWith("App") AndAlso LineWithApp.Split("="c)(1).Split(";"c).Length = 3 Then
                    AddNewApp(LineWithApp.Split("="c)(1).Split(";"c)(0), LineWithApp.Split("="c)(1).Split(";"c)(1))
                End If
            Next
        End If

        If File.Exists(GameLibraryPath) Then
            For Each Game In File.ReadAllLines(GameLibraryPath)
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

#Region "Animation Events"

    Private Sub GameStartAnimation_Completed(sender As Object, e As EventArgs) Handles GameStartAnimation.Completed
        Dim DiscInfos = CType(DiscApp.Tag, AppDetails)
        StartCDVD(DiscContentType, DiscInfos.AppPath)
    End Sub

    Private Sub LastUIMoveAnimation_Completed(sender As Object, e As EventArgs) Handles LastUIMoveAnimation.Completed
        DidAnimate = True
    End Sub

    Private Sub LastHomeAnimation_Completed(sender As Object, e As EventArgs) Handles LastHomeAnimation.Completed
        DidAnimate = True
    End Sub

    Private Sub LastHomeRestoreAnimation_Completed(sender As Object, e As EventArgs) Handles LastHomeRestoreAnimation.Completed
        DidAnimate = True
    End Sub

#End Region

#Region "UI Browsing Animations"

    'Animation shown when going to the right on the main menu, moves items to the left
    Private Sub MoveAppsRight(SelectedApp As Image, NextApp As Image)

        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(100))
        PlayBackgroundSound(Sounds.Move)

        For Each App In MainCanvas.Children

            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then

                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move - 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheApp), .To = Canvas.GetLeft(TheApp) - 210, .Duration = MoveDuration}
                    Dispatcher.BeginInvoke(Sub() TheApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
                End If

            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name = "DiscApp" Then

                Dim TheDiscApp As Image = CType(App, Image)

                If IsDiscInserted Then
                    'If the disc app is not the selected one and also not the next one, move - 210
                    If TheDiscApp.Name IsNot SelectedApp.Name AndAlso TheDiscApp.Name IsNot NextApp.Name Then
                        Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheDiscApp), .To = Canvas.GetLeft(TheDiscApp) - 210, .Duration = MoveDuration}
                        Dispatcher.BeginInvoke(Sub() TheDiscApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
                    End If
                End If

            End If

        Next

        Dim NextAppLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(NextApp), .To = Canvas.GetLeft(NextApp) - 345, .Duration = MoveDuration}
        Dim NextAppWidthAnimation As New DoubleAnimation With {.From = 200, .To = 330, .Duration = MoveDuration}
        Dim NextAppHeightAnimation As New DoubleAnimation With {.From = 200, .To = 330, .Duration = MoveDuration}

        Dim SelectedAppLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(SelectedApp), .To = Canvas.GetLeft(SelectedApp) - 210, .Duration = MoveDuration}
        Dim SelectedAppWidthAnimation As New DoubleAnimation With {.From = 330, .To = 200, .Duration = MoveDuration}
        Dim SelectedAppHeightAnimation As New DoubleAnimation With {.From = 330, .To = 200, .Duration = MoveDuration}

        Dim SelectedAppBorderLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(SelectedAppBorder), .To = Canvas.GetLeft(SelectedAppBorder) + 60, .Duration = MoveDuration, .AutoReverse = True}

        Dispatcher.BeginInvoke(Sub()
                                   NextApp.BeginAnimation(Canvas.LeftProperty, NextAppLeftAnimation)
                                   NextApp.BeginAnimation(WidthProperty, NextAppWidthAnimation)
                                   NextApp.BeginAnimation(HeightProperty, NextAppHeightAnimation)

                                   SelectedApp.BeginAnimation(Canvas.LeftProperty, SelectedAppLeftAnimation)
                                   SelectedApp.BeginAnimation(WidthProperty, SelectedAppWidthAnimation)
                                   SelectedApp.BeginAnimation(HeightProperty, SelectedAppHeightAnimation)

                                   SelectedAppBorder.BeginAnimation(Canvas.LeftProperty, SelectedAppBorderLeftAnimation)
                                   SelectedAppBorder.BeginAnimation(HeightProperty, LastUIMoveAnimation)
                               End Sub)

        'Set the selected application title and start label shown on the main menu and focus the app
        AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppExecutable

        NextApp.Focus()
    End Sub

    'Animation shown when going to the left on the main menu, moves items to the right
    Private Sub MoveAppsLeft(SelectedApp As Image, NextApp As Image)

        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(100))
        PlayBackgroundSound(Sounds.Move)

        For Each App In MainCanvas.Children

            If TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("App") Then

                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move + 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheApp), .To = Canvas.GetLeft(TheApp) + 210, .Duration = MoveDuration}
                    Dispatcher.BeginInvoke(Sub() TheApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
                End If

            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name = "DiscApp" Then

                Dim TheDiscApp As Image = CType(App, Image)

                If IsDiscInserted Then
                    'If the disc app is not the selected one and also not the next one, move + 210
                    If TheDiscApp.Name IsNot SelectedApp.Name AndAlso TheDiscApp.Name IsNot NextApp.Name Then
                        Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheDiscApp), .To = Canvas.GetLeft(TheDiscApp) + 210, .Duration = MoveDuration}
                        Dispatcher.BeginInvoke(Sub() TheDiscApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
                    End If
                End If

            End If

        Next

        Dim NextAppLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(NextApp), .To = Canvas.GetLeft(NextApp) + 210, .Duration = MoveDuration}
        Dim NextAppWidthAnimation As New DoubleAnimation With {.From = 200, .To = 330, .Duration = MoveDuration}
        Dim NextAppHeightAnimation As New DoubleAnimation With {.From = 200, .To = 330, .Duration = MoveDuration}

        Dim SelectedAppLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(SelectedApp), .To = Canvas.GetLeft(SelectedApp) + 345, .Duration = MoveDuration}
        Dim SelectedAppWidthAnimation As New DoubleAnimation With {.From = 330, .To = 200, .Duration = MoveDuration}
        Dim SelectedAppHeightAnimation As New DoubleAnimation With {.From = 330, .To = 200, .Duration = MoveDuration}

        Dim SelectedAppBorderLeftAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(SelectedAppBorder), .To = Canvas.GetLeft(SelectedAppBorder) - 130, .Duration = MoveDuration, .AutoReverse = True}

        Dispatcher.BeginInvoke(Sub()
                                   NextApp.BeginAnimation(Canvas.LeftProperty, NextAppLeftAnimation)
                                   NextApp.BeginAnimation(WidthProperty, NextAppWidthAnimation)
                                   NextApp.BeginAnimation(HeightProperty, NextAppHeightAnimation)

                                   SelectedApp.BeginAnimation(Canvas.LeftProperty, SelectedAppLeftAnimation)
                                   SelectedApp.BeginAnimation(WidthProperty, SelectedAppWidthAnimation)
                                   SelectedApp.BeginAnimation(HeightProperty, SelectedAppHeightAnimation)

                                   SelectedAppBorder.BeginAnimation(Canvas.LeftProperty, SelectedAppBorderLeftAnimation)
                                   SelectedAppBorder.BeginAnimation(HeightProperty, LastUIMoveAnimation)
                               End Sub)

        AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppExecutable

        NextApp.Focus()
    End Sub

#End Region

#End Region

#Region "Input"

    'Keyboard input
    Private Sub MainWindow_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False AndAlso DidAnimate Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            Select Case e.Key
                Case Key.O
                    PlayBackgroundSound(Sounds.Options)
                    PauseInput = True

                    Dim OpenWindowsManager As New OpenWindows() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "MainWindow"}

                    If Not String.IsNullOrEmpty(StartedGameExecutable) Then
                        OpenWindowsManager.OtherProcess = StartedGameExecutable
                    End If

                    OpenWindowsManager.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    OpenWindowsManager.Show()
                Case Key.X
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
                            OrbisNotifications.NotificationPopup(MainCanvas, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png")
                        End If

                    End If
                Case Key.Left
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If App2.IsFocused And IsDiscInserted Then
                            NextAppImage = CType(MainCanvas.FindName("DiscApp"), Image)
                        ElseIf DiscApp.IsFocused Then
                            NextAppImage = CType(MainCanvas.FindName("App1"), Image)
                        Else
                            Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                            If MainCanvas.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(MainCanvas.FindName("App" + SelectedAppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        End If

                        'Do not move if there's no next app
                        If NextAppImage IsNot Nothing Then
                            DidAnimate = False

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)
                            ChangeBackgroundImage(NextAppDetails.AppPath)
                            MoveAppsLeft(SelectedApp, NextAppImage)
                        End If
                    End If
                Case Key.Right
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If App1.IsFocused And IsDiscInserted Then
                            NextAppImage = CType(MainCanvas.FindName("DiscApp"), Image)
                        ElseIf DiscApp.IsFocused Then
                            NextAppImage = CType(MainCanvas.FindName("App2"), Image)
                        Else
                            Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                            If MainCanvas.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(MainCanvas.FindName("App" + SelectedAppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        End If

                        If NextAppImage IsNot Nothing Then
                            DidAnimate = False

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)
                            ChangeBackgroundImage(NextAppDetails.AppPath)
                            MoveAppsRight(SelectedApp, NextAppImage)
                        End If
                    End If
                Case Key.F1
                    ReloadHome()
                Case Key.F2
                    MasterVolumeDown() 'Testing
                Case Key.F3
                    MasterVolumeUp() 'Testing
            End Select

            LastKeyboardKey = e.Key
        Else
            LastKeyboardKey = e.Key
            e.Handled = True
        End If
    End Sub

    Private Sub MainWindow_KeyUp(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    'Get Home keyboard input
    Private Sub NewGlobalKeyboardHook_KeyDown(Key As Keys) Handles NewGlobalKeyboardHook.KeyDown
        Select Case Key
            Case Keys.Home
                If PauseInput Then
                    If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                        Try
                            'Suspend the running game process
                            PauseProcessThreads(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                        Catch ex As Exception
                            PauseInput = True
                            ExceptionDialog("System Error", ex.Message)
                        End Try

                        WindowState = WindowState.Normal
                        Topmost = True
                        DidAnimate = False
                        ReturnAnimation()
                        Activate()
                        Topmost = False
                    End If
                Else
                    If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                        'Resume the game process and bring it to the front
                        PauseInput = True
                        ReduceUsage()
                        ResumeProcessThreads()
                        ShowProcess(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                    End If
                End If
        End Select
    End Sub

    'Gamepad input
    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber AndAlso DidAnimate Then
                If Not PauseInput Then

                    Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                    Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                    Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                    Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0
                    Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                    Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                    Dim MainGamepadButton_L_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.LeftShoulder) <> 0
                    Dim MainGamepadButton_R_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.RightShoulder) <> 0

                    Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                    Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                    Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                    Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                    'Get the focused element to select different actions
                    Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                    If MainGamepadButton_Back_Button_Pressed AndAlso MainGamepadButton_Start_Button_Pressed Then

                        If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                            'Resume the game process and bring it to the front
                            PauseInput = True
                            ReduceUsage()
                            ResumeProcessThreads()
                            ShowProcess(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                        End If

                        'Do not leave the buttons in a pressed state
                        MainGamepadButton_Back_Button_Pressed = False
                        MainGamepadButton_Start_Button_Pressed = False
                    End If

                    If MainGamepadButton_L_Button_Pressed AndAlso MainGamepadButton_R_Button_Pressed Then

                        ReloadHome()

                        'Do not leave the buttons in a pressed state
                        MainGamepadButton_L_Button_Pressed = False
                        MainGamepadButton_R_Button_Pressed = False
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
                                OrbisNotifications.NotificationPopup(MainCanvas, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png")
                            End If

                        End If
                    ElseIf MainGamepadButton_DPad_Left_Pressed Then
                        If TypeOf FocusedItem Is Image Then
                            Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                            Dim NextAppImage As Image

                            If App2.IsFocused And IsDiscInserted Then
                                NextAppImage = CType(MainCanvas.FindName("DiscApp"), Image)
                            ElseIf DiscApp.IsFocused Then
                                NextAppImage = CType(MainCanvas.FindName("App1"), Image)
                            Else
                                Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                                If MainCanvas.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                    NextAppImage = CType(MainCanvas.FindName("App" + SelectedAppNumber.ToString), Image)
                                Else
                                    NextAppImage = Nothing
                                End If
                            End If

                            'Do not move if there's no next app
                            If NextAppImage IsNot Nothing Then
                                DidAnimate = False

                                Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)
                                ChangeBackgroundImage(NextAppDetails.AppPath)
                                MoveAppsLeft(SelectedApp, NextAppImage)
                            End If

                        End If
                    ElseIf MainGamepadButton_DPad_Right_Pressed Then
                        If TypeOf FocusedItem Is Image Then
                            Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                            Dim NextAppImage As Image

                            If App1.IsFocused And IsDiscInserted Then
                                NextAppImage = CType(MainCanvas.FindName("DiscApp"), Image)
                            ElseIf DiscApp.IsFocused Then
                                NextAppImage = CType(MainCanvas.FindName("App2"), Image)
                            Else
                                Dim SelectedAppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                                If MainCanvas.FindName("App" + SelectedAppNumber.ToString) IsNot Nothing Then
                                    NextAppImage = CType(MainCanvas.FindName("App" + SelectedAppNumber.ToString), Image)
                                Else
                                    NextAppImage = Nothing
                                End If
                            End If

                            If NextAppImage IsNot Nothing Then
                                DidAnimate = False

                                Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)
                                ChangeBackgroundImage(NextAppDetails.AppPath)
                                MoveAppsRight(SelectedApp, NextAppImage)
                            End If

                        End If
                    ElseIf MainGamepadButton_Back_Button_Pressed Then
                        PlayBackgroundSound(Sounds.Options)
                        PauseInput = True

                        Dim OpenWindowsManager As New OpenWindows() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "MainWindow"}

                        If Not String.IsNullOrEmpty(StartedGameExecutable) Then
                            OpenWindowsManager.OtherProcess = StartedGameExecutable
                        End If

                        OpenWindowsManager.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        OpenWindowsManager.Show()
                    End If

                Else

                    Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                    Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                    If MainGamepadButton_Back_Button_Pressed AndAlso MainGamepadButton_Start_Button_Pressed Then

                        If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                            'Suspend the running game process
                            Try
                                PauseProcessThreads(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                            Catch ex As Exception
                                PauseInput = True
                                ExceptionDialog("System Error", ex.Message)
                            End Try

                            WindowState = WindowState.Normal
                            Topmost = True
                            DidAnimate = False
                            ReturnAnimation()
                            Activate()
                            Topmost = False
                        Else
                            'Resume the game process and bring it to the front
                            If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                                PauseInput = True
                                ReduceUsage()
                                ResumeProcessThreads()
                                ShowProcess(Path.GetFileNameWithoutExtension(StartedGameExecutable))
                            End If
                        End If

                        'Do not leave the buttons in a pressed state
                        MainGamepadButton_Back_Button_Pressed = False
                        MainGamepadButton_Start_Button_Pressed = False
                    End If

                End If
            End If

            MainGamepadPreviousState = MainGamepadState

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

#End Region

#Region "Timers"

    Private Sub ClockTimer_Tick(sender As Object, e As EventArgs) Handles ClockTimer.Tick
        'Update clock
        SystemClock.Text = Date.Now.ToString("HH:mm")

        'Border Glowing
        SelectedAppBorder.BeginAnimation(OpacityProperty, SelectedAppBorderAnimation)
    End Sub

    Private Sub SystemTimer_Tick(sender As Object, e As EventArgs) Handles SystemTimer.Tick
        'Get battery info
        If IsMobileDevice() Then
            Dim NewBatteryInfo As BatteryInfo = GetBatteryInfo()
            If NewBatteryInfo.BatteryStatus = Forms.BatteryChargeStatus.Charging Then
                BatteryPercentageTextBlock.Text = (NewBatteryInfo.BatteryPercentage * 100).ToString() + "%"
                BatteryIndicatorImage.Source = GetBatteryImage(NewBatteryInfo.BatteryPercentage, True)
            Else
                BatteryPercentageTextBlock.Text = (NewBatteryInfo.BatteryPercentage * 100).ToString() + "%"
                BatteryIndicatorImage.Source = GetBatteryImage(NewBatteryInfo.BatteryPercentage, False)
            End If
        Else
            If BatteryIndicatorImage.Source IsNot Nothing Then
                BatteryPercentageTextBlock.Text = ""
                BatteryIndicatorImage.Source = Nothing
            End If
        End If

        'Get WiFi info
        If IsWiFiRadioOn() Then
            Dim SignalStrenght As Integer = GetWiFiSignalStrenght()
            If SignalStrenght <> 0 Then
                WiFiNetworkNameStrenghtTextBlock.Text = GetConnectedWiFiNetworkSSID().ToString() + " - " + SignalStrenght.ToString() + "%"
                WiFiIndicatorImage.Source = GetWiFiSignalImage(SignalStrenght)
            End If
        Else
            If WiFiIndicatorImage.Source IsNot Nothing Then
                WiFiNetworkNameStrenghtTextBlock.Text = ""
                WiFiIndicatorImage.Source = Nothing
            End If
        End If
    End Sub

#End Region

#Region "Background"

    Public Sub SetBackground()
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

                OrbisDisplay.SetScaling(OrbisMainWindow, MainCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(OrbisMainWindow, MainCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

    Private Sub ChangeBackgroundImage(AppFilePath As String)
        If MainConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
            'Change background animation
            If Path.GetExtension(AppFilePath) = ".exe" Then
                If Not String.IsNullOrEmpty(CheckForExistingBackgroundAsset(AppFilePath)) Then
                    Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    Dispatcher.BeginInvoke(Sub()
                                               Dim TempBitmapImage = New BitmapImage()
                                               TempBitmapImage.BeginInit()
                                               TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
                                               TempBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache
                                               TempBitmapImage.UriSource = New Uri(CheckForExistingBackgroundAsset(AppFilePath), UriKind.RelativeOrAbsolute)
                                               TempBitmapImage.EndInit()
                                               BackgroundImage.Source = TempBitmapImage
                                           End Sub)

                    Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                End If
            Else
                If BackgroundMedia.Opacity = 0 Then
                    BackgroundImage.Source = Nothing
                    Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                End If
            End If
        End If
    End Sub

#End Region

#Region "Game Disc Info & Cover Loading"

    Private Sub PSXDatacenterBrowser_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) Handles GameDataBrowser.DocumentCompleted

        Dim CoverImgSrc As BitmapImage = Nothing

        If DiscContentType = "PS1" Then

            If SecondWebSearch = False Then
                'Load game properties site first
                Dim TableRows As HtmlElementCollection = GameDataBrowser.Document.GetElementsByTagName("tr")
                Dim TableGameIDs As New List(Of String)

                For Each Row As HtmlElement In TableRows
                    Try
                        Dim TableGameID As String = Row.GetElementsByTagName("td")(1).InnerText.Trim

                        If TableGameID.Contains(DiscGameID) Then
                            Dim GameURL As String = Row.GetElementsByTagName("td")(0).GetElementsByTagName("a")(0).GetAttribute("href")
                            SecondWebSearch = True

                            GameDataBrowser.Navigate(GameURL)
                        End If

                    Catch ex As ArgumentOutOfRangeException
                    End Try
                Next

            Else

                'Get the game infos
                Dim InfoRows As HtmlElementCollection = GameDataBrowser.Document.GetElementsByTagName("tr")

                'Game Title
                If InfoRows.Item(4).InnerText IsNot Nothing Then
                    GameDatabaseReturnedTitle = InfoRows.Item(4).InnerText.Split(PS1TitleSeparator, StringSplitOptions.RemoveEmptyEntries)(0)
                Else
                    GameDatabaseReturnedTitle = ""
                End If

                'Game Cover
                If GameDataBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src") IsNot Nothing Then
                    CoverImgSrc = New BitmapImage(New Uri(GameDataBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src")))
                End If

                If CoverImgSrc IsNot Nothing Then
                    OrbisNotifications.NotificationPopup(MainCanvas, GameDatabaseReturnedTitle, "PS1 CD-ROM is now ready.", CoverImgSrc.UriSource.AbsoluteUri)
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                               .AppIconPath = CoverImgSrc.UriSource.AbsoluteUri,
                                                               .AppExecutable = "Start Disc",
                                                               .AppPath = DiscDriveName}))
                Else
                    OrbisNotifications.NotificationPopup(MainCanvas, GameDatabaseReturnedTitle, "PS1 CD-ROM is now ready.", "/Icons/Media-PS1-icon.png")
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
                Dim InfoRows As HtmlElementCollection = GameDataBrowser.Document.GetElementsByTagName("tr")

                'Game Title
                If InfoRows.Item(4).InnerText IsNot Nothing Then
                    GameDatabaseReturnedTitle = InfoRows.Item(4).InnerText.Split(PS2TitleSeparator, StringSplitOptions.RemoveEmptyEntries)(0)
                Else
                    GameDatabaseReturnedTitle = ""
                End If

                'Game Cover
                If GameDataBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src") IsNot Nothing Then
                    CoverImgSrc = New BitmapImage(New Uri(GameDataBrowser.Document.GetElementById("table2").GetElementsByTagName("img")(1).GetAttribute("src")))
                End If

                If CoverImgSrc IsNot Nothing Then
                    OrbisNotifications.NotificationPopup(MainCanvas, GameDatabaseReturnedTitle, "PS2 DVD-ROM is now ready.", CoverImgSrc.UriSource.AbsoluteUri)
                Else
                    OrbisNotifications.NotificationPopup(MainCanvas, GameDatabaseReturnedTitle, "PS2 DVD-ROM is now ready.", "/Icons/Media-DVD-icon.png")
                End If

                'Check if a PS2 disc image exists
                If GameDataBrowser.Document.GetElementById("table29").GetElementsByTagName("tr")(2).GetElementsByTagName("a")(0).GetAttribute("href") IsNot Nothing Then
                    Dim GameDiscImageSrc = GameDataBrowser.Document.GetElementById("table29").GetElementsByTagName("tr")(2).GetElementsByTagName("a")(0).GetAttribute("href")

                    'Do a second web browser navigation to get the game disc image
                    SecondWebSearch = True
                    GameDataBrowser.Navigate(GameDiscImageSrc)
                End If

            Else

                'Get the game disc image (if exists)
                If GameDataBrowser.Document.GetElementsByTagName("img")(2).GetAttribute("src") IsNot Nothing Then
                    Dim GameDiscImage = New BitmapImage(New Uri(GameDataBrowser.Document.GetElementsByTagName("img")(2).GetAttribute("src")))
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

#End Region

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

    Public Sub ReduceUsage()
        NotificationBannerTextBlock.BeginAnimation(Canvas.LeftProperty, Nothing) 'Stop notification banner
        ClockTimer.Stop() 'Stop clock timer
        SystemTimer.Stop()

        BackgroundMedia.Stop()
        BackgroundMedia.Source = Nothing
        BackgroundImage.Source = Nothing
        Width = 0
        Height = 0

        WindowState = WindowState.Minimized

        'Force the garbage collector to collect
        GC.Collect()

        'Force SetProcessWorkingSetSize
        Dim CurrentProcessHandle As IntPtr = GetCurrentProcess()
        SetProcessWorkingSetSize(CurrentProcessHandle, -1, -1)
    End Sub

End Class
