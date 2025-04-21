Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Management
Imports System.Net
Imports System.Net.Http
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json

Public Class OrbisUtils

    Private Declare Function DeleteObject Lib "gdi32.dll" (hObject As IntPtr) As Boolean
    Public Shared Property MainConfigFile As New INI.IniFile(FileIO.FileSystem.CurrentDirectory + "\System\Settings.ini")
    Public Shared Property AppLibraryPath As String = FileIO.FileSystem.CurrentDirectory + "\Apps\AppsList.json"
    Public Shared Property GameLibraryPath As String = FileIO.FileSystem.CurrentDirectory + "\Games\GamesList.json"
    Public Shared Property FoldersPath As String = FileIO.FileSystem.CurrentDirectory + "\System\Folders.json"

    Private Shared _OrbisBackground As String
    Private Shared _SharedDeviceModel As DeviceModel
    Private Shared _UsedDriveList As List(Of DriveListViewItem)
    Private Shared ReadOnly Separator As String() = New String() {"  "}

    Public Shared Property OrbisBackground As String
        Get
            Return _OrbisBackground
        End Get
        Set(Value As String)
            _OrbisBackground = Value
        End Set
    End Property

    Public Shared Property SharedDeviceModel As DeviceModel
        Get
            Return _SharedDeviceModel
        End Get
        Set(Value As DeviceModel)
            _SharedDeviceModel = Value
        End Set
    End Property

    Public Shared Property UsedDriveList As List(Of DriveListViewItem)
        Get
            Return _UsedDriveList
        End Get
        Set
            _UsedDriveList = Value
        End Set
    End Property

    Public Enum SystemMessage
        ErrorMessage
        WarningMessage
        InfoMessage
    End Enum

#Region "Device Identification"

    Public Enum DeviceModel
        PC
        ROGAlly
        ROGAllyX
        SteamDeck
        LegionGo
        Claw
    End Enum

    Public Shared Sub CheckDeviceModel()
        Dim MOS As New ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")
        Dim Model As String = String.Empty

        For Each objMgmt In MOS.Get
            Model = objMgmt("model").ToString()
            Exit For
        Next

        Select Case Model
            Case "ROG Ally RC71L_RC71L"
                SharedDeviceModel = DeviceModel.ROGAlly
            Case "ROG Ally RC72L_RC72L"
                SharedDeviceModel = DeviceModel.ROGAllyX
            Case Else
                SharedDeviceModel = DeviceModel.PC
        End Select
    End Sub

#End Region

#Region "ListView Helper Functions"

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

#End Region

#Region "Virtual Keyboard Helper"

    Public Shared Sub ShowVirtualKeyboard()
        Dim RunningProcesses = Process.GetProcesses()

        'Kill if already running
        For Each RunningProcess As Process In RunningProcesses
            If RunningProcess.ProcessName = "TabTip" Then
                RunningProcess.Kill()
                Exit For
            End If
        Next

        Dim NewProcessStartInfo As New ProcessStartInfo() With {.FileName = "C:\Program Files\Common Files\Microsoft Shared\Ink\TabTip.exe", .Arguments = "runas", .UseShellExecute = True}
        Dim NewTapTipProcess As New Process() With {.StartInfo = NewProcessStartInfo}
        NewTapTipProcess.Start()
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

#End Region

#Region "Image Processing"

    Public Shared Function GetExecutableIconAsImageSource(FileName As String) As ImageSource
        If Not String.IsNullOrEmpty(FileName) Then
            Dim icon As Icon = Icon.ExtractAssociatedIcon(FileName)
            Return ToImageSource(icon)
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

    Public Shared Function ToImageSource(InputIcon As Icon) As ImageSource
        Dim bitmap As Bitmap = InputIcon.ToBitmap()
        Dim hBitmap As IntPtr = bitmap.GetHbitmap()

        Dim wpfBitmap As ImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())

        If Not DeleteObject(hBitmap) Then
            Throw New Win32Exception()
        End If

        Return wpfBitmap
    End Function

    Public Shared Function CreateFolderImage(Image1 As BitmapImage, Image2 As BitmapImage, Image3 As BitmapImage, Image4 As BitmapImage) As ImageSource
        Dim NewDrawingVisual As New DrawingVisual()
        Using NewDrawingContext = NewDrawingVisual.RenderOpen()
            NewDrawingContext.DrawImage(Image1, New Rect(0, 0, 128, 128))
            NewDrawingContext.DrawImage(Image2, New Rect(128, 0, 128, 128))
            NewDrawingContext.DrawImage(Image3, New Rect(0, 128, 128, 128))
            NewDrawingContext.DrawImage(Image4, New Rect(128, 128, 128, 128))
        End Using

        Dim NewRenderTargetBitmap As New RenderTargetBitmap(256, 256, 96, 96, PixelFormats.Pbgra32)
        NewRenderTargetBitmap.Render(NewDrawingVisual)

        If NewRenderTargetBitmap IsNot Nothing Then
            Return NewRenderTargetBitmap
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function BitmapSourceFromByteArray(buffer As Byte()) As BitmapSource
        Dim bitmap = New BitmapImage()

        Using stream = New MemoryStream(buffer)
            bitmap.BeginInit()
            bitmap.CacheOption = BitmapCacheOption.OnLoad
            bitmap.StreamSource = stream
            bitmap.EndInit()
        End Using

        bitmap.Freeze()
        Return bitmap
    End Function

    Public Shared Function GetIconForExtension(WindowIconCache As Dictionary(Of String, BitmapImage), FileExtension As String) As BitmapImage
        FileExtension = FileExtension.ToLowerInvariant()
        Dim ExistingBitmapImage As BitmapImage = Nothing
        'Check for existing icon
        If WindowIconCache.TryGetValue(FileExtension, ExistingBitmapImage) Then
            Return ExistingBitmapImage
        Else
            'Add icon to cache
            Dim IconPath As String = $"/Icons/FileIcons/{FileExtension}.png"
            Dim NewBitmapImage As New BitmapImage()
            NewBitmapImage.BeginInit()
            NewBitmapImage.UriSource = New Uri(IconPath, UriKind.RelativeOrAbsolute)
            NewBitmapImage.DecodePixelHeight = 90
            NewBitmapImage.DecodePixelWidth = 90
            NewBitmapImage.EndInit()

            WindowIconCache(FileExtension) = NewBitmapImage
            Return NewBitmapImage
        End If
    End Function

#End Region

    Public Shared Async Function IsURLValidAsync(Url As String) As Task(Of Boolean)
        If NetworkInterface.GetIsNetworkAvailable Then
            Try
                Dim NewHttpClient As New HttpClient()
                Dim NewHttpRequestMessage As New HttpRequestMessage(HttpMethod.Head, Url)
                Dim NewHttpResponseMessage As HttpResponseMessage = Await NewHttpClient.SendAsync(NewHttpRequestMessage)

                If NewHttpResponseMessage.StatusCode = HttpStatusCode.OK Then
                    Return True
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.Found Then
                    Return True
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.NotFound Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.Unauthorized Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.Forbidden Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.BadGateway Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.BadRequest Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.RequestTimeout Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.GatewayTimeout Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.InternalServerError Then
                    Return False
                ElseIf NewHttpResponseMessage.StatusCode = HttpStatusCode.ServiceUnavailable Then
                    Return False
                Else
                    Return False
                End If

            Catch Ex As SocketException
                Return False
            End Try
        Else
            Return False
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

    Public Shared Sub CreateGameDirectories()
        'Create game directories (for roms) at default install location
        If Not Directory.Exists(FileIO.FileSystem.CurrentDirectory + "\Games") Then
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\PS1")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\PS2")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\PS3")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\PS4")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\NES")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\SNES")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\SMS")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\MegaDrive")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\Saturn")
            Directory.CreateDirectory(FileIO.FileSystem.CurrentDirectory + "\Games\Dreamcast")
        End If
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

    Public Shared Function GetUSBDeviceVendor(VID As String) As String
        If Not String.IsNullOrEmpty(VID) AndAlso File.Exists(FileIO.FileSystem.CurrentDirectory + "\System\usb.ids") Then

            Dim USBIDs As String() = File.ReadAllLines(FileIO.FileSystem.CurrentDirectory + "\System\usb.ids")
            Dim Vendor As String = ""

            For Each USBID As String In USBIDs
                If USBID.StartsWith(VID, StringComparison.OrdinalIgnoreCase) Then
                    Vendor = USBID.Split(Separator, StringSplitOptions.None)(1)
                    Exit For
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

    Public Shared Function FormatSizeAsString(Size As Long) As String
        Dim DoubleBytes As Double
        Try
            Select Case Size
                Case Is >= 1099511627776
                    DoubleBytes = Size / 1099511627776 'TB
                    Return FormatNumber(DoubleBytes, 2) & " TB"
                Case 1073741824 To 1099511627775
                    DoubleBytes = Size / 1073741824 'GB
                    Return FormatNumber(DoubleBytes, 2) & " GB"
                Case 1048576 To 1073741823
                    DoubleBytes = Size / 1048576 'MB
                    Return FormatNumber(DoubleBytes, 2) & " MB"
                Case 1024 To 1048575
                    DoubleBytes = Size / 1024 'KB
                    Return FormatNumber(DoubleBytes, 2) & " KB"
                Case 0 To 1023
                    DoubleBytes = Size ' Bytes
                    Return FormatNumber(DoubleBytes, 2) & " Bytes"
                Case Else
                    Return ""
            End Select
        Catch
            Return ""
        End Try
    End Function

    'Private Function GetPartitionAndDriveLetter() As String()
    '    Dim USBPartition As String = ""
    '    Dim USBLetter As String = ""

    '    For Each DiskDrive In New ManagementObjectSearcher("select * from Win32_DiskDrive where InterfaceType='USB'").Get()
    '        For Each DiskPartition As ManagementObject In New ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + DiskDrive("DeviceID").ToString + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition").Get()
    '            USBPartition = DiskPartition("Name").ToString()
    '            For Each Disk In New ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + DiskPartition("DeviceID").ToString + "'} WHERE AssocClass =Win32_LogicalDiskToPartition").Get()
    '                USBLetter = Disk("Name").ToString()
    '            Next
    '        Next
    '    Next

    '    If Not String.IsNullOrEmpty(USBPartition) AndAlso Not String.IsNullOrEmpty(USBLetter) Then
    '        Return {USBPartition, USBLetter}
    '    Else
    '        Return {"", ""}
    '    End If
    'End Function

    Public Shared Function GetDriveLetterFromMask(ByRef Unit As Integer) As Char
        Dim NewChar As Char = Nothing
        For i As Integer = 0 To 25
            If Unit = (2 ^ i) Then
                NewChar = Chr(Asc("A") + i)
            End If
        Next
        Return NewChar
    End Function

    Public Shared Function GetFolderItemsCount(GroupName As String) As String
        If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)
            Dim ListOfMatches As New List(Of String)()

            'Check the Apps list
            For Each RegisteredApp In AppsList.Apps()
                If RegisteredApp.Group = GroupName Then
                    ListOfMatches.Add(RegisteredApp.ExecutablePath)
                End If
            Next

            'Check the Games list
            For Each RegisteredGame In GamesList.Games()
                If RegisteredGame.Group = GroupName Then
                    ListOfMatches.Add(RegisteredGame.ExecutablePath)
                End If
            Next

            If ListOfMatches.Count > 0 Then
                Return ListOfMatches.Count.ToString()
            Else
                Return ""
            End If
        Else
            Return ""
        End If
    End Function

    Public Shared Function GetFolderItems(GroupName As String) As ObservableCollection(Of FolderContentListViewItem)

        Dim NewFolderContentListing As New ObservableCollection(Of FolderContentListViewItem)()

        If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

            'Get some executable paths to find some images for the folder
            Dim ListOfMatches As New List(Of Tuple(Of String, String))()
            Dim FinalListOfMatches As New List(Of Tuple(Of BitmapImage, String))()

            'Check the Apps list
            For Each RegisteredApp In AppsList.Apps()
                If RegisteredApp.Group = GroupName Then
                    If Not String.IsNullOrEmpty(RegisteredApp.ExecutablePath) Then
                        ListOfMatches.Add(New Tuple(Of String, String)(RegisteredApp.ExecutablePath, RegisteredApp.Name))
                    End If
                End If
            Next

            'Check the Games list
            For Each RegisteredGame In GamesList.Games()
                If RegisteredGame.Group = GroupName Then
                    If Not String.IsNullOrEmpty(RegisteredGame.ExecutablePath) Then
                        ListOfMatches.Add(New Tuple(Of String, String)(RegisteredGame.ExecutablePath, RegisteredGame.Name))
                    End If
                End If
            Next

            'Try to get up to 6 icons 
            If ListOfMatches.Count > 0 Then

                For Each FoundMatch In ListOfMatches
                    If Path.GetExtension(FoundMatch.Item1) = ".exe" Then
                        If Not String.IsNullOrEmpty(GameStarter.CheckForExistingIconAsset(FoundMatch.Item1)) Then
                            'Add with icon to the final collection
                            FinalListOfMatches.Add(New Tuple(Of BitmapImage, String)(New BitmapImage(New Uri(GameStarter.CheckForExistingIconAsset(FoundMatch.Item1), UriKind.RelativeOrAbsolute)), FoundMatch.Item2))
                        Else
                            'Add without icon to the final collection
                            FinalListOfMatches.Add(New Tuple(Of BitmapImage, String)(Nothing, FoundMatch.Item2))
                        End If
                    Else
                        'Add without icon to the final collection
                        FinalListOfMatches.Add(New Tuple(Of BitmapImage, String)(Nothing, FoundMatch.Item2))
                    End If
                Next

                'Add the found items to the collection
                For Each FoundGameOrApp In FinalListOfMatches
                    Dim NewFolderContentListViewItem As New FolderContentListViewItem() With {.FolderContentAppIcon = FoundGameOrApp.Item1, .FolderName = GroupName, .GameAppTitle = FoundGameOrApp.Item2}
                    NewFolderContentListing.Add(NewFolderContentListViewItem)
                Next

                'Return the collection
                Return NewFolderContentListing
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If

    End Function

End Class
