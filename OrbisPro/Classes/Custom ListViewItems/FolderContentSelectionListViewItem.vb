Imports System.ComponentModel

Public Class FolderContentSelectionListViewItem

    Implements INotifyPropertyChanged

    Private _IsFolderContentAppChecked As Boolean
    Private _FolderContentName As String
    Private _IsFolderContentAppSelected As Visibility
    Private _FolderContentAppIcon As ImageSource
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

    Public Property IsFolderContentAppSelected As Visibility
        Get
            Return _IsFolderContentAppSelected
        End Get
        Set
            _IsFolderContentAppSelected = Value
            NotifyPropertyChanged("IsFolderContentAppSelected")
        End Set
    End Property

    Public Property FolderContentName As String
        Get
            Return _FolderContentName
        End Get
        Set
            _FolderContentName = Value
            NotifyPropertyChanged("FolderContentName")
        End Set
    End Property

    Public Property IsFolderContentAppChecked As Boolean
        Get
            Return _IsFolderContentAppChecked
        End Get
        Set
            _IsFolderContentAppChecked = Value
            NotifyPropertyChanged("IsFolderContentAppChecked")
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
