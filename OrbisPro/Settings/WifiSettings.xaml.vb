﻿Imports ManagedNativeWifi
Imports OrbisPro.OrbisAnimations
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisNetwork
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput
Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Media.Animation

Public Class WifiSettings

    Private WithEvents NewGlobalKeyboardHook As New OrbisKeyboardHook()
    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private LastKeyboardKey As Key
    Private PasswordInputBox As New PSInputBox("WiFi Password :", True)
    Private WiFiNetworkToHandle As WiFiNetworkListViewItem
    Private WiFiNetworkListViewIndex As Integer = 0
    Private IsWiFiConnected As Boolean = False
    Private ConnectedWifi As NetworkIdentifier

    Public Opener As String = ""

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub WifiSettings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()

        'Add a Password input field to the canvas
        Canvas.SetLeft(PasswordInputBox, 1925)
        Canvas.SetTop(PasswordInputBox, 1085)
        WifiSettingsCanvas.Children.Add(PasswordInputBox)

        'Check if WiFi is connected
        ConnectedWifi = GetConnectedWiFiNetworkSSID()
        If ConnectedWifi IsNot Nothing Then IsWiFiConnected = True
        RefreshWiFiNetworks()

        'Set the current state of the WiFi radio
        If IsWiFiRadioOn() Then
            TurnWifiOnOffTextBlock.Text = "Turn WiFi Off"
        Else
            TurnWifiOnOffTextBlock.Text = "Turn WiFi On"
        End If
    End Sub

    Private Async Sub WifiSettings_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
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

    Private Sub WifiSettings_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub WifiSettings_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnim_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        Select Case Opener
            Case "GeneralSettings"
                For Each Win In System.Windows.Application.Current.Windows()
                    If Win.ToString = "OrbisPro.GeneralSettings" Then
                        CType(Win, GeneralSettings).Activate()
                        Exit For
                    End If
                Next
        End Select

        Close()
    End Sub

#Region "Input"

    Private Async Sub WifiSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)
            Select Case e.Key
                Case Key.A
                    RefreshWiFiNetworks()
                Case Key.B
                    If TypeOf FocusedItem Is ListViewItem Then

                        'Set selected WiFiNetworkListViewItem
                        WiFiNetworkListViewIndex = WiFiNetworksListView.SelectedIndex
                        Dim SelectedWiFiNetwork As WiFiNetworkListViewItem = CType(WiFiNetworksListView.SelectedItem, WiFiNetworkListViewItem)

                        If ConnectTextBlock.Text = "Connect" Then
                            If SelectedWiFiNetwork.IsWiFiSaved = False AndAlso SelectedWiFiNetwork.IsWiFiConnected = False Then
                                WiFiNetworkToHandle = SelectedWiFiNetwork

                                'Check if the WiFi network is secured
                                If SelectedWiFiNetwork.IsWiFiSecured Then

                                    'Show a password input field
                                    Animate(PasswordInputBox, Canvas.TopProperty, 1085, 400, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(PasswordInputBox, Canvas.LeftProperty, 1925, 500, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(PasswordInputBox, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))

                                    'Pause the window keyboard input and focus the password input field
                                    PauseInput = True
                                    PasswordInputBox.PasswordInputTextBox.Clear()
                                    PasswordInputBox.PasswordInputTextBox.Focus()
                                    ShowVirtualKeyboard()

                                Else

                                    'Create a new open WiFi network profile XML
                                    Dim WiFiNetworkNameAsHex As String = GetHexString(SelectedWiFiNetwork.WiFiSSID)
                                    Dim NewOpenWiFiProfile As String = CreateOpenWiFiProfile(SelectedWiFiNetwork.WiFiSSID, WiFiNetworkNameAsHex, "ESS", "auto", "open", "none", "false")
                                    Dim XMLContent As String = File.ReadAllText(NewOpenWiFiProfile)

                                    'Add (or overwrite) the WiFi profile and connect to it
                                    If NativeWifi.SetProfile(SelectedWiFiNetwork.WiFiInterface.Id, ProfileType.AllUser, XMLContent, Nothing, True) Then

                                        If NativeWifi.ConnectNetwork(SelectedWiFiNetwork.WiFiInterface.Id, SelectedWiFiNetwork.WiFiSSID, SelectedWiFiNetwork.WiFiBssType) Then

                                            'Change selected WiFiNetworkListViewItem properties
                                            SelectedWiFiNetwork.IsWiFiConnected = True
                                            SelectedWiFiNetwork.IsWiFiSaved = True
                                            ConnectTextBlock.Text = "Disconnect"

                                            OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Connection succeeded.", "/Icons/Wifi/WiFiNotification.png")

                                            'Set the focus back on the previously selected WiFiNetworkListViewItem
                                            Dim LastSelectedListViewItem As ListViewItem = TryCast(WiFiNetworksListView.ItemContainerGenerator.ContainerFromIndex(WiFiNetworkListViewIndex), ListViewItem)
                                            Dim LastSelectedItem As WiFiNetworkListViewItem = TryCast(LastSelectedListViewItem.Content, WiFiNetworkListViewItem)
                                            If LastSelectedItem IsNot Nothing Then
                                                LastSelectedItem.IsWiFiNetworkSelected = Visibility.Visible
                                                LastSelectedListViewItem.Focus()
                                            End If

                                        Else
                                            PauseInput = True
                                            ExceptionDialog("WiFi Network Error", "Could not connect to the selected WiFi network. WiFi network could be unstable.")
                                        End If

                                    Else
                                        PauseInput = True
                                        ExceptionDialog("WiFi Network Error", "Could not connect to the selected WiFi network. WiFi profile could be corrupted.")
                                    End If

                                End If

                            End If
                        Else
                            If SelectedWiFiNetwork.IsWiFiConnected = True Then
                                'Delete associated profile
                                NativeWifi.DeleteProfile(SelectedWiFiNetwork.WiFiInterface.Id, SelectedWiFiNetwork.WiFiSSID)

                                'Disconnect from SelectedWiFiNetwork
                                If Await DisconnectWiFiNetworkAsync(SelectedWiFiNetwork) Then
                                    'Change selected WiFiNetworkListViewItem properties
                                    SelectedWiFiNetwork.IsWiFiConnected = False
                                    ConnectTextBlock.Text = "Connect"

                                    OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Disconnect succeeded.", "/Icons/Wifi/WiFiNotification.png")
                                Else
                                    SelectedWiFiNetwork.IsWiFiConnected = False
                                    ConnectTextBlock.Text = "Connect"

                                    OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Disconnect did not succeed.", "/Icons/Wifi/WiFiNotification.png")
                                End If
                            End If
                        End If

                    End If
                Case Key.C
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                Case Key.X
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedWiFiNetwork As WiFiNetworkListViewItem = CType(WiFiNetworksListView.SelectedItem, WiFiNetworkListViewItem)
                        'Turn WiFi on or off
                        Select Case TurnWifiOnOffTextBlock.Text
                            Case "Turn WiFi Off"
                                If TurnWiFiOff(SelectedWiFiNetwork.WiFiInterface) Then
                                    TurnWifiOnOffTextBlock.Text = "Turn WiFi On"
                                    WiFiNetworksListView.Items.Clear()
                                End If
                            Case "Turn WiFi On"
                                If TurnWiFiOn(SelectedWiFiNetwork.WiFiInterface) Then
                                    TurnWifiOnOffTextBlock.Text = "Turn WiFi Off"
                                    RefreshWiFiNetworks()
                                End If
                        End Select
                    End If
            End Select
        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub WifiSettings_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Sub NewGlobalKeyboardHook_KeyDown(Key As Forms.Keys) Handles NewGlobalKeyboardHook.KeyDown
        If PauseInput Then
            'Only receive the key when Me.KeyDown is paused
            Select Case Key
                Case Forms.Keys.Return, Forms.Keys.Enter
                    'Remove the password input field
                    Animate(PasswordInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PasswordInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PasswordInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    'Connect to the WiFi network using the password
                    If WiFiNetworkToHandle IsNot Nothing Then
                        PauseInput = False
                        If Await ConnectToNewWiFiNetworkAsync(WiFiNetworkToHandle, PasswordInputBox.PasswordInputTextBox.Password) Then

                            'Change selected WiFiNetworkListViewItem properties
                            WiFiNetworkToHandle.IsWiFiConnected = True
                            WiFiNetworkToHandle.IsWiFiSaved = True
                            ConnectTextBlock.Text = "Disconnect"

                            OrbisNotifications.NotificationPopup(WifiSettingsCanvas, WiFiNetworkToHandle.WiFiSSID, "Connection succeeded.", "/Icons/Wifi/WiFiNotification.png")

                            'Set the focus back on the previously selected WiFiNetworkListViewItem
                            Dim LastSelectedListViewItem As ListViewItem = TryCast(WiFiNetworksListView.ItemContainerGenerator.ContainerFromIndex(WiFiNetworkListViewIndex), ListViewItem)
                            Dim LastSelectedItem As WiFiNetworkListViewItem = TryCast(LastSelectedListViewItem.Content, WiFiNetworkListViewItem)
                            If LastSelectedItem IsNot Nothing Then
                                LastSelectedItem.IsWiFiNetworkSelected = Visibility.Visible
                                LastSelectedListViewItem.Focus()
                            End If

                        Else
                            PauseInput = True
                            ExceptionDialog("WiFi Network Error", "Could not connect to the selected WiFi network. Please check your password.")
                        End If
                    End If

                    PauseInput = False
                Case Forms.Keys.Escape
                    'Remove the password input field
                    Animate(PasswordInputBox, Canvas.TopProperty, 400, 1085, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PasswordInputBox, Canvas.LeftProperty, 500, 1925, New Duration(TimeSpan.FromMilliseconds(500)))
                    Animate(PasswordInputBox, OpacityProperty, 1, 0, New Duration(TimeSpan.FromMilliseconds(500)))

                    'Set the focus back on the previously selected WiFiNetworkListViewItem
                    Dim LastSelectedListViewItem As ListViewItem = TryCast(WiFiNetworksListView.ItemContainerGenerator.ContainerFromIndex(WiFiNetworkListViewIndex), ListViewItem)
                    Dim LastSelectedItem As WiFiNetworkListViewItem = TryCast(LastSelectedListViewItem.Content, WiFiNetworkListViewItem)
                    If LastSelectedItem IsNot Nothing Then
                        LastSelectedItem.IsWiFiNetworkSelected = Visibility.Visible
                        LastSelectedListViewItem.Focus()
                    End If
                    PauseInput = False
            End Select
        End If
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

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
                    If TypeOf FocusedItem Is ListViewItem Then
                        Dim SelectedWiFiNetwork As WiFiNetworkListViewItem = CType(WiFiNetworksListView.SelectedItem, WiFiNetworkListViewItem)
                        'Turn WiFi on or off
                        Select Case TurnWifiOnOffTextBlock.Text
                            Case "Turn WiFi Off"
                                If TurnWiFiOff(SelectedWiFiNetwork.WiFiInterface) Then
                                    TurnWifiOnOffTextBlock.Text = "Turn WiFi On"
                                    WiFiNetworksListView.Items.Clear()
                                End If
                            Case "Turn WiFi On"
                                If TurnWiFiOn(SelectedWiFiNetwork.WiFiInterface) Then
                                    TurnWifiOnOffTextBlock.Text = "Turn WiFi Off"
                                    RefreshWiFiNetworks()
                                End If
                        End Select
                    End If
                ElseIf MainGamepadButton_B_Button_Pressed Then
                    BeginAnimation(OpacityProperty, ClosingAnimation)
                ElseIf MainGamepadButton_Y_Button_Pressed Then
                    RefreshWiFiNetworks()
                ElseIf MainGamepadButton_X_Button_Pressed Then
                    If TypeOf FocusedItem Is ListViewItem Then

                        'Set selected WiFiNetworkListViewItem
                        WiFiNetworkListViewIndex = WiFiNetworksListView.SelectedIndex
                        Dim SelectedWiFiNetwork As WiFiNetworkListViewItem = CType(WiFiNetworksListView.SelectedItem, WiFiNetworkListViewItem)

                        If ConnectTextBlock.Text = "Connect" Then
                            If SelectedWiFiNetwork.IsWiFiSaved = False AndAlso SelectedWiFiNetwork.IsWiFiConnected = False Then
                                WiFiNetworkToHandle = SelectedWiFiNetwork

                                'Check if the WiFi network is secured
                                If SelectedWiFiNetwork.IsWiFiSecured Then

                                    'Show a password input field
                                    Animate(PasswordInputBox, Canvas.TopProperty, 1085, 400, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(PasswordInputBox, Canvas.LeftProperty, 1925, 500, New Duration(TimeSpan.FromMilliseconds(500)))
                                    Animate(PasswordInputBox, OpacityProperty, 0, 1, New Duration(TimeSpan.FromMilliseconds(500)))

                                    'Pause the window keyboard input and focus the password input field
                                    PauseInput = True
                                    PasswordInputBox.PasswordInputTextBox.Clear()
                                    PasswordInputBox.PasswordInputTextBox.Focus()
                                    ShowVirtualKeyboard()

                                Else

                                    'Create a new open WiFi network profile XML
                                    Dim WiFiNetworkNameAsHex As String = GetHexString(SelectedWiFiNetwork.WiFiSSID)
                                    Dim NewOpenWiFiProfile As String = CreateOpenWiFiProfile(SelectedWiFiNetwork.WiFiSSID, WiFiNetworkNameAsHex, "ESS", "auto", "open", "none", "false")
                                    Dim XMLContent As String = File.ReadAllText(NewOpenWiFiProfile)

                                    'Add (or overwrite) the WiFi profile and connect to it
                                    If NativeWifi.SetProfile(SelectedWiFiNetwork.WiFiInterface.Id, ProfileType.AllUser, XMLContent, Nothing, True) Then

                                        If NativeWifi.ConnectNetwork(SelectedWiFiNetwork.WiFiInterface.Id, SelectedWiFiNetwork.WiFiSSID, SelectedWiFiNetwork.WiFiBssType) Then

                                            'Change selected WiFiNetworkListViewItem properties
                                            SelectedWiFiNetwork.IsWiFiConnected = True
                                            SelectedWiFiNetwork.IsWiFiSaved = True
                                            ConnectTextBlock.Text = "Disconnect"

                                            OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Connection succeeded.", "/Icons/Wifi/WiFiNotification.png")

                                            'Set the focus back on the previously selected WiFiNetworkListViewItem
                                            Dim LastSelectedListViewItem As ListViewItem = TryCast(WiFiNetworksListView.ItemContainerGenerator.ContainerFromIndex(WiFiNetworkListViewIndex), ListViewItem)
                                            Dim LastSelectedItem As WiFiNetworkListViewItem = TryCast(LastSelectedListViewItem.Content, WiFiNetworkListViewItem)
                                            If LastSelectedItem IsNot Nothing Then
                                                LastSelectedItem.IsWiFiNetworkSelected = Visibility.Visible
                                                LastSelectedListViewItem.Focus()
                                            End If

                                        Else
                                            PauseInput = True
                                            ExceptionDialog("WiFi Network Error", "Could not connect to the selected WiFi network. WiFi network could be unstable.")
                                        End If

                                    Else
                                        PauseInput = True
                                        ExceptionDialog("WiFi Network Error", "Could not connect to the selected WiFi network. WiFi profile could be corrupted.")
                                    End If

                                End If

                            End If
                        Else
                            If SelectedWiFiNetwork.IsWiFiConnected = True Then
                                'Delete associated profile
                                NativeWifi.DeleteProfile(SelectedWiFiNetwork.WiFiInterface.Id, SelectedWiFiNetwork.WiFiSSID)

                                'Disconnect from SelectedWiFiNetwork
                                If Await DisconnectWiFiNetworkAsync(SelectedWiFiNetwork) Then
                                    'Change selected WiFiNetworkListViewItem properties
                                    SelectedWiFiNetwork.IsWiFiConnected = False
                                    ConnectTextBlock.Text = "Connect"

                                    OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Disconnect succeeded.", "/Icons/Wifi/WiFiNotification.png")
                                Else
                                    SelectedWiFiNetwork.IsWiFiConnected = False
                                    ConnectTextBlock.Text = "Connect"

                                    OrbisNotifications.NotificationPopup(WifiSettingsCanvas, SelectedWiFiNetwork.WiFiSSID, "Disconnect did not succeed.", "/Icons/Wifi/WiFiNotification.png")
                                End If
                            End If
                        End If

                    End If
                ElseIf MainGamepadButton_DPad_Up_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
                    Dim SelectedIndex As Integer = WiFiNetworksListView.SelectedIndex
                    Dim NextIndex As Integer = WiFiNetworksListView.SelectedIndex - 1

                    If Not NextIndex <= -1 Then
                        WiFiNetworksListView.SelectedIndex -= 1
                    End If
                ElseIf MainGamepadButton_DPad_Down_Pressed Then
                    PlayBackgroundSound(Sounds.Move)

                    Dim CurrentListView As ListView = GetAncestorOfType(Of ListView)(CType(FocusedItem, FrameworkElement))
                    Dim SelectedIndex As Integer = WiFiNetworksListView.SelectedIndex
                    Dim NextIndex As Integer = WiFiNetworksListView.SelectedIndex + 1

                    If Not NextIndex = WiFiNetworksListView.Items.Count Then
                        WiFiNetworksListView.SelectedIndex += 1
                    End If
                ElseIf MainGamepadButton_Start_Button_Pressed Then

                ElseIf MainGamepadButton_RightThumbY_Up Then
                    ScrollUp()
                ElseIf MainGamepadButton_RightThumbY_Down Then
                    ScrollDown()
                End If
            End If

            MainGamepadPreviousState = MainGamepadState

            ' Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            BackButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
            ScanButton.Source = New BitmapImage(New Uri("/Icons/Keys/A_Key_Dark.png", UriKind.RelativeOrAbsolute))
            TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Keys/X_Key_Dark.png", UriKind.RelativeOrAbsolute))
            ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/S_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Triangle.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Cross.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Square.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Triangle.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Cross.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Square.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Triangle.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Cross.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Square.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Triangle.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Cross.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Square.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_Y.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_A.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_X.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_Y.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_A.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_X.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_Y.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_A.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_X.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        BackButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                        ScanButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_y.png", UriKind.RelativeOrAbsolute))
                        TurnWifiOnOffButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_a.png", UriKind.RelativeOrAbsolute))
                        ConnectButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_x.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

#End Region

#Region "Navigation"

    Private Sub WiFiNetworksListView_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles WiFiNetworksListView.SelectionChanged
        If WiFiNetworksListView.SelectedItem IsNot Nothing And e.RemovedItems.Count <> 0 Then
            PlayBackgroundSound(Sounds.Move)

            Dim PreviousItem As WiFiNetworkListViewItem = CType(e.RemovedItems(0), WiFiNetworkListViewItem)
            Dim SelectedItem As WiFiNetworkListViewItem = CType(e.AddedItems(0), WiFiNetworkListViewItem)

            SelectedItem.IsWiFiNetworkSelected = Visibility.Visible
            PreviousItem.IsWiFiNetworkSelected = Visibility.Hidden

            If SelectedItem.IsWiFiConnected Then
                ConnectTextBlock.Text = "Disconnect"
            Else
                ConnectTextBlock.Text = "Connect"
            End If

        End If
    End Sub

    Private Sub ScrollUp()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(WiFiNetworksListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset - 50)
    End Sub

    Private Sub ScrollDown()
        Dim OpenWindowsListViewScrollViewer As ScrollViewer = FindScrollViewer(WiFiNetworksListView)
        Dim VerticalOffset As Double = OpenWindowsListViewScrollViewer.VerticalOffset
        OpenWindowsListViewScrollViewer.ScrollToVerticalOffset(VerticalOffset + 50)
    End Sub

#End Region

#Region "WiFi Connection Events"

    Private Sub RefreshWiFiNetworks()
        WiFiNetworksListView.Items.Clear()

        Dim AvailableWiFiNetworks As IEnumerable(Of AvailableNetworkPack) = NativeWifi.EnumerateAvailableNetworks()
        If AvailableWiFiNetworks IsNot Nothing Then

            For Each AvailableWiFiNetwork As AvailableNetworkPack In AvailableWiFiNetworks
                Dim NewAvailableWiFiNetwork As New WiFiNetworkListViewItem() With {
                    .WiFiSSID = AvailableWiFiNetwork.Ssid.ToString(),
                    .WiFiSignalStrenght = "Signal Quality " + AvailableWiFiNetwork.SignalQuality.ToString() + "%",
                    .IsWiFiSecured = AvailableWiFiNetwork.IsSecurityEnabled,
                    .IsWiFiNetworkSelected = Visibility.Hidden,
                    .WiFiAuthenticationAlgorithm = AvailableWiFiNetwork.AuthenticationAlgorithm,
                    .WiFiBssType = AvailableWiFiNetwork.BssType,
                    .WiFiCipherAlgorithm = AvailableWiFiNetwork.CipherAlgorithm,
                    .WiFiInterface = AvailableWiFiNetwork.Interface,
                    .WiFiNetworkIdentifier = AvailableWiFiNetwork.Ssid}

                'Show lock if the WiFi network is secured
                If AvailableWiFiNetwork.IsSecurityEnabled Then
                    NewAvailableWiFiNetwork.IsWiFiSecuredLock = Visibility.Visible
                Else
                    NewAvailableWiFiNetwork.IsWiFiSecuredLock = Visibility.Hidden
                End If

                'Set signal strenght icon
                If AvailableWiFiNetwork.SignalQuality >= 80 Then
                    NewAvailableWiFiNetwork.WiFiSignalIcon = New BitmapImage(New Uri("/Icons/Wifi/WiFiHigh.png", UriKind.Relative))
                ElseIf AvailableWiFiNetwork.SignalQuality >= 40 Then
                    NewAvailableWiFiNetwork.WiFiSignalIcon = New BitmapImage(New Uri("/Icons/Wifi/WiFiMid.png", UriKind.Relative))
                ElseIf AvailableWiFiNetwork.SignalQuality >= 20 Then
                    NewAvailableWiFiNetwork.WiFiSignalIcon = New BitmapImage(New Uri("/Icons/Wifi/WiFiLow.png", UriKind.Relative))
                ElseIf AvailableWiFiNetwork.SignalQuality >= 1 Then
                    NewAvailableWiFiNetwork.WiFiSignalIcon = New BitmapImage(New Uri("/Icons/Wifi/WiFiOff.png", UriKind.Relative))
                End If

                'Check if a profile exists
                If Not String.IsNullOrEmpty(AvailableWiFiNetwork.ProfileName) Then
                    NewAvailableWiFiNetwork.IsWiFiSaved = True
                Else
                    'Do not add the already connected WiFi network twice
                    If ConnectedWifi IsNot Nothing Then
                        If AvailableWiFiNetwork.Ssid.ToString() = ConnectedWifi.ToString() Then
                            Continue For
                        End If
                    End If
                End If

                'Check if the found WiFi network is the connected one
                If ConnectedWifi IsNot Nothing Then
                    If AvailableWiFiNetwork.Ssid.ToString() = ConnectedWifi.ToString() Then
                        NewAvailableWiFiNetwork.IsWiFiConnected = True
                    End If
                End If

                WiFiNetworksListView.Items.Add(NewAvailableWiFiNetwork)
            Next

        End If

        WiFiNetworksListView.Items.Refresh()

        'Focus the first found WiFi network
        If WiFiNetworksListView.Items.Count > 0 Then
            Dim FirstListViewItem As ListViewItem = CType(WiFiNetworksListView.ItemContainerGenerator.ContainerFromIndex(0), ListViewItem)
            FirstListViewItem.Focus()
            Dim FirstSelectedItem As WiFiNetworkListViewItem = CType(FirstListViewItem.Content, WiFiNetworkListViewItem)
            FirstSelectedItem.IsWiFiNetworkSelected = Visibility.Visible
        End If
    End Sub

    Private Async Function ConnectToNewWiFiNetworkAsync(WiFiNetwork As WiFiNetworkListViewItem, WiFiPassword As String) As Task(Of Boolean)
        Dim WiFiAuthenticationAlg As String = ""
        Select Case WiFiNetwork.WiFiAuthenticationAlgorithm
            Case AuthenticationAlgorithm.Open
                WiFiAuthenticationAlg = "open"
            Case AuthenticationAlgorithm.Shared
                WiFiAuthenticationAlg = "shared"
            Case AuthenticationAlgorithm.WPA
                WiFiAuthenticationAlg = "WPA"
            Case AuthenticationAlgorithm.WPA3_ENT
                WiFiAuthenticationAlg = "WPA3"
            Case AuthenticationAlgorithm.WPA3_SAE
                WiFiAuthenticationAlg = "WPA3SAE"
            Case AuthenticationAlgorithm.WPA_PSK
                WiFiAuthenticationAlg = "WPAPSK"
            Case AuthenticationAlgorithm.RSNA
                WiFiAuthenticationAlg = "WPA2"
            Case AuthenticationAlgorithm.RSNA_PSK
                WiFiAuthenticationAlg = "WPA2PSK"
        End Select

        Dim WiFiEncryptionAlg As String = ""
        Select Case WiFiNetwork.WiFiCipherAlgorithm
            Case CipherAlgorithm.TKIP
                WiFiEncryptionAlg = "TKIP"
            Case CipherAlgorithm.WEP
                WiFiEncryptionAlg = "WEP"
            Case CipherAlgorithm.CCMP
                WiFiEncryptionAlg = "AES"
        End Select

        'Create a new WiFi network profile XML
        Dim WiFiNetworkNameAsHex As String = GetHexString(WiFiNetwork.WiFiSSID)
        Dim NewWiFiProfile As String = CreateWiFiProfile(WiFiNetwork.WiFiSSID, WiFiNetworkNameAsHex, "ESS", "auto", WiFiAuthenticationAlg, WiFiEncryptionAlg, "false", "passPhrase", "false", WiFiPassword)

        'Add (or overwrite) the WiFi profile and connect to it
        If Not String.IsNullOrEmpty(NewWiFiProfile) Then
            Dim XMLContent As String = File.ReadAllText(NewWiFiProfile)
            If Not String.IsNullOrEmpty(XMLContent) Then
                If NativeWifi.SetProfile(WiFiNetwork.WiFiInterface.Id, ProfileType.AllUser, XMLContent, Nothing, True) Then
                    Return Await NativeWifi.ConnectNetworkAsync(WiFiNetwork.WiFiInterface.Id, WiFiNetwork.WiFiSSID, WiFiNetwork.WiFiBssType, New TimeSpan(0, 0, 10))
                Else
                    Return False
                End If
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Private Async Function DisconnectWiFiNetworkAsync(WiFiNetwork As WiFiNetworkListViewItem) As Task(Of Boolean)
        If WiFiNetwork IsNot Nothing Then
            Return Await NativeWifi.DisconnectNetworkAsync(WiFiNetwork.WiFiInterface.Id, New TimeSpan(0, 0, 10))
        Else
            Return False
        End If
    End Function

#End Region

#Region "Background"

    Private Sub SetBackground()
        'Set the background
        Select Case MainConfigFile.IniReadValue("System", "Background")
            Case "Blue Bubbles"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\bluecircles.mp4", UriKind.Absolute)
            Case "Orange/Red Gradient Waves"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\gradient_bg.mp4", UriKind.Absolute)
            Case "PS2 Dots"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\ps2_bg.mp4", UriKind.Absolute)
            Case "Blue Bokeh Dust"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\Bluebokehdust.mp4", UriKind.Absolute)
            Case "Golden Dust"
                BackgroundMedia.Source = New Uri(FileIO.FileSystem.CurrentDirectory + "\System\Backgrounds\Goldendust.mp4", UriKind.Absolute)
            Case "Custom"
                BackgroundMedia.Source = New Uri(MainConfigFile.IniReadValue("System", "CustomBackgroundPath"), UriKind.Absolute)
            Case Else
                BackgroundMedia.Source = Nothing
        End Select

        'Play the background media if Source is not empty
        If BackgroundMedia.Source IsNot Nothing Then
            BackgroundMedia.Play()
        End If

        'Go to first second of the background video and pause it if BackgroundAnimation = False
        If MainConfigFile.IniReadValue("System", "BackgroundAnimation") = "false" Then
            BackgroundMedia.Position = New TimeSpan(0, 0, 1)
            BackgroundMedia.Pause()
        End If

        'Mute BackgroundMedia if BackgroundMusic = False
        If MainConfigFile.IniReadValue("System", "BackgroundMusic") = "false" Then
            BackgroundMedia.IsMuted = True
        End If

        'Set width & height
        If Not MainConfigFile.IniReadValue("System", "DisplayScaling") = "AutoScaling" Then
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(WifiSettingsWindow, WifiSettingsCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(WifiSettingsWindow, WifiSettingsCanvas)
            End If
        End If
    End Sub

    Private Sub BackgroundMedia_MediaEnded(sender As Object, e As RoutedEventArgs) Handles BackgroundMedia.MediaEnded
        'Loop the background media
        BackgroundMedia.Position = TimeSpan.FromSeconds(0)
        BackgroundMedia.Play()
    End Sub

#End Region

    Private Sub ExceptionDialog(MessageTitle As String, MessageDescription As String)
        Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True,
            .Top = 0,
            .Left = 0,
            .Opacity = 0,
            .SetupStep = True,
            .Opener = "WifiSettings",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

End Class
