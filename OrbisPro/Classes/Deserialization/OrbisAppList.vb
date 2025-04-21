Public Class OrbisAppList

    Private _Apps As List(Of App)

    Public Property Apps As List(Of App)
        Get
            Return _Apps
        End Get
        Set
            _Apps = Value
        End Set
    End Property

    Public Class App

        Private _Name As String
        Private _Group As String
        Private _IconPath As String
        Private _ExecutablePath As String
        Private _ShowInLibrary As String
        Private _ShowOnHome As String
        Private _BackgroundPath As String
        Private _Platform As String

        Public Property Name As String
            Get
                Return _Name
            End Get
            Set
                _Name = Value
            End Set
        End Property

        Public Property Group As String
            Get
                Return _Group
            End Get
            Set
                _Group = Value
            End Set
        End Property

        Public Property IconPath As String
            Get
                Return _IconPath
            End Get
            Set
                _IconPath = Value
            End Set
        End Property

        Public Property ExecutablePath As String
            Get
                Return _ExecutablePath
            End Get
            Set
                _ExecutablePath = Value
            End Set
        End Property

        Public Property ShowInLibrary As String
            Get
                Return _ShowInLibrary
            End Get
            Set
                _ShowInLibrary = Value
            End Set
        End Property

        Public Property ShowOnHome As String
            Get
                Return _ShowOnHome
            End Get
            Set
                _ShowOnHome = Value
            End Set
        End Property

        Public Property BackgroundPath As String
            Get
                Return _BackgroundPath
            End Get
            Set
                _BackgroundPath = Value
            End Set
        End Property

        Public Property Platform As String
            Get
                Return _Platform
            End Get
            Set
                _Platform = Value
            End Set
        End Property

    End Class

End Class
