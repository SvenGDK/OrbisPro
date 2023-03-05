Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports XInput.Wrapper

Public Class SetupEmulators

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

    Private Sub SetupEmulators_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GetAttachedControllers()
        SetupPS3Button.Focus()
    End Sub

    Public Sub NewPS3SetupWin()
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)

        Dim NewPS3Setup As New SetupPS3() With {.ShowActivated = True, .Top = Top, .Left = Left}
        NewPS3Setup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(1000))})
        NewPS3Setup.Show()

        'We're done here for now. Close the 'Emulator Setup'.
        OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)
        BeginAnimation(OpacityProperty, ClosingAnim)
    End Sub

    Private Sub SetupPS1Button_GotFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS1Button.GotFocus
        SetupPS1Button.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub SetupPS1Button_LostFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS1Button.LostFocus
        SetupPS1Button.BorderBrush = Nothing
    End Sub

    Private Sub SetupPS2Button_GotFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS2Button.GotFocus
        SetupPS2Button.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub SetupPS2Button_LostFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS2Button.LostFocus
        SetupPS2Button.BorderBrush = Nothing
    End Sub

    Private Sub SetupPS3Button_GotFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS3Button.GotFocus
        SetupPS3Button.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub SetupPS3Button_LostFocus(sender As Object, e As RoutedEventArgs) Handles SetupPS3Button.LostFocus
        SetupPS3Button.BorderBrush = Nothing
    End Sub

    Private Sub SetupEmulators_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing 'Important, otherwise this window still records controller input
    End Sub

    Private Sub SetupEmulators_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.X Then
            'Start the PS3 emulator setup
            NewPS3SetupWin()
        End If
    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged
        If CurrentController.A_up Then 'Cross for PS3
            'Start the PS3 emulator setup
            NewPS3SetupWin()
        ElseIf CurrentController.Dpad_Down_up Then 'D-Pad Down
            If SetupPS3Button.IsFocused Then
                SetupPS2Button.Focus()
            ElseIf SetupPS2Button.IsFocused Then
                SetupPS1Button.Focus()
            ElseIf SetupPS1Button.IsFocused Then
                SetupPS3Button.Focus()
            End If
        ElseIf CurrentController.Dpad_Up_up Then 'D-Pad Up
            If SetupPS3Button.IsFocused Then
                SetupPS1Button.Focus()
            ElseIf SetupPS1Button.IsFocused Then
                SetupPS2Button.Focus()
            ElseIf SetupPS2Button.IsFocused Then
                SetupPS3Button.Focus()
            End If
        End If
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

End Class
