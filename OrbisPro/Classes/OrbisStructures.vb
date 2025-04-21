Public Class OrbisStructures

    Public Structure AppDetails

        Private _AppTitle As String
        Private _AppFolder As String
        Private _AppButtonExecutionString As String
        Private _AppIconPath As String
        Private _AppBackgroundPath As String
        Private _AppExecutableFilePath As String
        Private _IsFolder As Boolean
        Private _FolderName As String
        Private _IsGame As Boolean
        Private _AppPlatform As String

        Public Property AppTitle As String
            Get
                Return _AppTitle
            End Get
            Set
                _AppTitle = Value
            End Set
        End Property

        Public Property AppFolder As String
            Get
                Return _AppFolder
            End Get
            Set
                _AppFolder = Value
            End Set
        End Property

        Public Property AppButtonExecutionString As String
            Get
                Return _AppButtonExecutionString
            End Get
            Set
                _AppButtonExecutionString = Value
            End Set
        End Property

        Public Property AppIconPath As String
            Get
                Return _AppIconPath
            End Get
            Set
                _AppIconPath = Value
            End Set
        End Property

        Public Property AppBackgroundPath As String
            Get
                Return _AppBackgroundPath
            End Get
            Set
                _AppBackgroundPath = Value
            End Set
        End Property

        Public Property AppExecutableFilePath As String
            Get
                Return _AppExecutableFilePath
            End Get
            Set
                _AppExecutableFilePath = Value
            End Set
        End Property

        Public Property IsFolder As Boolean
            Get
                Return _IsFolder
            End Get
            Set
                _IsFolder = Value
            End Set
        End Property

        Public Property FolderName As String
            Get
                Return _FolderName
            End Get
            Set
                _FolderName = Value
            End Set
        End Property

        Public Property IsGame As Boolean
            Get
                Return _IsGame
            End Get
            Set
                _IsGame = Value
            End Set
        End Property

        Public Property AppPlatform As String
            Get
                Return _AppPlatform
            End Get
            Set
                _AppPlatform = Value
            End Set
        End Property

    End Structure

    Public Structure GameAppInstallerWorkerArgs
        Private _InstallFrom As String
        Private _InstallLocation As String
        Private _Type As String
        Private _PlaceInLibrary As Boolean
        Private _PlaceOnMainMenu As Boolean

        Public Property InstallFrom As String
            Get
                Return _InstallFrom
            End Get
            Set
                _InstallFrom = Value
            End Set
        End Property

        Public Property InstallLocation As String
            Get
                Return _InstallLocation
            End Get
            Set
                _InstallLocation = Value
            End Set
        End Property

        Public Property Type As String
            Get
                Return _Type
            End Get
            Set
                _Type = Value
            End Set
        End Property

        Public Property PlaceInLibrary As Boolean
            Get
                Return _PlaceInLibrary
            End Get
            Set
                _PlaceInLibrary = Value
            End Set
        End Property

        Public Property PlaceOnMainMenu As Boolean
            Get
                Return _PlaceOnMainMenu
            End Get
            Set
                _PlaceOnMainMenu = Value
            End Set
        End Property
    End Structure

    Public Structure GamepadInfo
        Private _GamepadName As String
        Private _GamepadGUID As Guid

        Public Property GamepadName As String
            Get
                Return _GamepadName
            End Get
            Set
                _GamepadName = Value
            End Set
        End Property

        Public Property GamepadGUID As Guid
            Get
                Return _GamepadGUID
            End Get
            Set
                _GamepadGUID = Value
            End Set
        End Property

    End Structure

    Public Structure InstalledApplication

        Private _DisplayIconPath As String
        Private _DisplayName As String
        Private _InstallLocation As String
        Private _UninstallString As String
        Private _ExecutableLocation As String

        Public Property DisplayIconPath As String
            Get
                Return _DisplayIconPath
            End Get
            Set
                _DisplayIconPath = Value
            End Set
        End Property

        Public Property DisplayName As String
            Get
                Return _DisplayName
            End Get
            Set
                _DisplayName = Value
            End Set
        End Property

        Public Property InstallLocation As String
            Get
                Return _InstallLocation
            End Get
            Set
                _InstallLocation = Value
            End Set
        End Property

        Public Property UninstallString As String
            Get
                Return _UninstallString
            End Get
            Set
                _UninstallString = Value
            End Set
        End Property

        Public Property ExecutableLocation As String
            Get
                Return _ExecutableLocation
            End Get
            Set
                _ExecutableLocation = Value
            End Set
        End Property
    End Structure

    Public Structure DiscoveredBluetoothDevice

        Private _Manufacturer As String
        Private _Connected As Boolean
        Private _Authenticated As Boolean
        Private _Remembered As Boolean
        Private _ClassOfDevice As InTheHand.Net.Bluetooth.DeviceClass
        Private _Address As InTheHand.Net.BluetoothAddress
        Private _FriendlyName As String

        Public Property FriendlyName As String
            Get
                Return _FriendlyName
            End Get
            Set
                _FriendlyName = Value
            End Set
        End Property

        Public Property Address As InTheHand.Net.BluetoothAddress
            Get
                Return _Address
            End Get
            Set
                _Address = Value
            End Set
        End Property

        Public Property ClassOfDevice As InTheHand.Net.Bluetooth.DeviceClass
            Get
                Return _ClassOfDevice
            End Get
            Set
                _ClassOfDevice = Value
            End Set
        End Property

        Public Property Remembered As Boolean
            Get
                Return _Remembered
            End Get
            Set
                _Remembered = Value
            End Set
        End Property

        Public Property Authenticated As Boolean
            Get
                Return _Authenticated
            End Get
            Set
                _Authenticated = Value
            End Set
        End Property

        Public Property Connected As Boolean
            Get
                Return _Connected
            End Get
            Set
                _Connected = Value
            End Set
        End Property

        Public Property Manufacturer As String
            Get
                Return _Manufacturer
            End Get
            Set
                _Manufacturer = Value
            End Set
        End Property

    End Structure

    Public Structure EthernetDevice

        Private _Speed As ULong
        Private _NetEnabled As Boolean
        Private _Name As String
        Private _Manufacturer As String
        Private _DeviceID As String
        Private _Description As String
        Private _AdapterType As String
        Private _MACAddress As String
        Private _NetworkAddresses As String()

        Public Property MACAddress As String
            Get
                Return _MACAddress
            End Get
            Set
                _MACAddress = Value
            End Set
        End Property

        Public Property AdapterType As String
            Get
                Return _AdapterType
            End Get
            Set
                _AdapterType = Value
            End Set
        End Property

        Public Property Description As String
            Get
                Return _Description
            End Get
            Set
                _Description = Value
            End Set
        End Property

        Public Property DeviceID As String
            Get
                Return _DeviceID
            End Get
            Set
                _DeviceID = Value
            End Set
        End Property

        Public Property Manufacturer As String
            Get
                Return _Manufacturer
            End Get
            Set
                _Manufacturer = Value
            End Set
        End Property

        Public Property Name As String
            Get
                Return _Name
            End Get
            Set
                _Name = Value
            End Set
        End Property

        Public Property NetEnabled As Boolean
            Get
                Return _NetEnabled
            End Get
            Set
                _NetEnabled = Value
            End Set
        End Property

        Public Property Speed As ULong
            Get
                Return _Speed
            End Get
            Set
                _Speed = Value
            End Set
        End Property

        Public Property NetworkAddresses As String()
            Get
                Return _NetworkAddresses
            End Get
            Set
                _NetworkAddresses = Value
            End Set
        End Property

    End Structure

End Class
