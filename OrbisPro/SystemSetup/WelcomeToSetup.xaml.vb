Imports System.ComponentModel
Imports System.Windows.Media.Animation
Imports XInput.Wrapper

Public Class WelcomeToSetup

    Dim ConfigFile As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\Config\Settings.ini")

    Dim WithEvents CurrentController As X.Gamepad
    Dim Part2Done As Boolean = False

    'Get connected controllers
    Private Sub GetAttachedControllers()

        'If a compatible controller is found set 'CurrentController' to 'X.Gamepad_1'
        If X.IsAvailable Then
            CurrentController = X.Gamepad_1
            X.UpdatesPerSecond = 13 'This is important, otherwise the controller input is too fast
            X.StartPolling(CurrentController) 'Start listening to controller input
        End If

    End Sub

    Private Sub WelcomeToSetup_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        GetAttachedControllers()

        If ConfigFile.IniReadValue("Setup", "Done") = "True" Then
            'Boot into OrbisPro
            Dim OrbisProMainWindow As New MainWindow() With {.ShowActivated = True, .Top = Top, .Left = Left}
            OrbisAnimations.Animate(OrbisProMainWindow, OpacityProperty, 0, 1, New Duration(TimeSpan.FromSeconds(1)))
            OrbisProMainWindow.Show()
            Close()
        Else
            'Start the first setup
            SetupPartX()
        End If
    End Sub

    Private Sub SystemSetup_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        'The first pressed key/button (must) be HOME to continue
        If e.Key = Key.Home Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)
            SetupPart1()
        ElseIf e.Key = Key.X Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)
            If Not Part2Done = True Then
                SetupPart2()
            Else
                SetupPart3()
            End If
        ElseIf e.Key = Key.Up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
        ElseIf e.Key = Key.Down Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
        End If

    End Sub

    Private Sub CurrentController_StateChanged(sender As Object, e As EventArgs) Handles CurrentController.StateChanged

        If CurrentController.A_up Then 'Cross for PS3
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.SelectItem)
            If Not Part2Done = True Then
                SetupPart2()
            Else
                SetupPart3()
            End If
        ElseIf CurrentController.Dpad_Down_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If EnglishButton.IsFocused Then
                GermanButton.Focus()
            ElseIf GermanButton.IsFocused Then
                FrenchButton.Focus()
            ElseIf FrenchButton.IsFocused Then
                EnglishButton.Focus()
            End If
        ElseIf CurrentController.Dpad_Up_up Then
            OrbisAudio.PlayBackgroundSound(OrbisAudio.Sounds.Move)
            If EnglishButton.IsFocused Then
                FrenchButton.Focus()
            ElseIf FrenchButton.IsFocused Then
                GermanButton.Focus()
            ElseIf GermanButton.IsFocused Then
                EnglishButton.Focus()
            End If
        End If

    End Sub

    Private Sub SetupPartX()
        'Show the controller image 
        Dim ControllerLeftAnim As New DoubleAnimation With {.From = 1320, .To = 340, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim ControllerTextLeftAnim As New DoubleAnimation With {.From = 1920, .To = 940, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

        WelcomeLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ControllerSetupImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ControllerTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ControllerSetupImage.BeginAnimation(Canvas.LeftProperty, ControllerLeftAnim)
        ControllerTextBlock.BeginAnimation(Canvas.LeftProperty, ControllerTextLeftAnim)
    End Sub

    Private Sub SetupPart1()
        Dim ControllerLeftAnim As New DoubleAnimation With {.From = 340, .To = -600, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim ControllerTextLeftAnim As New DoubleAnimation With {.From = 940, .To = -1040, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim ButtonsLeftAnim As New DoubleAnimation With {.From = 1920, .To = 284, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

        TopSeparator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        BottomSeparator.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ControllerSetupImage.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        ControllerTextBlock.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        ControllerSetupImage.BeginAnimation(Canvas.LeftProperty, ControllerLeftAnim)
        ControllerTextBlock.BeginAnimation(Canvas.LeftProperty, ControllerTextLeftAnim)

        EnglishButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)
        GermanButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)
        FrenchButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)

        CrossButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        BackLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        EnglishButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        GermanButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        FrenchButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        SetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        EnglishButton.Focus()
    End Sub

    Private Sub SetupPart2()
        Dim ButtonsLeftAnim As New DoubleAnimation With {.From = 284, .To = -1300, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim TitleAndBoxLeftAnim As New DoubleAnimation With {.From = 1920, .To = 284, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
        Dim InnerLabelsLeftAnim As New DoubleAnimation With {.From = 1920, .To = 444, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}

        EnglishButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        GermanButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        FrenchButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        SetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        EnglishButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)
        GermanButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)
        GermanButton.BeginAnimation(Canvas.LeftProperty, ButtonsLeftAnim)

        MainSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        SecondSetupTitle.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        DarkBox.Visibility = Visibility.Visible
        SetupEmuLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        CheckUpdatesLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        DateTimeLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        NextButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        CircleButton.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})
        NewBackLabel.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(300))})

        SecondSetupTitle.BeginAnimation(Canvas.LeftProperty, TitleAndBoxLeftAnim)
        DarkBox.BeginAnimation(Canvas.LeftProperty, TitleAndBoxLeftAnim)
        SetupEmuLabel.BeginAnimation(Canvas.LeftProperty, InnerLabelsLeftAnim)
        CheckUpdatesLabel.BeginAnimation(Canvas.LeftProperty, InnerLabelsLeftAnim)
        DateTimeLabel.BeginAnimation(Canvas.LeftProperty, InnerLabelsLeftAnim)

        Part2Done = True
    End Sub

    Private Sub SetupPart3()
        Dim NewEmulatorSetup As New SetupEmulators() With {.ShowActivated = True, .Left = Left, .Top = Top}
        NewEmulatorSetup.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(1000))})
        NewEmulatorSetup.Show()

        'We're done here. Close the 'Welcome Setup'.
        Close()
    End Sub

    Private Sub GermanButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles GermanButton.LostFocus
        GermanButton.BorderBrush = Nothing
    End Sub

    Private Sub GermanButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles GermanButton.GotFocus
        GermanButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub FrenchButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles FrenchButton.GotFocus
        FrenchButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub FrenchButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles FrenchButton.LostFocus
        FrenchButton.BorderBrush = Nothing
    End Sub

    Private Sub EnglishButton_GotFocus(sender As Object, e As RoutedEventArgs) Handles EnglishButton.GotFocus
        EnglishButton.BorderBrush = New SolidColorBrush(CType(ColorConverter.ConvertFromString("#FF00BAFF"), Color))
    End Sub

    Private Sub EnglishButton_LostFocus(sender As Object, e As RoutedEventArgs) Handles EnglishButton.LostFocus
        EnglishButton.BorderBrush = Nothing
    End Sub

    Private Sub WelcomeToSetup_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        'Stop listening to controller input on this window
        X.StopPolling()
        CurrentController = Nothing 'Important, otherwise this window still records controller input
    End Sub

End Class
