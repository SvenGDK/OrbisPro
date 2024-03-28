Imports System.ComponentModel

Public Class OpenWindowListViewItem

    Implements INotifyPropertyChanged

    Private _IsItemSelected As Visibility
    Private _ItemIcon As BitmapImage
    Private _ItemName As String
    Private _ItemSubDescription As String
    Private _ItemProcessID As Integer
    Private _ItemProcessHwnd As IntPtr


    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

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

    Public Property ItemProcessID As Integer
        Get
            Return _ItemProcessID
        End Get
        Set
            _ItemProcessID = Value
        End Set
    End Property

    Public Property ItemProcessHwnd As IntPtr
        Get
            Return _ItemProcessHwnd
        End Get
        Set
            _ItemProcessHwnd = Value
        End Set
    End Property

End Class
