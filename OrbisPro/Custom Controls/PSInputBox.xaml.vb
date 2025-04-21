Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisUtils

Public Class PSInputBox

    Public Event Closing As EventHandler

    Public Sub New(Title As String, Optional UsePasswordBox As Boolean = False)
        InitializeComponent()

        TitleTextBlock.Text = Title

        If UsePasswordBox Then
            InputTextBox.Visibility = Visibility.Hidden
            PasswordInputTextBox.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs) Handles CloseButton.Click
        Animate(Me, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))
        HideVirtualKeyboard()
        CloseControl()
    End Sub

    Public Sub CloseControl()
        RaiseEvent Closing(Me, EventArgs.Empty)
        Dim ParentPanel As Panel = TryCast(Parent, Panel)
        ParentPanel?.Children.Remove(Me)
    End Sub

End Class
