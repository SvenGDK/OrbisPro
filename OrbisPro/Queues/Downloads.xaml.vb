Imports System.ComponentModel
Imports System.Net
Imports System.Windows.Media.Animation
Imports XInput.Wrapper

Public Class Downloads

    Public Shared DownloadsList As New List(Of DownloadListViewItem)()

    Public WithEvents DownloadThread1 As New WebClient()
    Public WithEvents DownloadThread2 As New WebClient()
    Public WithEvents DownloadThread3 As New WebClient()

    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Dim WithEvents CurrentController As X.Gamepad

    'Get connected controllers
    Private Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            X.UpdatesPerSecond = 12 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    Public Sub DownloadCurrentPS3Firmware()

        Dim ItemIndexOfNewDownload As Integer

        'Add Firmware to Downloads list
        DownloadsList.Add(New DownloadListViewItem() With {.AppName = "PS3 Firmware 4.90", .AppIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute)),
                           .AppIsDownloading = Visibility.Visible, .IsAppSelected = Visibility.Hidden,
                           .AllDataLabel = "All Data", .AppDataLabel = "",
                           .DownloadProgress = "Preparing", .SecondDownloadProgress = "", .ProgressValue = 0,
                           .InstalledOrUpdated = ""})

        'Get Index of added Item
        ItemIndexOfNewDownload = 0

        DownloadsListView.Items.Refresh()
        OrbisNotifications.NotificationPopup(DownloadsCanvas, "PS3 Firmware Version", "4.90", "/Icons/rpcs3black.png")

        'Check if DownloadThread is busy
        If DownloadThread1.IsBusy = False Then
            'Add 'PS3FW' to the query (so we can find this one back) and download the file
            DownloadThread1.QueryString.Add("PS3FW", ItemIndexOfNewDownload.ToString())
            DownloadThread1.DownloadFileAsync(New Uri("http://deu01.ps3.update.playstation.net/update/ps3/image/shop/2023_0228_f8c0c15ebda82e4347fbabf4a7cf53bc/PS3UPDAT.PUP"),
                                              My.Computer.FileSystem.CurrentDirectory + "\System\Downloads\PS3UPDAT.PUP", Stopwatch.StartNew)
        ElseIf DownloadThread2.IsBusy = False Then
            DownloadThread2.QueryString.Add("PS3FW", ItemIndexOfNewDownload.ToString())
            DownloadThread2.DownloadFileAsync(New Uri("http://deu01.ps3.update.playstation.net/update/ps3/image/shop/2023_0228_f8c0c15ebda82e4347fbabf4a7cf53bc/PS3UPDAT.PUP"),
                                              My.Computer.FileSystem.CurrentDirectory + "\System\Downloads\PS3UPDAT.PUP", Stopwatch.StartNew)
        ElseIf DownloadThread3.IsBusy = False Then
            DownloadThread3.QueryString.Add("PS3FW", ItemIndexOfNewDownload.ToString())
            DownloadThread3.DownloadFileAsync(New Uri("http://deu01.ps3.update.playstation.net/update/ps3/image/shop/2023_0228_f8c0c15ebda82e4347fbabf4a7cf53bc/PS3UPDAT.PUP"),
                                              My.Computer.FileSystem.CurrentDirectory + "\System\Downloads\PS3UPDAT.PUP", Stopwatch.StartNew)
        End If
    End Sub

    Private Sub DownloadThread1_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs) Handles DownloadThread1.DownloadProgressChanged
        'Convert sender to Webclient and update the correct item in the ListView
        Dim ClientSender As WebClient = CType(sender, WebClient)

        'Update
        DownloadsList.Item(CInt(ClientSender.QueryString("PS3FW"))).DownloadProgress = (e.BytesReceived / (1024 * 1024)).ToString("0.000 MB") + "/" + (e.TotalBytesToReceive / (1024 * 1024)).ToString("0.000 MB")
        DownloadsList.Item(CInt(ClientSender.QueryString("PS3FW"))).ProgressValue = e.ProgressPercentage
    End Sub

    Private Sub DownloadThread1_DownloadFileCompleted(sender As Object, e As AsyncCompletedEventArgs) Handles DownloadThread1.DownloadFileCompleted

        'Get the WebClient that downloaded the file
        Dim ClientSender As WebClient = CType(sender, WebClient)

        'Update
        DownloadsList.Item(CInt(ClientSender.QueryString("PS3FW"))).AppIsDownloading = Visibility.Hidden
        DownloadsList.Item(CInt(ClientSender.QueryString("PS3FW"))).AllDataLabel = "Completed and ready to use."
        DownloadsList.Item(CInt(ClientSender.QueryString("PS3FW"))).DownloadProgress = ""

        'Let the installer know that the PS3 firmware download finished
        For Each Win In Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.SetupPS3" Then
                CType(Win, SetupPS3).FirmwareDownloadCompleted = True
                Exit For
            End If
        Next

        'Send notification when download did finish
        OrbisNotifications.NotificationPopup(DownloadsCanvas, "Download completed", "PS3 FW", "/Icons/rpcs3black.png")
    End Sub

    Private Sub DownloadThread2_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs) Handles DownloadThread2.DownloadProgressChanged
        'To be completed
    End Sub

    Private Sub DownloadThread2_DownloadFileCompleted(sender As Object, e As AsyncCompletedEventArgs) Handles DownloadThread2.DownloadFileCompleted
        'To be completed
    End Sub

    Private Sub DownloadThread3_DownloadProgressChanged(sender As Object, e As DownloadProgressChangedEventArgs) Handles DownloadThread3.DownloadProgressChanged
        'To be completed
    End Sub

    Private Sub DownloadThread3_DownloadFileCompleted(sender As Object, e As AsyncCompletedEventArgs) Handles DownloadThread3.DownloadFileCompleted
        'To be completed
    End Sub

    Private Sub Downloads_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        GetAttachedControllers()

        'This one was used in a preview
        '
        'DownloadsList.Add(New DownloadListViewItem() With {.AppName = "OrbisPro: Version 0.1.X", .AppIcon = New BitmapImage(New Uri("C:\Users\SvenGDK\Desktop\iconsPro\orbisProLogo.png", UriKind.Absolute)),
        '           .AppIsDownloading = Visibility.Visible, .IsAppSelected = Visibility.Visible,
        '           .AllDataLabel = "All Data", .AppDataLabel = "Data to Start Application",
        '           .DownloadProgress = "Preparing", .SecondDownloadProgress = "", .ProgressValue = 0,
        '           .InstalledOrUpdated = ""})

        DownloadsListView.Focus()
        DownloadsListView.ItemsSource = DownloadsList

        DownloadCurrentPS3Firmware()
    End Sub

    Private Sub Downloads_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.X Then
            'We're done here. Close the 'Downloader'
            BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1))})

            'Activate previous window
            For Each Win In Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.SetupPS3" Then
                    CType(Win, SetupPS3).Activate()
                    CType(Win, SetupPS3).InstallButton.Focus()
                    Exit For
                End If
            Next

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
            BeginAnimation(OpacityProperty, ClosingAnim)
        ElseIf e.Key = Key.O Then
            'Cancel the download
            DownloadThread1.CancelAsync()

            'Return to PS3 setup
            BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1))})

            'Activate previous window
            For Each Win In Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.SetupPS3" Then
                    CType(Win, SetupPS3).Activate()
                    CType(Win, SetupPS3).InstallButton.Focus()
                    Exit For
                End If
            Next

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
            BeginAnimation(OpacityProperty, ClosingAnim)
        End If

    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        If CurrentController.A_down Then
            'We're done here. Close the 'Downloader'
            BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1))})

            'Activate previous window
            For Each Win In Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.SetupPS3" Then
                    CType(Win, SetupPS3).Activate()
                    CType(Win, SetupPS3).InstallButton.Focus()
                    Exit For
                End If
            Next

            Close()
        ElseIf CurrentController.B_down Then
            'Cancel the download
            DownloadThread1.CancelAsync()

            'Return to PS3 setup
            BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1))})

            'Activate previous window
            For Each Win In Windows.Application.Current.Windows()
                If Win.ToString = "OrbisPro.SetupPS3" Then
                    CType(Win, SetupPS3).Activate()
                    CType(Win, SetupPS3).InstallButton.Focus()
                    Exit For
                End If
            Next

            Close()
        End If

    End Sub

    Private Sub Downloads_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

End Class
