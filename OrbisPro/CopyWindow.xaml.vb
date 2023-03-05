Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Media.Animation

Public Class CopyWindow

    Public WithEvents CopyWorker As New BackgroundWorker() With {.WorkerReportsProgress = True, .WorkerSupportsCancellation = True}
    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Public CopyFrom As String
    Public CopyTo As String

    Private Sub CopyWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If File.Exists(CopyTo) Then
            CopyDescriptionTextBlock.Text = "File already exists at destination." + vbCrLf + "Overwrite ?"
            CopyFromToTextBlock.Text = "From :" + vbCrLf + CopyFrom + vbCrLf + "To :" + vbCrLf + CopyTo
            CrossButtonLabel.Content = "Confirm"
        Else
            CopyDescriptionTextBlock.Text = "Copying, please wait ..."
            CopyFromToTextBlock.Text = "From :" + vbCrLf + CopyFrom + vbCrLf + "To :" + vbCrLf + CopyTo
            CopyStatusTextBlock.Text = "0 of " + CopyProgressBar.Maximum.ToString
            CopyWorker.RunWorkerAsync()
        End If

    End Sub

    Private Sub CopyWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles CopyWorker.DoWork

        If e.Argument Is Nothing Then
            File.Copy(CopyFrom, CopyTo, True)
            CopyWorker.ReportProgress(1)
        End If

    End Sub

    Private Sub CopyWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles CopyWorker.ProgressChanged

        If Not Dispatcher.CheckAccess() Then
            Dispatcher.BeginInvoke(Sub() CopyStatusTextBlock.Text = "File " + e.ProgressPercentage.ToString + " of " + CopyProgressBar.Maximum.ToString)
        Else
            CopyStatusTextBlock.Text = e.ProgressPercentage.ToString + " of " + CopyProgressBar.Maximum.ToString
        End If

        CopyProgressBar.Value += 1
    End Sub

    Private Sub CopyWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles CopyWorker.RunWorkerCompleted
        WindowTitle.Content = "Done Copying"
        CopyDescriptionTextBlock.Text = "Copy completed." + vbCrLf + "Close with Cross (X) or Circle (O)."
        CrossButtonLabel.Content = "Close"
        BackLabel.Content = "Close"
    End Sub

    Private Sub CopyWindow_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.Key = Key.X Then

            If CopyDescriptionTextBlock.Text.Contains("already exists") Then
                CopyWorker.RunWorkerAsync()
                CrossButtonLabel.Content = "Hide"
            End If

            If CrossButtonLabel.Content.ToString = "Close" Then
                BeginAnimation(OpacityProperty, ClosingAnim)
            End If

        ElseIf e.Key = Key.O Then

            If CopyDescriptionTextBlock.Text.Contains("already exists") Then
                BeginAnimation(OpacityProperty, ClosingAnim)
            End If

            If BackLabel.Content.ToString = "Close" Then
                BeginAnimation(OpacityProperty, ClosingAnim)
            ElseIf BackLabel.Content.ToString = "Cancel" Then
                CopyWorker.CancelAsync()
            End If

        End If

    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed

        'Reload the files in the File Explorer
        For Each Win In Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.FileExplorer" Then
                CType(Win, FileExplorer).OpenNewFolder(FileExplorer.LastPath)
                Exit For
            End If
        Next

        Close()
    End Sub

End Class
