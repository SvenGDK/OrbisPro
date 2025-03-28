Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNotifications
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading

Public Class SetupGames

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private WithEvents GameCollectionWorker As New BackgroundWorker() With {.WorkerReportsProgress = True}
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private WaitedFor As Integer = 0
    Private LastKeyboardKey As Key

    Private GameTitleList As New List(Of String)()

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SetupGames_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()
    End Sub

    Private Sub SetupGames_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        WaitTimer.Start()

        'Check for gamepads
        If GetAndSetGamepads() Then MainController = SharedController1
        ChangeButtonLayout()
    End Sub

    Private Sub SetupGames_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SetupGames_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
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
            GameCollectionWorker.RunWorkerAsync()
        End If
    End Sub

    Private Sub ContinueSetup()
        PlayBackgroundSound(Sounds.SelectItem)

        Dim NewSetupApps As New SetupApps() With {.ShowActivated = True, .Top = Top, .Left = Left, .Opacity = 0}
        NewSetupApps.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupApps.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub ReturnToPreviousSetupStep()
        PlayBackgroundSound(Sounds.Back)

        Dim NewSetupCheckUpdates As New SetupCheckUpdates() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewSetupCheckUpdates.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSetupCheckUpdates.Show()

        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

#Region "Game Loader"

    Private Sub GameCollectionWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles GameCollectionWorker.DoWork

        'Add games from C:\Program Files (x86)
        If Directory.Exists("C:\Program Files (x86)\") Then
            For Each Executable In Directory.GetFiles("C:\Program Files (x86)\", "*.exe", SearchOption.AllDirectories)
                Try
                    Dim ExecutableDirectory As String = Path.GetDirectoryName(Executable)
                    Dim NewGameListViewItem As New AppListViewItem()
                    Dim AddToListView As Boolean = False

                    'Try to detect games
                    If File.Exists(ExecutableDirectory + "\steam_api64.dll") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\Engine\Binaries\Win64\CrashReportClient.exe") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\embree.2.14.0.dll") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\nvtt_64.dll") Then
                        AddToListView = True
                    End If

                    If AddToListView Then

                        Dim FileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(Executable)

                        'Skip other executables that are shipped with the game
                        If GameTitleList.Exists(Function(s) s.Equals(FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase)) Then Continue For

                        If FileNameWithoutExtension.Contains("crash", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("installer", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("Language", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("unins", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UE3", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("Unreal", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("start", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UbisoftConnect", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UbisoftExtension", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UbisoftGameLauncher", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UplayService", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UplayWebCore", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        End If

                        Dim ExecutableFileVersionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(Executable)

                        'Set the icon
                        GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(Executable))

                        'Set the title
                        If String.IsNullOrEmpty(ExecutableFileVersionInfo.ProductName) Then
                            NewGameListViewItem.AppTitle = FileNameWithoutExtension
                            GameTitleList.Add(FileNameWithoutExtension)
                        Else
                            NewGameListViewItem.AppTitle = ExecutableFileVersionInfo.ProductName
                            GameTitleList.Add(ExecutableFileVersionInfo.ProductName)
                        End If

                        'Set launch path and hide the selection (for now)
                        NewGameListViewItem.AppLaunchPath = Executable
                        NewGameListViewItem.IsAppSelected = Visibility.Hidden

                        GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                        Thread.Sleep(150)
                    End If

                Catch UnauthorizedEx As UnauthorizedAccessException
                    Continue For
                Catch IOEx As IOException
                    Continue For
                Catch Ex As Exception
                    Continue For
                End Try
            Next
        End If

        If Directory.Exists("C:\Games\") Then
            'Add games from C:\Games (x86)
            For Each Executable In Directory.GetFiles("C:\Games\", "*.exe", SearchOption.AllDirectories)
                Try
                    Dim ExecutableDirectory As String = Path.GetDirectoryName(Executable)
                    Dim NewGameListViewItem As New AppListViewItem()
                    Dim AddToListView As Boolean = False

                    'Try to detect games
                    If File.Exists(ExecutableDirectory + "\steam_api64.dll") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\Engine\Binaries\Win64\CrashReportClient.exe") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\embree.2.14.0.dll") Then
                        AddToListView = True
                    ElseIf File.Exists(ExecutableDirectory + "\nvtt_64.dll") Then
                        AddToListView = True
                    End If

                    If AddToListView Then

                        Dim FileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(Executable)

                        'Skip other executables that are shipped with the game
                        If GameTitleList.Exists(Function(s) s.Equals(FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase)) Then Continue For
                        If FileNameWithoutExtension.Contains("crash", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("installer", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("Language", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("unins", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("UE3", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("Unreal", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        ElseIf FileNameWithoutExtension.Contains("start", StringComparison.OrdinalIgnoreCase) Then
                            Continue For
                        End If

                        Dim ExecutableFileVersionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(Executable)

                        'Set the icon
                        GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(Executable))

                        'Set the title
                        If String.IsNullOrEmpty(ExecutableFileVersionInfo.ProductName) Then
                            NewGameListViewItem.AppTitle = FileNameWithoutExtension
                            GameTitleList.Add(FileNameWithoutExtension)
                        Else
                            NewGameListViewItem.AppTitle = ExecutableFileVersionInfo.ProductName
                            GameTitleList.Add(ExecutableFileVersionInfo.ProductName)
                        End If

                        'Set launch path and hide the selection (for now)
                        NewGameListViewItem.AppLaunchPath = Executable
                        NewGameListViewItem.IsAppSelected = Visibility.Hidden

                        GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                        Thread.Sleep(150)
                    End If

                Catch UnauthorizedEx As UnauthorizedAccessException
                    Continue For
                Catch IOEx As IOException
                    Continue For
                Catch Ex As Exception
                    Continue For
                End Try
            Next
        End If

        'Add game launchers
        Try
            If File.Exists("C:\Program Files (x86)\Steam\steam.exe") Then
                Dim NewGameListViewItem As New AppListViewItem()

                If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\Steam_icon.png") Then
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = New BitmapImage(New Uri(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\Steam_icon.png", UriKind.RelativeOrAbsolute)))
                Else
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource("C:\Program Files (x86)\Steam\steam.exe"))
                End If

                NewGameListViewItem.AppTitle = Path.GetFileNameWithoutExtension("C:\Program Files (x86)\Steam\steam.exe")
                NewGameListViewItem.AppLaunchPath = "C:\Program Files (x86)\Steam\steam.exe"
                NewGameListViewItem.IsAppSelected = Visibility.Hidden
                GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(150)
            End If
            If File.Exists("C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe") Then
                Dim NewGameListViewItem As New AppListViewItem()

                If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\BattleNet_icon.png") Then
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = New BitmapImage(New Uri(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\BattleNet_icon.png", UriKind.RelativeOrAbsolute)))
                Else
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource("C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe"))
                End If

                NewGameListViewItem.AppTitle = Path.GetFileNameWithoutExtension("C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe")
                NewGameListViewItem.AppLaunchPath = "C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe"
                NewGameListViewItem.IsAppSelected = Visibility.Hidden
                GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(150)
            End If
            If File.Exists("C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe") Then
                Dim NewGameListViewItem As New AppListViewItem()

                If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\EpicGamesLauncher_icon.png") Then
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = New BitmapImage(New Uri(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\EpicGamesLauncher_icon.png", UriKind.RelativeOrAbsolute)))
                Else
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource("C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe"))
                End If

                NewGameListViewItem.AppTitle = Path.GetFileNameWithoutExtension("C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe")
                NewGameListViewItem.AppLaunchPath = "C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win64\EpicGamesLauncher.exe"
                NewGameListViewItem.IsAppSelected = Visibility.Hidden
                GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(150)
            End If
            If File.Exists("C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe") Then
                Dim NewGameListViewItem As New AppListViewItem()

                If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\UbisoftConnect_icon.png") Then
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = New BitmapImage(New Uri(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\UbisoftConnect_icon.png", UriKind.RelativeOrAbsolute)))
                Else
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource("C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe"))
                End If

                NewGameListViewItem.AppTitle = Path.GetFileNameWithoutExtension("C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe")
                NewGameListViewItem.AppLaunchPath = "C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\UbisoftConnect.exe"
                NewGameListViewItem.IsAppSelected = Visibility.Hidden
                GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(150)
            End If
            If File.Exists("C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EALauncher.exe") Then
                Dim NewGameListViewItem As New AppListViewItem()

                If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\EALauncher_icon.png") Then
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = New BitmapImage(New Uri(FileIO.FileSystem.CurrentDirectory + "\Assets\GameIcons\EALauncher_icon.png", UriKind.RelativeOrAbsolute)))
                Else
                    GamesLibrary.Dispatcher.BeginInvoke(Sub() NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource("C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EALauncher.exe"))
                End If

                NewGameListViewItem.AppTitle = Path.GetFileNameWithoutExtension("C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EALauncher.exe")
                NewGameListViewItem.AppLaunchPath = "C:\Program Files\Electronic Arts\EA Desktop\EA Desktop\EALauncher.exe"
                NewGameListViewItem.IsAppSelected = Visibility.Hidden
                GamesLibrary.Dispatcher.BeginInvoke(Sub() GamesLibrary.Items.Add(NewGameListViewItem))
                Thread.Sleep(150)
            End If
        Catch Ex As Exception
        End Try

        'Create game directories (for roms) at default install location
        CreateGameDirectories()
    End Sub

    Private Async Sub GameCollectionWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles GameCollectionWorker.RunWorkerCompleted
        'Hide loading indicator
        FontAwesome.Sharp.Awesome.SetSpin(LoadingIndicator, False)
        LoadingIndicator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        TopLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(100)), .AutoReverse = True})
        TopLabel.Text = "Select the games that you want to add to the Games Library"

        'Show buttons and activate gamepad
        BackButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        BackTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        OptionsButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        MoreTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        AddGameTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        EnterButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})
        ContinueTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(100))})

        If GamesLibrary.Items.Count > 0 Then
            'Focus the first game
            Dim FirstListViewItem As ListViewItem = CType(GamesLibrary.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert to DeviceListViewItem to control the item's customized properties
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

    Private Sub SetupGames_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            Select Case e.Key
                Case Key.A
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedGame As AppListViewItem = CType(GamesLibrary.SelectedItem, AppListViewItem)

                        'Add selected game to the library
                        Using GameWriter As New StreamWriter(GameLibraryPath, True)
                            GameWriter.WriteLine("PC;" + SelectedGame.AppTitle + ";" + SelectedGame.AppLaunchPath + ";" + "ShowInLibrary=True" + ";" + "ShowOnHome=True")
                        End Using

                        NotificationPopup(SetupGamesCanvas, SelectedGame.AppTitle, "Added to Games Library", SelectedGame.AppIcon)
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
                Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0
                Dim MainGamepadButton_Back_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Back) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                Dim MainGamepadButton_RightThumbX_Left As Boolean = MainGamepadState.Gamepad.RightThumbX = CShort(-32768)
                Dim MainGamepadButton_RightThumbX_Right As Boolean = MainGamepadState.Gamepad.RightThumbX = CShort(32767)
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

                        Dim SelectedGame As AppListViewItem = CType(GamesLibrary.SelectedItem, AppListViewItem)

                        'Add selected game to the library
                        Using GameWriter As New StreamWriter(GameLibraryPath, True)
                            GameWriter.WriteLine("PC;" + SelectedGame.AppTitle + ";" + SelectedGame.AppLaunchPath + ";" + "ShowInLibrary=True" + ";" + "ShowOnHome=True")
                            GameWriter.Close()
                        End Using

                        'Notify that the game has been added and add additional delay due to animation
                        NotificationPopup(SetupGamesCanvas, SelectedGame.AppTitle, "Added to Games Library", SelectedGame.AppIcon)
                    End If
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = GamesLibrary.SelectedIndex
                        Dim NextIndex As Integer = GamesLibrary.SelectedIndex - 1

                        If Not NextIndex = -1 Then
                            GamesLibrary.SelectedIndex -= 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = GamesLibrary.SelectedIndex
                        Dim NextIndex As Integer = GamesLibrary.SelectedIndex + 1

                        If Not NextIndex = GamesLibrary.Items.Count Then
                            GamesLibrary.SelectedIndex += 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = GamesLibrary.SelectedIndex
                        Dim NextIndex As Integer = GamesLibrary.SelectedIndex - 4

                        If Not NextIndex = GamesLibrary.Items.Count Then
                            GamesLibrary.SelectedIndex -= 4
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        'Get the ListView of the selected item
                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        Dim SelectedIndex As Integer = GamesLibrary.SelectedIndex
                        Dim NextIndex As Integer = GamesLibrary.SelectedIndex + 4

                        If Not NextIndex = GamesLibrary.Items.Count Then
                            GamesLibrary.SelectedIndex += 4
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
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))

            OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_options.png", UriKind.RelativeOrAbsolute))
            OptionsButton.Width = 48
            Canvas.SetTop(OptionsButton, 955)
            Canvas.SetLeft(OptionsButton, 385)

            AddButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub GamesLibrary_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles GamesLibrary.SelectionChanged
        If GamesLibrary.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As AppListViewItem = CType(e.RemovedItems(0), AppListViewItem)
            Dim SelectedItem As AppListViewItem = CType(e.AddedItems(0), AppListViewItem)

            SelectedItem.IsAppSelected = Visibility.Visible
            PreviousItem.IsAppSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub ScrollUp()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(GamesLibrary)
        Dim VerticalOffset As Double = GamesListViewScrollViewer.VerticalOffset
        GamesListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 70)
    End Sub

    Private Sub ScrollDown()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(GamesLibrary)
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

                OrbisDisplay.SetScaling(SetupGamesWindow, SetupGamesCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SetupGamesWindow, SetupGamesCanvas)
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
