﻿Imports Microsoft.Win32
Imports Newtonsoft.Json
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNotifications
Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Public Class SetupApps

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents AppCollectionWorker As New BackgroundWorker() With {.WorkerReportsProgress = True}
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private WaitedFor As Integer = 0
    Private LastKeyboardKey As Key

    Private AppShortcuts As String = FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt"

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupApps_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()
    End Sub

    Private Sub SetupApps_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        'Check for gamepads
        If GetAndSetGamepads() Then MainController = SharedController1
        ChangeButtonLayout()

        WaitTimer.Start()
    End Sub

    Private Sub SetupApps_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupApps_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

    Private Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        WaitedFor += 1

        If WaitedFor = 2 Then
            WaitTimer.Stop()
            AppCollectionWorker.RunWorkerAsync()
        End If
    End Sub

#Region "App Loader"

    Private Sub AppCollectionWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles AppCollectionWorker.DoWork
        Try
            'Get installed applications through Registry
            Dim UninstallKeys = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", False)
            Dim AppPathKeys = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", False)

            Dim ListOfUninstallSubKeys As New List(Of String)()
            Dim ListOfAppPathSubKeys As New List(Of String)()

            Dim FoundApplicationsInUninstall As New List(Of String)()
            Dim FoundApplicationsInAppPaths As New List(Of String)()

            'List all installation keys
            For Each InstallationKey In UninstallKeys.GetSubKeyNames()
                ListOfUninstallSubKeys.Add(InstallationKey)
            Next

            'List all App Path keys
            For Each AppPathKey In AppPathKeys.GetSubKeyNames()
                ListOfAppPathSubKeys.Add(AppPathKey)
            Next

            'Get all default values for the AppPathKeys
            For Each AppPathSubKeyName In ListOfAppPathSubKeys
                If Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + AppPathSubKeyName, False) IsNot Nothing Then
                    Dim DefaultKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + AppPathSubKeyName, False)
                    If DefaultKey.GetValue("") IsNot Nothing Then
                        'Remove special characters from string value
                        If DefaultKey.GetValue("").ToString().Contains(""""c) Then
                            'Check if file still exists
                            If File.Exists(DefaultKey.GetValue("").ToString().Replace(""""c, "")) Then
                                FoundApplicationsInAppPaths.Add(DefaultKey.GetValue("").ToString().Replace(""""c, ""))
                            End If
                        Else
                            If File.Exists(DefaultKey.GetValue("").ToString()) Then
                                FoundApplicationsInAppPaths.Add(DefaultKey.GetValue("").ToString())
                            End If
                        End If
                    End If
                End If
            Next

            'Add applications from App Paths
            For Each FoundApp In FoundApplicationsInAppPaths
                Dim ApplicationInstallation As New InstalledApplication With {.ExecutableLocation = FoundApp, .DisplayIconPath = FoundApp, .InstallLocation = Path.GetDirectoryName(FoundApp)}

                'Adjust Application Title if there's a FileDescription
                Dim ExecutableFileVersionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(FoundApp)
                If Not String.IsNullOrEmpty(ExecutableFileVersionInfo.FileDescription) Then
                    ApplicationInstallation.DisplayName = ExecutableFileVersionInfo.FileDescription
                Else
                    ApplicationInstallation.DisplayName = ExecutableFileVersionInfo.FileName
                End If

                Dim NewGameListViewItem As New AppListViewItem()
                ApplicationLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(ApplicationInstallation.ExecutableLocation))
                NewGameListViewItem.AppTitle = ApplicationInstallation.DisplayName
                NewGameListViewItem.AppLaunchPath = ApplicationInstallation.ExecutableLocation
                NewGameListViewItem.IsAppSelected = Visibility.Hidden

                ApplicationLibrary.Dispatcher.BeginInvoke(Sub() ApplicationLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(125)
            Next

            'Add applications from Uninstall
            For Each SubKeyName In ListOfUninstallSubKeys

                If Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + SubKeyName, False) IsNot Nothing Then

                    Dim ApplicationKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + SubKeyName, False)
                    Dim ApplicationInstallation As New InstalledApplication()

                    'Get the values
                    If ApplicationKey.GetValue("DisplayIcon") IsNot Nothing AndAlso ApplicationKey.GetValue("DisplayIcon").ToString().Contains("\"c) Then
                        ApplicationInstallation.DisplayIconPath = ApplicationKey.GetValue("DisplayIcon").ToString()
                    End If
                    If ApplicationKey.GetValue("DisplayName") IsNot Nothing Then
                        ApplicationInstallation.DisplayName = ApplicationKey.GetValue("DisplayName").ToString()
                    End If
                    If ApplicationKey.GetValue("InstallLocation") IsNot Nothing Then
                        ApplicationInstallation.InstallLocation = ApplicationKey.GetValue("InstallLocation").ToString()
                    End If

                    'Proceed if we have a DisplayIconPath, DisplayName and InstallLocation
                    If Not String.IsNullOrEmpty(ApplicationInstallation.DisplayIconPath) AndAlso Not String.IsNullOrEmpty(ApplicationInstallation.DisplayName) AndAlso Not String.IsNullOrEmpty(ApplicationInstallation.InstallLocation) Then

                        If Path.GetExtension(ApplicationInstallation.DisplayIconPath) = ".exe" Then

                            If File.Exists(ApplicationInstallation.DisplayIconPath) Then
                                ApplicationInstallation.ExecutableLocation = ApplicationInstallation.DisplayIconPath

                                Dim NewGameListViewItem As New AppListViewItem()
                                ApplicationLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(ApplicationInstallation.DisplayIconPath))
                                NewGameListViewItem.AppTitle = ApplicationInstallation.DisplayName
                                NewGameListViewItem.AppLaunchPath = ApplicationInstallation.DisplayIconPath
                                NewGameListViewItem.IsAppSelected = Visibility.Hidden

                                ApplicationLibrary.Dispatcher.BeginInvoke(Sub() ApplicationLibrary.Items.Add(NewGameListViewItem))
                                Thread.Sleep(125)
                            End If

                        End If

                    End If

                End If
            Next

        Catch UnauthorizedEx As UnauthorizedAccessException
        Catch Ex As Exception
        End Try
    End Sub

    Private Async Sub AppCollectionWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles AppCollectionWorker.RunWorkerCompleted
        'Hide loading indicator
        FontAwesome.Sharp.Awesome.SetSpin(LoadingIndicator, False)
        LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        Canvas.SetLeft(TopLabel, Canvas.GetLeft(TopLabel) - 225)
        TopLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100)), .AutoReverse = True})
        TopLabel.Text = "Select the applications that you want to add to the Apps Library"

        'Show buttons and activate gamepad
        BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddGameTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        EnterButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        ContinueTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        If ApplicationLibrary.Items.Count > 0 Then
            'Focus the first app
            Dim FirstListViewItem As ListViewItem = CType(ApplicationLibrary.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert to FirstListViewItem to control the item's customized properties
            Dim FirstSelectedItem As AppListViewItem = CType(FirstListViewItem.Content, AppListViewItem)
            FirstSelectedItem.IsAppSelected = Visibility.Visible 'Show the selection border
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

    Private Sub SetupApps_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            Select Case e.Key
                Case Key.A
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.SelectItem)

                        'Create new OrbisGamesList.App
                        Dim SelectedApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        Dim NewOrbisApp As New OrbisAppList.App() With {.Name = SelectedApp.AppTitle, .ExecutablePath = SelectedApp.AppLaunchPath, .ShowInLibrary = "True", .ShowOnHome = "True", .Platform = "PC", .BackgroundPath = "", .IconPath = ""}

                        'Add selected App to the library
                        Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
                        Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
                        AppsList.Apps.Add(NewOrbisApp)

                        Dim NewAppsListJSON As String = JsonConvert.SerializeObject(AppsList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
                        File.WriteAllText(AppLibraryPath, NewAppsListJSON)

                        NotificationPopup(SetupAppsCanvas, SelectedApp.AppTitle, "Added to Apps Library", SelectedApp.AppIcon)
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

    Private Sub SetupApps_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    ContinueSetup()
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    ReturnToPreviousSetupStep()
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.SelectItem)

                        'Create new OrbisGamesList.App
                        Dim SelectedApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        Dim NewOrbisApp As New OrbisAppList.App() With {.Name = SelectedApp.AppTitle, .ExecutablePath = SelectedApp.AppLaunchPath, .ShowInLibrary = "True", .ShowOnHome = "True", .Platform = "PC", .BackgroundPath = "", .IconPath = ""}

                        'Add selected App to the library
                        Dim AppsListJSON As String = File.ReadAllText(GameLibraryPath)
                        Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
                        AppsList.Apps.Add(NewOrbisApp)

                        Dim NewAppsListJSON As String = JsonConvert.SerializeObject(AppsList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
                        File.WriteAllText(GameLibraryPath, NewAppsListJSON)

                        NotificationPopup(SetupAppsCanvas, SelectedApp.AppTitle, "Added to Apps Library", SelectedApp.AppIcon)
                    End If
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
                        Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex - 1

                        If Not NextIndex = -1 Then
                            ApplicationLibrary.SelectedIndex -= 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
                        Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex + 1

                        If Not NextIndex = ApplicationLibrary.Items.Count Then
                            ApplicationLibrary.SelectedIndex += 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
                        Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex - 4

                        If Not NextIndex <= -1 Then
                            ApplicationLibrary.SelectedIndex -= 4
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
                        Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex + 4

                        If Not NextIndex = ApplicationLibrary.Items.Count Then
                            ApplicationLibrary.SelectedIndex += 4
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

    Private Sub ContinueSetup()
        PlayBackgroundSound(Sounds.SelectItem)

        Dim NewSetupCustomize As New SetupCustomize() With {.ShowActivated = True, .Top = Top, .Left = Left, .Opacity = 0}
        NewSetupCustomize.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupCustomize.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub ReturnToPreviousSetupStep()
        PlayBackgroundSound(Sounds.Back)

        Dim NewSetupGames As New SetupGames() With {.ShowActivated = True, .Top = Top, .Left = Left, .Opacity = 0}
        NewSetupGames.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupGames.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#Region "Navigation"

    Private Sub ApplicationLibrary_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ApplicationLibrary.SelectionChanged
        If ApplicationLibrary.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As AppListViewItem = CType(e.RemovedItems(0), AppListViewItem)
            Dim SelectedItem As AppListViewItem = CType(e.AddedItems(0), AppListViewItem)

            SelectedItem.IsAppSelected = Visibility.Visible
            PreviousItem.IsAppSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub ScrollUp()
        Dim AppsListViewScrollViewer As ScrollViewer = FindScrollViewer(ApplicationLibrary)
        Dim VerticalOffset As Double = AppsListViewScrollViewer.VerticalOffset
        AppsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim AppsListViewScrollViewer As ScrollViewer = FindScrollViewer(ApplicationLibrary)
        Dim VerticalOffset As Double = AppsListViewScrollViewer.VerticalOffset
        AppsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
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

                OrbisDisplay.SetScaling(SetupAppsWindow, SetupAppsCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SetupAppsWindow, SetupAppsCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

    Private Function ItemAlreadyExists(DisplayName As String) As Boolean
        Dim Found As Boolean = False
        For Each LVItem In ApplicationLibrary.Items
            Dim AsAppListViewItem As AppListViewItem = CType(LVItem, AppListViewItem)
            If AsAppListViewItem.AppTitle = DisplayName Then
                Found = True
                Exit For
            End If
        Next
        Return Found
    End Function

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "SetupApps",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
