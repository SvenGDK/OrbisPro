Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports Newtonsoft.Json
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class SelectFolderContentDialog

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Private LastKeyboardKey As Key

    Public Opener As String
    Public ExistingFolderName As String
    Public ExistingFolderItemsCount As String
    Public StoredAppsGamesInFolders As ObservableCollection(Of FolderContentListViewItem)
    Public ExistingAppsGamesInFolders As ObservableCollection(Of FolderContentListViewItem)

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True


#Region "Window Events"

    Private Sub CreateFolderDialog_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Load all games
        If File.Exists(GameLibraryPath) Then
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
            If GamesList IsNot Nothing Then
                For Each Game In GamesList.Games()
                    Select Case Game.Platform
                        Case "PC"
                            Dim NewGameListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = Game.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = Game.Name}

                            If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(Game.ExecutablePath)) Then
                                NewGameListViewItem.FolderContentAppIcon = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(Game.ExecutablePath), UriKind.RelativeOrAbsolute))
                            Else
                                NewGameListViewItem.FolderContentAppIcon = GetExecutableIconAsImageSource(Game.ExecutablePath)
                            End If

                            FolderContentSelection.Items.Add(NewGameListViewItem)
                        Case "PS1", "PS2"
                            Dim NewGameListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = Game.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = Game.Name}
                            FolderContentSelection.Items.Add(NewGameListViewItem)
                        Case "PS3"
                            Dim PS3GameFolderName = Directory.GetParent(Game.ExecutablePath)
                            Dim NewGameListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = Game.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = PS3GameFolderName.Parent.Parent.Name}
                            FolderContentSelection.Items.Add(NewGameListViewItem)
                        Case "PS4"
                            Dim NewGameListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = Game.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = Game.Name}
                            If Not String.IsNullOrEmpty(Game.IconPath) Then
                                NewGameListViewItem.FolderContentAppIcon = New BitmapImage(New Uri(Game.IconPath, UriKind.RelativeOrAbsolute))
                            End If
                            FolderContentSelection.Items.Add(NewGameListViewItem)
                    End Select
                Next
            End If
        End If

        'Load all applications
        If File.Exists(AppLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            If AppsList IsNot Nothing Then
                For Each RegisteredApp In AppsList.Apps()
                    If Not String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                        Dim NewAppListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = RegisteredApp.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = RegisteredApp.Name}
                        If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath)) Then
                            NewAppListViewItem.FolderContentAppIcon = New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(RegisteredApp.ExecutablePath), UriKind.RelativeOrAbsolute))
                        Else
                            NewAppListViewItem.FolderContentAppIcon = GetExecutableIconAsImageSource(RegisteredApp.ExecutablePath)
                        End If
                        FolderContentSelection.Items.Add(NewAppListViewItem)
                    Else
                        Dim NewAppListViewItem As New FolderContentSelectionListViewItem() With {.FolderContentName = RegisteredApp.Group, .IsFolderContentAppSelected = Visibility.Hidden, .GameAppTitle = RegisteredApp.Name}
                        If Not String.IsNullOrEmpty(RegisteredApp.IconPath) Then
                            NewAppListViewItem.FolderContentAppIcon = New BitmapImage(New Uri(RegisteredApp.IconPath, UriKind.RelativeOrAbsolute))
                        End If
                        FolderContentSelection.Items.Add(NewAppListViewItem)
                    End If
                Next
            End If
        End If

        'Set the existing folder items if the collection is not empty
        If ExistingAppsGamesInFolders IsNot Nothing AndAlso ExistingAppsGamesInFolders.Count > 0 Then
            StoredAppsGamesInFolders = ExistingAppsGamesInFolders
            FolderContentListView.ItemsSource = ExistingAppsGamesInFolders
        End If

        'Set the existing folder items count if ExistingFolderItemsCount is not empty
        If Not String.IsNullOrEmpty(ExistingFolderItemsCount) Then
            ContentItemsCountTextBlock.Text = ExistingFolderItemsCount
        End If

        'Check items that are already in the folder if ExistingFolderName is not empty
        If Not String.IsNullOrEmpty(ExistingFolderName) Then
            For Each GameOrApp As FolderContentSelectionListViewItem In FolderContentSelection.Items
                If GameOrApp.FolderContentName = ExistingFolderName Then
                    GameOrApp.IsFolderContentAppChecked = True
                End If
            Next
        End If

        If FolderContentSelection.Items.Count > 0 Then
            Dim FirstListViewItem As ListViewItem = CType(FolderContentSelection.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            Dim FirstSelectedListViewItem As FolderContentSelectionListViewItem = TryCast(FolderContentSelection.Items(0), FolderContentSelectionListViewItem)
            FolderContentSelection.SelectedIndex = 0
            FirstSelectedListViewItem.IsFolderContentAppSelected = Visibility.Visible
        End If
    End Sub

    Private Async Sub CreateFolderDialog_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub CreateFolderDialog_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

    Private Sub CreateFolderDialog_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "CreateNewFolderDialog"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                        CType(Win, CreateNewFolderDialog).Activate()
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

    Private Sub FolderContentSelection_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FolderContentSelection.SelectionChanged
        If FolderContentSelection.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As FolderContentSelectionListViewItem = CType(e.RemovedItems(0), FolderContentSelectionListViewItem)
            Dim SelectedItem As FolderContentSelectionListViewItem = CType(e.AddedItems(0), FolderContentSelectionListViewItem)

            SelectedItem.IsFolderContentAppSelected = Visibility.Visible
            PreviousItem.IsFolderContentAppSelected = Visibility.Hidden
        End If
    End Sub

#Region "Input"

    Private Sub CreateFolderDialog_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Select Case e.Key

                Case Key.A
                    If FolderContentSelection.SelectedItem IsNot Nothing Then

                        PlayBackgroundSound(Sounds.SelectItem)

                        'Change checked state
                        Dim SelectedContentItem As FolderContentSelectionListViewItem = CType(FolderContentSelection.SelectedItem, FolderContentSelectionListViewItem)
                        If SelectedContentItem.IsFolderContentAppChecked Then
                            SelectedContentItem.IsFolderContentAppChecked = False

                            'Remove from to the collection
                            For Each FolderContentItem In ExistingAppsGamesInFolders
                                If FolderContentItem.GameAppTitle = SelectedContentItem.GameAppTitle Then
                                    ExistingAppsGamesInFolders.Remove(FolderContentItem)
                                    Exit For
                                End If
                            Next

                            ContentItemsCountTextBlock.Text = (Integer.Parse(ContentItemsCountTextBlock.Text) - 1).ToString()
                            FolderContentListView.Items.Refresh()

                        Else
                            SelectedContentItem.IsFolderContentAppChecked = True

                            'Add to the collection
                            ExistingAppsGamesInFolders.Add(New FolderContentListViewItem() With {.FolderContentAppIcon = SelectedContentItem.FolderContentAppIcon, .FolderName = ExistingFolderName, .GameAppTitle = SelectedContentItem.GameAppTitle})
                            ContentItemsCountTextBlock.Text = (Integer.Parse(ContentItemsCountTextBlock.Text) + 1).ToString()
                            FolderContentListView.Items.Refresh()
                        End If

                    End If
                Case Key.C
                    'Cancel the addition to the folder
                    For Each Win In System.Windows.Application.Current.Windows()
                        If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                            CType(Win, CreateNewFolderDialog).AddToExistingFolderCanceled = True
                            CType(Win, CreateNewFolderDialog).ExistingAppsGamesInFolders = StoredAppsGamesInFolders
                            CType(Win, CreateNewFolderDialog).FolderItemsCountTextBlock.Text = ContentItemsCountTextBlock.Text
                            Exit For
                        End If
                    Next

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X
                    'Return to the Add to New/Existing Folder window and return the modified ExistingAppsGamesInFolders list
                    For Each Win In System.Windows.Application.Current.Windows()
                        If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                            CType(Win, CreateNewFolderDialog).ExistingAppsGamesInFolders = ExistingAppsGamesInFolders
                            CType(Win, CreateNewFolderDialog).FolderItemsCountTextBlock.Text = ContentItemsCountTextBlock.Text
                            Exit For
                        End If
                    Next

                    BeginAnimation(OpacityProperty, ClosingAnimation)
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub CreateFolderDialog_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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
                    'Return to the Add to New/Existing Folder window and return the modified ExistingAppsGamesInFolders list
                    For Each Win In System.Windows.Application.Current.Windows()
                        If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                            CType(Win, CreateNewFolderDialog).ExistingAppsGamesInFolders = ExistingAppsGamesInFolders
                            CType(Win, CreateNewFolderDialog).FolderItemsCountTextBlock.Text = ContentItemsCountTextBlock.Text
                            Exit For
                        End If
                    Next

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    'Cancel the addition to the folder
                    For Each Win In System.Windows.Application.Current.Windows()
                        If Win.ToString = "OrbisPro.CreateNewFolderDialog" Then
                            CType(Win, CreateNewFolderDialog).AddToExistingFolderCanceled = True
                            CType(Win, CreateNewFolderDialog).ExistingAppsGamesInFolders = StoredAppsGamesInFolders
                            CType(Win, CreateNewFolderDialog).FolderItemsCountTextBlock.Text = ContentItemsCountTextBlock.Text
                            Exit For
                        End If
                    Next

                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If FolderContentSelection.SelectedItem IsNot Nothing Then

                        PlayBackgroundSound(Sounds.SelectItem)

                        'Change checked state
                        Dim SelectedContentItem As FolderContentSelectionListViewItem = CType(FolderContentSelection.SelectedItem, FolderContentSelectionListViewItem)
                        If SelectedContentItem.IsFolderContentAppChecked Then
                            SelectedContentItem.IsFolderContentAppChecked = False

                            'Remove from to the collection
                            For Each FolderContentItem In ExistingAppsGamesInFolders
                                If FolderContentItem.GameAppTitle = SelectedContentItem.GameAppTitle Then
                                    ExistingAppsGamesInFolders.Remove(FolderContentItem)
                                    Exit For
                                End If
                            Next

                            ContentItemsCountTextBlock.Text = (Integer.Parse(ContentItemsCountTextBlock.Text) - 1).ToString()
                            FolderContentListView.Items.Refresh()

                        Else
                            SelectedContentItem.IsFolderContentAppChecked = True

                            'Add to the collection
                            ExistingAppsGamesInFolders.Add(New FolderContentListViewItem() With {.FolderContentAppIcon = SelectedContentItem.FolderContentAppIcon, .FolderName = ExistingFolderName, .GameAppTitle = SelectedContentItem.GameAppTitle})
                            ContentItemsCountTextBlock.Text = (Integer.Parse(ContentItemsCountTextBlock.Text) + 1).ToString()
                            FolderContentListView.Items.Refresh()
                        End If

                    End If
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = FolderContentSelection.SelectedIndex
                        Dim NextIndex As Integer = FolderContentSelection.SelectedIndex - 1

                        If Not NextIndex = -1 Then
                            FolderContentSelection.SelectedIndex -= 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = FolderContentSelection.SelectedIndex
                        Dim NextIndex As Integer = FolderContentSelection.SelectedIndex + 1

                        If Not NextIndex = FolderContentSelection.Items.Count Then
                            FolderContentSelection.SelectedIndex += 1
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = FolderContentSelection.SelectedIndex
                        Dim NextIndex As Integer = FolderContentSelection.SelectedIndex - 4

                        If Not NextIndex = FolderContentSelection.Items.Count Then
                            FolderContentSelection.SelectedIndex -= 4
                        End If
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then
                        PlayBackgroundSound(Sounds.Move)

                        Dim SelectedIndex As Integer = FolderContentSelection.SelectedIndex
                        Dim NextIndex As Integer = FolderContentSelection.SelectedIndex + 4

                        If Not NextIndex = FolderContentSelection.Items.Count Then
                            FolderContentSelection.SelectedIndex += 4
                        End If
                    End If
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
            ReturnButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            CofirmButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
            CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        ReturnButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        CofirmButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                        CheckButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub ApplicationLibrary_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FolderContentSelection.SelectionChanged
        If FolderContentSelection.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As FolderContentSelectionListViewItem = CType(e.RemovedItems(0), FolderContentSelectionListViewItem)
            Dim SelectedItem As FolderContentSelectionListViewItem = CType(e.AddedItems(0), FolderContentSelectionListViewItem)

            SelectedItem.IsFolderContentAppSelected = Visibility.Visible
            PreviousItem.IsFolderContentAppSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub ScrollUp()
        Dim AppsListViewScrollViewer As ScrollViewer = FindScrollViewer(FolderContentSelection)
        Dim VerticalOffset As Double = AppsListViewScrollViewer.VerticalOffset
        AppsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim AppsListViewScrollViewer As ScrollViewer = FindScrollViewer(FolderContentSelection)
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

                OrbisDisplay.SetScaling(SelectFolderContentDialogWindow, SelectFolderContentDialogCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(SelectFolderContentDialogWindow, SelectFolderContentDialogCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

End Class
