Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports XInput.Wrapper

Public Class SystemDialog

    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Dim WithEvents CurrentController As X.Gamepad
    Public Opener As String

    'Get connected controllers
    Private Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            X.UpdatesPerSecond = 13 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    Private Sub SystemDialog_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.O Then
            BeginAnimation(OpacityProperty, ClosingAnim)
        End If

    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        If CurrentController.B_down Then
            BeginAnimation(OpacityProperty, ClosingAnim)
        End If

    End Sub

    Private Sub SystemDialog_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        X.StopPolling()
        CurrentController = Nothing
    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed

        Select Case Opener
            Case "FileExplorer"
                For Each Win In Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.FileExplorer" Then
                        'Re-activate the 'File Explorer'
                        CType(Win, FileExplorer).Activate()
                        CType(Win, FileExplorer).GetAttachedControllers()
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

    Private Sub SystemDialog_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GetAttachedControllers()
    End Sub

End Class
