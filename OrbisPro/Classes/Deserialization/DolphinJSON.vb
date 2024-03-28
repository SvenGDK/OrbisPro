Public Class DolphinJSON

    Private _country As String
    Private _game_id As String
    Private _internal_name As String
    Private _region As String
    Private _revision As Integer

    Public Property country As String
        Get
            Return _country
        End Get
        Set
            _country = Value
        End Set
    End Property

    Public Property game_id As String
        Get
            Return _game_id
        End Get
        Set
            _game_id = Value
        End Set
    End Property

    Public Property internal_name As String
        Get
            Return _internal_name
        End Get
        Set
            _internal_name = Value
        End Set
    End Property

    Public Property region As String
        Get
            Return _region
        End Get
        Set
            _region = Value
        End Set
    End Property

    Public Property revision As Integer
        Get
            Return _revision
        End Get
        Set
            _revision = Value
        End Set
    End Property

End Class
