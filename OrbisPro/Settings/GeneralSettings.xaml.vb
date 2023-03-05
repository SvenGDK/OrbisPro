Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisAnimations
Imports XInput.Wrapper

Public Class GeneralSettings

    Dim ConfigFile As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\Config\Settings.ini")

    Dim LastSettingsIndex As Integer
    Dim LastGeneralSettingsIndex As Integer
    Dim LastAudioSettingsIndex As Integer
    Dim LastBackgroundSettingsIndex As Integer
    Dim LastGeneralEmulatorSettingsIndex As Integer
    Dim LastPS3EmulatorSettingsIndex As Integer
    Dim LastPS3AudioSettingsIndex As Integer

    Dim LastSettingsWindowTitle As String
    Dim LastListView As ListView
    Dim LastListViewItem As SettingsListViewItem

    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Dim WithEvents CurrentController As X.Gamepad

    'Get connected controllers
    Public Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            X.UpdatesPerSecond = 13 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    Private Sub GeneralSettings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GetAttachedControllers()
        LoadGeneralSettings()
        ShowListAnimation(GeneralSettingsListView)

        Dim NextSelectedItem As SettingsListViewItem = TryCast(GeneralSettingsListView.Items(0).Content, SettingsListViewItem)
        NextSelectedItem.IsSettingSelected = Visibility.Visible

        Dim NextSelectedListViewItem As ListViewItem = TryCast(GeneralSettingsListView.Items(0), ListViewItem)
        NextSelectedListViewItem.Focus()
    End Sub

    Public Sub LoadGeneralSettings()

        WindowTitle.Content = "General Settings"

        GeneralSettingsListView.Items.Clear()

        Dim SettingWithDescription As DataTemplate = SettingsWindow.Resources("SettingWithDescription")

        Dim GeneralSetting1 As New SettingsListViewItem With {.SettingsTitle = "Audio Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralSetting2 As New SettingsListViewItem With {.SettingsTitle = "Video Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/Video.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralSetting3 As New SettingsListViewItem With {.SettingsTitle = "Background Settings", .SettingsIcon = New BitmapImage(New Uri("/Icons/GalleryTransparent.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralSetting4 As New SettingsListViewItem With {.SettingsTitle = "Emulator Settings", .SettingsDescription = "Here you can change each emulator's settings.", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralSetting5 As New SettingsListViewItem With {.SettingsTitle = "System Settings", .SettingsDescription = "Change OrbisPro system settings.", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute))}
        Dim GeneralSetting6 As New SettingsListViewItem With {.SettingsTitle = "System Update", .SettingsDescription = "No Update available." + vbCrLf + "This feature is not enabled yet.", .SettingsIcon = New BitmapImage(New Uri("/Icons/Update.png", UriKind.RelativeOrAbsolute))}

        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralSetting6}

        GeneralSettingsListView.Items.Add(Setting1Item)
        GeneralSettingsListView.Items.Add(Setting2Item)
        GeneralSettingsListView.Items.Add(Setting3Item)
        GeneralSettingsListView.Items.Add(Setting4Item)
        GeneralSettingsListView.Items.Add(Setting5Item)

        GeneralSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadAudioSettings()

        WindowTitle.Content = "Audio Settings"

        AudioSettingsListView.Items.Clear()

        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")
        Dim SettingWithAudioControlStyle As DataTemplate = SettingsWindow.Resources("SettingWithAudioControl")
        Dim SettingWithDescription As DataTemplate = SettingsWindow.Resources("SettingWithDescription")

        'Declare the settings
        Dim AudioSetting1 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "System Volume"}
        Dim AudioSetting2 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Notification Volume"}
        Dim AudioSetting3 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Background Video Audio"}
        Dim AudioSetting4 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Background Video Audio Volume"}
        Dim AudioSetting5 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Navigation Audio Pack",
            .SettingsState = ConfigFile.IniReadValue("Audio Settings", "Navigation Audio Pack")}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = AudioSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = AudioSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = AudioSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = AudioSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = AudioSetting5}

        'Add the settings to the list
        AudioSettingsListView.Items.Add(Setting2Item)
        AudioSettingsListView.Items.Add(Setting3Item)
        AudioSettingsListView.Items.Add(Setting4Item)
        AudioSettingsListView.Items.Add(Setting5Item)

        AudioSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadBackgroundSettings()

        WindowTitle.Content = "Background Settings"

        BackgroundSettingsListView.Items.Clear()

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

        'Declare the settings
        Dim BackgroundSetting1 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Video.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Use Video Background", .IsSettingChecked = True}
        Dim BackgroundSetting2 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Use Custom Background", .IsSettingChecked = False}
        Dim BackgroundSetting3 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "Selected Background"}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = BackgroundSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = BackgroundSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = BackgroundSetting3}

        'Add the settings to the list
        BackgroundSettingsListView.Items.Add(Setting1Item)
        BackgroundSettingsListView.Items.Add(Setting2Item)
        BackgroundSettingsListView.Items.Add(Setting3Item)

        BackgroundSettingsListView.Items.Refresh()

    End Sub

    Public Sub LoadGeneralEmulatorSettings()
        EmulatorSettingsListView.Items.Clear()

        WindowTitle.Content = "General Emulator Settings"

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "List of Installed Emulators"}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS1 Emulator Settings"}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS2 Emulator Settings"}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)), .SettingsTitle = "PS3 Emulator Settings"}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting4}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting2Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)

        EmulatorSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS1EmulatorSettings()

        WindowTitle.Content = "PS1 Settings"

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Use HLE Bios", .IsSettingSelected = Visibility.Visible}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "Selected Bios", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Resolution", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Show FPS", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Use VSync", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_devices.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Configure Keyboard / Gamepad",
            .IsSettingSelected = Visibility.Hidden}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting6Item)
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting2Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)
        EmulatorSettingsListView.Items.Add(Setting5Item)

        EmulatorSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS2EmulatorSettings()

    End Sub

    Public Sub LoadPS3EmulatorSettings()
        EmulatorSettingsListView.Items.Clear()

        WindowTitle.Content = "PS3 Emulator Settings"

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithDescription As DataTemplate = SettingsWindow.Resources("SettingWithDescription")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

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

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting2Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)
        EmulatorSettingsListView.Items.Add(Setting5Item)
        EmulatorSettingsListView.Items.Add(Setting6Item)

        EmulatorSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS3AudioSettings()
        EmulatorSettingsListView.Items.Clear()

        WindowTitle.Content = "PS3 Audio Settings"

        Dim SettingWithDescription As DataTemplate = SettingsWindow.Resources("SettingWithDescription")
        Dim SettingWithAudioControlStyle As DataTemplate = SettingsWindow.Resources("SettingWithAudioControl")

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Renderer", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Cubeb is the recommended option and should be used whenever possible." + vbCrLf + "Windows: XAudio2 is the next best alternative." + vbCrLf + "Linux: PulseAudio is the next best alternative",
            .SettingsState = "Cubeb"}

        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Convert To 16 Bit", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Uses 16-bit audio samples instead of default 32-bit floating point. Use with buggy audio drivers if you have no sound or completely broken sound.",
            .SettingsState = "Off"}

        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio Format", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Determines the sound format." + vbCrLf + "Configure this setting if you want to switch between stereo and surround sound." + vbCrLf + "The manual setting will use your selected formats while the automatic setting will let the game choose from all available formats.",
            .SettingsState = "Stereo"}

        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Audio Device", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Determines which audio device to use.",
            .SettingsState = "Default"}

        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsTitle = "Master Volume", .SettingsIcon = New BitmapImage(New Uri("/Icons/Music.png", UriKind.RelativeOrAbsolute))}

        Dim GeneralEmulatorSetting7 As New SettingsListViewItem With {.SettingsTitle = "Audio Buffer Duration", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Target buffer duration in milliseconds. Higher values make the buffering algorithm's job easier, but may introduce noticeable audio latency." + vbCrLf + "Note that you can use keyboard arrow keys for precise changes on the slide bars.",
            .SettingsState = "100ms"}

        Dim GeneralEmulatorSetting8 As New SettingsListViewItem With {.SettingsTitle = "Enable Time Stretching", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Reduces crackle and stutter further, but may cause a very noticeable reduction in audio quality on slower CPUs." + vbCrLf + "Requires audio buffering to be enabled.",
            .SettingsState = "Off"}

        Dim GeneralEmulatorSetting9 As New SettingsListViewItem With {.SettingsTitle = "Time Stretching Threshold", .SettingsIcon = New BitmapImage(New Uri("/Icons/Setting.png", UriKind.RelativeOrAbsolute)),
            .SettingsDescription = "Buffer fill level (in percentage) below which time stretching will start." + vbCrLf + "Note that you can use keyboard arrow keys for precise changes on the slide bars.",
            .SettingsState = "75%"}


        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting1}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = SettingWithAudioControlStyle, .Content = GeneralEmulatorSetting6}
        Dim Setting7Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting7}
        Dim Setting8Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting8}
        Dim Setting9Item As New ListViewItem With {.ContentTemplate = SettingWithDescription, .Content = GeneralEmulatorSetting9}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)
        EmulatorSettingsListView.Items.Add(Setting5Item)
        EmulatorSettingsListView.Items.Add(Setting6Item)
        EmulatorSettingsListView.Items.Add(Setting7Item)
        EmulatorSettingsListView.Items.Add(Setting8Item)
        EmulatorSettingsListView.Items.Add(Setting9Item)

        EmulatorSettingsListView.Items.Refresh()
    End Sub

    Public Sub LoadPS3VideoSettings()
        EmulatorSettingsListView.Items.Clear()

        WindowTitle.Content = "PS3 Video Settings"

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Console Language", .IsSettingSelected = Visibility.Visible, .SettingsState = "English (US)"}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "License Area", .IsSettingSelected = Visibility.Hidden, .SettingsState = "SCEE"}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Enter button assignment", .IsSettingSelected = Visibility.Hidden, .SettingsState = "Cross"}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Video Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_devices.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Core Settings",
            .IsSettingSelected = Visibility.Hidden}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting6Item)
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting2Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)
        EmulatorSettingsListView.Items.Add(Setting5Item)

        EmulatorSettingsListView.Items.Refresh()
        EmulatorSettingsListView.Focus()
    End Sub

    Public Sub LoadPS3CoreSettings()
        EmulatorSettingsListView.Items.Clear()

        WindowTitle.Content = "PS3 Core Settings"

        Dim DefaultSettingStyle As DataTemplate = SettingsWindow.Resources("DefaultSetting")
        Dim SettingWithCheckBoxStyle As DataTemplate = SettingsWindow.Resources("SettingWithCheckBox")

        'Declare the settings
        Dim GeneralEmulatorSetting1 As New SettingsListViewItem With {.SettingsTitle = "Console Language", .IsSettingSelected = Visibility.Visible}
        Dim GeneralEmulatorSetting2 As New SettingsListViewItem With {.SettingsTitle = "License Area", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting3 As New SettingsListViewItem With {.SettingsTitle = "Enter button assignment", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting4 As New SettingsListViewItem With {.SettingsTitle = "Audio Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting5 As New SettingsListViewItem With {.SettingsTitle = "Video Settings", .IsSettingSelected = Visibility.Hidden}
        Dim GeneralEmulatorSetting6 As New SettingsListViewItem With {.SettingsIcon = New BitmapImage(New Uri("/Icons/quickmenu_devices.png", UriKind.RelativeOrAbsolute)),
            .SettingsTitle = "Core Settings",
            .IsSettingSelected = Visibility.Hidden}

        'Here we create a new ListViewItem and override the content template style
        Dim Setting1Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting1}
        Dim Setting2Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting2}
        Dim Setting3Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting3}
        Dim Setting4Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting4}
        Dim Setting5Item As New ListViewItem With {.ContentTemplate = SettingWithCheckBoxStyle, .Content = GeneralEmulatorSetting5}
        Dim Setting6Item As New ListViewItem With {.ContentTemplate = DefaultSettingStyle, .Content = GeneralEmulatorSetting6}

        'Add the settings to the list
        EmulatorSettingsListView.Items.Add(Setting6Item)
        EmulatorSettingsListView.Items.Add(Setting1Item)
        EmulatorSettingsListView.Items.Add(Setting2Item)
        EmulatorSettingsListView.Items.Add(Setting3Item)
        EmulatorSettingsListView.Items.Add(Setting4Item)
        EmulatorSettingsListView.Items.Add(Setting5Item)

        EmulatorSettingsListView.Items.Refresh()
    End Sub

    Private Sub GeneralSettingsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles GeneralSettingsListView.SelectionChanged
        If GeneralSettingsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem = CType(e.RemovedItems(0).Content, SettingsListViewItem)
            Dim SelectedItem = CType(e.AddedItems(0).Content, SettingsListViewItem)

            SelectedItem.IsSettingSelected = Visibility.Visible
            PreviousItem.IsSettingSelected = Visibility.Hidden

            If Not String.IsNullOrEmpty(SelectedItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.AddedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 100, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemBorder, HeightProperty, 75, 200, New Duration(TimeSpan.FromMilliseconds(400)))

                    'Animate(SelectedItemImage, HeightProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 97, 197, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

            End If

            If Not String.IsNullOrEmpty(PreviousItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.RemovedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    'Previous item needs to be adjusted a bit faster
                    Animate(SelectedItemCanvas, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemBorder, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))

                    'Animate(SelectedItemImage, HeightProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 197, 97, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If
        End If
    End Sub

    Private Sub BackgroundSettingsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles BackgroundSettingsListView.SelectionChanged
        If BackgroundSettingsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem = CType(e.RemovedItems(0).Content, SettingsListViewItem)
            Dim SelectedItem = CType(e.AddedItems(0).Content, SettingsListViewItem)

            SelectedItem.IsSettingSelected = Visibility.Visible
            PreviousItem.IsSettingSelected = Visibility.Hidden

            If Not String.IsNullOrEmpty(SelectedItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.AddedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 100, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemBorder, HeightProperty, 75, 200, New Duration(TimeSpan.FromMilliseconds(400)))

                    'Animate(SelectedItemImage, HeightProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 97, 197, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

            End If

            If Not String.IsNullOrEmpty(PreviousItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.RemovedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    'Previous item needs to be adjusted a bit faster
                    Animate(SelectedItemCanvas, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemBorder, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))

                    'Animate(SelectedItemImage, HeightProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 197, 97, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If
        End If
    End Sub

    Private Sub AudioSettingsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles AudioSettingsListView.SelectionChanged
        If AudioSettingsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem = CType(e.RemovedItems(0).Content, SettingsListViewItem)
            Dim SelectedItem = CType(e.AddedItems(0).Content, SettingsListViewItem)

            SelectedItem.IsSettingSelected = Visibility.Visible
            PreviousItem.IsSettingSelected = Visibility.Hidden

            If Not String.IsNullOrEmpty(SelectedItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.AddedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 100, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemBorder, HeightProperty, 75, 200, New Duration(TimeSpan.FromMilliseconds(400)))

                    'Animate(SelectedItemImage, HeightProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 97, 197, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

            End If

            If Not String.IsNullOrEmpty(PreviousItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.RemovedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    'Previous item needs to be adjusted a bit faster
                    Animate(SelectedItemCanvas, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemBorder, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))

                    'Animate(SelectedItemImage, HeightProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 197, 97, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If
        End If
    End Sub

    Private Sub EmulatorSettingsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles EmulatorSettingsListView.SelectionChanged
        If EmulatorSettingsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem = CType(e.RemovedItems(0).Content, SettingsListViewItem)
            Dim SelectedItem = CType(e.AddedItems(0).Content, SettingsListViewItem)

            SelectedItem.IsSettingSelected = Visibility.Visible
            PreviousItem.IsSettingSelected = Visibility.Hidden

            If Not String.IsNullOrEmpty(SelectedItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.AddedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    Animate(SelectedItemCanvas, HeightProperty, 100, 200, New Duration(TimeSpan.FromMilliseconds(400)))
                    Animate(SelectedItemBorder, HeightProperty, 75, 200, New Duration(TimeSpan.FromMilliseconds(400)))

                    'Animate(SelectedItemImage, HeightProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 64, 100, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 97, 197, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))
                End If

            End If

            If Not String.IsNullOrEmpty(PreviousItem.SettingsDescription) Then
                Dim SettingContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.RemovedItems(0))
                Dim SettingUsedDataTemplate As DataTemplate = SettingContentPresenter.ContentTemplate

                Dim SelectedItemCanvas As Canvas = TryCast(SettingUsedDataTemplate.FindName("SettingCanvas", SettingContentPresenter), Canvas)
                Dim SelectedItemBorder As Border = TryCast(SettingUsedDataTemplate.FindName("SelectedBorder", SettingContentPresenter), Border)
                Dim SelectedItemSeparator As Separator = TryCast(SettingUsedDataTemplate.FindName("SettingSeparator", SettingContentPresenter), Separator)
                Dim SelectedItemDescription As TextBlock = TryCast(SettingUsedDataTemplate.FindName("SettingDescription", SettingContentPresenter), TextBlock)
                Dim SelectedItemImage As Image = TryCast(SettingUsedDataTemplate.FindName("SettingIcon", SettingContentPresenter), Image)

                If SelectedItemBorder IsNot Nothing Then
                    'Previous item needs to be adjusted a bit faster
                    Animate(SelectedItemCanvas, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))
                    Animate(SelectedItemBorder, HeightProperty, 200, 100, New Duration(TimeSpan.FromMilliseconds(100)))

                    'Animate(SelectedItemImage, HeightProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))
                    'Animate(SelectedItemImage, WidthProperty, 100, 64, New Duration(TimeSpan.FromMilliseconds(400)))

                    Animate(SelectedItemSeparator, Canvas.TopProperty, 197, 97, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

                If SelectedItemDescription IsNot Nothing Then
                    Animate(SelectedItemDescription, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(100)))
                End If

            End If

        End If

    End Sub

    Private Sub GeneralSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.X Then
            OpenNewSettings()
        ElseIf e.Key = Key.O Then
            ReturnToPreviousSettings()
        ElseIf e.Key = Key.Up Then
            MoveUp()
        ElseIf e.Key = Key.Down Then
            MoveDown()
        End If

    End Sub

    Public Sub SaveSetting()

    End Sub

    Private Sub OpenNewSettings()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
            Dim SelectedItem As SettingsListViewItem = CType(CurrentListView.SelectedItem.Content, SettingsListViewItem)

            Select Case SelectedItem.SettingsTitle
                Case "General Settings"
                    LastGeneralSettingsIndex = CurrentListView.SelectedIndex
                    LoadGeneralSettings()
                    GoForwardAnimation(CurrentListView, GeneralSettingsListView)
                Case "Audio Settings"
                    LastAudioSettingsIndex = CurrentListView.SelectedIndex
                    LoadAudioSettings()
                    GoForwardAnimation(CurrentListView, AudioSettingsListView)
                Case "Background Settings"
                    LastBackgroundSettingsIndex = CurrentListView.SelectedIndex
                    LoadBackgroundSettings()
                    GoForwardAnimation(CurrentListView, BackgroundSettingsListView)
                Case "Emulator Settings"
                    LastGeneralEmulatorSettingsIndex = CurrentListView.SelectedIndex
                    LoadGeneralEmulatorSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS1 Emulator Settings"
                    LoadPS1EmulatorSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS2 Emulator Settings"
                    LoadPS2EmulatorSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS3 Emulator Settings"
                    LastPS3EmulatorSettingsIndex = CurrentListView.SelectedIndex
                    LoadPS3EmulatorSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS3 Audio Settings"
                    LastPS3AudioSettingsIndex = CurrentListView.SelectedIndex
                    LoadPS3AudioSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS3 Video Settings"
                    LoadPS3VideoSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
                Case "PS3 Core Settings"
                    LoadPS3CoreSettings()
                    GoForwardAnimation(CurrentListView, EmulatorSettingsListView)
            End Select

        ElseIf TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim SelectedSetting = SelectedButton.Content.ToString

            LastListViewItem.SettingsState = SelectedSetting
            ConfigFile.IniWriteValue("Audio Settings", LastListViewItem.SettingsTitle, SelectedSetting)

        End If
    End Sub

    Private Sub OpenSideMenuSettings()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then
            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
            Dim SelectedItem = CType(CurrentListView.SelectedItem.Content, SettingsListViewItem)

            LastListView = CurrentListView
            LastListViewItem = SelectedItem

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

            If Canvas.GetLeft(RightMenu) = 1925 Then
                Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton5, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                Select Case SelectedItem.SettingsTitle
                    Case "Navigation Audio Pack"
                        SettingButton1.Content = "PS1"
                        SettingButton2.Content = "PS2"
                        SettingButton3.Content = "PS3"
                        SettingButton4.Content = "PS4"
                        SettingButton5.Content = "PS5"
                    Case "PS3 Audio Renderer"
                        SettingButton1.Content = "Cubeb"
                        SettingButton2.Content = "XAudio2"
                        SettingButton3.Content = "Disable Audio"
                        SettingButton4.Content = ""
                        SettingButton5.Content = ""
                    Case "PS3 Audio Format"
                        SettingButton1.Content = "Stereo"
                        SettingButton2.Content = "Surround 5.1"
                        SettingButton3.Content = "Surround 7.1"
                        SettingButton4.Content = "Automatic"
                        SettingButton5.Content = ""
                End Select

                Dim SettingsButton As Button

                For Each SettingButton In SettingsCanvas.Children
                    If TypeOf SettingButton Is Button Then

                        SettingsButton = CType(SettingButton, Button)

                        'Check if the setting exists in the list
                        If SettingsButton.Content.ToString = SelectedItem.SettingsState Then

                            SettingsButton = CType(SettingButton, Button)

                            SettingButton1.BorderBrush = Brushes.Transparent
                            SettingButton1.BorderThickness = New Thickness(0, 0, 0, 0)

                            'Select the setting that has been set
                            SettingsButton.BorderBrush = Brushes.White
                            SettingsButton.BorderThickness = New Thickness(3, 3, 3, 3)

                            Exit For
                        End If

                    End If

                Next

                'Focus the right item
                If SettingsButton IsNot Nothing Then
                    SettingsButton.Focus()
                Else
                    SettingButton1.Focus()
                End If

            End If

        End If
    End Sub

    Private Sub ReturnToPreviousSettings()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
            Dim CurrentSelectedItem As SettingsListViewItem = CType(CurrentListView.SelectedItem.Content, SettingsListViewItem)

            Select Case WindowTitle.Content.ToString
                Case "General Settings"
                    'Close the settings
                    OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
                    BeginAnimation(OpacityProperty, ClosingAnim)
                Case "Audio Settings"
                    LoadGeneralSettings()
                    GoBackAnimation(CurrentListView, GeneralSettingsListView, LastGeneralSettingsIndex)
                Case "Background Settings"
                    LoadGeneralSettings()
                    GoBackAnimation(CurrentListView, GeneralSettingsListView, LastGeneralSettingsIndex)
                Case "General Emulator Settings"
                    LoadGeneralSettings()
                    GoBackAnimation(CurrentListView, GeneralSettingsListView, LastGeneralSettingsIndex)
                Case "PS1 Emulator Settings"
                    LoadPS1EmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastSettingsIndex)
                Case "PS2 Emulator Settings"
                    LoadPS2EmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastSettingsIndex)
                Case "PS3 Emulator Settings"
                    LoadGeneralEmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastGeneralEmulatorSettingsIndex)
                Case "PS3 Audio Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastPS3EmulatorSettingsIndex)
                Case "PS3 Video Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastSettingsIndex)
                Case "PS3 Core Settings"
                    LoadPS3EmulatorSettings()
                    GoBackAnimation(CurrentListView, EmulatorSettingsListView, LastSettingsIndex)
            End Select

        ElseIf TypeOf FocusedItem Is Button Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

            Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            'Set the focus back
            Dim NextSelectedListViewItem As ListViewItem = TryCast(LastListView.Items(LastListView.SelectedIndex), ListViewItem)
            NextSelectedListViewItem.Focus()
        End If
    End Sub

    Private Sub MoveUp()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim SelectedButtonNumber As Integer = GetIntOnly(SelectedButton.Name) - 1
            Dim NextButton As Button = CType(SettingsCanvas.FindName("SettingButton" + SelectedButtonNumber.ToString), Button)

            If NextButton IsNot Nothing Then
                SelectedButton.BorderBrush = Brushes.Transparent
                SelectedButton.BorderThickness = New Thickness(0, 0, 0, 0)

                NextButton.BorderBrush = Brushes.White
                NextButton.BorderThickness = New Thickness(3, 3, 3, 3)
            End If

        ElseIf TypeOf FocusedItem Is ListViewitem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
            Dim SelectedIndex As Integer
            Dim NextIndex As Integer

            'We are in the Devices ListView
            If CurrentListView.Name = "GeneralSettingsListView" Then
                SelectedIndex = GeneralSettingsListView.SelectedIndex
                NextIndex = GeneralSettingsListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    GeneralSettingsListView.SelectedIndex -= 1
                End If
            ElseIf CurrentListView.Name = "AudioSettingsListView" Then
                SelectedIndex = AudioSettingsListView.SelectedIndex
                NextIndex = AudioSettingsListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    AudioSettingsListView.SelectedIndex -= 1
                End If
            ElseIf CurrentListView.Name = "BackgroundSettingsListView" Then
                SelectedIndex = BackgroundSettingsListView.SelectedIndex
                NextIndex = BackgroundSettingsListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    BackgroundSettingsListView.SelectedIndex -= 1
                End If
            ElseIf CurrentListView.Name = "EmulatorSettingsListView" Then
                SelectedIndex = EmulatorSettingsListView.SelectedIndex
                NextIndex = EmulatorSettingsListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    EmulatorSettingsListView.SelectedIndex -= 1
                End If
            End If

        End If
    End Sub

    Private Sub MoveDown()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim SelectedButtonNumber As Integer = GetIntOnly(SelectedButton.Name) + 1
            Dim NextButton As Button = CType(SettingsCanvas.FindName("SettingButton" + SelectedButtonNumber.ToString), Button)

            If NextButton IsNot Nothing Then
                SelectedButton.BorderBrush = Brushes.Transparent
                SelectedButton.BorderThickness = New Thickness(0, 0, 0, 0)

                NextButton.BorderBrush = Brushes.White
                NextButton.BorderThickness = New Thickness(3, 3, 3, 3)
            End If

        ElseIf TypeOf FocusedItem Is ListViewitem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
            Dim SelectedIndex As Integer
            Dim NextIndex As Integer
            Dim ItemCount As Integer

            'We are in the Devices ListView
            If CurrentListView.Name = "GeneralSettingsListView" Then
                SelectedIndex = GeneralSettingsListView.SelectedIndex
                NextIndex = GeneralSettingsListView.SelectedIndex + 1
                ItemCount = GeneralSettingsListView.Items.Count

                If Not NextIndex = ItemCount Then
                    GeneralSettingsListView.SelectedIndex += 1
                End If
            ElseIf CurrentListView.Name = "AudioSettingsListView" Then
                SelectedIndex = AudioSettingsListView.SelectedIndex
                NextIndex = AudioSettingsListView.SelectedIndex + 1
                ItemCount = AudioSettingsListView.Items.Count

                If Not NextIndex = ItemCount Then
                    AudioSettingsListView.SelectedIndex += 1
                End If
            ElseIf CurrentListView.Name = "BackgroundSettingsListView" Then
                SelectedIndex = BackgroundSettingsListView.SelectedIndex
                NextIndex = BackgroundSettingsListView.SelectedIndex + 1
                ItemCount = BackgroundSettingsListView.Items.Count

                If Not NextIndex = ItemCount Then
                    BackgroundSettingsListView.SelectedIndex += 1
                End If
            ElseIf CurrentListView.Name = "EmulatorSettingsListView" Then
                SelectedIndex = EmulatorSettingsListView.SelectedIndex
                NextIndex = EmulatorSettingsListView.SelectedIndex + 1
                ItemCount = EmulatorSettingsListView.Items.Count

                If Not NextIndex = ItemCount Then
                    BackgroundSettingsListView.SelectedIndex += 1
                End If
            End If

        End If
    End Sub

    Private Sub HideListAnimation(SettingsList As ListView, Optional ShowNewList As Boolean = False, Optional NewList As ListView = Nothing)

        Animate(SettingsList, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(300)))

        SettingsList.RenderTransform = New ScaleTransform()
        SettingsList.RenderTransformOrigin = New Point(0.5, 0.5)

        SettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(200))))
        SettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(200))))

        If ShowNewList And NewList IsNot Nothing Then
            ShowListAnimation(NewList)
        End If

    End Sub

    Private Sub ShowListAnimation(SettingsList As ListView)

        Animate(SettingsList, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(300)))

        SettingsList.RenderTransform = New ScaleTransform()
        SettingsList.RenderTransformOrigin = New Point(0.5, 0.5)

        SettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))
        SettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))

        SettingsList.Focus()
    End Sub

    Private Sub GoBackAnimation(CurrentSettingsList As ListView, PreviousSettingsList As ListView, PreviousIndexInListView As Integer)

        Animate(CurrentSettingsList, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(400)))
        Animate(PreviousSettingsList, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))

        CurrentSettingsList.RenderTransform = New ScaleTransform()
        CurrentSettingsList.RenderTransformOrigin = New Point(0.5, 0.5)
        CurrentSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))
        CurrentSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))

        PreviousSettingsList.RenderTransform = New ScaleTransform()
        PreviousSettingsList.RenderTransformOrigin = New Point(0.5, 0.5)
        PreviousSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))
        PreviousSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))

        Dim LastSelectedItem As SettingsListViewItem = TryCast(PreviousSettingsList.Items(PreviousIndexInListView).Content, SettingsListViewItem)
        LastSelectedItem.IsSettingSelected = Visibility.Visible

        Dim LastSelectedListViewItem As ListViewItem = TryCast(PreviousSettingsList.ItemContainerGenerator.ContainerFromIndex(PreviousIndexInListView), ListViewItem)
        LastSelectedListViewItem.Focus()

        PreviousSettingsList.SelectedIndex = PreviousIndexInListView

    End Sub

    Private Sub GoForwardAnimation(CurrentSettingsList As ListView, NextSettingsList As ListView)

        Animate(CurrentSettingsList, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(400)))
        Animate(NextSettingsList, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(400)))

        CurrentSettingsList.RenderTransform = New ScaleTransform()
        CurrentSettingsList.RenderTransformOrigin = New Point(0.5, 0.5)
        CurrentSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))
        CurrentSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(1, 0.5, New Duration(TimeSpan.FromMilliseconds(400))))

        NextSettingsList.RenderTransform = New ScaleTransform()
        NextSettingsList.RenderTransformOrigin = New Point(0.5, 0.5)
        NextSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))
        NextSettingsList.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(400))))

        Dim NextSelectedItem As SettingsListViewItem = TryCast(NextSettingsList.Items(0).Content, SettingsListViewItem)
        NextSelectedItem.IsSettingSelected = Visibility.Visible

        Dim NextSelectedListViewItem As ListViewItem = TryCast(NextSettingsList.Items(0), ListViewItem)
        NextSelectedListViewItem.Focus()

        'NextSettingsList.SelectedIndex = 0

    End Sub

    Public Function GetAncestorOfType(Of T As FrameworkElement)(child As FrameworkElement) As T
        Dim parent = VisualTreeHelper.GetParent(child)
        If parent IsNot Nothing AndAlso Not (TypeOf parent Is T) Then Return GetAncestorOfType(Of T)(CType(parent, FrameworkElement))
        Return parent
    End Function

    Private Function FindVisualChild(Of childItem As DependencyObject)(obj As DependencyObject) As childItem
        For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(obj) - 1
            Dim child As DependencyObject = VisualTreeHelper.GetChild(obj, i)
            If child IsNot Nothing AndAlso TypeOf child Is childItem Then
                Return CType(child, childItem)
            Else
                Dim childOfChild As childItem = FindVisualChild(Of childItem)(child)
                If childOfChild IsNot Nothing Then
                    Return childOfChild
                End If
            End If
        Next i
        Return Nothing
    End Function

    Private Shared Function GetIntOnly(value As String) As Integer
        Dim returnVal As String = String.Empty
        Dim collection As MatchCollection = Regex.Matches(value, "\d+")
        For Each m As Match In collection
            returnVal += m.ToString()
        Next
        Return Convert.ToInt32(returnVal)
    End Function

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

    Private Sub GeneralSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing
    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        If CurrentController.A_up Then
            OpenNewSettings()
        ElseIf CurrentController.B_up Then
            ReturnToPreviousSettings()
        ElseIf CurrentController.Dpad_Up_up Then
            MoveUp()
        ElseIf CurrentController.Dpad_Down_up Then
            MoveDown()
        End If

    End Sub

    Private Sub GeneralSettings_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        If e.Key = Key.Space Then
            OpenSideMenuSettings()
        End If
    End Sub

End Class
