Imports System.Runtime.InteropServices

Module DevBroadcastInterface

    Public Const WM_DEVICECHANGE = 537
    Public Const DBT_DEVICEARRIVAL = 32768
    Public Const DBT_DEVICEREMOVECOMPLETE = 32772
    Public Const DBT_DEVTYP_DEVICEINTERFACE = 5
    Public Const DBT_DEVTYP_VOLUME = 2
    Public Const DEVICE_NOTIFY_WINDOW_HANDLE = 0

    <StructLayout(LayoutKind.Sequential)>
    Public Class DEV_BROADCAST_DEVICEINTERFACE
        Public dbcc_size As Integer
        Public dbcc_devicetype As Integer
        Public dbcc_reserved As Integer
        Public dbcc_classguid As Guid
        Public dbcc_name As Short
    End Class

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Class DEV_BROADCAST_DEVICEINTERFACE_1
        Public dbcc_size As Integer
        Public dbcc_devicetype As Integer
        Public dbcc_reserved As Integer
        Public dbcc_classguid As Guid
        <MarshalAs(UnmanagedType.ByValArray, sizeconst:=255)>
        Public dbcc_name() As Char
    End Class

    <StructLayout(LayoutKind.Sequential)>
    Public Class DEV_BROADCAST_HDR
        Public dbch_size As Integer
        Public dbch_devicetype As Integer
        Public dbch_reserved As Integer
    End Class

    <StructLayout(LayoutKind.Sequential)>
    Public Class DEV_BROADCAST_VOLUME
        Public dbcv_size As Integer
        Public dbcv_devicetype As Integer
        Public dbcv_reserved As Integer
        Public dbcv_unitmask As Integer
        Public dbcv_flags As Short
    End Class

    Public Declare Auto Function RegisterDeviceNotification Lib "user32.dll" (hRecipient As IntPtr, NotificationFilter As IntPtr, Flags As UInteger) As IntPtr
    Public Declare Function UnregisterDeviceNotification Lib "user32.dll" (Handle As IntPtr) As UInteger

End Module
