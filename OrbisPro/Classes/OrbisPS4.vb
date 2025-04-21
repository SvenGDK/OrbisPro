Public Class OrbisPS4

    Public Class Info
        Private _time As Double
        Private _pages As Integer
        Private _issues As Integer

        Public Property issues As Integer
            Get
                Return _issues
            End Get
            Set
                _issues = Value
            End Set
        End Property

        Public Property pages As Integer
            Get
                Return _pages
            End Get
            Set
                _pages = Value
            End Set
        End Property

        Public Property time As Double
            Get
                Return _time
            End Get
            Set
                _time = Value
            End Set
        End Property
    End Class

    Public Class Game
        Private _image As Boolean
        Private _region As String
        Private _updatedDate As String
        Private _version As String
        Private _os As String
        Private _status As String
        Private _type As String
        Private _code As String
        Private _title As String
        Private _id As Integer

        Public Property id As Integer
            Get
                Return _id
            End Get
            Set
                _id = Value
            End Set
        End Property

        Public Property title As String
            Get
                Return _title
            End Get
            Set
                _title = Value
            End Set
        End Property

        Public Property code As String
            Get
                Return _code
            End Get
            Set
                _code = Value
            End Set
        End Property

        Public Property type As String
            Get
                Return _type
            End Get
            Set
                _type = Value
            End Set
        End Property

        Public Property status As String
            Get
                Return _status
            End Get
            Set
                _status = Value
            End Set
        End Property

        Public Property os As String
            Get
                Return _os
            End Get
            Set
                _os = Value
            End Set
        End Property

        Public Property version As String
            Get
                Return _version
            End Get
            Set
                _version = Value
            End Set
        End Property

        Public Property updatedDate As String
            Get
                Return _updatedDate
            End Get
            Set
                _updatedDate = Value
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

        Public Property image As Boolean
            Get
                Return _image
            End Get
            Set
                _image = Value
            End Set
        End Property
    End Class

    Public Class StatWin
        Private _total As Integer
        Private _count As Integer
        Private _percent As Double
        Private _tag As String

        Public Property tag As String
            Get
                Return _tag
            End Get
            Set
                _tag = Value
            End Set
        End Property

        Public Property percent As Double
            Get
                Return _percent
            End Get
            Set
                _percent = Value
            End Set
        End Property

        Public Property count As Integer
            Get
                Return _count
            End Get
            Set
                _count = Value
            End Set
        End Property

        Public Property total As Integer
            Get
                Return _total
            End Get
            Set
                _total = Value
            End Set
        End Property
    End Class

    Public Class StatLinux
        Private _total As Integer
        Private _count As Integer
        Private _percent As Double
        Private _tag As String

        Public Property tag As String
            Get
                Return _tag
            End Get
            Set
                _tag = Value
            End Set
        End Property

        Public Property percent As Double
            Get
                Return _percent
            End Get
            Set
                _percent = Value
            End Set
        End Property

        Public Property count As Integer
            Get
                Return _count
            End Get
            Set
                _count = Value
            End Set
        End Property

        Public Property total As Integer
            Get
                Return _total
            End Get
            Set
                _total = Value
            End Set
        End Property
    End Class

    Public Class StatMac
        Private _total As Integer
        Private _count As Integer
        Private _percent As Double
        Private _tag As String

        Public Property tag As String
            Get
                Return _tag
            End Get
            Set
                _tag = Value
            End Set
        End Property

        Public Property percent As Double
            Get
                Return _percent
            End Get
            Set
                _percent = Value
            End Set
        End Property

        Public Property count As Integer
            Get
                Return _count
            End Get
            Set
                _count = Value
            End Set
        End Property

        Public Property total As Integer
            Get
                Return _total
            End Get
            Set
                _total = Value
            End Set
        End Property
    End Class

    Public Class ShadPS4Compatibility
        Private _statMac As StatMac()
        Private _statLinux As StatLinux()
        Private _statWin As StatWin()
        Private _games As List(Of Game)
        Private _info As Info

        Public Property info As Info
            Get
                Return _info
            End Get
            Set
                _info = Value
            End Set
        End Property

        Public Property games As List(Of Game)
            Get
                Return _games
            End Get
            Set
                _games = Value
            End Set
        End Property

        Public Property statWin As StatWin()
            Get
                Return _statWin
            End Get
            Set
                _statWin = Value
            End Set
        End Property

        Public Property statLinux As StatLinux()
            Get
                Return _statLinux
            End Get
            Set
                _statLinux = Value
            End Set
        End Property

        Public Property statMac As StatMac()
            Get
                Return _statMac
            End Get
            Set
                _statMac = Value
            End Set
        End Property
    End Class

End Class
