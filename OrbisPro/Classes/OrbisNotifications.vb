Imports System.Windows.Media.Animation

Public Class OrbisNotifications

    Public Shared NewNotification As MessagePopup 'Custom control'
    Public Shared WithEvents NotificationLeftAnim As New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    Public Shared Sub NotificationPopup(CanvasToPopup As Canvas, NotificationTitle As String, NotificationDetails As String, NotificationImageLoc As String)

        'Create a new 'MessagePopup' and add the details to it
        NewNotification = New MessagePopup(NotificationTitle, NotificationDetails, New BitmapImage(New Uri(NotificationImageLoc, UriKind.RelativeOrAbsolute)))

        'Set the location where to pop up
        Canvas.SetLeft(NewNotification, 0)
        Canvas.SetTop(NewNotification, 130)

        'Add to the window where it should pop up
        CanvasToPopup.Children.Add(NewNotification)

        'Animate the 'MessagePopup'
        NewNotification.BeginAnimation(Canvas.LeftProperty, NotificationLeftAnim)

    End Sub

    'After the notification ends we hide it
    Public Shared Sub NotificationLeftAnim_Completed(sender As Object, e As EventArgs) Handles NotificationLeftAnim.Completed
        Dim NotificationHideAnim As New DoubleAnimation With {.From = 0, .To = -535, .BeginTime = TimeSpan.FromSeconds(3), .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        NewNotification.BeginAnimation(Canvas.LeftProperty, NotificationHideAnim)
    End Sub

End Class
