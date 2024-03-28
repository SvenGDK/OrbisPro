Imports System.ComponentModel
Imports ManagedNativeWifi

Public Class WiFiNetworkListViewItem

    Implements INotifyPropertyChanged

    Private _IsWiFiNetworkSelected As Visibility
    Private _WiFiSSID As String
    Private _IsWiFiSaved As Boolean
    Private _IsWiFiConnected As Boolean
    Private _WiFiSignalStrenght As String
    Private _IsWiFiSecured As Boolean
    Private _WiFiCipherAlgorithm As CipherAlgorithm
    Private _WiFiAuthenticationAlgorithm As AuthenticationAlgorithm
    Private _WiFiNetworkIdentifier As NetworkIdentifier
    Private _WiFiBssType As BssType
    Private _WiFiInterface As InterfaceInfo
    Private _IsWiFiSecuredLock As Visibility
    Private _WiFiSignalIcon As ImageSource
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property IsWiFiNetworkSelected As Visibility
        Get
            Return _IsWiFiNetworkSelected
        End Get
        Set(Value As Visibility)
            _IsWiFiNetworkSelected = Value
            NotifyPropertyChanged("IsWiFiNetworkSelected")
        End Set
    End Property

    Public Property WiFiSSID As String
        Get
            Return _WiFiSSID
        End Get
        Set(Value As String)
            _WiFiSSID = Value
            NotifyPropertyChanged("WiFiSSID")
        End Set
    End Property

    Public Property IsWiFiSaved As Boolean
        Get
            Return _IsWiFiSaved
        End Get
        Set(Value As Boolean)
            _IsWiFiSaved = Value
            NotifyPropertyChanged("IsWiFiSaved")
        End Set
    End Property

    Public Property IsWiFiConnected As Boolean
        Get
            Return _IsWiFiConnected
        End Get
        Set(Value As Boolean)
            _IsWiFiConnected = Value
            NotifyPropertyChanged("IsWiFiConnected")
        End Set
    End Property

    Public Property WiFiSignalStrenght As String
        Get
            Return _WiFiSignalStrenght
        End Get
        Set(Value As String)
            _WiFiSignalStrenght = Value
            NotifyPropertyChanged("WiFiSignalStrenght")
        End Set
    End Property

    Public Property WiFiSignalIcon As ImageSource
        Get
            Return _WiFiSignalIcon
        End Get
        Set(Value As ImageSource)
            _WiFiSignalIcon = Value
            NotifyPropertyChanged("WiFiSignalIcon")
        End Set
    End Property

    Public Property IsWiFiSecured As Boolean
        Get
            Return _IsWiFiSecured
        End Get
        Set(Value As Boolean)
            _IsWiFiSecured = Value
            NotifyPropertyChanged("IsWiFiSecured")
        End Set
    End Property

    Public Property IsWiFiSecuredLock As Visibility
        Get
            Return _IsWiFiSecuredLock
        End Get
        Set(Value As Visibility)
            _IsWiFiSecuredLock = Value
            NotifyPropertyChanged("IsWiFiSecuredLock")
        End Set
    End Property

    Public Property WiFiCipherAlgorithm As CipherAlgorithm
        Get
            Return _WiFiCipherAlgorithm
        End Get
        Set(Value As CipherAlgorithm)
            _WiFiCipherAlgorithm = Value
            NotifyPropertyChanged("WiFiCipherAlgorithm")
        End Set
    End Property

    Public Property WiFiAuthenticationAlgorithm As AuthenticationAlgorithm
        Get
            Return _WiFiAuthenticationAlgorithm
        End Get
        Set(Value As AuthenticationAlgorithm)
            _WiFiAuthenticationAlgorithm = Value
            NotifyPropertyChanged("WiFiAuthenticationAlgorithm")
        End Set
    End Property

    Public Property WiFiNetworkIdentifier As NetworkIdentifier
        Get
            Return _WiFiNetworkIdentifier
        End Get
        Set(Value As NetworkIdentifier)
            _WiFiNetworkIdentifier = Value
            NotifyPropertyChanged("WiFiNetworkIdentifier")
        End Set
    End Property

    Public Property WiFiBssType As BssType
        Get
            Return _WiFiBssType
        End Get
        Set(Value As BssType)
            _WiFiBssType = Value
            NotifyPropertyChanged("WiFiBssType")
        End Set
    End Property

    Public Property WiFiInterface As InterfaceInfo
        Get
            Return _WiFiInterface
        End Get
        Set(Value As InterfaceInfo)
            _WiFiInterface = Value
            NotifyPropertyChanged("WiFiInterface")
        End Set
    End Property

End Class
