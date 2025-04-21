Imports System.ComponentModel
Imports System.IO
Imports System.Net.Http
Imports System.Threading
Imports System.Windows.Media.Animation
Imports Newtonsoft.Json
Imports OrbisPro.OrbisAudio
Imports OrbisPro.OrbisInput
Imports OrbisPro.OrbisUtils
Imports PS4_Tools
Imports SharpDX.XInput

Public Class PKGInstaller

    Private WithEvents ClosingAnimation As New DoubleAnimation With {.From = 1, .To = 0, .Duration = New Duration(TimeSpan.FromMilliseconds(500))}
    Private LastKeyboardKey As Key

    Public PKGToExtract As String = ""
    Public InstallationConfirmed As Boolean = False

    Private PKGID As String = ""
    Private PKGContentID As String = ""
    Private PKGTitle As String = ""
    Private PKGIcon As ImageSource = Nothing

    Private OrbisPubCMD As New Process()
    Private WithEvents InstallerWorker As New BackgroundWorker() With {.WorkerSupportsCancellation = True}
    Public InstallationAborted As Boolean = False

    'Controller input
    Private MainController As Controller
    Private MainGamepadPreviousState As State
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

#Region "Window Events"

    Private Sub PKGInstaller_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'Set background
        SetBackground()
    End Sub

    Private Async Sub PKGInstaller_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
        'Check for gamepads
        If GetAndSetGamepads() Then MainController = SharedController1
        ChangeButtonLayout()

        'Load PKG infos
        If Not String.IsNullOrEmpty(PKGToExtract) Then
            Try
                Dim PS4PKGInfo As PKG.SceneRelated.Unprotected_PKG = PKG.SceneRelated.Read_PKG(PKGToExtract)

                If PS4PKGInfo IsNot Nothing Then
                    'Get the Title ID
                    Dim ParamTables = PS4PKGInfo.Param.Tables
                    Dim TitleID As String = ""
                    For Each ParamTable In ParamTables
                        If ParamTable.Name = "TITLE_ID" Then
                            TitleID = ParamTable.Value
                            Exit For
                        End If
                    Next

                    If Not String.IsNullOrEmpty(TitleID) Then
                        PKGID = TitleID
                    Else
                        PKGID = PS4PKGInfo.Content_ID
                    End If

                    PKGContentID = PS4PKGInfo.Content_ID
                    PKGTitle = PS4PKGInfo.PS4_Title

                    If PS4PKGInfo.Icon IsNot Nothing Then
                        PKGIcon = BitmapSourceFromByteArray(PS4PKGInfo.Icon)
                    End If

                    PS4PKGInfo = Nothing

                    If Dispatcher.CheckAccess() = False Then
                        Await Dispatcher.BeginInvoke(Sub()
                                                         InstallerHeaderTextBlock.Text = PKGTitle + vbCrLf + PKGContentID
                                                         InstallerStatusTextBlock.Text = "Checking game compatibility..."
                                                     End Sub)

                        If PKGIcon IsNot Nothing Then
                            Await Dispatcher.BeginInvoke(Sub() PKGImage.Source = PKGIcon)
                        End If
                    Else
                        InstallerHeaderTextBlock.Text = PKGTitle + vbCrLf + PKGContentID
                        InstallerStatusTextBlock.Text = "Checking game compatibility..."

                        If PKGIcon IsNot Nothing Then
                            PKGImage.Source = PKGIcon
                        End If
                    End If

                    'Get the compatibility status for the game before installing
                    Dim PS4CompatibilityStatus As String = Await FetchShadPS4Compatibility(PKGID.Trim())

                    If String.IsNullOrEmpty(PS4CompatibilityStatus) Then
                        PS4CompatibilityStatus = "Not Tested"
                    End If

                    'Show a message about the game's compatibility
                    Dim NewSystemDialog As New SystemDialog() With {.ShowActivated = True, .Top = 0, .Left = 0, .Opacity = 0, .Opener = "PKGInstaller",
                        .MessageTitle = PKGTitle,
                        .MessageDescription = "Current Game Status: " + PS4CompatibilityStatus + vbCrLf + "Please confirm to install this PKG."}

                    NewSystemDialog.ConfirmButton.Visibility = Visibility.Visible
                    NewSystemDialog.ConfirmTextBlock.Visibility = Visibility.Visible
                    NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
                    NewSystemDialog.Show()

                    PauseInput = True
                End If
            Catch ex As Exception
                PauseInput = True
                ExceptionDialog("PKG Installer Error", ex.Message)
            End Try
        End If
    End Sub

    Private Sub PKGInstaller_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        PauseInput = False
    End Sub

    Private Sub PKGInstaller_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        CTS?.Cancel()
        MainController = Nothing
        RemoteController = Nothing
    End Sub

#End Region

    Private Sub ClosingAnimation_Completed(sender As Object, e As EventArgs) Handles ClosingAnimation.Completed
        PlayBackgroundSound(Sounds.Back)

        'Re-activate the 'File Explorer'
        For Each Win In System.Windows.Application.Current.Windows()
            If Win.ToString = "OrbisPro.FileExplorer" Then
                CType(Win, FileExplorer).Activate()
                Exit For
            End If
        Next

        Close()
    End Sub

#Region "Input"

    Private Sub SetupGames_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If Not e.Key = LastKeyboardKey AndAlso PauseInput = False Then
            Dim FocusedItem = FocusManager.GetFocusedElement(Me)

            Select Case e.Key
                Case Key.C
                    If CancelTextBlock.Text = "Cancel" Then
                        'Attempt to close gracefully
                        If Not String.IsNullOrEmpty(OrbisPubCMD.StartInfo.Arguments) Then
                            If Not OrbisPubCMD.HasExited Then
                                OrbisPubCMD.CloseMainWindow()
                                OrbisPubCMD.WaitForExit(1000)
                            End If
                            'Kill if still running
                            If Not OrbisPubCMD.HasExited Then
                                OrbisPubCMD.Kill()
                                OrbisPubCMD.Dispose()
                            End If
                        End If

                        InstallationAborted = True
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    Else
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If
            End Select

        Else
            e.Handled = True
        End If

        LastKeyboardKey = e.Key
    End Sub

    Private Sub SetupGames_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        LastKeyboardKey = Nothing
    End Sub

    Private Async Function ReadGamepadInputAsync(CancelToken As CancellationToken) As Task
        While Not CancelToken.IsCancellationRequested

            Dim MainGamepadState As State = MainController.GetState()
            Dim MainGamepadButtonFlags As GamepadButtonFlags = MainGamepadState.Gamepad.Buttons

            If Not PauseInput AndAlso MainGamepadPreviousState.PacketNumber <> MainGamepadState.PacketNumber Then

                Dim MainGamepadButton_B_Button_Pressed As Boolean = (MainGamepadButtonFlags And GamepadButtonFlags.B) <> 0
                Dim FocusedItem = FocusManager.GetFocusedElement(Me)

                If MainGamepadButton_B_Button_Pressed Then
                    If CancelTextBlock.Text = "Cancel" Then
                        'Attempt to close gracefully
                        If Not String.IsNullOrEmpty(OrbisPubCMD.StartInfo.Arguments) Then
                            If Not OrbisPubCMD.HasExited Then
                                OrbisPubCMD.CloseMainWindow()
                                OrbisPubCMD.WaitForExit(1000)
                            End If
                            'Kill if still running
                            If Not OrbisPubCMD.HasExited Then
                                OrbisPubCMD.Kill()
                                OrbisPubCMD.Dispose()
                            End If
                        End If

                        InstallationAborted = True
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    Else
                        BeginAnimation(OpacityProperty, ClosingAnimation)
                    End If
                End If

            End If

            MainGamepadPreviousState = MainGamepadState

            'Delay to avoid excessive polling
            Await Task.Delay(SharedController1PollingRate, CancellationToken.None)
        End While
    End Function

    Private Sub ChangeButtonLayout()
        Dim GamepadButtonLayout As String = MainConfigFile.IniReadValue("Gamepads", "ButtonLayout")

        If SharedDeviceModel = DeviceModel.PC AndAlso MainController Is Nothing Then
            'Show keyboard keys instead of gamepad buttons
            CancelButton.Source = New BitmapImage(New Uri("/Icons/Keys/C_Key_Dark.png", UriKind.RelativeOrAbsolute))
        Else
            If Not String.IsNullOrEmpty(GamepadButtonLayout) Then
                Select Case GamepadButtonLayout
                    Case "PS3"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS3/PS3_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS4"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS4/PS4_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS5"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PS5/PS5_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "PS Vita"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/PSV/Vita_Circle.png", UriKind.RelativeOrAbsolute))
                    Case "Steam"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Steam/Steam_B.png", UriKind.RelativeOrAbsolute))
                    Case "Steam Deck"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/SteamDeck/SteamDeck_B.png", UriKind.RelativeOrAbsolute))
                    Case "Xbox 360"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/Xbox360/360_B.png", UriKind.RelativeOrAbsolute))
                    Case "ROG Ally"
                        CancelButton.Source = New BitmapImage(New Uri("/Icons/Buttons/ROGAlly/rog_b.png", UriKind.RelativeOrAbsolute))
                End Select
            End If
        End If
    End Sub

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

                OrbisDisplay.SetScaling(PKGInstallerWindow, PKGInstallerCanvas, False, NewWidth, NewHeight)
            End If
        Else
            Dim SplittedValues As String() = MainConfigFile.IniReadValue("System", "DisplayResolution").Split("x")
            If SplittedValues.Length <> 0 Then
                Dim NewWidth As Double = CDbl(SplittedValues(0))
                Dim NewHeight As Double = CDbl(SplittedValues(1))

                OrbisDisplay.SetScaling(PKGInstallerWindow, PKGInstallerCanvas)
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
            .Opener = "SetupGames",
            .MessageTitle = MessageTitle,
            .MessageDescription = MessageDescription}

        NewSystemDialog.BeginAnimation(OpacityProperty, New DoubleAnimation With {.From = 0, .To = 1, .Duration = New Duration(TimeSpan.FromMilliseconds(500))})
        NewSystemDialog.Show()
    End Sub

    Public Async Sub ContinuePKGInstallation()
        PauseInput = False

        If InstallationConfirmed Then
            'Continue and install the PKG
            If Dispatcher.CheckAccess() = False Then
                Await Dispatcher.BeginInvoke(Sub()
                                                 InstallerHeaderTextBlock.Text = "Installing " + PKGTitle + vbCrLf + PKGContentID
                                                 InstallerStatusTextBlock.Text = "Please wait while " + PKGTitle + " is installing..."
                                             End Sub)

                If PKGIcon IsNot Nothing Then
                    Await Dispatcher.BeginInvoke(Sub() PKGImage.Source = PKGIcon)
                End If
            Else
                InstallerHeaderTextBlock.Text = "Installing " + PKGTitle + vbCrLf + PKGContentID
                InstallerStatusTextBlock.Text = "Please wait while " + PKGTitle + " is installing..."

                If PKGIcon IsNot Nothing Then
                    PKGImage.Source = PKGIcon
                End If
            End If

            InstallerWorker.RunWorkerAsync()
            If SharedController1 IsNot Nothing Then Await ReadGamepadInputAsync(CTS.Token)
        Else
            If InstallationAborted Then
                'Show cancellation message
                If Dispatcher.CheckAccess() = False Then
                    Await Dispatcher.BeginInvoke(Sub()
                                                     InstallerHeaderTextBlock.Text = PKGTitle + vbCrLf + PKGContentID
                                                     InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                                                     InstallProgressBar.IsIndeterminate = False
                                                     InstallProgressBar.Value = 0
                                                 End Sub)

                    If PKGIcon IsNot Nothing Then
                        Await Dispatcher.BeginInvoke(Sub() PKGImage.Source = PKGIcon)
                    End If
                Else
                    InstallerHeaderTextBlock.Text = PKGTitle + vbCrLf + PKGContentID
                    InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                    InstallProgressBar.IsIndeterminate = False
                    InstallProgressBar.Value = 0

                    If PKGIcon IsNot Nothing Then
                        PKGImage.Source = PKGIcon
                    End If
                End If
            End If
        End If
    End Sub

    Private Async Function FetchShadPS4Compatibility(GameTitleID As String) As Task(Of String)
        Dim ShadPS4CompatibilityURL As String = "https://shadps4.net/scripts/search.php?page=1"
        Dim PageURL As String = "https://shadps4.net/scripts/search.php?page="
        Dim GameStatus As String = ""

        Using NewHttpClient As New HttpClient()
            Try
                Dim HttpResponse As String = Await NewHttpClient.GetStringAsync(ShadPS4CompatibilityURL)
                Dim CompatibilityList As OrbisPS4.ShadPS4Compatibility = JsonConvert.DeserializeObject(Of OrbisPS4.ShadPS4Compatibility)(HttpResponse)

                If CompatibilityList IsNot Nothing Then

                    'Go through each page until it finds the game compatibility info
                    Dim PageCount As Integer = CompatibilityList.info.pages

                    For Page = 1 To PageCount
                        Dim NewHttpResponse As String = Await NewHttpClient.GetStringAsync(PageURL + Page.ToString() + "&oldest=false")
                        Dim NewCompatibilityList As OrbisPS4.ShadPS4Compatibility = JsonConvert.DeserializeObject(Of OrbisPS4.ShadPS4Compatibility)(NewHttpResponse)
                        Dim CompatibilityGamesList As List(Of OrbisPS4.Game) = NewCompatibilityList.games
                        Dim GameFound As Boolean = False

                        For Each Game In CompatibilityGamesList
                            If Game.code = GameTitleID Then
                                GameFound = True
                                GameStatus = Game.status
                                Exit For
                            End If
                        Next

                        'Exit the pages loop
                        If GameFound Then
                            Exit For
                        End If

                    Next

                End If

            Catch ex As Exception
                PauseInput = True
                ExceptionDialog("Network Error", ex.Message)
            End Try
        End Using

        Return GameStatus
    End Function

    Private Sub InstallerWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles InstallerWorker.DoWork
        Dim DefaultInstallPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games"
        Dim Args As String = "img_extract --passcode 00000000000000000000000000000000 """ + PKGToExtract + """ """ + DefaultInstallPath + """"

        OrbisPubCMD = New Process()
        OrbisPubCMD.StartInfo.FileName = FileIO.FileSystem.CurrentDirectory + "\System\Tools\PS4\orbis-pub-cmd.exe"
        OrbisPubCMD.StartInfo.Arguments = Args
        OrbisPubCMD.StartInfo.UseShellExecute = False
        OrbisPubCMD.StartInfo.CreateNoWindow = True
        OrbisPubCMD.Start()
        OrbisPubCMD.WaitForExit()
    End Sub

    Private Sub InstallerWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles InstallerWorker.RunWorkerCompleted
        OrbisPubCMD.Dispose()

        'Update progress
        If Dispatcher.CheckAccess() = False Then
            Dispatcher.BeginInvoke(Sub()
                                       If InstallationAborted Then
                                           InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                                           InstallProgressBar.IsIndeterminate = False
                                           InstallProgressBar.Value = 0
                                       Else
                                           InstallerStatusTextBlock.Text = "Finishing Installation of " + PKGTitle + ", please wait..."
                                           InstallerHeaderTextBlock.Text = "Please wait until " + PKGTitle + " is completely installed."
                                       End If
                                   End Sub)
        Else
            If InstallationAborted Then
                InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                InstallProgressBar.IsIndeterminate = False
                InstallProgressBar.Value = 0
            Else
                InstallerStatusTextBlock.Text = "Finishing Installation of " + PKGTitle + ", please wait..."
                InstallerHeaderTextBlock.Text = "Please wait until " + PKGTitle + " is completely installed."
            End If
        End If

        'Final steps
        If Not InstallationAborted Then

            'Grab the necessary files from the Sc0 folder and move them to sce_sys
            If Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Sc0") AndAlso Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0") Then

                'If the sce_sys folder is somehow missing then create it
                If Not Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0\sce_sys") Then
                    Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0\sce_sys")
                End If

                'Copy the Sc0 files into the Image0\sce_sys folder
                For Each FileInSc0 As String In Directory.GetFiles(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Sc0")
                    Dim RetrievedFileName As String = Path.GetFileName(FileInSc0)
                    Dim DestinationPath As String = Path.Combine(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0\sce_sys", RetrievedFileName)

                    File.Copy(FileInSc0, DestinationPath, True)
                Next

            End If

            'Rename the Image0 folder to the PKGID
            If Not String.IsNullOrEmpty(PKGID) Then
                If Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0") Then
                    FileIO.FileSystem.RenameDirectory(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Image0", PKGID)
                End If
            End If

            'Delete the SC0 folder
            If Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Sc0") Then
                Directory.Delete(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\Sc0", True)
            End If

            'Check the extracted folder and add the game to the library
            If File.Exists(FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\sce_sys\param.sfo") Then
                Dim PS4Executable As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\eboot.bin"
                Dim GameIconPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\sce_sys\icon0.png"
                Dim GameAlternativeIconPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\sce_sys\icon0.png"
                Dim GameBackgroundPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\sce_sys\pic0.png"
                Dim GameAlternativeBackgroundPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Emulators\shadps4\games\" + PKGID + "\sce_sys\pic1.png"

                'Add the game to GamesList.json
                Dim NewOrbisGame As New OrbisGamesList.Game() With {.Platform = "PS4", .Name = PKGTitle, .ExecutablePath = PS4Executable, .ShowInLibrary = "True", .ShowOnHome = "True"}

                If File.Exists(GameIconPath) Then
                    NewOrbisGame.IconPath = GameIconPath
                ElseIf File.Exists(GameAlternativeIconPath) Then
                    NewOrbisGame.IconPath = GameAlternativeIconPath
                End If
                If File.Exists(GameBackgroundPath) Then
                    NewOrbisGame.BackgroundPath = GameBackgroundPath
                ElseIf File.Exists(GameAlternativeBackgroundPath) Then
                    NewOrbisGame.BackgroundPath = GameAlternativeBackgroundPath
                End If

                'Add selected game to the library
                Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
                Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
                GamesList.Games.Add(NewOrbisGame)

                Dim NewGamesListJSON As String = JsonConvert.SerializeObject(GamesList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
                File.WriteAllText(GameLibraryPath, NewGamesListJSON)
            End If

        End If

        'Update progress
        If Dispatcher.CheckAccess() = False Then
            Dispatcher.BeginInvoke(Sub()
                                       CancelTextBlock.Text = "Close"

                                       If InstallationAborted Then
                                           InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                                           InstallProgressBar.IsIndeterminate = False
                                           InstallProgressBar.Value = 0
                                       Else
                                           InstallerStatusTextBlock.Text = "PKG Installation Done"
                                           InstallerHeaderTextBlock.Text = PKGTitle + " has been installed."
                                           InstallProgressBar.IsIndeterminate = False
                                           InstallProgressBar.Value = 100
                                       End If
                                   End Sub)
        Else
            CancelTextBlock.Text = "Close"

            If InstallationAborted Then
                InstallerStatusTextBlock.Text = "PKG Installation Aborted"
                InstallProgressBar.IsIndeterminate = False
                InstallProgressBar.Value = 0
            Else
                InstallerStatusTextBlock.Text = "PKG Installation Done"
                InstallerHeaderTextBlock.Text = PKGTitle + " has been installed."
                InstallProgressBar.IsIndeterminate = False
                InstallProgressBar.Value = 100
            End If
        End If

        PauseInput = False
    End Sub

End Class
