Imports System.ComponentModel

Public Class DownloadListViewItem

    Implements INotifyPropertyChanged

    Public _AppName As String
    Public _AppIcon As ImageSource
    Public _AppIsDownloading As Visibility
    Public _IsAppSelected As Visibility
    Public _AppDataLabel As String
    Public _AllDataLabel As String
    Public _DownloadProgress As String
    Public _SecondDownloadProgress As String
    Public _ProgressValue As Integer
    Public _InstalledOrUpdated As String

    Public Property AppName() As String
        Get
            Return _AppName
        End Get
        Set(Value As String)
            _AppName = Value
            NotifyPropertyChanged("AppName")
        End Set
    End Property
    Public Property AppIcon() As ImageSource
        Get
            Return _AppIcon
        End Get
        Set(Value As ImageSource)
            _AppIcon = Value
            NotifyPropertyChanged("AppIcon")
        End Set
    End Property
    Public Property AppIsDownloading() As Visibility
        Get
            Return _AppIsDownloading
        End Get
        Set(Value As Visibility)
            _AppIsDownloading = Value
            NotifyPropertyChanged("AppIsDownloadinge")
        End Set
    End Property
    Public Property IsAppSelected() As Visibility
        Get
            Return _IsAppSelected
        End Get
        Set(Value As Visibility)
            _IsAppSelected = Value
            NotifyPropertyChanged("IsAppSelected")
        End Set
    End Property
    Public Property AppDataLabel() As String
        Get
            Return _AppDataLabel
        End Get
        Set(Value As String)
            _AppDataLabel = Value
            NotifyPropertyChanged("AppDataLabel")
        End Set
    End Property
    Public Property AllDataLabel() As String
        Get
            Return _AllDataLabel
        End Get
        Set(Value As String)
            _AllDataLabel = Value
            NotifyPropertyChanged("AllDataLabel")
        End Set
    End Property
    Public Property DownloadProgress() As String
        Get
            Return _DownloadProgress
        End Get
        Set(Value As String)
            _DownloadProgress = Value
            NotifyPropertyChanged("DownloadProgress")
        End Set
    End Property
    Public Property SecondDownloadProgress() As String
        Get
            Return _SecondDownloadProgress
        End Get
        Set(Value As String)
            _SecondDownloadProgress = Value
            NotifyPropertyChanged("SecondDownloadProgress")
        End Set
    End Property

    Public Property ProgressValue() As Integer
        Get
            Return _ProgressValue
        End Get
        Set(Value As Integer)
            _ProgressValue = Value
            NotifyPropertyChanged("ProgressValue")
        End Set
    End Property

    Public Property InstalledOrUpdated() As String
        Get
            Return _InstalledOrUpdated
        End Get
        Set(Value As String)
            _InstalledOrUpdated = Value
            NotifyPropertyChanged("InstalledOrUpdated")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

End Class
