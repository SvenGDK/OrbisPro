Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.ComponentModel
Imports System.Threading
Imports System.Windows.Media.Animation
Imports SharpDX.XInput
Imports System.Net.Http
Imports System.IO
Imports System.Windows.Threading

Public Class Downloads

    Public Opener As String
    Public PS3SetupDownload As Boolean = False
    Public PSVitaSetupDownload As Boolean = False

    Public Shared DownloadsList As New List(Of DownloadListViewItem)()
    Private DownloadPath As String
    Public IsDownloadClientBusy As Boolean = False
    Private DownloadClientCTS As CancellationTokenSource

    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

    Private Sub Downloads_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        If Not MainConfigFile.IniReadValue("Network", "DownloadPath") = "Default" Then
            DownloadPath = MainConfigFile.IniReadValue("Network", "DownloadPath")
        End If
    End Sub

    Private Async Sub Downloads_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered

        'Start downloading a firmware if requested
        If PS3SetupDownload Then
            DownloadCurrentPS3Firmware()
        End If
        If PSVitaSetupDownload Then
            DownloadCurrentPSVitaFirmware()
        End If

        'Select the first item if there's an active download
        If DownloadsList.Count > 0 Then
            'Focus the first item
            Dim FirstListViewItem As ListViewItem = CType(DownloadsListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()

            'Convert FirstListViewItem to DownloadListViewItem to control the item's customized properties
            Dim FirstSelectedItem As DownloadListViewItem = CType(FirstListViewItem.Content, DownloadListViewItem)
            FirstSelectedItem.IsAppSelected = Visibility.Visible 'Show the selection border
        Else
            DownloadsListView.Focus()
        End If

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

    Private Sub Downloads_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

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

                        If PS3SetupDownload Then
                            CType(Win, SetupPS3).FirmwareDownloadedCheckBox.IsChecked = True
                            CType(Win, SetupPS3).DownloadFirmwareButton.BorderBrush = Nothing
                            CType(Win, SetupPS3).InstallFirmwareButton.Focus()
                        End If

                        CType(Win, SetupPS3).AdditionalPauseDelay = 100
                        CType(Win, SetupPS3).PauseInput = False
                        CType(Win, SetupPS3).Activate()

                        Exit For
                    End If
                Next
            Case "SetupPSVita"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.SetupPSVita" Then

                        If PS3SetupDownload Then
                            CType(Win, SetupPSVita).FirmwareDownloadedCheckBox.IsChecked = True
                            CType(Win, SetupPSVita).DownloadFirmwareButton.BorderBrush = Nothing
                            CType(Win, SetupPSVita).InstallFirmwareButton.Focus()
                        End If

                        CType(Win, SetupPSVita).AdditionalPauseDelay = 100
                        CType(Win, SetupPSVita).PauseInput = False
                        CType(Win, SetupPSVita).Activate()

                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

    Public Async Sub DownloadCurrentPS3Firmware()

        Dim ItemIndexOfNewDownload As Integer = 0
        OrbisNotifications.NotificationPopup(DownloadsCanvas, "PS3 Firmware Version", "4.92", "/Icons/rpcs3black.png")

        Dim NewDownload As New DownloadListViewItem() With {
                          .AppName = "PS3 Firmware 4.92",
                          .AppIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute)),
                          .AppIsDownloading = Visibility.Visible,
                          .IsAppSelected = Visibility.Visible,
                          .AllDataLabel = "All Data",
                          .AppDataLabel = "",
                          .DownloadProgress = "Preparing",
                          .SecondDownloadProgress = "",
                          .ProgressValue = 0,
                          .InstalledOrUpdated = ""}

        DownloadsListView.Items.Add(NewDownload)
        DownloadsListView.Items.Refresh()

        If String.IsNullOrEmpty(DownloadPath) Then DownloadPath = FileIO.FileSystem.CurrentDirectory + "\System\Downloads"

        Try
            Using NewHttpClient As New HttpClient()
                Using NewHttpResponseMessage As HttpResponseMessage = Await NewHttpClient.GetAsync("http://dus01.ps3.update.playstation.net/update/ps3/image/us/2025_0305_c179ad173bbc08b55431d30947725a4b/PS3UPDAT.PUP", HttpCompletionOption.ResponseHeadersRead)
                    If NewHttpResponseMessage.IsSuccessStatusCode Then

                        'Set CancellationToken 
                        DownloadClientCTS = New CancellationTokenSource()
                        Dim DownloadCancellationToken As CancellationToken = DownloadClientCTS.Token
                        IsDownloadClientBusy = True

                        'Start downloading
                        Dim TotalBytes As Long = NewHttpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault(0)
                        Using ResponseStream As Stream = Await NewHttpResponseMessage.Content.ReadAsStreamAsync(DownloadCancellationToken)

                            Using NewFileStream As New FileStream(DownloadPath + "\PS3UPDAT.PUP", FileMode.Create, FileAccess.Write, FileShare.None, 8192, True)
                                Dim Buffer(8191) As Byte
                                Dim TotalBytesRead As Long = 0
                                Dim BytesRead As Integer

                                Dim NewStopwatch As New Stopwatch()
                                NewStopwatch.Start()

                                Do
                                    BytesRead = Await ResponseStream.ReadAsync(Buffer, DownloadCancellationToken)
                                    If BytesRead = 0 Then Exit Do

                                    Await NewFileStream.WriteAsync(Buffer.AsMemory(0, BytesRead))

                                    TotalBytesRead += BytesRead

                                    Dim ElapsedSeconds As Double = NewStopwatch.Elapsed.TotalSeconds
                                    Dim DownloadSpeed As Double = TotalBytesRead / ElapsedSeconds
                                    Dim SpeedInKbps As Double = DownloadSpeed / 1024
                                    Dim ETAInSeconds As Double = If(TotalBytes > 0, (TotalBytes - TotalBytesRead) / DownloadSpeed, 0)

                                    'Display progress
                                    If TotalBytes > 0 Then
                                        Dim DLProgress As Double = TotalBytesRead * 100 / TotalBytes

                                        If Dispatcher.CheckAccess() = False Then
                                            Await Dispatcher.BeginInvoke(Sub()
                                                                             For Each DownloadItem In DownloadsListView.Items
                                                                                 Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                                                                 If DLListViewItem.AppName = "PS3 Firmware 4.92" Then
                                                                                     DLListViewItem.DownloadProgress = (TotalBytesRead / (1024 * 1024)).ToString("0.000 MB") + "/" + (TotalBytes / (1024 * 1024)).ToString("0.000 MB")
                                                                                     DLListViewItem.ProgressValue = DLProgress
                                                                                 End If
                                                                             Next
                                                                         End Sub)

                                        Else
                                            For Each DownloadItem In DownloadsListView.Items
                                                Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                                If DLListViewItem.AppName = "PS3 Firmware 4.92" Then
                                                    DLListViewItem.DownloadProgress = (TotalBytesRead / (1024 * 1024)).ToString("0.000 MB") + "/" + (TotalBytes / (1024 * 1024)).ToString("0.000 MB")
                                                    DLListViewItem.ProgressValue = DLProgress
                                                End If
                                            Next
                                        End If

                                    End If
                                Loop

                                NewStopwatch.Stop()

                                'Update values
                                For Each DownloadItem In DownloadsListView.Items
                                    Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                    If DLListViewItem.AppName = "PS3 Firmware 4.92" Then
                                        DLListViewItem.AppIsDownloading = Visibility.Hidden
                                        DLListViewItem.AllDataLabel = "Completed and ready to use."
                                        DLListViewItem.DownloadProgress = ""
                                    End If
                                Next

                                'Let the installer know that the PS3 firmware download finished
                                For Each Win In System.Windows.Application.Current.Windows()
                                    If Win.ToString = "OrbisPro.SetupPS3" Then
                                        CType(Win, SetupPS3).FirmwareDownloadCompleted = True
                                        Exit For
                                    End If
                                Next

                                'Send notification when download did finish
                                OrbisNotifications.NotificationPopup(DownloadsCanvas, "PS3 Firmware 4.92", "DL Completed", "/Icons/rpcs3black.png")
                                PlayBackgroundSound(Sounds.Trophy)
                            End Using
                        End Using
                    End If
                End Using
            End Using
        Catch ex As Exception
        Finally
            If DownloadClientCTS IsNot Nothing Then
                DownloadClientCTS.Dispose()
                DownloadClientCTS = Nothing
            End If
        End Try
    End Sub

    Public Async Sub DownloadCurrentPSVitaFirmware()

        Dim ItemIndexOfNewDownload As Integer = 0
        OrbisNotifications.NotificationPopup(DownloadsCanvas, "PS3 Firmware Version", "4.91", "/Icons/rpcs3black.png")

        Dim NewDownload As New DownloadListViewItem() With {
                          .AppName = "PS Vita Firmware 3.74",
                          .AppIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute)),
                          .AppIsDownloading = Visibility.Visible,
                          .IsAppSelected = Visibility.Visible,
                          .AllDataLabel = "All Data",
                          .AppDataLabel = "",
                          .DownloadProgress = "Preparing",
                          .SecondDownloadProgress = "",
                          .ProgressValue = 0,
                          .InstalledOrUpdated = ""}

        DownloadsListView.Items.Add(NewDownload)
        DownloadsListView.Items.Refresh()

        If String.IsNullOrEmpty(DownloadPath) Then DownloadPath = FileIO.FileSystem.CurrentDirectory + "\System\Downloads"

        Try
            Using NewHttpClient As New HttpClient()
                Using NewHttpResponseMessage As HttpResponseMessage = Await NewHttpClient.GetAsync("http://dus01.psv.update.playstation.net/update/psv/image/2022_0209/rel_f2c7b12fe85496ec88a0391b514d6e3b/PSVUPDAT.PUP", HttpCompletionOption.ResponseHeadersRead)
                    If NewHttpResponseMessage.IsSuccessStatusCode Then

                        'Set CancellationToken 
                        DownloadClientCTS = New CancellationTokenSource()
                        Dim DownloadCancellationToken As CancellationToken = DownloadClientCTS.Token
                        IsDownloadClientBusy = True

                        Dim TotalBytes As Long = NewHttpResponseMessage.Content.Headers.ContentLength.GetValueOrDefault(0)
                        Using ResponseStream As Stream = Await NewHttpResponseMessage.Content.ReadAsStreamAsync(DownloadCancellationToken)

                            Using NewFileStream As New FileStream(DownloadPath + "\PSVUPDAT.PUP", FileMode.Create, FileAccess.Write, FileShare.None, 8192, True)
                                Dim Buffer(8191) As Byte
                                Dim TotalBytesRead As Long = 0
                                Dim BytesRead As Integer

                                Dim NewStopwatch As New Stopwatch()
                                NewStopwatch.Start()

                                Do
                                    BytesRead = Await ResponseStream.ReadAsync(Buffer, DownloadCancellationToken)
                                    If BytesRead = 0 Then Exit Do

                                    Await NewFileStream.WriteAsync(Buffer.AsMemory(0, BytesRead))

                                    TotalBytesRead += BytesRead

                                    Dim ElapsedSeconds As Double = NewStopwatch.Elapsed.TotalSeconds
                                    Dim DownloadSpeed As Double = TotalBytesRead / ElapsedSeconds
                                    Dim SpeedInKbps As Double = DownloadSpeed / 1024
                                    Dim ETAInSeconds As Double = If(TotalBytes > 0, (TotalBytes - TotalBytesRead) / DownloadSpeed, 0)

                                    'Display progress
                                    If TotalBytes > 0 Then
                                        Dim DLProgress As Double = TotalBytesRead * 100 / TotalBytes

                                        If Dispatcher.CheckAccess() = False Then
                                            Await Dispatcher.BeginInvoke(Sub()
                                                                             For Each DownloadItem In DownloadsListView.Items
                                                                                 Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                                                                 If DLListViewItem.AppName = "PS Vita Firmware 3.74" Then
                                                                                     DLListViewItem.DownloadProgress = (TotalBytesRead / (1024 * 1024)).ToString("0.000 MB") + "/" + (TotalBytes / (1024 * 1024)).ToString("0.000 MB")
                                                                                     DLListViewItem.ProgressValue = DLProgress
                                                                                 End If
                                                                             Next
                                                                         End Sub)

                                        Else
                                            For Each DownloadItem In DownloadsListView.Items
                                                Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                                If DLListViewItem.AppName = "PS Vita Firmware 3.74" Then
                                                    DLListViewItem.DownloadProgress = (TotalBytesRead / (1024 * 1024)).ToString("0.000 MB") + "/" + (TotalBytes / (1024 * 1024)).ToString("0.000 MB")
                                                    DLListViewItem.ProgressValue = DLProgress
                                                End If
                                            Next
                                        End If

                                    End If
                                Loop

                                NewStopwatch.Stop()

                                'Update values
                                For Each DownloadItem In DownloadsListView.Items
                                    Dim DLListViewItem As DownloadListViewItem = CType(DownloadItem, DownloadListViewItem)
                                    If DLListViewItem.AppName = "PS Vita Firmware 3.74" Then
                                        DLListViewItem.AppIsDownloading = Visibility.Hidden
                                        DLListViewItem.AllDataLabel = "Completed and ready to use."
                                        DLListViewItem.DownloadProgress = ""
                                    End If
                                Next

                                'Let the installer know that the PS Vita firmware download finished
                                For Each Win In System.Windows.Application.Current.Windows()
                                    If Win.ToString = "OrbisPro.SetupPSVita" Then
                                        CType(Win, SetupPSVita).FirmwareDownloadCompleted = True
                                        Exit For
                                    End If
                                Next

                                'Send notification when download did finish
                                OrbisNotifications.NotificationPopup(DownloadsCanvas, "PS Vita Firmware 3.74", "DL Completed", "/Icons/Update.png")
                                PlayBackgroundSound(Sounds.Trophy)
                            End Using
                        End Using
                    End If
                End Using
            End Using
        Catch ex As Exception
        Finally
            If DownloadClientCTS IsNot Nothing Then
                DownloadClientCTS.Dispose()
                DownloadClientCTS = Nothing
            End If
        End Try
    End Sub

#Region "Input"

    Private Sub Downloads_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Dim FocusedItem = FocusManager.GetFocusedElement(Me)
        Select Case e.Key
            Case Key.A
                If TypeOf FocusedItem Is ListViewItem Then
                    Dim SelectedDownloadItem As DownloadListViewItem = CType(FocusedItem, DownloadListViewItem)
                    Try

                        If IsDownloadClientBusy AndAlso DownloadClientCTS IsNot Nothing Then
                            DownloadClientCTS.Cancel()
                            DownloadClientCTS.Dispose()
                            DownloadClientCTS = Nothing
                        End If

                        OrbisNotifications.NotificationPopup(DownloadsCanvas, SelectedDownloadItem.AppName, "Download aborted.", SelectedDownloadItem.AppIcon)
                        PlayBackgroundSound(Sounds.Trophy)
                    Catch ex As Exception
                        PauseInput = True
                        ExceptionDialog("System Error", ex.Message)
                    End Try
                End If
            Case Key.C
                BeginAnimation(OpacityProperty, ClosingAnimation)
            Case Key.Up
                MoveUp()
            Case Key.Down
                MoveDown()
        End Select
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim AdditionalDelayAmount As Integer

            If Not PauseInput Then
                Dim MainGamepadState As State = MainController.GetState()
                Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

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

                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then

                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedDownloadItem As DownloadListViewItem = CType(FocusedItem, DownloadListViewItem)
                        Try

                            If IsDownloadClientBusy AndAlso DownloadClientCTS IsNot Nothing Then
                                DownloadClientCTS.Cancel()
                                DownloadClientCTS.Dispose()
                                DownloadClientCTS = Nothing
                            End If

                            OrbisNotifications.NotificationPopup(DownloadsCanvas, SelectedDownloadItem.AppName, "Download aborted.", SelectedDownloadItem.AppIcon)
                            PlayBackgroundSound(Sounds.Trophy)
                        Catch ex As Exception
                            PauseInput = True
                            ExceptionDialog("System Error", ex.Message)
                        End Try
                    End If

                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                End If

                AdditionalDelayAmount += 50
            Else
                AdditionalDelayAmount += 500
            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))

            OptionsButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_options.png", UriKind.RelativeOrAbsolute))
            OptionsButton.Width = 48

            ActionButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub DownloadsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DownloadsListView.SelectionChanged
        If DownloadsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            Dim PreviousItem As DownloadListViewItem = CType(e.RemovedItems(0), DownloadListViewItem)
            Dim SelectedItem As DownloadListViewItem = CType(e.AddedItems(0), DownloadListViewItem)

            SelectedItem.IsAppSelected = Visibility.Visible
            PreviousItem.IsAppSelected = Visibility.Hidden
        End If
    End Sub

    Private Sub MoveUp()
        PlayBackgroundSound(Sounds.Move)

        Dim SelectedIndex As Integer = DownloadsListView.SelectedIndex
        Dim NextIndex As Integer = DownloadsListView.SelectedIndex - 1

        If Not NextIndex <= -1 Then
            DownloadsListView.SelectedIndex -= 1
        End If
    End Sub

    Private Sub MoveDown()
        PlayBackgroundSound(Sounds.Move)

        Dim SelectedIndex As Integer = DownloadsListView.SelectedIndex
        Dim NextIndex As Integer = DownloadsListView.SelectedIndex + 1

        If Not NextIndex = DownloadsListView.Items.Count Then
            DownloadsListView.SelectedIndex += 1
        End If
    End Sub

    Private Sub ScrollUp()
        Dim DownloadsListViewScrollViewer As ScrollViewer = FindScrollViewer(DownloadsListView)
        Dim VerticalOffset As Double = DownloadsListViewScrollViewer.VerticalOffset
        DownloadsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 70)
    End Sub

    Private Sub ScrollDown()
        Dim DownloadsListViewScrollViewer As ScrollViewer = FindScrollViewer(DownloadsListView)
        Dim VerticalOffset As Double = DownloadsListViewScrollViewer.VerticalOffset
        DownloadsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 70)
    End Sub

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "Downloads",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
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

                OrbisDisplay.SetScaling(DownloadsWindow, DownloadsCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(DownloadsWindow, DownloadsCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

End Class
