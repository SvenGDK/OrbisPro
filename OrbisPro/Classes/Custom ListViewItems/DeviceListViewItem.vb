Imports System.ComponentModel

Public Class DeviceListViewItem

    Implements INotifyPropertyChanged

    Private _DeviceName As String
    Private _DeviceIcon As BitmapImage
    Private _IsDeviceSelected As Visibility
    Private _DeviceType As String
    Private _AccessiblePath As String

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property DeviceName As String
        Get
            Return _DeviceName
        End Get
        Set(Value As String)
            _DeviceName = Value
            NotifyPropertyChanged("DeviceName")
        End Set
    End Property

    Public Property DeviceIcon As BitmapImage
        Get
            Return _DeviceIcon
        End Get
        Set(Value As BitmapImage)
            _DeviceIcon = Value
            NotifyPropertyChanged("DeviceIcon")
        End Set
    End Property

    Public Property IsDeviceSelected As Visibility
        Get
            Return _IsDeviceSelected
        End Get
        Set(Value As Visibility)
            _IsDeviceSelected = Value
            NotifyPropertyChanged("IsDeviceSelected")
        End Set
    End Property

    Public Property DeviceType As String
        Get
            Return _DeviceType
        End Get
        Set(Value As String)
            _DeviceType = Value
            NotifyPropertyChanged("DeviceType")
        End Set
    End Property

    Public Property AccessiblePath As String
        Get
            Return _AccessiblePath
        End Get
        Set(Value As String)
            _AccessiblePath = Value
            NotifyPropertyChanged("AccessiblePath")
        End Set
    End Property

End Class
