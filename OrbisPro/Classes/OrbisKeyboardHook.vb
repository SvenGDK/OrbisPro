Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class OrbisKeyboardHook

    <DllImport("user32.dll")>
    Public Shared Function SetWindowsHookEx(idHook As Integer, lpfn As CallBack, hMod As IntPtr, dwThreadId As Integer) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function UnhookWindowsHookEx(idHook As IntPtr) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function CallNextHookEx(idHook As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure KBDLLHOOKSTRUCT
        Public vkCode As UInteger
        Public scanCode As UInteger
        Public flags As UInteger
        Public time As UInteger
        Public dwExtraInfo As UInteger
    End Structure

    Private Const WH_KEYBOARD_LL As Integer = 13
    Private Const HC_ACTION As Integer = 0
    Private Const WM_KEYDOWN = 256
    Private Const WM_KEYUP = 257
    Private Const WM_SYSKEYDOWN = 260
    Private Const WM_SYSKEYUP = 261

    Public Delegate Function CallBack(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As Integer
    Private Delegate Function KBDLLHookProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As Integer
    Private HHookID As IntPtr = IntPtr.Zero

    Public Shared Event KeyDown(Key As Keys)
    Public Shared Event KeyUp(Key As Keys)

    Public Sub New()
        Static WindowsHookProc As New CallBack(AddressOf KeyboardProc)
        HHookID = SetWindowsHookEx(WH_KEYBOARD_LL, WindowsHookProc, IntPtr.Zero, 0)

        If HHookID = IntPtr.Zero Then
            Throw New Exception("Keyboard hooking is not available.")
        End If
    End Sub

    Protected Overrides Sub Finalize()
        If Not HHookID = IntPtr.Zero Then
            UnhookWindowsHookEx(HHookID)
        End If
        MyBase.Finalize()
    End Sub

    Private Function KeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As Integer
        Dim Key As Keys
        If nCode = HC_ACTION Then

            Dim struct As KBDLLHOOKSTRUCT

            Select Case wParam
                Case CType(WM_KEYDOWN, IntPtr), CType(WM_SYSKEYDOWN, IntPtr)
                    Key = CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys)
                    RaiseEvent KeyDown(Key)
                Case CType(WM_KEYUP, IntPtr), CType(WM_SYSKEYUP, IntPtr)
                    Key = CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys)
                    RaiseEvent KeyUp(Key)
            End Select
        End If

        Dim ret As Integer = CInt(CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam))
        Return ret
    End Function

End Class
