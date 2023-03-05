Imports System.ComponentModel

Public Class AppListViewItem

    'Notify on changes
    Implements INotifyPropertyChanged

    Public _AppTitle As String
    Public _AppLaunchPath As String
    Public _AppIcon As BitmapImage
    Public _IsAppSelected As Visibility

    'Properties
    Public Property AppTitle() As String
        Get
            Return _AppTitle
        End Get
        Set(Value As String)
            _AppTitle = Value
            NotifyPropertyChanged("AppTitle")
        End Set
    End Property

    Public Property AppIcon() As BitmapImage
        Get
            Return _AppIcon
        End Get
        Set(Value As BitmapImage)
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

    Public Property FocusVisualStyle As Style

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

End Class
