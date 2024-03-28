Imports System.Threading
Imports System.Windows.Media.Animation

Public Class OrbisAnimations

    'Frequently used animations & durations
    Public Shared ReadOnly NotificationBannerAnimation As New DoubleAnimation With {.RepeatBehavior = RepeatBehavior.Forever, .From = 260, .To = 430, .Duration = New Duration(TimeSpan.FromSeconds(7)), .AutoReverse = True}
    Public Shared ReadOnly SelectedBoxAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromSeconds(1)), .AutoReverse = True}
    Public Shared HomeMoveDuration As New Duration(TimeSpan.FromMilliseconds(100))

    'Animates the given UIElement with a specified duration
    Public Shared Sub Animate(ElementToAnimate As UIElement, AnimProperty As DependencyProperty, AnimFrom As Double, AnimTo As Double, AnimDuration As Duration, Optional AutoRev As Boolean = False)
        Dim Animation As New DoubleAnimation With {.From = AnimFrom, .To = AnimTo, .Duration = AnimDuration}

        If AutoRev = False Then
            Animation.AutoReverse = False
        Else
            Animation.AutoReverse = True
        End If

        Dim NewThread As New Thread(New ThreadStart(Sub()
                                                        ElementToAnimate.Dispatcher.BeginInvoke(Sub() ElementToAnimate.BeginAnimation(AnimProperty, Animation))
                                                    End Sub))
        NewThread.Start()

        'If ElementToAnimate.CheckAccess() = True Then
        '    ElementToAnimate.BeginAnimation(AnimProperty, Animation)
        'Else
        '    ElementToAnimate.Dispatcher.BeginInvoke(Sub() ElementToAnimate.BeginAnimation(AnimProperty, Animation))
        'End If
    End Sub

End Class
