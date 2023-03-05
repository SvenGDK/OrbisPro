Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Windows.Media.Animation
Imports OrbisPro.OrbisAnimations
Imports XInput.Wrapper

Public Class FileExplorer

    Public Shared LastPath As String 'Keep track of the last path
    Public LastSelectedIndex As Integer
    Public SelectedItemToCopy As FileBrowserListViewItem

    Dim GameLibraryPath As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")

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

    Private Sub FileExplorer_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        GetAttachedControllers()

        'Get all drives
        For Each Device In Directory.GetLogicalDrives()
            Dim DeviceInfo As New DriveInfo(Device)

            If DeviceInfo.IsReady And DeviceInfo.DriveType = DriveType.Fixed Then
                'Add internal storage
                DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = DeviceInfo.Name,
                                          .DeviceType = "Fixed",
                                          .AccessiblePath = DeviceInfo.Name,
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/Storage.png", UriKind.RelativeOrAbsolute))}})
            ElseIf DeviceInfo.DriveType = DriveType.CDRom Then
                'Add CDRom drives
                DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = DeviceInfo.Name,
                                          .DeviceType = "CDRom",
                                          .AccessiblePath = DeviceInfo.Name,
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))}})


            ElseIf DeviceInfo.IsReady And DeviceInfo.DriveType = DriveType.Removable Then
                'Add removable devices
                DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = DeviceInfo.VolumeLabel,
                                          .DeviceType = "Removable",
                                          .AccessiblePath = DeviceInfo.Name,
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/USBDevice.png", UriKind.RelativeOrAbsolute))}})
            End If
        Next

        'Add OrbisPro system folder to the list
        DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = "System",
                                          .DeviceType = "Fixed",
                                          .AccessiblePath = My.Computer.FileSystem.CurrentDirectory + "\system\",
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/InternalStorage.png", UriKind.RelativeOrAbsolute))}})

        'Focus the first device item
        Dim LastSelectedListViewItem As ListViewItem = CType(DevicesListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
        LastSelectedListViewItem.Focus()

        'Convert to DeviceListViewItem to control the item's customized properties
        Dim LastSelectedItem As DeviceListViewItem = CType(DevicesListView.Items(0).Content, DeviceListViewItem)
        LastSelectedItem.IsDeviceSelected = Visibility.Visible 'Show the selection border

    End Sub

    Private Sub DevicesListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DevicesListView.SelectionChanged

        If DevicesListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            'Play the 'move' sound effect
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            'Get the previous and next selected item and convert to DeviceListViewItem
            Dim PreviousItem As DeviceListViewItem = CType(e.RemovedItems(0).Content, DeviceListViewItem)
            Dim SelectedItem As DeviceListViewItem = CType(e.AddedItems(0).Content, DeviceListViewItem)

            'Set the selection border visibility
            SelectedItem.IsDeviceSelected = Visibility.Visible
            PreviousItem.IsDeviceSelected = Visibility.Hidden
        End If

    End Sub

    Private Sub FilesFoldersListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FilesFoldersListView.SelectionChanged
        If FilesFoldersListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            'Play the 'move' sound effect
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            'Get the previous and next selected item and convert to FileBrowserListViewItem
            Dim PreviousItem = CType(e.RemovedItems(0).Content, FileBrowserListViewItem)
            Dim SelectedItem = CType(e.AddedItems(0).Content, FileBrowserListViewItem)

            'Set the selection border visibility
            SelectedItem.IsFileFolderSelected = Visibility.Visible
            PreviousItem.IsFileFolderSelected = Visibility.Hidden

            'If the length of 'FileFolderName' (complete path for the most part) is too long then we animate the displayed text
            If SelectedItem.FileFolderName.Length >= 85 Then

                'Here it gets a bit complicated as we need to access 'FileFolderNameTextBlock' from the custom DataTemplate
                Dim FileFolderContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(e.AddedItems(0))
                Dim FileFolderDataTemplate As DataTemplate = FilesFoldersListView.ItemTemplate
                Dim SelectedItemDescription As TextBlock = TryCast(FileFolderDataTemplate.FindName("FileFolderNameTextBlock", FileFolderContentPresenter), TextBlock)

                Animate(SelectedItemDescription, Canvas.LeftProperty, 105, -SelectedItemDescription.ActualWidth, New Duration(TimeSpan.FromMilliseconds(10400)), True)
            End If
        End If
    End Sub

    Private Sub FileExplorer_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.X Then
            GoToFolder()
        ElseIf e.Key = Key.O Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            If TypeOf FocusedItem Is ListViewItem Then

                Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                If CurrentListView.Name = "FilesFoldersListView" Then
                    'Get the previously selected device
                    Dim CurrentSelectedDeviceItem As DeviceListViewItem = CType(DevicesListView.SelectedItem.Content, DeviceListViewItem)
                    Dim NewBrowsePath As String = CurrentSelectedDeviceItem.AccessiblePath

                    Dim CurrentSelectedFileFolderItem As String = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem).FileFolderName
                    Dim ParentFolder = Directory.GetParent(CurrentSelectedFileFolderItem).FullName

                    'If the previous folder is non-existant return to the device selection
                    If Not ParentFolder = NewBrowsePath Then
                        ReturnTo()
                    Else
                        'Play the 'return' sound effect
                        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

                        'Return to device selection
                        Dim SelectedDeviceItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                        DevicesListView.Focus()
                        SelectedDeviceItem.Focus()
                    End If
                Else
                    'Close the 'File Explorer'
                    OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
                    BeginAnimation(OpacityProperty, ClosingAnim)
                End If

            ElseIf TypeOf FocusedItem Is Button Then

                'Remove the options side menu
                Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                If Canvas.GetLeft(SettingButton5) = 1430 Then
                    Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                End If
                If Canvas.GetLeft(SettingButton6) = 1430 Then
                    Animate(SettingButton6, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                End If

                'Set the focus back
                Dim NextSelectedListViewItem As ListViewItem = TryCast(FilesFoldersListView.Items(FilesFoldersListView.SelectedIndex), ListViewItem)
                NextSelectedListViewItem.Focus()

            End If
        ElseIf e.Key = Key.T Then
            CopySelectedItem()
        ElseIf e.Key = Key.Up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            GoUp()
        ElseIf e.Key = Key.Down Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            GoDown()
        End If

    End Sub

    'This is the main animation while browsing through the folders -> If TypeOf FocusedItem Is Button
    Private Sub ShowListAnimation()
        Animate(FilesFoldersListView, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(300)))

        FilesFoldersListView.RenderTransform = New ScaleTransform()
        FilesFoldersListView.RenderTransformOrigin = New Point(0.5, 0.5)

        FilesFoldersListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))
        FilesFoldersListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))

        FilesFoldersListView.Focus()
    End Sub

    'Open the next folder - here we filter also the extensions
    Public Sub OpenNewFolder(NewBrowsePath As String)

        'First clear the items
        FilesFoldersListView.Items.Clear()

        'Check if the new folder contains specific directories
        For Each Folder In Directory.GetDirectories(NewBrowsePath)
            If Directory.Exists(Folder + "\PS3_GAME") Then
                FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
               .IsFileFolderSelected = Visibility.Hidden,
               .Type = "Folder",
               .IsExecutable = True,
               .FileFolderIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute))}
               })
            Else 'Otherwise just add as default folder
                FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
               .IsFileFolderSelected = Visibility.Hidden,
               .Type = "Folder",
               .FileFolderIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute))}
               })
            End If
        Next

        'Add all files from the new folder to the list and display custom extension icons
        For Each FileInFolder In Directory.GetFiles(NewBrowsePath)
            Dim FInfo As New FileInfo(FileInFolder)
            Dim FExtensionImage As BitmapImage
            Dim FExecutable As Boolean = False

            Select Case FInfo.Extension
                Case ".exe"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileEXE.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".dll"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileDLL.png", UriKind.RelativeOrAbsolute))
                Case ".sys"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileSYS.png", UriKind.RelativeOrAbsolute))
                Case ".tmp"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileTMP.png", UriKind.RelativeOrAbsolute))
                Case ".ini"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileINI.png", UriKind.RelativeOrAbsolute))
                Case ".iso"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".cue"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".jpg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileJPG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".png"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FilePNG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".txt"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileTXT.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".bin"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileBIN.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".BIN"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileBIN.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case Else
                    FExtensionImage = Nothing
            End Select

            FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = FileInFolder,
                                           .IsFileFolderSelected = Visibility.Hidden,
                                           .Type = "File",
                                           .FileFolderIcon = FExtensionImage,
                                           .IsExecutable = FExecutable}
                                           })
        Next

        ShowListAnimation()

        'Focus the first item
        Dim LastSelectedListViewItem As ListViewItem = FilesFoldersListView.ItemContainerGenerator.ContainerFromIndex(0)
        LastSelectedListViewItem.Focus()

        'Convert to FileBrowserListViewItem to set the border visibility on the first item
        Dim LastSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.Items(0).Content, FileBrowserListViewItem)
        LastSelectedItem.IsFileFolderSelected = Visibility.Visible
    End Sub

    Public Sub CopyFile(CopyFrom As String, CopyTo As String, Optional TransferIcon As ImageSource = Nothing)

        'Open a new 'CopyWindow'
        '{.Top = Top, .Left = Left} prevents the window being display at a different position
        Dim NewCopyWindow As New CopyWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewCopyWindow.CopyDescriptionTextBlock.Text = ""
        NewCopyWindow.CopyProgressBar.Maximum = 1
        NewCopyWindow.CopyFrom = CopyFrom
        NewCopyWindow.CopyTo = CopyTo
        NewCopyWindow.FileImage.Source = TransferIcon

        Animate(NewCopyWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
        NewCopyWindow.Show()

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
        'Wait for the animation to complete, then actually close the window

        For Each Win In Windows.Application.Current.Windows()
            If Not Win.ToString = "OrbisPro.MainWindow" Then
                'If no MainWindow is open, re-create it (after setup)
                Dim OrbisProMainWindow As New MainWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
                Animate(OrbisProMainWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                OrbisProMainWindow.Show()
                Exit For 'Exit the loop, otherwise it creates probably more windows
            Else
                Exit For
            End If
        Next

        Close()
    End Sub

    Private Sub FileExplorer_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing
    End Sub

    'Some keys need to be catched in the 'PreviewKeyDown' event
    Private Sub FileExplorer_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        If e.Key = Key.Space Then
            ShowHideSideOptions()
        End If
    End Sub

    'Go to selected folder - contains also code for the options side menu
    Private Sub GoToFolder()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Play the 'select' sound effect
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            'We are in the Devices ListView
            If CurrentListView.Name = "DevicesListView" Then
                Dim CurrentSelectedItem As DeviceListViewItem = CType(DevicesListView.SelectedItem.Content, DeviceListViewItem)
                Dim NewBrowsePath As String = CurrentSelectedItem.AccessiblePath

                LastPath = NewBrowsePath
                FilesFoldersListView.Items.Clear()

                'List all folders
                For Each Folder In Directory.GetDirectories(NewBrowsePath)

                    If Directory.Exists(NewBrowsePath + "\PS3_GAME") Then 'A PS3 game has been found, mark it with a RPCS3 logo
                        FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
                           .IsFileFolderSelected = Visibility.Hidden,
                           .Type = "Folder",
                           .IsExecutable = True,'Important - To be able to start the game from the 'Options' side menu
                           .FileFolderIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute))}
                           })
                    Else
                        FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
                           .IsFileFolderSelected = Visibility.Hidden,
                           .Type = "Folder",
                           .FileFolderIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute))}
                           })
                    End If

                Next

                'List all files inside the folder
                For Each FileInFolder In Directory.GetFiles(NewBrowsePath)
                    Dim FInfo As New FileInfo(FileInFolder)
                    Dim FExtensionImage As BitmapImage
                    Dim FExecutable As Boolean = False 'Mark the file as executable

                    'Set a custom extension image for 'known' file types
                    Select Case FInfo.Extension
                        Case ".exe"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileEXE.png", UriKind.RelativeOrAbsolute))
                        Case ".dll"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileDLL.png", UriKind.RelativeOrAbsolute))
                        Case ".sys"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileSYS.png", UriKind.RelativeOrAbsolute))
                        Case ".tmp"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileTMP.png", UriKind.RelativeOrAbsolute))
                        Case ".ini"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileINI.png", UriKind.RelativeOrAbsolute))
                        Case ".iso"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".jpg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileJPG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".png"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FilePNG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".txt"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileTXT.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".BIN"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileBIN.png", UriKind.RelativeOrAbsolute))
                        Case Else
                            FExtensionImage = Nothing
                    End Select

                    FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = FileInFolder,
                                                   .IsFileFolderSelected = Visibility.Hidden,
                                                   .Type = "File",
                                                   .FileFolderIcon = FExtensionImage,
                                                   .IsExecutable = FExecutable}
                                                   })
                Next

                ShowListAnimation()

                'Focus the first item
                Dim LastSelectedListViewItem As ListViewItem = CType(FilesFoldersListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
                LastSelectedListViewItem.Focus()

                'Convert to FileBrowserListViewItem to set the border visibility on the first item
                Dim LastSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.Items(0).Content, FileBrowserListViewItem)
                LastSelectedItem.IsFileFolderSelected = Visibility.Visible

                'We are in the Files&Folders ListView
            ElseIf CurrentListView.Name = "FilesFoldersListView" Then
                Dim CurrentSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)

                If CurrentSelectedItem.Type = "Folder" Then

                    Dim NewBrowsePath As String = CurrentSelectedItem.FileFolderName
                    LastPath = CurrentSelectedItem.FileFolderName
                    LastSelectedIndex = CurrentListView.SelectedIndex

                    'Open the new folder
                    OpenNewFolder(NewBrowsePath)
                End If

            End If

        ElseIf TypeOf FocusedItem Is Button Then

            Dim CurrentSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)
            Dim SelectedButton = CType(FocusedItem, Button)
            Dim SelectedSetting = SelectedButton.Content.ToString

            Select Case SelectedSetting
                Case "Add to game library"
                    If CurrentSelectedItem.FileFolderName.Contains(".iso") Then

                        Dim PS2GamesCount As Integer = 0
                        Dim NewGameINIKey As String

                        'Get the games to add our new game after it
                        For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")
                            If Game.Contains("PS2Game") Then
                                PS2GamesCount += 1
                            End If
                        Next

                        'Add the game to 'GameList.ini'
                        NewGameINIKey = "PS2Game" + (PS2GamesCount + 1).ToString
                        GameLibraryPath.IniWriteValue("PS2", NewGameINIKey, CurrentSelectedItem.FileFolderName)

                        'Show system dialog to inform that the game has been successfully added
                        Dim NewSuccessDialog As New SystemDialog() With {.ShowActivated = True, .Top = Top, .Left = Left}
                        Animate(NewSuccessDialog, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                        NewSuccessDialog.SystemDialogTextBlock.Text = "PS2 Game added to Library" + vbCrLf + vbCrLf + CurrentSelectedItem.FileFolderName
                        NewSuccessDialog.Opener = "FileExplorer"
                        NewSuccessDialog.Show()

                        X.StopPolling()
                        CurrentController = Nothing
                    ElseIf CurrentSelectedItem.FileFolderName.Contains(".cue") Then

                        Dim PS2GamesCount As Integer = 0
                        Dim NewGameINIKey As String

                        'Get the games to add our new game after it
                        For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")
                            If Game.Contains("PS1Game") Then
                                PS2GamesCount += 1
                            End If
                        Next

                        'Add the game to 'GameList.ini'
                        NewGameINIKey = "PS1Game" + (PS2GamesCount + 1).ToString
                        GameLibraryPath.IniWriteValue("PS1", NewGameINIKey, CurrentSelectedItem.FileFolderName)

                        'Show system dialog to inform that the game has been successfully added
                        Dim NewSuccessDialog As New SystemDialog() With {.ShowActivated = True, .Top = Top, .Left = Left}
                        Animate(NewSuccessDialog, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                        NewSuccessDialog.SystemDialogTextBlock.Text = "PS1 Game added to Library" + vbCrLf + vbCrLf + CurrentSelectedItem.FileFolderName
                        NewSuccessDialog.Opener = "FileExplorer"
                        NewSuccessDialog.Show()

                        X.StopPolling()
                        CurrentController = Nothing
                    ElseIf CurrentSelectedItem.FileFolderName.Contains("EBOOT.BIN") Then

                        Dim PS2GamesCount As Integer = 0
                        Dim NewGameINIKey As String

                        'Get the games to add our new game after it
                        For Each Game In File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\Games\GameList.ini")
                            If Game.Contains("PS3Game") Then
                                PS2GamesCount += 1
                            End If
                        Next

                        'Add the game to 'GameList.ini'
                        NewGameINIKey = "PS3Game" + (PS2GamesCount + 1).ToString
                        GameLibraryPath.IniWriteValue("PS3", NewGameINIKey, CurrentSelectedItem.FileFolderName)

                        'Show system dialog to inform that the game has been successfully added
                        Dim NewSuccessDialog As New SystemDialog() With {.ShowActivated = True, .Top = Top, .Left = Left}
                        Animate(NewSuccessDialog, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
                        NewSuccessDialog.SystemDialogTextBlock.Text = "PS3 Game added to Library" + vbCrLf + vbCrLf + CurrentSelectedItem.FileFolderName
                        NewSuccessDialog.Opener = "FileExplorer"
                        NewSuccessDialog.Show()

                        X.StopPolling()
                        CurrentController = Nothing
                    End If
            End Select

        End If
    End Sub

    Private Sub ReturnTo()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Play the 'return' sound effect
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            If CurrentListView.Name = "FilesFoldersListView" Then
                Dim CurrentSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)
                Dim ParentFolder = Directory.GetParent(CurrentSelectedItem.FileFolderName).FullName

                If Directory.GetParent(ParentFolder) IsNot Nothing Then

                    FilesFoldersListView.Items.Clear()

                    'List all folders
                    For Each Folder In Directory.GetDirectories(Directory.GetParent(ParentFolder).FullName)

                        If Directory.Exists(Folder + "\PS3_GAME") Then 'A PS3 game has been found, mark it with a RPCS3 logo
                            FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
                           .IsFileFolderSelected = Visibility.Hidden,
                           .Type = "Folder",
                           .IsExecutable = True,'Important - To be able to start the game from the 'Options' side menu
                           .FileFolderIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute))}
                           })
                        Else
                            FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = Folder,
                           .IsFileFolderSelected = Visibility.Hidden,
                           .Type = "Folder",
                           .FileFolderIcon = New BitmapImage(New Uri("/Icons/Folder.png", UriKind.RelativeOrAbsolute))}
                           })
                        End If

                    Next

                    'List all files inside the folder
                    For Each FileInFolder In Directory.GetFiles(Directory.GetParent(ParentFolder).FullName)
                        Dim FInfo As New FileInfo(FileInFolder)
                        Dim FExtensionImage As BitmapImage
                        Dim FExecutable As Boolean = False

                        Select Case FInfo.Extension
                            Case ".exe"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileEXE.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".dll"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileDLL.png", UriKind.RelativeOrAbsolute))
                            Case ".sys"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileSYS.png", UriKind.RelativeOrAbsolute))
                            Case ".tmp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileTMP.png", UriKind.RelativeOrAbsolute))
                            Case ".ini"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileINI.png", UriKind.RelativeOrAbsolute))
                            Case ".iso"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".jpg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileJPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".png"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FilePNG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".txt"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileTXT.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".BIN"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileBIN.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case Else
                                FExtensionImage = Nothing
                        End Select

                        FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = FileInFolder,
                                                       .IsFileFolderSelected = Visibility.Hidden,
                                                       .Type = "File",
                                                       .FileFolderIcon = FExtensionImage,
                                                       .IsExecutable = FExecutable}
                                                       })
                    Next

                    ShowListAnimation()

                    Dim LastSelectedListViewItem As ListViewItem = CType(FilesFoldersListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
                    LastSelectedListViewItem.Focus()

                    Dim LastSelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.Items(0).Content, FileBrowserListViewItem)
                    LastSelectedItem.IsFileFolderSelected = Visibility.Visible
                End If

            End If

        ElseIf TypeOf FocusedItem Is Button Then

            'Play the 'return' sound effect
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

            'Remove the options side menu
            Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            If Canvas.GetLeft(SettingButton5) = 1430 Then
                Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            End If
            If Canvas.GetLeft(SettingButton6) = 1430 Then
                Animate(SettingButton6, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            End If

            'Set the focus back
            Dim NextSelectedListViewItem As ListViewItem = TryCast(FilesFoldersListView.Items(FilesFoldersListView.SelectedIndex), ListViewItem)
            NextSelectedListViewItem.Focus()

        End If
    End Sub

    Private Sub CopySelectedItem()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            If CurrentListView.Name = "FilesFoldersListView" Then

                Dim SelectedItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)
                OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

                If CopyLabel.Content.ToString = "Copy" Then
                    SelectedItemToCopy = SelectedItem
                    CopyLabel.Content = "Paste"
                    OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Added to clipboard", SelectedItemToCopy.FileFolderName, SelectedItem.FileFolderIcon.UriSource.OriginalString)

                ElseIf CopyLabel.Content.ToString = "Paste" Then

                    'Get the directory from file or folder
                    If SelectedItem.Type = "File" Then
                        Dim isDir As Boolean = (File.GetAttributes(SelectedItemToCopy.FileFolderName) And FileAttributes.Directory) = FileAttributes.Directory

                        If isDir Then
                            Dim DInfo As New DirectoryInfo(SelectedItemToCopy.FileFolderName)
                            Dim DirName As String = DInfo.Name
                            Dim FInfo As New FileInfo(SelectedItem.FileFolderName)

                            MsgBox(FInfo.DirectoryName + "\" + DirName)
                        Else
                            Dim ClipboardFileInfo As New FileInfo(SelectedItemToCopy.FileFolderName)
                            Dim FInfo As New FileInfo(SelectedItem.FileFolderName)

                            CopyFile(SelectedItemToCopy.FileFolderName, FInfo.DirectoryName + "\" + ClipboardFileInfo.Name, SelectedItemToCopy.FileFolderIcon)
                        End If

                        'CopyFile(SelectedItemToCopy, FInfo.DirectoryName + "\" + FInfo.Name)
                    ElseIf SelectedItem.Type = "Folder" Then
                        Dim isDir As Boolean = (File.GetAttributes(SelectedItemToCopy.FileFolderName) And FileAttributes.Directory) = FileAttributes.Directory

                        If isDir Then

                        Else
                            Dim DInfo As New DirectoryInfo(SelectedItem.FileFolderName)
                            Dim ClipboardFileInfo As New FileInfo(SelectedItemToCopy.FileFolderName)

                            CopyFile(SelectedItemToCopy.FileFolderName, DInfo.Parent.FullName + "\" + ClipboardFileInfo.Name, SelectedItemToCopy.FileFolderIcon)
                        End If

                    End If

                    'CopyFile(SelectedItemToCopy, "")
                End If

            End If

        End If
    End Sub

    Private Sub GoUp()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim NextSelectedButtonNumber As Integer = GetIntOnly(SelectedButton.Name) - 1
            Dim NextButton As Button = CType(FileExplorerCanvas.FindName("SettingButton" + NextSelectedButtonNumber.ToString), Button)

            If NextButton IsNot Nothing Then
                SelectedButton.BorderBrush = Brushes.Transparent
                SelectedButton.BorderThickness = New Thickness(0, 0, 0, 0)

                NextButton.BorderBrush = Brushes.White
                NextButton.BorderThickness = New Thickness(3, 3, 3, 3)

                NextButton.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            'We are in the Devices ListView
            If CurrentListView.Name = "DevicesListView" Then

                Dim SelectedIndex As Integer = DevicesListView.SelectedIndex
                Dim NextIndex As Integer = DevicesListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    DevicesListView.SelectedIndex -= 1
                End If

            ElseIf CurrentListView.Name = "FilesFoldersListView" Then

                Dim SelectedIndex As Integer = FilesFoldersListView.SelectedIndex
                Dim NextIndex As Integer = FilesFoldersListView.SelectedIndex - 1

                If Not NextIndex = -1 Then
                    FilesFoldersListView.SelectedIndex -= 1
                End If
            End If

        End If
    End Sub

    Private Sub GoDown()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim NextSelectedButtonNumber As Integer = GetIntOnly(SelectedButton.Name) + 1
            Dim NextButton As Button = CType(FileExplorerCanvas.FindName("SettingButton" + NextSelectedButtonNumber.ToString), Button)

            If NextButton IsNot Nothing Then
                SelectedButton.BorderBrush = Brushes.Transparent
                SelectedButton.BorderThickness = New Thickness(0, 0, 0, 0)

                NextButton.BorderBrush = Brushes.White
                NextButton.BorderThickness = New Thickness(3, 3, 3, 3)

                NextButton.Focus()
            End If

        ElseIf TypeOf FocusedItem Is ListViewItem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            'We are in the Devices ListView
            If CurrentListView.Name = "DevicesListView" Then

                Dim SelectedIndex As Integer = DevicesListView.SelectedIndex
                Dim NextIndex As Integer = DevicesListView.SelectedIndex + 1
                Dim ItemCount As Integer = DevicesListView.Items.Count

                If Not NextIndex = ItemCount Then
                    DevicesListView.SelectedIndex += 1
                End If

            ElseIf CurrentListView.Name = "FilesFoldersListView" Then

                Dim SelectedIndex As Integer = FilesFoldersListView.SelectedIndex
                Dim NextIndex As Integer = FilesFoldersListView.SelectedIndex + 1
                Dim ItemCount As Integer = FilesFoldersListView.Items.Count

                If Not NextIndex = ItemCount Then
                    FilesFoldersListView.SelectedIndex += 1
                End If

            End If

        End If
    End Sub

    Private Sub ShowHideSideOptions()
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        If TypeOf FocusedItem Is ListViewItem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            'We are in the Devices ListView
            If CurrentListView.Name = "DevicesListView" Then

            ElseIf CurrentListView.Name = "FilesFoldersListView" Then
                'Play the 'select' sound effect
                OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

                Dim SelectedItem As FileBrowserListViewItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)

                'Display the options side menu
                If Canvas.GetLeft(RightMenu) = 1925 Then
                    Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                    Select Case SelectedItem.Type
                        Case "File"

                            'If the file is executable in OrbisPro add the 'Start' and 'Add to game library' options
                            If SelectedItem.IsExecutable Then
                                SettingButton1.Content = "Start"
                                SettingButton2.Content = "Add to copy list"
                                SettingButton3.Content = "Add to game library"
                                SettingButton4.Content = "Delete"
                                SettingButton5.Content = "Infos"

                                'Show the buttons
                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton5, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            Else
                                SettingButton1.Content = "Add to copy list"
                                SettingButton2.Content = "Move"
                                SettingButton3.Content = "Delete"
                                SettingButton4.Content = "Infos"

                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            End If

                            SettingButton1.Focus()

                        Case "Folder"

                            'If the folder is executable in OrbisPro (like for PS3 games) add the 'Start' and 'Add to game library' options
                            If SelectedItem.IsExecutable Then
                                SettingButton1.Content = "Start"
                                SettingButton2.Content = "Add to copy list"
                                SettingButton3.Content = "Add to game library"
                                SettingButton4.Content = "Delete"
                                SettingButton5.Content = "Infos"

                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton5, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            Else
                                SettingButton1.Content = "Add to copy list"
                                SettingButton2.Content = "Move"
                                SettingButton3.Content = "Delete"
                                SettingButton4.Content = "Infos"

                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            End If

                            SettingButton1.Focus()
                    End Select

                Else
                    'Remove the options side menu
                    Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                    Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                    Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                    Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                    Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                    If Canvas.GetLeft(SettingButton5) = 1430 Then
                        Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                    End If
                    If Canvas.GetLeft(SettingButton6) = 1430 Then
                        Animate(SettingButton6, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                    End If

                    'Set the focus back
                    Dim NextSelectedListViewItem As ListViewItem = TryCast(FilesFoldersListView.Items(FilesFoldersListView.SelectedIndex), ListViewItem)
                    NextSelectedListViewItem.Focus()
                End If
            End If

        End If
    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        If CurrentController.A_up Then
            GoToFolder()
        ElseIf CurrentController.B_up Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            If TypeOf FocusedItem Is ListViewItem Then

                Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                If CurrentListView.Name = "FilesFoldersListView" Then
                    'Get the previously selected device
                    Dim CurrentSelectedDeviceItem As DeviceListViewItem = CType(DevicesListView.SelectedItem.Content, DeviceListViewItem)
                    Dim NewBrowsePath As String = CurrentSelectedDeviceItem.AccessiblePath

                    Dim CurrentSelectedFileFolderItem As FileBrowserListViewItem = CType(FilesFoldersListView.SelectedItem.Content, FileBrowserListViewItem)
                    Dim ParentFolder = Directory.GetParent(CurrentSelectedFileFolderItem.FileFolderName).FullName

                    'If the previous folder is non-existant return to the device selection
                    If Not ParentFolder = NewBrowsePath Then
                        ReturnTo()
                    Else
                        'Play the 'return' sound effect
                        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)

                        'Return to device selection
                        Dim SelectedDeviceItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                        DevicesListView.Focus()
                        SelectedDeviceItem.Focus()
                    End If
                Else
                    'Close the 'File Explorer'
                    OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
                    BeginAnimation(OpacityProperty, ClosingAnim)
                End If

            ElseIf TypeOf FocusedItem Is Button Then

                'Remove the options side menu
                Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

                If Canvas.GetLeft(SettingButton5) = 1430 Then
                    Animate(SettingButton5, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                End If
                If Canvas.GetLeft(SettingButton6) = 1430 Then
                    Animate(SettingButton6, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
                End If

                'Set the focus back
                Dim NextSelectedListViewItem As ListViewItem = TryCast(FilesFoldersListView.Items(FilesFoldersListView.SelectedIndex), ListViewItem)
                NextSelectedListViewItem.Focus()

            End If

        ElseIf CurrentController.Y_up Then
            CopySelectedItem()
        ElseIf CurrentController.Dpad_Up_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            GoUp()
        ElseIf CurrentController.Dpad_Down_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            GoDown()
        ElseIf CurrentController.Start_up Then
            ShowHideSideOptions()
        End If

    End Sub

End Class
