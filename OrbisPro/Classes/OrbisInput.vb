Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class OrbisInput

    Private Shared _SharedController1 As Controller = Nothing
    Private Shared _SharedController2 As Controller = Nothing
    Private Shared _SharedController3 As Controller = Nothing
    Private Shared _SharedController4 As Controller = Nothing
    Private Shared _SharedController1PollingRate As Integer = 10
    Private Shared _SharedController2PollingRate As Integer = 10
    Private Shared _SharedController3PollingRate As Integer = 10
    Private Shared _SharedController4PollingRate As Integer = 10

    'Gamepad variables
    Public Shared Property SharedController1 As Controller
        Get
            Return _SharedController1
        End Get
        Set(Value As Controller)
            _SharedController1 = Value
        End Set
    End Property

    Public Shared Property SharedController2 As Controller
        Get
            Return _SharedController2
        End Get
        Set(Value As Controller)
            _SharedController2 = Value
        End Set
    End Property

    Public Shared Property SharedController3 As Controller
        Get
            Return _SharedController3
        End Get
        Set(Value As Controller)
            _SharedController3 = Value
        End Set
    End Property

    Public Shared Property SharedController4 As Controller
        Get
            Return _SharedController4
        End Get
        Set(Value As Controller)
            _SharedController4 = Value
        End Set
    End Property

    Public Shared Property SharedController1PollingRate As Integer
        Get
            Return _SharedController1PollingRate
        End Get
        Set(Value As Integer)
            _SharedController1PollingRate = Value
        End Set
    End Property

    Public Shared Property SharedController2PollingRate As Integer
        Get
            Return _SharedController2PollingRate
        End Get
        Set(Value As Integer)
            _SharedController2PollingRate = Value
        End Set
    End Property

    Public Shared Property SharedController3PollingRate As Integer
        Get
            Return _SharedController3PollingRate
        End Get
        Set(Value As Integer)
            _SharedController3PollingRate = Value
        End Set
    End Property

    Public Shared Property SharedController4PollingRate As Integer
        Get
            Return _SharedController4PollingRate
        End Get
        Set(Value As Integer)
            _SharedController4PollingRate = Value
        End Set
    End Property

    Public Shared Function GetAndSetGamepads() As Boolean
        'Assign gamepads
        SharedController1 = New Controller(UserIndex.One)
        SharedController2 = New Controller(UserIndex.Two)
        SharedController3 = New Controller(UserIndex.Three)
        SharedController4 = New Controller(UserIndex.Four)

        'Check if gamepad is connected
        If Not SharedController1.IsConnected Then
            SharedController1 = Nothing
            Return False 'If no gamepad is connected -> Return False
        End If
        If Not SharedController2.IsConnected Then
            SharedController2 = Nothing
        End If
        If Not SharedController3.IsConnected Then
            SharedController3 = Nothing
        End If
        If Not SharedController4.IsConnected Then
            SharedController4 = Nothing
        End If

        'Set polling rates
        If Not String.IsNullOrEmpty(MainConfigFile.IniReadValue("Gamepads", "Gamepad1PollingRate")) Then
            SharedController1PollingRate = CInt(MainConfigFile.IniReadValue("Gamepads", "Gamepad1PollingRate"))
        End If
        If Not String.IsNullOrEmpty(MainConfigFile.IniReadValue("Gamepads", "Gamepad2PollingRate")) Then
            SharedController2PollingRate = CInt(MainConfigFile.IniReadValue("Gamepads", "Gamepad2PollingRate"))
        End If

        Return True
    End Function

End Class
