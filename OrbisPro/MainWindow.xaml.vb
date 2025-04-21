Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports Newtonsoft.Json
Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisCDVDManager
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNetwork
Imports OrbisPro.OrbisPowerUtils
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports OrbisPro.ProcessUtils
Imports SharpDX.XInput

Class MainWindow

    'Used for current time, border glowing, battery & WiFi info
    Private WithEvents ClockTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5)}
    Private WithEvents SystemTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 45)}
    Private WithEvents DelayTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 3)}

    'Hook the 'Home' key
    Private WithEvents NewGlobalKeyboardHook As New OrbisKeyboardHook()

    'Background WebBrowser to retrieve some game infos (covers, cd image, description, ...)
    Private WithEvents GameDataBrowser As New WebBrowser()
    Public GameDatabaseReturnedTitle As String
    Public SecondWebSearch As Boolean = False

    'Used to keep track of some variables
    Public HomeAppsCount As Integer = 0
    Public HomeGroupAppsCount As Integer = 0
    Public GroupAppsOnHome As Boolean = False
    Public LastFocusedApp As Image = Nothing
    Public StartedGameExecutable As String

    Private IsBatteryPresent As Boolean = False
    Private IsWiFiAvailable As Boolean = False
    Private IsDeviceInterface As Boolean = False
    Private LastKeyboardKey As Key = Nothing
    Public DidAnimate As Boolean = False

    'Animations
    Dim WithEvents FirstHomeAnimation As New DoubleAnimation With {.From = 175, .To = 410, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
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

        'Set background
        MainCanvas.Background = New SolidColorBrush(Colors.Black)
        SetBackground()
    End Sub

    Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered

        'Run Home animation after background properties have been set
        HomeAnimation()

        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
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
                                                           .AppButtonExecutionString = "Start Disc",
                                                           .AppExecutableFilePath = DiscDriveName}))
        Else
            Dispatcher.BeginInvoke(Sub() PopupDeviceNotification("Disc " + DriveLetter, "Disc can now be used.", True))
        End If
    End Sub

#End Region

#Region "Animations"

    'Distance App1 - App2 = 345 (330 image size + 15 space to next app)
    'Distance App2 - App3 = 210 (200 image size + 10 space To Next app)
    'GroupApp Top default = 650 / On home = 209

    Private WithEvents GameStartAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(2))}

    'Animation shown when entering Home
    Private Sub HomeAnimation(Optional WithSound As Boolean = True)
        Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(500))

        If WithSound Then PlayBackgroundSound(Sounds.Start)

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

        Animate(SelectedAppBorder, Canvas.LeftProperty, 456, 280, AnimDuration)
        Animate(SelectedAppBorder, WidthProperty, 175, 340, AnimDuration)
        SelectedAppBorder.BeginAnimation(HeightProperty, FirstHomeAnimation)

        AppTitle.Visibility = Visibility.Visible
        AppStartLabel.Visibility = Visibility.Visible
        StartRect.Visibility = Visibility.Visible
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
        StartedGameExecutable = GameInfos.AppExecutableFilePath
        ReduceUsage()
        GameStarter.StartGame(GameInfos.AppExecutableFilePath, Platform:=GameInfos.AppPlatform)
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
            Case "Gallery"
                Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True, .Top = 0, .Left = 0, .Opacity = 0, .Opener = "MainWindow",
                    .MessageTitle = "Image/Picture Gallery",
                    .MessageDescription = "This Image/Picture Gallery application is not available yet. The File Explorer allows opening image files."}

                NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                NewSystemDialog.Show()
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
            AppStartLabel.Text = CType(App1.Tag, AppDetails).AppButtonExecutionString
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
    Public Sub AddNewApp(AppTitle As String, ExecutableFilePath As String, Optional IsFolder As Boolean = False, Optional FolderName As String = "", Optional BackgroundPath As String = "", Optional IconPath As String = "", Optional Platform As String = "")

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
            .Name = "App" + (InstalledAppsOnMenu + 1).ToString, 'Here the new app get it's number, so the code can iterate through it
            .Focusable = True,
            .FocusVisualStyle = Nothing,
            .Tag = New AppDetails() With {
            .AppTitle = AppTitle,
            .AppButtonExecutionString = "Start",
            .AppExecutableFilePath = ExecutableFilePath,
            .FolderName = FolderName,
            .IsFolder = IsFolder,
            .AppBackgroundPath = BackgroundPath,
            .AppIconPath = IconPath,
            .AppPlatform = Platform}}

        'Set app image to executable icon or existing asset file (if exists)
        If Not String.IsNullOrEmpty(ExecutableFilePath) Then
            If Path.GetExtension(ExecutableFilePath) = ".exe" Then
                If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(ExecutableFilePath)) Then
                    NewAppImage.Source = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(ExecutableFilePath), UriKind.RelativeOrAbsolute))
                Else
                    NewAppImage.Source = GetExecutableIconAsImageSource(ExecutableFilePath)
                End If
            Else
                If Not String.IsNullOrEmpty(IconPath) Then
                    NewAppImage.Source = New BitmapImage(New Uri(IconPath, UriKind.RelativeOrAbsolute))
                Else
                    NewAppImage.Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                End If
            End If
        End If

        If IsFolder AndAlso Not String.IsNullOrEmpty(FolderName) Then
            If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then
                Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
                Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
                Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
                Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

                'Get some executable paths to find some images for the folder
                Dim ListOfMatches As New List(Of String)()

                'Check the Apps list
                For Each RegisteredApp In AppsList.Apps()
                    If RegisteredApp.Group = FolderName Then
                        If Not String.IsNullOrEmpty(RegisteredApp.IconPath) Then
                            ListOfMatches.Add(RegisteredApp.IconPath)
                            Continue For 'Skip to next app if IconPath is present
                        ElseIf Not String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                            ListOfMatches.Add(RegisteredApp.ExecutablePath)
                        End If
                    End If
                Next

                'Check the Games list
                For Each RegisteredGame In GamesList.Games()
                    If RegisteredGame.Group = FolderName Then
                        'Check if an IconPath exists first
                        If Not String.IsNullOrEmpty(RegisteredGame.IconPath) Then
                            ListOfMatches.Add(RegisteredGame.IconPath)
                            Continue For 'Skip to next game if IconPath is present
                        ElseIf Not String.IsNullOrEmpty(RegisteredGame.ExecutablePath) Then
                            ListOfMatches.Add(RegisteredGame.ExecutablePath)
                        End If
                    End If
                Next

                If ListOfMatches.Count > 0 Then
                    Dim BitmapImage1 As BitmapImage = Nothing
                    Dim BitmapImage2 As BitmapImage = Nothing
                    Dim BitmapImage3 As BitmapImage = Nothing
                    Dim BitmapImage4 As BitmapImage = Nothing

                    For Each FoundMatch In ListOfMatches
                        If Path.GetExtension(FoundMatch) = ".exe" Then
                            'Get icon from assets
                            If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(FoundMatch)) Then
                                If BitmapImage1 Is Nothing Then
                                    BitmapImage1 = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(FoundMatch), UriKind.RelativeOrAbsolute))
                                ElseIf BitmapImage2 Is Nothing Then
                                    BitmapImage2 = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(FoundMatch), UriKind.RelativeOrAbsolute))
                                ElseIf BitmapImage3 Is Nothing Then
                                    BitmapImage3 = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(FoundMatch), UriKind.RelativeOrAbsolute))
                                ElseIf BitmapImage4 Is Nothing Then
                                    BitmapImage4 = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(FoundMatch), UriKind.RelativeOrAbsolute))
                                End If
                            End If
                        Else
                            'Use IconPath
                            If BitmapImage1 Is Nothing Then
                                BitmapImage1 = New BitmapImage(New Uri(FoundMatch, UriKind.RelativeOrAbsolute))
                            ElseIf BitmapImage2 Is Nothing Then
                                BitmapImage2 = New BitmapImage(New Uri(FoundMatch, UriKind.RelativeOrAbsolute))
                            ElseIf BitmapImage3 Is Nothing Then
                                BitmapImage3 = New BitmapImage(New Uri(FoundMatch, UriKind.RelativeOrAbsolute))
                            ElseIf BitmapImage4 Is Nothing Then
                                BitmapImage4 = New BitmapImage(New Uri(FoundMatch, UriKind.RelativeOrAbsolute))
                            End If
                        End If
                    Next

                    'Set the folder image
                    NewAppImage.Source = CreateFolderImage(BitmapImage1, BitmapImage2, BitmapImage3, BitmapImage4)
                End If
            End If
        End If

        NewAppImage.Height = 200
        NewAppImage.Width = 200
        NewAppImage.Stretch = Stretch.Uniform
        NewAppImage.StretchDirection = StretchDirection.Both

        'Get the last app on the main menu
        Dim LastAppInMenu As Image = CType(MainCanvas.FindName("App" + InstalledAppsOnMenu.ToString()), Image)
        HomeAppsCount = InstalledAppsOnMenu + 1

        'Set the position of the new app
        Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastAppInMenu) + 210)
        Canvas.SetTop(NewAppImage, Canvas.GetTop(LastAppInMenu))

        'Register new app
        RegisterName(NewAppImage.Name, NewAppImage)

        'Add the new app on the canvas
        MainCanvas.Children.Add(NewAppImage)
    End Sub

    'Animation shown when reloading Home
    Public Sub ReloadHome()
        'Reset
        HomeAppsCount = 0
        DidAnimate = False

        'Get UIElements (apps) to remove
        Dim AppsToRemove As New List(Of UIElement)()
        For Each App In MainCanvas.Children
            If TypeOf App Is Image Then

                Dim TheApp As Image = CType(App, Image)

                Select Case TheApp.Name
                    Case "App1", "App2", "App3", "App4", "App5", "App6", "App7"
                        Continue For
                    Case Else
                        'Remove only apps that have been added during runtime from the canvas
                        If TheApp.Name.StartsWith("App") Then
                            AppsToRemove.Add(TheApp)
                            UnregisterName(TheApp.Name)
                        ElseIf TheApp.Name.StartsWith("GroupApp") Then
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

        'Restore Home
        SetBackground()
        HomeAnimation(False)
    End Sub

    Public Sub ShowGroupAppsOnHome()
        Dim AnimDuration As New Duration(TimeSpan.FromMilliseconds(150))

        'Animate apps on the main menu
        For Each App In MainCanvas.Children
            If TypeOf App Is Image Then
                Dim AppImage As Image = CType(App, Image)
                If AppImage.Name.StartsWith("App") Then
                    Animate(AppImage, OpacityProperty, 1, 0, AnimDuration)
                    Animate(AppImage, Canvas.TopProperty, 209, -300, AnimDuration)
                ElseIf AppImage.Name.StartsWith("GroupApp") Then
                    Animate(AppImage, OpacityProperty, 0, 1, AnimDuration)
                    Animate(AppImage, Canvas.TopProperty, 650, 209, AnimDuration)
                End If
            End If
        Next

        'Fix layout
        Dim TheGroupApp1 As Image = Nothing
        For Each App In MainCanvas.Children
            If TypeOf App Is Image Then

                Dim TheGroupApp As Image = CType(App, Image)
                If TheGroupApp.Name = "GroupApp1" Then
                    TheGroupApp1 = TheGroupApp
                    Animate(TheGroupApp, Canvas.LeftProperty, 350, 285, AnimDuration)
                    Animate(TheGroupApp, WidthProperty, 200, 330, AnimDuration)
                    Animate(TheGroupApp, HeightProperty, 200, 330, AnimDuration)
                ElseIf TheGroupApp.Name.StartsWith("GroupApp") AndAlso TheGroupApp.Name IsNot "GroupApp1" Then
                    Canvas.SetLeft(TheGroupApp, Canvas.GetLeft(TheGroupApp) + 75)
                End If
            End If
        Next

        AppTitle.Text = CType(TheGroupApp1.Tag, AppDetails).AppTitle
        AppStartLabel.Text = CType(TheGroupApp1.Tag, AppDetails).AppButtonExecutionString
        TheGroupApp1.Focus()
    End Sub

    Public Sub RemoveGroupAppsOnHome()
        Dim AnimDuration As New Duration(TimeSpan.FromMilliseconds(150))

        'Animate apps on the main menu
        For Each App In MainCanvas.Children
            If TypeOf App Is Image Then
                Dim AppImage As Image = CType(App, Image)
                If AppImage.Name.StartsWith("App") Then
                    Animate(AppImage, OpacityProperty, 0, 1, AnimDuration)
                    Animate(AppImage, Canvas.TopProperty, -300, 209, AnimDuration)
                ElseIf AppImage.Name = "GroupApp1" Then
                    Animate(AppImage, WidthProperty, 330, 200, AnimDuration)
                    Animate(AppImage, HeightProperty, 330, 200, AnimDuration)
                    Animate(AppImage, Canvas.LeftProperty, 285, 350, AnimDuration)
                    Animate(AppImage, Canvas.TopProperty, 209, 650, AnimDuration)
                ElseIf AppImage.Name.StartsWith("GroupApp") AndAlso AppImage.Name IsNot "GroupApp1" Then
                    Animate(AppImage, Canvas.TopProperty, 209, 650, AnimDuration)
                End If
            End If
        Next

        'Fix layout
        For Each App In MainCanvas.Children
            If TypeOf App Is Image Then
                Dim TheGroupApp As Image = CType(App, Image)
                If TheGroupApp.Name.StartsWith("GroupApp") AndAlso TheGroupApp.Name IsNot "GroupApp1" Then
                    Canvas.SetLeft(TheGroupApp, Canvas.GetLeft(TheGroupApp) - 75)
                End If
            End If
        Next

        AppTitle.Text = CType(LastFocusedApp.Tag, AppDetails).FolderName
        AppStartLabel.Text = "↓"
        LastFocusedApp.Focus()
    End Sub

#Region "Animation Events"

    Private Sub FirstHomeAnimation_Completed(sender As Object, e As EventArgs) Handles FirstHomeAnimation.Completed
        'Version Banner
        NotificationBannerTextBlock.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)
        NotificationBannerTextBlock.Dispatcher.Invoke(Sub() NotificationBannerTextBlock.Text = "Initializing library, please wait")

        'Check for battery info
        If IsMobileDevice() Then

            IsBatteryPresent = True

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

        'Check for WiFi info
        If IsWiFiRadioOn() Then

            IsWiFiAvailable = True

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

        'Clock
        SystemClock.Text = Date.Now.ToString("HH:mm")
        ClockTimer.Start()

        'Battery & WiFi status background timer (do not start when no battery or WiFi available)
        If IsBatteryPresent Or IsWiFiAvailable Then
            SystemTimer.Start()
        End If

        DelayTimer.Start()

        App1.Focus()
    End Sub

    Private Sub GameStartAnimation_Completed(sender As Object, e As EventArgs) Handles GameStartAnimation.Completed
        Dim DiscInfos = CType(DiscApp.Tag, AppDetails)
        StartCDVD(DiscContentType, DiscInfos.AppExecutableFilePath)
    End Sub

    Private Sub LastUIMoveAnimation_Completed(sender As Object, e As EventArgs) Handles LastUIMoveAnimation.Completed
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

            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("GroupApp") Then

                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move - 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheApp), .To = Canvas.GetLeft(TheApp) - 210, .Duration = MoveDuration}
                    Dispatcher.BeginInvoke(Sub() TheApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
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

        'Remove previous group view if exists
        If Not NextApp.Name.StartsWith("GroupApp") Then
            If MainCanvas.FindName("GroupApp1") IsNot Nothing Then
                Dim GroupAppsToRemove As New List(Of UIElement)()

                'Get all GroupApps
                For Each GroupApp In MainCanvas.Children
                    If TypeOf GroupApp Is Image Then
                        Dim TheGroupApp As Image = CType(GroupApp, Image)
                        If TheGroupApp.Name.StartsWith("GroupApp") Then
                            GroupAppsToRemove.Add(TheGroupApp)
                            UnregisterName(TheGroupApp.Name)
                        End If
                    End If
                Next

                'Remove the UIElements from the canvas (removing while iterating will throw an exception)
                For Each AppToRemove In GroupAppsToRemove
                    MainCanvas.Children.Remove(AppToRemove)
                Next

                HomeGroupAppsCount = 0
            End If
        End If

        'Show group items when next items is a folder
        If CType(NextApp.Tag, AppDetails).IsFolder Then
            If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then

                Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
                Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
                Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
                Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

                If AppsList IsNot Nothing AndAlso GamesList IsNot Nothing Then

                    'Check the Apps list
                    For Each RegisteredApp In AppsList.Apps()
                        'Check if the next selected app is within a group
                        If RegisteredApp.Group = CType(NextApp.Tag, AppDetails).FolderName Then

                            'Declare new group app
                            Dim NewAppImage As New Image With {
                                        .Name = "GroupApp" + (HomeGroupAppsCount + 1).ToString, 'Here the new GroupApp get it's number, so the code can iterate through it
                                        .Focusable = True,
                                        .FocusVisualStyle = Nothing,
                                        .Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .AppExecutableFilePath = RegisteredApp.ExecutablePath}
                                    }

                            'Set app image to executable icon or existing asset file (if exists)
                            If Not String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                                If Path.GetExtension(RegisteredApp.ExecutablePath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath)) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath), UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = GetExecutableIconAsImageSource(RegisteredApp.ExecutablePath)
                                    End If
                                Else
                                    If Not String.IsNullOrEmpty(RegisteredApp.IconPath) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(RegisteredApp.IconPath, UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                    End If
                                End If
                            End If

                            NewAppImage.Height = 200
                            NewAppImage.Width = 200
                            NewAppImage.Stretch = Stretch.Uniform
                            NewAppImage.StretchDirection = StretchDirection.Both

                            If HomeGroupAppsCount = 0 Then
                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, 350)
                                Canvas.SetTop(NewAppImage, 650)

                            Else
                                'Get the last app on the main menu
                                Dim LastGroupAppInMenu As Image = CType(MainCanvas.FindName("GroupApp" + HomeGroupAppsCount.ToString()), Image)

                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastGroupAppInMenu) + 205)
                                Canvas.SetTop(NewAppImage, Canvas.GetTop(LastGroupAppInMenu))
                            End If

                            HomeGroupAppsCount += 1

                            'Register new app
                            RegisterName(NewAppImage.Name, NewAppImage)

                            'Add the new app on the canvas
                            MainCanvas.Children.Add(NewAppImage)
                            Dispatcher.BeginInvoke(Sub() NewAppImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(200))}))
                        End If
                    Next

                    'Check the Games list
                    For Each RegisteredGame In GamesList.Games()
                        'Check if the next selected app is within a group
                        If RegisteredGame.Group = CType(NextApp.Tag, AppDetails).FolderName Then

                            'Declare new group app
                            Dim NewAppImage As New Image With {
                                        .Name = "GroupApp" + (HomeGroupAppsCount + 1).ToString, 'Here the new GroupApp get it's number, so the code can iterate through it
                                        .Focusable = True,
                                        .FocusVisualStyle = Nothing,
                                        .Tag = New AppDetails() With {.AppTitle = RegisteredGame.Name, .AppButtonExecutionString = "Start", .AppExecutableFilePath = RegisteredGame.ExecutablePath}
                                    }

                            'Set app image to executable icon or existing asset file (if exists)
                            If Not String.IsNullOrEmpty(RegisteredGame.ExecutablePath) Then
                                If Path.GetExtension(RegisteredGame.ExecutablePath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredGame.ExecutablePath)) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredGame.ExecutablePath), UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = GetExecutableIconAsImageSource(RegisteredGame.ExecutablePath)
                                    End If
                                Else
                                    If Not String.IsNullOrEmpty(RegisteredGame.IconPath) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(RegisteredGame.IconPath, UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                    End If
                                End If
                            End If

                            NewAppImage.Height = 200
                            NewAppImage.Width = 200
                            NewAppImage.Stretch = Stretch.Uniform
                            NewAppImage.StretchDirection = StretchDirection.Both

                            If HomeGroupAppsCount = 0 Then
                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, 350)
                                Canvas.SetTop(NewAppImage, 650)

                            Else
                                'Get the last app on the main menu
                                Dim LastGroupAppInMenu As Image = CType(MainCanvas.FindName("GroupApp" + HomeGroupAppsCount.ToString()), Image)

                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastGroupAppInMenu) + 205)
                                Canvas.SetTop(NewAppImage, Canvas.GetTop(LastGroupAppInMenu))
                            End If

                            HomeGroupAppsCount += 1

                            'Register new app
                            RegisterName(NewAppImage.Name, NewAppImage)

                            'Add the new app on the canvas
                            MainCanvas.Children.Add(NewAppImage)
                            Dispatcher.BeginInvoke(Sub() NewAppImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(200))}))
                        End If
                    Next

                End If
            End If

            AppTitle.Text = CType(NextApp.Tag, AppDetails).FolderName
            AppStartLabel.Text = "↓"
        Else
            AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
            AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppButtonExecutionString
        End If

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
            ElseIf TypeOf App Is Image AndAlso CType(App, Image).Name.StartsWith("GroupApp") Then
                Dim TheApp As Image = CType(App, Image)

                'If the app is not the selected one and also not the next one, move + 210
                If TheApp.Name IsNot SelectedApp.Name AndAlso TheApp.Name IsNot NextApp.Name Then
                    Dim NewAnimation As New DoubleAnimation With {.From = Canvas.GetLeft(TheApp), .To = Canvas.GetLeft(TheApp) + 210, .Duration = MoveDuration}
                    Dispatcher.BeginInvoke(Sub() TheApp.BeginAnimation(Canvas.LeftProperty, NewAnimation))
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

        'Remove previous group view if exists
        If Not NextApp.Name.StartsWith("GroupApp") Then
            If MainCanvas.FindName("GroupApp1") IsNot Nothing Then
                Dim GroupAppsToRemove As New List(Of UIElement)()

                'Get all GroupApps
                For Each GroupApp In MainCanvas.Children
                    If TypeOf GroupApp Is Image Then
                        Dim TheGroupApp As Image = CType(GroupApp, Image)
                        If TheGroupApp.Name.StartsWith("GroupApp") Then
                            GroupAppsToRemove.Add(TheGroupApp)
                            UnregisterName(TheGroupApp.Name)
                        End If
                    End If
                Next
                'Remove the UIElements from the canvas (removing while iterating will throw an exception)
                For Each AppToRemove In GroupAppsToRemove
                    MainCanvas.Children.Remove(AppToRemove)
                Next

                HomeGroupAppsCount = 0
            End If
        End If

        'Show group items when next items is a folder
        If CType(NextApp.Tag, AppDetails).IsFolder Then
            If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then

                Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
                Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
                Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
                Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

                If AppsList IsNot Nothing AndAlso GamesList IsNot Nothing Then

                    'Check the Apps list
                    For Each RegisteredApp In AppsList.Apps()
                        'Check if the next selected app is within a group
                        If RegisteredApp.Group = CType(NextApp.Tag, AppDetails).FolderName Then

                            'Declare new group app
                            Dim NewAppImage As New Image With {
                                        .Name = "GroupApp" + (HomeGroupAppsCount + 1).ToString, 'Here the new GroupApp get it's number, so the code can iterate through it
                                        .Focusable = True,
                                        .FocusVisualStyle = Nothing,
                                        .Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .AppExecutableFilePath = RegisteredApp.ExecutablePath}
                                    }

                            'Set app image to executable icon or existing asset file (if exists)
                            If Not String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                                If Path.GetExtension(RegisteredApp.ExecutablePath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath)) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath), UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = GetExecutableIconAsImageSource(RegisteredApp.ExecutablePath)
                                    End If
                                Else
                                    If Not String.IsNullOrEmpty(RegisteredApp.IconPath) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(RegisteredApp.IconPath, UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                    End If
                                End If
                            End If

                            NewAppImage.Height = 200
                            NewAppImage.Width = 200
                            NewAppImage.Stretch = Stretch.Uniform
                            NewAppImage.StretchDirection = StretchDirection.Both

                            If HomeGroupAppsCount = 0 Then
                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, 350)
                                Canvas.SetTop(NewAppImage, 650)

                            Else
                                'Get the last app on the main menu
                                Dim LastGroupAppInMenu As Image = CType(MainCanvas.FindName("GroupApp" + HomeGroupAppsCount.ToString()), Image)

                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastGroupAppInMenu) + 205)
                                Canvas.SetTop(NewAppImage, Canvas.GetTop(LastGroupAppInMenu))
                            End If

                            HomeGroupAppsCount += 1

                            'Register new app
                            RegisterName(NewAppImage.Name, NewAppImage)

                            'Add the new app on the canvas
                            MainCanvas.Children.Add(NewAppImage)
                            Dispatcher.BeginInvoke(Sub() NewAppImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(200))}))
                        End If
                    Next

                    'Check the Games list
                    For Each RegisteredGame In GamesList.Games()
                        'Check if the next selected app is within a group
                        If RegisteredGame.Group = CType(NextApp.Tag, AppDetails).FolderName Then

                            'Declare new group app
                            Dim NewAppImage As New Image With {
                                        .Name = "GroupApp" + (HomeGroupAppsCount + 1).ToString, 'Here the new GroupApp get it's number, so the code can iterate through it
                                        .Focusable = True,
                                        .FocusVisualStyle = Nothing,
                                        .Tag = New AppDetails() With {.AppTitle = RegisteredGame.Name, .AppButtonExecutionString = "Start", .AppExecutableFilePath = RegisteredGame.ExecutablePath}
                                    }

                            'Set app image to executable icon or existing asset file (if exists)
                            If Not String.IsNullOrEmpty(RegisteredGame.ExecutablePath) Then
                                If Path.GetExtension(RegisteredGame.ExecutablePath) = ".exe" Then
                                    If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredGame.ExecutablePath)) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredGame.ExecutablePath), UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = GetExecutableIconAsImageSource(RegisteredGame.ExecutablePath)
                                    End If
                                Else
                                    If Not String.IsNullOrEmpty(RegisteredGame.IconPath) Then
                                        NewAppImage.Source = New BitmapImage(New Uri(RegisteredGame.IconPath, UriKind.RelativeOrAbsolute))
                                    Else
                                        NewAppImage.Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                    End If
                                End If
                            End If

                            NewAppImage.Height = 200
                            NewAppImage.Width = 200
                            NewAppImage.Stretch = Stretch.Uniform
                            NewAppImage.StretchDirection = StretchDirection.Both

                            If HomeGroupAppsCount = 0 Then
                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, 350)
                                Canvas.SetTop(NewAppImage, 650)

                            Else
                                'Get the last app on the main menu
                                Dim LastGroupAppInMenu As Image = CType(MainCanvas.FindName("GroupApp" + HomeGroupAppsCount.ToString()), Image)

                                'Set the position of the new app
                                Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastGroupAppInMenu) + 205)
                                Canvas.SetTop(NewAppImage, Canvas.GetTop(LastGroupAppInMenu))
                            End If

                            HomeGroupAppsCount += 1

                            'Register new app
                            RegisterName(NewAppImage.Name, NewAppImage)

                            'Add the new app on the canvas
                            MainCanvas.Children.Add(NewAppImage)
                            Dispatcher.BeginInvoke(Sub() NewAppImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(200))}))
                        End If
                    Next

                End If
            End If

            AppTitle.Text = CType(NextApp.Tag, AppDetails).FolderName
            AppStartLabel.Text = "↓"
        Else
            AppTitle.Text = CType(NextApp.Tag, AppDetails).AppTitle
            AppStartLabel.Text = CType(NextApp.Tag, AppDetails).AppButtonExecutionString
        End If

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

                    'Open the Open Windows Manager
                    Dim OpenWindowsManager As New OpenWindows() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "MainWindow"}

                    'Let the Open Windows Manager know there is an active process running
                    If Not String.IsNullOrEmpty(StartedGameExecutable) Then
                        OpenWindowsManager.OtherProcess = StartedGameExecutable
                    End If

                    OpenWindowsManager.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    OpenWindowsManager.Show()
                Case Key.X
                    If TypeOf FocusedItem Is Image Then

                        If String.IsNullOrEmpty(StartedGameExecutable) Then
                            Dim FocusedApp As Image = TryCast(FocusedItem, Image)
                            Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppExecutableFilePath 'FocusedAppPath = Executable path

                            PauseInput = True

                            If String.IsNullOrEmpty(FocusedAppPath) Then
                                SimpleAppStartAnimation(FocusedApp)
                            Else
                                StartGameAnimation(FocusedApp)
                            End If
                        Else
                            OrbisNotifications.NotificationPopup(MainCanvas, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png")
                        End If

                        'Side menu actions
                    ElseIf TypeOf FocusedItem Is Controls.Button Then

                        Dim SelectedButton As Controls.Button = CType(FocusedItem, Controls.Button)
                        Dim SelectedButtonValue As String = SelectedButton.Content.ToString()

                        Select Case SelectedButtonValue
                            Case "Add to Folder"

                                'Hide the side menu
                                If Canvas.GetLeft(RightMenu) = 1430 Then
                                    PlayBackgroundSound(Sounds.SelectItem)
                                    Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                    Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                    Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                    Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                    LastFocusedApp.Focus()
                                End If

                                PauseInput = True
                                PlayBackgroundSound(Sounds.SelectItem)

                                'Show the existing folders
                                Dim NewExistingFoldersWindow As New ExistingFolders() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow", .FirstSelectedAppOrGame = LastFocusedApp}
                                NewExistingFoldersWindow.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                NewExistingFoldersWindow.Show()

                            Case "Delete"



                            Case "Delete Folder"
                                PauseInput = True

                                Dim FolderToDelete As String = CType(LastFocusedApp.Tag, AppDetails).FolderName
                                Dim FolderContentNames As List(Of String) = OrbisFolders.GetFolderContentNames(FolderToDelete)

                                'Change the group name
                                For Each GameApp In FolderContentNames
                                    OrbisFolders.ChangeFolderNameOfAppGame("", GameApp)
                                Next

                                'Remove folder from list
                                OrbisFolders.RemoveFolder(FolderToDelete)

                                'Hide the side menu
                                If Canvas.GetLeft(RightMenu) = 1430 Then
                                    PlayBackgroundSound(Sounds.SelectItem)
                                    Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                    Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                    Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                    Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                End If

                                'Reload Home
                                DidAnimate = False
                                ReloadHome()
                        End Select
                    End If
                Case Key.Left
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If SelectedApp.Name.StartsWith("GroupApp") Then
                            'Move within the folder items
                            Dim SelectedGroupApppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                            If MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        Else
                            'Move within the Home items
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
                        End If

                        'Do not move if there's no next app
                        If NextAppImage IsNot Nothing Then
                            DidAnimate = False

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                            If Not String.IsNullOrEmpty(NextAppDetails.AppBackgroundPath) Then
                                ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath, NextAppDetails.AppBackgroundPath)
                            Else
                                ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath)
                            End If

                            MoveAppsLeft(SelectedApp, NextAppImage)
                        End If
                    End If
                Case Key.Right
                    If TypeOf FocusedItem Is Image Then
                        Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                        Dim NextAppImage As Image

                        If SelectedApp.Name.StartsWith("GroupApp") Then
                            Dim SelectedGroupApppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                            If MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString) IsNot Nothing Then
                                NextAppImage = CType(MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString), Image)
                            Else
                                NextAppImage = Nothing
                            End If
                        Else
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
                        End If

                        If NextAppImage IsNot Nothing Then
                            DidAnimate = False

                            Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                            If Not String.IsNullOrEmpty(NextAppDetails.AppBackgroundPath) Then
                                ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath, NextAppDetails.AppBackgroundPath)
                            Else
                                ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath)
                            End If

                            MoveAppsRight(SelectedApp, NextAppImage)
                        End If
                    End If
                Case Key.Up
                    If TypeOf FocusedItem Is Controls.Button Then
                        PlayBackgroundSound(Sounds.Move)

                        If SettingButton1.IsFocused Then
                            SettingButton3.Focus()
                        ElseIf SettingButton3.IsFocused Then
                            SettingButton2.Focus()
                        ElseIf SettingButton2.IsFocused Then
                            SettingButton1.Focus()
                        End If
                    ElseIf TypeOf FocusedItem Is Image Then
                        PlayBackgroundSound(Sounds.Move)
                        Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                        'Remove the GroupApps from main Home screen (move them down)
                        If FocusedApp.Name = "GroupApp1" Then
                            If GroupAppsOnHome Then
                                RemoveGroupAppsOnHome()
                                GroupAppsOnHome = False
                            End If
                        End If
                    End If
                Case Key.Down
                    If TypeOf FocusedItem Is Controls.Button Then
                        PlayBackgroundSound(Sounds.Move)

                        If SettingButton1.IsFocused Then
                            SettingButton2.Focus()
                        ElseIf SettingButton2.IsFocused Then
                            SettingButton3.Focus()
                        ElseIf SettingButton3.IsFocused Then
                            SettingButton1.Focus()
                        End If
                    ElseIf TypeOf FocusedItem Is Image Then
                        PlayBackgroundSound(Sounds.Move)
                        Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                        'Show the GroupApps on main Home screen (move them up)
                        If CType(FocusedApp.Tag, AppDetails).IsFolder Then
                            If GroupAppsOnHome = False Then
                                LastFocusedApp = FocusedApp
                                GroupAppsOnHome = True
                                ShowGroupAppsOnHome()
                            End If
                        End If
                    End If
                Case Key.F1
                    DidAnimate = False
                    ReloadHome()
                Case Key.F2
                    If TypeOf FocusedItem Is Image Then

                        Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                        If Canvas.GetLeft(RightMenu) = 1925 Then
                            LastFocusedApp = FocusedApp

                            'Show the side menu
                            PlayBackgroundSound(Sounds.SelectItem)

                            'Make sure the menu is always on top (elements added during rutime would mess up the side menu)
                            EnsureHighestZIndex(RightMenu)

                            Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                            If Not CType(FocusedApp.Tag, AppDetails).IsFolder Then
                                SettingButton1.Content = "Add to Folder"
                                SettingButton2.Content = "Delete"
                                SettingButton3.Content = "Information"

                                ShowSideSettingButtons(3)
                            Else
                                SettingButton1.Content = "Delete Folder"

                                ShowSideSettingButtons(1)
                            End If

                            'Set focus on the first available option
                            SettingButton1.Focus()
                        End If

                    ElseIf TypeOf FocusedItem Is Controls.Button Then
                        If Canvas.GetLeft(RightMenu) = 1430 Then
                            'Hide the side menu
                            PlayBackgroundSound(Sounds.SelectItem)
                            Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                            Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                            Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                            Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                            'Set the focus back
                            LastFocusedApp.Focus()
                        End If
                    End If
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
                            PauseProcessThreads(ActiveProcess.StartInfo.FileName)
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
                        ShowProcess(ActiveProcess.StartInfo.FileName)
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
                'While Home is present
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
                            ShowProcess(ActiveProcess.StartInfo.FileName)
                        End If

                        'Do not leave the buttons in a pressed state
                        MainGamepadButton_Back_Button_Pressed = False
                        MainGamepadButton_Start_Button_Pressed = False
                    End If

                    If MainGamepadButton_L_Button_Pressed AndAlso MainGamepadButton_R_Button_Pressed Then

                        DidAnimate = False
                        ReloadHome()

                        'Do not leave the buttons in a pressed state
                        MainGamepadButton_L_Button_Pressed = False
                        MainGamepadButton_R_Button_Pressed = False
                    End If

                    If MainGamepadButton_A_Button_Pressed Then
                        If TypeOf FocusedItem Is Image Then

                            If String.IsNullOrEmpty(StartedGameExecutable) Then
                                Dim FocusedApp As Image = TryCast(FocusedItem, Image)
                                Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppExecutableFilePath 'FocusedAppPath = Executable path

                                PauseInput = True

                                If String.IsNullOrEmpty(FocusedAppPath) Then
                                    SimpleAppStartAnimation(FocusedApp)
                                Else
                                    StartGameAnimation(FocusedApp)
                                End If
                            Else
                                OrbisNotifications.NotificationPopup(MainCanvas, "Game Running", "A game is already running.", "/Icons/Media-CD-icon.png")
                            End If

                            'Side menu actions
                        ElseIf TypeOf FocusedItem Is Controls.Button Then

                            Dim SelectedButton As Controls.Button = CType(FocusedItem, Controls.Button)
                            Dim SelectedButtonValue As String = SelectedButton.Content.ToString()

                            Select Case SelectedButtonValue
                                Case "Add to Folder"

                                    'Hide the side menu
                                    If Canvas.GetLeft(RightMenu) = 1430 Then
                                        PlayBackgroundSound(Sounds.SelectItem)
                                        Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                        Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                        Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                        Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                        LastFocusedApp.Focus()
                                    End If

                                    PauseInput = True
                                    PlayBackgroundSound(Sounds.SelectItem)

                                    'Show the existing folders
                                    Dim NewExistingFoldersWindow As New ExistingFolders() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "MainWindow", .FirstSelectedAppOrGame = LastFocusedApp}
                                    NewExistingFoldersWindow.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                    NewExistingFoldersWindow.Show()

                                Case "Delete"
                                    PauseInput = True



                                Case "Delete Folder"
                                    PauseInput = True

                                    Dim FolderToDelete As String = CType(LastFocusedApp.Tag, AppDetails).FolderName
                                    Dim FolderContentNames As List(Of String) = OrbisFolders.GetFolderContentNames(FolderToDelete)

                                    'Change the group name
                                    For Each GameApp In FolderContentNames
                                        OrbisFolders.ChangeFolderNameOfAppGame("", GameApp)
                                    Next

                                    'Remove folder from list
                                    OrbisFolders.RemoveFolder(FolderToDelete)

                                    'Hide the side menu
                                    If Canvas.GetLeft(RightMenu) = 1430 Then
                                        PlayBackgroundSound(Sounds.SelectItem)
                                        Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                        Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                        Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                        Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                    End If

                                    'Reload Home
                                    DidAnimate = False
                                    ReloadHome()
                            End Select
                        End If
                    ElseIf MainGamepadButton_DPad_Left_Pressed Then
                        If TypeOf FocusedItem Is Image Then
                            Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                            Dim NextAppImage As Image

                            If SelectedApp.Name.StartsWith("GroupApp") Then
                                'Move within the folder items
                                Dim SelectedGroupApppNumber As Integer = GetIntegerOnly(SelectedApp.Name) - 1
                                If MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString) IsNot Nothing Then
                                    NextAppImage = CType(MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString), Image)
                                Else
                                    NextAppImage = Nothing
                                End If
                            Else
                                'Move within the Home items
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
                            End If

                            'Do not move if there's no next app
                            If NextAppImage IsNot Nothing Then
                                DidAnimate = False

                                Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                                If Not String.IsNullOrEmpty(NextAppDetails.AppBackgroundPath) Then
                                    ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath, NextAppDetails.AppBackgroundPath)
                                Else
                                    ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath)
                                End If

                                MoveAppsLeft(SelectedApp, NextAppImage)
                            End If

                        End If
                    ElseIf MainGamepadButton_DPad_Right_Pressed Then
                        If TypeOf FocusedItem Is Image Then
                            Dim SelectedApp As Image = TryCast(FocusedItem, Image)
                            Dim NextAppImage As Image

                            If SelectedApp.Name.StartsWith("GroupApp") Then
                                Dim SelectedGroupApppNumber As Integer = GetIntegerOnly(SelectedApp.Name) + 1
                                If MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString) IsNot Nothing Then
                                    NextAppImage = CType(MainCanvas.FindName("GroupApp" + SelectedGroupApppNumber.ToString), Image)
                                Else
                                    NextAppImage = Nothing
                                End If
                            Else
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
                            End If

                            If NextAppImage IsNot Nothing Then
                                DidAnimate = False

                                Dim NextAppDetails As AppDetails = CType(NextAppImage.Tag, AppDetails)

                                If Not String.IsNullOrEmpty(NextAppDetails.AppBackgroundPath) Then
                                    ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath, NextAppDetails.AppBackgroundPath)
                                Else
                                    ChangeBackgroundImage(NextAppDetails.AppExecutableFilePath)
                                End If

                                MoveAppsRight(SelectedApp, NextAppImage)
                            End If

                        End If
                    ElseIf MainGamepadButton_DPad_Up_Pressed Then
                        If TypeOf FocusedItem Is Controls.Button Then
                            PlayBackgroundSound(Sounds.Move)

                            If SettingButton1.IsFocused Then
                                SettingButton3.Focus()
                            ElseIf SettingButton3.IsFocused Then
                                SettingButton2.Focus()
                            ElseIf SettingButton2.IsFocused Then
                                SettingButton1.Focus()
                            End If
                        ElseIf TypeOf FocusedItem Is Image Then
                            PlayBackgroundSound(Sounds.Move)
                            Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                            'Remove the GroupApps from main Home screen (move them down)
                            If FocusedApp.Name = "GroupApp1" Then
                                If GroupAppsOnHome Then
                                    RemoveGroupAppsOnHome()
                                    GroupAppsOnHome = False
                                End If
                            End If
                        End If
                    ElseIf MainGamepadButton_DPad_Down_Pressed Then
                        If TypeOf FocusedItem Is Controls.Button Then
                            PlayBackgroundSound(Sounds.Move)

                            If SettingButton1.IsFocused Then
                                SettingButton2.Focus()
                            ElseIf SettingButton2.IsFocused Then
                                SettingButton3.Focus()
                            ElseIf SettingButton3.IsFocused Then
                                SettingButton1.Focus()
                            End If
                        ElseIf TypeOf FocusedItem Is Image Then
                            PlayBackgroundSound(Sounds.Move)
                            Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                            'Show the GroupApps on main Home screen (move them up)
                            If CType(FocusedApp.Tag, AppDetails).IsFolder Then
                                If GroupAppsOnHome = False Then
                                    LastFocusedApp = FocusedApp
                                    GroupAppsOnHome = True
                                    ShowGroupAppsOnHome()
                                End If
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
                    ElseIf MainGamepadButton_Start_Button_Pressed Then
                        If TypeOf FocusedItem Is Image Then

                            Dim FocusedApp As Image = TryCast(FocusedItem, Image)

                            If Canvas.GetLeft(RightMenu) = 1925 Then
                                LastFocusedApp = FocusedApp

                                'Show the side menu
                                PlayBackgroundSound(Sounds.SelectItem)

                                'Make sure the menu is always on top (elements added during rutime would mess up the side menu)
                                EnsureHighestZIndex(RightMenu)

                                Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                                If Not CType(FocusedApp.Tag, AppDetails).IsFolder Then
                                    SettingButton1.Content = "Add to Folder"
                                    SettingButton2.Content = "Delete"
                                    SettingButton3.Content = "Information"

                                    ShowSideSettingButtons(3)
                                Else
                                    SettingButton1.Content = "Delete Folder"

                                    ShowSideSettingButtons(1)
                                End If

                                'Set focus on the first available option
                                SettingButton1.Focus()
                            End If

                        ElseIf TypeOf FocusedItem Is Controls.Button Then
                            If Canvas.GetLeft(RightMenu) = 1430 Then
                                'Hide the side menu
                                PlayBackgroundSound(Sounds.SelectItem)
                                Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                                'Set the focus back
                                LastFocusedApp.Focus()
                            Else
                                MsgBox(Canvas.GetLeft(RightMenu).ToString())
                            End If
                        End If
                    End If

                Else
                    'While a process is running
                    Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                    Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                    If MainGamepadButton_Back_Button_Pressed AndAlso MainGamepadButton_Start_Button_Pressed Then

                        If Not String.IsNullOrEmpty(StartedGameExecutable) AndAlso ActiveProcess IsNot Nothing Then
                            'Suspend the running game process
                            Try
                                PauseProcessThreads(ActiveProcess.StartInfo.FileName)
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
                                ShowProcess(ActiveProcess.StartInfo.FileName)
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

#Region "Button Focus Changes"

    Private Sub SettingButton1_GotFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton1.GotFocus
        SettingButton1.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton1_LostFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton1.LostFocus
        SettingButton1.BorderBrush = Nothing
    End Sub

    Private Sub SettingButton2_GotFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton2.GotFocus
        SettingButton2.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton2_LostFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton2.LostFocus
        SettingButton2.BorderBrush = Nothing
    End Sub

    Private Sub SettingButton3_GotFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton3.GotFocus
        SettingButton3.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton3_LostFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton3.LostFocus
        SettingButton3.BorderBrush = Nothing
    End Sub

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

    Private Sub DelayTimer_Tick(sender As Object, e As EventArgs) Handles DelayTimer.Tick
        DelayTimer.Stop()

        'Add folders from \System\Folders.json
        If File.Exists(AppLibraryPath) Then
            Dim FoldersListJSON As String = File.ReadAllText(FoldersPath)
            Dim FoldersList As OrbisFolders = JsonConvert.DeserializeObject(Of OrbisFolders)(FoldersListJSON)
            If FoldersList IsNot Nothing Then
                For Each Folder In FoldersList.Folders
                    AddNewApp(Folder.Name, "", True, Folder.Name)
                Next
            End If
        End If

        'Add default and custom applications from \Apps\AppsList.json
        If File.Exists(AppLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            If AppsList IsNot Nothing Then
                For Each RegisteredApp In AppsList.Apps()
                    'Internal applications
                    If String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                        If App1.Tag Is Nothing Then
                            App1.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                            AppStartLabel.Text = "Start"
                        ElseIf App2.Tag Is Nothing Then
                            App2.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        ElseIf App3.Tag Is Nothing Then
                            App3.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        ElseIf App4.Tag Is Nothing Then
                            App4.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        ElseIf App5.Tag Is Nothing Then
                            App5.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        ElseIf App6.Tag Is Nothing Then
                            App6.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        ElseIf App7.Tag Is Nothing Then
                            App7.Tag = New AppDetails() With {.AppTitle = RegisteredApp.Name, .AppButtonExecutionString = "Start", .FolderName = RegisteredApp.Group, .AppBackgroundPath = RegisteredApp.BackgroundPath, .AppIconPath = RegisteredApp.IconPath}
                        End If
                    Else
                        'Custom applications
                        AddNewApp(RegisteredApp.Name, RegisteredApp.ExecutablePath, BackgroundPath:=RegisteredApp.BackgroundPath, IconPath:=RegisteredApp.IconPath, Platform:=RegisteredApp.Platform)
                    End If
                Next
            End If
        End If

        'Add games from \Games\GamesList.json
        If File.Exists(GameLibraryPath) Then
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
            If GamesList IsNot Nothing Then
                For Each RegisteredGame In GamesList.Games()
                    Select Case RegisteredGame.Platform
                        Case "PC", "PS1", "PS2", "PS4"
                            AddNewApp(RegisteredGame.Name, RegisteredGame.ExecutablePath, BackgroundPath:=RegisteredGame.BackgroundPath, IconPath:=RegisteredGame.IconPath, Platform:=RegisteredGame.Platform)
                        Case "PS3"
                            Dim PS3GameFolderName As String = Directory.GetParent(RegisteredGame.ExecutablePath).Parent.Parent.Name
                            AddNewApp(PS3GameFolderName, RegisteredGame.ExecutablePath, BackgroundPath:=RegisteredGame.BackgroundPath, IconPath:=RegisteredGame.IconPath, Platform:=RegisteredGame.Platform)
                    End Select
                Next
            End If
        End If

        'Accept input from this point
        DidAnimate = True
        PauseInput = False
        NotificationBannerTextBlock.Dispatcher.Invoke(Sub() NotificationBannerTextBlock.Text = "OrbisPro BETA")
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

    Private Sub ChangeBackgroundImage(AppFilePath As String, Optional BackgroundPath As String = "")
        If MainConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation") = "true" Then
            'Change background animation
            If Not String.IsNullOrEmpty(BackgroundPath) Then
                Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                Dispatcher.BeginInvoke(Sub()
                                           Dim TempBitmapImage = New BitmapImage()
                                           TempBitmapImage.BeginInit()
                                           TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
                                           TempBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache
                                           TempBitmapImage.UriSource = New Uri(BackgroundPath, UriKind.RelativeOrAbsolute)
                                           TempBitmapImage.EndInit()
                                           BackgroundImage.Source = TempBitmapImage
                                       End Sub)

                Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
            Else
                If Path.GetExtension(AppFilePath) = ".exe" Then
                    If Not String.IsNullOrEmpty(GameStarter.CheckForExistingBackgroundAsset(AppFilePath)) Then
                        Animate(BackgroundMedia, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                        Dispatcher.BeginInvoke(Sub()
                                                   Dim TempBitmapImage = New BitmapImage()
                                                   TempBitmapImage.BeginInit()
                                                   TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
                                                   TempBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache
                                                   TempBitmapImage.UriSource = New Uri(GameStarter.CheckForExistingBackgroundAsset(AppFilePath), UriKind.RelativeOrAbsolute)
                                                   TempBitmapImage.EndInit()
                                                   BackgroundImage.Source = TempBitmapImage
                                               End Sub)

                        Animate(BackgroundImage, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                    Else
                        BackgroundImage.Source = Nothing
                        Animate(BackgroundImage, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
                        Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                    End If
                Else
                    If BackgroundMedia.Opacity = 0 Then
                        BackgroundImage.Source = Nothing
                        Animate(BackgroundMedia, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
                    End If
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
                                                               .AppButtonExecutionString = "Start Disc",
                                                               .AppExecutableFilePath = DiscDriveName}))
                Else
                    OrbisNotifications.NotificationPopup(MainCanvas, GameDatabaseReturnedTitle, "PS1 CD-ROM is now ready.", "/Icons/Media-PS1-icon.png")
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                               .AppIconPath = "/Icons/Media-PS1-icon.png",
                                                               .AppButtonExecutionString = "Start Disc",
                                                               .AppExecutableFilePath = DiscDriveName}))
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
                    Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle, .AppIconPath = GameDiscImage.UriSource.AbsoluteUri, .AppButtonExecutionString = "Start"}))
                Else
                    'If it doesn't exists, add the previous found cover, if this one is also not present then add a default disc image
                    If CoverImgSrc IsNot Nothing Then
                        Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                                   .AppIconPath = CoverImgSrc.UriSource.AbsoluteUri,
                                                                   .AppButtonExecutionString = "Start",
                                                                   .AppExecutableFilePath = DiscDriveName}))
                    Else
                        Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = GameDatabaseReturnedTitle,
                                                                   .AppIconPath = "/Icons/Media-DVD-icon.png",
                                                                   .AppButtonExecutionString = "Start",
                                                                   .AppExecutableFilePath = DiscDriveName}))
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

    Private Sub ShowSideSettingButtons(Count As Integer)
        For i = 1 To Count
            Dim SettingsButton As Controls.Button = CType(MainCanvas.FindName("SettingButton" + i.ToString()), Controls.Button)
            EnsureHighestZIndex(SettingsButton)
            Animate(SettingsButton, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
        Next
    End Sub

    Private Sub EnsureHighestZIndex(AnimatedElement As UIElement)
        Dim maxZIndex As Integer = 0

        For Each child As UIElement In MainCanvas.Children
            Dim currentZIndex As Integer = Canvas.GetZIndex(child)
            If currentZIndex > maxZIndex Then
                maxZIndex = currentZIndex
            End If
        Next

        Canvas.SetZIndex(AnimatedElement, maxZIndex + 1)
    End Sub

End Class
