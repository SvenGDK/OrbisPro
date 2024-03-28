Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Management
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions

Public Class OrbisUtils

    Public Shared ConfigFile As New INI.IniFile(My.Computer.FileSystem.CurrentDirectory + "\System\Settings.ini")

    <DllImport("gdi32.dll")>
    Private Shared Function GetDeviceCaps(hDC As IntPtr, nIndex As Integer) As Integer
    End Function

    Public Shared OrbisUser As String
    Public Shared OrbisBackground As String
    Public Shared SharedDeviceModel As DeviceModel = Nothing

    Public Shared ProcessList As New List(Of Process)()

    Public Enum SystemMessage
        ErrorMessage
        WarningMessage
        InfoMessage
    End Enum

    Public Enum DeviceModel
        PC
        ROGAlly
        SteamDeck
        LegionGo
    End Enum

    Public Shared Function IsURLValid(Url As String) As Boolean
        If NetworkInterface.GetIsNetworkAvailable Then
            Try
                Dim request As HttpWebRequest = CType(WebRequest.Create(Url), HttpWebRequest)
                Using response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)
                    If response.StatusCode = HttpStatusCode.OK Then
                        Return True
                    ElseIf response.StatusCode = HttpStatusCode.Found Then
                        Return True
                    ElseIf response.StatusCode = HttpStatusCode.NotFound Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.Unauthorized Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.Forbidden Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.BadGateway Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.BadRequest Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.RequestTimeout Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.GatewayTimeout Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.InternalServerError Then
                        Return False
                    ElseIf response.StatusCode = HttpStatusCode.ServiceUnavailable Then
                        Return False
                    Else
                        Return False
                    End If
                End Using
            Catch Ex As WebException
                Return False
            End Try
        Else
            Return False
        End If
    End Function

    Public Shared Function GetExecutableIconAsImageSource(FileName As String) As ImageSource
        If Not String.IsNullOrEmpty(FileName) Then
            Dim icon As Icon = Icon.ExtractAssociatedIcon(FileName)
            Return Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, New Int32Rect(Nothing, Nothing, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions())
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetExecutableIconAsBitmapImage(FileName As String) As BitmapImage
        If Not String.IsNullOrEmpty(FileName) Then
            Dim NewIcon As Icon = Icon.ExtractAssociatedIcon(FileName)

            If NewIcon IsNot Nothing Then
                Dim NewBitmap As Bitmap = NewIcon.ToBitmap()
                Dim TempBitmapImage = New BitmapImage()

                Using TempMemoryStream As New MemoryStream()
                    NewBitmap.Save(TempMemoryStream, ImageFormat.Png)
                    TempMemoryStream.Position = 0
                    TempBitmapImage.BeginInit()
                    TempBitmapImage.CacheOption = BitmapCacheOption.OnLoad
                    TempBitmapImage.StreamSource = TempMemoryStream
                    TempBitmapImage.EndInit()
                End Using

                Return TempBitmapImage
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetIntegerOnly(StringValue As String) As Integer
        Dim ReturnValue As String = String.Empty
        Dim NewMatchCollection As MatchCollection = Regex.Matches(StringValue, "\d+")
        For Each MatchInCollection As Match In NewMatchCollection
            ReturnValue += MatchInCollection.ToString()
        Next
        Return Convert.ToInt32(ReturnValue)
    End Function

    Public Shared Function GetAncestorOfType(Of T As FrameworkElement)(child As FrameworkElement) As T
        Dim parent = VisualTreeHelper.GetParent(child)
        If parent IsNot Nothing AndAlso Not (TypeOf parent Is T) Then Return GetAncestorOfType(Of T)(CType(parent, FrameworkElement))
        Return CType(parent, T)
    End Function

    Public Shared Function FindScrollViewer(DepObj As DependencyObject) As ScrollViewer
        If TypeOf DepObj Is ScrollViewer Then Return TryCast(DepObj, ScrollViewer)

        For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(DepObj) - 1
            Dim FoundScrollViewer = FindScrollViewer(VisualTreeHelper.GetChild(DepObj, i))
            If FoundScrollViewer IsNot Nothing Then Return FoundScrollViewer
        Next

        Return Nothing
    End Function

    Public Shared Function FindVisualChild(Of childItem As DependencyObject)(obj As DependencyObject) As childItem
        For i As Integer = 0 To VisualTreeHelper.GetChildrenCount(obj) - 1
            Dim child As DependencyObject = VisualTreeHelper.GetChild(obj, i)
            If child IsNot Nothing AndAlso TypeOf child Is childItem Then
                Return CType(child, childItem)
            Else
                Dim childOfChild As childItem = FindVisualChild(Of childItem)(child)
                If childOfChild IsNot Nothing Then
                    Return childOfChild
                End If
            End If
        Next i
        Return Nothing
    End Function

    Public Shared Sub CreateGameDirectories()
        'Create game directories (for roms) at default install location
        If Not Directory.Exists(My.Computer.FileSystem.CurrentDirectory + "\Games") Then
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS1")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS2")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS3")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\PS4")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\NES")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\SNES")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\SMS")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\MegaDrive")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\Saturn")
            Directory.CreateDirectory(My.Computer.FileSystem.CurrentDirectory + "\Games\Dreamcast")
        End If
    End Sub

    Public Shared Function CheckForExistingIconAsset(ExecutableFileName As String) As String
        Dim NewShortName As String = ""

        'Get asset for executable
        If Path.GetFileName(ExecutableFileName) = "re4.exe" Then
            NewShortName = "RE4N"
        ElseIf Path.GetFileName(ExecutableFileName) = "Resident Evil Village" Then
            NewShortName = "REVillage"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvmgo.exe" Then
            NewShortName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvtpp.exe" Then
            NewShortName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP.exe" Then
            NewShortName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP-Win64-Shipping.exe" Then
            NewShortName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "THPS12.exe" Then
            NewShortName = "TH1+2"
        ElseIf Path.GetFileName(ExecutableFileName) = "Audiosurf.exe" Then
            NewShortName = "Audiosurf"
        ElseIf Path.GetFileName(ExecutableFileName) = "Palworld.exe" Then
            NewShortName = "Palworld"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2.exe" Then
            NewShortName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2-Win64-Shipping.exe" Then
            NewShortName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "tomb123.exe" Then
            NewShortName = "TR1-3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tropico6.exe" Then
            NewShortName = "Tropico6"
        ElseIf Path.GetFileName(ExecutableFileName) = "WRCG.exe" Then
            NewShortName = "WRCGen"
        ElseIf Path.GetFileName(ExecutableFileName) = "Blasphemous 2.exe" Then
            NewShortName = "Blasphemous2"
        ElseIf Path.GetFileName(ExecutableFileName) = "GoW.exe" Then
            NewShortName = "GoW"
        ElseIf Path.GetFileName(ExecutableFileName) = "RiftApart.exe" Then
            NewShortName = "RatchetClankRA"
        ElseIf Path.GetFileName(ExecutableFileName) = "Diablo IV.exe" Then
            NewShortName = "DiabloIV"
        ElseIf Path.GetFileName(ExecutableFileName) = "Hearthstone.exe" Then
            NewShortName = "Hearthstone"
        ElseIf Path.GetFileName(ExecutableFileName) = "Cyberpunk2077.exe" Then
            NewShortName = "Cyberpunk"
        ElseIf Path.GetFileName(ExecutableFileName) = "AC4BFSP.exe" Then
            NewShortName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "AC4BFMP.exe" Then
            NewShortName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOdyssey.exe" Then
            NewShortName = "ACOdyssey"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOrigins.exe" Then
            NewShortName = "ACOrigins"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACC.exe" Then
            NewShortName = "ACRogue"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACS.exe" Then
            NewShortName = "ACSyndicate"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACU.exe" Then
            NewShortName = "ACUnity"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACValhalla.exe" Then
            NewShortName = "ACValhalla"
        ElseIf Path.GetFileName(ExecutableFileName) = "r5apex.exe" Then
            NewShortName = "ApexLegends"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3.exe" Then
            NewShortName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3_dx11.exe" Then
            NewShortName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "D2R.exe" Then
            NewShortName = "Diablo2R"
        ElseIf Path.GetFileName(ExecutableFileName) = "eldenring.exe" Then
            NewShortName = "EldenRing"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteLauncher.exe" Then
            NewShortName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteClient-Win64-Shipping.exe" Then
            NewShortName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "MilesMorales.exe" Then
            NewShortName = "SpiderManMM"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i.exe" Then
            NewShortName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i-l.exe" Then
            NewShortName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA3.exe" Then
            NewShortName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "LibertyCity.exe" Then
            NewShortName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta-vc.exe" Then
            NewShortName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "ViceCity.exe" Then
            NewShortName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "SanAndreas.exe" Then
            NewShortName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta_sa.exe" Then
            NewShortName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAV.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAVLauncher.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA5.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "TslGame.exe" Then
            NewShortName = "PUBG"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tomb Raider.exe" Then
            NewShortName = "TombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "ROTTR.exe" Then
            NewShortName = "ROTombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicMania.exe" Then
            NewShortName = "SonicMania"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicFrontiers.exe" Then
            NewShortName = "SonicFrontiers"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicSuperstars.exe" Then
            NewShortName = "SonicSuperstars"
        ElseIf Path.GetFileName(ExecutableFileName) = "HorizonForbiddenWest.exe" Then
            NewShortName = "HorizonFW"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID.exe" Then
            NewShortName = "MGS1"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID2.exe" Then
            NewShortName = "MGS2"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID3.exe" Then
            NewShortName = "MGS3"
        ElseIf Path.GetFileName(ExecutableFileName) = "witcher3.exe" Then
            NewShortName = "TW3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.x64.exe" Then
            NewShortName = "Warframe"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.exe" Then
            NewShortName = "Warframe"
        ElseIf Path.GetFileName(ExecutableFileName) = "steam.exe" Then
            NewShortName = "Steam"
        ElseIf Path.GetFileName(ExecutableFileName) = "Battle.net Launcher.exe" Then
            NewShortName = "BattleNet"
        ElseIf Path.GetFileName(ExecutableFileName) = "EpicGamesLauncher.exe" Then
            NewShortName = "EpicGamesLauncher"
        ElseIf Path.GetFileName(ExecutableFileName) = "UbisoftConnect.exe" Then
            NewShortName = "UbisoftConnect"
        ElseIf Path.GetFileName(ExecutableFileName) = "EALauncher.exe" Then
            NewShortName = "EALauncher"
        Else
            Return String.Empty
        End If

        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Assets\GameIcons\" + NewShortName + "_icon.jpg") Then
            Return My.Computer.FileSystem.CurrentDirectory + "\Assets\GameIcons\" + NewShortName + "_icon.jpg"
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function CheckForExistingBackgroundAsset(ExecutableFileName As String) As String
        Dim NewShortName As String = ""

        'Get asset for executable
        If Path.GetFileName(ExecutableFileName) = "re4.exe" Then
            NewShortName = "RE4N"
        ElseIf Path.GetFileName(ExecutableFileName) = "Resident Evil Village" Then
            NewShortName = "REVillage"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvmgo.exe" Then
            NewShortName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "mgsvtpp.exe" Then
            NewShortName = "MGSV"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP.exe" Then
            NewShortName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOP-Win64-Shipping.exe" Then
            NewShortName = "LiesOfP"
        ElseIf Path.GetFileName(ExecutableFileName) = "THPS12.exe" Then
            NewShortName = "TH1+2"
        ElseIf Path.GetFileName(ExecutableFileName) = "Audiosurf.exe" Then
            NewShortName = "Audiosurf"
        ElseIf Path.GetFileName(ExecutableFileName) = "Palworld.exe" Then
            NewShortName = "Palworld"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2.exe" Then
            NewShortName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "LOTF2-Win64-Shipping.exe" Then
            NewShortName = "LOTF"
        ElseIf Path.GetFileName(ExecutableFileName) = "tomb123.exe" Then
            NewShortName = "TR1-3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tropico6.exe" Then
            NewShortName = "Tropico6"
        ElseIf Path.GetFileName(ExecutableFileName) = "WRCG.exe" Then
            NewShortName = "WRCGen"
        ElseIf Path.GetFileName(ExecutableFileName) = "Blasphemous 2.exe" Then
            NewShortName = "Blasphemous2"
        ElseIf Path.GetFileName(ExecutableFileName) = "GoW.exe" Then
            NewShortName = "GoW"
        ElseIf Path.GetFileName(ExecutableFileName) = "RiftApart.exe" Then
            NewShortName = "RatchetClankRA"
        ElseIf Path.GetFileName(ExecutableFileName) = "Diablo IV.exe" Then
            NewShortName = "DiabloIV"
        ElseIf Path.GetFileName(ExecutableFileName) = "Hearthstone.exe" Then
            NewShortName = "Hearthstone"
        ElseIf Path.GetFileName(ExecutableFileName) = "Cyberpunk2077.exe" Then
            NewShortName = "Cyberpunk"
        ElseIf Path.GetFileName(ExecutableFileName) = "AC4BFSP.exe" Then
            NewShortName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "AC4BFMP.exe" Then
            NewShortName = "ACBlackFlag"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOdyssey.exe" Then
            NewShortName = "ACOdyssey"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACOrigins.exe" Then
            NewShortName = "ACOrigins"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACC.exe" Then
            NewShortName = "ACRogue"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACS.exe" Then
            NewShortName = "ACSyndicate"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACU.exe" Then
            NewShortName = "ACUnity"
        ElseIf Path.GetFileName(ExecutableFileName) = "ACValhalla.exe" Then
            NewShortName = "ACValhalla"
        ElseIf Path.GetFileName(ExecutableFileName) = "r5apex.exe" Then
            NewShortName = "ApexLegends"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3.exe" Then
            NewShortName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "bg3_dx11.exe" Then
            NewShortName = "BG3"
        ElseIf Path.GetFileName(ExecutableFileName) = "D2R.exe" Then
            NewShortName = "Diablo2R"
        ElseIf Path.GetFileName(ExecutableFileName) = "eldenring.exe" Then
            NewShortName = "EldenRing"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteLauncher.exe" Then
            NewShortName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "FortniteClient-Win64-Shipping.exe" Then
            NewShortName = "Fortnite"
        ElseIf Path.GetFileName(ExecutableFileName) = "MilesMorales.exe" Then
            NewShortName = "SpiderManMM"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i.exe" Then
            NewShortName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "tlou-i-l.exe" Then
            NewShortName = "TLOUP1"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA3.exe" Then
            NewShortName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "LibertyCity.exe" Then
            NewShortName = "GTA3Def"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta-vc.exe" Then
            NewShortName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "ViceCity.exe" Then
            NewShortName = "GTAVCDef"
        ElseIf Path.GetFileName(ExecutableFileName) = "SanAndreas.exe" Then
            NewShortName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "gta_sa.exe" Then
            NewShortName = "GTASADef"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAV.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTAVLauncher.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "GTA5.exe" Then
            NewShortName = "GTA5"
        ElseIf Path.GetFileName(ExecutableFileName) = "TslGame.exe" Then
            NewShortName = "PUBG"
        ElseIf Path.GetFileName(ExecutableFileName) = "Tomb Raider.exe" Then
            NewShortName = "TombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "ROTTR.exe" Then
            NewShortName = "ROTombRaider"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicMania.exe" Then
            NewShortName = "SonicMania"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicFrontiers.exe" Then
            NewShortName = "SonicFrontiers"
        ElseIf Path.GetFileName(ExecutableFileName) = "SonicSuperstars.exe" Then
            NewShortName = "SonicSuperstars"
        ElseIf Path.GetFileName(ExecutableFileName) = "HorizonForbiddenWest.exe" Then
            NewShortName = "HorizonFW"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID.exe" Then
            NewShortName = "MGS1"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID2.exe" Then
            NewShortName = "MGS2"
        ElseIf Path.GetFileName(ExecutableFileName) = "METAL GEAR SOLID3.exe" Then
            NewShortName = "MGS3"
        ElseIf Path.GetFileName(ExecutableFileName) = "witcher3.exe" Then
            NewShortName = "TW3"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.x64.exe" Then
            NewShortName = "Warframe"
        ElseIf Path.GetFileName(ExecutableFileName) = "Warframe.exe" Then
            NewShortName = "Warframe"
        Else
            Return String.Empty
        End If

        If File.Exists(My.Computer.FileSystem.CurrentDirectory + "\Assets\GameBackgrounds\" + NewShortName + "_BG.jpg") Then
            Return My.Computer.FileSystem.CurrentDirectory + "\Assets\GameBackgrounds\" + NewShortName + "_BG.jpg"
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function GetMonitorFrequency() As Integer
        Dim Graphs As Graphics = Graphics.FromHwnd(IntPtr.Zero)
        Dim Desk As IntPtr = Graphs.GetHdc()
        Dim RefreshRate As Integer = GetDeviceCaps(Desk, 116)

        Return RefreshRate
    End Function

    Public Shared Function CheckDeviceModel() As String
        Dim MOS As New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
        Dim Model As String = String.Empty

        For Each objMgmt In MOS.Get
            Model = objMgmt("model").ToString()
            Exit For
        Next

        Select Case Model
            Case "ROG Ally RC71L_RC71L"
                SharedDeviceModel = DeviceModel.ROGAlly
            Case Else
                SharedDeviceModel = DeviceModel.PC
        End Select

        Return Model
    End Function

    Public Shared Sub SetUserName()


        If Not String.IsNullOrEmpty(ConfigFile.IniReadValue("System", "Username")) Then
            OrbisUser = ConfigFile.IniReadValue("System", "Username")
        End If
    End Sub

    Public Shared Sub ShowVirtualKeyboard()
        Dim RunningProcesses = Process.GetProcesses()

        'Kill if already running
        For Each RunningProcess As Process In RunningProcesses
            If RunningProcess.ProcessName = "TabTip" Then
                RunningProcess.Kill()
                Exit For
            End If
        Next

        Process.Start("C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe")
    End Sub

    Public Shared Sub HideVirtualKeyboard()
        Dim RunningProcesses = Process.GetProcesses()
        For Each RunningProcess As Process In RunningProcesses
            If RunningProcess.ProcessName = "TabTip" Then
                RunningProcess.Kill()
                Exit For
            End If
        Next
    End Sub

    Public Shared Sub SetInternalBackgroundName(BackgroundFileName As String)
        If Not String.IsNullOrEmpty(BackgroundFileName) Then
            Select Case Path.GetFileNameWithoutExtension(BackgroundFileName)
                Case "bluecircles"
                    OrbisBackground = "BlueCircles"
                Case "gradient_bg"
                    OrbisBackground = "OrangeRed Gradient Waves"
                Case "ps2_bg"
                    OrbisBackground = "PS2 Dots"
                Case Else
                    OrbisBackground = "Custom"
            End Select
        End If
    End Sub

    Public Shared Function GetDeviceVendor(VID As String) As String
        If Not String.IsNullOrEmpty(VID) Then

            Dim USBIDs As String() = File.ReadAllLines(My.Computer.FileSystem.CurrentDirectory + "\System\usb.ids")
            Dim Vendor As String = ""

            For Each USBID As String In USBIDs
                If USBID.StartsWith(VID, StringComparison.OrdinalIgnoreCase) Then
                    Vendor = USBID.Split(New String() {"  "}, StringSplitOptions.None)(1)
                End If
            Next

            Return Vendor
        Else
            Return String.Empty
        End If
    End Function

    Public Shared Function GetHexString(Source As String) As String
        Dim b As Byte() = System.Text.Encoding.UTF8.GetBytes(Source)
        Return BitConverter.ToString(b).Replace("-", "")
    End Function

End Class
