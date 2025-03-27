Imports System.Windows.Media.Animation

Public Class OrbisAnimations

    'Frequently used animations & durations
    Public Shared ReadOnly NotificationBannerAnimation As New DoubleAnimation With {.RepeatBehavior = RepeatBehavior.Forever, .From = 150, .To = 480, .Duration = New Duration(TimeSpan.FromSeconds(6)), .AutoReverse = True}
    Public Shared ReadOnly SelectedAppBorderAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1)), .AutoReverse = True}

    'Animates the given UIElement with a specified duration
    Public Shared Sub Animate(UIElementToAnimate As UIElement, AnimationDependencyProperty As DependencyProperty, AnimateFrom As Double, AnimateTo As Double, AnimationDuration As Duration, Optional Reverse As Boolean = False)
        Dim NewDoubleAnimation As New DoubleAnimation With {.From = AnimateFrom, .To = AnimateTo, .Duration = AnimationDuration}

        If Reverse = True Then
            NewDoubleAnimation.AutoReverse = True
        End If

        If UIElementToAnimate.CheckAccess() = True Then
            UIElementToAnimate.BeginAnimation(AnimationDependencyProperty, NewDoubleAnimation)
        Else
            UIElementToAnimate.Dispatcher.BeginInvoke(Sub() UIElementToAnimate.BeginAnimation(AnimationDependencyProperty, NewDoubleAnimation))
        End If
    End Sub

End Class
