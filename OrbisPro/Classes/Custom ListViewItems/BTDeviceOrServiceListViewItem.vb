Imports System.ComponentModel

Public Class BTDeviceOrServiceListViewItem

    Implements INotifyPropertyChanged

    Private _IsDeviceSelected As Visibility
    Private _DeviceIcon As ImageSource
    Private _DeviceTitle As String
    Private _IsDevicePaired As Boolean
    Private _IsDeviceConnected As Boolean
    Private _DeviceAddress As InTheHand.Net.BluetoothAddress

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property IsDeviceSelected() As Visibility
        Get
            Return _IsDeviceSelected
        End Get
        Set(Value As Visibility)
            _IsDeviceSelected = Value
            NotifyPropertyChanged("IsDeviceSelected")
        End Set
    End Property

    Public Property DeviceIcon() As ImageSource
        Get
            Return _DeviceIcon
        End Get
        Set(Value As ImageSource)
            _DeviceIcon = Value
            NotifyPropertyChanged("DeviceIcon")
        End Set
    End Property

    Public Property DeviceTitle() As String
        Get
            Return _DeviceTitle
        End Get
        Set(Value As String)
            _DeviceTitle = Value
            NotifyPropertyChanged("DeviceTitle")
        End Set
    End Property

    Public Property IsDevicePaired() As Boolean
        Get
            Return _IsDevicePaired
        End Get
        Set(Value As Boolean)
            _IsDevicePaired = Value
            NotifyPropertyChanged("IsDevicePaired")
        End Set
    End Property

    Public Property IsDeviceConnected() As Boolean
        Get
            Return _IsDeviceConnected
        End Get
        Set(Value As Boolean)
            _IsDeviceConnected = Value
            NotifyPropertyChanged("IsDeviceConnected")
        End Set
    End Property

    Public Property DeviceAddress As InTheHand.Net.BluetoothAddress
        Get
            Return _DeviceAddress
        End Get
        Set
            _DeviceAddress = Value
        End Set
    End Property

End Class
