Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports System.Threading
Imports System.Windows.Media.Animation
Imports Microsoft.Web.WebView2.Core
Imports SharpDX.XInput
Imports System.ComponentModel

Public Class SystemWebBrowser

    Public Opener As String
    Dim WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub SystemWebBrowser_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()
        WebNavigationBarTextBox.Focus()
    End Sub

    Private Async Sub SystemWebBrowser_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        Try
            'Check for gamepads
            If GetAndSetGamepads() Then MainController = SharedController1
            ChangeButtonLayout()

            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Catch ex As Exception
            PauseInput = True
            ExceptionDialog("System Error", ex.Message)
        End Try
    End Sub

    Private Sub SystemWebBrowser_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub SystemWebBrowser_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

#Region "WebView2 Events"

    Private Sub InternalBrowser_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs) Handles InternalBrowser.NavigationCompleted
        'Get title and final url
        WebNavigationBarTextBox.Text = InternalBrowser.Source.ToString()
        WebPageTitleTextBlock.Text = InternalBrowser.CoreWebView2.DocumentTitle

        'Set favicon image
        If Not String.IsNullOrEmpty(InternalBrowser.CoreWebView2.FaviconUri) Then
            FaviconImage.Source = New BitmapImage(New Uri(InternalBrowser.CoreWebView2.FaviconUri, UriKind.RelativeOrAbsolute))
        Else
            FaviconImage.Source = Nothing
        End If
    End Sub

#End Region

#Region "Input"

    Private Sub SystemWebBrowser_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.A Then
            If Not WebNavigationBarTextBox.IsFocused Then WebNavigationBarTextBox.Focus()
        ElseIf e.Key = Key.Enter Then
            If WebNavigationBarTextBox.IsFocused Then InternalBrowser.CoreWebView2.Navigate(WebNavigationBarTextBox.Text)
        ElseIf e.Key = Key.Escape Then
            BeginAnimation(OpacityProperty, ClosingAnimation)
        End If
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim AdditionalDelayAmount As Integer = 0

            If Not PauseInput Then
                Dim MainGamepadState As State = MainController.GetState()
                Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

                Dim MainGamepadButton_A_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.A) <> 0
                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim MainGamepadButton_X_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.X) <> 0
                Dim MainGamepadButton_Y_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Y) <> 0
                Dim MainGamepadButton_Start_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.Start) <> 0

                Dim MainGamepadButton_DPad_Up_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadUp) <> 0
                Dim MainGamepadButton_DPad_Down_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadDown) <> 0
                Dim MainGamepadButton_DPad_Left_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadLeft) <> 0
                Dim MainGamepadButton_DPad_Right_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.DPadRight) <> 0

                Dim MainGamepadButton_RightThumbY_Up As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(32767)
                Dim MainGamepadButton_RightThumbY_Down As Boolean = MainGamepadState.Gamepad.RightThumbY = CShort(-32768)

                'Get the focused element to select different actions
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_A_Button_Pressed Then
                    If WebNavigationBarTextBox.IsFocused Then InternalBrowser.CoreWebView2.Navigate(WebNavigationBarTextBox.Text)
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    If Not WebNavigationBarTextBox.IsFocused Then WebNavigationBarTextBox.Focus()
                ElseIf MainGamepadButton_DPad_Left_Pressed Then
                    MoveLeft()
                ElseIf MainGamepadButton_DPad_Right_Pressed Then
                    MoveRight()
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    MoveUp()
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    MoveDown()
                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                End If

                AdditionalDelayAmount += 50
            Else
                AdditionalDelayAmount += 500
            End If

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate + AdditionalDelayAmount)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        If SharedDeviceModel = DeviceModel.ROGAlly Then
            BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_b.png", UriKind.RelativeOrAbsolute))
            NavigateButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_a.png", UriKind.RelativeOrAbsolute))
            FocusButton.Source = New BitmapImage(New Uri("/Icons/Buttons/rog_y.png", UriKind.RelativeOrAbsolute))
        End If
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        Close()
    End Sub

    Private Sub SetBackground()
        'Set the background
        Select Case ConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(My.Computer.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
            Case "Custom"
                BackgroundMedia.Source = New Uri(ConfigFile.IniReadValue("System", "CustomBackgroundPath"), UriKind.Absolute)
            Case Else
                BackgroundMedia.Source = Nothing
        End Select

        'Play the background media if Source is not empty
        If BackgroundMedia.Source IsNot Nothing Then
            BackgroundMedia.Play()
        End If

        'Go to first second of the background video and pause it if BackgroundAnimation = False
        If ConfigFile.IniReadValue("System", "BackgroundAnimation") = "false" Then
            BackgroundMedia.Position = New TimeSpan(0, 0, 1)
            BackgroundMedia.Pause()
        End If

        'Mute BackgroundMedia if BackgroundMusic = False
        If ConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then
            BackgroundMedia.IsMuted = True
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "SystemWebBrowser",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

#Region "Navigation"

    Private Sub MoveUp()
        If WebNavigationBarTextBox.IsFocused Then
            InternalBrowser.Focus()
        ElseIf WebSearchTextBox.IsFocused Then
            InternalBrowser.Focus()
        End If
    End Sub

    Private Sub MoveDown()
        If WebNavigationBarTextBox.IsFocused Then
            InternalBrowser.Focus()
        ElseIf WebSearchTextBox.IsFocused Then
            InternalBrowser.Focus()
        End If
    End Sub

    Private Sub MoveRight()
        If WebNavigationBarTextBox.IsFocused Then
            WebSearchTextBox.Focus()
        ElseIf WebSearchTextBox.IsFocused Then
            WebNavigationBarTextBox.Focus()
        End If
    End Sub

    Private Sub MoveLeft()
        If WebNavigationBarTextBox.IsFocused Then
            WebSearchTextBox.Focus()
        ElseIf WebSearchTextBox.IsFocused Then
            WebSearchTextBox.Focus()
        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(InternalBrowser)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(InternalBrowser)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

End Class
