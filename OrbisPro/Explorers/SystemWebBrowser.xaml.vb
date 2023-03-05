Imports System.Windows.Media.Animation
Imports Microsoft.Web.WebView2.Core

Public Class SystemWebBrowser

    Dim WithEvents ClosingAnim As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}


    Private Sub InternalBrowser_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles InternalBrowser.NavigationCompleted
        'Get title and final url
        WebNavigationBarTextBox.Text = InternalBrowser.Source.ToString
        WebPageTitleTextBlock.Text = InternalBrowser.CoreWebView2.DocumentTitle

        'Set favicon image
        If Not InternalBrowser.CoreWebView2.FaviconUri = "" Then
            FaviconImage.Source = New BitmapImage(New Uri(InternalBrowser.CoreWebView2.FaviconUri, UriKind.RelativeOrAbsolute))
        End If

    End Sub

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnim.Completed
        Close()
    End Sub

End Class
