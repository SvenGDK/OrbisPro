Imports System.ComponentModel

Public Class SettingsListViewItem

    'Notify on changes
    Implements INotifyPropertyChanged

    Private _SettingsTitle As String
    Private _SettingsIcon As BitmapImage
    Private _IsSettingSelected As Visibility = Visibility.Hidden
    Private _IsSettingChecked As Boolean
    Private _SettingsState As String
    Private _SettingsDescription As String
    Private _DescriptionVisibility As Visibility

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    'Properties
    Public Property SettingsTitle As String
        Get
            Return _SettingsTitle
        End Get
        Set
            _SettingsTitle = Value
            NotifyPropertyChanged("SettingsTitle")
        End Set
    End Property

    Public Property SettingsIcon As BitmapImage
        Get
            Return _SettingsIcon
        End Get
        Set
            _SettingsIcon = Value
            NotifyPropertyChanged("SettingsIcon")
        End Set
    End Property

    Public Property IsSettingSelected As Visibility
        Get
            Return _IsSettingSelected
        End Get
        Set
            _IsSettingSelected = Value
            NotifyPropertyChanged("IsSettingSelected")
        End Set
    End Property

    Public Property IsSettingChecked As Boolean
        Get
            Return _IsSettingChecked
        End Get
        Set
            _IsSettingChecked = Value
            NotifyPropertyChanged("IsSettingChecked")
        End Set
    End Property

    Public Property SettingsState As String
        Get
            Return _SettingsState
        End Get
        Set
            _SettingsState = Value
            NotifyPropertyChanged("SettingsState")
        End Set
    End Property

    Public Property SettingsDescription As String
        Get
            Return _SettingsDescription
        End Get
        Set
            _SettingsDescription = Value
            NotifyPropertyChanged("SettingsDescription")
        End Set
    End Property

    Public Property DescriptionVisibility As Visibility
        Get
            Return _DescriptionVisibility
        End Get
        Set
            _DescriptionVisibility = Value
            NotifyPropertyChanged("DescriptionVisibility")
        End Set
    End Property

End Class
