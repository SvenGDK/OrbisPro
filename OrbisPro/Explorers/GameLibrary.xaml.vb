Imports System.IO
Imports System.Windows.Media.Animation
Imports XInput.Wrapper
Imports OrbisPro.OrbisAnimations

Public Class GameLibrary

    'Required animations
    Dim OptionsBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 1428, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim ButtonsBoxLeftAnim As New DoubleAnimation With {.From = 1930, .To = 1438, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim OptionsBoxRightAnim As New DoubleAnimation With {.From = 1428, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}
    Dim ButtonsBoxRightAnim As New DoubleAnimation With {.From = 1438, .To = 1930, .Duration = New Duration(TimeSpan.FromMilliseconds(300))}

    'This animations takes further action after it has ended
    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Dim WithEvents CurrentController As X.Gamepad

    'Get connected controllers
    Private Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            X.UpdatesPerSecond = 13 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    Private Sub GameLibrary_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        GetAttachedControllers()

        'If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Apps.ini") Then
        '    'Load internal and custom applications (these are currently fixed and should not be changed atm)
        '    For Each App In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Apps.ini")
        '        If App.Contains("App") Then
        '            Dim NewAppListViewItem As New AppListViewItem() With {.AppIcon = New BitmapImage(New Uri(App.Split("="c)(1).Split(";"c)(1), UriKind.RelativeOrAbsolute)),
        '                .AppTitle = App.Split("="c)(1),
        '                .IsAppSelected = Visibility.Hidden}
        '            ApplicationLibrary.Items.Add(NewAppListViewItem)
        '        End If
        '    Next
        'End If

        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini") Then
            'Load installed games in OrbisPro
            For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")

                If Game.Contains("PS1Game") Then

                    Dim NewGameListViewItem As New AppListViewItem() With {.AppIcon = New BitmapImage(New Uri(Game.Split("="c)(1).Split(";"c)(1), UriKind.RelativeOrAbsolute)),
                        .AppTitle = Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)),
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0)}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                ElseIf Game.Contains("PS2Game") Then

                    Dim NewGameListViewItem As New AppListViewItem() With {.AppIcon = New BitmapImage(New Uri(Game.Split("="c)(1).Split(";"c)(1), UriKind.RelativeOrAbsolute)),
                        .AppTitle = Path.GetFileNameWithoutExtension(Game.Split("="c)(1).Split(";"c)(0)),
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0)}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                ElseIf Game.Contains("PS3Game") Then

                    Dim PS3GameFolderName = Directory.GetParent(Game.Split("="c)(1).Split(";"c)(0))
                    Dim NewGameListViewItem As New AppListViewItem() With {.AppIcon = New BitmapImage(New Uri(Game.Split("="c)(1).Split(";"c)(1), UriKind.RelativeOrAbsolute)),
                        .AppTitle = PS3GameFolderName.Parent.Parent.Name,
                        .IsAppSelected = Visibility.Hidden,
                        .AppLaunchPath = Game.Split("="c)(1).Split(";"c)(0)}

                    ApplicationLibrary.Items.Add(NewGameListViewItem)
                End If

            Next
        End If

    End Sub

    Private Sub GameLibrary_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If e.Key = Key.Left Then

        ElseIf e.Key = Key.Right Then

        ElseIf e.Key = Key.X Then

            If TypeOf FocusedItem Is Button Then
                Dim SelectedButton As Button = CType(FocusedItem, Button)

                If SelectedButton.Name = "MenuStartAppButton" Then
                    'Start the selected game
                    Dim SelectedGameOrApp As AppListViewItem = CType(ApplicationLibrary.SelectedItem, AppListViewItem)
                    StartGameAnimation(SelectedGameOrApp)
                End If

            End If

        ElseIf e.Key = Key.O Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)
            BeginAnimation(OpacityProperty, ClosingAnim)
        End If

    End Sub

    Private Sub ApplicationLibrary_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ApplicationLibrary.SelectionChanged

        If ApplicationLibrary.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            Dim PreviousItem As AppListViewItem = CType(e.RemovedItems(0), AppListViewItem)
            Dim SelectedItem As AppListViewItem = CType(e.AddedItems(0), AppListViewItem)

            SelectedItem.IsAppSelected = Visibility.Visible
            PreviousItem.IsAppSelected = Visibility.Hidden
        End If

    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

    End Sub

    Private Sub GameLibrary_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        If e.Key = Key.Space Then
            'Show the options side menu
            If Canvas.GetLeft(RightMenu) = 1930 Then
                RightMenu.BeginAnimation(Canvas.LeftProperty, OptionsBoxLeftAnim)
                MenuStartAppButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
                MenuInformationButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
                MenuMoveCreateFolderButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
                MenuDeleteButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxLeftAnim)
                MenuStartAppButton.Focus()
            ElseIf Canvas.GetLeft(RightMenu) = 1428 Then
                RightMenu.BeginAnimation(Canvas.LeftProperty, OptionsBoxRightAnim)
                MenuStartAppButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
                MenuInformationButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
                MenuMoveCreateFolderButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
                MenuDeleteButton.BeginAnimation(Canvas.LeftProperty, ButtonsBoxRightAnim)
                ApplicationLibrary.Focus()
            End If
        End If
    End Sub

    Private Sub StartGameAnimation(SelectedApp As AppListViewItem)

        'Play 'start' sound effect
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

        'Start the game
        GameStarter.StartGame(SelectedApp.AppLaunchPath)

    End Sub

End Class
