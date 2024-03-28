Imports System.Threading
Imports System.Windows.Media.Animation

Public Class OrbisNotifications

    Public Shared NotificationDuration As Double = 1.0
    Public Shared DisablePopups As Boolean = False

    Public Shared Sub NotificationPopup(CanvasToPopup As Canvas, NotificationTitle As String, NotificationDetails As String, NotificationImageLocation As String)
        'Create a new 'MessagePopup' and add the details to it
        Dim NewNotification As New MessagePopup(NotificationTitle, NotificationDetails, New BitmapImage(New Uri(NotificationImageLocation, UriKind.RelativeOrAbsolute)))

        'Set the location where to pop up
        Canvas.SetLeft(NewNotification, 0)
        Canvas.SetTop(NewNotification, 130)

        'Add to the window where it should pop up
        CanvasToPopup.Children.Add(NewNotification)

        'Animate the 'MessagePopup'
        Dim NewThread As New Thread(New ThreadStart(Sub()
                                                        If NewNotification.Dispatcher.CheckAccess() = True Then
                                                            NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True})
                                                        Else
                                                            NewNotification.Dispatcher.BeginInvoke(Sub() NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True}))
                                                        End If
                                                    End Sub))
        NewThread.Start()
    End Sub

    Public Shared Sub NotificationPopup(CanvasToPopup As Canvas, NotificationTitle As String, NotificationDetails As String, NotificationImage As ImageSource)
        'Create a new 'MessagePopup' and add the details to it
        Dim NewNotification As New MessagePopup(NotificationTitle, NotificationDetails, NotificationImage)

        'Set the location where to pop up
        Canvas.SetLeft(NewNotification, 0)
        Canvas.SetTop(NewNotification, 130)

        'Add to the window where it should pop up
        CanvasToPopup.Children.Add(NewNotification)

        'Animate the 'MessagePopup'
        Dim NewThread As New Thread(New ThreadStart(Sub()
                                                        If NewNotification.Dispatcher.CheckAccess() = True Then
                                                            NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True})
                                                        Else
                                                            NewNotification.Dispatcher.BeginInvoke(Sub() NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True}))
                                                        End If

                                                    End Sub))
        NewThread.Start()
    End Sub

End Class
