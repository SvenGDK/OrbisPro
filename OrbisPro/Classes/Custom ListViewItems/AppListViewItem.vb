Imports System.ComponentModel

Public Class AppListViewItem

    Implements INotifyPropertyChanged

    Public _AppTitle As String
    Public _AppLaunchPath As String
    Public _AppIcon As ImageSource
    Public _IsAppSelected As Visibility
    Private _IsGame As Boolean
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property AppTitle() As String
        Get
            Return _AppTitle
        End Get
        Set(Value As String)
            _AppTitle = Value
            NotifyPropertyChanged("AppTitle")
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

    Public Property IsAppSelected() As Visibility
        Get
            Return _IsAppSelected
        End Get
        Set(Value As Visibility)
            _IsAppSelected = Value
            NotifyPropertyChanged("IsAppSelected")
        End Set
    End Property

    Public Property AppLaunchPath() As String
        Get
            Return _AppLaunchPath
        End Get
        Set(Value As String)
            _AppLaunchPath = Value
            NotifyPropertyChanged("AppLaunchPath")
        End Set
    End Property

    Public Property IsGame As Boolean
        Get
            Return _IsGame
        End Get
        Set(Value As Boolean)
            _IsGame = Value
            NotifyPropertyChanged("IsGame")
        End Set
    End Property

End Class
