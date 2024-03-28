Imports InTheHand.Net.Bluetooth
Imports InTheHand.Net.Sockets
Imports OrbisPro.OrbisStructures

Public Class OrbisBluetooth

    Public Shared Function DiscoverBTDevices() As List(Of DiscoveredBluetoothDevice)
        Dim BTClient As New BluetoothClient()
        Dim BTDevices As IReadOnlyCollection(Of BluetoothDeviceInfo) = BTClient.DiscoverDevices()
        Dim ListOfDiscoveredBluetoothDevices As New List(Of DiscoveredBluetoothDevice)

        For Each BTDevice As BluetoothDeviceInfo In BTDevices
            Dim NewDiscoveredBluetoothDevice As New DiscoveredBluetoothDevice With {
                .FriendlyName = BTDevice.DeviceName,
                .Address = BTDevice.DeviceAddress,
                .ClassOfDevice = BTDevice.ClassOfDevice.Device,
                .Authenticated = BTDevice.Authenticated,
                .Connected = BTDevice.Connected
            }
            ListOfDiscoveredBluetoothDevices.Add(NewDiscoveredBluetoothDevice)
        Next

        Return ListOfDiscoveredBluetoothDevices
    End Function

    Public Shared Function PairBTDevice(BTDevice As BTDeviceOrServiceListViewItem, Optional PINCode As String = "0000") As Boolean
        If Not BTDevice.IsDevicePaired Then

            Dim BTDeviceInfo As New BluetoothDeviceInfo(BTDevice.DeviceAddress)

            Select Case BTDeviceInfo.ClassOfDevice.Device
                Case DeviceClass.PeripheralGamepad
                    BTDeviceInfo.SetServiceState(BluetoothService.SerialPort, True)
                Case DeviceClass.AudioVideoHeadphones
                    BTDeviceInfo.SetServiceState(BluetoothService.Headset, True)
                Case DeviceClass.PeripheralKeyboard
                    BTDeviceInfo.SetServiceState(BluetoothService.HumanInterfaceDevice, True)
                Case DeviceClass.Smartphone
                    BTDeviceInfo.SetServiceState(BluetoothService.GenericFileTransfer, True)
                Case DeviceClass.AudioVideoDisplayLoudspeaker
                    BTDeviceInfo.SetServiceState(BluetoothService.SerialPort, True)
                Case Else
                    BTDeviceInfo.SetServiceState(BluetoothService.SerialPort, True)
            End Select

            BTDeviceInfo.Refresh()

            If Not PINCode = "0000" Then
                If BluetoothSecurity.PairRequest(BTDevice.DeviceAddress, PINCode) Then
                    Return True
                Else
                    Return False
                End If
            Else
                If BluetoothSecurity.PairRequest(BTDevice.DeviceAddress) Then
                    Return True
                Else
                    Return False
                End If
            End If

        Else
            Return False
        End If
    End Function

    Public Shared Function UnpairBTDevice(BTDevice As BTDeviceOrServiceListViewItem) As Boolean
        If BTDevice.IsDevicePaired Then
            Dim BTDeviceInfo As New BluetoothDeviceInfo(BTDevice.DeviceAddress)
            If BluetoothSecurity.RemoveDevice(BTDevice.DeviceAddress) Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

End Class
