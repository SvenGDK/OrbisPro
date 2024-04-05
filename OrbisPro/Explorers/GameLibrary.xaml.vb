Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.ComponentModel
Imports SharpDX.XInput
Imports System.Threading
Imports System.Text

Public Class GameLibrary

    Public Opener As String
    Private LastKeyboardKey As Key
    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    'Options menu animations
    Dim OptionsBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 1430, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim ButtonsBoxLeftAnim As New DoubleAnimation With {.From = 1930, .To = 1438, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim OptionsBoxRightAnim As New DoubleAnimation With {.From = 1430, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim ButtonsBoxRightAnim As New DoubleAnimation With {.From = 1438, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}

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
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Games\GameList.txt") Then
            For Each Game In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\Games\GameList.txt")
                If Game.StartsWith("PS1Game") Then
                    Dim NewGameListViewItem As New AppListViewItem() With {
                        .AppTitle = Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)),
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0),
                        .IsGame = True}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                ElseIf Game.StartsWith("PS2Game") Then
                    Dim NewGameListViewItem As New AppListViewItem() With {
                        .AppTitle = Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)),
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0),
                        .IsGame = True}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                ElseIf Game.StartsWith("PS3Game") Then
                    Dim PS3GameFolderName = Directory.GetParent(Game.Split("="c)(1).Split(";"c)(0))
                    Dim NewGameListViewItem As New AppListViewItem() With {
                        .AppTitle = PS3GameFolderName.Parent.Parent.Name,
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0),
                        .IsGame = True}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                ElseIf Game.StartsWith("PC") Then
                    Dim NewGameListViewItem As New AppListViewItem() With {
                        .AppTitle = Game.Split(";"c)(1),
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split(";"c)(2),
                        .IsGame = True}

                    If Not String.IsNullOrEmpty(CheckForExistingIconAsset(Game.Split(";"c)(2))) Then
                        NewGameListViewItem.AppIcon = New BitmapImage(New Uri(CheckForExistingIconAsset(Game.Split(";"c)(2)), UriKind.RelativeOrAbsolute))
                    Else
                        NewGameListViewItem.AppIcon = GetExecutableIconAsImageSource(Game.Split(";"c)(2))
                    End If

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                End If
            Next
        End If

        'Load applications
        If File.Exists(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt") Then
            For Each LineWithApp As String In File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                If LineWithApp.Split("="c)(1).Split(";"c).Length = 3 AndAlso LineWithApp.StartsWith("App") Then
                    Dim NewAppListViewItem As New AppListViewItem() With {
                        .AppTitle = LineWithApp.Split("="c)(1).Split(";"c)(0),
                        .AppLaunchPath = LineWithApp.Split("="c)(1).Split(";"c)(1),
                        .IsGame = False,
                        .IsAppSelected = Visibility.Hidden}

                    If Not String.IsNullOrEmpty(CheckForExistingIconAsset(LineWithApp.Split("="c)(1).Split(";"c)(1))) Then
                        NewAppListViewItem.AppIcon = New BitmapImage(New Uri(CheckForExistingIconAsset(LineWithApp.Split("="c)(1).Split(";"c)(1)), UriKind.RelativeOrAbsolute))
                    Else
                        NewAppListViewItem.AppIcon = GetExecutableIconAsImageSource(LineWithApp.Split("="c)(1).Split(";"c)(1))
                    End If

                    ApplicationLibrary.Items.Add(NewAppListViewItem)
                End If
            Next
        End If

        If ApplicationLibrary.Items.Count > 0 Then
            'Focus the first game
            Dim FirstListViewItem As ListViewItem = CType(ApplicationLibrary.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert to FirstListViewItem to control the item's customized properties
            Dim FirstSelectedItem As AppListViewItem = CType(FirstListViewItem.Content, AppListViewItem)
            FirstSelectedItem.IsAppSelected = Visibility.Visible 'Show the selection border
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
    End Sub

    Private Sub GameLibrary_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

#Region "Input"

    Private Sub GameLibrary_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.A
                    ShowHideSideMenu()
                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.S
                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        If SelectedGameOrApp.IsGame Then
                            RemoveGameOrAppFromLibrary(SelectedGameOrApp, FileIO.FileSystem.CurrentDirectory + "\Games\GameList.txt")
                        Else
                            RemoveGameOrAppFromLibrary(SelectedGameOrApp, FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                        End If

                    End If
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
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    ShowHideSideMenu()
                ElseIf MainGamepadButton_X_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                        If SelectedGameOrApp.IsGame Then
                            RemoveGameOrAppFromLibrary(SelectedGameOrApp, FileIO.FileSystem.CurrentDirectory + "\Games\GameList.txt")
                        Else
                            RemoveGameOrAppFromLibrary(SelectedGameOrApp, FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt")
                        End If

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
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            StartButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
            RemoveButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_x.png", UriKind.RelativeOrAbsolute))
            ActionButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub ApplicationLibrary_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ApplicationLibrary.SelectionChanged
        If ApplicationLibrary.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
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
            PlayBackgroundSound(Sounds.Move)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex - 3

            If Not NextIndex = ApplicationLibrary.Items.Count Then
                ApplicationLibrary.SelectedIndex -= 3
            End If
        ElseIf TypeOf FocusedItem Is Button Then
            PlayBackgroundSound(Sounds.Move)

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
            PlayBackgroundSound(Sounds.Move)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            Dim SelectedIndex As Integer = ApplicationLibrary.SelectedIndex
            Dim NextIndex As Integer = ApplicationLibrary.SelectedIndex + 3

            If Not NextIndex = ApplicationLibrary.Items.Count Then
                ApplicationLibrary.SelectedIndex += 3
            End If
        ElseIf TypeOf FocusedItem Is Button Then
            PlayBackgroundSound(Sounds.Move)

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
            PlayBackgroundSound(Sounds.Move)

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
            PlayBackgroundSound(Sounds.Move)

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

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

    Private Sub LaunchGameOrApplication(SelectedApp As AppListViewItem)
        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Start the game
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.MainWindow" Then
                CType(Win, MainWindow).StartedGameExecutable = SelectedApp.AppLaunchPath
                Exit For
            End If
        Next

        GameStarter.StartGame(SelectedApp.AppLaunchPath)
        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

    Private Sub RemoveGameOrAppFromLibrary(SelectedApp As AppListViewItem, FromFile As String)
        Dim NewLines As New List(Of String)()

        For Each Line As String In File.ReadAllLines(FromFile)
            If Not Line.Contains(SelectedApp.AppTitle) Then
                NewLines.Add(Line)
            End If
        Next

        'Save
        File.WriteAllLines(FromFile, NewLines.ToArray(), Encoding.UTF8)

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
        End If
    End Sub

    Private Sub SetBackground()
        'Set the background
        Select Case ConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
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

        'Set width & height
        If Not ConfigFile.IniReadValue("System", "DisplayScaling") = "AutoScaling" Then
            Dim SplittedValues As String() = ConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(GameLibraryWindow, GameLibraryCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = ConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
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

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
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
