Imports OrbisPro.OrbisStructures
Imports OrbisPro.OrbisUtils
Imports SharpDX.XInput

Public Class OrbisInput

    'Gamepad variables
    Public Shared SharedController1 As Controller = Nothing
    Public Shared SharedController2 As Controller = Nothing
    Public Shared SharedController3 As Controller = Nothing
    Public Shared SharedController4 As Controller = Nothing

    Public Shared SharedController1PollingRate As Integer = 0
    Public Shared SharedController2PollingRate As Integer = 0
    Public Shared SharedController3PollingRate As Integer = 0
    Public Shared SharedController4PollingRate As Integer = 0

    'Currently unused
    Public Shared SharedController1Properties As GamepadInfo
    Public Shared SharedController2Properties As GamepadInfo
    Public Shared SharedController3Properties As GamepadInfo
    Public Shared SharedController4Properties As GamepadInfo

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

        'Adjust gamepad polling rates
        Dim MonitorFrequency As Integer = GetMonitorFrequency()
        If MonitorFrequency = 144 Then
            SharedController1PollingRate = 25
            SharedController2PollingRate = 25
            SharedController3PollingRate = 25
            SharedController4PollingRate = 25
        ElseIf MonitorFrequency = 120 Then
            SharedController1PollingRate = 27
            SharedController2PollingRate = 27
            SharedController3PollingRate = 27
            SharedController4PollingRate = 27
        ElseIf MonitorFrequency <= 60 Then
            SharedController1PollingRate = 50
            SharedController2PollingRate = 50
            SharedController3PollingRate = 50
            SharedController4PollingRate = 50
        End If

        Return True

    End Function

    'Currently unused
    Public Shared Sub SetGamepadProperties()

        Dim DInput As New SharpDX.DirectInput.DirectInput()

        For Each deviceInstance In DInput.GetDevices()

        Next

        'Using searcher As New ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass LIKE 'XnaComposite'")
        '    Using collection As ManagementObjectCollection = searcher.Get()
        '        For Each device As ManagementObject In collection
        '            MsgBox(device.GetPropertyValue("DeviceID"))
        '            MsgBox(device.GetPropertyValue("Description"))
        '        Next
        '    End Using
        'End Using
    End Sub

End Class
