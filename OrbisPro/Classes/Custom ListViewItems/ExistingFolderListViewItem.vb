Imports System.Collections.ObjectModel
Imports System.ComponentModel

Public Class ExistingFolderListViewItem

    Implements INotifyPropertyChanged

    Private _FolderContentItems As ObservableCollection(Of FolderContentListViewItem)
    Private _FolderContentCount As String
    Private _IsFolderSelected As Visibility
    Private _FolderName As String
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property FolderName As String
        Get
            Return _FolderName
        End Get
        Set
            _FolderName = Value
            NotifyPropertyChanged("FolderName")
        End Set
    End Property

    Public Property IsFolderSelected As Visibility
        Get
            Return _IsFolderSelected
        End Get
        Set
            _IsFolderSelected = Value
            NotifyPropertyChanged("IsFolderSelected")
        End Set
    End Property

    Public Property FolderContentCount As String
        Get
            Return _FolderContentCount
        End Get
        Set
            _FolderContentCount = Value
            NotifyPropertyChanged("FolderContentCount")
        End Set
    End Property

    Public Property FolderContentItems As ObservableCollection(Of FolderContentListViewItem)
        Get
            Return _FolderContentItems
        End Get
        Set
            _FolderContentItems = Value
            NotifyPropertyChanged("FolderContentItems")
        End Set
    End Property

End Class
