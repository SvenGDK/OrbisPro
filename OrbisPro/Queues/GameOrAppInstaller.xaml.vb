Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Media.Animation

Public Class GameOrAppInstaller

    Public GameAppToInstall As String = ""
    Public TotalFiles As Integer

    Dim SystemInstallLocation As String = My.Computer.FileSystem.CurrentDirectory + "\Games"
    Dim GameConfig As New INI.IniFile(SystemInstallLocation + "\GameList.ini")

    Dim WithEvents InstallWorker As New BackgroundWorker() With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}
    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Public Structure WorkerArgs
        Public Property InstallFrom As String
        Public Property InstallLocation As String
        Public Property Type As String
        Public Property PlaceInLibrary As Boolean
        Public Property PlaceOnMainMenu As Boolean
    End Structure

    Private Sub GameOrAppInstaller_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Dim T1 As New MainWindow()
        T1.Show()

        'Create game directories at default install location
        If Not Directory.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games") Then
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS1")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS2")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS3")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS4")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\NES")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\SNES")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\SMS")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\MegaDrive")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\Saturn")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\Dreamcast")
        End If

        For Each Device In Directory.GetLogicalDrives()
            Dim DeviceInfo As New DriveInfo(Device)

            If DeviceInfo.IsReady And DeviceInfo.DriveType = DriveType.Fixed Then
                'Add internal storage
                DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = DeviceInfo.Name,
                                          .DeviceType = "Fixed",
                                          .AccessiblePath = DeviceInfo.Name,
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/Storage.png", UriKind.RelativeOrAbsolute))}})

            ElseIf DeviceInfo.IsReady And DeviceInfo.DriveType = DriveType.Removable Then
                'Add removable devices
                DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = DeviceInfo.VolumeLabel,
                                          .DeviceType = "Removable",
                                          .AccessiblePath = DeviceInfo.Name,
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/USBDevice.png", UriKind.RelativeOrAbsolute))}})
            End If
        Next

        'Add system folder to the list
        DevicesListView.Items.Add(New ListViewItem With {.ContentTemplate = DevicesListView.ItemTemplate, .Content = New DeviceListViewItem() With {.DeviceName = "System",
                                          .DeviceType = "Fixed",
                                          .AccessiblePath = My.Computer.FileSystem.CurrentDirectory + "\system\",
                                          .IsDeviceSelected = Visibility.Hidden,
                                          .DeviceIcon = New BitmapImage(New Uri("/Icons/InternalStorage.png", UriKind.RelativeOrAbsolute))}})

        'Focus the item itself
        Dim LastSelectedListViewItem As ListViewItem = DevicesListView.ItemContainerGenerator.ContainerFromIndex(0)
        LastSelectedListViewItem.Focus()

        'Convert to DeviceListViewItem to set the border visibility on the first item
        Dim LastSelectedItem As DeviceListViewItem = CType(DevicesListView.Items(0).Content, DeviceListViewItem)
        LastSelectedItem.IsDeviceSelected = Visibility.Visible

    End Sub

    Private Sub DevicesListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DevicesListView.SelectionChanged
        If DevicesListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem As DeviceListViewItem = CType(e.RemovedItems(0).Content, DeviceListViewItem)
            Dim SelectedItem As DeviceListViewItem = CType(e.AddedItems(0).Content, DeviceListViewItem)

            SelectedItem.IsDeviceSelected = Visibility.Visible
            PreviousItem.IsDeviceSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub GameOrAppInstaller_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.I Then

            Dim AddToLibrary As Boolean = False
            Dim AddToMainMenu As Boolean = False

            If GetGameType(GameAppToInstall).Platform = "PS2" Then

                If LibraryCheckBox.IsChecked Then AddToLibrary = True
                If MainMenuCheckBox.IsChecked Then AddToMainMenu = True

                DevicesListView.Opacity = 0
                LibraryCheckBox.Opacity = 0
                MainMenuCheckBox.Opacity = 0
                AppShortcutTextBlock.Opacity = 0
                InstallProgressBar.Opacity = 1

                InstallLabel.Content = "Cancel"
                SetupWizardTitle.Text = "Installing, please wait ..."
                InstallStatusTextBlock.Text = "File 0 of 1"

                InstallProgressBar.Maximum = 1
                TotalFiles = 1

                InstallWorker.RunWorkerAsync(New WorkerArgs() With {.InstallFrom = GameAppToInstall,
                .InstallLocation = My.Computer.FileSystem.CurrentDirectory + "\Games\PS2\" + GetGameType(GameAppToInstall).FileName,
                .Type = "PS2",
                .PlaceInLibrary = AddToLibrary,
                .PlaceOnMainMenu = AddToMainMenu})

            End If
        ElseIf e.Key = Key.Escape Then
            BeginAnimation(OpacityProperty, ClosingAnim)
        End If

    End Sub

    Private Sub InstallWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles InstallWorker.DoWork

        Dim WorkArgs As WorkerArgs = CType(e.Argument, WorkerArgs)

        Select Case WorkArgs.Type
            Case "PS1"
                File.Copy(WorkArgs.InstallFrom, WorkArgs.InstallLocation, True)
                InstallWorker.ReportProgress(1)

                If WorkArgs.PlaceInLibrary And WorkArgs.PlaceOnMainMenu Then
                    GameConfig.IniWriteValue("PS1", "Game1", WorkArgs.InstallLocation + ";ShowInLibrary;ShowOnMainMenu")
                ElseIf WorkArgs.PlaceOnMainMenu Then
                    GameConfig.IniWriteValue("PS1", "Game1", WorkArgs.InstallLocation + ";ShowInLibrary;ShowOnMainMenu")
                ElseIf WorkArgs.PlaceInLibrary Then
                    GameConfig.IniWriteValue("PS1", "Game1", WorkArgs.InstallLocation + ";ShowInLibrary;ShowOnMainMenu")
                End If

            Case "PS2"
                File.Copy(WorkArgs.InstallFrom, WorkArgs.InstallLocation, True)
                InstallWorker.ReportProgress(1)

                If WorkArgs.PlaceInLibrary And WorkArgs.PlaceOnMainMenu Then
                    GameConfig.IniWriteValue("PS2", "Game1", WorkArgs.InstallLocation + ";ShowInLibrary;ShowOnMainMenu")
                ElseIf WorkArgs.PlaceOnMainMenu Then
                    GameConfig.IniWriteValue("PS2", "Game1", WorkArgs.InstallLocation + ";ShowOnMainMenu")
                ElseIf WorkArgs.PlaceInLibrary Then
                    GameConfig.IniWriteValue("PS2", "Game1", WorkArgs.InstallLocation + ";ShowInLibrary")
                End If

            Case "PS3"

        End Select

    End Sub

    Public Function GetGameType(GamePathOrFile As String) As (Platform As String, FileName As String)

        Dim IsDirectory As Boolean = (File.GetAttributes(GamePathOrFile) And FileAttributes.Directory) = FileAttributes.Directory

        If IsDirectory = False Then

            Dim FInfo As New FileInfo(GamePathOrFile)

            Select Case FInfo.Extension
                Case ".iso"

                    Return ("PS2", FInfo.Name)
                Case ".bin"

                    Return ("PS1", FInfo.Name)
                Case ".BIN"

                    Return ("PS3", FInfo.Name)
                Case ".gdi"
                    Return ("Dreamcast", FInfo.Name)
                Case ".cdi"
                    Return ("Dreamcast", FInfo.Name)
                Case Else
                    Return ("", "")
            End Select
        Else

            If Directory.Exists(GamePathOrFile + "\PS3_GAME") Then
                Return ("PS3", "")
            Else
                Return ("", "")
            End If

        End If

    End Function

    Private Sub InstallWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles InstallWorker.ProgressChanged

        If Not Dispatcher.CheckAccess() Then
            Dispatcher.BeginInvoke(Sub() InstallStatusTextBlock.Text = "")
        Else
            InstallStatusTextBlock.Text = "File " + e.ProgressPercentage.ToString + " of " + TotalFiles.ToString
        End If

        InstallProgressBar.Value = e.ProgressPercentage
    End Sub

    Private Sub InstallWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles InstallWorker.RunWorkerCompleted

        InstallLabel.Content = "Close"
        SetupWizardTitle.Text = "Installation finished !" + vbCrLf + "You can close this window now."

        For Each Win In Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.MainWindow" Then
                CType(Win, MainWindow).AddNewApp("Metal Gear Solid 2: Sons of Liberty", My.Computer.FileSystem.CurrentDirectory + "\Games\PS2\" + GetGameType(GameAppToInstall).FileName)
                Exit For
            End If
        Next

    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

End Class
