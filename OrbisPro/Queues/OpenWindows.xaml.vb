Imports System.Windows.Media.Animation

Public Class OpenWindows

    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Public RPCS3ProcessID As Integer
    Public PCSX2ProcessID As Integer
    Public DuckstationProcessID As Integer
    Public ePSXeProcessID As Integer

    Private Sub OpenWindows_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        WindowDescriptionTextBlock.Text = "List of opened windows & applications." + vbCrLf + "Choose a window/application from the list and select a command."

        'Get the opened windows of OrbisPro
        For Each Win In Windows.Application.Current.Windows()

            'Don't add this window
            If Win.ToString = "OrbisPro.OpenWindows" Then Continue For

            'Create the new item and add as ListViewItem
            Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = Win.ToString.Replace("OrbisPro.", ""),
                .IsItemSelected = Visibility.Hidden,
                .ItemSubDescription = "Window",
                .ItemIcon = New BitmapImage(New Uri("/Icons/OrbisProLogo.png", UriKind.RelativeOrAbsolute))}

            Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}

            OpenWindowsListView.Items.Add(NewListViewItem)
        Next

        'Check for opened emulators
        For Each RunningProcess In Process.GetProcesses()
            If RunningProcess.ProcessName = "rpcs3" Then
                Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = "RPCS3",
                    .IsItemSelected = Visibility.Hidden,
                    .ItemSubDescription = RunningProcess.MainWindowTitle,
                    .ItemIcon = New BitmapImage(New Uri("/Icons/rpcs3.png", UriKind.RelativeOrAbsolute))}

                Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
                OpenWindowsListView.Items.Add(NewListViewItem)

                RPCS3ProcessID = RunningProcess.Id
            ElseIf RunningProcess.ProcessName = "pcsx2" Then
                Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = "PCSX2",
                    .IsItemSelected = Visibility.Hidden,
                    .ItemSubDescription = RunningProcess.MainWindowTitle,
                    .ItemIcon = New BitmapImage(New Uri("/Icons/PCSX2.png", UriKind.RelativeOrAbsolute))}

                Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
                OpenWindowsListView.Items.Add(NewListViewItem)

                PCSX2ProcessID = RunningProcess.Id
            ElseIf RunningProcess.ProcessName = "duckstation" Then
                Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = "Duckstation",
                    .IsItemSelected = Visibility.Hidden,
                    .ItemSubDescription = RunningProcess.MainWindowTitle,
                    .ItemIcon = New BitmapImage(New Uri("/Icons/Duckstation.png", UriKind.RelativeOrAbsolute))}

                Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
                OpenWindowsListView.Items.Add(NewListViewItem)

                DuckstationProcessID = RunningProcess.Id
            ElseIf RunningProcess.ProcessName = "ePSXe" Then
                Dim OrbisOpenWindow As New OpenWindowListViewItem() With {.ItemName = "ePSXe",
                    .IsItemSelected = Visibility.Hidden,
                    .ItemSubDescription = RunningProcess.MainWindowTitle,
                    .ItemIcon = New BitmapImage(New Uri("/Icons/ePSXe.png", UriKind.RelativeOrAbsolute))}

                Dim NewListViewItem As New ListViewItem() With {.ContentTemplate = OpenWindowsListView.ItemTemplate, .Content = OrbisOpenWindow}
                OpenWindowsListView.Items.Add(NewListViewItem)

                ePSXeProcessID = RunningProcess.Id
            End If
        Next

        'Focus the first item itself
        Dim LastSelectedListViewItem As ListViewItem = CType(OpenWindowsListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
        LastSelectedListViewItem.Focus()

        'Convert to DeviceListViewItem to set the border visibility on the first item
        Dim LastSelectedItem As OpenWindowListViewItem = CType(OpenWindowsListView.Items(0).Content, OpenWindowListViewItem)
        LastSelectedItem.IsItemSelected = Visibility.Visible

    End Sub

    Private Sub OpenWindows_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If IsActive Then
            If e.Key = Key.X Then

                If TypeOf FocusedItem Is ListViewItem Then

                    Dim CurrentSelectedItem As OpenWindowListViewItem = CType(OpenWindowsListView.SelectedItem.Content, OpenWindowListViewItem)

                    OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.CancelOptions)

                    If CurrentSelectedItem.ItemName = "RPCS3" Then
                        AppActivate(RPCS3ProcessID)
                    ElseIf CurrentSelectedItem.ItemName = "PCSX2" Then
                        AppActivate(PCSX2ProcessID)
                    ElseIf CurrentSelectedItem.ItemName = "Duckstation" Then
                        AppActivate(DuckstationProcessID)
                    ElseIf CurrentSelectedItem.ItemName = "ePSXe" Then
                        AppActivate(ePSXeProcessID)
                    ElseIf CurrentSelectedItem.ItemName = "MainWindow" Then
                        SearchWindowAndActivate("MainWindow")
                    End If

                End If

            ElseIf e.Key = Key.T Then

                Dim CurrentSelectedItem As OpenWindowListViewItem = CType(OpenWindowsListView.SelectedItem.Content, OpenWindowListViewItem)

                OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.CancelOptions)

                Dim ActiveProcess As Process

                If CurrentSelectedItem.ItemName = "RPCS3" Then
                    ActiveProcess = Process.GetProcessById(RPCS3ProcessID)
                    ActiveProcess.Kill()
                    OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)
                ElseIf CurrentSelectedItem.ItemName = "PCSX2" Then
                    ActiveProcess = Process.GetProcessById(PCSX2ProcessID)
                    ActiveProcess.Kill()
                    OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)
                ElseIf CurrentSelectedItem.ItemName = "Duckstation" Then
                    ActiveProcess = Process.GetProcessById(DuckstationProcessID)
                    ActiveProcess.Kill()
                    OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)
                ElseIf CurrentSelectedItem.ItemName = "ePSXe" Then
                    ActiveProcess = Process.GetProcessById(ePSXeProcessID)
                    ActiveProcess.Kill()
                    OpenWindowsListView.Items.Remove(OpenWindowsListView.SelectedItem)
                End If

            ElseIf e.Key = Key.Escape Then
                BeginAnimation(OpacityProperty, ClosingAnim)

            ElseIf e.Key = Key.O Then
                BeginAnimation(OpacityProperty, ClosingAnim)

                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.MainWindow" Then
                        CType(Win, MainWindow).ReturnAnimation()
                        Exit For
                    End If
                Next

            End If

        End If

    End Sub

    Private Sub OpenWindowsListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OpenWindowsListView.SelectionChanged

        If OpenWindowsListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then

            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            Dim PreviousItem As OpenWindowListViewItem = CType(e.RemovedItems(0).Content, OpenWindowListViewItem)
            Dim SelectedItem As OpenWindowListViewItem = CType(e.AddedItems(0).Content, OpenWindowListViewItem)

            SelectedItem.IsItemSelected = Visibility.Visible
            PreviousItem.IsItemSelected = Visibility.Hidden
        End If

    End Sub

    Public Sub SearchWindowAndActivate(WindowTitle As String)

        For Each Win In Windows.Application.Current.Windows()

            If Win.ToString = "OrbisPro." + WindowTitle Then
                CType(Win, Window).Activate()
                Exit For
            End If

        Next

    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Back)
        Close()
    End Sub

End Class
