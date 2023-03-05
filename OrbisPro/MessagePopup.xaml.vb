Public Class MessagePopup

    Public Sub New(Title As String, Details As String, AppIcon As ImageSource)

        InitializeComponent()

        PopupTitle.Content = Title
        PopupDetail.Content = Details
        PopupImage.Source = AppIcon

    End Sub

End Class
