﻿Imports System.Windows.Media.Animation

Public Class OrbisNotifications

    Private Shared _DisablePopups As Boolean = False
    Private Shared _NotificationDuration As Double = 3.0

    Public Shared Property NotificationDuration As Double
        Get
            Return _NotificationDuration
        End Get
        Set(Value As Double)
            _NotificationDuration = Value
        End Set
    End Property

    Public Shared Property DisablePopups As Boolean
        Get
            Return _DisablePopups
        End Get
        Set(Value As Boolean)
            _DisablePopups = Value
        End Set
    End Property

    Public Shared Sub NotificationPopup(CanvasToPopup As Canvas, NotificationTitle As String, NotificationDetails As String, NotificationImageLocation As String)
        'Create a new NewNotification and add the details to it
        Dim NewNotification As New MessagePopup(NotificationTitle, NotificationDetails, New BitmapImage(New Uri(NotificationImageLocation, UriKind.RelativeOrAbsolute)))

        'Set the location where to pop up
        Canvas.SetLeft(NewNotification, 0)
        Canvas.SetTop(NewNotification, 130)

        'Add to the window where it should pop up
        CanvasToPopup.Children.Add(NewNotification)

        'Animate the NewNotification
        If NewNotification.Dispatcher.CheckAccess() = True Then
            NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True})
        Else
            NewNotification.Dispatcher.BeginInvoke(Sub() NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True}))
        End If
    End Sub

    Public Shared Sub NotificationPopup(CanvasToPopup As Canvas, NotificationTitle As String, NotificationDetails As String, NotificationImage As ImageSource)
        'Create a new NewNotification and add the details to it
        Dim NewNotification As New MessagePopup(NotificationTitle, NotificationDetails, NotificationImage)

        'Set the location where to pop up
        Canvas.SetLeft(NewNotification, 0)
        Canvas.SetTop(NewNotification, 130)

        'Add to the window where it should pop up
        CanvasToPopup.Children.Add(NewNotification)

        'Animate the NewNotification
        If NewNotification.Dispatcher.CheckAccess() = True Then
            NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True})
        Else
            NewNotification.Dispatcher.BeginInvoke(Sub() NewNotification.BeginAnimation(Canvas.LeftProperty, New DoubleAnimation With {.From = -535, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(NotificationDuration)), .AutoReverse = True}))
        End If
    End Sub

End Class
