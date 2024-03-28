Imports System.ComponentModel

Public Class FileBrowserListViewItem

    Implements INotifyPropertyChanged

    Private _IsFileFolderSelected As Visibility
    Private _FileFolderIcon As BitmapImage
    Private _FileFolderName As String
    Private _Type As String
    Private _IsExecutable As Boolean

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property IsFileFolderSelected As Visibility
        Get
            Return _IsFileFolderSelected
        End Get
        Set(Value As Visibility)
            _IsFileFolderSelected = Value
            NotifyPropertyChanged("IsFileFolderSelected")
        End Set
    End Property

    Public Property FileFolderIcon As BitmapImage
        Get
            Return _FileFolderIcon
        End Get
        Set(Value As BitmapImage)
            _FileFolderIcon = Value
            NotifyPropertyChanged("FileFolderIcon")
        End Set
    End Property

    Public Property FileFolderName As String
        Get
            Return _FileFolderName
        End Get
        Set(Value As String)
            _FileFolderName = Value
            NotifyPropertyChanged("FileFolderName")
        End Set
    End Property

    Public Property Type As String
        Get
            Return _Type
        End Get
        Set(Value As String)
            _Type = Value
            NotifyPropertyChanged("Type")
        End Set
    End Property

    Public Property IsExecutable As Boolean
        Get
            Return _IsExecutable
        End Get
        Set(Value As Boolean)
            _IsExecutable = Value
            NotifyPropertyChanged("IsExecutable")
        End Set
    End Property

End Class
