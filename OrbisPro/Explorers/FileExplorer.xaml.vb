Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.GameStarter
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation
Imports SharpDX.XInput

Public Class FileExplorer

    Public Opener As String
    Public LastPath As String 'Keep track of the last path
    Public LastSelectedIndex As Integer
    Public SelectedItemToCopy As FileBrowserListViewItem
    Private LastKeyboardKey As Key

    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub FileExplorer_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

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
                                          .AccessiblePath = FileIO.FileSystem.CurrentDirectory + "\system\",
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/InternalStorage.png", UriKind.RelativeOrAbsolute))}})

        'Focus the first device item
        Dim LastSelectedListViewItem As ListViewItem = CType(DevicesListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
        LastSelectedListViewItem.Focus()

        'Convert to DeviceListViewItem to control the item's customized properties
        Dim LastSelectedItem As DeviceListViewItem = CType(LastSelectedListViewItem.Content, DeviceListViewItem)
        LastSelectedItem.IsDeviceSelected = Visibility.Visible 'Show the selection border
    End Sub

    Private Async Sub FileExplorer_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub FileExplorer_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub FileExplorer_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        'Reactive previous window
        Select Case Opener
            Case "FileExplorer"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, FileExplorer).Activate()
                        CType(Win, FileExplorer).PauseInput = False
                        Exit For
                    End If
                Next
            Case "GameLibrary"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GameLibrary" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, GameLibrary).Activate()
                        CType(Win, GameLibrary).PauseInput = False
                        Exit For
                    End If
                Next
            Case "GeneralSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, GeneralSettings).Activate()
                        CType(Win, GeneralSettings).PauseInput = False
                        Exit For
                    End If
                Next
            Case "MainWindow"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).Activate()
                        CType(Win, MainWindow).PauseInput = False
                        Exit For
                    End If
                Next
            Case "OpenWindows"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.OpenWindows" Then
                        CType(Win, OpenWindows).Activate()
                        CType(Win, OpenWindows).PauseInput = False
                        Exit For
                    End If
                Next
            Case "SetupPS3"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupPS3" Then

                        'If PS3SetupDownload Then
                        '    CType(Win, SetupPS3).FirmwareDownloadedCheckBox.IsChecked = True
                        '    CType(Win, SetupPS3).DownloadFirmwareButton.BorderBrush = Nothing
                        '    CType(Win, SetupPS3).InstallFirmwareButton.Focus()
                        'End If

                        CType(Win, SetupPS3).AdditionalPauseDelay = 100
                        CType(Win, SetupPS3).PauseInput = False
                        CType(Win, SetupPS3).Activate()

                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Animations"

    'This is the main animation while browsing through the folders
    Private Sub ShowListAnimation()
        Animate(FilesFoldersListView, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(300)))

        FilesFoldersListView.RenderTransform = New ScaleTransform()
        FilesFoldersListView.RenderTransformOrigin = New Point(0.5, 0.5)

        FilesFoldersListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))
        FilesFoldersListView.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, New DoubleAnimation(0.5, 1, New Duration(TimeSpan.FromMilliseconds(200))))

        FilesFoldersListView.Focus()
    End Sub

#End Region

#Region "Input"

    Private Sub FileExplorer_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey Then
            Select Case e.Key
                Case Key.A
                    CopySelectedItem()
                Case Key.C
                    Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        If CurrentListView.Name = "FilesFoldersListView" Then
                            'Get the previously selected device
                            Dim SelectedDeviceItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                            Dim CurrentSelectedDeviceItem As DeviceListViewItem = CType(SelectedDeviceItem.Content, DeviceListViewItem)
                            Dim NewBrowsePath As String = CurrentSelectedDeviceItem.AccessiblePath

                            Dim SelectedFileFolderItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                            Dim CurrentSelectedFileFolderItem As String = CType(SelectedFileFolderItem.Content, FileBrowserListViewItem).FileFolderName
                            Dim ParentFolder = Directory.GetParent(CurrentSelectedFileFolderItem).FullName

                            'If the previous folder is non-existant return to the device selection
                            If Not ParentFolder = NewBrowsePath Then
                                ReturnTo()
                            Else
                                'Play the 'return' sound effect
                                PlayBackgroundSound(Sounds.Back)

                                'Return to device selection
                                DevicesListView.Focus()
                                SelectedDeviceItem.Focus()
                            End If
                        Else
                            'Close the 'File Explorer'
                            BeginAnimation(OpacityProperty, ClosingAnimation)
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

                    ElseIf TypeOf FocusedItem Is ListView Then

                        Dim CurrentListView As ListView = CType(FocusedItem, ListView)

                        If CurrentListView.Name = "FilesFoldersListView" Then
                            If Not String.IsNullOrEmpty(LastPath) Then
                                ReturnTo()
                            End If
                        End If

                    End If
                Case Key.S
                    ShowHideSideOptions()
                Case Key.X
                    GoTo_Device_FileFolder_OR_DoSideMenuAction()
                Case Key.Up
                    MoveUp()
                Case Key.Down
                    MoveDown()
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub FileExplorer_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
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

                If MainGamepadButton_A_Button_Pressed Then
                    GoTo_Device_FileFolder_OR_DoSideMenuAction()
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                    If TypeOf FocusedItem Is ListViewItem Then

                        Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

                        If CurrentListView.Name = "FilesFoldersListView" Then
                            'Get the previously selected device
                            Dim SelectedDeviceListViewItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                            Dim CurrentSelectedDeviceItem As DeviceListViewItem = CType(SelectedDeviceListViewItem.Content, DeviceListViewItem)
                            Dim NewBrowsePath As String = CurrentSelectedDeviceItem.AccessiblePath

                            Dim SelectedFileFolderListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                            Dim CurrentSelectedFileFolderItem As FileBrowserListViewItem = CType(SelectedFileFolderListViewItem.Content, FileBrowserListViewItem)
                            Dim ParentFolder = Directory.GetParent(CurrentSelectedFileFolderItem.FileFolderName).FullName

                            'If the previous folder is non-existant return to the device selection
                            If Not ParentFolder = NewBrowsePath Then
                                ReturnTo()
                            Else
                                'Play the 'return' sound effect
                                PlayBackgroundSound(Sounds.Back)

                                'Return to device selection
                                Dim SelectedDeviceItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                                DevicesListView.Focus()
                                SelectedDeviceItem.Focus()
                            End If
                        Else
                            'Close the 'File Explorer'
                            BeginAnimation(OpacityProperty, ClosingAnimation)
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

                    ElseIf TypeOf FocusedItem Is ListView Then

                        Dim CurrentListView As ListView = CType(FocusedItem, ListView)

                        If CurrentListView.Name = "FilesFoldersListView" Then
                            If Not String.IsNullOrEmpty(LastPath) Then
                                ReturnTo()
                            End If
                        End If

                    End If

                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    CopySelectedItem()
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
                ElseIf MainGamepadButton_Start_Button_Pressed Then
                    ShowHideSideOptions()
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

            OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_options.png", UriKind.RelativeOrAbsolute))
            OptionsButton.Width = 48
            Canvas.SetTop(OptionsButton, 955)
            Canvas.SetLeft(OptionsButton, 385)

            ActionButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
            EnterButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

#Region "Navigation"

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
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".dll"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDLL.png", UriKind.RelativeOrAbsolute))
                Case ".sys"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileSYS.png", UriKind.RelativeOrAbsolute))
                Case ".tmp"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTMP.png", UriKind.RelativeOrAbsolute))
                Case ".ini"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileINI.png", UriKind.RelativeOrAbsolute))
                Case ".iso"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".cue"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".jpg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".jpeg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".png"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FilePNG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".txt"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTXT.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".bin"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".BIN"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mp4"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP4.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mpeg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mpg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".flv"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileFLV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".webm"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBM.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mkv"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMKV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mov"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMOV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".avi"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAVI.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".wmv"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWMV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".m4v"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM4V.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".3gp"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3GP.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".3g2"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3G2.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".f4v"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileF4V.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mts"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMTS.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".ts"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTS.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".m2ts"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM2TS.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".webp"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBP.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".bmp"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBMP.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".tif"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIF.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".tiff"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIFF.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".gif"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileGIF.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".apng"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAPNG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".heif"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileHEIF.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".wav"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWAV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".mp3"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP3.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".json"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJSON.png", UriKind.RelativeOrAbsolute))
                Case ".rne"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileRNE.png", UriKind.RelativeOrAbsolute))
                Case ".dat"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDAT.png", UriKind.RelativeOrAbsolute))
                Case ".EXE"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".tps"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTPS.png", UriKind.RelativeOrAbsolute))
                Case ".trm"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTRM.png", UriKind.RelativeOrAbsolute))
                Case ".vdf"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileVDF.png", UriKind.RelativeOrAbsolute))
                Case ".dds"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDDS.png", UriKind.RelativeOrAbsolute))
                Case ".md5"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMD5.png", UriKind.RelativeOrAbsolute))
                Case ".ogg"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGG.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case ".ogv"
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGV.png", UriKind.RelativeOrAbsolute))
                    FExecutable = True
                Case Else
                    FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileIcon.png", UriKind.RelativeOrAbsolute))
                    FExecutable = False
            End Select

            FilesFoldersListView.Items.Add(New ListViewItem With {.ContentTemplate = FilesFoldersListView.ItemTemplate, .Content = New FileBrowserListViewItem() With {.FileFolderName = FileInFolder,
                                           .IsFileFolderSelected = Visibility.Hidden,
                                           .Type = "File",
                                           .FileFolderIcon = FExtensionImage,
                                           .IsExecutable = FExecutable}
                                           })
        Next

        ShowListAnimation()

        If FilesFoldersListView.Items.Count > 0 Then
            'Focus the first item
            Dim LastSelectedListViewItem As ListViewItem = CType(FilesFoldersListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            LastSelectedListViewItem.Focus()

            'Convert to FileBrowserListViewItem to set the border visibility on the first item
            Dim LastSelectedItem As FileBrowserListViewItem = CType(LastSelectedListViewItem.Content, FileBrowserListViewItem)
            LastSelectedItem.IsFileFolderSelected = Visibility.Visible
        Else
            FilesFoldersListView.Focus()
        End If
    End Sub

    'Open next device, folder or do a side menu action
    Private Sub GoTo_Device_FileFolder_OR_DoSideMenuAction()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Play the 'select' sound effect
            PlayBackgroundSound(Sounds.SelectItem)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            'We are in the Devices ListView
            If CurrentListView.Name = "DevicesListView" Then
                Dim SelectedListViewItem As ListViewItem = CType(DevicesListView.SelectedItem, ListViewItem)
                Dim CurrentSelectedItem As DeviceListViewItem = CType(SelectedListViewItem.Content, DeviceListViewItem)
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
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".dll"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDLL.png", UriKind.RelativeOrAbsolute))
                        Case ".sys"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileSYS.png", UriKind.RelativeOrAbsolute))
                        Case ".tmp"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTMP.png", UriKind.RelativeOrAbsolute))
                        Case ".ini"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileINI.png", UriKind.RelativeOrAbsolute))
                        Case ".iso"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".cue"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".jpg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".jpeg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".png"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FilePNG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".txt"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTXT.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".bin"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".BIN"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mp4"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP4.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mpeg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mpg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".flv"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileFLV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".webm"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBM.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mkv"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMKV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mov"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMOV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".avi"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAVI.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".wmv"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWMV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".m4v"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM4V.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".3gp"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3GP.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".3g2"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3G2.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".f4v"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileF4V.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mts"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMTS.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".ts"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTS.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".m2ts"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM2TS.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".webp"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBP.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".bmp"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBMP.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".tif"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIF.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".tiff"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIFF.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".gif"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileGIF.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".apng"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAPNG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".heif"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileHEIF.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".wav"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWAV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".mp3"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP3.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".json"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJSON.png", UriKind.RelativeOrAbsolute))
                        Case ".rne"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileRNE.png", UriKind.RelativeOrAbsolute))
                        Case ".dat"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDAT.png", UriKind.RelativeOrAbsolute))
                        Case ".EXE"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".tps"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTPS.png", UriKind.RelativeOrAbsolute))
                        Case ".trm"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTRM.png", UriKind.RelativeOrAbsolute))
                        Case ".vdf"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileVDF.png", UriKind.RelativeOrAbsolute))
                        Case ".dds"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDDS.png", UriKind.RelativeOrAbsolute))
                        Case ".md5"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMD5.png", UriKind.RelativeOrAbsolute))
                        Case ".ogg"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGG.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case ".ogv"
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGV.png", UriKind.RelativeOrAbsolute))
                            FExecutable = True
                        Case Else
                            FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileIcon.png", UriKind.RelativeOrAbsolute))
                            FExecutable = False
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
                Dim LastSelectedItem As FileBrowserListViewItem = CType(LastSelectedListViewItem.Content, FileBrowserListViewItem)
                LastSelectedItem.IsFileFolderSelected = Visibility.Visible

            ElseIf CurrentListView.Name = "FilesFoldersListView" Then
                Dim SelectedListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                Dim CurrentSelectedItem As FileBrowserListViewItem = CType(SelectedListViewItem.Content, FileBrowserListViewItem)

                If CurrentSelectedItem.Type = "Folder" Then

                    Dim NewBrowsePath As String = CurrentSelectedItem.FileFolderName
                    LastPath = CurrentSelectedItem.FileFolderName
                    LastSelectedIndex = CurrentListView.SelectedIndex

                    'Open the new folder
                    OpenNewFolder(NewBrowsePath)
                ElseIf CurrentSelectedItem.Type = "File" Then

                    Select Case Path.GetExtension(CurrentSelectedItem.FileFolderName)
                        Case ".mp4", ".mpeg", ".mpg", ".flv", ".webm", ".mkv", ".mov", ".avi", ".wmv", ".m4v", ".3gp", ".3g2", ".f4v", ".mts", ".ts", ".m2ts"
                            PauseInput = True

                            Dim NewMediaPlayer As New SystemMediaPlayer() With {.Top = Top, .Left = Left, .ShowActivated = True, .Opener = "FileExplorer", .VideoFile = CurrentSelectedItem.FileFolderName}
                            NewMediaPlayer.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                            NewMediaPlayer.Show()
                        Case ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff", ".gif", ".apng", ".heif"
                            'Future build
                        Case ".exe"
                            LaunchGameOrApplication(CurrentSelectedItem)
                    End Select

                End If

            End If

        ElseIf TypeOf FocusedItem Is Button Then
            Dim SelectedListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
            Dim CurrentSelectedItem As FileBrowserListViewItem = CType(SelectedListViewItem.Content, FileBrowserListViewItem)
            Dim SelectedButton As Button = CType(FocusedItem, Button)

            PlayBackgroundSound(Sounds.SelectItem)

            Select Case SelectedButton.Content.ToString()
                Case "Add to Game Library"
                    If AddGameFromFileExplorer(CurrentSelectedItem) Then
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Added to Game Library", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                    Else
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Cannot add to Game Library", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                    End If
                Case "Add to Apps Library"
                    If AddApplicationFromFileExplorer(CurrentSelectedItem) Then
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Added to App Library", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                    Else
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Cannot add to App Library", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                    End If
                Case "Add to Copy List"
                    SelectedItemToCopy = CurrentSelectedItem
                    ActionTextBlock.Text = "Paste"

                    'Notify that the item is in the clipboard
                    OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Added to clipboard", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                Case "Delete"
                    Dim OldFileFolderName As String = CurrentSelectedItem.FileFolderName
                    Dim OldFileFolderIcon As BitmapImage = CurrentSelectedItem.FileFolderIcon

                    If DeleteFileOrFolder(CurrentSelectedItem) Then
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Deleted", OldFileFolderName, OldFileFolderIcon)
                    Else
                        OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Could not delete", CurrentSelectedItem.FileFolderName, CurrentSelectedItem.FileFolderIcon)
                    End If
                Case "Infos"

                Case "Start"
                    LaunchGameOrApplication(CurrentSelectedItem)
                Case "Move"

            End Select

        End If
    End Sub

    Private Sub ReturnTo()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Play the 'return' sound effect
            PlayBackgroundSound(Sounds.Back)

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            If CurrentListView.Name = "FilesFoldersListView" Then
                Dim SelectedListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                Dim CurrentSelectedItem As FileBrowserListViewItem = CType(SelectedListViewItem.Content, FileBrowserListViewItem)
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
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".dll"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDLL.png", UriKind.RelativeOrAbsolute))
                            Case ".sys"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileSYS.png", UriKind.RelativeOrAbsolute))
                            Case ".tmp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTMP.png", UriKind.RelativeOrAbsolute))
                            Case ".ini"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileINI.png", UriKind.RelativeOrAbsolute))
                            Case ".iso"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".cue"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".jpg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".jpeg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".png"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FilePNG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".txt"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTXT.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".bin"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".BIN"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mp4"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP4.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mpeg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mpg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".flv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileFLV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".webm"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBM.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mkv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMKV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mov"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMOV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".avi"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAVI.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".wmv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWMV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".m4v"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM4V.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".3gp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3GP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".3g2"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3G2.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".f4v"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileF4V.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMTS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".ts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".m2ts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM2TS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".webp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".bmp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBMP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tiff"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIFF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".gif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileGIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".apng"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAPNG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".heif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileHEIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".wav"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWAV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mp3"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP3.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".json"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJSON.png", UriKind.RelativeOrAbsolute))
                            Case ".rne"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileRNE.png", UriKind.RelativeOrAbsolute))
                            Case ".dat"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDAT.png", UriKind.RelativeOrAbsolute))
                            Case ".EXE"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tps"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTPS.png", UriKind.RelativeOrAbsolute))
                            Case ".trm"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTRM.png", UriKind.RelativeOrAbsolute))
                            Case ".vdf"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileVDF.png", UriKind.RelativeOrAbsolute))
                            Case ".dds"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDDS.png", UriKind.RelativeOrAbsolute))
                            Case ".md5"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMD5.png", UriKind.RelativeOrAbsolute))
                            Case ".ogg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".ogv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case Else
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileIcon.png", UriKind.RelativeOrAbsolute))
                                FExecutable = False
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

                    Dim LastSelectedItem As FileBrowserListViewItem = CType(LastSelectedListViewItem.Content, FileBrowserListViewItem)
                    LastSelectedItem.IsFileFolderSelected = Visibility.Visible
                End If

            End If

        ElseIf TypeOf FocusedItem Is Button Then

            'Play the 'return' sound effect
            PlayBackgroundSound(Sounds.Back)

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

        ElseIf TypeOf FocusedItem Is ListView Then

            Dim CurrentListView As ListView = CType(FocusedItem, ListView)

            If CurrentListView.Name = "FilesFoldersListView" Then

                If Not String.IsNullOrEmpty(LastPath) Then

                    FilesFoldersListView.Items.Clear()

                    'List all folders
                    For Each Folder In Directory.GetDirectories(Directory.GetParent(LastPath).FullName)

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
                    For Each FileInFolder In Directory.GetFiles(Directory.GetParent(LastPath).FullName)
                        Dim FInfo As New FileInfo(FileInFolder)
                        Dim FExtensionImage As BitmapImage
                        Dim FExecutable As Boolean = False

                        Select Case FInfo.Extension
                            Case ".exe"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".dll"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDLL.png", UriKind.RelativeOrAbsolute))
                            Case ".sys"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileSYS.png", UriKind.RelativeOrAbsolute))
                            Case ".tmp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTMP.png", UriKind.RelativeOrAbsolute))
                            Case ".ini"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileINI.png", UriKind.RelativeOrAbsolute))
                            Case ".iso"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".cue"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/CDDrive.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".jpg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".jpeg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".png"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FilePNG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".txt"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTXT.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".bin"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".BIN"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBIN.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mp4"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP4.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mpeg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mpg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMPG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".flv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileFLV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".webm"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBM.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mkv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMKV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mov"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMOV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".avi"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAVI.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".wmv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWMV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".m4v"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM4V.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".3gp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3GP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".3g2"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/File3G2.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".f4v"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileF4V.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMTS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".ts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".m2ts"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileM2TS.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".webp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWEBP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".bmp"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileBMP.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tiff"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTIFF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".gif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileGIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".apng"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileAPNG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".heif"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileHEIF.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".wav"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileWAV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".mp3"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMP3.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".json"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileJSON.png", UriKind.RelativeOrAbsolute))
                            Case ".rne"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileRNE.png", UriKind.RelativeOrAbsolute))
                            Case ".dat"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDAT.png", UriKind.RelativeOrAbsolute))
                            Case ".EXE"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileEXE.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".tps"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTPS.png", UriKind.RelativeOrAbsolute))
                            Case ".trm"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileTRM.png", UriKind.RelativeOrAbsolute))
                            Case ".vdf"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileVDF.png", UriKind.RelativeOrAbsolute))
                            Case ".dds"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileDDS.png", UriKind.RelativeOrAbsolute))
                            Case ".md5"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileMD5.png", UriKind.RelativeOrAbsolute))
                            Case ".ogg"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGG.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case ".ogv"
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileOGV.png", UriKind.RelativeOrAbsolute))
                                FExecutable = True
                            Case Else
                                FExtensionImage = New BitmapImage(New Uri("/Icons/FileIcons/FileIcon.png", UriKind.RelativeOrAbsolute))
                                FExecutable = False
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

                    Dim LastSelectedItem As FileBrowserListViewItem = CType(LastSelectedListViewItem.Content, FileBrowserListViewItem)
                    LastSelectedItem.IsFileFolderSelected = Visibility.Visible

                End If

            End If

        End If
    End Sub

    Private Sub MoveUp()
        PlayBackgroundSound(Sounds.Move)

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim NextSelectedButtonNumber As Integer = GetIntegerOnly(SelectedButton.Name) - 1
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

    Private Sub MoveDown()
        PlayBackgroundSound(Sounds.Move)

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is Button Then

            Dim SelectedButton = CType(FocusedItem, Button)
            Dim NextSelectedButtonNumber As Integer = GetIntegerOnly(SelectedButton.Name) + 1
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

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(FilesFoldersListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(FilesFoldersListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

#Region "Selection Changes"

    Private Sub DevicesListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DevicesListView.SelectionChanged

        If DevicesListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            'Play the 'move' sound effect
            PlayBackgroundSound(Sounds.Move)

            Dim RemovedItem As ListViewItem = CType(e.RemovedItems(0), ListViewItem)
            Dim AddedItem As ListViewItem = CType(e.AddedItems(0), ListViewItem)

            'Get the previous and next selected item and convert to DeviceListViewItem
            Dim PreviousItem As DeviceListViewItem = CType(RemovedItem.Content, DeviceListViewItem)
            Dim NewSelectedItem As DeviceListViewItem = CType(AddedItem.Content, DeviceListViewItem)

            'Set the selection border visibility
            NewSelectedItem.IsDeviceSelected = Visibility.Visible
            PreviousItem.IsDeviceSelected = Visibility.Hidden
        End If

    End Sub

    Private Sub FilesFoldersListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FilesFoldersListView.SelectionChanged
        If FilesFoldersListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            'Play the 'move' sound effect
            PlayBackgroundSound(Sounds.Move)

            Dim RemovedItem As ListViewItem = CType(e.RemovedItems(0), ListViewItem)
            Dim AddedItem As ListViewItem = CType(e.AddedItems(0), ListViewItem)

            'Get the previous and next selected item and convert to FileBrowserListViewItem
            Dim PreviousItem = CType(RemovedItem.Content, FileBrowserListViewItem)
            Dim NewSelectedItem = CType(AddedItem.Content, FileBrowserListViewItem)

            'Set the selection border visibility
            NewSelectedItem.IsFileFolderSelected = Visibility.Visible
            PreviousItem.IsFileFolderSelected = Visibility.Hidden

            'If the length of 'FileFolderName' (complete path for the most part) is too long then we animate the displayed text
            If NewSelectedItem.FileFolderName.Length >= 85 Then

                'Here it gets a bit complicated as we need to access 'FileFolderNameTextBlock' from the custom DataTemplate
                Dim FileFolderContentPresenter As ContentPresenter = FindVisualChild(Of ContentPresenter)(AddedItem)
                Dim FileFolderDataTemplate As DataTemplate = FilesFoldersListView.ItemTemplate
                Dim SelectedItemDescription As TextBlock = TryCast(FileFolderDataTemplate.FindName("FileFolderNameTextBlock", FileFolderContentPresenter), TextBlock)

                Animate(SelectedItemDescription, Canvas.LeftProperty, 105, -SelectedItemDescription.ActualWidth, New Duration(TimeSpan.FromMilliseconds(10400)), True)
            End If

            If NewSelectedItem.Type = "File" Then
                Dim SelectedFileExtension As String = Path.GetExtension(NewSelectedItem.FileFolderName)

                Select Case SelectedFileExtension
                    Case ".mp4", ".mpeg", ".mpg", ".flv", ".webm", ".mkv", ".mov", ".avi", ".wmv", ".m4v", ".3gp", ".3g2", ".f4v", ".mts", ".ts", ".m2ts"
                        CrossButtonLabel.Text = "Play Video"
                    Case ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff", ".gif", ".apng", ".heif"
                        CrossButtonLabel.Text = "Show Image"
                    Case ".exe"
                        CrossButtonLabel.Text = "Run"
                    Case Else
                        'More in future builds
                        CrossButtonLabel.Text = "Enter"
                End Select
            Else
                CrossButtonLabel.Text = "Enter"
            End If
        End If
    End Sub

#End Region

    Public Sub CopyFile(CopyFrom As String, CopyTo As String, Optional TransferIcon As ImageSource = Nothing)
        Dim NewCopyWindow As New CopyWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewCopyWindow.CopyDescriptionTextBlock.Text = ""
        NewCopyWindow.CopyProgressBar.Maximum = 1
        NewCopyWindow.CopyFrom = CopyFrom
        NewCopyWindow.CopyTo = CopyTo
        NewCopyWindow.FileImage.Source = TransferIcon

        Animate(NewCopyWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
        NewCopyWindow.Show()
    End Sub

    Private Sub CopySelectedItem()

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If TypeOf FocusedItem Is ListViewItem Then

            'Get the ListView of the selected item
            Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))

            If CurrentListView.Name = "FilesFoldersListView" Then
                Dim SelectedListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                Dim SelectedItem = CType(SelectedListViewItem.Content, FileBrowserListViewItem)

                PlayBackgroundSound(Sounds.SelectItem)

                If ActionTextBlock.Text = "Copy" Then

                    SelectedItemToCopy = SelectedItem
                    ActionTextBlock.Text = "Paste"

                    'Notify that the item is in the clipboard
                    OrbisNotifications.NotificationPopup(FileExplorerCanvas, "Added to clipboard", SelectedItemToCopy.FileFolderName, SelectedItem.FileFolderIcon.UriSource.OriginalString)

                ElseIf ActionTextBlock.Text = "Paste" Then

                    'Check if the selected item is a file or folder
                    If SelectedItem.Type = "File" Then

                        Dim ClipboardFileInfo As New FileInfo(SelectedItemToCopy.FileFolderName)
                        Dim SelectedItemFileInfo As New FileInfo(SelectedItem.FileFolderName)

                        'If the selected item is a file then get it's DirectoryName and paste the file in the same directory
                        CopyFile(SelectedItemToCopy.FileFolderName, SelectedItemFileInfo.DirectoryName + "\" + ClipboardFileInfo.Name, SelectedItemToCopy.FileFolderIcon)
                    ElseIf SelectedItem.Type = "Folder" Then

                        Dim DInfo As New DirectoryInfo(SelectedItem.FileFolderName)
                        Dim ClipboardFileInfo As New FileInfo(SelectedItemToCopy.FileFolderName)

                        CopyFile(SelectedItemToCopy.FileFolderName, DInfo.Parent.FullName + "\" + ClipboardFileInfo.Name, SelectedItemToCopy.FileFolderIcon)
                    End If

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
                'We are in the Files & Folders ListView
                PlayBackgroundSound(Sounds.SelectItem)

                Dim SelectedListViewItem As ListViewItem = CType(FilesFoldersListView.SelectedItem, ListViewItem)
                Dim SelectedItem As FileBrowserListViewItem = CType(SelectedListViewItem.Content, FileBrowserListViewItem)

                'Show side menu options
                If Canvas.GetLeft(RightMenu) = 1925 Then
                    Animate(RightMenu, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))

                    Select Case SelectedItem.Type
                        Case "File"

                            If SelectedItem.IsExecutable Then
                                'If the file is executable then add the 'Start' and 'Add to game library' options
                                SettingButton1.Content = "Start"
                                SettingButton2.Content = "Add to Copy List"
                                SettingButton3.Content = "Add to Game Library"
                                SettingButton4.Content = "Add to Apps Library"
                                SettingButton5.Content = "Delete"
                                SettingButton6.Content = "Infos"

                                'Show the buttons
                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton5, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton6, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            Else
                                'If the file is not executable then add default options
                                SettingButton1.Content = "Add to Copy List"
                                SettingButton2.Content = "Move"
                                SettingButton3.Content = "Delete"
                                SettingButton4.Content = "Infos"

                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            End If

                            'Focus the first option
                            SettingButton1.Focus()

                        Case "Folder"

                            'If the folder is executable (like PS3 game backups) add the 'Start' and 'Add to game library' options
                            If SelectedItem.IsExecutable Then
                                SettingButton1.Content = "Start"
                                SettingButton2.Content = "Add to Copy List"
                                SettingButton3.Content = "Add to Game Library"
                                SettingButton4.Content = "Add to Apps Library"
                                SettingButton5.Content = "Delete"
                                SettingButton6.Content = "Infos"

                                Animate(SettingButton1, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton2, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton3, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton4, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton5, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                                Animate(SettingButton6, Canvas.LeftProperty, 1925, 1430, New Duration(TimeSpan.FromMilliseconds(300)))
                            Else
                                SettingButton1.Content = "Add to Copy List"
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

                    'Remove SettingButton5 & SettingButton 6 if they were shown
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
        ElseIf TypeOf FocusedItem Is Button Then
            'Remove the options side menu
            Animate(RightMenu, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            Animate(SettingButton1, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton2, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton3, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))
            Animate(SettingButton4, Canvas.LeftProperty, 1430, 1925, New Duration(TimeSpan.FromMilliseconds(300)))

            'Remove SettingButton5 & SettingButton 6 if they were shown
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

    Private Function AddGameFromFileExplorer(SelectedFileBrowserListViewItem As FileBrowserListViewItem) As Boolean
        Dim IsDirectory As Boolean = (File.GetAttributes(SelectedFileBrowserListViewItem.FileFolderName) And FileAttributes.Directory) = FileAttributes.Directory

        If Not IsDirectory AndAlso SupportedGameExtension(SelectedFileBrowserListViewItem.FileFolderName) Then
            Dim FileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(SelectedFileBrowserListViewItem.FileFolderName)
            Dim FileVersionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(SelectedFileBrowserListViewItem.FileFolderName)
            Dim NewTitle As String

            'Set the title
            If Not String.IsNullOrEmpty(FileVersionInfo.ProductName) Then
                NewTitle = FileVersionInfo.ProductName
            Else
                NewTitle = FileNameWithoutExtension
            End If

            'Add to GameList
            Using SW As New StreamWriter(GameLibraryPath, True)
                SW.WriteLine("PC;" + NewTitle + ";" + SelectedFileBrowserListViewItem.FileFolderName + ";" + "ShowInLibrary=True" + ";" + "ShowOnHome=True")
                SW.Close()
            End Using

            'Reload the Home menu
            For Each Win In System.Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.MainWindow" Then
                    CType(Win, MainWindow).ReloadHome()
                    CType(Win, MainWindow).PauseInput = True
                    Activate()
                    Exit For
                End If
            Next

            Return True
        Else
            Return False
        End If
    End Function

    Private Function AddApplicationFromFileExplorer(SelectedFileBrowserListViewItem As FileBrowserListViewItem) As Boolean
        Dim IsDirectory As Boolean = (File.GetAttributes(SelectedFileBrowserListViewItem.FileFolderName) And FileAttributes.Directory) = FileAttributes.Directory

        If Not IsDirectory AndAlso Path.GetExtension(SelectedFileBrowserListViewItem.FileFolderName) = ".exe" Then
            Dim FileNameWithoutExtension As String = Path.GetFileNameWithoutExtension(SelectedFileBrowserListViewItem.FileFolderName)
            Dim FileVersionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(SelectedFileBrowserListViewItem.FileFolderName)
            Dim NewTitle As String

            'Set the title
            If Not String.IsNullOrEmpty(FileVersionInfo.ProductName) Then
                NewTitle = FileVersionInfo.ProductName
            Else
                NewTitle = FileNameWithoutExtension
            End If

            'Add to AppsList
            Using SW As New StreamWriter(FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.txt", True)
                SW.WriteLine("App=" + NewTitle + ";" + SelectedFileBrowserListViewItem.FileFolderName + ";" + "ShowInLibrary=True" + ";" + "ShowOnHome=True")
                SW.Close()
            End Using

            'Reload the Home menu
            For Each Win In System.Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.MainWindow" Then
                    CType(Win, MainWindow).ReloadHome()
                    CType(Win, MainWindow).PauseInput = True
                    Activate()
                    Exit For
                End If
            Next

            Return True
        Else
            Return False
        End If
    End Function

    Private Function DeleteFileOrFolder(SelectedFileBrowserListViewItem As FileBrowserListViewItem) As Boolean
        If SelectedFileBrowserListViewItem IsNot Nothing Then
            Dim IsDirectory As Boolean = (File.GetAttributes(SelectedFileBrowserListViewItem.FileFolderName) And FileAttributes.Directory) = FileAttributes.Directory
            If IsDirectory Then
                Directory.Delete(SelectedFileBrowserListViewItem.FileFolderName, True)
                FilesFoldersListView.Items.Remove(FilesFoldersListView.SelectedItem)
                SelectNextListViewItem()
                Return True
            Else
                File.Delete(SelectedFileBrowserListViewItem.FileFolderName)
                FilesFoldersListView.Items.Remove(FilesFoldersListView.SelectedItem)
                SelectNextListViewItem()
                Return True
            End If
        Else
            Return False
        End If

        FilesFoldersListView.Items.Refresh()
    End Function

    Private Sub SelectNextListViewItem()
        Dim TotalListViewItems As Integer = FilesFoldersListView.Items.Count
        Dim CurrentIndex As Integer = FilesFoldersListView.SelectedIndex

        If CurrentIndex + 1 <= TotalListViewItems Then
            'Simply select the next item in the collection
            FilesFoldersListView.SelectedIndex = CurrentIndex + 1
        Else
            'Check if there any items in the collection and select the previous item
            If TotalListViewItems > 0 AndAlso CurrentIndex - 1 < TotalListViewItems Then
                FilesFoldersListView.SelectedIndex = CurrentIndex - 1
            End If
        End If
    End Sub

    Private Sub LaunchGameOrApplication(SelectedFile As FileBrowserListViewItem)
        'Play 'start' sound effect
        PlayBackgroundSound(Sounds.SelectItem)

        'Start the game
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.MainWindow" Then
                CType(Win, MainWindow).StartedGameExecutable = SelectedFile.FileFolderName
                Exit For
            End If
        Next

        StartGame(SelectedFile.FileFolderName)
        BeginAnimation(OpacityProperty, ClosingAnimation)
    End Sub

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

                OrbisDisplay.SetScaling(FileExplorerWindow, FileExplorerCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(FileExplorerWindow, FileExplorerCanvas)
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
            .Opener = "FileExplorer",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
