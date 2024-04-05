Imports System.Drawing
Imports System.Runtime.InteropServices

Public Class OrbisDisplay

    <DllImport("gdi32.dll")>
    Private Shared Function GetDeviceCaps(hDC As IntPtr, nIndex As Integer) As Integer
    End Function

    Public Shared Function GetMonitorFrequency() As Integer
        Dim Graphs As Graphics = Graphics.FromHwnd(IntPtr.Zero)
        Dim Desk As IntPtr = Graphs.GetHdc()
        Dim RefreshRate As Integer = GetDeviceCaps(Desk, 116)

        Return RefreshRate
    End Function

    Public Shared Sub SetScaling(WindowToScale As Window, CanvasToScale As Canvas, Optional AutoScaling As Boolean = True, Optional NewWidth As Double = 1920, Optional NewHeight As Double = 1080)
        Dim SystemPrimaryScreenWidth As Integer
        Dim SystemPrimaryScreenHeight As Integer

        'Set the size to the PrimaryScreenWidth & PrimaryScreenHeight
        If AutoScaling Then
            SystemPrimaryScreenWidth = CInt(SystemParameters.PrimaryScreenWidth)
            SystemPrimaryScreenHeight = CInt(SystemParameters.PrimaryScreenHeight)

            WindowToScale.Width = SystemParameters.PrimaryScreenWidth
            WindowToScale.Height = SystemParameters.PrimaryScreenHeight
        Else
            SystemPrimaryScreenWidth = CInt(NewWidth)
            SystemPrimaryScreenHeight = CInt(NewHeight)

            WindowToScale.Width = NewWidth
            WindowToScale.Height = NewHeight
        End If

        'Scale the canvas
        CanvasToScale.LayoutTransform = New ScaleTransform(SystemPrimaryScreenWidth / 1920, SystemPrimaryScreenHeight / 1080)
    End Sub

End Class
