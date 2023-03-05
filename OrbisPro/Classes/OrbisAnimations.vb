Imports System.Windows.Media.Animation

Public Class OrbisAnimations

    'Animates the given UIElement with a specified duration
    Public Shared Sub Animate(ElementToAnimate As UIElement, AnimProperty As DependencyProperty, AnimFrom As Double, AnimTo As Double, AnimDuration As Duration, Optional AutoRev As Boolean = False)
        If AutoRev = False Then
            Dim Animation As New DoubleAnimation With {.From = AnimFrom, .To = AnimTo, .Duration = AnimDuration}
            ElementToAnimate.BeginAnimation(AnimProperty, Animation)
        Else
            Dim Animation As New DoubleAnimation With {.From = AnimFrom, .To = AnimTo, .Duration = AnimDuration, .AutoReverse = True}
            ElementToAnimate.BeginAnimation(AnimProperty, Animation)
        End If
    End Sub

End Class
