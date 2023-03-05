Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports XInput.Wrapper
Imports OrbisPro.OrbisAnimations

Public Class SetupPS3

    Implements INotifyPropertyChanged

    Private WithEvents PS3Emulator As Process

    Public _FirmwareDownloadCompleted As Boolean = False
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

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

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property FirmwareDownloadCompleted() As Boolean
        Get
            Return _FirmwareDownloadCompleted
        End Get
        Set(Value As Boolean)
            _FirmwareDownloadCompleted = Value
            NotifyPropertyChanged("FirmwareDownloadCompleted")
        End Set
    End Property

    Private Sub RequirementsReadBox_Checked(sender As Object, e As RoutedEventArgs) Handles RequirementsReadBox.Checked
        If RequirementsReadBox.IsChecked Then
            RequirementsButton.BorderBrush.Opacity = 100
            DownloadButton.Focus()
        End If
    End Sub

    Private Sub SetupPS3Config_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GetAttachedControllers()
        RequirementsReadBox.Focus()
    End Sub

    Private Sub PS3Emulator_Exited(sender As Object, e As EventArgs) Handles PS3Emulator.Exited
        If Dispatcher.CheckAccess() = False Then
            Dispatcher.BeginInvoke(Sub() InstallButton.BorderBrush.Opacity = 100)
            Dispatcher.BeginInvoke(Sub() BackupsButton.Focus())
        Else
            InstallButton.BorderBrush.Opacity = 100
            BackupsButton.Focus()
        End If
    End Sub

    Private Sub SetupPS3_PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Handles Me.PropertyChanged
        If e.PropertyName = "FirmwareDownloadCompleted" Then
            If FirmwareDownloadCompleted = True Then
                DownloadButton.BorderBrush.Opacity = 100
            End If
        End If
    End Sub

    Private Sub SetupPS3_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing
    End Sub

    Private Sub SetupPS3_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If e.Key = Key.X Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

            If TypeOf FocusedItem Is CheckBox Then

                Dim SelectedCheckBox As CheckBox = CType(FocusedItem, CheckBox)
                If SelectedCheckBox.IsChecked Then
                    SelectedCheckBox.IsChecked = False
                Else
                    SelectedCheckBox.IsChecked = True
                End If

            ElseIf TypeOf FocusedItem Is Button Then

                Dim SelectedButton As Button = CType(FocusedItem, Button)
                Select Case SelectedButton.Name
                    Case "DownloadButton"
                        DownloadPS3Firmware()
                    Case "InstallButton"
                        InstallFirmware()
                    Case "BackupsButton"
                        AddBackups()
                End Select

            End If

        ElseIf e.Key = Key.Right Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If TypeOf FocusedItem Is CheckBox Then
                RequirementsButton.Focus()
            End If

        ElseIf e.Key = Key.Left Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If TypeOf FocusedItem Is Button Then
                RequirementsReadBox.Focus()
            End If

        End If

    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        Dim FocusedItem = FocusManager.GetFocusedElement(Me)

        If CurrentController.A_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

            If TypeOf FocusedItem Is CheckBox Then

                Dim SelectedCheckBox As CheckBox = CType(FocusedItem, CheckBox)
                If SelectedCheckBox.IsChecked Then
                    SelectedCheckBox.IsChecked = False
                Else
                    SelectedCheckBox.IsChecked = True
                End If

            ElseIf TypeOf FocusedItem Is Button Then

                Dim SelectedButton As Button = CType(FocusedItem, Button)
                Select Case SelectedButton.Name
                    Case "DownloadButton"
                        DownloadPS3Firmware()
                    Case "InstallButton"
                        InstallFirmware()
                    Case "BackupsButton"
                        AddBackups()
                End Select

            End If

        ElseIf CurrentController.Dpad_Right_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If TypeOf FocusedItem Is CheckBox Then
                RequirementsButton.Focus()
            End If

        ElseIf CurrentController.Dpad_Left_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If TypeOf FocusedItem Is Button Then
                RequirementsReadBox.Focus()
            End If

        ElseIf CurrentController.Dpad_Down_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            If TypeOf FocusedItem Is Button Then
                Dim SelectedButton As Button = CType(FocusedItem, Button)
                Select Case SelectedButton.Name
                    Case "DownloadButton"
                        InstallButton.Focus()
                    Case "InstallButton"
                        BackupsButton.Focus()
                    Case "BackupsButton"
                        DownloadButton.Focus()
                End Select
            End If

        ElseIf CurrentController.Dpad_Up_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)

            If TypeOf FocusedItem Is Button Then
                Dim SelectedButton As Button = CType(FocusedItem, Button)
                Select Case SelectedButton.Name
                    Case "DownloadButton"
                        BackupsButton.Focus()
                    Case "InstallButton"
                        DownloadButton.Focus()
                    Case "BackupsButton"
                        InstallButton.Focus()
                End Select
            End If

        End If

    End Sub

    Private Sub DownloadPS3Firmware()
        Dim DownloadsWindow As New Downloads() With {.ShowActivated = True, .Top = Top, .Left = Left}
        DownloadsWindow.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromSeconds(1))})
        DownloadsWindow.Show()
    End Sub

    Private Sub InstallFirmware()
        Try
            PS3Emulator = New Process
            PS3Emulator.StartInfo.FileName = My.Computer.FileSystem.CurrentDirectory + "\System\Emulators\rpcs3\rpcs3.exe"
            PS3Emulator.StartInfo.Arguments = "--installfw """ + My.Computer.FileSystem.CurrentDirectory + "\System\Downloads\PS3UPDAT.PUP"""
            PS3Emulator.EnableRaisingEvents = True
            PS3Emulator.Start()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub AddBackups()
        Dim NewExplorerWindow As New FileExplorer() With {.Top = Top, .Left = Left, .ShowActivated = True}
        Animate(NewExplorerWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
        NewExplorerWindow.Show()
        BeginAnimation(OpacityProperty, ClosingAnim)
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        'Save the 'Setup Done' config
        Dim ConfigFile As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\Config\Settings.ini")
        ConfigFile.IniWriteValue("Setup", "Done", "True")

        Close()
    End Sub

End Class
