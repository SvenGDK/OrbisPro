Imports System.Management
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports System.Windows.Forms
Imports System.Text.RegularExpressions
Imports OrbisPro.OrbisAnimations
Imports XInput.Wrapper
Imports System.IO

Class MainWindow

    'Frequently used animations
    ReadOnly NotificationBannerAnimation As New DoubleAnimation With {.RepeatBehavior = RepeatBehavior.Forever, .From = 325, .To = 130, .Duration = New Duration(TimeSpan.FromSeconds(7)), .AutoReverse = True}
    ReadOnly SelectedBoxAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1)), .AutoReverse = True}
    Dim WithEvents GameStartAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(2))}

    'Used to show the current time and to hook the 'Home' button
    Private WithEvents SystemTimer As DispatcherTimer
    Dim WithEvents GlobalKeyboardHook As New KeyboardHook

    'Our background webbrowser to retrieve some game infos (covers, cd image, descr...)
    Private WithEvents PSXDatacenterBrowser As New WebBrowser()
    Public Shared GameDatabaseReturnedTitle As String
    Public Shared SecondWebSearch As Boolean = False

    'Used to keep track of some variables
    'Public FocusedItem As IInputElement 
    Public Shared CurrentMenu As String
    Public Shared LastSelectedApp As String

    'Disc variables
    Public Shared IsDiscInserted As Boolean = False
    Public Shared DiscDriveName As String
    Public Shared DiscLabel As String
    Public Shared DiscContentType As String
    Public Shared DiscGameID As String = ""

    'Controller input
    Dim CurrentController As X.Gamepad

    'This structure holds informations about an application displayed on the main menu
    Public Structure AppDetails
        Private _AppTitle As String
        Private _AppFolder As String
        Private _AppExecutable As String
        Private _AppIconPath As String
        Private _AppBackgroundPath As String
        Private _AppPath As String

        Public Property AppTitle As String
            Get
                Return _AppTitle
            End Get
            Set
                _AppTitle = Value
            End Set
        End Property

        Public Property AppFolder As String
            Get
                Return _AppFolder
            End Get
            Set
                _AppFolder = Value
            End Set
        End Property

        Public Property AppExecutable As String
            Get
                Return _AppExecutable
            End Get
            Set
                _AppExecutable = Value
            End Set
        End Property

        Public Property AppIconPath As String
            Get
                Return _AppIconPath
            End Get
            Set
                _AppIconPath = Value
            End Set
        End Property

        Public Property AppBackgroundPath As String
            Get
                Return _AppBackgroundPath
            End Get
            Set
                _AppBackgroundPath = Value
            End Set
        End Property

        Public Property AppPath As String
            Get
                Return _AppPath
            End Get
            Set
                _AppPath = Value
            End Set
        End Property
    End Structure

#Region "Event Watchers"
    'To detect usb devices and discs changes we add 2 ManagementEventWatchers (probably needs a replacement)
    Private WithEvents DevicesEventWatcher As ManagementEventWatcher
    Private DevicesEventQuery As WqlEventQuery

    Private WithEvents CDVDEventWatcher As ManagementEventWatcher
    Private CDVDEventQuery As WqlEventQuery

    Private Sub DevicesEventWatcher_EventArrived(sender As Object, e As EventArrivedEventArgs) Handles DevicesEventWatcher.EventArrived
        Using moBase = CType(e.NewEvent.Properties("TargetInstance").Value, ManagementBaseObject)
            'Dim InterfaceType As String = moBase.Properties("InterfaceType").Value.ToString() 'Currently unused
            Dim DeviceCaption As String = moBase.Properties("Caption").Value.ToString() 'Given name of the connected USB device

            Select Case e.NewEvent.ClassPath.ClassName
                Case "__InstanceDeletionEvent"
                    'The USB device got removed
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, DeviceCaption, "Has been removed.", "/Icons/usb-icon.png"))
                Case "__InstanceCreationEvent"
                    'A new USB device has been detected
                    Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, DeviceCaption, "Drive can now be used.", "/Icons/usb-icon.png"))
            End Select
        End Using
    End Sub

    Private Sub CDVDEventWatcher_EventArrived(sender As Object, e As EventArrivedEventArgs) Handles CDVDEventWatcher.EventArrived
        Using moBase = CType(e.NewEvent.Properties("TargetInstance").Value, ManagementBaseObject)
            Dim CDVDDrive As String = moBase.Properties("Drive").Value.ToString()
            Dim CDVDMediaInserted As Boolean = CBool(moBase.Properties("MediaLoaded").Value)

            Select Case e.NewEvent.ClassPath.ClassName
                Case "__InstanceModificationEvent"
                    If CDVDMediaInserted = True Then
                        Dim CDVDVolumeLabel As String

                        'Get the volume name of the disc otherwise return the disc's drive letter
                        If moBase.Properties("VolumeName").Value IsNot Nothing Then
                            CDVDVolumeLabel = moBase.Properties("VolumeName").Value.ToString()
                        Else
                            CDVDVolumeLabel = CDVDDrive
                        End If

                        'Check the content of the disc with OrbisCDVDManager to return a better presentation of the disc on the main menu and allow further actions
                        If OrbisCDVDManager.CheckCDVDContent(CDVDDrive).Platform = "PS2" Then
                            'Handle PS2 disc
                            DiscContentType = "PS2"
                            DiscDriveName = CDVDDrive
                            DiscGameID = OrbisCDVDManager.CheckCDVDContent(CDVDDrive).GameID
                            Dispatcher.BeginInvoke(Sub() PSXDatacenterBrowser.Navigate("https://psxdatacenter.com/psx2/games2/" + DiscGameID + ".html"))
                        ElseIf OrbisCDVDManager.CheckCDVDContent(CDVDDrive).Platform = "PS1" Then
                            'Handle PS1 disc
                            DiscContentType = "PS1"
                            DiscDriveName = CDVDDrive
                            DiscGameID = OrbisCDVDManager.CheckCDVDContent(CDVDDrive).GameID.ToUpper
                            Dispatcher.BeginInvoke(Sub() PSXDatacenterBrowser.Navigate("https://psxdatacenter.com/plist.html"))
                        ElseIf OrbisCDVDManager.CheckCDVDContent(CDVDDrive).Platform = "PCE" Then
                            'Handle PC-Engine disc
                            DiscContentType = "PCE"
                            DiscDriveName = CDVDDrive

                            'The game id is a bit harder to retrieve and we got almost no infos about the disc ... it's compatible anyway so add it as default disc on the main menu
                            'DiscContentType is the var here that chooses the right emulator afterwards
                            Dispatcher.BeginInvoke(Sub() OrbisNotifications.NotificationPopup(OrbisGrid, CDVDVolumeLabel, "PC-Engine CD-ROM ready.", "/Icons/Media-CD-icon.png"))
                            Dispatcher.BeginInvoke(Sub() AddDiscToHome(New AppDetails() With {.AppTitle = CDVDVolumeLabel,
                                                                       .AppIconPath = "/Icons/Media-CD-icon.png",
                                                                       .AppExecutable = "Start Disc",
                                                                       .AppPath = DiscDriveName}))
                        End If
                    Else
                        Dispatcher.BeginInvoke(Sub() RemoveDiscFromHome())
                    End If
            End Select
        End Using
    End Sub
#End Region

#Region "Animations"

    'The animation when entering the main menu
    Private Sub HomeAnimation()
        Dim AnimDuration = New Duration(TimeSpan.FromMilliseconds(500))

        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Start)

        Animate(App1, Canvas.LeftProperty, 461, 288, AnimDuration)
        Animate(App1, WidthProperty, 165, 330, AnimDuration)
        Animate(App1, HeightProperty, 165, 330, AnimDuration)

        Animate(App2, Canvas.LeftProperty, 631, 632, AnimDuration)
        Animate(App2, WidthProperty, 150, 200, AnimDuration)
        Animate(App2, HeightProperty, 150, 200, AnimDuration)

        Animate(App3, Canvas.LeftProperty, 786, 836, AnimDuration)
        Animate(App3, WidthProperty, 150, 200, AnimDuration)
        Animate(App3, HeightProperty, 150, 200, AnimDuration)

        Animate(App4, Canvas.LeftProperty, 941, 1040, AnimDuration)
        Animate(App4, WidthProperty, 150, 200, AnimDuration)
        Animate(App4, HeightProperty, 150, 200, AnimDuration)

        Animate(App5, Canvas.LeftProperty, 1096, 1244, AnimDuration)
        Animate(App5, WidthProperty, 150, 200, AnimDuration)
        Animate(App5, HeightProperty, 150, 200, AnimDuration)

        Animate(App6, Canvas.LeftProperty, 1251, 1448, AnimDuration)
        Animate(App6, WidthProperty, 150, 200, AnimDuration)
        Animate(App6, HeightProperty, 150, 200, AnimDuration)

        Animate(App7, Canvas.LeftProperty, 1406, 1652, AnimDuration)
        Animate(App7, WidthProperty, 150, 200, AnimDuration)
        Animate(App7, HeightProperty, 150, 200, AnimDuration)

        Animate(App8, Canvas.LeftProperty, 1561, 1856, AnimDuration)
        Animate(App8, WidthProperty, 150, 200, AnimDuration)
        Animate(App8, HeightProperty, 150, 200, AnimDuration)

        Animate(SelectedAppBorder, Canvas.LeftProperty, 456, 283, AnimDuration)
        Animate(SelectedAppBorder, WidthProperty, 175, 340, AnimDuration)
        Animate(SelectedAppBorder, HeightProperty, 175, 410, AnimDuration)

        AppTitle.Visibility = Visibility.Visible
        AppStartLabel.Visibility = Visibility.Visible
    End Sub

    'The animation when returning to the main menu
    Public Sub ReturnAnimation()

        Dim SelectedApp As Image = TryCast(FocusManager.GetFocusedElement(Me), Image)

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

    End Sub

    'Animation when starting a game
    Private Sub StartGameAnimation(SelectedApp As Image)

        'Get game infos
        Dim GameInfos As AppDetails = CType(SelectedApp.Tag, AppDetails)

        'Durations for the animations
        Dim PositionDuration = New Duration(TimeSpan.FromSeconds(1))
        Dim LongShowHideDuration = New Duration(TimeSpan.FromSeconds(2))

        'Play 'start' sound effect
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

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
        GameStarter.StartGame(GameInfos.AppPath)

    End Sub

    'Currently unused
    'Private Sub StartAppAnimation(SelectedApp As Image)

    '    Dim PositionDuration = New Duration(TimeSpan.FromSeconds(3))
    '    Dim ShowHideDuration = New Duration(TimeSpan.FromSeconds(2))
    '    Dim SelectedAppInfos = CType(SelectedApp.Tag, AppDetails)

    '    'Top elements
    '    Animate(PlusBanner, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(NotificationsBannerB, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(NotificationsBanner, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(FriendsBanner, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(OnlineBanner, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(UsernameLabel, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(TrophyBanner, Canvas.TopProperty, 78, -78, PositionDuration)
    '    Animate(SystemClock, Canvas.TopProperty, 78, -78, PositionDuration)

    '    For Each App In OrbisGrid.Children
    '        If TypeOf App Is Image Then
    '            Dim AppImage As Image = CType(App, Image)
    '            If AppImage.Name.StartsWith("App") And AppImage.Name IsNot SelectedApp.Name Then
    '                Animate(AppImage, OpacityProperty, 1, 0, PositionDuration)
    '                Animate(AppImage, WidthProperty, 200, 330, PositionDuration)
    '                Animate(AppImage, HeightProperty, 200, 330, PositionDuration)
    '            ElseIf App.Name = "DiscApp" Then

    '            ElseIf AppImage.Name Is SelectedApp.Name Then
    '                Animate(AppImage, OpacityProperty, 1, 0, ShowHideDuration)
    '                Animate(AppImage, WidthProperty, 200, 660, ShowHideDuration)
    '                Animate(AppImage, HeightProperty, 200, 660, ShowHideDuration)
    '            End If
    '        End If
    '    Next

    '    For Each App In OrbisGrid.Children
    '        If TypeOf App Is Image And App.Name.StartsWith("App") Then
    '            If App.Name IsNot SelectedApp.Name Then
    '                Animate(App, OpacityProperty, 1, 0, PositionDuration)
    '                Animate(App, WidthProperty, 200, 330, PositionDuration)
    '                Animate(App, HeightProperty, 200, 330, PositionDuration)
    '            Else
    '                Animate(App, OpacityProperty, 1, 0, ShowHideDuration)
    '                Animate(App, WidthProperty, 200, 660, ShowHideDuration)
    '                Animate(App, HeightProperty, 200, 660, ShowHideDuration)
    '            End If
    '        ElseIf TypeOf App Is Image And App.Name = "DiscApp" Then
    '            If IsDiscInserted Then
    '                If App.Name IsNot SelectedApp.Name Then
    '                    Animate(App, OpacityProperty, 1, 0, PositionDuration)
    '                    Animate(App, WidthProperty, 200, 330, PositionDuration)
    '                    Animate(App, HeightProperty, 200, 330, PositionDuration)
    '                Else
    '                    Animate(App, OpacityProperty, 1, 0, ShowHideDuration)
    '                    Animate(App, WidthProperty, 200, 660, ShowHideDuration)
    '                    Animate(App, HeightProperty, 200, 660, ShowHideDuration)
    '                End If
    '            End If
    '        End If
    '    Next

    '    'Background elements - hide main bg and show game bg
    '    Animate(BackgroundMedia, OpacityProperty, 1, 0, ShowHideDuration)
    '    Animate(OrbisGrid, OpacityProperty, 1, 0, ShowHideDuration)

    '    Animate(AppTitle, OpacityProperty, 1, 0, ShowHideDuration)
    '    Animate(AppStartLabel, OpacityProperty, 1, 0, ShowHideDuration)

    '    Animate(SelectedAppBorder, OpacityProperty, 1, 0, PositionDuration)
    '    SelectedAppBorder.Visibility = Visibility.Hidden

    '    Hide()

    'End Sub


    'Animation when starting an application
    Private Sub SimpleAppStartAnimation(SelectedApp As Image)

        'Get the details about the selected application
        Dim SelectedAppInfos As AppDetails = CType(SelectedApp.Tag, AppDetails)

        'Play 'start' sound effect
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

        'Show the new window
        Select Case SelectedAppInfos.AppTitle
            Case "Twitter"
                Dim NewWebBrowser As New SystemWebBrowser() With {.Top = Top, .Left = Left, .ShowActivated = True}
                Animate(NewWebBrowser, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                NewWebBrowser.Show()
                X.StopPolling()
                CurrentController = Nothing
            Case "Browser"
                Dim NewWebBrowser As New SystemWebBrowser() With {.Top = Top, .Left = Left, .ShowActivated = True}
                Animate(NewWebBrowser, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                NewWebBrowser.Show()
                X.StopPolling()
                CurrentController = Nothing
            Case "File Browser"
                Dim NewExplorerWindow As New FileExplorer() With {.Top = Top, .Left = Left, .ShowActivated = True}
                Animate(NewExplorerWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                NewExplorerWindow.Show()
                X.StopPolling()
                CurrentController = Nothing
            Case "Library"
                Dim NewGameLibraryWindow As New GameLibrary() With {.Top = Top, .Left = Left, .ShowActivated = True}
                Animate(NewGameLibraryWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                NewGameLibraryWindow.Show()
                X.StopPolling()
                CurrentController = Nothing
                NewGameLibraryWindow.GameLibraryButton.Focus()
            Case "Settings"
                Dim NewSettingsWindow As New GeneralSettings() With {.Top = Top, .Left = Left, .ShowActivated = True}
                Animate(NewSettingsWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                X.StopPolling()
                CurrentController = Nothing
                NewSettingsWindow.Show()
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
            If TypeOf App Is Image And App.Name.StartsWith("App") Then

                'Exlude App1 (the DiscApp will always be shown after App1 atm)
                If Not App.Name = "App1" Then
                    Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) + 205, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If
        Next

        'Place the DiscApp on the main menu
        If Not FocusedImage.Name = "App1" Then
            Animate(DiscApp, Canvas.LeftProperty, 461, App1Position + 205, New Duration(TimeSpan.FromMilliseconds(100)))
        Else
            'Needs a different Left position because the size of App1 is bigger
            Animate(DiscApp, Canvas.LeftProperty, 461, App1Position + 345, New Duration(TimeSpan.FromMilliseconds(100)))
        End If

        Animate(DiscApp, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(100)))

        If Not FocusedImage.Name = "App1" Then
            'Re-position the selection border & labels
            Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) + 205, New Duration(TimeSpan.FromMilliseconds(100)))
            Animate(AppTitle, Canvas.LeftProperty, Canvas.GetLeft(AppTitle), Canvas.GetLeft(AppTitle) + 205, New Duration(TimeSpan.FromMilliseconds(100)))
            Animate(AppStartLabel, Canvas.LeftProperty, Canvas.GetLeft(AppStartLabel), Canvas.GetLeft(AppStartLabel) + 205, New Duration(TimeSpan.FromMilliseconds(100)))
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
            If TypeOf App Is Image And App.Name.StartsWith("App") Then

                'Exlude App1
                If Not App.Name = "App1" Then
                    Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) - 205, AnimDuration)
                End If

            End If
        Next

        'Set the App1 size back if DiscApp was selected
        If FocusedImage.Name = "DiscApp" Then
            Animate(App1, WidthProperty, 200, 330, AnimDuration)
            Animate(App1, HeightProperty, 200, 330, AnimDuration)

            AppTitle.Content = CType(App1.Tag, AppDetails).AppTitle
            AppStartLabel.Content = CType(App1.Tag, AppDetails).AppExecutable
            App1.Focus()

            'Move every app back to the origin position 
            For Each App In OrbisGrid.Children
                If TypeOf App Is Image And App.Name.StartsWith("App") Then
                    Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) + 205, AnimDuration)
                End If
            Next
        End If

        If Not FocusedImage.Name = "App1" And Not FocusedImage.Name = "DiscApp" Then
            'Re-position the selection border & labels
            Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) - 205, AnimDuration)
            Animate(AppTitle, Canvas.LeftProperty, Canvas.GetLeft(AppTitle), Canvas.GetLeft(AppTitle) - 205, AnimDuration)
            Animate(AppStartLabel, Canvas.LeftProperty, Canvas.GetLeft(AppStartLabel), Canvas.GetLeft(AppStartLabel) - 205, AnimDuration)
        ElseIf Not FocusedImage.Name = "App1" And FocusedImage.Name = "DiscApp" Then
            'Move every app from the right back to the left
            For Each App In OrbisGrid.Children
                If TypeOf App Is Image And App.Name.StartsWith("App") Then

                    'Exlude App1
                    If Not App.Name = "App1" Then
                        Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App) - 205, Canvas.GetLeft(App), AnimDuration)
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

#End Region

#Region "UI Browsing"

    'Return Int contained in a String using Regex
    Private Shared Function GetIntOnly(value As String) As Integer
        Dim returnVal As String = String.Empty
        Dim collection As MatchCollection = Regex.Matches(value, "\d+")
        For Each m As Match In collection
            returnVal += m.ToString()
        Next
        Return Convert.ToInt32(returnVal)
    End Function

    Private Sub MoveAppsRight(SelectedApp As Image, NextApp As Image)

        'Set the animation duration (100ms) and sound
        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(100))
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

        'This loop goes trough every application presented on the main menu and animates the location of each one including the 'DiscApp' if presented (maybe this could be a bug).
        For Each App In OrbisGrid.Children 'Loop trough the UIElementCollection
            If TypeOf App Is Image And App.Name.StartsWith("App") Then 'An application is presented as 'Image' and starts with the name 'App' so we can "filter" here with 'If'
                If App.Name IsNot SelectedApp.Name And App.Name IsNot NextApp.Name Then 'The selected and next-selected application have a different animation - here we ONLY animate all OTHER applications to move right
                    Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) - 205, MoveDuration)
                End If
            ElseIf TypeOf App Is Image And App.Name = "DiscApp" Then 'Don't forget to move the 'DiscApp'
                If IsDiscInserted Then 'This could be a bug (checking this later) because the 'DiscApp' actually only moves when a disc is inserted
                    If App.Name IsNot SelectedApp.Name And App.Name IsNot NextApp.Name Then
                        Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) - 205, MoveDuration)
                    End If
                End If
            End If
        Next

        'The next application's animation
        Animate(NextApp, Canvas.LeftProperty, Canvas.GetLeft(NextApp), Canvas.GetLeft(NextApp) - 344, MoveDuration)
        Animate(NextApp, WidthProperty, 200, 330, MoveDuration)
        Animate(NextApp, HeightProperty, 200, 330, MoveDuration)

        'The selected application's animation
        Animate(SelectedApp, Canvas.LeftProperty, Canvas.GetLeft(SelectedApp), Canvas.GetLeft(SelectedApp) - 208, MoveDuration)
        Animate(SelectedApp, WidthProperty, 330, 200, MoveDuration)
        Animate(SelectedApp, HeightProperty, 330, 200, MoveDuration)

        'Selection border animation
        Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) + 60, MoveDuration, True)
        Animate(SelectedAppBorder, HeightProperty, 410, 200, MoveDuration, True)

        'Set the selected application title and start label shown on the main menu and focus the app
        AppTitle.Content = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Content = CType(NextApp.Tag, AppDetails).AppExecutable
        NextApp.Focus()
    End Sub

    'Same as before, just the other way
    Private Sub MoveAppsLeft(SelectedApp As Image, NextApp As Image)

        Dim MoveDuration = New Duration(TimeSpan.FromMilliseconds(100))
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

        For Each App In OrbisGrid.Children
            If TypeOf App Is Image And App.Name.StartsWith("App") Then
                If App.Name IsNot SelectedApp.Name And App.Name IsNot NextApp.Name Then
                    Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) + 205, MoveDuration)
                End If
            ElseIf TypeOf App Is Image And App.Name = "DiscApp" Then
                If IsDiscInserted Then
                    If App.Name IsNot SelectedApp.Name And App.Name IsNot NextApp.Name Then
                        Animate(App, Canvas.LeftProperty, Canvas.GetLeft(App), Canvas.GetLeft(App) + 205, MoveDuration)
                    End If
                End If
            End If
        Next

        Animate(NextApp, Canvas.LeftProperty, Canvas.GetLeft(NextApp), Canvas.GetLeft(NextApp) + 208, MoveDuration)
        Animate(NextApp, WidthProperty, 200, 330, MoveDuration)
        Animate(NextApp, HeightProperty, 200, 330, MoveDuration)

        Animate(SelectedApp, Canvas.LeftProperty, Canvas.GetLeft(SelectedApp), Canvas.GetLeft(SelectedApp) + 344, MoveDuration)
        Animate(SelectedApp, WidthProperty, 330, 200, MoveDuration)
        Animate(SelectedApp, HeightProperty, 330, 200, MoveDuration)

        Animate(SelectedAppBorder, Canvas.LeftProperty, Canvas.GetLeft(SelectedAppBorder), Canvas.GetLeft(SelectedAppBorder) - 130, MoveDuration, True)
        Animate(SelectedAppBorder, HeightProperty, 410, 200, MoveDuration, True)

        AppTitle.Content = CType(NextApp.Tag, AppDetails).AppTitle
        AppStartLabel.Content = CType(NextApp.Tag, AppDetails).AppExecutable
        NextApp.Focus()
    End Sub

#End Region

#Region "Keyboard and Controller Events"

    'Get keyboard user input
    Private Sub MainWindow_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyDown

        'Get the focused element to select different actions
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If e.Key = Key.Q Then
            'Used for testing purposes -> WebBrowserDocumentCompleted
            'PSXDatacenterBrowser.Navigate("https://psxdatacenter.com/psx2/games2/SLES-53702.html")
        ElseIf e.Key = Key.W Then

            'This is the execute key
            Dim FocusedApp As Image = TryCast(FocusedItem, Image)
            Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppPath

            If FocusedAppPath Is Nothing Then 'Internal apps don't have a path
                SimpleAppStartAnimation(FocusedApp)
            Else
                StartGameAnimation(FocusedApp)
            End If

        ElseIf e.Key = Key.Right Then

            'Move applications on the main menu visually to the left
            If TypeOf FocusedItem Is Image Then
                Dim SelectedApp As Image = TryCast(FocusManager.GetFocusedElement(Me), Image) 'Get the selected app by the FocusedElement
                Dim NextAppImage As Image

                'Before we animate anything we have to set the NextAppImage
                If App1.IsFocused And IsDiscInserted Then 'The 'DiscApp' is (atm) always placed after 'App1', so if a disc is inserted the next app will be the 'DiscApp' IF App1 is currently focused
                    NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                ElseIf DiscApp.IsFocused Then
                    NextAppImage = CType(OrbisGrid.FindName("App2"), Image) 'If the 'DiscApp' is selected we know that 'App2' will be the next app
                Else
                    'If we don't know what app will be the next one then we get the currently selected app's number and
                    'do +1 (ex. selected app: App4 gets App5, this will be the next application)
                    Dim SelectedAppNumber As Integer = GetIntOnly(SelectedApp.Name) + 1
                    NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image) 'Find the next application and set 'NextAppImage' so we can control it
                End If

                MoveAppsRight(SelectedApp, NextAppImage)
            End If

        ElseIf e.Key = Key.Left Then

            'Move applications on the main menu visually to the right
            If TypeOf FocusedItem Is Image Then
                Dim SelectedApp As Image = TryCast(FocusManager.GetFocusedElement(Me), Image)
                Dim NextAppImage As Image

                If App2.IsFocused And IsDiscInserted Then
                    NextAppImage = OrbisGrid.FindName("DiscApp")
                ElseIf DiscApp.IsFocused Then
                    NextAppImage = OrbisGrid.FindName("App1")
                Else
                    Dim SelectedAppNumber As Integer = GetIntOnly(SelectedApp.Name) - 1
                    NextAppImage = OrbisGrid.FindName("App" + SelectedAppNumber.ToString)
                End If

                MoveAppsLeft(SelectedApp, NextAppImage)
            End If

        ElseIf e.Key = Key.K Then
            'Used for testing purposes -> AddNewApp
            'AddNewApp("Test App", "Preview")
        ElseIf e.Key = Key.D Then
            'Used for testing purposes
            'If IsDiscInserted Then
            '    RemoveDiscFromHome()
            'Else
            '    AddDiscToHome(New AppDetails() With {.AppTitle = "Test Disc", .AppIconPath = "/Icons/Media-DVD-icon.png", .AppExecutable = "Start Disc"})
            'End If
        End If
    End Sub

    'Get connected controllers
    Private Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            AddHandler CurrentController.StateChanged, AddressOf CurrentController_StateChanged 'Add the handler
            X.UpdatesPerSecond = 12 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    'Get controller input In the background
    'Xbox 360 controller emulation

#End Region

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        'Get controller if attached
        GetAttachedControllers()

        OrbisGrid.Background = New SolidColorBrush(Colors.Black)

        'USB Event Watcher
        DevicesEventQuery = New WqlEventQuery() With {.EventClassName = "__InstanceOperationEvent", .WithinInterval = New TimeSpan(0, 0, 1), .Condition = "TargetInstance ISA 'Win32_DiskDrive'"}
        DevicesEventWatcher = New ManagementEventWatcher(DevicesEventQuery)
        DevicesEventWatcher.Start()

        'CDVD Event Watcher
        CDVDEventQuery = New WqlEventQuery() With {.EventClassName = "__InstanceOperationEvent", .WithinInterval = New TimeSpan(0, 0, 3), .Condition = "TargetInstance ISA 'Win32_CDROMDrive'"}
        CDVDEventWatcher = New ManagementEventWatcher(CDVDEventQuery)
        CDVDEventWatcher.Start()

        'Version Banner
        NotificationsBanner.BeginAnimation(Canvas.LeftProperty, NotificationBannerAnimation)

        'Clock
        SystemClock.Content = Date.Now.ToString("HH:mm")
        SystemTimer = New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 5)}
        SystemTimer.Start()

        'Check if any apps are enabled/installed
        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Apps.ini") Then
            'Load internal and custom applications (these are currently fixed and should not be changed atm)
            For Each App In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Apps.ini")
                If App.Contains("App") Then
                    If App1.Tag Is Nothing Then
                        App1.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                        AppTitle.Content = App.Split("="c)(1).Split(";"c)(0) 'Set the title on the screen
                    ElseIf App2.Tag Is Nothing Then
                        App2.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App3.Tag Is Nothing Then
                        App3.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App4.Tag Is Nothing Then
                        App4.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App5.Tag Is Nothing Then
                        App5.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App6.Tag Is Nothing Then
                        App6.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App7.Tag Is Nothing Then
                        App7.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    ElseIf App8.Tag Is Nothing Then
                        App8.Tag = New AppDetails() With {.AppTitle = App.Split("="c)(1).Split(";"c)(0), .AppExecutable = "Start"}
                    End If
                End If
            Next
        End If

        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini") Then

            'Load installed games in OrbisPro
            For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")
                If Game.Contains("PS1Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.Contains("PS2Game") Then
                    AddNewApp(Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)), Game.Split("="c)(1).Split(";"c)(0))
                ElseIf Game.Contains("PS3Game") Then
                    'For PS3 games show the folder name
                    Dim PS3GameFolderName = Directory.GetParent(Game.Split("="c)(1).Split(";"c)(0))
                    AddNewApp(PS3GameFolderName.Parent.Parent.Name, Game.Split("="c)(1).Split(";"c)(0))
                End If
            Next

        End If

        HomeAnimation()

        App1.Focus()
        CurrentMenu = "AppMenu"

    End Sub

    Private Sub PSXDatacenterBrowser_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) Handles PSXDatacenterBrowser.DocumentCompleted

        Dim CoverImgSrc As BitmapImage

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
                Else
                    CoverImgSrc = Nothing
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
                Else
                    CoverImgSrc = Nothing
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
        SystemClock.Content = Date.Now.ToString("HH:mm")

        'Border Glowing
        SelectedAppBorder.BeginAnimation(OpacityProperty, SelectedBoxAnimation)
    End Sub

    Public Sub AddNewApp(AppTitle As String, AppFolderOrFile As String)

        'Get the count of displayed apps
        Dim InstalledAppsOnMenu As Integer = 0
        For Each AppImage In OrbisGrid.Children
            If TypeOf AppImage Is Image Then
                Dim Img As Image = TryCast(AppImage, Image)
                If Img.Name.StartsWith("App") Then
                    InstalledAppsOnMenu += 1
                End If
            End If
        Next

        'Declare the new app
        Dim NewAppImage As New Image With {
            .Height = 200, .Width = 200,
            .Stretch = Stretch.Uniform,
            .StretchDirection = StretchDirection.Both,
            .Name = "App" + (InstalledAppsOnMenu + 1).ToString, 'Here the new app get it's number, so the code can iterate through it
            .Focusable = True,
            .FocusVisualStyle = Nothing,
            .Source = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute)),
            .Tag = New AppDetails() With {.AppTitle = AppTitle, .AppExecutable = "Start", .AppPath = AppFolderOrFile}
        }

        'Get the last app on the main menu
        Dim LastAppInMenu As Image = CType(OrbisGrid.FindName("App" + InstalledAppsOnMenu.ToString), Image)

        'Set the position of the new app

        If LastAppInMenu.Name = "App8" Then 'Required for the first time, otherwise all custom added games will be not displayed correctly
            Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastAppInMenu) + 505)
        Else
            Canvas.SetLeft(NewAppImage, Canvas.GetLeft(LastAppInMenu) + 205)
        End If

        Canvas.SetTop(NewAppImage, Canvas.GetTop(LastAppInMenu))

        'Important, otherwise it can't find the new app
        RegisterName(NewAppImage.Name, NewAppImage)

        'Add the new app on the canvas
        OrbisGrid.Children.Add(NewAppImage)
    End Sub

    Private Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        'Add a global keyboard hook for the 'Home' key
        Try
            GlobalKeyboardHook.Attach()
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Sub GameStartAnimation_Completed(sender As Object, e As EventArgs) Handles GameStartAnimation.Completed

        Dim DiscInfos = CType(DiscApp.Tag, AppDetails)
        OrbisCDVDManager.StartCDVD(DiscContentType, DiscInfos.AppPath)

    End Sub

    Private Sub GlobalKeyboardHook_KeyDown(sender As Object, e As Winnster.Interop.LibHook.KeyPressEventArgs) Handles GlobalKeyboardHook.KeyDown
        If e.Key = VirtualKeyEnum.VK_HOME Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Options)

            'For Each Win In Windows.Application.Current.Windows()
            '    If Win.ToString = "OrbisPro.OpenWindows" Then
            '        CType(Win, OpenWindows).Activate()
            '        Exit For
            '    End If
            'Next

            Dim OpenWindowsManager As New OpenWindows() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opacity = 0}
            OpenWindowsManager.Show()
            X.StopPolling()
            Animate(OpenWindowsManager, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))
            OpenWindowsManager.Activate()
        End If

    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs)

        'Get the focused element to select different actions
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If CurrentController.Dpad_Left_up Then 'D-Pad Left
            'Move applications on the main menu to the right
            If TypeOf FocusedItem Is Image Then
                Dim SelectedApp As Image = TryCast(FocusManager.GetFocusedElement(Me), Image)
                Dim NextAppImage As Image

                If App2.IsFocused And IsDiscInserted Then
                    NextAppImage = OrbisGrid.FindName("DiscApp")
                ElseIf DiscApp.IsFocused Then
                    NextAppImage = OrbisGrid.FindName("App1")
                Else
                    Dim SelectedAppNumber As Integer = GetIntOnly(SelectedApp.Name) - 1
                    NextAppImage = OrbisGrid.FindName("App" + SelectedAppNumber.ToString)
                End If

                MoveAppsLeft(SelectedApp, NextAppImage)
            End If

        ElseIf CurrentController.Dpad_Right_up Then 'D-Pad Right

            'Move applications on the main menu to the left
            If TypeOf FocusedItem Is Image Then
                Dim SelectedApp As Image = TryCast(FocusManager.GetFocusedElement(Me), Image) 'Get the selected app by the FocusedElement
                Dim NextAppImage As Image

                'Before we animate anything we have to set the NextAppImage
                If App1.IsFocused And IsDiscInserted Then 'The 'DiscApp' is (atm) always placed after 'App1', so if a disc is inserted the next app will be the 'DiscApp' IF App1 is currently focused
                    NextAppImage = CType(OrbisGrid.FindName("DiscApp"), Image)
                ElseIf DiscApp.IsFocused Then
                    NextAppImage = CType(OrbisGrid.FindName("App2"), Image) 'If the 'DiscApp' is selected we know that 'App2' will be the next app
                Else
                    'If we don't know what app will be the next one then we get the currently selected app's number and
                    'do +1 (ex. selected app: App4 gets App5, this will be the next application)
                    Dim SelectedAppNumber As Integer = GetIntOnly(SelectedApp.Name) + 1
                    NextAppImage = CType(OrbisGrid.FindName("App" + SelectedAppNumber.ToString), Image) 'Find the next application and set 'NextAppImage' so we can control it
                End If

                MoveAppsRight(SelectedApp, NextAppImage)
            End If

        ElseIf CurrentController.A_up Then 'Cross for PS3

            If TypeOf FocusedItem Is Image Then 'If the focused item is a game or application -> start

                Dim FocusedApp As Image = TryCast(FocusedItem, Image)
                Dim FocusedAppPath = CType(FocusedApp.Tag, AppDetails).AppPath

                If FocusedAppPath Is Nothing Then 'Internal apps don't have a path
                    SimpleAppStartAnimation(FocusedApp)
                Else
                    StartGameAnimation(FocusedApp)
                End If

            End If

        End If

    End Sub


End Class
