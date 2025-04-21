Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.ComponentModel
Imports SharpDX.XInput
Imports System.Threading
Imports Newtonsoft.Json

Public Class GameLibrary

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private LastKeyboardKey As Key

    'Options menu animations
    Private OptionsBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 1430, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Private ButtonsBoxLeftAnim As New DoubleAnimation With {.From = 1930, .To = 1438, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Private OptionsBoxRightAnim As New DoubleAnimation With {.From = 1430, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Private ButtonsBoxRightAnim As New DoubleAnimation With {.From = 1438, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}

    Public Opener As String
    Public Confirmed As Boolean = False
    Public LastSelectedItem As AppListViewItem

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub GameLibrary_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Load games
        If File.Exists(GameLibraryPath) Then
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
            If GamesList IsNot Nothing Then
                For Each Game In GamesList.Games()
                    Select Case Game.Platform
                        Case "PC"
                            Dim NewGameListViewItem As New AppListViewItem() With {.AppTitle = Game.Name, .IsAppSelected = Visibility.Hidden, .AppLaunchPath = Game.ExecutablePath, .IsGame = True}
                            If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(Game.ExecutablePath)) Then
                                NewGameListViewItem.AppIcon = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(Game.ExecutablePath), UriKind.RelativeOrAbsolute))
                            Else
                                NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(Game.ExecutablePath)
                            End If
                            ApplicationLibrary.Items.Add(NewGameListViewItem)
                        Case "PS1", "PS2"
                            Dim NewGameListViewItem As New AppListViewItem() With {.AppTitle = Game.Name, .IsAppSelected = Visibility.Hidden, .AppLaunchPath = Game.ExecutablePath, .IsGame = True}
                            ApplicationLibrary.Items.Add(NewGameListViewItem)
                        Case "PS3"
                            Dim PS3GameFolderName = Directory.GetParent(Game.ExecutablePath)
                            Dim NewGameListViewItem As New AppListViewItem() With {.AppTitle = PS3GameFolderName.Parent.Parent.Name, .IsAppSelected = Visibility.Hidden, .AppLaunchPath = Game.ExecutablePath, .IsGame = True}
                            ApplicationLibrary.Items.Add(NewGameListViewItem)
                        Case "PS4"
                            Dim NewGameListViewItem As New AppListViewItem() With {.IsAppSelected = Visibility.Hidden, .AppTitle = Game.Name, .AppLaunchPath = Game.ExecutablePath, .IsGame = True}
                            If Not String.IsNullOrEmpty(Game.IconPath) Then
                                NewGameListViewItem.AppIcon = New BitmapImage(New Uri(Game.IconPath, UriKind.RelativeOrAbsolute))
                            End If
                            ApplicationLibrary.Items.Add(NewGameListViewItem)
                    End Select
                Next
            End If
        End If

        'Load applications
        If File.Exists(AppLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            If AppsList IsNot Nothing Then
                For Each RegisteredApp In AppsList.Apps()
                    If String.IsNullOrEmpty(RegisteredApp.IconPath) Then
                        Dim NewAppListViewItem As New AppListViewItem() With {.AppTitle = RegisteredApp.Name, .AppLaunchPath = RegisteredApp.ExecutablePath, .IsGame = False, .IsAppSelected = Visibility.Hidden}
                        If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath)) Then
                            NewAppListViewItem.AppIcon = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath), UriKind.RelativeOrAbsolute))
                        Else
                            NewAppListViewItem.AppIcon = GetExecutableIconAsImageSource(RegisteredApp.ExecutablePath)
                        End If
                        ApplicationLibrary.Items.Add(NewAppListViewItem)
                    End If
                Next
            End If
        End If

        If ApplicationLibrary.Items.Count > 0 Then
            'Focus the first item
            Dim FirstListViewItem As ListViewItem = CType(ApplicationLibrary.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert to FirstListViewItem to control the item's customized properties
            Dim FirstSelectedItem As AppListViewItem = CType(FirstListViewItem.Content, AppListViewItem)
            FirstSelectedItem.IsAppSelected = Visibility.Visible 'Show the selection border
        Else
            ApplicationLibrary.Focus()
        End If
    End Sub

    Private Async Sub GameLibrary_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub GameLibrary_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False

        'Remove selected game if confirmed
        If LastSelectedItem IsNot Nothing AndAlso Confirmed Then
            RemoveGameOrAppFromLibrary(LastSelectedItem)
            LastSelectedItem = Nothing
            Confirmed = False
        End If
    End Sub

    Private Sub GameLibrary_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        'Reactivate previous window
        Select Case Opener
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        CType(Win, FileExplorer).Activate()
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        Exit For
                    End If
                Next
            Case "OpenWindows"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.OpenWindows" Then
                        CType(Win, OpenWindows).Activate()
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Input"

    Private Sub GameLibrary_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.A
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        LastSelectedItem = SelectedGameOrApp

                        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
                            .Top = 0,
                            .Left = 0,
                            .Opacity = 0,
                            .Opener = "GameLibrary",
                            .MessageTitle = SelectedGameOrApp.AppTitle,
                            .MessageDescription = "Do you really want to remove " + SelectedGameOrApp.AppTitle + " from your Library ? This will also remove it from the Home screen."}

                        NewSystemDialog.ConfirmButton.Visibility = Visibility.Visible
                        NewSystemDialog.ConfirmTextBlock.Visibility = Visibility.Visible
                        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewSystemDialog.Show()
                    End If
                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.S
                    ShowHideSideMenu()
                Case Key.X
                    If TypeOf FocusedItem Is Button Then

                        Dim SelectedButton As Button = CType(FocusedItem, Button)
                        If SelectedButton.Name = "MenuStartAppButton" Then
                            Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                            LaunchGameOrApplication(SelectedGameOrApp)
                        End If

                    ElseIf TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        LaunchGameOrApplication(SelectedGameOrApp)
                    End If
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub GameLibrary_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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
                    If TypeOf FocusedItem Is Button Then

                        Dim SelectedButton As Button = CType(FocusedItem, Button)
                        If SelectedButton.Name = "MenuStartAppButton" Then
                            Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                            LaunchGameOrApplication(SelectedGameOrApp)
                        End If

                    ElseIf TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        LaunchGameOrApplication(SelectedGameOrApp)
                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Start_Button_Pressed Then
                    ShowHideSideMenu()
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        LastSelectedItem = SelectedGameOrApp

                        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
                            .Top = 0,
                            .Left = 0,
                            .Opacity = 0,
                            .SetupStep = True,
                            .Opener = "GameLibrary",
                            .MessageTitle = SelectedGameOrApp.AppTitle,
                            .MessageDescription = "Do you really want to remove " + SelectedGameOrApp.AppTitle + " from your Library ? This will also remove it the Home screen."}

                        NewSystemDialog.ConfirmButton.Visibility = Visibility.Visible
                        NewSystemDialog.ConfirmTextBlock.Visibility = Visibility.Visible
                        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewSystemDialog.Show()
                    End If
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    MoveLeft()
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    MoveRight()
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

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            BackButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            RemoveButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
            StartButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
            OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/S_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Start.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Triangle.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Options.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Triangle.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Options.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Triangle.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Start.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Triangle.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_Start.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_Y.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_Dots.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_Y.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_Start.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_Y.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_options.png", UriKind.RelativeOrAbsolute))
                        StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                        RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_y.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

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

    Private Sub ShowHideSideMenu()
        If Canvas.GetLeft(RightMenu) = 1930 Then
            RightMenu.BeginAnimation(Canvas.LeftProperty, OptionsBoxLeftAnim)
            MenuStartAppButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
            MenuInformationButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
            MenuMoveCreateFolderButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
            MenuUninstallButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)

            MenuMoveCreateFolderButton.Focus()
        ElseIf Canvas.GetLeft(RightMenu) = 1430 Then
            RightMenu.BeginAnimation(Canvas.LeftProperty, OptionsBoxRightAnim)
            MenuStartAppButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
            MenuInformationButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
            MenuMoveCreateFolderButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
            MenuUninstallButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)

            Dim SelectedListViewItem As ListViewItem = CType(ApplicationLibrary.ItemContainerGenerator.ContainerFromIndex(ApplicationLibrary.SelectedIndex), ListViewItem)
            SelectedListViewItem.Focus()
        End If
    End Sub

    Private Sub MoveUp()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If TypeOf FocusedItem Is ListViewItem Then
            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex - 3

            If Not NextIndex = ApplicationLibrary.Items.Count Then
                ApplicationLibrary.SelectedIndex -= 3
            End If
        ElseIf TypeOf FocusedItem Is Button Then
            If MenuMoveCreateFolderButton.IsFocused Then
                MenuUninstallButton.Focus()
            ElseIf MenuUninstallButton.IsFocused Then
                MenuInformationButton.Focus()
            ElseIf MenuInformationButton.IsFocused Then
                MenuStartAppButton.Focus()
            ElseIf MenuStartAppButton.IsFocused Then
                MenuMoveCreateFolderButton.Focus()
            End If
        End If
    End Sub

    Private Sub MoveDown()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If TypeOf FocusedItem Is ListViewItem Then
            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex + 3

            If Not NextIndex = ApplicationLibrary.Items.Count Then
                ApplicationLibrary.SelectedIndex += 3
            End If
        ElseIf TypeOf FocusedItem Is Button Then
            If MenuMoveCreateFolderButton.IsFocused Then
                MenuStartAppButton.Focus()
            ElseIf MenuStartAppButton.IsFocused Then
                MenuInformationButton.Focus()
            ElseIf MenuInformationButton.IsFocused Then
                MenuUninstallButton.Focus()
            ElseIf MenuUninstallButton.IsFocused Then
                MenuMoveCreateFolderButton.Focus()
            End If
        End If
    End Sub

    Private Sub MoveLeft()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If TypeOf FocusedItem Is ListViewItem Then
            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex - 1

            If Not NextIndex = -1 Then
                ApplicationLibrary.SelectedIndex -= 1
            End If
        End If
    End Sub

    Private Sub MoveRight()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If TypeOf FocusedItem Is ListViewItem Then
            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex + 1

            If Not NextIndex = ApplicationLibrary.Items.Count Then
                ApplicationLibrary.SelectedIndex += 1
            End If
        End If
    End Sub

    Private Sub ScrollUp()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(ApplicationLibrary)
        Dim VerticalOffset As Double = GamesListViewScrollViewer.VerticalOffset
        GamesListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 70)
    End Sub

    Private Sub ScrollDown()
        Dim GamesListViewScrollViewer As ScrollViewer = FindScrollViewer(ApplicationLibrary)
        Dim VerticalOffset As Double = GamesListViewScrollViewer.VerticalOffset
        GamesListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 70)
    End Sub

#End Region

#Region "Focus changes"

    Private Sub MenuMoveCreateFolderButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles MenuMoveCreateFolderButton.GotFocus
        MenuMoveCreateFolderButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub MenuMoveCreateFolderButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles MenuMoveCreateFolderButton.LostFocus
        MenuMoveCreateFolderButton.BorderBrush = Nothing
    End Sub

    Private Sub MenuStartAppButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles MenuStartAppButton.GotFocus
        MenuStartAppButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub MenuStartAppButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles MenuStartAppButton.LostFocus
        MenuStartAppButton.BorderBrush = Nothing
    End Sub

    Private Sub MenuInformationButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles MenuInformationButton.GotFocus
        MenuInformationButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub MenuInformationButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles MenuInformationButton.LostFocus
        MenuInformationButton.BorderBrush = Nothing
    End Sub

    Private Sub MenuUninstallButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles MenuUninstallButton.GotFocus
        MenuUninstallButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub MenuUninstallButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles MenuUninstallButton.LostFocus
        MenuUninstallButton.BorderBrush = Nothing
    End Sub

#End Region

#Region "Game Library Options"

    Private Sub LaunchGameOrApplication(SelectedApp As AppListViewItem)
        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Start the game
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.MainWindow" Then
                CType(Win, MainWindow).PauseInput = True
                CType(Win, MainWindow).StartedGameExecutable = SelectedApp.AppLaunchPath
                Exit For
            End If
        Next

        GameStarter.StartGame(SelectedApp.AppLaunchPath)
        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub RemoveGameOrAppFromLibrary(SelectedApp As AppListViewItem)

        If SelectedApp.IsGame Then
            If File.Exists(GameLibraryPath) Then
                Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
                Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
                If GamesList IsNot Nothing Then

                    For Each Game In GamesList.Games()
                        If Game.Name = SelectedApp.AppTitle Then
                            GamesList.Games.Remove(Game)
                            Exit For
                        End If
                    Next

                    Dim NewGamesListJSON As String = JsonConvert.SerializeObject(GamesList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
                    File.WriteAllText(GameLibraryPath, NewGamesListJSON)
                End If
            End If
        Else
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            If AppsList IsNot Nothing Then

                For Each RegisteredApp In AppsList.Apps()
                    If RegisteredApp.Name = SelectedApp.AppTitle Then
                        AppsList.Apps.Remove(RegisteredApp)
                        Exit For
                    End If
                Next

                Dim NewAppsListJSON As String = JsonConvert.SerializeObject(AppsList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
                File.WriteAllText(AppLibraryPath, NewAppsListJSON)
            End If
        End If

        'Remove from ApplicationLibrary
        ApplicationLibrary.Items.Remove(ApplicationLibrary.SelectedItem)

        'Reload the Home menu
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.MainWindow" Then
                CType(Win, MainWindow).ReloadHome()
                CType(Win, MainWindow).PauseInput = True
                Activate()
                Exit For
            End If
        Next

        'Set focus to the first item
        If ApplicationLibrary.Items.Count > 0 Then
            Dim FirstListViewItem As ListViewItem = CType(ApplicationLibrary.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            Dim FirstSelectedItem As AppListViewItem = CType(FirstListViewItem.Content, AppListViewItem)
            FirstSelectedItem.IsAppSelected = Visibility.Visible
        Else
            ApplicationLibrary.Focus()
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

                OrbisDisplay.SetScaling(GameLibraryWindow, GameLibraryCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(GameLibraryWindow, GameLibraryCanvas)
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
        PauseInput = True

        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "GameLibrary",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
