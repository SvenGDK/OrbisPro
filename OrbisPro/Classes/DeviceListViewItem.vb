Imports System.ComponentModel

Public Class DeviceListViewItem

    Implements INotifyPropertyChanged

    Private _DeviceName As String
    Private _DeviceIcon As BitmapImage
    Private _IsDeviceSelected As Visibility
    Private _DeviceType As String
    Private _AccessiblePath As String

    Public Property DeviceName As String
        Get
            Return _DeviceName
        End Get
        Set
            _DeviceName = Value
            NotifyPropertyChanged("DeviceName")
        End Set
    End Property

    Public Property DeviceIcon As BitmapImage
        Get
            Return _DeviceIcon
        End Get
        Set
            _DeviceIcon = Value
            NotifyPropertyChanged("DeviceIcon")
        End Set
    End Property

    Public Property IsDeviceSelected As Visibility
        Get
            Return _IsDeviceSelected
        End Get
        Set
            _IsDeviceSelected = Value
            NotifyPropertyChanged("IsDeviceSelected")
        End Set
    End Property

    Public Property DeviceType As String
        Get
            Return _DeviceType
        End Get
        Set
            _DeviceType = Value
            NotifyPropertyChanged("DeviceType")
        End Set
    End Property

    Public Property AccessiblePath As String
        Get
            Return _AccessiblePath
        End Get
        Set
            _AccessiblePath = Value
            NotifyPropertyChanged("AccessiblePath")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

End Class
