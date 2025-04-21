Imports System.ComponentModel
Imports System.IO

Public Class DriveListViewItem

    Implements INotifyPropertyChanged

    Private _DriveName As String
    Private _DriveTotalSize As Long
    Private _DriveLeftSize As Long
    Private _IsDriveSelected As Visibility
    Private _UsedForScanning As Boolean
    Private _DriveType As DriveType
    Private _DriveFormat As String
    Private _DriveTotalFreeSpace As Long
    Private _DriveVolumeLabel As String
    Private _DriveSizeText As String
    Private _DriveFullNameText As String
    Private _DriveIcon As ImageSource
    Private _DriveSelectionBorderBrush As Brush = Brushes.White
    Private _DriveUsedSpace As Double
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(propName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
    End Sub

    Public Property DriveName As String
        Get
            Return _DriveName
        End Get
        Set
            _DriveName = Value
            NotifyPropertyChanged("DriveName")
        End Set
    End Property

    Public Property DriveTotalSize As Long
        Get
            Return _DriveTotalSize
        End Get
        Set
            _DriveTotalSize = Value
            NotifyPropertyChanged("DriveTotalSize")
        End Set
    End Property

    Public Property DriveLeftSize As Long
        Get
            Return _DriveLeftSize
        End Get
        Set
            _DriveLeftSize = Value
            NotifyPropertyChanged("DriveLeftSize")
        End Set
    End Property

    Public Property IsDriveSelected As Visibility
        Get
            Return _IsDriveSelected
        End Get
        Set
            _IsDriveSelected = Value
            NotifyPropertyChanged("IsDriveSelected")
        End Set
    End Property

    Public Property UsedForScanning As Boolean
        Get
            Return _UsedForScanning
        End Get
        Set
            _UsedForScanning = Value
            NotifyPropertyChanged("UsedForScanning")
        End Set
    End Property

    Public Property DriveType As DriveType
        Get
            Return _DriveType
        End Get
        Set
            _DriveType = Value
            NotifyPropertyChanged("DriveType")
        End Set
    End Property

    Public Property DriveFormat As String
        Get
            Return _DriveFormat
        End Get
        Set
            _DriveFormat = Value
            NotifyPropertyChanged("DriveFormat")
        End Set
    End Property

    Public Property DriveTotalFreeSpace As Long
        Get
            Return _DriveTotalFreeSpace
        End Get
        Set
            _DriveTotalFreeSpace = Value
            NotifyPropertyChanged("DriveTotalFreeSpace")
        End Set
    End Property

    Public Property DriveVolumeLabel As String
        Get
            Return _DriveVolumeLabel
        End Get
        Set
            _DriveVolumeLabel = Value
            NotifyPropertyChanged("DriveVolumeLabel")
        End Set
    End Property

    Public Property DriveSizeText As String
        Get
            Return _DriveSizeText
        End Get
        Set
            _DriveSizeText = Value
            NotifyPropertyChanged("DriveSizeText")
        End Set
    End Property

    Public Property DriveFullNameText As String
        Get
            Return _DriveFullNameText
        End Get
        Set
            _DriveFullNameText = Value
            NotifyPropertyChanged("DriveFullNameText")
        End Set
    End Property

    Public Property DriveIcon As ImageSource
        Get
            Return _DriveIcon
        End Get
        Set
            _DriveIcon = Value
            NotifyPropertyChanged("DriveIcon")
        End Set
    End Property

    Public Property DriveSelectionBorderBrush As Brush
        Get
            Return _DriveSelectionBorderBrush
        End Get
        Set
            _DriveSelectionBorderBrush = Value
            NotifyPropertyChanged("DriveSelectionBorderBrush")
        End Set
    End Property

    Public Property DriveUsedSpace As Double
        Get
            Return _DriveUsedSpace
        End Get
        Set
            _DriveUsedSpace = Value
            NotifyPropertyChanged("DriveUsedSpace")
        End Set
    End Property

End Class
