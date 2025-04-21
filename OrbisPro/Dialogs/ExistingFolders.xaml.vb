Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports Newtonsoft.Json
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class ExistingFolders

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Private WithEvents WaitTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 1)}
    Private LastKeyboardKey As Key
    Private WaitedFor As Integer = 0

    Public Opener As String
    Public FirstSelectedAppOrGame As Image

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub ExistingFolders_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Get existing folders
        If File.Exists(AppLibraryPath) Then
            Dim FoldersListJSON As String = File.ReadAllText(FoldersPath)
            Dim FoldersList As OrbisFolders = JsonConvert.DeserializeObject(Of OrbisFolders)(FoldersListJSON)
            If FoldersList IsNot Nothing Then

                Dim ExistingFolderStyle As DataTemplate = CType(ExistingFoldersWindow.Resources("ExistingFolderStyle"), DataTemplate)

                For Each Folder In FoldersList.Folders
                    Dim ExistingAppsGamesInFolders As ObservableCollection(Of FolderContentListViewItem) = GetFolderItems(Folder.Name)
                    Dim NewExistingFolderListViewItem As New ExistingFolderListViewItem() With {
                        .FolderName = Folder.Name,
                        .FolderContentCount = GetFolderItemsCount(Folder.Name),
                        .IsFolderSelected = Visibility.Hidden,
                        .FolderContentItems = ExistingAppsGamesInFolders}

                    Dim ExistingFolderAsListViewItem As New ListViewItem With {.ContentTemplate = ExistingFolderStyle, .Content = NewExistingFolderListViewItem}
                    ExistingFoldersListView.Items.Add(ExistingFolderAsListViewItem)
                Next

                ExistingFoldersListView.Items.Refresh()
            End If
        End If

        PauseInput = True
        WaitTimer.Start()

        AddToNewFolderButton.Focus()
    End Sub

    Private Sub ExistingFolders_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ExistingFolders_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

    Private Sub ExistingFolders_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        CType(Win, FileExplorer).Activate()
                        Exit For
                    End If
                Next
            Case "GameLibrary"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GameLibrary" Then
                        CType(Win, GameLibrary).Activate()
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
        End Select

        Close()
    End Sub

#Region "Input"

    Private Sub ExistingFolders_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Select Case e.Key
                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X
                    Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                    If TypeOf FocusedItem Is Button Then
                        'Create a new folder
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        'Open a new CreateFolderDialog
                        Dim NewCreateNewFolderDialog As New CreateNewFolderDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "ExistingFolders", .FirstSelectedAppOrGame = FirstSelectedAppOrGame}
                        NewCreateNewFolderDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewCreateNewFolderDialog.Show()

                    ElseIf TypeOf FocusedItem Is ListViewItem Then
                        'Add to existing folder
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        'Open a new CreateFolderDialog and check already existing items
                        Dim SelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(ExistingFoldersListView.SelectedIndex), ListViewItem)
                        Dim SelectedItem As ExistingFolderListViewItem = TryCast(SelectedListViewItem.Content, ExistingFolderListViewItem)

                        Dim NewCreateNewFolderDialog As New CreateNewFolderDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "ExistingFolders",
                            .ExistingFolderName = SelectedItem.FolderName,
                            .ExistingFolderItemsCount = SelectedItem.FolderContentCount,
                            .ExistingAppsGamesInFolders = SelectedItem.FolderContentItems,
                            .FirstSelectedAppOrGame = FirstSelectedAppOrGame}

                        NewCreateNewFolderDialog.MainTitle.Text = "Add to Existing Folder"
                        NewCreateNewFolderDialog.FolderNameTextBox.Text = SelectedItem.FolderName
                        NewCreateNewFolderDialog.FolderItemsCountTextBlock.Text = SelectedItem.FolderContentCount

                        NewCreateNewFolderDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewCreateNewFolderDialog.Show()
                    End If
                Case Key.Down
                    MoveDown()
                Case Key.Up
                    MoveUp()
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub ExistingFolders_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
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
                    If TypeOf FocusedItem Is Button Then
                        'Create a new folder
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        'Open a new CreateFolderDialog
                        Dim NewCreateNewFolderDialog As New CreateNewFolderDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "ExistingFolders", .FirstSelectedAppOrGame = FirstSelectedAppOrGame}
                        NewCreateNewFolderDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewCreateNewFolderDialog.Show()

                    ElseIf TypeOf FocusedItem Is ListViewItem Then
                        'Add to existing folder
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        'Open a new CreateFolderDialog and check already existing items
                        Dim SelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(ExistingFoldersListView.SelectedIndex), ListViewItem)
                        Dim SelectedItem As ExistingFolderListViewItem = TryCast(SelectedListViewItem.Content, ExistingFolderListViewItem)

                        Dim NewCreateNewFolderDialog As New CreateNewFolderDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "ExistingFolders",
                            .ExistingFolderName = SelectedItem.FolderName,
                            .ExistingFolderItemsCount = SelectedItem.FolderContentCount,
                            .ExistingAppsGamesInFolders = SelectedItem.FolderContentItems,
                            .FirstSelectedAppOrGame = FirstSelectedAppOrGame}

                        NewCreateNewFolderDialog.MainTitle.Text = "Add to Existing Folder"
                        NewCreateNewFolderDialog.FolderNameTextBox.Text = SelectedItem.FolderName
                        NewCreateNewFolderDialog.FolderItemsCountTextBlock.Text = SelectedItem.FolderContentCount

                        NewCreateNewFolderDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                        NewCreateNewFolderDialog.Show()
                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
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
            CancelButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub MoveUp()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            PlayBackgroundSound(Sounds.Move)

            'Select the last item in the ExistingFolders ListView if exists
            If ExistingFoldersListView.Items.Count > 0 Then
                Dim NextSelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(ExistingFoldersListView.Items.Count - 1), ListViewItem)
                Dim NextSelectedItem As ExistingFolderListViewItem = TryCast(NextSelectedListViewItem.Content, ExistingFolderListViewItem)

                ExistingFoldersListView.SelectedIndex = ExistingFoldersListView.Items.Count - 1
                NextSelectedItem.IsFolderSelected = Visibility.Visible
                ExistingFoldersListView.ScrollIntoView(ExistingFoldersListView.SelectedItem)
                NextSelectedListViewItem.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            If ExistingFoldersListView.SelectedIndex = 0 Then

                'Remove focus from last selected item
                Dim SelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(ExistingFoldersListView.SelectedIndex), ListViewItem)
                Dim SelectedItem As ExistingFolderListViewItem = TryCast(SelectedListViewItem.Content, ExistingFolderListViewItem)
                SelectedItem.IsFolderSelected = Visibility.Hidden

                'Focus back on the button
                PlayBackgroundSound(Sounds.Move)
                AddToNewFolderButton.Focus()
            Else
                'Navigate on items
                Dim SelectedIndex As Integer
                Dim NextIndex As Integer

                SelectedIndex = ExistingFoldersListView.SelectedIndex
                NextIndex = ExistingFoldersListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    ExistingFoldersListView.SelectedIndex -= 1
                End If
            End If

        End If

    End Sub

    Private Sub MoveDown()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            PlayBackgroundSound(Sounds.Move)

            'Select the first item in the ExistingFolders ListView if exists
            If ExistingFoldersListView.Items.Count > 0 Then
                Dim NextSelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(0), ListViewItem)
                Dim NextSelectedItem As ExistingFolderListViewItem = TryCast(NextSelectedListViewItem.Content, ExistingFolderListViewItem)

                ExistingFoldersListView.SelectedIndex = 0
                NextSelectedItem.IsFolderSelected = Visibility.Visible
                ExistingFoldersListView.ScrollIntoView(ExistingFoldersListView.SelectedItem)
                NextSelectedListViewItem.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            If ExistingFoldersListView.SelectedIndex = ExistingFoldersListView.Items.Count - 1 Then

                'Remove focus from last selected item
                Dim SelectedListViewItem As ListViewItem = TryCast(ExistingFoldersListView.Items(ExistingFoldersListView.SelectedIndex), ListViewItem)
                Dim SelectedItem As ExistingFolderListViewItem = TryCast(SelectedListViewItem.Content, ExistingFolderListViewItem)
                SelectedItem.IsFolderSelected = Visibility.Hidden

                'Focus back on the button
                PlayBackgroundSound(Sounds.Move)
                AddToNewFolderButton.Focus()
            Else
                'Navigate on items
                Dim SelectedIndex As Integer
                Dim NextIndex As Integer
                Dim ItemCount As Integer

                SelectedIndex = ExistingFoldersListView.SelectedIndex
                NextIndex = ExistingFoldersListView.SelectedIndex + 1
                ItemCount = ExistingFoldersListView.Items.Count

                If Not NextIndex = ItemCount Then
                    ExistingFoldersListView.SelectedIndex += 1
                End If
            End If

        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(ExistingFoldersListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(ExistingFoldersListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

#Region "Selection & Focus Changes"

    Private Sub ExistingFoldersListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ExistingFoldersListView.SelectionChanged
        If ExistingFoldersListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim RemovedItem As ListViewItem = CType(e.RemovedItems(0), ListViewItem)
            Dim AddedItem As ListViewItem = CType(e.AddedItems(0), ListViewItem)

            Dim PreviousItem = CType(RemovedItem.Content, ExistingFolderListViewItem)
            Dim SelectedItem = CType(AddedItem.Content, ExistingFolderListViewItem)

            SelectedItem.IsFolderSelected = Visibility.Visible
            PreviousItem.IsFolderSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub SettingButton1_GotFocus(sender As Object, e As RoutedEventArgs) Handles AddToNewFolderButton.GotFocus
        AddToNewFolderButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SettingButton1_LostFocus(sender As Object, e As RoutedEventArgs) Handles AddToNewFolderButton.LostFocus
        AddToNewFolderButton.BorderBrush = Nothing
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

                OrbisDisplay.SetScaling(ExistingFoldersWindow, ExistingFoldersCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(ExistingFoldersWindow, ExistingFoldersCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

    Private Async Sub WaitTimer_Tick(sender As Object, e As EventArgs) Handles WaitTimer.Tick
        WaitedFor += 1

        If WaitedFor = 1 Then
            WaitTimer.Stop()
            WaitedFor = 0
            PauseInput = False

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        End If
    End Sub

End Class
