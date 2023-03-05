Imports System.ComponentModel

Public Class OpenWindowListViewItem

    'Notify on changes
    Implements INotifyPropertyChanged

    Private _IsItemSelected As Visibility
    Private _ItemIcon As BitmapImage
    Private _ItemName As String
    Private _ItemSubDescription As String

    'Properties
    Public Property IsItemSelected As Visibility
        Get
            Return _IsItemSelected
        End Get
        Set
            _IsItemSelected = Value
            NotifyPropertyChanged("IsItemSelected")
        End Set
    End Property

    Public Property ItemIcon As BitmapImage
        Get
            Return _ItemIcon
        End Get
        Set
            _ItemIcon = Value
            NotifyPropertyChanged("ItemIcon")
        End Set
    End Property

    Public Property ItemName As String
        Get
            Return _ItemName
        End Get
        Set
            _ItemName = Value
            NotifyPropertyChanged("ItemName")
        End Set
    End Property

    Public Property ItemSubDescription As String
        Get
            Return _ItemSubDescription
        End Get
        Set
            _ItemSubDescription = Value
            NotifyPropertyChanged("ItemSubDescription")
        End Set
    End Property

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

End Class
