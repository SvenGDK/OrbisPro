Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class CreateNewFolderDialog

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(400))}
    Private LastKeyboardKey As Key

    Public Opener As String
    Public FirstSelectedAppOrGame As Image

    Public ExistingFolderName As String
    Public ExistingFolderItemsCount As String
    Public ExistingAppsGamesInFolders As ObservableCollection(Of FolderContentListViewItem)

    Public AddToExistingFolderCanceled As Boolean = False

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

        'Add the first selected App/Game to the collection and set the existing folder items if the collection is not empty
        If ExistingAppsGamesInFolders IsNot Nothing AndAlso ExistingAppsGamesInFolders.Count > 0 Then
            FolderContentListView.ItemsSource = ExistingAppsGamesInFolders

            Dim FirstNewContentItem As New FolderContentListViewItem() With {.FolderContentAppIcon = FirstSelectedAppOrGame.Source, .GameAppTitle = CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle}
            ExistingAppsGamesInFolders.Add(FirstNewContentItem)
            FolderContentListView.Items.Refresh()

            FolderItemsCountTextBlock.Text = (Integer.Parse(FolderItemsCountTextBlock.Text) + 1).ToString()
        End If

        'Add the first selected App/Game to the collection when creating a new folder
        If ExistingAppsGamesInFolders Is Nothing Then
            ExistingAppsGamesInFolders = New ObservableCollection(Of FolderContentListViewItem)

            Dim FirstNewContentItem As New FolderContentListViewItem() With {.FolderContentAppIcon = FirstSelectedAppOrGame.Source, .GameAppTitle = CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle}
            ExistingAppsGamesInFolders.Add(FirstNewContentItem)

            FolderContentListView.ItemsSource = ExistingAppsGamesInFolders
            FolderContentListView.Items.Refresh()
        End If

        FolderNameTextBox.Focus()
    End Sub

    Private Async Sub ExistingFolders_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
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

        If AddToExistingFolderCanceled Then
            'Remove the previously selected item from the folder collection
            OrbisFolders.ChangeFolderNameOfAppGame("", CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle)
        End If

        Select Case Opener
            Case "ExistingFolders"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.ExistingFolders" Then
                        CType(Win, ExistingFolders).Activate()
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
                    If Not FolderNameTextBox.IsFocused Then
                        If Not MainTitle.Text = "Add to Existing Folder" Then
                            'Remove the temporary folder (if created)
                            OrbisFolders.RemoveFolder(FolderNameTextBox.Text)
                            AddToExistingFolderCanceled = True 'Changes the group name back when already added to the collection
                        End If

                        'Return
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If
                Case Key.X
                    'Select Content
                    If SelectButton.IsFocused Then
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        If MainTitle.Text = "Add to Existing Folder" Then

                            'Add the previously selected item to the folder collection
                            OrbisFolders.ChangeFolderNameOfAppGame(ExistingFolderName, CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle)

                            'Open a new SelectFolderContentDialog and check already existing items
                            Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                .ExistingFolderName = ExistingFolderName,
                                .ExistingFolderItemsCount = (Integer.Parse(ExistingFolderItemsCount) + 1).ToString(),
                                .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                            NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = ExistingFolderItemsCount

                            NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                            NewSelectFolderContentDialog.Show()

                        Else

                            'Create the new folder
                            If Not String.IsNullOrEmpty(FolderNameTextBox.Text) Then
                                If OrbisFolders.CreateNewFolder(FolderNameTextBox.Text) Then

                                    'Add the first item to the folder collection
                                    OrbisFolders.ChangeFolderNameOfAppGame(FolderNameTextBox.Text, CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle)

                                    'Open a new SelectFolderContentDialog and check already existing items
                                    Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                        .ExistingFolderName = FolderNameTextBox.Text,
                                        .ExistingFolderItemsCount = FolderItemsCountTextBlock.Text,
                                        .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                                    NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = FolderItemsCountTextBlock.Text

                                    NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                    NewSelectFolderContentDialog.Show()

                                Else
                                    'Continue with existing temporary folder
                                    'Open a new SelectFolderContentDialog and check already existing items
                                    Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                            .ExistingFolderName = FolderNameTextBox.Text,
                                            .ExistingFolderItemsCount = FolderItemsCountTextBlock.Text,
                                            .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                                    NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = FolderItemsCountTextBlock.Text

                                    NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                    NewSelectFolderContentDialog.Show()

                                End If
                            Else
                                'Empty folder name message
                                PlayBackgroundSound(Sounds.Options)

                                Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True, .Top = 0, .Left = 0, .Opacity = 0, .Opener = "CreateNewFolderDialog",
                                    .MessageTitle = "Folder Name Empty",
                                    .MessageDescription = "Folder name cannot be empty before selecting the content."}

                                NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                NewSystemDialog.Show()
                            End If

                        End If


                        'Cancel
                    ElseIf BigCancelButton.IsFocused Then

                        If Not MainTitle.Text = "Add to Existing Folder" Then
                            'Remove the temporary folder (if created)
                            OrbisFolders.RemoveFolder(FolderNameTextBox.Text)
                            AddToExistingFolderCanceled = True 'Changes the group name back when already added to the collection
                        End If

                        'Return
                        BeginAnimation(OpacityProperty, ClosingAnimation)


                        'OK
                    ElseIf OKButton.IsFocused Then

                        'Save folder info
                        If ExistingAppsGamesInFolders IsNot Nothing AndAlso ExistingAppsGamesInFolders.Count > 0 Then
                            For Each AppOrGame As FolderContentListViewItem In ExistingAppsGamesInFolders
                                'Set the group name of each App/Game in the collection to the new folder name
                                OrbisFolders.ChangeFolderNameOfAppGame(FolderNameTextBox.Text, AppOrGame.GameAppTitle)
                            Next

                            'Return to Home and reload
                            'Close the ExisistingFolders window (prevents reactivation)
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.ExistingFolders" Then
                                    CType(Win, ExistingFolders).Close()
                                    Exit For
                                End If
                            Next

                            'Reload the Home menu
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.MainWindow" Then
                                    CType(Win, MainWindow).PauseInput = True
                                    CType(Win, MainWindow).DidAnimate = False
                                    CType(Win, MainWindow).ReloadHome()
                                    Exit For
                                End If
                            Next

                            BeginAnimation(OpacityProperty, ClosingAnimation)
                        End If

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

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    'Select Content
                    If SelectButton.IsFocused Then
                        PlayBackgroundSound(Sounds.SelectItem)
                        PauseInput = True

                        If MainTitle.Text = "Add to Existing Folder" Then

                            'Add the previously selected item to the folder collection
                            OrbisFolders.ChangeFolderNameOfAppGame(ExistingFolderName, CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle)

                            'Open a new SelectFolderContentDialog and check already existing items
                            Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                .ExistingFolderName = ExistingFolderName,
                                .ExistingFolderItemsCount = (Integer.Parse(ExistingFolderItemsCount) + 1).ToString(),
                                .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                            NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = ExistingFolderItemsCount

                            NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                            NewSelectFolderContentDialog.Show()

                        Else

                            'Create the new folder
                            If Not String.IsNullOrEmpty(FolderNameTextBox.Text) Then
                                If OrbisFolders.CreateNewFolder(FolderNameTextBox.Text) Then

                                    'Add the first item to the folder collection
                                    OrbisFolders.ChangeFolderNameOfAppGame(FolderNameTextBox.Text, CType(FirstSelectedAppOrGame.Tag, OrbisStructures.AppDetails).AppTitle)

                                    'Open a new SelectFolderContentDialog and check already existing items
                                    Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                        .ExistingFolderName = FolderNameTextBox.Text,
                                        .ExistingFolderItemsCount = FolderItemsCountTextBlock.Text,
                                        .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                                    NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = FolderItemsCountTextBlock.Text

                                    NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                    NewSelectFolderContentDialog.Show()

                                Else
                                    'Continue with existing temporary folder
                                    'Open a new SelectFolderContentDialog and check already existing items
                                    Dim NewSelectFolderContentDialog As New SelectFolderContentDialog() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "CreateNewFolderDialog",
                                            .ExistingFolderName = FolderNameTextBox.Text,
                                            .ExistingFolderItemsCount = FolderItemsCountTextBlock.Text,
                                            .ExistingAppsGamesInFolders = ExistingAppsGamesInFolders}

                                    NewSelectFolderContentDialog.ContentItemsCountTextBlock.Text = FolderItemsCountTextBlock.Text

                                    NewSelectFolderContentDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                    NewSelectFolderContentDialog.Show()

                                End If
                            Else
                                'Empty folder name message
                                PlayBackgroundSound(Sounds.Options)

                                Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True, .Top = 0, .Left = 0, .Opacity = 0, .Opener = "CreateNewFolderDialog",
                                    .MessageTitle = "Folder Name Empty",
                                    .MessageDescription = "Folder name cannot be empty before selecting the content."}

                                NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                                NewSystemDialog.Show()
                            End If

                        End If


                        'Cancel
                    ElseIf BigCancelButton.IsFocused Then

                        If Not MainTitle.Text = "Add to Existing Folder" Then
                            'Remove the temporary folder (if created)
                            OrbisFolders.RemoveFolder(FolderNameTextBox.Text)
                            AddToExistingFolderCanceled = True 'Changes the group name back when already added to the collection
                        End If

                        'Return
                        BeginAnimation(OpacityProperty, ClosingAnimation)


                        'OK
                    ElseIf OKButton.IsFocused Then

                        'Save folder info
                        If ExistingAppsGamesInFolders IsNot Nothing AndAlso ExistingAppsGamesInFolders.Count > 0 Then
                            For Each AppOrGame As FolderContentListViewItem In ExistingAppsGamesInFolders
                                'Set the group name of each App/Game in the collection to the new folder name
                                OrbisFolders.ChangeFolderNameOfAppGame(FolderNameTextBox.Text, AppOrGame.GameAppTitle)
                            Next

                            'Return to Home and reload
                            'Close the ExisistingFolders window (prevents reactivation)
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.ExistingFolders" Then
                                    CType(Win, ExistingFolders).Close()
                                    Exit For
                                End If
                            Next

                            'Reload the Home menu
                            For Each Win In System.Windows.Application.Current.Windows()
                                If Win.ToString = "OrbisPro.MainWindow" Then
                                    CType(Win, MainWindow).PauseInput = True
                                    CType(Win, MainWindow).DidAnimate = False
                                    CType(Win, MainWindow).ReloadHome()
                                    Exit For
                                End If
                            Next

                            BeginAnimation(OpacityProperty, ClosingAnimation)
                        End If

                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    If Not FolderNameTextBox.IsFocused Then
                        If Not MainTitle.Text = "Add to Existing Folder" Then
                            'Remove the temporary folder (if created)
                            OrbisFolders.RemoveFolder(FolderNameTextBox.Text)
                            AddToExistingFolderCanceled = True 'Changes the group name back when already added to the collection
                        End If

                        'Return
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
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
        PlayBackgroundSound(Sounds.Move)

        If OKButton.IsFocused Then
            BigCancelButton.Focus()
        ElseIf BigCancelButton.IsFocused Then
            SelectButton.Focus()
        ElseIf SelectButton.IsFocused Then
            FolderNameTextBox.Focus()
        ElseIf FolderNameTextBox.IsFocused Then
            OKButton.Focus()
        End If
    End Sub

    Private Sub MoveDown()
        PlayBackgroundSound(Sounds.Move)

        If SelectButton.IsFocused Then
            BigCancelButton.Focus()
        ElseIf BigCancelButton.IsFocused Then
            OKButton.Focus()
        ElseIf OKButton.IsFocused Then
            FolderNameTextBox.Focus()
        ElseIf FolderNameTextBox.IsFocused Then
            SelectButton.Focus()
        End If
    End Sub

#End Region

#Region "Selection & Focus Changes"

    Private Sub FolderNameTextBox_GotFocus(sender As Object, e As RoutedEventArgs) Handles FolderNameTextBox.GotFocus
        FolderNameTextBox.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub FolderNameTextBox_LostFocus(sender As Object, e As RoutedEventArgs) Handles FolderNameTextBox.LostFocus
        FolderNameTextBox.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF959595"), Color))
    End Sub

    Private Sub SelectButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles SelectButton.GotFocus
        SelectButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub SelectButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles SelectButton.LostFocus
        SelectButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF959595"), Color))
    End Sub

    Private Sub BigCancelButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles BigCancelButton.GotFocus
        BigCancelButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub BigCancelButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles BigCancelButton.LostFocus
        BigCancelButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF959595"), Color))
    End Sub

    Private Sub OKButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles OKButton.GotFocus
        OKButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FFFFFFFF"), Color))
    End Sub

    Private Sub OKButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles OKButton.LostFocus
        OKButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF959595"), Color))
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

                OrbisDisplay.SetScaling(CreateNewFolderDialogWindow, CreateNewFolderDialogCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(CreateNewFolderDialogWindow, CreateNewFolderDialogCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

    Private Sub FolderNameTextBox_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles FolderNameTextBox.PreviewKeyDown
        If SharedController1 Is Nothing Then 'Only process when no controller is connected - Key.Down & Key.Up have to be catched in PreviewKeyDown
            If e.Key = Key.Down AndAlso FolderNameTextBox.IsFocused Then
                PlayBackgroundSound(Sounds.Move)
                SelectButton.Focus()
                e.Handled = True
            ElseIf e.Key = Key.Up AndAlso FolderNameTextBox.IsFocused Then
                PlayBackgroundSound(Sounds.Move)
                OKButton.Focus()
                e.Handled = True
            End If
        End If
    End Sub

#End Region

End Class
