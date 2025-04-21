Imports System.ComponentModel

Public Class FolderContentListViewItem

    Implements INotifyPropertyChanged

    Private _FolderContentAppIcon As ImageSource
    Private _FolderName As String
    Private _GameAppTitle As String
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property FolderContentAppIcon As ImageSource
        Get
            Return _FolderContentAppIcon
        End Get
        Set
            _FolderContentAppIcon = Value
            NotifyPropertyChanged("FolderContentAppIcon")
        End Set
    End Property

    Public Property FolderName As String
        Get
            Return _FolderName
        End Get
        Set
            _FolderName = Value
            NotifyPropertyChanged("FolderName")
        End Set
    End Property

    Public Property GameAppTitle As String
        Get
            Return _GameAppTitle
        End Get
        Set
            _GameAppTitle = Value
            NotifyPropertyChanged("GameAppTitle")
        End Set
    End Property

End Class
