Imports System.ComponentModel
Imports System.IO
Imports System.Text
Imports System.Threading
Imports System.Windows.Media.Animation
Imports Microsoft.Win32
Imports OrbisPro.INI
Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNetwork
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports Tomlyn
Imports Tomlyn.Model

Public Class GeneralSettings

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents NewGlobalKeyboardHook As New OrbisKeyboardHook()
    Private LastKeyboardKey As Key

#Region "Selection Index Trackers"
    Private LastGeneralSettingsIndex As Integer

    Private LastAccountManagementSettingsIndex As Integer = 0
    Private LastNetworkSettingsIndex As Integer = 0
    Private LastNotificationsSettingsIndex As Integer = 0
    Private LastSystemSettingsIndex As Integer = 0
    Private LastGeneralEmulatorSettingsIndex As Integer = 0
    Private LastGamepadSettingsIndex As Integer = 0

    Private LastAudioSettingsIndex As Integer = 0
    Private LastDisplaySettingsIndex As Integer = 0
    Private LastBackgroundSettingsIndex As Integer = 0

    Private LastPS1EmulatorSettingsIndex As Integer = 0
    Private LastPS2EmulatorSettingsIndex As Integer = 0
    Private LastPS3EmulatorSettingsIndex As Integer = 0
    Private LastPS4EmulatorSettingsIndex As Integer = 0
#End Region

    Private WithEvents NewInputBox As New PSInputBox("Enter a new value :")
    Private SettingToChange As SettingsListViewItem

    Public Opener As String = ""

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub GeneralSettings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Add an input field to the canvas
        Canvas.SetLeft(NewInputBox, 1925)
        Canvas.SetTop(NewInputBox, 1085)
        SettingsCanvas.Children.Add(NewInputBox)

        'Show the main settings first
        LoadGeneralSettings()
        GoForwardAnimation()
    End Sub

    Private Async Sub GeneralSettings_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub GeneralSettings_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub GeneralSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "BluetoothSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.BluetoothSettings" Then
                        CType(Win, BluetoothSettings).Activate()
                        CType(Win, BluetoothSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "Downloads"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.Downloads" Then
                        CType(Win, Downloads).Activate()
                        CType(Win, Downloads).PauseInput = False
                        Exit For
                    End If
                Next
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        CType(Win, FileExplorer).Activate()
                        CType(Win, FileExplorer).PauseInput = False
                        Exit For
                    End If
                Next
            Case "GameLibrary"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GameLibrary" Then
                        CType(Win, GameLibrary).Activate()
                        CType(Win, GameLibrary).PauseInput = False
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        CType(Win, MainWindow).PauseInput = False
                        Exit For
                    End If
                Next
            Case "WifiSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.WifiSettings" Then
                        CType(Win, WifiSettings).Activate()
                        CType(Win, WifiSettings).PauseInput = False
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#End Region

#Region "Main Settings"

    Public Sub LoadGeneralSettings()
        WindowTitle.Text = "Settings"

        GeneralSettingsListView.Items.Clear()

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)

        Dim AccountManagementSetting As New SettingsListViewItem With {.SettingsTitle = "User Management", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim NetworkSetting As New SettingsListViewItem With {.SettingsTitle = "Network", .SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute))}
        Dim NotificationSetting As New SettingsListViewItem With {.SettingsTitle = "Notifications", .SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_notifications.png", UriKind.RelativeOrAbsolute))}
        Dim SystemSetting As New SettingsListViewItem With {.SettingsTitle = "System", .SettingsIcon = New BitmapImage(New Uri("/Icons/InternalStorage.png", UriKind.RelativeOrAbsolute))}
        Dim EmulatorSetting As New SettingsListViewItem With {.SettingsTitle = "Emulators", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim BluetoothSetting As New SettingsListViewItem With {.SettingsTitle = "Bluetooth", .SettingsIcon = New BitmapImage(New Uri("/Icons/Bluetooth.png", UriKind.RelativeOrAbsolute))}
        Dim GamepadSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute))}

        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = AccountManagementSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = NetworkSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = NotificationSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SystemSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = EmulatorSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = BluetoothSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GamepadSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadUserManagementSettings()
        WindowTitle.Text = "User Management"

        GeneralSettingsListView.Items.Clear()

        'Declare the style
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        'Get the value
        Dim Username As String = MainConfigFile.IniReadValue("System", "Username")

        'Declare the setting
        Dim ChangeUsernameSetting As New SettingsListViewItem With {.SettingsTitle = "Username",
            .SettingsDescription = "Change your User Name.",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_friends.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = Username,
            .ConfigSectionName = "System",
            .ConfigToChange = "Username"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ChangeUsernameSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadNotificationSettings()
        WindowTitle.Text = "Notifications"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim DisableNotifications As Boolean = GetBoolValue(MainConfigFile.IniReadValue("Notifications", "DisableNotifications"))
        Dim NotificationDuration As String = MainConfigFile.IniReadValue("Notifications", "NotificationDuration")

        'Declare the settings
        Dim DisableNotificationsSetting As New SettingsListViewItem With {.SettingsTitle = "Disable Notifications",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_friends.png", UriKind.RelativeOrAbsolute)),
            .IsSettingChecked = DisableNotifications,
            .ConfigSectionName = "Notifications",
            .ConfigToChange = "DisableNotifications"}
        Dim NotificationDurationSetting As New SettingsListViewItem With {.SettingsTitle = "Duration of Notifications",
            .SettingsDescription = "Select how many seconds the notification will be displayed.",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_friends.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = NotificationDuration + " sec.",
            .ConfigSectionName = "Notifications",
            .ConfigToChange = "NotificationDuration"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableNotificationsSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = NotificationDurationSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadSystemSettings()
        WindowTitle.Text = "System"

        GeneralSettingsListView.Items.Clear()

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        Dim AudioSetting As New SettingsListViewItem With {.SettingsTitle = "Audio", .SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute))}
        Dim VideoSetting As New SettingsListViewItem With {.SettingsTitle = "Video", .SettingsIcon = New BitmapImage(New Uri("/Icons/Video.png", UriKind.RelativeOrAbsolute))}
        Dim BackgroundSetting As New SettingsListViewItem With {.SettingsTitle = "Display", .SettingsIcon = New BitmapImage(New Uri("/Icons/TVIcon.png", UriKind.RelativeOrAbsolute))}
        Dim SystemUpdateSetting As New SettingsListViewItem With {.SettingsTitle = "Check for OrbisPro Updates", .SettingsIcon = New BitmapImage(New Uri("/Icons/Update.png", UriKind.RelativeOrAbsolute))}

        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = AudioSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = VideoSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = BackgroundSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SystemUpdateSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadAudioSettings()
        WindowTitle.Text = "Audio"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithAudioControlStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithAudioControl"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        'Get the values
        Dim SystemVolume As String = MainConfigFile.IniReadValue("Audio", "SystemVolume")
        Dim SystemNavigationAudio As String = MainConfigFile.IniReadValue("Audio", "Navigation Audio Pack")

        'Declare the settings
        Dim SystemVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "System Volume",
            .SettingsState = SystemVolume,
            .ConfigSectionName = "Audio",
            .ConfigToChange = "SystemVolume"}
        Dim NavigationSoundsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Navigation Audio Pack",
            .SettingsDescription = "Change the interface navigation sounds.",
            .SettingsState = SystemNavigationAudio,
            .ConfigSectionName = "Audio",
            .ConfigToChange = "Navigation Audio Pack"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = SystemVolumeSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = NavigationSoundsSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadDisplaySettings()
        WindowTitle.Text = "Display"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        'Get the values
        Dim DisplayResolution As String = MainConfigFile.IniReadValue("System", "DisplayResolution")

        'Declare the settings
        Dim SelectedBackgroundSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/GalleryTransparent.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Background Settings"}
        Dim CustomBackgroundPathSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/TVIcon.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Display Resolution",
            .SettingsState = DisplayResolution,
            .SettingsDescription = "Set the Display Resoltion of OrbisPro.",
            .ConfigSectionName = "System",
            .ConfigToChange = "DisplayResolution"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SelectedBackgroundSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CustomBackgroundPathSetting}

        'Add the settings to the ListView
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadBackgroundSettings()
        WindowTitle.Text = "Background"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim SelectedBackground As String = MainConfigFile.IniReadValue("System", "Background")
        Dim CustomBackgroundPath As String = MainConfigFile.IniReadValue("System", "CustomBackgroundPath")
        Dim EnableBackgroundAnimation As Boolean = GetBoolValue(MainConfigFile.IniReadValue("System", "BackgroundAnimation"))
        Dim EnableBackgroundMusic As Boolean = GetBoolValue(MainConfigFile.IniReadValue("System", "BackgroundMusic"))
        Dim EnableBackgroundSwitchingAnimation As Boolean = GetBoolValue(MainConfigFile.IniReadValue("System", "BackgroundSwitchtingAnimation"))

        'Declare the settings
        Dim SelectedBackgroundSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Selected Background",
            .SettingsState = SelectedBackground,
            .SettingsDescription = "",
            .ConfigSectionName = "System",
            .ConfigToChange = "Background"}
        Dim CustomBackgroundPathSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Custom Background Path",
            .SettingsState = CustomBackgroundPath,
            .SettingsDescription = "Set a custom .mp4 background video. Enter the full path to the file.",
            .ConfigSectionName = "System",
            .ConfigToChange = "CustomBackgroundPath"}
        Dim EnableBackgroundSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Background Animation",
            .IsSettingChecked = EnableBackgroundAnimation,
            .ConfigSectionName = "System",
            .ConfigToChange = "BackgroundAnimation"}
        Dim EnableBackgroundMusicSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Background Music",
            .IsSettingChecked = EnableBackgroundMusic,
            .ConfigSectionName = "System",
            .ConfigToChange = "BackgroundMusic"}
        Dim EnableBackgroundSwitchingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Switch Game Backgrounds",
            .IsSettingChecked = EnableBackgroundSwitchingAnimation,
            .ConfigSectionName = "System",
            .ConfigToChange = "BackgroundSwitchtingAnimation"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SelectedBackgroundSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CustomBackgroundPathSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableBackgroundSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableBackgroundMusicSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableBackgroundSwitchingSetting}

        'Add the settings to the ListView
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadGeneralEmulatorSettings()
        WindowTitle.Text = "Emulators"

        GeneralSettingsListView.Items.Clear()

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)

        Dim SetupEmulatorsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "Setup Emulators"}
        Dim PS1EmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PS1 Emulator (ePSXe)"}
        Dim PS2EmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PS2 Emulator (pcsx2)"}
        Dim PS3EmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PS3 Emulator (rpcs3)"}
        Dim PS4EmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PS4 Emulator (shadPS4)"}
        Dim PSPEmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PSP Emulator (ppsspp)"}
        Dim PSVitaEmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "PS Vita Emulator (vita3k)"}
        Dim DolphinEmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "Dolpin Emulator"}
        Dim SegaEmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "Sega Emulator (Fusion)"}
        Dim MednafenEmulatorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.Relative)), .SettingsTitle = "Mednafen Emulator"}

        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SetupEmulatorsSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PS1EmulatorSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PS2EmulatorSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PS3EmulatorSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PS4EmulatorSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PSPEmulatorSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = PSVitaEmulatorSetting}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = DolphinEmulatorSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SegaEmulatorSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = MednafenEmulatorSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadNetworkSettings()
        WindowTitle.Text = "Network"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim ConnectToInternet As Boolean = GetBoolValue(MainConfigFile.IniReadValue("Network", "ConnectToInternet"))
        Dim DownloadPath As String = MainConfigFile.IniReadValue("Network", "DownloadPath")

        'Declare the settings
        Dim ConnectToInternetSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Connect to the Internet",
            .IsSettingChecked = ConnectToInternet,
            .ConfigSectionName = "Network",
            .ConfigToChange = "ConnectToInternet"}
        Dim SetupInternetSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Set Up Internet Connection"}
        Dim TestInternetSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Test Internet Connection"}
        Dim ViewConnectionSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "View Connection Status"}
        Dim ChangeDownloadPathSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Download Path",
            .SettingsDescription = "Change the default download path.",
            .SettingsState = DownloadPath,
            .ConfigSectionName = "Network",
            .ConfigToChange = "DownloadPath"}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ConnectToInternetSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = SetupInternetSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = TestInternetSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = ViewConnectionSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ChangeDownloadPathSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadGamepadSettings()
        WindowTitle.Text = "Gamepad Settings"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        'Get the values
        'Polling rates
        Dim Gamepad1PollingRate As String = MainConfigFile.IniReadValue("Gamepads", "Gamepad1PollingRate")
        Dim Gamepad2PollingRate As String = MainConfigFile.IniReadValue("Gamepads", "Gamepad2PollingRate")
        Dim Gamepad3PollingRate As String = MainConfigFile.IniReadValue("Gamepads", "Gamepad3PollingRate")
        Dim Gamepad4PollingRate As String = MainConfigFile.IniReadValue("Gamepads", "Gamepad4PollingRate")

        'Button layout
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        'Input Tester
        Dim InputTesterSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad Input Tester", .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute))}

        'Declare the settings
        Dim Gamepad1PollingRateSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad 1 Polling Rate",
            .SettingsDescription = "Set the polling rate of gamepad 1",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = Gamepad1PollingRate,
            .ConfigSectionName = "Gamepads",
            .ConfigToChange = "Gamepad1PollingRate"}
        Dim Gamepad2PollingRateSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad 2 Polling Rate",
            .SettingsDescription = "Set the polling rate of gamepad 2",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = Gamepad2PollingRate,
            .ConfigSectionName = "Gamepads",
            .ConfigToChange = "Gamepad2PollingRate"}
        Dim Gamepad3PollingRateSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad 3 Polling Rate",
            .SettingsDescription = "Set the polling rate of gamepad 3",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = Gamepad3PollingRate,
            .ConfigSectionName = "Gamepads",
            .ConfigToChange = "Gamepad3PollingRate"}
        Dim Gamepad4PollingRateSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad 4 Polling Rate",
            .SettingsDescription = "Set the polling rate of gamepad 4",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = Gamepad4PollingRate,
            .ConfigSectionName = "Gamepads",
            .ConfigToChange = "Gamepad4PollingRate"}
        Dim GamepadButtonLayoutSetting As New SettingsListViewItem With {.SettingsTitle = "Gamepad Button Layout",
            .SettingsDescription = "Set the gamepad button layout",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)),
            .SettingsState = GamepadButtonLayout,
            .ConfigSectionName = "Gamepads",
            .ConfigToChange = "ButtonLayout"}

        'Create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = InputTesterSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = Gamepad1PollingRateSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = Gamepad2PollingRateSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = Gamepad3PollingRateSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = Gamepad4PollingRateSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GamepadButtonLayoutSetting}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PS1 (ePSXe) Emulator Settings"

    Public Sub LoadPS1EmulatorSettings()
        WindowTitle.Text = "PS1 Emulator (ePSXe)"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim AutoloadppfFiles As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "AutoPpfLoad"))
        Dim UseHLEBios As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "BiosHLE"))
        Dim SelectedBios As String = GetRegistryStringValue("Software\epsxe\config", "BiosName")
        Dim CPUMode As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "CPUMode"))
        Dim FrameLimit As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "GPUFrameLimit"))
        Dim ShowFPS As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "GPUShowFPS"))
        Dim VSync As Boolean = GetBoolValue(GetRegistryStringValue("Software\epsxe\config", "GPUVsync"))

        'Declare the settings
        Dim AutoloadppfFilesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Autoload ppf files",
            .IsSettingChecked = AutoloadppfFiles,
            .ConfigToChange = "AutoPpfLoad"}
        Dim UseHLEBiosSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use HLE Bios",
            .IsSettingChecked = UseHLEBios,
            .ConfigToChange = "BiosHLE"}
        Dim SelectedBiosSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Selected Bios",
            .SettingsDescription = SelectedBios,
            .ConfigToChange = "BiosName"}
        Dim CPUModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Mode",
            .IsSettingChecked = CPUMode,
            .ConfigToChange = "CPUMode"}
        Dim FrameLimitSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Framelimit",
            .IsSettingChecked = FrameLimit,
            .ConfigToChange = "GPUFrameLimit"}
        Dim ShowFPSSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Show FPS",
            .IsSettingChecked = ShowFPS,
            .ConfigToChange = "GPUShowFPS"}
        Dim VSyncSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "VSync",
            .IsSettingChecked = VSync,
            .ConfigToChange = "GPUVsync"}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AutoloadppfFilesSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseHLEBiosSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SelectedBiosSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CPUModeSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FrameLimitSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShowFPSSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VSyncSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PS2 (pcsx2) Emulator Settings"

    Public Sub LoadPS2EmulatorSettings()
        WindowTitle.Text = "PS2 Emulator (pcsx2)"

        GeneralSettingsListView.Items.Clear()

        'Get the PCSX2 config
        Dim PCSX2Config As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\inis\PCSX2_ui.ini")

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
#Region "General"
        Dim EnableSpeedHacks As Boolean = GetBoolValue(PCSX2Config.IniReadValue("", "EnableSpeedHacks"))
        Dim EnableGameFixes As Boolean = GetBoolValue(PCSX2Config.IniReadValue("", "EnableGameFixes"))
        Dim EnablePresets As Boolean = GetBoolValue(PCSX2Config.IniReadValue("", "EnablePresets"))
        Dim McdCompressNTFS As Boolean = GetBoolValue(PCSX2Config.IniReadValue("", "McdCompressNTFS"))
#End Region
#Region "Memory Cards"
        Dim EnableMMC1 As Boolean = GetBoolValue(PCSX2Config.IniReadValue("MemoryCards", "Slot1_Enable"))
        Dim EnableMMC2 As Boolean = GetBoolValue(PCSX2Config.IniReadValue("MemoryCards", "Slot2_Enable"))
#End Region
#Region "Folders"
        Dim UseDefaultBios As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultBios"))
        Dim UseDefaultSnapshots As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultSnapshots"))
        Dim UseDefaultSavestates As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultSavestates"))
        Dim UseDefaultMemoryCards As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultMemoryCards"))
        Dim UseDefaultLogs As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultLogs"))
        Dim UseDefaultLangs As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultLangs"))
        Dim UseDefaultPluginsFolder As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultPluginsFolder"))
        Dim UseDefaultCheats As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultCheats"))
        Dim UseDefaultCheatsWS As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Folders", "UseDefaultCheatsWS"))

        Dim BiosFolder As String = PCSX2Config.IniReadValue("Folders", "Bios")
        Dim SnapshotsFolder As String = PCSX2Config.IniReadValue("Folders", "Snapshots")
        Dim SavestatesFolder As String = PCSX2Config.IniReadValue("Folders", "Savestates")
        Dim MemoryCardsFolder As String = PCSX2Config.IniReadValue("Folders", "MemoryCards")
        Dim LogsFolder As String = PCSX2Config.IniReadValue("Folders", "Logs")
        Dim LangsFolder As String = PCSX2Config.IniReadValue("Folders", "Langs")
        Dim CheatsFolder As String = PCSX2Config.IniReadValue("Folders", "Cheats")
        Dim CheatsWSFolder As String = PCSX2Config.IniReadValue("Folders", "CheatsWS")
        Dim PluginsFolder As String = PCSX2Config.IniReadValue("Folders", "PluginsFolder")
#End Region
#Region "Filenames"
        Dim BiosFilename As String = PCSX2Config.IniReadValue("Filenames", "BIOS")
#End Region
#Region "GSWindow"
        Dim DisableResizeBorders As Boolean = GetBoolValue(PCSX2Config.IniReadValue("GSWindow", "DisableResizeBorders"))
        Dim DisableScreenSaver As Boolean = GetBoolValue(PCSX2Config.IniReadValue("GSWindow", "DisableScreenSaver"))
        Dim EnableVsyncWindowFlag As Boolean = GetBoolValue(PCSX2Config.IniReadValue("GSWindow", "EnableVsyncWindowFlag"))
        Dim AspectRatio As String = PCSX2Config.IniReadValue("GSWindow", "AspectRatio")
        Dim Zoom As String = PCSX2Config.IniReadValue("GSWindow", "Zoom")
#End Region
#Region "Framerate"
        Dim NominalScalar As String = PCSX2Config.IniReadValue("Framerate", "NominalScalar")
        Dim TurboScalar As String = PCSX2Config.IniReadValue("Framerate", "TurboScalar")
        Dim SlomoScalar As String = PCSX2Config.IniReadValue("Framerate", "SlomoScalar")
        Dim SkipOnLimit As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Framerate", "SkipOnLimit"))
        Dim SkipOnTurbo As Boolean = GetBoolValue(PCSX2Config.IniReadValue("Framerate", "SkipOnTurbo"))
#End Region

        'Declare the settings
#Region "General Settings"
        Dim EnableSpeedhacksSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Speedhacks",
            .IsSettingChecked = EnableSpeedHacks,
            .ConfigSectionName = "",
            .ConfigToChange = "EnableSpeedHacks"}
        Dim EnableGameFixesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Game Fixes",
            .IsSettingChecked = EnableGameFixes,
            .ConfigSectionName = "",
            .ConfigToChange = "EnableGameFixes"}
        Dim EnablePresetsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Presets",
            .IsSettingChecked = EnablePresets,
            .ConfigSectionName = "",
            .ConfigToChange = "EnablePresets"}
        Dim McdCompressNTFSSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Mcd Compress NTFS",
            .IsSettingChecked = McdCompressNTFS,
            .ConfigSectionName = "",
            .ConfigToChange = "McdCompressNTFS"}
#End Region
#Region "Memory Cards Settings"
        Dim EnableMMC1Setting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Memory Card 1",
            .IsSettingChecked = EnableMMC1,
            .ConfigSectionName = "MemoryCards",
            .ConfigToChange = "EnableMMC1"}
        Dim EnableMMC2Setting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Memory Card 2",
            .IsSettingChecked = EnableMMC2,
            .ConfigSectionName = "MemoryCards",
            .ConfigToChange = "EnableMMC2"}
#End Region
#Region "Folders Settings"
        Dim FoldersSeparator As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Folders"}

        Dim UseDefaultBiosSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Bios Folder",
            .IsSettingChecked = UseDefaultBios,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultBios"}
        Dim UseDefaultSnapshotsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Snapshots Folder",
            .IsSettingChecked = UseDefaultSnapshots,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultSnapshots"}
        Dim UseDefaultSavestatesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Savestates Folder",
            .IsSettingChecked = UseDefaultSavestates,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultSavestates"}
        Dim UseDefaultMemoryCardsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Memory Cards Folder",
            .IsSettingChecked = UseDefaultMemoryCards,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultMemoryCards"}
        Dim UseDefaultLogsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Logs Folder",
            .IsSettingChecked = UseDefaultLogs,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultLogs"}
        Dim UseDefaultLangsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Langs Folder",
            .IsSettingChecked = UseDefaultLangs,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultLangs"}
        Dim UseDefaultPluginsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Plugins Folder",
            .IsSettingChecked = UseDefaultPluginsFolder,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultPluginsFolder"}
        Dim UseDefaultCheatsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Cheats Folder",
            .IsSettingChecked = UseDefaultCheats,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultCheats"}
        Dim UseDefaultCheatsWSSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default CheatsWS Folder",
            .IsSettingChecked = UseDefaultCheatsWS,
            .ConfigSectionName = "Folders",
            .ConfigToChange = "UseDefaultCheatsWS"}

        Dim BiosFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Bios Folder",
            .SettingsState = BiosFolder,
            .SettingsDescription = "Set the BIOS foldername.",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "BiosFolder"}
        Dim SnapshotsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Snapshots Folder",
            .SettingsState = SnapshotsFolder,
            .SettingsDescription = "Set the Snapshots foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "SnapshotsFolder"}
        Dim SavestatesFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Savestates Folder",
            .SettingsState = SavestatesFolder,
            .SettingsDescription = "Set the Savestates foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "SavestatesFolder"}
        Dim MemoryCardsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Memory Cards Folder",
            .SettingsState = MemoryCardsFolder,
            .SettingsDescription = "Set the Memory Cards foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "MemoryCardsFolder"}
        Dim LogsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Logs Folder",
            .SettingsState = LogsFolder,
            .SettingsDescription = "Set the Logs foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "LogsFolder"}
        Dim LangsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Langs Folder",
            .SettingsState = LangsFolder,
            .SettingsDescription = "Set the Langs foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "LangsFolder"}
        Dim CheatsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Plugins Folder",
            .SettingsState = CheatsFolder,
            .SettingsDescription = "Set the Cheats foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "CheatsFolder"}
        Dim CheatsWSFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default Cheats Folder",
            .SettingsState = CheatsWSFolder,
            .SettingsDescription = "Set the CheatsWS foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "CheatsWSFolder"}
        Dim PluginsFolderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Default CheatsWS Folder",
            .SettingsState = PluginsFolder,
            .SettingsDescription = "Set the Plugins foldername. (Cannot be changed in this build)",
            .ConfigSectionName = "Folders",
            .ConfigToChange = "PluginsFolder"}
#End Region
#Region "Filenames Settings"
        Dim BiosFilenameSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Bios Filename",
            .SettingsState = BiosFilename,
            .SettingsDescription = "Name of the BIOS filename to use.",
            .ConfigSectionName = "Filenames",
            .ConfigToChange = "BiosFilename"}
#End Region
#Region "GSWindow Settings"
        Dim DisableResizeBordersSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable Resize Borders",
            .IsSettingChecked = DisableResizeBorders,
            .ConfigSectionName = "GSWindow",
            .ConfigToChange = "DisableResizeBorders"}
        Dim DisableScreenSaverSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable ScreenSaver",
            .IsSettingChecked = DisableScreenSaver,
            .ConfigSectionName = "GSWindow",
            .ConfigToChange = "DisableScreenSaver"}
        Dim EnableVsyncWindowFlagSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Vsync",
            .IsSettingChecked = EnableVsyncWindowFlag,
            .ConfigSectionName = "GSWindow",
            .ConfigToChange = "EnableVsyncWindowFlag"}
        Dim AspectRatioSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Aspect Ratio",
            .SettingsState = AspectRatio,
            .SettingsDescription = "Set Aspect Ratio. (4:3 is default)",
            .ConfigSectionName = "GSWindow",
            .ConfigToChange = "AspectRatio"}
        Dim ZoomSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Zoom",
            .SettingsState = Zoom,
            .SettingsDescription = "Set Zoom. (100.00 is default)",
            .ConfigSectionName = "GSWindow",
            .ConfigToChange = "Zoom"}
#End Region
#Region "Framerate Settings"
        Dim NominalScalarSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Nominal Scalar",
            .SettingsState = NominalScalar,
            .SettingsDescription = "Set Nominal Scalar. (1.00 is default)",
            .ConfigSectionName = "Framerate",
            .ConfigToChange = "NominalScalar"}
        Dim TurboScalarSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Turbo Scalar",
            .SettingsState = TurboScalar,
            .SettingsDescription = "Set Turbo Scalar. (2.00 is default)",
            .ConfigSectionName = "Framerate",
            .ConfigToChange = "TurboScalar"}
        Dim SlomoScalarSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Slomo Scalar",
            .SettingsState = SlomoScalar,
            .SettingsDescription = "Set Slomo Scalar. (0.50 is default)",
            .ConfigSectionName = "Framerate",
            .ConfigToChange = "SlomoScalar"}
        Dim SkipOnLimitSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Skip On Limit",
            .IsSettingChecked = SkipOnLimit,
            .ConfigSectionName = "Framerate",
            .ConfigToChange = "SkipOnLimit"}
        Dim SkipOnTurboSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Skip On Turbo",
            .IsSettingChecked = SkipOnTurbo,
            .ConfigSectionName = "Framerate",
            .ConfigToChange = "Framerate"}
#End Region

        'Create a new ListViewItem and set the content
#Region "Create ListViewItem"
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableSpeedhacksSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableGameFixesSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnablePresetsSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = McdCompressNTFSSetting}

        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableMMC1Setting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableMMC2Setting}

        Dim Setting7Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = FoldersSeparator}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultBiosSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultSnapshotsSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultSavestatesSetting}
        Dim Setting11Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultMemoryCardsSetting}
        Dim Setting12Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultLogsSetting}
        Dim Setting13Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultLangsSetting}
        Dim Setting14Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultPluginsFolderSetting}
        Dim Setting15Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultCheatsSetting}
        Dim Setting16Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseDefaultCheatsWSSetting}
        Dim Setting17Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BiosFolderSetting}
        Dim Setting18Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SnapshotsFolderSetting}
        Dim Setting19Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SavestatesFolderSetting}
        Dim Setting20Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = MemoryCardsFolderSetting}
        Dim Setting21Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = LogsFolderSetting}
        Dim Setting22Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = LangsFolderSetting}
        Dim Setting23Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CheatsFolderSetting}
        Dim Setting24Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CheatsWSFolderSetting}
        Dim Setting25Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = PluginsFolderSetting}

        Dim Setting26Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BiosFilenameSetting}

        Dim Setting27Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableResizeBordersSetting}
        Dim Setting28Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableScreenSaverSetting}
        Dim Setting29Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableVsyncWindowFlagSetting}
        Dim Setting30Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AspectRatioSetting}
        Dim Setting31Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ZoomSetting}

        Dim Setting32Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = NominalScalarSetting}
        Dim Setting33Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = TurboScalarSetting}
        Dim Setting34Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SlomoScalarSetting}
        Dim Setting35Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SkipOnLimitSetting}
        Dim Setting36Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SkipOnTurboSetting}
#End Region

        'Add the settings to the list
#Region "Add Settings"
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)
        GeneralSettingsListView.Items.Add(Setting11Item)
        GeneralSettingsListView.Items.Add(Setting12Item)
        GeneralSettingsListView.Items.Add(Setting13Item)
        GeneralSettingsListView.Items.Add(Setting14Item)
        GeneralSettingsListView.Items.Add(Setting15Item)
        GeneralSettingsListView.Items.Add(Setting16Item)
        GeneralSettingsListView.Items.Add(Setting17Item)
        GeneralSettingsListView.Items.Add(Setting18Item)
        GeneralSettingsListView.Items.Add(Setting19Item)
        GeneralSettingsListView.Items.Add(Setting20Item)
        GeneralSettingsListView.Items.Add(Setting21Item)
        GeneralSettingsListView.Items.Add(Setting22Item)
        GeneralSettingsListView.Items.Add(Setting23Item)
        GeneralSettingsListView.Items.Add(Setting24Item)
        GeneralSettingsListView.Items.Add(Setting25Item)
        GeneralSettingsListView.Items.Add(Setting26Item)
        GeneralSettingsListView.Items.Add(Setting27Item)
        GeneralSettingsListView.Items.Add(Setting28Item)
        GeneralSettingsListView.Items.Add(Setting29Item)
        GeneralSettingsListView.Items.Add(Setting30Item)
        GeneralSettingsListView.Items.Add(Setting31Item)
        GeneralSettingsListView.Items.Add(Setting32Item)
        GeneralSettingsListView.Items.Add(Setting33Item)
        GeneralSettingsListView.Items.Add(Setting34Item)
        GeneralSettingsListView.Items.Add(Setting35Item)
        GeneralSettingsListView.Items.Add(Setting36Item)
#End Region

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PS3 (rpcs3) Emulator Settings"

    Public Sub LoadPS3EmulatorSettings()
        GeneralSettingsListView.Items.Clear()

        WindowTitle.Text = "PS3 Emulator (rpcs3)"

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Console Language",
            .SettingsDescription = "Change your console language. This can also affect the game language.",
            .SettingsState = "English (US)",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "License Area",
            .SettingsDescription = "Set the console region.",
            .SettingsState = "SCEE",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/browser_icon.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Enter button assignment",
            .SettingsDescription = "Choose whether to use Cross or Circle ass 'Enter' button.",
            .SettingsState = "Cross",
            .SettingsIcon = New BitmapImage(New Uri("/Icons/cross.png", UriKind.RelativeOrAbsolute))}

        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "PS3 Audio Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "PS3 Video Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsTitle = "PS3 Core Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS3AudioSettings()
        GeneralSettingsListView.Items.Clear()

        WindowTitle.Text = "PS3 Audio Settings"

        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithAudioControlStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithAudioControl"), DataTemplate)

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Renderer", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Cubeb is the recommended option and should be used whenever possible." + vbCrLf + "Windows: XAudio2 is the next best alternative." + vbCrLf + "Linux: PulseAudio is the next best alternative",
            .SettingsState = "Cubeb"}

        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "Convert To 16 Bit", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Uses 16-bit audio samples instead of default 32-bit floating point. Use with buggy audio drivers if you have no sound or completely broken sound.",
            .SettingsState = "Off"}

        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Audio Format", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Determines the sound format." + vbCrLf + "Configure this setting if you want to switch between stereo and surround sound." + vbCrLf + "The manual setting will use your selected formats while the automatic setting will let the game choose from all available formats.",
            .SettingsState = "Stereo"}

        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio Device", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Determines which audio device to use.",
            .SettingsState = "Default"}

        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Master Volume", .SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute))}

        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsTitle = "Audio Buffer Duration", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Target buffer duration in milliseconds. Higher values make the buffering algorithm's job easier, but may introduce noticeable audio latency." + vbCrLf + "Note that you can use keyboard arrow keys for precise changes on the slide bars.",
            .SettingsState = "100ms"}

        Dim GeneralEmulatorSetting7 As New SettingsListViewItem With {.SettingsTitle = "Enable Time Stretching", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Reduces crackle and stutter further, but may cause a very noticeable reduction in audio quality on slower CPUs." + vbCrLf + "Requires audio buffering to be enabled.",
            .SettingsState = "Off"}

        Dim GeneralEmulatorSetting8 As New SettingsListViewItem With {.SettingsTitle = "Time Stretching Threshold", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Buffer fill level (in percentage) below which time stretching will start." + vbCrLf + "Note that you can use keyboard arrow keys for precise changes on the slide bars.",
            .SettingsState = "75%"}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting6}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting7}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting8}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS3VideoSettings()
        GeneralSettingsListView.Items.Clear()

        WindowTitle.Text = "PS3 Video Settings"

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Console Language", .IsSettingSelected = Visibility.Visible, .SettingsState = "English (US)"}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "License Area", .IsSettingSelected = Visibility.Hidden, .SettingsState = "SCEE"}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Enter button assignment", .IsSettingSelected = Visibility.Hidden, .SettingsState = "Cross"}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Video Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_devices.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Core Settings",
            .IsSettingSelected = Visibility.Hidden}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
        GeneralSettingsListView.Focus()
    End Sub

    Public Sub LoadPS3CoreSettings()
        GeneralSettingsListView.Items.Clear()

        WindowTitle.Text = "PS3 Core Settings"

        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Console Language", .IsSettingSelected = Visibility.Visible}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "License Area", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Enter button assignment", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Video Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_devices.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Core Settings",
            .IsSettingSelected = Visibility.Hidden}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PS4 (shadps4) Emulator Settings"

    Public Sub LoadPS4EmulatorSettings()
        WindowTitle.Text = "PS4 Emulator (shadPS4)"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
        Dim ConfigContent As String = File.ReadAllText(ConfigPath)
        Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
        Dim GeneralTable As TomlTable = CType(ConfigTable("General"), TomlTable)
        Dim GUITable As TomlTable = CType(ConfigTable("GUI"), TomlTable)

        Dim IsPS4Pro As Boolean = CBool(GeneralTable("isPS4Pro"))
        Dim UserName As String = CStr(GeneralTable("userName"))
        Dim EmulatorLanguage As String = CStr(GUITable("emulatorLanguage"))

        'Declare the settings
        Dim IsPS4ProSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "PS4 Pro",
            .ConfigToChange = "isPS4Pro",
            .IsSettingChecked = IsPS4Pro}
        Dim UserNameSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "User Name",
            .ConfigToChange = "userName",
            .SettingsState = UserName,
            .SettingsDescription = "Sets the User Name for the PS4 Emulator."}
        Dim EmulatorLanguageSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Emulator Language",
            .ConfigToChange = "emulatorLanguage",
            .SettingsState = EmulatorLanguage,
            .SettingsDescription = "Sets the Language for the PS4 Emulator."}

        Dim GPUSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS4 GPU Settings"}
        Dim InputSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/ControllerWhite.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS4 Input Settings"}
        Dim OtherSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS4 Other General Settings"}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IsPS4ProSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = UserNameSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = EmulatorLanguageSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GPUSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = InputSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = OtherSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS4GPUSettings()
        WindowTitle.Text = "PS4 GPU Settings"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
        Dim ConfigContent As String = File.ReadAllText(ConfigPath)
        Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
        Dim GPUTable As TomlTable = CType(ConfigTable("GPU"), TomlTable)

        Dim ScreenWidth As Integer = CInt(GPUTable("screenWidth"))
        Dim ScreenHeight As Integer = CInt(GPUTable("screenHeight"))
        Dim NullGpu As Boolean = CBool(GPUTable("nullGpu"))
        Dim CopyGPUBuffers As Boolean = CBool(GPUTable("copyGPUBuffers"))
        Dim DumpShaders As Boolean = CBool(GPUTable("dumpShaders"))
        Dim PatchShaders As Boolean = CBool(GPUTable("patchShaders"))
        Dim VblankDivider As Integer = CInt(GPUTable("vblankDivider"))
        Dim Fullscreen As Boolean = CBool(GPUTable("Fullscreen"))
        Dim FullscreenMode As String = CStr(GPUTable("FullscreenMode"))
        Dim AllowHDR As Boolean = CBool(GPUTable("allowHDR"))

        'Declare the settings
        Dim ScreenWidthSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Screen Width",
            .ConfigToChange = "screenWidth",
            .SettingsState = ScreenWidth.ToString()}
        Dim ScreenHeightSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Screen Height",
            .ConfigToChange = "screenHeight",
            .SettingsState = ScreenHeight.ToString()}
        Dim NullGpuSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use NullGpu",
            .ConfigToChange = "nullGpu",
            .IsSettingChecked = NullGpu}
        Dim CopyGPUBuffersSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Copy GPU Buffers",
            .ConfigToChange = "copyGPUBuffers",
            .IsSettingChecked = CopyGPUBuffers}
        Dim DumpShadersSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Dump Shaders",
            .ConfigToChange = "dumpShaders",
            .IsSettingChecked = DumpShaders}
        Dim PatchShadersSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Patch Shaders",
            .ConfigToChange = "patchShaders",
            .IsSettingChecked = PatchShaders}
        Dim VblankDividerSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Vblank Divider",
            .ConfigToChange = "vblankDivider",
            .SettingsState = VblankDivider.ToString()}
        Dim FullscreenSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Fullscreen",
            .ConfigToChange = "Fullscreen",
            .IsSettingChecked = Fullscreen}
        Dim FullscreenModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Fullscreen Mode",
            .ConfigToChange = "FullscreenMode",
            .SettingsState = FullscreenMode}
        Dim AllowHDRSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable HDR",
            .ConfigToChange = "allowHDR",
            .IsSettingChecked = AllowHDR}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ScreenWidthSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ScreenHeightSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = NullGpuSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CopyGPUBuffersSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DumpShadersSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PatchShadersSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = VblankDividerSetting}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FullscreenSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = FullscreenModeSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AllowHDRSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS4InputSettings()
        WindowTitle.Text = "PS4 Input Settings"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
        Dim ConfigContent As String = File.ReadAllText(ConfigPath)
        Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
        Dim InputTable As TomlTable = CType(ConfigTable("Input"), TomlTable)

        Dim BackButtonBehavior As String = CStr(InputTable("backButtonBehavior"))
        Dim UseSpecialPad As Boolean = CBool(InputTable("useSpecialPad"))
        Dim SpecialPadClass As Integer = CInt(InputTable("specialPadClass"))
        Dim IsMotionControlsEnabled As Boolean = CBool(InputTable("isMotionControlsEnabled"))
        Dim UseUnifiedInputConfig As Boolean = CBool(InputTable("useUnifiedInputConfig"))

        'Declare the settings
        Dim BackButtonBehaviorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Back Button Behavior",
            .ConfigToChange = "backButtonBehavior",
            .SettingsState = BackButtonBehavior}
        Dim UseSpecialPadSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Special Pad",
            .ConfigToChange = "useSpecialPad",
            .IsSettingChecked = UseSpecialPad}
        Dim SpecialPadClassSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Special Pad Class",
            .ConfigToChange = "specialPadClass",
            .SettingsState = SpecialPadClass.ToString()}
        Dim IsMotionControlsEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Motion Controls",
            .ConfigToChange = "isMotionControlsEnabled",
            .IsSettingChecked = IsMotionControlsEnabled}
        Dim UseUnifiedInputConfigSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Unified Input Config",
            .ConfigToChange = "useUnifiedInputConfig",
            .IsSettingChecked = UseUnifiedInputConfig}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BackButtonBehaviorSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseSpecialPadSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SpecialPadClassSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IsMotionControlsEnabledSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseUnifiedInputConfigSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS4OtherSettings()
        WindowTitle.Text = "PS4 Other General Settings"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
        Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
        Dim ConfigContent As String = File.ReadAllText(ConfigPath)
        Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
        Dim GeneralTable As TomlTable = CType(ConfigTable("General"), TomlTable)

        Dim IsTrophyPopupDisabled As Boolean = CBool(GeneralTable("isTrophyPopupDisabled"))
        Dim TrophyNotificationDuration As Double = CDbl(GeneralTable("trophyNotificationDuration"))
        Dim PlayBGM As Boolean = CBool(GeneralTable("playBGM"))
        Dim BGMvolume As Integer = CInt(GeneralTable("BGMvolume"))
        Dim EnableDiscordRPC As Boolean = CBool(GeneralTable("enableDiscordRPC"))
        Dim UpdateChannel As String = CStr(GeneralTable("updateChannel"))
        Dim AutoUpdate As Boolean = CBool(GeneralTable("autoUpdate"))
        Dim SeparateUpdateEnabled As Boolean = CBool(GeneralTable("separateUpdateEnabled"))
        Dim CompatibilityEnabled As Boolean = CBool(GeneralTable("compatibilityEnabled"))
        Dim CheckCompatibilityOnStartup As Boolean = CBool(GeneralTable("checkCompatibilityOnStartup"))

        'Declare the settings
        Dim IsTrophyPopupDisabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable Tropy Popups",
            .ConfigToChange = "isTrophyPopupDisabled",
            .IsSettingChecked = IsTrophyPopupDisabled}
        Dim TrophyNotificationDurationSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Trophy Notification Duration",
            .ConfigToChange = "trophyNotificationDuration",
            .SettingsState = TrophyNotificationDuration.ToString()}
        Dim PlayBGMSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Play Background Music",
            .ConfigToChange = "playBGM",
            .IsSettingChecked = PlayBGM}
        Dim BGMvolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Background Music volume",
            .ConfigToChange = "BGMvolume",
            .SettingsState = BGMvolume.ToString()}
        Dim EnableDiscordRPCSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Discord RPC",
            .ConfigToChange = "enableDiscordRPC",
            .IsSettingChecked = EnableDiscordRPC}
        Dim UpdateChannelSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Update Channel",
            .ConfigToChange = "updateChannel",
            .SettingsState = UpdateChannel}
        Dim AutoUpdateSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Auto Update",
            .ConfigToChange = "autoUpdate",
            .IsSettingChecked = AutoUpdate}
        Dim SeparateUpdateEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Separate Update Enabled",
            .ConfigToChange = "separateUpdateEnabled",
            .IsSettingChecked = SeparateUpdateEnabled}
        Dim CompatibilityEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Compatibility Enabled",
            .ConfigToChange = "compatibilityEnabled",
            .IsSettingChecked = CompatibilityEnabled}
        Dim CheckCompatibilityOnStartupSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Check Compatibility on Startup",
            .ConfigToChange = "checkCompatibilityOnStartup",
            .IsSettingChecked = CheckCompatibilityOnStartup}

        'Create a new ListViewItem and set the content
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IsTrophyPopupDisabledSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = TrophyNotificationDurationSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PlayBGMSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BGMvolumeSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableDiscordRPCSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = UpdateChannelSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AutoUpdateSetting}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SeparateUpdateEnabledSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CompatibilityEnabledSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CheckCompatibilityOnStartupSetting}

        'Add the settings to the list
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PSP (ppsspp) Emulator Settings"

    Public Sub LoadPSPEmulatorSettings()
        WindowTitle.Text = "PSP Emulator (ppsspp)"

        GeneralSettingsListView.Items.Clear()

        'Get the PPSSPP config
        Dim PPSSPPConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\memstick\PSP\SYSTEM\ppsspp.ini")

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
#Region "General"
        Dim EnableLogging As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "Enable Logging"))
        Dim IgnoreBadMemAccess As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "IgnoreBadMemAccess"))
        Dim EnableCheats As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "EnableCheats"))
        Dim ScreenshotsAsPNG As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "ScreenshotsAsPNG"))
        Dim UseFFV1 As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "UseFFV1"))
        Dim MemStickInserted As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "MemStickInserted"))
        Dim EnablePlugins As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("General", "EnablePlugins"))
#End Region
#Region "CPU"
        Dim CPUCore As String = PPSSPPConfig.IniReadValue("CPU", "CPUCore")
        Dim SeparateSASThread As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "SeparateSASThread"))
        Dim IOTimingMethod As String = PPSSPPConfig.IniReadValue("CPU", "IOTimingMethod")
        Dim FastMemoryAccess As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "FastMemoryAccess"))
        Dim FunctionReplacements As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "FunctionReplacements"))
        Dim HideSlowWarnings As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "HideSlowWarnings"))
        Dim HideStateWarnings As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "HideStateWarnings"))
        Dim PreloadFunctions As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("CPU", "PreloadFunctions"))
        Dim CPUSpeed As String = PPSSPPConfig.IniReadValue("CPU", "CPUSpeed")
#End Region
#Region "Graphics"
        Dim GraphicsBackend As String = PPSSPPConfig.IniReadValue("Graphics", "GraphicsBackend")
        Dim UseGeometryShader As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "UseGeometryShader"))
        Dim SkipBufferEffects As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SkipBufferEffects"))
        Dim DisableRangeCulling As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "DisableRangeCulling"))
        Dim SoftwareRenderer As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SoftwareRenderer"))
        Dim SoftwareRendererJit As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SoftwareRendererJit"))
        Dim HardwareTransform As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "HardwareTransform"))
        Dim SoftwareSkinning As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SoftwareSkinning"))
        Dim TextureFiltering As String = PPSSPPConfig.IniReadValue("Graphics", "TextureFiltering")
        Dim Smart2DTexFiltering As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "Smart2DTexFiltering"))
        Dim InternalResolution As String = PPSSPPConfig.IniReadValue("Graphics", "InternalResolution")
        Dim HighQualityDepth As String = PPSSPPConfig.IniReadValue("Graphics", "HighQualityDepth")
        Dim FrameSkip As String = PPSSPPConfig.IniReadValue("Graphics", "FrameSkip")
        Dim FrameSkipType As String = PPSSPPConfig.IniReadValue("Graphics", "FrameSkipType")
        Dim AutoFrameSkip As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "AutoFrameSkip"))
        Dim AnisotropyLevel As String = PPSSPPConfig.IniReadValue("Graphics", "AnisotropyLevel")
        Dim MultiSampleLevel As String = PPSSPPConfig.IniReadValue("Graphics", "MultiSampleLevel")
        Dim TextureBackoffCache As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "TextureBackoffCache"))
        Dim BufferFiltering As String = PPSSPPConfig.IniReadValue("Graphics", "BufferFiltering")
        Dim ImmersiveMode As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "ImmersiveMode"))
        Dim SustainedPerformanceMode As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SustainedPerformanceMode"))
        Dim IgnoreScreenInsets As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "IgnoreScreenInsets"))
        Dim ReplaceTextures As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "ReplaceTextures"))
        Dim SaveNewTextures As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "SaveNewTextures"))
        Dim IgnoreTextureFilenames As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "IgnoreTextureFilenames"))
        Dim TexScalingLevel As String = PPSSPPConfig.IniReadValue("Graphics", "TexScalingLevel")
        Dim TexScalingType As String = PPSSPPConfig.IniReadValue("Graphics", "TexScalingType")
        Dim TexDeposterize As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "TexDeposterize"))
        Dim TexHardwareScaling As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "TexHardwareScaling"))
        Dim VSync As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "VSync"))
        Dim BloomHack As String = PPSSPPConfig.IniReadValue("Graphics", "BloomHack")
        Dim SplineBezierQuality As String = PPSSPPConfig.IniReadValue("Graphics", "SplineBezierQuality")
        Dim HardwareTessellation As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "HardwareTessellation"))
        Dim TextureShader As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "TextureShader"))
        Dim ShaderChainRequires60FPS As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "ShaderChainRequires60FPS"))
        Dim SkipGPUReadbackMode As String = PPSSPPConfig.IniReadValue("Graphics", "SkipGPUReadbackMode")
        Dim LogFrameDrops As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "LogFrameDrops"))
        Dim InflightFrames As String = PPSSPPConfig.IniReadValue("Graphics", "InflightFrames")
        Dim RenderDuplicateFrames As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "RenderDuplicateFrames"))
        Dim MultiThreading As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "MultiThreading"))
        Dim GpuLogProfiler As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "GpuLogProfiler"))
        Dim UberShaderVertex As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "UberShaderVertex"))
        Dim UberShaderFragment As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Graphics", "UberShaderFragment"))
#End Region
#Region "Sound"
        Dim EnableSound As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Sound", "Enable"))
        Dim AudioBackend As String = PPSSPPConfig.IniReadValue("Sound", "AudioBackend")
        Dim ExtraAudioBuffering As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Sound", "ExtraAudioBuffering"))
        Dim GlobalVolume As String = PPSSPPConfig.IniReadValue("Sound", "GlobalVolume")
        Dim ReverbVolume As String = PPSSPPConfig.IniReadValue("Sound", "ReverbVolume")
        Dim AltSpeedVolume As String = PPSSPPConfig.IniReadValue("Sound", "AltSpeedVolume")
        Dim AchievementSoundVolume As String = PPSSPPConfig.IniReadValue("Sound", "AchievementSoundVolume")
        Dim AutoAudioDevice As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Sound", "AutoAudioDevice"))
#End Region
#Region "Network"
        Dim EnableWlan As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "EnableWlan"))
        Dim EnableAdhocServer As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "EnableAdhocServer"))
        Dim proAdhocServer As String = PPSSPPConfig.IniReadValue("Network", "proAdhocServer")
        Dim PortOffset As String = PPSSPPConfig.IniReadValue("Network", "PortOffset")
        Dim MinTimeout As String = PPSSPPConfig.IniReadValue("Network", "MinTimeout")
        Dim ForcedFirstConnect As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "ForcedFirstConnect"))
        Dim EnableUPnP As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "EnableUPnP"))
        Dim UPnPUseOriginalPort As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "UPnPUseOriginalPort"))
        Dim EnableNetworkChat As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "EnableNetworkChat"))
        Dim EnableQuickChat As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("Network", "EnableQuickChat"))
#End Region
#Region "System"
        Dim PSPModel As String = PPSSPPConfig.IniReadValue("SystemParam", "PSPModel")
        Dim PSPFirmwareVersion As String = PPSSPPConfig.IniReadValue("SystemParam", "PSPFirmwareVersion")
        Dim NickName As String = PPSSPPConfig.IniReadValue("SystemParam", "NickName")
        Dim MacAddress As String = PPSSPPConfig.IniReadValue("SystemParam", "MacAddress")
        Dim GameLanguage As String = PPSSPPConfig.IniReadValue("SystemParam", "GameLanguage")
        Dim DayLightSavings As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("SystemParam", "DayLightSavings"))
        Dim ButtonPreference As String = PPSSPPConfig.IniReadValue("SystemParam", "ButtonPreference")
        Dim BypassOSKWithKeyboard As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("SystemParam", "BypassOSKWithKeyboard"))
        Dim EncryptSave As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("SystemParam", "EncryptSave"))
        Dim SavedataUpgradeVersion As Boolean = GetBoolValue(PPSSPPConfig.IniReadValue("SystemParam", "SavedataUpgradeVersion"))
        Dim MemStickSize As String = PPSSPPConfig.IniReadValue("SystemParam", "MemStickSize")
#End Region
#Region "More in next build"

#End Region

        'Declare the settings
#Region "General Settings"
        Dim EnableLoggingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Logging",
            .IsSettingChecked = EnableLogging,
            .ConfigSectionName = "General",
            .ConfigToChange = "EnableLogging"}
        Dim IgnoreBadMemAccessSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Ignore Bad Memory Access",
            .IsSettingChecked = IgnoreBadMemAccess,
            .ConfigSectionName = "General",
            .ConfigToChange = "IgnoreBadMemAccess"}
        Dim EnableCheatsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Cheats",
            .IsSettingChecked = EnableCheats,
            .ConfigSectionName = "General",
            .ConfigToChange = "EnableCheats"}
        Dim ScreenshotsAsPNGSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Save Screenshots As PNG",
            .IsSettingChecked = ScreenshotsAsPNG,
            .ConfigSectionName = "General",
            .ConfigToChange = "ScreenshotsAsPNG"}
        Dim UseFFV1Setting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use FFV1",
            .IsSettingChecked = UseFFV1,
            .ConfigSectionName = "General",
            .ConfigToChange = "UseFFV1"}
        Dim MemStickInsertedSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Memory Stick Inserted",
            .IsSettingChecked = MemStickInserted,
            .ConfigSectionName = "General",
            .ConfigToChange = "MemStickInserted"}
        Dim EnablePluginsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Plugins",
            .IsSettingChecked = EnablePlugins,
            .ConfigSectionName = "General",
            .ConfigToChange = "EnablePlugins"}
#End Region
#Region "CPU Settings"
        Dim CPUCoreSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Core",
            .SettingsState = CPUCore,
            .SettingsDescription = "",
            .ConfigSectionName = "CPU",
            .ConfigToChange = "CPUCore"}
        Dim SeparateSASThreadSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Separate SAS Thread",
            .IsSettingChecked = SeparateSASThread,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "SeparateSASThread"}
        Dim IOTimingMethodSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "IO Timing Method",
            .SettingsState = IOTimingMethod,
            .SettingsDescription = "",
            .ConfigSectionName = "CPU",
            .ConfigToChange = "IOTimingMethod"}
        Dim FastMemoryAccessSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Fast Memory Access",
            .IsSettingChecked = FastMemoryAccess,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "FastMemoryAccess"}
        Dim FunctionReplacementsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Function Replacements",
            .IsSettingChecked = FunctionReplacements,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "FunctionReplacements"}
        Dim HideSlowWarningsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Hide Slow Warnings",
            .IsSettingChecked = HideSlowWarnings,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "HideSlowWarnings"}
        Dim HideStateWarningsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Hide State Warnings",
            .IsSettingChecked = HideStateWarnings,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "HideStateWarnings"}
        Dim PreloadFunctionsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Preload Functions",
            .IsSettingChecked = PreloadFunctions,
            .ConfigSectionName = "CPU",
            .ConfigToChange = "PreloadFunctions"}
        Dim CPUSpeedSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Speed",
            .SettingsState = CPUSpeed,
            .SettingsDescription = "",
            .ConfigSectionName = "CPU",
            .ConfigToChange = "CPUSpeed"}
#End Region
#Region "Graphics Settings"
        Dim GraphicsBackendSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Graphics Backend",
            .SettingsState = GraphicsBackend,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "GraphicsBackend"}
        Dim UseGeometryShaderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Use Geometry Shader",
            .IsSettingChecked = UseGeometryShader,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "UseGeometryShader"}
        Dim SkipBufferEffectsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Skip Buffer Effects",
            .IsSettingChecked = SkipBufferEffects,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SkipBufferEffects"}
        Dim DisableRangeCullingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable Range Culling",
            .IsSettingChecked = DisableRangeCulling,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "DisableRangeCulling"}
        Dim SoftwareRendererSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Software Renderer",
            .IsSettingChecked = SoftwareRenderer,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SoftwareRenderer"}
        Dim SoftwareRendererJitSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Software Renderer Jit",
            .IsSettingChecked = SoftwareRendererJit,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SoftwareRendererJit"}
        Dim HardwareTransformSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Hardware Transform",
            .IsSettingChecked = HardwareTransform,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "HardwareTransform"}
        Dim SoftwareSkinningSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Software Skinning",
            .IsSettingChecked = SoftwareSkinning,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SoftwareSkinning"}
        Dim TextureFilteringSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Filtering",
            .SettingsState = TextureFiltering,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TextureFiltering"}
        Dim Smart2DTexFilteringSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Smart 2D Texture Filtering",
            .IsSettingChecked = Smart2DTexFiltering,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "Smart2DTexFiltering"}
        Dim InternalResolutionSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Internal Resolution",
            .SettingsState = InternalResolution,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "InternalResolution"}
        Dim HighQualityDepthSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "High Quality Depth",
            .SettingsState = HighQualityDepth,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "HighQualityDepth"}
        Dim FrameSkipSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Frame Skip",
            .SettingsState = FrameSkip,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "FrameSkip"}
        Dim FrameSkipTypeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Frame Skip Type",
            .SettingsState = FrameSkipType,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "FrameSkipType"}
        Dim AutoFrameSkipSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Auto Frame Skip",
            .IsSettingChecked = AutoFrameSkip,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "AutoFrameSkip"}
        Dim AnisotropyLevelSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Anisotropy Level",
            .SettingsState = AnisotropyLevel,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "AnisotropyLevel"}
        Dim MultiSampleLevelSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Multi Sample Level",
            .SettingsState = MultiSampleLevel,
            .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "MultiSampleLevel"}
        Dim TextureBackoffCacheSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Backoff Cache",
            .IsSettingChecked = TextureBackoffCache,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TextureBackoffCache"}
        Dim BufferFilteringSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Buffer Filtering",
           .SettingsState = BufferFiltering,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "BufferFiltering"}
        Dim ImmersiveModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Immersive Mode",
            .IsSettingChecked = ImmersiveMode,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "ImmersiveMode"}
        Dim SustainedPerformanceModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Sustained Performance Mode",
            .IsSettingChecked = SustainedPerformanceMode,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SustainedPerformanceMode"}
        Dim IgnoreScreenInsetsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Ignore Screen Insets",
            .IsSettingChecked = IgnoreScreenInsets,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "IgnoreScreenInsets"}
        Dim ReplaceTexturesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Replace Textures",
            .IsSettingChecked = ReplaceTextures,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "ReplaceTextures"}
        Dim SaveNewTexturesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Save New Textures",
            .IsSettingChecked = SaveNewTextures,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SaveNewTextures"}
        Dim IgnoreTextureFilenamesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Ignore Texture Filenames",
            .IsSettingChecked = IgnoreTextureFilenames,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "IgnoreTextureFilenames"}
        Dim TexScalingLevelSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Scaling Level",
           .SettingsState = TexScalingLevel,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TexScalingLevel"}
        Dim TexScalingTypeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Scaling Type",
           .SettingsState = TexScalingType,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TexScalingType"}
        Dim TexDeposterizeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Deposterize",
            .IsSettingChecked = TexDeposterize,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TexDeposterize"}
        Dim TexHardwareScalingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Hardware Scaling",
            .IsSettingChecked = TexHardwareScaling,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TexHardwareScaling"}
        Dim VSyncSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "VSync",
            .IsSettingChecked = VSync,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "VSync"}
        Dim BloomHackSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Bloom Hack",
           .SettingsState = BloomHack,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "BloomHack"}
        Dim SplineBezierQualitySetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Spline Bezier Quality",
           .SettingsState = SplineBezierQuality,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SplineBezierQuality"}
        Dim HardwareTessellationSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Hardware Tessellation",
            .IsSettingChecked = HardwareTessellation,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "HardwareTessellation"}
        Dim TextureShaderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Shader",
            .IsSettingChecked = TextureShader,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "TextureShader"}
        Dim ShaderChainRequires60FPSSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Shader Chain Requires 60FPS",
            .IsSettingChecked = ShaderChainRequires60FPS,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "ShaderChainRequires60FPS"}
        Dim SkipGPUReadbackModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Skip GPU Readback Mode",
           .SettingsState = SkipGPUReadbackMode,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "SkipGPUReadbackMode"}
        Dim LogFrameDropsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Log Frame Drops",
            .IsSettingChecked = LogFrameDrops,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "LogFrameDrops"}
        Dim InflightFramesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Inflight Frames",
           .SettingsState = InflightFrames,
           .SettingsDescription = "",
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "InflightFrames"}
        Dim RenderDuplicateFramesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Render Duplicate Frames",
            .IsSettingChecked = RenderDuplicateFrames,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "RenderDuplicateFrames"}
        Dim MultiThreadingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Multi Threading",
            .IsSettingChecked = MultiThreading,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "MultiThreading"}
        Dim GpuLogProfilerSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "GPU Log Profiler",
            .IsSettingChecked = GpuLogProfiler,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "GpuLogProfiler"}
        Dim UberShaderVertexSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Uber Shader Vertex",
            .IsSettingChecked = UberShaderVertex,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "UberShaderVertex"}
        Dim UberShaderFragmentSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Uber Shader Fragment",
            .IsSettingChecked = UberShaderFragment,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "UberShaderFragment"}
#End Region
#Region "Sound Settings"
        Dim EnableSoundSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Sound",
            .IsSettingChecked = EnableSound,
            .ConfigSectionName = "Sound",
            .ConfigToChange = "EnableSound"}
        Dim AudioBackendSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Audio Backend",
            .SettingsState = AudioBackend,
            .SettingsDescription = "",
            .ConfigSectionName = "Sound",
            .ConfigToChange = "AudioBackend"}
        Dim ExtraAudioBufferingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Extra Audio Buffering",
            .IsSettingChecked = ExtraAudioBuffering,
            .ConfigSectionName = "Sound",
            .ConfigToChange = "ExtraAudioBuffering"}
        Dim GlobalVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Global Volume",
            .SettingsState = GlobalVolume,
            .SettingsDescription = "",
            .ConfigSectionName = "Sound",
            .ConfigToChange = "GlobalVolume"}
        Dim ReverbVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Reverb Volume",
            .SettingsState = ReverbVolume,
            .SettingsDescription = "",
            .ConfigSectionName = "Sound",
            .ConfigToChange = "ReverbVolume"}
        Dim AltSpeedVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Alt Speed Volume",
            .SettingsState = AltSpeedVolume,
            .SettingsDescription = "",
            .ConfigSectionName = "Sound",
            .ConfigToChange = "AltSpeedVolume"}
        Dim AchievementSoundVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Achievement Sound Volume",
            .SettingsState = AchievementSoundVolume,
            .SettingsDescription = "",
            .ConfigSectionName = "Sound",
            .ConfigToChange = "AchievementSoundVolume"}
        Dim AutoAudioDeviceSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Auto Audio Device",
            .IsSettingChecked = AutoAudioDevice,
            .ConfigSectionName = "Sound",
            .ConfigToChange = "AutoAudioDevice"}
#End Region
#Region "Network Settings"
        Dim EnableWlanSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Wlan",
            .IsSettingChecked = EnableWlan,
            .ConfigSectionName = "Network",
            .ConfigToChange = "EnableWlan"}
        Dim EnableAdhocServerSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Adhoc Server",
            .IsSettingChecked = EnableAdhocServer,
            .ConfigSectionName = "Network",
            .ConfigToChange = "EnableAdhocServer"}
        Dim proAdhocServerSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "pro Adhoc Server",
            .SettingsState = proAdhocServer,
            .SettingsDescription = "",
            .ConfigSectionName = "Network",
            .ConfigToChange = "proAdhocServer"}
        Dim PortOffsetSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Port Offset",
            .SettingsState = PortOffset,
            .SettingsDescription = "",
            .ConfigSectionName = "Network",
            .ConfigToChange = "PortOffset"}
        Dim MinTimeoutSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Min Timeout",
            .SettingsState = MinTimeout,
            .SettingsDescription = "",
            .ConfigSectionName = "Network",
            .ConfigToChange = "MinTimeout"}
        Dim ForcedFirstConnectSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Forced First Connect",
            .IsSettingChecked = ForcedFirstConnect,
            .ConfigSectionName = "Network",
            .ConfigToChange = "ForcedFirstConnect"}
        Dim EnableUPnPSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable UPnP",
            .IsSettingChecked = EnableUPnP,
            .ConfigSectionName = "Network",
            .ConfigToChange = "EnableUPnP"}
        Dim UPnPUseOriginalPortSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "UPnP Use Original Port",
            .IsSettingChecked = UPnPUseOriginalPort,
            .ConfigSectionName = "Network",
            .ConfigToChange = "UPnPUseOriginalPort"}
        Dim EnableNetworkChatSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Network Chat",
            .IsSettingChecked = EnableNetworkChat,
            .ConfigSectionName = "Network",
            .ConfigToChange = "EnableNetworkChat"}
        Dim EnableQuickChatSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enable Quick Chat",
            .IsSettingChecked = EnableQuickChat,
            .ConfigSectionName = "Network",
            .ConfigToChange = "EnableQuickChat"}
#End Region
#Region "System Settings"
        Dim PSPModelSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "PSP Model",
            .SettingsState = PSPModel,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "PSPModel"}
        Dim PSPFirmwareVersionSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "PSP Firmware Version",
            .SettingsState = PSPFirmwareVersion,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "PSPFirmwareVersion"}
        Dim NickNameSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Nick Name",
            .SettingsState = NickName,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "NickName"}
        Dim MacAddressSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Mac Address",
            .SettingsState = MacAddress,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "MacAddress"}
        Dim GameLanguageSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Game Language",
            .SettingsState = GameLanguage,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "GameLanguage"}
        Dim DayLightSavingsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Day Light Savings",
            .IsSettingChecked = DayLightSavings,
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "DayLightSavings"}
        Dim ButtonPreferenceSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Button Preference",
            .SettingsState = ButtonPreference,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "ButtonPreference"}
        Dim BypassOSKWithKeyboardSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Bypass OSK With Keyboard",
            .IsSettingChecked = BypassOSKWithKeyboard,
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "BypassOSKWithKeyboard"}
        Dim EncryptSaveSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Encrypt Save",
            .IsSettingChecked = EncryptSave,
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "EncryptSave"}
        Dim SavedataUpgradeVersionSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Savedata Upgrade Version",
            .IsSettingChecked = SavedataUpgradeVersion,
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "SavedataUpgradeVersion"}
        Dim MemStickSizeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Memory Stick Size",
            .SettingsState = MemStickSize,
            .SettingsDescription = "",
            .ConfigSectionName = "SystemParam",
            .ConfigToChange = "MemStickSize"}
#End Region

        'Create a new ListViewItem and set the content
#Region "Create ListViewItem"
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableLoggingSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IgnoreBadMemAccessSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableCheatsSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ScreenshotsAsPNGSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseFFV1Setting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = MemStickInsertedSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnablePluginsSetting}

        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CPUCoreSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SeparateSASThreadSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = IOTimingMethodSetting}
        Dim Setting11Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FastMemoryAccessSetting}
        Dim Setting12Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FunctionReplacementsSetting}
        Dim Setting13Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HideSlowWarningsSetting}
        Dim Setting14Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HideStateWarningsSetting}
        Dim Setting15Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PreloadFunctionsSetting}
        Dim Setting16Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CPUSpeedSetting}

        Dim Setting17Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GraphicsBackendSetting}
        Dim Setting18Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UseGeometryShaderSetting}
        Dim Setting19Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SkipBufferEffectsSetting}
        Dim Setting20Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableRangeCullingSetting}
        Dim Setting21Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoftwareRendererSetting}
        Dim Setting22Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoftwareRendererJitSetting}
        Dim Setting23Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HardwareTransformSetting}
        Dim Setting24Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoftwareSkinningSetting}
        Dim Setting25Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = TextureFilteringSetting}
        Dim Setting26Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = Smart2DTexFilteringSetting}
        Dim Setting27Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = InternalResolutionSetting}
        Dim Setting28Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = HighQualityDepthSetting}
        Dim Setting29Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = FrameSkipSetting}
        Dim Setting30Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = FrameSkipTypeSetting}
        Dim Setting31Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AutoFrameSkipSetting}
        Dim Setting32Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AnisotropyLevelSetting}
        Dim Setting33Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = MultiSampleLevelSetting}
        Dim Setting34Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = TextureBackoffCacheSetting}
        Dim Setting35Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BufferFilteringSetting}
        Dim Setting36Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ImmersiveModeSetting}
        Dim Setting37Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SustainedPerformanceModeSetting}
        Dim Setting38Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IgnoreScreenInsetsSetting}
        Dim Setting39Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ReplaceTexturesSetting}
        Dim Setting40Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SaveNewTexturesSetting}
        Dim Setting41Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = IgnoreTextureFilenamesSetting}
        Dim Setting42Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = TexScalingLevelSetting}
        Dim Setting43Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = TexScalingTypeSetting}
        Dim Setting44Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = TexDeposterizeSetting}
        Dim Setting45Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = TexHardwareScalingSetting}
        Dim Setting46Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VSyncSetting}
        Dim Setting47Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BloomHackSetting}
        Dim Setting48Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SplineBezierQualitySetting}
        Dim Setting49Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HardwareTessellationSetting}
        Dim Setting50Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = TextureShaderSetting}
        Dim Setting51Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShaderChainRequires60FPSSetting}
        Dim Setting52Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SkipGPUReadbackModeSetting}
        Dim Setting53Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = LogFrameDropsSetting}
        Dim Setting54Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = InflightFramesSetting}
        Dim Setting55Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = RenderDuplicateFramesSetting}
        Dim Setting56Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = MultiThreadingSetting}
        Dim Setting57Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GpuLogProfilerSetting}
        Dim Setting58Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UberShaderVertexSetting}
        Dim Setting59Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UberShaderFragmentSetting}

        Dim Setting60Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableSoundSetting}
        Dim Setting61Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AudioBackendSetting}
        Dim Setting62Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ExtraAudioBufferingSetting}
        Dim Setting63Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GlobalVolumeSetting}
        Dim Setting64Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ReverbVolumeSetting}
        Dim Setting65Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AltSpeedVolumeSetting}
        Dim Setting66Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AchievementSoundVolumeSetting}
        Dim Setting67Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AutoAudioDeviceSetting}

        Dim Setting68Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableWlanSetting}
        Dim Setting69Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableAdhocServerSetting}
        Dim Setting70Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = proAdhocServerSetting}
        Dim Setting71Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = PortOffsetSetting}
        Dim Setting72Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = MinTimeoutSetting}
        Dim Setting73Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ForcedFirstConnectSetting}
        Dim Setting74Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableUPnPSetting}
        Dim Setting75Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = UPnPUseOriginalPortSetting}
        Dim Setting76Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableNetworkChatSetting}
        Dim Setting77Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableQuickChatSetting}

        Dim Setting78Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = PSPModelSetting}
        Dim Setting79Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = PSPFirmwareVersionSetting}
        Dim Setting80Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = NickNameSetting}
        Dim Setting81Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = MacAddressSetting}
        Dim Setting82Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GameLanguageSetting}
        Dim Setting83Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DayLightSavingsSetting}
        Dim Setting84Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ButtonPreferenceSetting}
        Dim Setting85Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = BypassOSKWithKeyboardSetting}
        Dim Setting86Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EncryptSaveSetting}
        Dim Setting87Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SavedataUpgradeVersionSetting}
        Dim Setting88Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = MemStickSizeSetting}
#End Region

        'Add the settings to the list
#Region "Add Settings"
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)
        GeneralSettingsListView.Items.Add(Setting11Item)
        GeneralSettingsListView.Items.Add(Setting12Item)
        GeneralSettingsListView.Items.Add(Setting13Item)
        GeneralSettingsListView.Items.Add(Setting14Item)
        GeneralSettingsListView.Items.Add(Setting15Item)
        GeneralSettingsListView.Items.Add(Setting16Item)
        GeneralSettingsListView.Items.Add(Setting17Item)
        GeneralSettingsListView.Items.Add(Setting18Item)
        GeneralSettingsListView.Items.Add(Setting19Item)
        GeneralSettingsListView.Items.Add(Setting20Item)
        GeneralSettingsListView.Items.Add(Setting21Item)
        GeneralSettingsListView.Items.Add(Setting22Item)
        GeneralSettingsListView.Items.Add(Setting23Item)
        GeneralSettingsListView.Items.Add(Setting24Item)
        GeneralSettingsListView.Items.Add(Setting25Item)
        GeneralSettingsListView.Items.Add(Setting26Item)
        GeneralSettingsListView.Items.Add(Setting27Item)
        GeneralSettingsListView.Items.Add(Setting28Item)
        GeneralSettingsListView.Items.Add(Setting29Item)
        GeneralSettingsListView.Items.Add(Setting30Item)
        GeneralSettingsListView.Items.Add(Setting31Item)
        GeneralSettingsListView.Items.Add(Setting32Item)
        GeneralSettingsListView.Items.Add(Setting33Item)
        GeneralSettingsListView.Items.Add(Setting34Item)
        GeneralSettingsListView.Items.Add(Setting35Item)
        GeneralSettingsListView.Items.Add(Setting36Item)
        GeneralSettingsListView.Items.Add(Setting37Item)
        GeneralSettingsListView.Items.Add(Setting38Item)
        GeneralSettingsListView.Items.Add(Setting39Item)
        GeneralSettingsListView.Items.Add(Setting40Item)
        GeneralSettingsListView.Items.Add(Setting41Item)
        GeneralSettingsListView.Items.Add(Setting42Item)
        GeneralSettingsListView.Items.Add(Setting43Item)
        GeneralSettingsListView.Items.Add(Setting44Item)
        GeneralSettingsListView.Items.Add(Setting45Item)
        GeneralSettingsListView.Items.Add(Setting46Item)
        GeneralSettingsListView.Items.Add(Setting47Item)
        GeneralSettingsListView.Items.Add(Setting48Item)
        GeneralSettingsListView.Items.Add(Setting49Item)
        GeneralSettingsListView.Items.Add(Setting50Item)
        GeneralSettingsListView.Items.Add(Setting51Item)
        GeneralSettingsListView.Items.Add(Setting52Item)
        GeneralSettingsListView.Items.Add(Setting53Item)
        GeneralSettingsListView.Items.Add(Setting54Item)
        GeneralSettingsListView.Items.Add(Setting55Item)
        GeneralSettingsListView.Items.Add(Setting56Item)
        GeneralSettingsListView.Items.Add(Setting57Item)
        GeneralSettingsListView.Items.Add(Setting58Item)
        GeneralSettingsListView.Items.Add(Setting59Item)
        GeneralSettingsListView.Items.Add(Setting60Item)
        GeneralSettingsListView.Items.Add(Setting61Item)
        GeneralSettingsListView.Items.Add(Setting62Item)
        GeneralSettingsListView.Items.Add(Setting63Item)
        GeneralSettingsListView.Items.Add(Setting64Item)
        GeneralSettingsListView.Items.Add(Setting65Item)
        GeneralSettingsListView.Items.Add(Setting66Item)
        GeneralSettingsListView.Items.Add(Setting67Item)
        GeneralSettingsListView.Items.Add(Setting68Item)
        GeneralSettingsListView.Items.Add(Setting69Item)
        GeneralSettingsListView.Items.Add(Setting70Item)
        GeneralSettingsListView.Items.Add(Setting71Item)
        GeneralSettingsListView.Items.Add(Setting72Item)
        GeneralSettingsListView.Items.Add(Setting73Item)
        GeneralSettingsListView.Items.Add(Setting74Item)
        GeneralSettingsListView.Items.Add(Setting75Item)
        GeneralSettingsListView.Items.Add(Setting76Item)
        GeneralSettingsListView.Items.Add(Setting77Item)
        GeneralSettingsListView.Items.Add(Setting78Item)
        GeneralSettingsListView.Items.Add(Setting79Item)
        GeneralSettingsListView.Items.Add(Setting80Item)
        GeneralSettingsListView.Items.Add(Setting81Item)
        GeneralSettingsListView.Items.Add(Setting82Item)
        GeneralSettingsListView.Items.Add(Setting83Item)
        GeneralSettingsListView.Items.Add(Setting84Item)
        GeneralSettingsListView.Items.Add(Setting85Item)
        GeneralSettingsListView.Items.Add(Setting86Item)
        GeneralSettingsListView.Items.Add(Setting87Item)
        GeneralSettingsListView.Items.Add(Setting88Item)
#End Region

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "PS Vita (vita3k) Emulator Settings"

    Public Sub LoadVita3kEmulatorSettings()
        WindowTitle.Text = "PS Vita Emulator (vita3k)"

        GeneralSettingsListView.Items.Clear()

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
#Region "Config values"
        Dim ValidationLayer As Boolean = GetBoolValue(GetVitaConfigValue("validation-layer"))
        Dim PSTVMode As Boolean = GetBoolValue(GetVitaConfigValue("pstv-mode"))
        Dim ShowMode As Boolean = GetBoolValue(GetVitaConfigValue("show-mode"))
        Dim DemoMode As Boolean = GetBoolValue(GetVitaConfigValue("demo-mode"))
        Dim DisplaySystemApps As Boolean = GetBoolValue(GetVitaConfigValue("display-system-apps"))
        Dim StretchTheDisplayArea As Boolean = GetBoolValue(GetVitaConfigValue("stretch_the_display_area"))
        Dim ShowLiveAreaScreen As Boolean = GetBoolValue(GetVitaConfigValue("show-live-area-screen"))
        Dim BackendRenderer As String = GetVitaConfigValue("backend-renderer")
        Dim HighAccuracy As Boolean = GetBoolValue(GetVitaConfigValue("high-accuracy"))
        Dim DisableSurfaceSync As Boolean = GetBoolValue(GetVitaConfigValue("disable-surface-sync"))
        Dim ScreenFilter As String = GetVitaConfigValue("screen-filter")
        Dim VSync As Boolean = GetBoolValue(GetVitaConfigValue("v-sync"))
        Dim AnisotropicFiltering As String = GetVitaConfigValue("anisotropic-filtering")
        Dim TextureCache As Boolean = GetBoolValue(GetVitaConfigValue("texture-cache"))
        Dim AsyncPipelineCompilation As Boolean = GetBoolValue(GetVitaConfigValue("async-pipeline-compilation"))
        Dim ShowCompileShaders As Boolean = GetBoolValue(GetVitaConfigValue("show-compile-shaders"))
        Dim HashlessTextureCache As Boolean = GetBoolValue(GetVitaConfigValue("hashless-texture-cache"))
        Dim ImportTextures As Boolean = GetBoolValue(GetVitaConfigValue("import-textures"))
        Dim ExportTextures As Boolean = GetBoolValue(GetVitaConfigValue("export-textures"))
        Dim ExportAsPNG As Boolean = GetBoolValue(GetVitaConfigValue("export-as-png"))
        Dim BootAppsFullScreen As Boolean = GetBoolValue(GetVitaConfigValue("boot-apps-full-screen"))
        Dim AudioBackend As String = GetVitaConfigValue("audio-backend")
        Dim AudioVolume As String = GetVitaConfigValue("audio-volume")
        Dim NGSEnable As Boolean = GetBoolValue(GetVitaConfigValue("ngs-enable"))
        Dim SysButton As String = GetVitaConfigValue("sys-button")
        Dim SysLang As String = GetVitaConfigValue("sys-lang")
        Dim SysDateFormat As String = GetVitaConfigValue("sys-date-format")
        Dim SysTimeFormat As String = GetVitaConfigValue("sys-time-format")
        Dim CPUPoolSize As String = GetVitaConfigValue("cpu-pool-size")
        Dim ModulesMode As String = GetVitaConfigValue("modules-mode")
        Dim DelayBackground As String = GetVitaConfigValue("delay-background")
        Dim DelayStart As String = GetVitaConfigValue("delay-start")
        Dim CPUBackend As String = GetVitaConfigValue("cpu-backend")
        Dim CPUOpt As Boolean = GetBoolValue(GetVitaConfigValue("cpu-opt"))
        Dim ShowTouchpadCursor As Boolean = GetBoolValue(GetVitaConfigValue("show-touchpad-cursor"))
        Dim PerformanceOverlay As Boolean = GetBoolValue(GetVitaConfigValue("performance-overlay"))
        Dim DisplayInfoMessage As Boolean = GetBoolValue(GetVitaConfigValue("display-info-message"))
        Dim AsiaFontSupport As Boolean = GetBoolValue(GetVitaConfigValue("asia-font-support"))
        Dim ShaderCache As Boolean = GetBoolValue(GetVitaConfigValue("shader-cache"))
        Dim SpirvShader As Boolean = GetBoolValue(GetVitaConfigValue("spirv-shader"))
        Dim FPSHack As Boolean = GetBoolValue(GetVitaConfigValue("fps-hack"))
        Dim HTTPEnable As Boolean = GetBoolValue(GetVitaConfigValue("http-enable"))
#End Region

        'Declare the settings
#Region "Settings"
        Dim ValidationLayerSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Validation Layer",
            .IsSettingChecked = ValidationLayer,
            .ConfigSectionName = "",
            .ConfigToChange = "validation-layer"}
        Dim PSTVModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "PSTV Mode",
            .IsSettingChecked = PSTVMode,
            .ConfigSectionName = "",
            .ConfigToChange = "pstv-mode"}
        Dim ShowModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Show Mode",
            .IsSettingChecked = ShowMode,
            .ConfigSectionName = "",
            .ConfigToChange = "show-mode"}
        Dim DemoModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Demo Mode",
            .IsSettingChecked = ValidationLayer,
            .ConfigSectionName = "",
            .ConfigToChange = "demo-mode"}
        Dim DisplaySystemAppsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Display System Apps",
            .IsSettingChecked = DisplaySystemApps,
            .ConfigSectionName = "",
            .ConfigToChange = "display-system-apps"}
        Dim StretchTheDisplayAreaSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Stretch The Display Area",
            .IsSettingChecked = StretchTheDisplayArea,
            .ConfigSectionName = "",
            .ConfigToChange = "stretch_the_display_area"}
        Dim ShowLiveAreaScreenSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Show Live Area Screen",
            .IsSettingChecked = ShowLiveAreaScreen,
            .ConfigSectionName = "",
            .ConfigToChange = "show-live-area-screen"}
        Dim BackendRendererSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Backend Renderer",
            .SettingsState = BackendRenderer,
            .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "backend-renderer"}
        Dim HighAccuracySetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "High Accuracy",
            .IsSettingChecked = HighAccuracy,
            .ConfigSectionName = "",
            .ConfigToChange = "high-accuracy"}
        Dim DisableSurfaceSyncSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable Surface Sync",
            .IsSettingChecked = DisableSurfaceSync,
            .ConfigSectionName = "",
            .ConfigToChange = "disable-surface-sync"}
        Dim ScreenFilterSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Screen Filter",
            .SettingsState = ScreenFilter,
            .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "screen-filter"}
        Dim VSyncSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "VSync",
            .IsSettingChecked = VSync,
            .ConfigSectionName = "",
            .ConfigToChange = "v-sync"}
        Dim AnisotropicFilteringSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Anisotropic Filtering",
           .SettingsState = AnisotropicFiltering,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "anisotropic-filtering"}
        Dim TextureCacheSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Texture Cache",
            .IsSettingChecked = TextureCache,
            .ConfigSectionName = "",
            .ConfigToChange = "texture-cache"}
        Dim AsyncPipelineCompilationSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Async Pipeline Compilation",
            .IsSettingChecked = AsyncPipelineCompilation,
            .ConfigSectionName = "",
            .ConfigToChange = "async-pipeline-compilation"}
        Dim ShowCompileShadersSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Show Compile Shaders",
            .IsSettingChecked = ShowCompileShaders,
            .ConfigSectionName = "",
            .ConfigToChange = "show-compile-shaders"}
        Dim HashlessTextureCacheSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Hashless Texture Cache",
            .IsSettingChecked = HashlessTextureCache,
            .ConfigSectionName = "",
            .ConfigToChange = "hashless-texture-cache"}
        Dim ImportTexturesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Import Textures",
            .IsSettingChecked = ImportTextures,
            .ConfigSectionName = "",
            .ConfigToChange = "import-textures"}
        Dim ExportTexturesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Export Textures",
            .IsSettingChecked = ExportTextures,
            .ConfigSectionName = "",
            .ConfigToChange = "export-textures"}
        Dim ExportAsPNGSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Export As PNG",
            .IsSettingChecked = ExportAsPNG,
            .ConfigSectionName = "",
            .ConfigToChange = "export-as-png"}
        Dim BootAppsFullScreenSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Boot Apps Fullscreen",
            .IsSettingChecked = BootAppsFullScreen,
            .ConfigSectionName = "",
            .ConfigToChange = "boot-apps-full-screen"}
        Dim AudioBackendSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Audio Backend",
           .SettingsState = AudioBackend,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "audio-backend"}
        Dim AudioVolumeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Audio Volume",
           .SettingsState = AudioVolume,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "audio-volume"}
        Dim NGSEnableSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "NGS Enable",
            .IsSettingChecked = NGSEnable,
            .ConfigSectionName = "",
            .ConfigToChange = "ngs-enable"}
        Dim SysButtonSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "System Button",
           .SettingsState = SysButton,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "sys-button"}
        Dim SysLangSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "System Lang",
           .SettingsState = SysLang,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "sys-lang"}
        Dim SysDateFormatSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "System Date Format",
           .SettingsState = SysDateFormat,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "sys-date-format"}
        Dim SysTimeFormatSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "System Time Format",
           .SettingsState = SysTimeFormat,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "sys-time-format"}
        Dim CPUPoolSizeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Pool Size",
           .SettingsState = CPUPoolSize,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "cpu-pool-size"}
        Dim ModulesModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Modules Mode",
           .SettingsState = ModulesMode,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "modules-mode"}
        Dim DelayBackgroundSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Delay Background",
           .SettingsState = DelayBackground,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "delay-background"}
        Dim DelayStartSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Delay Start",
           .SettingsState = DelayStart,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "delay-start"}
        Dim CPUBackendSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Backend",
           .SettingsState = CPUBackend,
           .SettingsDescription = "",
            .ConfigSectionName = "",
            .ConfigToChange = "cpu-backend"}
        Dim CPUOptSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "CPU Opt",
            .IsSettingChecked = CPUOpt,
            .ConfigSectionName = "",
            .ConfigToChange = "cpu-opt"}
        Dim ShowTouchpadCursorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Show Touchpad Cursor",
            .IsSettingChecked = ShowTouchpadCursor,
            .ConfigSectionName = "",
            .ConfigToChange = "show-touchpad-cursor"}
        Dim PerformanceOverlaySetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Performance Overlay",
            .IsSettingChecked = PerformanceOverlay,
            .ConfigSectionName = "",
            .ConfigToChange = "performance-overlay"}
        Dim DisplayInfoMessageSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Display Info Message",
            .IsSettingChecked = DisplayInfoMessage,
            .ConfigSectionName = "",
            .ConfigToChange = "display-info-message"}
        Dim AsiaFontSupportSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Asia Font Support",
            .IsSettingChecked = AsiaFontSupport,
            .ConfigSectionName = "",
            .ConfigToChange = "asia-font-support"}
        Dim ShaderCacheSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Shader Cache",
            .IsSettingChecked = ShaderCache,
            .ConfigSectionName = "",
            .ConfigToChange = "shader-cache"}
        Dim SpirvShaderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Spirv Shader",
            .IsSettingChecked = SpirvShader,
            .ConfigSectionName = "",
            .ConfigToChange = "spirv-shader"}
        Dim FPSHackSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "FPS Hack",
            .IsSettingChecked = FPSHack,
            .ConfigSectionName = "",
            .ConfigToChange = "fps-hack"}
        Dim HTTPEnableSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "HTTP Enable",
            .IsSettingChecked = HTTPEnable,
            .ConfigSectionName = "",
            .ConfigToChange = "http-enable"}
#End Region

        'Create a new ListViewItem and set the content
#Region "Create ListViewItem"
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ValidationLayerSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PSTVModeSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShowModeSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DemoModeSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisplaySystemAppsSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = StretchTheDisplayAreaSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShowLiveAreaScreenSetting}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = BackendRendererSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HighAccuracySetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableSurfaceSyncSetting}
        Dim Setting11Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ScreenFilterSetting}
        Dim Setting12Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VSyncSetting}
        Dim Setting13Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AnisotropicFilteringSetting}
        Dim Setting14Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = TextureCacheSetting}
        Dim Setting15Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AsyncPipelineCompilationSetting}
        Dim Setting16Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShowCompileShadersSetting}
        Dim Setting17Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HashlessTextureCacheSetting}
        Dim Setting18Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ImportTexturesSetting}
        Dim Setting19Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ExportTexturesSetting}
        Dim Setting20Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ExportAsPNGSetting}
        Dim Setting21Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = BootAppsFullScreenSetting}
        Dim Setting22Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AudioBackendSetting}
        Dim Setting23Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AudioVolumeSetting}
        Dim Setting24Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = NGSEnableSetting}

        Dim Setting25Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SysButtonSetting}
        Dim Setting26Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SysLangSetting}
        Dim Setting27Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SysDateFormatSetting}
        Dim Setting28Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = SysTimeFormatSetting}
        Dim Setting29Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CPUPoolSizeSetting}
        Dim Setting30Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = ModulesModeSetting}
        Dim Setting31Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = DelayBackgroundSetting}
        Dim Setting32Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = DelayStartSetting}
        Dim Setting33Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CPUBackendSetting}

        Dim Setting34Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CPUOptSetting}
        Dim Setting35Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShowTouchpadCursorSetting}
        Dim Setting36Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PerformanceOverlaySetting}
        Dim Setting37Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisplayInfoMessageSetting}
        Dim Setting38Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AsiaFontSupportSetting}
        Dim Setting39Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ShaderCacheSetting}
        Dim Setting40Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SpirvShaderSetting}
        Dim Setting41Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FPSHackSetting}
        Dim Setting42Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HTTPEnableSetting}
#End Region

        'Add the settings to the list
#Region "Add Settings"
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)
        GeneralSettingsListView.Items.Add(Setting11Item)
        GeneralSettingsListView.Items.Add(Setting12Item)
        GeneralSettingsListView.Items.Add(Setting13Item)
        GeneralSettingsListView.Items.Add(Setting14Item)
        GeneralSettingsListView.Items.Add(Setting15Item)
        GeneralSettingsListView.Items.Add(Setting16Item)
        GeneralSettingsListView.Items.Add(Setting17Item)
        GeneralSettingsListView.Items.Add(Setting18Item)
        GeneralSettingsListView.Items.Add(Setting19Item)
        GeneralSettingsListView.Items.Add(Setting20Item)
        GeneralSettingsListView.Items.Add(Setting21Item)
        GeneralSettingsListView.Items.Add(Setting22Item)
        GeneralSettingsListView.Items.Add(Setting23Item)
        GeneralSettingsListView.Items.Add(Setting24Item)
        GeneralSettingsListView.Items.Add(Setting25Item)
        GeneralSettingsListView.Items.Add(Setting26Item)
        GeneralSettingsListView.Items.Add(Setting27Item)
        GeneralSettingsListView.Items.Add(Setting28Item)
        GeneralSettingsListView.Items.Add(Setting29Item)
        GeneralSettingsListView.Items.Add(Setting30Item)
        GeneralSettingsListView.Items.Add(Setting31Item)
        GeneralSettingsListView.Items.Add(Setting32Item)
        GeneralSettingsListView.Items.Add(Setting33Item)
        GeneralSettingsListView.Items.Add(Setting34Item)
        GeneralSettingsListView.Items.Add(Setting35Item)
        GeneralSettingsListView.Items.Add(Setting36Item)
        GeneralSettingsListView.Items.Add(Setting37Item)
        GeneralSettingsListView.Items.Add(Setting38Item)
        GeneralSettingsListView.Items.Add(Setting39Item)
        GeneralSettingsListView.Items.Add(Setting40Item)
        GeneralSettingsListView.Items.Add(Setting41Item)
        GeneralSettingsListView.Items.Add(Setting42Item)
#End Region

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "Dolpin Emulator Settings"

    Public Sub LoadDolpinEmulatorSettings()
        WindowTitle.Text = "Dolpin Emulator"

        GeneralSettingsListView.Items.Clear()

        'Get the Dolphin config
        Dim DolphinConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\User\Config\Dolphin.ini")

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
#Region "Core"
        Dim AutoDiscChange As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Core", "AutoDiscChange"))
        Dim CPUThread As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Core", "CPUThread"))
        Dim EnableCheats As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Core", "EnableCheats"))
        Dim OverrideRegionSettings As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Core", "OverrideRegionSettings"))
        Dim FallbackRegion As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Core", "FallbackRegion"))
#End Region
#Region "Display"
        Dim DisableScreenSaver As Boolean = GetBoolValue(DolphinConfig.IniReadValue("Display", "DisableScreenSaver"))
#End Region
#Region "General"
        Dim HotkeysRequireFocus As Boolean = GetBoolValue(DolphinConfig.IniReadValue("General", "HotkeysRequireFocus"))
#End Region

        'Declare the settings
#Region "Graphics Settings"
        Dim AutoDiscChangeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = AutoDiscChange,
            .ConfigSectionName = "Core",
            .ConfigToChange = "AutoDiscChange"}
        Dim CPUThreadSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = CPUThread,
            .ConfigSectionName = "Core",
            .ConfigToChange = "CPUThread"}
        Dim EnableCheatsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = EnableCheats,
            .ConfigSectionName = "Core",
            .ConfigToChange = "EnableCheats"}
        Dim OverrideRegionSettingsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = OverrideRegionSettings,
            .ConfigSectionName = "Core",
            .ConfigToChange = "OverrideRegionSettings"}
        Dim FallbackRegionSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = FallbackRegion,
            .ConfigSectionName = "Core",
            .ConfigToChange = "FallbackRegion"}
#End Region
#Region "Display Settings"
        Dim DisableScreenSaverSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = DisableScreenSaver,
            .ConfigSectionName = "Display",
            .ConfigToChange = "DisableScreenSaver"}
#End Region
#Region "General Settings"
        Dim HotkeysRequireFocusSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = HotkeysRequireFocus,
            .ConfigSectionName = "General",
            .ConfigToChange = "HotkeysRequireFocus"}
#End Region

        'Create a new ListViewItem and set the content
#Region "Create ListViewItem"
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AutoDiscChangeSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CPUThreadSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnableCheatsSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = OverrideRegionSettingsSetting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FallbackRegionSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableScreenSaverSetting}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = HotkeysRequireFocusSetting}
#End Region

        'Add the settings to the list
#Region "Add Settings"
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
#End Region

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "Sega (Fusion) Emulator Settings"

    Public Sub LoadFusionEmulatorSettings()
        WindowTitle.Text = "Sega Emulator (Fusion)"

        GeneralSettingsListView.Items.Clear()

        'Add missing INI config file sections
        InsertFusionConfigSections()

        'Get the Fusion config     
        Dim FusionConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.ini")

        'Declare the styles
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)

        'Get the values
#Region "Graphics"
        Dim ForceCompatibleGFX As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "ForceCompatibleGFX"))
        Dim CompatibleGFXOpt As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "CompatibleGFXOpt"))
        Dim EnhancedGFXOpt As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "EnhancedGFXOpt"))
        Dim ForceFullScreen32 As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "ForceFullScreen32"))
        Dim VistaAeroIgnore As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "VistaAeroIgnore"))
        Dim VistaNoWndVSync As Boolean = GetBoolValue(FusionConfig.IniReadValue("Graphics", "VistaNoWndVSync"))
#End Region
#Region "Sound Compatibility"
        Dim ForceSWBuffer As Boolean = GetBoolValue(FusionConfig.IniReadValue("Sound", "ForceSWBuffer"))
#End Region
#Region "SG1000/SC3000/SMS/GG Specific"

#End Region
#Region "MegaDrive/Genesis Specific"

#End Region
#Region "Mega CD/Sega CD Specific"

#End Region
#Region "32X Specific"

#End Region
#Region "General"
        Dim ScreenshotType As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "ScreenshotType"))
        Dim ScreenshotMode As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "ScreenshotMode"))
        Dim CurrentCountry As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CurrentCountry"))
        Dim CountryAutoDetect As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CountryAutoDetect"))
        Dim CountryOrder As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CountryOrder"))
        Dim CurrentWaveFormat As String = FusionConfig.IniReadValue("General", "CurrentWaveFormat")
        Dim SoundOverdrive As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "SoundOverdrive"))
        Dim PSGNoiseBoost As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "PSGNoiseBoost"))
        Dim SoundSuperHQ As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "SoundSuperHQ"))
        Dim SoundDisabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "SoundDisabled"))
        Dim SoundFilter As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "SoundFilter"))
        Dim CurrentRenderMode As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CurrentRenderMode"))
        Dim DRenderMode As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DRenderMode"))
        Dim DFixedAspect As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DFixedAspect"))
        Dim DFixedZoom As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DFixedZoom"))
        Dim DFiltered As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DFiltered"))
        Dim DNTSCAspect As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DNTSCAspect"))
        Dim DNearestMultiple As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DNearestMultiple"))
        Dim DScanlines As String = FusionConfig.IniReadValue("General", "DScanlines")
        Dim VSyncEnabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "VSyncEnabled"))
        Dim LightgunCursor As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "LightgunCursor"))
        Dim FPSEnabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "FPSEnabled"))
        Dim CurrentRenderPlugin As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CurrentRenderPlugin"))
        Dim AllowSleeping As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "AllowSleeping"))
        Dim AlternateTiming As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "AlternateTiming"))
        Dim DisableShortcuts As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "DisableShortcuts"))
        Dim ThreadPriority As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "ThreadPriority"))
        Dim StaticDisabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "StaticDisabled"))
        Dim Brighten As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "Brighten"))
        Dim CartBootEnabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "CartBootEnabled"))
        Dim MSNStatusEnabled As Boolean = GetBoolValue(FusionConfig.IniReadValue("General", "MSNStatusEnabled"))
#End Region

        'Declare the settings
#Region "Graphics Settings"
        Dim ForceCompatibleGFXSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force Compatible GFX",
            .IsSettingChecked = ForceCompatibleGFX,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "ForceCompatibleGFX"}
        Dim CompatibleGFXOptSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Compatible GFX Opt",
            .IsSettingChecked = CompatibleGFXOpt,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "CompatibleGFXOpt"}
        Dim EnhancedGFXOptSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Enhanced GFX Opt",
            .IsSettingChecked = EnhancedGFXOpt,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "EnhancedGFXOpt"}
        Dim ForceFullScreen32Setting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force FullScreen 32",
            .IsSettingChecked = ForceFullScreen32,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "ForceFullScreen32"}
        Dim VistaAeroIgnoreSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Vista Aero Ignore",
            .IsSettingChecked = VistaAeroIgnore,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "VistaAeroIgnore"}
        Dim VistaNoWndVSyncSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Vista No Wnd VSync",
            .IsSettingChecked = VistaNoWndVSync,
            .ConfigSectionName = "Graphics",
            .ConfigToChange = "VistaNoWndVSync"}
#End Region
#Region "Sound Settings"
        Dim ForceSWBufferSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Force SW Buffer",
            .IsSettingChecked = ForceSWBuffer,
            .ConfigSectionName = "Sound",
            .ConfigToChange = "ForceSWBuffer"}
#End Region
#Region "General Settings"
        Dim ScreenshotTypeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Screenshot Type",
            .IsSettingChecked = ScreenshotType,
            .ConfigSectionName = "General",
            .ConfigToChange = "ScreenshotType"}
        Dim ScreenshotModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Screenshot Mode",
            .IsSettingChecked = ScreenshotMode,
            .ConfigSectionName = "General",
            .ConfigToChange = "ScreenshotMode"}
        Dim CurrentCountrySetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Current Country",
            .IsSettingChecked = CurrentCountry,
            .ConfigSectionName = "General",
            .ConfigToChange = "CurrentCountry"}
        Dim CountryAutoDetectSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Country Auto Detect",
            .IsSettingChecked = CountryAutoDetect,
            .ConfigSectionName = "General",
            .ConfigToChange = "CountryAutoDetect"}
        Dim CountryOrderSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Country Order",
            .IsSettingChecked = CountryOrder,
            .ConfigSectionName = "General",
            .ConfigToChange = "CountryOrder"}
        Dim CurrentWaveFormatSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Current Wave Format",
            .SettingsState = CurrentWaveFormat,
            .SettingsDescription = "",
            .ConfigSectionName = "General",
            .ConfigToChange = "CurrentWaveFormat"}
        Dim SoundOverdriveSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Sound Overdrive",
            .IsSettingChecked = SoundOverdrive,
            .ConfigSectionName = "General",
            .ConfigToChange = "SoundOverdrive"}
        Dim PSGNoiseBoostSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "PSG Noise Boost",
            .IsSettingChecked = PSGNoiseBoost,
            .ConfigSectionName = "General",
            .ConfigToChange = "PSGNoiseBoost"}
        Dim SoundSuperHQSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Sound Super HQ",
            .IsSettingChecked = SoundSuperHQ,
            .ConfigSectionName = "General",
            .ConfigToChange = "SoundSuperHQ"}
        Dim SoundDisabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Sound Disabled",
            .IsSettingChecked = SoundDisabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "SoundDisabled"}
        Dim SoundFilterSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Sound.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Sound Filter",
            .IsSettingChecked = SoundFilter,
            .ConfigSectionName = "General",
            .ConfigToChange = "SoundFilter"}
        Dim CurrentRenderModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Current Render Mode",
            .IsSettingChecked = CurrentRenderMode,
            .ConfigSectionName = "General",
            .ConfigToChange = "CurrentRenderMode"}
        Dim DRenderModeSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Render Mode",
            .IsSettingChecked = DRenderMode,
            .ConfigSectionName = "General",
            .ConfigToChange = "DRenderMode"}
        Dim DFixedAspectSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Fixed Aspect",
            .IsSettingChecked = DFixedAspect,
            .ConfigSectionName = "General",
            .ConfigToChange = "DFixedAspect"}
        Dim DFixedZoomSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Fixed Zoom",
            .IsSettingChecked = DFixedZoom,
            .ConfigSectionName = "General",
            .ConfigToChange = "DFixedZoom"}
        Dim DFilteredSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Filtered",
            .IsSettingChecked = DFiltered,
            .ConfigSectionName = "General",
            .ConfigToChange = "DFiltered"}
        Dim DNTSCAspectSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D NTSC Aspect",
            .IsSettingChecked = DNTSCAspect,
            .ConfigSectionName = "General",
            .ConfigToChange = "DNTSCAspect"}
        Dim DNearestMultipleSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Nearest Multiple",
            .IsSettingChecked = DNearestMultiple,
            .ConfigSectionName = "General",
            .ConfigToChange = "DNearestMultiple"}
        Dim DScanlinesSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "D Scanlines",
            .SettingsState = DScanlines,
            .SettingsDescription = "",
            .ConfigSectionName = "General",
            .ConfigToChange = "DScanlines"}
        Dim VSyncEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "VSync Enabled",
            .IsSettingChecked = VSyncEnabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "VSyncEnabled"}
        Dim LightgunCursorSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Lightgun Cursor",
            .IsSettingChecked = LightgunCursor,
            .ConfigSectionName = "General",
            .ConfigToChange = "LightgunCursor"}
        Dim FPSEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "FPS Enabled",
            .IsSettingChecked = FPSEnabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "FPSEnabled"}
        Dim CurrentRenderPluginSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Current Render Plugin",
            .IsSettingChecked = CurrentRenderPlugin,
            .ConfigSectionName = "General",
            .ConfigToChange = "CurrentRenderPlugin"}
        Dim AllowSleepingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Allow Sleeping",
            .IsSettingChecked = AllowSleeping,
            .ConfigSectionName = "General",
            .ConfigToChange = "AllowSleeping"}
        Dim AlternateTimingSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Alternate Timing",
            .IsSettingChecked = AlternateTiming,
            .ConfigSectionName = "General",
            .ConfigToChange = "AlternateTiming"}
        Dim DisableShortcutsSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Disable Shortcuts",
            .IsSettingChecked = DisableShortcuts,
            .ConfigSectionName = "General",
            .ConfigToChange = "DisableShortcuts"}
        Dim ThreadPrioritySetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Thread Priority",
            .IsSettingChecked = ThreadPriority,
            .ConfigSectionName = "General",
            .ConfigToChange = "ThreadPriority"}
        Dim StaticDisabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Static Disabled",
            .IsSettingChecked = StaticDisabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "StaticDisabled"}
        Dim BrightenSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Brighten",
            .IsSettingChecked = Brighten,
            .ConfigSectionName = "General",
            .ConfigToChange = "Brighten"}
        Dim CartBootEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Cart Boot Enabled",
            .IsSettingChecked = CartBootEnabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "CartBootEnabled"}
        Dim MSNStatusEnabledSetting As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Network.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "MSN Status Enabled",
            .IsSettingChecked = MSNStatusEnabled,
            .ConfigSectionName = "General",
            .ConfigToChange = "MSNStatusEnabled"}
#End Region

        'Create a new ListViewItem and set the content
#Region "Create ListViewItem"
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ForceCompatibleGFXSetting}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CompatibleGFXOptSetting}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = EnhancedGFXOptSetting}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ForceFullScreen32Setting}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VistaAeroIgnoreSetting}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VistaNoWndVSyncSetting}

        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ForceSWBufferSetting}

        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ScreenshotTypeSetting}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ScreenshotModeSetting}
        Dim Setting10Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CurrentCountrySetting}
        Dim Setting11Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CountryAutoDetectSetting}
        Dim Setting12Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CountryOrderSetting}
        Dim Setting13Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = CurrentWaveFormatSetting}
        Dim Setting14Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoundOverdriveSetting}
        Dim Setting15Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = PSGNoiseBoostSetting}
        Dim Setting16Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoundSuperHQSetting}
        Dim Setting17Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoundDisabledSetting}
        Dim Setting18Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = SoundFilterSetting}
        Dim Setting19Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CurrentRenderModeSetting}
        Dim Setting20Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DRenderModeSetting}
        Dim Setting21Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DFixedAspectSetting}
        Dim Setting22Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DFixedZoomSetting}
        Dim Setting23Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DFilteredSetting}
        Dim Setting24Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DNTSCAspectSetting}
        Dim Setting25Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DNearestMultipleSetting}
        Dim Setting26Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = DScanlinesSetting}
        Dim Setting27Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = VSyncEnabledSetting}
        Dim Setting28Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = LightgunCursorSetting}
        Dim Setting29Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = FPSEnabledSetting}
        Dim Setting30Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CurrentRenderPluginSetting}
        Dim Setting31Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AllowSleepingSetting}
        Dim Setting32Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AlternateTimingSetting}
        Dim Setting33Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = DisableShortcutsSetting}
        Dim Setting34Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = ThreadPrioritySetting}
        Dim Setting35Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = StaticDisabledSetting}
        Dim Setting36Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = BrightenSetting}
        Dim Setting37Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = CartBootEnabledSetting}
        Dim Setting38Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = MSNStatusEnabledSetting}
#End Region

        'Add the settings to the list
#Region "Add Settings"
        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)
        GeneralSettingsListView.Items.Add(Setting6Item)
        GeneralSettingsListView.Items.Add(Setting7Item)
        GeneralSettingsListView.Items.Add(Setting8Item)
        GeneralSettingsListView.Items.Add(Setting9Item)
        GeneralSettingsListView.Items.Add(Setting10Item)
        GeneralSettingsListView.Items.Add(Setting11Item)
        GeneralSettingsListView.Items.Add(Setting12Item)
        GeneralSettingsListView.Items.Add(Setting13Item)
        GeneralSettingsListView.Items.Add(Setting14Item)
        GeneralSettingsListView.Items.Add(Setting15Item)
        GeneralSettingsListView.Items.Add(Setting16Item)
        GeneralSettingsListView.Items.Add(Setting17Item)
        GeneralSettingsListView.Items.Add(Setting18Item)
        GeneralSettingsListView.Items.Add(Setting19Item)
        GeneralSettingsListView.Items.Add(Setting20Item)
        GeneralSettingsListView.Items.Add(Setting21Item)
        GeneralSettingsListView.Items.Add(Setting22Item)
        GeneralSettingsListView.Items.Add(Setting23Item)
        GeneralSettingsListView.Items.Add(Setting24Item)
        GeneralSettingsListView.Items.Add(Setting25Item)
        GeneralSettingsListView.Items.Add(Setting26Item)
        GeneralSettingsListView.Items.Add(Setting27Item)
        GeneralSettingsListView.Items.Add(Setting28Item)
        GeneralSettingsListView.Items.Add(Setting29Item)
        GeneralSettingsListView.Items.Add(Setting30Item)
        GeneralSettingsListView.Items.Add(Setting31Item)
        GeneralSettingsListView.Items.Add(Setting32Item)
        GeneralSettingsListView.Items.Add(Setting33Item)
        GeneralSettingsListView.Items.Add(Setting34Item)
        GeneralSettingsListView.Items.Add(Setting35Item)
        GeneralSettingsListView.Items.Add(Setting36Item)
        GeneralSettingsListView.Items.Add(Setting37Item)
        GeneralSettingsListView.Items.Add(Setting38Item)
#End Region

        GeneralSettingsListView.Items.Refresh()
    End Sub

#End Region

#Region "Mednafen (Multi) Emulator Settings"

    Public Sub LoadMednafenEmulatorSettings()
        WindowTitle.Text = "Mednafen Emulator"

        GeneralSettingsListView.Items.Clear()

        'Get the PPSSPP config
        Dim PPSSPPConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\memstick\PSP\SYSTEM\ppsspp.ini")

        'Declare the styles
        Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
        Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
        Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

        'Get the values
    End Sub

#End Region

#Region "Selection & Focus Changes"

    Private Sub GeneralSettingsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles GeneralSettingsListView.SelectionChanged
        If GeneralSettingsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim RemovedItem As ListViewItem = CType(e.RemovedItems(0), ListViewItem)
            Dim AddedItem As ListViewItem = CType(e.AddedItems(0), ListViewItem)

            Dim PreviousItem = CType(RemovedItem.Content, SettingsListViewItem)
            Dim SelectedItem = CType(AddedItem.Content, SettingsListViewItem)

            SelectedItem.IsSettingSelected = Visibility.Visible
            PreviousItem.IsSettingSelected = Visibility.Hidden

            'Hide the description of the previously selected item
            If Not String.IsNullOrEmpty(PreviousItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(RemovedItem)
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemBorder, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemSeparator, Canvas.TopProperty, 197, 97, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If

            'Show the description of the newly selected item
            If Not String.IsNullOrEmpty(SelectedItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(AddedItem)
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 100, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemBorder, HeightProperty, 75, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemSeparator, Canvas.TopProperty, 97, 197, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))
                End If
            End If

            'Check the ContentTemplate and change the ActionButtonTextBlock.Text
            Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
            Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

            If AddedItem.ContentTemplate Is SettingWithCheckBoxStyle Then
                ActionButtonTextBlock.Text = "Toggle"
            ElseIf AddedItem.ContentTemplate Is SettingWithDescription Then
                ActionButtonTextBlock.Text = "Change"
            Else
                ActionButtonTextBlock.Text = "Enter"
            End If

        End If
    End Sub

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

    Private Sub SettingButton4_GotFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton4.GotFocus
        SettingButton4.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton4_LostFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton4.LostFocus
        SettingButton4.BorderBrush = Nothing
    End Sub

    Private Sub SettingButton5_GotFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton5.GotFocus
        SettingButton5.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton5_LostFocus(sender As Object, e As RoutedEventArgs) Handles SettingButton5.LostFocus
        SettingButton5.BorderBrush = Nothing
    End Sub

#End Region

#Region "Navigation"

    Private Sub OpenNewSettings()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            PlayBackgroundSound(Sounds.SelectItem)

            'Get the ListView of the selected item
            Dim SelectedListViewItem As ListViewItem = CType(GeneralSettingsListView.SelectedItem, ListViewItem)
            Dim SelectedItem As SettingsListViewItem = CType(SelectedListViewItem.Content, SettingsListViewItem)

            'Open the next list of settings if the title matches a new list of settings
            Select Case SelectedItem.SettingsTitle
                Case "User Management"
                    'Set the LastGeneralSettingsIndex to the currently selected index from GeneralSettingsListView
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    'Load the settings for User Management
                    LoadUserManagementSettings()
                    'Animate the new ListView
                    GoForwardAnimation()
                Case "Network"
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadNetworkSettings()
                    GoForwardAnimation()
                Case "Notifications"
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadNotificationSettings()
                    GoForwardAnimation()
                Case "System"
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadSystemSettings()
                    GoForwardAnimation()
                Case "Emulators"
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralEmulatorSettings()
                    GoForwardAnimation()

                Case "Check for OrbisPro Updates"
                    PauseInput = True

                    Dim NewSetupCheckUpdates As New SetupCheckUpdates() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "GeneralSettings"}
                    NewSetupCheckUpdates.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewSetupCheckUpdates.Show()

                Case "Bluetooth"
                    PauseInput = True

                    Dim NewBluetoothSettings As New BluetoothSettings() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "GeneralSettings"}
                    NewBluetoothSettings.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewBluetoothSettings.Show()

                Case "Gamepad Settings"
                    LastGeneralSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGamepadSettings()
                    GoForwardAnimation()

                Case "Gamepad Input Tester"
                    PauseInput = True

                    Dim NewGamepadInputTester As New GamepadInputTester() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0}
                    NewGamepadInputTester.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewGamepadInputTester.Show()


                Case "Set Up Internet Connection"
                    PauseInput = True

                    Dim NewWifiSettings As New WifiSettings() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "GeneralSettings"}
                    NewWifiSettings.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewWifiSettings.Show()

                Case "Audio"
                    LastSystemSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadAudioSettings()
                    GoForwardAnimation()
                Case "Display"
                    LastSystemSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadDisplaySettings()
                    GoForwardAnimation()
                Case "Background Settings"
                    LastDisplaySettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadBackgroundSettings()
                    GoForwardAnimation()
                Case "Video"
                    'LastSystemSettingsIndex = GeneralSettingsListView.SelectedIndex

                Case "Setup Emulators"
                    PauseInput = True

                    Dim NewSetupEmulators As New SetupEmulators() With {.Top = 0, .Left = 0, .ShowActivated = True, .Opacity = 0, .Opener = "GeneralSettings"}
                    NewSetupEmulators.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewSetupEmulators.Show()

                Case "PS1 Emulator (ePSXe)"
                    'Set the LastGeneralEmulatorSettingsIndex to the currently selected index from GeneralSettingsListView
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS1EmulatorSettings()
                    GoForwardAnimation()
                Case "PS2 Emulator (pcsx2)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS2EmulatorSettings()
                    GoForwardAnimation()
                Case "PS3 Emulator (rpcs3)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS3EmulatorSettings()
                    GoForwardAnimation()
                Case "PS4 Emulator (shadPS4)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS4EmulatorSettings()
                    GoForwardAnimation()
                Case "PSP Emulator (ppsspp)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPSPEmulatorSettings()
                    GoForwardAnimation()
                Case "PS Vita Emulator (vita3k)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadVita3kEmulatorSettings()
                    GoForwardAnimation()
                Case "Dolpin Emulator"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadDolpinEmulatorSettings()
                    GoForwardAnimation()
                Case "Sega Emulator (Fusion)"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadFusionEmulatorSettings()
                    GoForwardAnimation()
                Case "Mednafen Emulator"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadMednafenEmulatorSettings()
                    GoForwardAnimation()

                Case "PS3 Audio Settings"
                    LastPS3EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS3AudioSettings()
                    GoForwardAnimation()
                Case "PS3 Video Settings"
                    LastPS3EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS3VideoSettings()
                    GoForwardAnimation()
                Case "PS3 Core Settings"
                    LastPS3EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS3CoreSettings()
                    GoForwardAnimation()

                Case "PS4 GPU Settings"
                    LastPS4EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS4GPUSettings()
                    GoForwardAnimation()
                Case "PS4 Input Settings"
                    LastPS4EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS4InputSettings()
                    GoForwardAnimation()
                Case "PS4 Other General Settings"
                    LastPS4EmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadPS4OtherSettings()
                    GoForwardAnimation()
            End Select

            'If the ContentTemplate is SettingWithCheckBoxStyle, change the boolean value
            'If the ContentTemplate is SettingWithDescription, show an input box (current exception are PS3 related settings)
            Dim DefaultSettingStyle As DataTemplate = CType(SettingsWindow.Resources("DefaultSetting"), DataTemplate)
            Dim SettingWithDescription As DataTemplate = CType(SettingsWindow.Resources("SettingWithDescription"), DataTemplate)
            Dim SettingWithCheckBoxStyle As DataTemplate = CType(SettingsWindow.Resources("SettingWithCheckBox"), DataTemplate)

            If SelectedListViewItem.ContentTemplate Is SettingWithCheckBoxStyle Then

                Select Case WindowTitle.Text
                    Case "Network"
                        If SelectedItem.SettingsTitle = "Connect to the Internet" Then

                            ChangeBoolValue(SelectedItem)

                            If SelectedItem.IsSettingChecked Then
                                Dim ListOfActiveEthernetDevices As List(Of EthernetDevice) = GetActiveEthernetDevices()
                                If ListOfActiveEthernetDevices IsNot Nothing AndAlso ListOfActiveEthernetDevices.Count > 0 Then
                                    For Each ActiveEthernetDevice In ListOfActiveEthernetDevices
                                        DisableEthernetDevice(ActiveEthernetDevice.Name)
                                    Next

                                    SelectedItem.IsSettingChecked = False
                                End If
                            Else
                                Dim ListOfDisabledEthernetDevices As List(Of EthernetDevice) = GetDisabledEthernetDevices()
                                If ListOfDisabledEthernetDevices IsNot Nothing AndAlso ListOfDisabledEthernetDevices.Count > 0 Then
                                    For Each DisabledEthernetDevice In ListOfDisabledEthernetDevices
                                        EnableEthernetDevice(DisabledEthernetDevice.Name)
                                    Next

                                    SelectedItem.IsSettingChecked = True
                                End If
                            End If

                        End If
                    Case Else
                        'Show visual change
                        If SelectedItem.IsSettingChecked Then
                            SelectedItem.IsSettingChecked = False
                        Else
                            SelectedItem.IsSettingChecked = True
                        End If

                        ChangeBoolValue(SelectedItem)
                End Select

            ElseIf SelectedListViewItem.ContentTemplate Is SettingWithDescription Then

                SettingToChange = SelectedItem

                Select Case WindowTitle.Text

                    'PS3 related settings
                    Case "PS3 Emulator (rpcs3)", "PS3 Audio Settings", "PS3 Video Settings", "PS3 Core Settings"
                        Select Case SelectedItem.SettingsTitle
                            'Known and pre-defined settings for the PS3 emulator
                            Case "Console Language", "License Area", "Enter button assignment", "Renderer", "Audio Format"
                                ShowHideSideMenuSettings()
                        End Select
                    Case Else
                        Select Case SelectedItem.SettingsTitle
                            Case "Duration of Notifications", "Selected Background", "Navigation Audio Pack", "Display Resolution", "Gamepad Button Layout"
                                'Show available options for specific settings
                                ShowHideSideMenuSettings()
                            Case Else
                                'Check if the selected setting got a value to change
                                If Not String.IsNullOrEmpty(SelectedItem.SettingsState) Then
                                    'For any other setting that has no boolean value or pre-defined list of settings -> Show an input field to change the setting manually
                                    'Show the input field
                                    Animate(NewInputBox, Canvas.TopProperty, 1085, 400, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(NewInputBox, Canvas.LeftProperty, 1925, 500, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(NewInputBox, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))

                                    'Pause the window keyboard input and focus the input field
                                    PauseInput = True
                                    NewInputBox.InputTextBox.Clear()
                                    NewInputBox.InputTextBox.Text = SelectedItem.SettingsState
                                    NewInputBox.InputTextBox.Focus()

                                    If MainController IsNot Nothing Then
                                        ShowVirtualKeyboard()
                                    End If
                                End If
                        End Select

                End Select

            End If

        ElseIf TypeOf FocusedItem Is Button Then

            Dim SelectedButton As Button = CType(FocusedItem, Button)
            Dim SelectedSettingValue As String = SelectedButton.Content.ToString()

            If SettingToChange IsNot Nothing Then
                'Save the new value
                ChangeValue(SettingToChange, SelectedSettingValue)

                'Special cases
                Select Case SettingToChange.SettingsTitle
                    Case "Selected Background", "Background Animation", "Background Music"
                        'Update background
                        SetBackground()

                        'Update background on MainWindow
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.MainWindow" Then
                                CType(Win, MainWindow).SetBackground()
                                Exit For
                            End If
                        Next
                    Case "Display Resolution"

                        If SelectedSettingValue = "AutoScaling" Then
                            MainConfigFile.IniWriteValue("System", "DisplayScaling", "AutoScaling")
                        Else
                            MainConfigFile.IniWriteValue("System", "DisplayScaling", "Manual")
                        End If

                        'Update background
                        SetBackground()

                        'Update background on MainWindow
                        For Each Win In System.Windows.Application.Current.Windows()
                            If Win.ToString = "OrbisPro.MainWindow" Then
                                CType(Win, MainWindow).SetBackground()
                                Exit For
                            End If
                        Next
                End Select

            End If

            'Remove the side menu
            ShowHideSideMenuSettings()
        End If
    End Sub

    Private Sub ReturnToPreviousSettings()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            If Not WindowTitle.Text = "Settings" Then
                'Don't play the back sound twice when returning to Home
                PlayBackgroundSound(Sounds.Back)
            End If

            'Get the ListView of the selected item
            Dim SelectedListViewItem As ListViewItem = CType(GeneralSettingsListView.SelectedItem, ListViewItem)
            Dim CurrentSelectedItem As SettingsListViewItem = CType(SelectedListViewItem.Content, SettingsListViewItem)

            Select Case WindowTitle.Text
                Case "Settings"
                    'Close if we're in the main settings
                    BeginAnimation(OpacityProperty, ClosingAnimation)

                Case "User Management"
                    LastAccountManagementSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)
                Case "Network"
                    LastNetworkSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)
                Case "Notifications"
                    LastNotificationsSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)
                Case "Emulators"
                    LastGeneralEmulatorSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)
                Case "System"
                    LastSystemSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)
                Case "Gamepad Settings"
                    LastGamepadSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadGeneralSettings()
                    GoBackAnimation(LastGeneralSettingsIndex)

                Case "Audio"
                    LastAudioSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadSystemSettings()
                    GoBackAnimation(LastSystemSettingsIndex)
                Case "Display"
                    LastDisplaySettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadSystemSettings()
                    GoBackAnimation(LastSystemSettingsIndex)
                Case "Background"
                    LastBackgroundSettingsIndex = GeneralSettingsListView.SelectedIndex
                    LoadDisplaySettings()
                    GoBackAnimation(LastDisplaySettingsIndex)


                Case "PS1 Emulator (ePSXe)", "PS2 Emulator (pcsx2)", "PS3 Emulator (rpcs3)", "PS4 Emulator (shadPS4)", "PSP Emulator (ppsspp)", "PS Vita Emulator (vita3k)", "Dolpin Emulator", "Sega Emulator (Fusion)", "Mednafen Emulator"
                    LoadGeneralEmulatorSettings()
                    GoBackAnimation(LastGeneralEmulatorSettingsIndex)


                Case "PS3 Audio Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(LastPS3EmulatorSettingsIndex)
                Case "PS3 Video Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(LastPS3EmulatorSettingsIndex)
                Case "PS3 Core Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(LastPS3EmulatorSettingsIndex)

                Case "PS4 GPU Settings"
                    LoadPS4EmulatorSettings()
                    GoBackAnimation(LastPS4EmulatorSettingsIndex)
                Case "PS4 Input Settings"
                    LoadPS4EmulatorSettings()
                    GoBackAnimation(LastPS4EmulatorSettingsIndex)
                Case "PS4 Other General Settings"
                    LoadPS4EmulatorSettings()
                    GoBackAnimation(LastPS4EmulatorSettingsIndex)
            End Select

        ElseIf TypeOf FocusedItem Is Button Then 'Close the side menu

            PlayBackgroundSound(Sounds.Back)

            Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            'Set the focus back
            Dim NextSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.Items(GeneralSettingsListView.SelectedIndex), ListViewItem)
            NextSelectedListViewItem.Focus()
        End If

    End Sub

    Private Sub ShowHideSideMenuSettings()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            Dim SelectedListViewItem As ListViewItem = CType(FocusedItem, ListViewItem)
            Dim SelectedItem As SettingsListViewItem = CType(SelectedListViewItem.Content, SettingsListViewItem)

            If Canvas.GetLeft(RightMenu) = 1925 Then
                'Show the side menu
                PlayBackgroundSound(Sounds.SelectItem)
                Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                Select Case SelectedItem.SettingsTitle
#Region "OrbisPro Settings"
                    Case "Duration of Notifications"
                        SettingButton1.Content = "1"
                        SettingButton2.Content = "2"
                        SettingButton3.Content = "3"
                        SettingButton4.Content = "4"
                        SettingButton5.Content = "5"

                        ShowSideSettingButtons(5)
                    Case "Selected Background"
                        SettingButton1.Content = "Blue Bubbles"
                        SettingButton2.Content = "Blue Bokeh Dust"
                        SettingButton3.Content = "Golden Dust"
                        SettingButton4.Content = "Orange/Red Gradient Waves"
                        SettingButton5.Content = "PS2 Dots"
                        SettingButton6.Content = "Custom"
                        SettingButton7.Content = "None"

                        ShowSideSettingButtons(7)
                    Case "Navigation Audio Pack"
                        SettingButton1.Content = "PS1"
                        SettingButton2.Content = "PS2"
                        SettingButton3.Content = "PS3"
                        SettingButton4.Content = "PS4"
                        SettingButton5.Content = "PS5"

                        ShowSideSettingButtons(5)
                    Case "Display Resolution"
                        SettingButton1.Content = "AutoScaling"
                        SettingButton2.Content = "1280x720"
                        SettingButton3.Content = "1920x1080"
                        SettingButton4.Content = "2560x1440"
                        SettingButton5.Content = "3840x2160"

                        ShowSideSettingButtons(5)
                    Case "Gamepad Button Layout"
                        SettingButton1.Content = "PS3"
                        SettingButton2.Content = "PS4"
                        SettingButton3.Content = "PS5"
                        SettingButton4.Content = "PS Vita"
                        SettingButton5.Content = "Steam"
                        SettingButton6.Content = "Steam Deck"
                        SettingButton7.Content = "Xbox 360"
                        SettingButton8.Content = "ROG Ally"

                        ShowSideSettingButtons(8)
#End Region
#Region "PS3 General Settings"
                    Case "Console Language"
                        SettingButton1.Content = "English (UK)"
                        SettingButton2.Content = "French"
                        SettingButton3.Content = "Spanish"
                        SettingButton4.Content = "German"
                        SettingButton5.Content = "Portuguese (Brazil)"

                        ShowSideSettingButtons(5)
                    Case "License Area"
                        SettingButton1.Content = "SCEA"
                        SettingButton2.Content = "SCEE"
                        SettingButton3.Content = "SCEJ"
                        SettingButton4.Content = "SCEH"
                        SettingButton5.Content = "SCH"

                        ShowSideSettingButtons(5)
                    Case "Enter button assignment"
                        SettingButton1.Content = "Enter with cross"
                        SettingButton2.Content = "Enter with circle"

                        ShowSideSettingButtons(2)
#End Region
#Region "PS3 Audio Settings"
                    Case "Renderer"
                        SettingButton1.Content = "Cubeb"
                        SettingButton2.Content = "XAudio2"
                        SettingButton3.Content = """Null"""

                        ShowSideSettingButtons(3)
                    Case "Audio Format"
                        SettingButton1.Content = "Stereo"
                        SettingButton2.Content = "Surround 5.1"
                        SettingButton3.Content = "Surround 7.1"
                        SettingButton4.Content = "Automatic"

                        ShowSideSettingButtons(4)
#End Region
#Region "PS3 Video Settings"

#End Region
#Region "PS3 Core Settings"

#End Region
                End Select

                'Set focus on the first available option
                SettingButton1.Focus()
            End If

        ElseIf TypeOf FocusedItem Is Button Then
            If Canvas.GetLeft(RightMenu) = 1430 Then
                'Hide the side menu
                PlayBackgroundSound(Sounds.SelectItem)
                Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton6, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton7, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton8, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton9, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton10, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                'Set the focus back
                Dim NextSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.Items(GeneralSettingsListView.SelectedIndex), ListViewItem)
                GeneralSettingsListView.Focus()
                NextSelectedListViewItem.Focus()
            End If
        End If

    End Sub

    Private Sub ShowSideSettingButtons(Count As Integer)
        For i = 1 To Count
            Dim SettingsButton As Button = CType(SettingsCanvas.FindName("SettingButton" + i.ToString()), Button)
            Animate(SettingsButton, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
        Next
    End Sub

    Private Sub MoveUp()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            PlayBackgroundSound(Sounds.Move)

            If SettingButton1.IsFocused Then
                SettingButton10.Focus()
            ElseIf SettingButton10.IsFocused Then
                SettingButton9.Focus()
            ElseIf SettingButton9.IsFocused Then
                SettingButton8.Focus()
            ElseIf SettingButton8.IsFocused Then
                SettingButton7.Focus()
            ElseIf SettingButton7.IsFocused Then
                SettingButton6.Focus()
            ElseIf SettingButton6.IsFocused Then
                SettingButton5.Focus()
            ElseIf SettingButton5.IsFocused Then
                SettingButton4.Focus()
            ElseIf SettingButton4.IsFocused Then
                SettingButton3.Focus()
            ElseIf SettingButton3.IsFocused Then
                SettingButton2.Focus()
            ElseIf SettingButton2.IsFocused Then
                SettingButton1.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            Dim SelectedIndex As Integer
            Dim NextIndex As Integer

            SelectedIndex = GeneralSettingsListView.SelectedIndex
            NextIndex = GeneralSettingsListView.SelectedIndex - 1

            If Not NextIndex = -1 Then
                GeneralSettingsListView.SelectedIndex -= 1
            End If

        End If

    End Sub

    Private Sub MoveDown()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            PlayBackgroundSound(Sounds.Move)

            If SettingButton1.IsFocused Then
                SettingButton2.Focus()
            ElseIf SettingButton2.IsFocused Then
                SettingButton3.Focus()
            ElseIf SettingButton3.IsFocused Then
                SettingButton4.Focus()
            ElseIf SettingButton4.IsFocused Then
                SettingButton5.Focus()
            ElseIf SettingButton5.IsFocused Then
                SettingButton6.Focus()
            ElseIf SettingButton6.IsFocused Then
                SettingButton7.Focus()
            ElseIf SettingButton7.IsFocused Then
                SettingButton8.Focus()
            ElseIf SettingButton8.IsFocused Then
                SettingButton9.Focus()
            ElseIf SettingButton9.IsFocused Then
                SettingButton10.Focus()
            ElseIf SettingButton10.IsFocused Then
                SettingButton1.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            Dim SelectedIndex As Integer
            Dim NextIndex As Integer
            Dim ItemCount As Integer

            SelectedIndex = GeneralSettingsListView.SelectedIndex
            NextIndex = GeneralSettingsListView.SelectedIndex + 1
            ItemCount = GeneralSettingsListView.Items.Count

            If Not NextIndex = ItemCount Then
                GeneralSettingsListView.SelectedIndex += 1
            End If

        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(GeneralSettingsListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(GeneralSettingsListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

#Region "Animations"

    Private Sub GoBackAnimation(PreviousIndexInListView As Integer)
        Animate(GeneralSettingsListView, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(400)))
        Animate(GeneralSettingsListView, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))

        GeneralSettingsListView.RenderTransform = New ScaleTransform()
        GeneralSettingsListView.RenderTransformOrigin = New Point(0.5, 0.5)
        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))
        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))

        GeneralSettingsListView.RenderTransform = New ScaleTransform()
        GeneralSettingsListView.RenderTransformOrigin = New Point(0.5, 0.5)
        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))
        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))

        Dim LastSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.ItemContainerGenerator.ContainerFromIndex(PreviousIndexInListView), ListViewItem)
        Dim LastSelectedItem As SettingsListViewItem = TryCast(LastSelectedListViewItem.Content, SettingsListViewItem)

        If LastSelectedItem IsNot Nothing Then

            LastSelectedItem.IsSettingSelected = Visibility.Visible
            LastSelectedListViewItem.Focus()

            GeneralSettingsListView.SelectedIndex = PreviousIndexInListView
        End If
    End Sub

    Private Sub GoForwardAnimation()
        Animate(GeneralSettingsListView, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(300)))

        GeneralSettingsListView.RenderTransform = New ScaleTransform()
        GeneralSettingsListView.RenderTransformOrigin = New Point(0.5, 0.5)

        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))
        GeneralSettingsListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))

        GeneralSettingsListView.Focus()

        Dim NextSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.Items(0), ListViewItem)
        Dim NextSelectedItem As SettingsListViewItem = TryCast(NextSelectedListViewItem.Content, SettingsListViewItem)

        GeneralSettingsListView.SelectedIndex = 0
        NextSelectedItem.IsSettingSelected = Visibility.Visible
        NextSelectedListViewItem.Focus()
    End Sub

#End Region

#Region "Input"

    Private Sub GeneralSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            If PauseInput = False Then
                Select Case e.Key
                    Case Key.C
                        ReturnToPreviousSettings()
                    Case Key.X
                        OpenNewSettings()
                    Case Key.Up
                        MoveUp()
                    Case Key.Down
                        MoveDown()
                End Select
            End If
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub GeneralSettings_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Sub NewGlobalKeyboardHook_KeyDown(Key As Forms.Keys) Handles NewGlobalKeyboardHook.KeyDown
        If PauseInput Then
            Select Case Key
                Case Forms.Keys.Return, Forms.Keys.Enter
                    If SettingToChange IsNot Nothing AndAlso Not String.IsNullOrEmpty(NewInputBox.InputTextBox.Text) Then
                        ChangeValue(SettingToChange, NewInputBox.InputTextBox.Text)
                    End If

                    'Remove the input field
                    Animate(NewInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(NewInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(NewInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    Dim LastSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.ItemContainerGenerator.ContainerFromIndex(GeneralSettingsListView.SelectedIndex), ListViewItem)
                    Dim LastSelectedItem As SettingsListViewItem = TryCast(LastSelectedListViewItem.Content, SettingsListViewItem)

                    If LastSelectedItem IsNot Nothing Then
                        LastSelectedItem.IsSettingSelected = Visibility.Visible
                        LastSelectedListViewItem.Focus()
                    End If

                    PauseInput = False
                Case Forms.Keys.Escape
                    'Remove the input field
                    Animate(NewInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(NewInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(NewInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    Dim LastSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.ItemContainerGenerator.ContainerFromIndex(GeneralSettingsListView.SelectedIndex), ListViewItem)
                    Dim LastSelectedItem As SettingsListViewItem = TryCast(LastSelectedListViewItem.Content, SettingsListViewItem)

                    If LastSelectedItem IsNot Nothing Then
                        LastSelectedItem.IsSettingSelected = Visibility.Visible
                        LastSelectedListViewItem.Focus()
                    End If

                    PauseInput = False
            End Select
        End If
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0

                Dim MainGamepadButton_RightThumbY_Up As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(32767)
                Dim MainGamepadButton_RightThumbY_Down As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(-32768)

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    OpenNewSettings()
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    ReturnToPreviousSettings()
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
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
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

#Region "Config Readers/Converters/Writers"

    Private Function GetBoolValue(ConfigValue As String) As Boolean
        Select Case ConfigValue.ToLower()
            Case "false", "off", "disabled", "0"
                Return False
            Case "true", "on", "enabled", "1"
                Return True
            Case Else
                Return False
        End Select
    End Function

    Private Function GetRegistryStringValue(RegPath As String, RegValueName As String) As String
        If Registry.CurrentUser.OpenSubKey(RegPath) IsNot Nothing Then
            If Registry.CurrentUser.OpenSubKey(RegPath).GetValue(RegValueName) IsNot Nothing Then
                Select Case Registry.CurrentUser.OpenSubKey(RegPath).GetValue(RegValueName).ToString()
                    Case "0"
                        Return "off"
                    Case "1"
                        Return "on"
                    Case Else
                        Return Registry.CurrentUser.OpenSubKey(RegPath).GetValue(RegValueName).ToString()
                End Select
            Else
                Return String.Empty
            End If
        Else
            Return String.Empty
        End If
    End Function

    Private Sub SetRegistryStringValue(RegPath As String, RegValueName As String, NewValue As String)
        If Registry.CurrentUser.OpenSubKey(RegPath) IsNot Nothing Then
            If Registry.CurrentUser.OpenSubKey(RegPath).GetValue(RegValueName) IsNot Nothing Then
                Registry.CurrentUser.OpenSubKey(RegPath, True).SetValue(RegValueName, NewValue)
            End If
        End If
    End Sub

    Private Function GetVitaConfigValue(ConfigName As String) As String
        Dim ConfigValue As String = ""

        For Each Line As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\config.yml")
            If Line.Contains(ConfigName) Then
                ConfigValue = Line.Split(":"c)(1)
                Exit For
            End If
        Next

        If Not String.IsNullOrEmpty(ConfigValue) Then
            Return ConfigValue
        Else
            Return String.Empty
        End If
    End Function

    Private Sub SetVitaConfigValue(ConfigName As String, NewValue As String)
        Dim NewConfigLines As New List(Of String)()

        For Each Line As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\config.yml")
            If Line.Contains(ConfigName) Then
                'Split the line of the config -> initial-setup(0): false(1)
                Dim SplittedLine As String() = Line.Split(":"c)
                'Replace the line with the new value -> initial-setup(0) + ": " + NewValue 
                NewConfigLines.Add(SplittedLine(0) + ": " + NewValue)
            Else
                'Add the other lines of the config file
                NewConfigLines.Add(Line)
            End If
        Next

        'Save as new config
        File.WriteAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\vita3k\config.yml", NewConfigLines.ToArray(), Encoding.UTF8)
    End Sub

    Private Function GetPS3ConfigValue(ConfigName As String) As String
        Dim ConfigValue As String = ""

        For Each Line As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\config.yml")
            If Line.Contains(ConfigName) Then
                ConfigValue = Line.Split(":"c)(1)
                Exit For
            End If
        Next

        If Not String.IsNullOrEmpty(ConfigValue) Then
            Return ConfigValue
        Else
            Return String.Empty
        End If
    End Function

    Private Sub SetPS3ConfigValue(ConfigName As String, NewValue As String)
        Dim NewConfigLines As New List(Of String)()

        For Each Line As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\config.yml")
            If Line.Contains(ConfigName) Then
                'Split the line of the config -> initial-setup(0): false(1)
                Dim SplittedLine As String() = Line.Split(":"c)
                'Replace the line with the new value -> initial-setup(0) + ": " + NewValue 
                NewConfigLines.Add(SplittedLine(0) + ": " + NewValue)
            Else
                'Add the other lines of the config file
                NewConfigLines.Add(Line)
            End If
        Next

        'Save as new config
        File.WriteAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\config.yml", NewConfigLines.ToArray(), Encoding.UTF8)
    End Sub

    Private Sub InsertFusionConfigSections()
        Dim FusionConfigLines() As String = File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.ini")

        'Add sections
        If Not FusionConfigLines(9) = "[Graphics]" Then
            FusionConfigLines(9) = "[Graphics]"
            FusionConfigLines(21) = "[Sound]"
            FusionConfigLines(27) = "[SG1000/SC3000/SMS/GG]"
            FusionConfigLines(49) = "[MegaDrive/Genesis]"
            FusionConfigLines(61) = "[MegaCD/SegaCD]"
            FusionConfigLines(79) = "[32X]"
            FusionConfigLines(89) = "[Expert]"
            FusionConfigLines(103) = "[FileHistory]"
            FusionConfigLines(125) = "[Netplay]"
            FusionConfigLines(135) = "[General]"
        End If

        'Save config
        File.WriteAllLines(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.ini", FusionConfigLines)
    End Sub

    Private Sub ChangeBoolValue(SelectedConfig As SettingsListViewItem)
        'Set the boolean value for the change
        Dim BooleanTrueValue As String
        Dim BooleanFalseValue As String

        Select Case WindowTitle.Text
            Case "Dolpin Emulator"
                BooleanTrueValue = "True"
                BooleanFalseValue = "False"

                Dim DolphinConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\User\Config\Dolphin.ini")
                If SelectedConfig.IsSettingChecked Then
                    DolphinConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    DolphinConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "PS1 Emulator (ePSXe)"
                BooleanTrueValue = "1"
                BooleanFalseValue = "0"

                If SelectedConfig.IsSettingChecked Then
                    SetRegistryStringValue("Software\epsxe\config", SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    SetRegistryStringValue("Software\epsxe\config", SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "PS2 Emulator (pcsx2)"
                BooleanTrueValue = "enabled"
                BooleanFalseValue = "disabled"

                Dim PCSX2Config As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\inis\PCSX2_ui.ini")
                If SelectedConfig.IsSettingChecked Then
                    PCSX2Config.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    PCSX2Config.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "PS3 Emulator (rpcs3)", "PS3 Audio Settings", "PS3 Video Settings", "PS3 Core Settings"
                BooleanTrueValue = "true"
                BooleanFalseValue = "false"

                If SelectedConfig.IsSettingChecked Then
                    SetPS3ConfigValue(SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    SetPS3ConfigValue(SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "PS4 Emulator (shadPS4)", "PS4 Other General Settings", "PS4 Input Settings", "PS4 GPU Settings"
                Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
                Dim ConfigContent As String = File.ReadAllText(ConfigPath)
                Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
                Dim GeneralTable As TomlTable = CType(ConfigTable("General"), TomlTable)
                Dim GUITable As TomlTable = CType(ConfigTable("GUI"), TomlTable)
                Dim GPUTable As TomlTable = CType(ConfigTable("GPU"), TomlTable)
                Dim InputTable As TomlTable = CType(ConfigTable("Input"), TomlTable)

                If SelectedConfig.IsSettingChecked Then
                    If GeneralTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GeneralTable(SelectedConfig.ConfigToChange) = True
                        ConfigTable("General") = GeneralTable
                    ElseIf GUITable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GUITable(SelectedConfig.ConfigToChange) = True
                        ConfigTable("GUI") = GUITable
                    ElseIf InputTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        InputTable(SelectedConfig.ConfigToChange) = True
                        ConfigTable("Input") = InputTable
                    ElseIf GPUTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GPUTable(SelectedConfig.ConfigToChange) = True
                        ConfigTable("GPU") = GPUTable
                    End If
                Else
                    If GeneralTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GeneralTable(SelectedConfig.ConfigToChange) = False
                        ConfigTable("General") = GeneralTable
                    ElseIf GUITable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GUITable(SelectedConfig.ConfigToChange) = False
                        ConfigTable("GUI") = GUITable
                    ElseIf InputTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        InputTable(SelectedConfig.ConfigToChange) = False
                        ConfigTable("Input") = InputTable
                    ElseIf GPUTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                        GPUTable(SelectedConfig.ConfigToChange) = False
                        ConfigTable("GPU") = GPUTable
                    End If
                End If

                Dim ModifiedConfigContent As String = Toml.FromModel(ConfigTable)
                File.WriteAllText(ConfigPath, ModifiedConfigContent)
            Case "PSP Emulator (ppsspp)"
                BooleanTrueValue = "True"
                BooleanFalseValue = "False"

                Dim PPSSPPConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\memstick\PSP\SYSTEM\ppsspp.ini")
                If SelectedConfig.IsSettingChecked Then
                    PPSSPPConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    PPSSPPConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "PS Vita Emulator (vita3k)"
                BooleanTrueValue = "true"
                BooleanFalseValue = "false"

                If SelectedConfig.IsSettingChecked Then
                    SetVitaConfigValue(SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    SetVitaConfigValue(SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "Sega Emulator (Fusion)"
                BooleanTrueValue = "1"
                BooleanFalseValue = "0"

                Dim FusionConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.ini")
                If SelectedConfig.IsSettingChecked Then
                    FusionConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    FusionConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
            Case "Mednafen Emulator"
                'BooleanTrueValue = "1"
                'BooleanFalseValue = "0"
                'Future build

            Case "Notifications", "Notifications", "Background", "Network"
                BooleanTrueValue = "true"
                BooleanFalseValue = "false"

                If SelectedConfig.IsSettingChecked Then
                    MainConfigFile.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanFalseValue)
                Else
                    MainConfigFile.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, BooleanTrueValue)
                End If
        End Select
    End Sub

    Private Sub ChangeValue(SelectedConfig As SettingsListViewItem, NewValue As String)
        Select Case WindowTitle.Text
            Case "Dolpin Emulator"
                Dim DolphinConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Dolphin\User\Config\Dolphin.ini")
                DolphinConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "PS1 Emulator (ePSXe)"
                SetRegistryStringValue("Software\epsxe\config", SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "PS2 Emulator (pcsx2)"
                Dim PCSX2Config As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\PCSX2\inis\PCSX2_ui.ini")
                PCSX2Config.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "PS3 Emulator (rpcs3)", "PS3 Audio Settings", "PS3 Video Settings", "PS3 Core Settings"
                SetPS3ConfigValue(SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "PS4 Emulator (shadPS4)", "PS4 GPU Settings", "PS4 Input Settings", "PS4 Other General Settings"
                'Check value and convert if required
                Dim NewIntValue As Integer
                If Integer.TryParse(NewValue, NewIntValue) Then
                    'Change an Integer value
                    ChangePS4ConfigValue(SettingToChange, NewIntValue)
                Else
                    Dim NewDoubleValue As Double
                    If Double.TryParse(NewValue, NewDoubleValue) Then
                        'Change a Double value
                        ChangePS4ConfigValue(SettingToChange, NewDoubleValue)
                    Else
                        'Change a String value
                        ChangePS4ConfigValue(SettingToChange, NewValue)
                    End If
                End If
            Case "PSP Emulator (ppsspp)"
                Dim PPSSPPConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\ppsspp\memstick\PSP\SYSTEM\ppsspp.ini")
                PPSSPPConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "PS Vita Emulator (vita3k)"
                SetVitaConfigValue(SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "Sega Emulator (Fusion)"
                Dim FusionConfig As New IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\Fusion\Fusion.ini")
                FusionConfig.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
            Case "Mednafen Emulator"
                'Future build

            Case "User Management", "Audio", "Background", "Display", "Network", "Notifications", "System"
                MainConfigFile.IniWriteValue(SelectedConfig.ConfigSectionName, SelectedConfig.ConfigToChange, NewValue)
                SelectedConfig.SettingsState = NewValue
        End Select
    End Sub

    Private Sub ChangePS4ConfigValue(SelectedConfig As SettingsListViewItem, NewObjectValue As Object)
        If NewObjectValue IsNot Nothing Then
            Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
            Dim ConfigContent As String = File.ReadAllText(ConfigPath)
            Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()
            Dim GeneralTable As TomlTable = CType(ConfigTable("General"), TomlTable)
            Dim GUITable As TomlTable = CType(ConfigTable("GUI"), TomlTable)
            Dim GPUTable As TomlTable = CType(ConfigTable("GPU"), TomlTable)
            Dim InputTable As TomlTable = CType(ConfigTable("Input"), TomlTable)

            If GeneralTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                GeneralTable(SelectedConfig.ConfigToChange) = NewObjectValue
                ConfigTable("General") = GeneralTable
            ElseIf GUITable.ContainsKey(SelectedConfig.ConfigToChange) Then
                GUITable(SelectedConfig.ConfigToChange) = NewObjectValue
                ConfigTable("GUI") = GUITable
            ElseIf InputTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                InputTable(SelectedConfig.ConfigToChange) = NewObjectValue
                ConfigTable("Input") = InputTable
            ElseIf GPUTable.ContainsKey(SelectedConfig.ConfigToChange) Then
                GPUTable(SelectedConfig.ConfigToChange) = NewObjectValue
                ConfigTable("GPU") = GPUTable
            End If

            Dim ModifiedConfigContent As String = Toml.FromModel(ConfigTable)
            File.WriteAllText(ConfigPath, ModifiedConfigContent)

            SelectedConfig.SettingsState = NewObjectValue.ToString()
        End If
    End Sub

    Private Function GetTOMLValue(ConfigName As String) As Object
        Dim ConfigPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\user\config.toml"
        Dim ConfigContent As String = File.ReadAllText(ConfigPath)
        Dim ConfigTable As TomlTable = Toml.Parse(ConfigContent).ToModel()

        If ConfigTable.ContainsKey(ConfigName) Then
            Return ConfigTable(ConfigName)
        Else
            Return Nothing
        End If
    End Function

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

                OrbisDisplay.SetScaling(SettingsWindow, SettingsCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SettingsWindow, SettingsCanvas)
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
            .Opener = "GeneralSettings",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
