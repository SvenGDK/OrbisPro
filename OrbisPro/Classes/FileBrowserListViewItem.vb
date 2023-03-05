Imports System.ComponentModel

Public Class FileBrowserListViewItem

    'Notify on changes
    Implements INotifyPropertyChanged

    Private _IsFileFolderSelected As Visibility
    Private _FileFolderIcon As BitmapImage
    Private _FileFolderName As String
    Private _Type As String
    Private _IsExecutable As Boolean

    'Properties
    Public Property IsFileFolderSelected As Visibility
        Get
            Return _IsFileFolderSelected
        End Get
        Set
            _IsFileFolderSelected = Value
            NotifyPropertyChanged("IsFileFolderSelected")
        End Set
    End Property

    Public Property FileFolderIcon As BitmapImage
        Get
            Return _FileFolderIcon
        End Get
        Set
            _FileFolderIcon = Value
            NotifyPropertyChanged("FileFolderIcon")
        End Set
    End Property

    Public Property FileFolderName As String
        Get
            Return _FileFolderName
        End Get
        Set
            _FileFolderName = Value
            NotifyPropertyChanged("FileFolderName")
        End Set
    End Property

    Public Property Type As String 'FILE/FOLDER/EXECUTABLE
        Get
            Return _Type
        End Get
        Set
            _Type = Value
            NotifyPropertyChanged("Type")
        End Set
    End Property

    Public Property IsExecutable As Boolean 'FALSE = FILE/FOLDER
        Get
            Return _IsExecutable
        End Get
        Set
            _IsExecutable = Value
            NotifyPropertyChanged("IsExecutable")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

End Class
