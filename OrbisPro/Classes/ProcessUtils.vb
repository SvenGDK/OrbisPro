Public Class ProcessUtils

    Public Shared SuspendedThreads As New List(Of IntPtr)()
    Declare Function SuspendThread Lib "kernel32.dll" (hThread As IntPtr) As UInteger
    Declare Function ResumeThread Lib "kernel32.dll" (hThread As IntPtr) As UInteger
    Declare Function OpenThread Lib "kernel32.dll" (dwDesiredAccess As ThreadAccess, bInheritHandle As Boolean, dwThreadId As UInteger) As IntPtr

    Declare Function SetForegroundWindow Lib "user32.dll" (hWnd As IntPtr) As Boolean
    Declare Function ShowWindow Lib "user32.dll" (handle As IntPtr, nCmdShow As Integer) As Boolean
    Declare Function GetActiveWindow Lib "user32.dll" Alias "GetActiveWindow" () As IntPtr
    Declare Function GetForegroundWindow Lib "user32.dll" () As IntPtr
    Declare Function GetWindowText Lib "user32.dll" Alias "GetWindowTextA" (hwnd As IntPtr, lpString As Text.StringBuilder, cch As Integer) As Integer
    Declare Function BringWindowToTop Lib "user32.dll" (hwnd As IntPtr) As Boolean
    Declare Function IsIconic Lib "user32.dll" (handle As IntPtr) As Boolean

    Public Enum ThreadAccess As Integer
        TERMINATE = 1
        SUSPEND_RESUME = 2
        GET_CONTEXT = 8
        SET_CONTEXT = 16
        SET_INFORMATION = 32
        QUERY_INFORMATION = 64
        SET_THREAD_TOKEN = 128
        IMPERSONATE = 256
        DIRECT_IMPERSONATION = 512
    End Enum

    'Switch to another process window
    Public Shared Sub ShowProcess(ActiveProcessName As String)
        Dim ProcessesByName As Process() = Process.GetProcessesByName(ActiveProcessName)
        If ProcessesByName.Count > 0 Then
            Dim MainProcess As Process = ProcessesByName(0)
            Dim NewHandle As IntPtr = MainProcess.MainWindowHandle
            If NewHandle <> IntPtr.Zero Then

                'Used for debugging
                Dim ForegroundWindow As IntPtr = GetActiveWindow()
                If ForegroundWindow <> IntPtr.Zero Then
                    Dim Caption As New Text.StringBuilder(256)
                    GetWindowText(ForegroundWindow, Caption, Caption.Capacity)
                    Console.WriteLine("Window Title (GetActiveWindow) : " + Caption.ToString())
                Else
                    ForegroundWindow = GetForegroundWindow()
                    If ForegroundWindow <> IntPtr.Zero Then
                        Dim Caption As New Text.StringBuilder(256)
                        GetWindowText(ForegroundWindow, Caption, Caption.Capacity)
                        Console.WriteLine("Window Title (GetForegroundWindow) : " + Caption.ToString())
                    End If
                End If

                If ShowWindow(NewHandle, 9) Then Console.WriteLine("Sucess ShowWindow - 9 (Restore)")
                If SetForegroundWindow(NewHandle) Then Console.WriteLine("Success SetForegroundWindow")

                'Used for debugging
                Dim NewForegroundWindow As IntPtr = GetForegroundWindow()
                If NewForegroundWindow <> IntPtr.Zero Then
                    Dim Caption As New Text.StringBuilder(256)
                    GetWindowText(NewForegroundWindow, Caption, Caption.Capacity)
                    Console.WriteLine("Window Title (GetActiveWindow) : " + Caption.ToString())
                Else
                    NewForegroundWindow = GetForegroundWindow()
                    If NewForegroundWindow <> IntPtr.Zero Then
                        Dim Caption As New Text.StringBuilder(256)
                        GetWindowText(NewForegroundWindow, Caption, Caption.Capacity)
                        Console.WriteLine("Window Title (GetForegroundWindow) : " + Caption.ToString())
                    End If
                End If

            End If
        Else
            Console.WriteLine("No active process found - " + ActiveProcessName)
        End If
    End Sub

    'Suspend all process threads
    Public Shared Sub PauseProcessThread(ActiveProcessName As String)
        Dim ProcessByName As Process() = Process.GetProcessesByName(ActiveProcessName)
        If ProcessByName.Count > 0 Then
            Dim FoundProcess As Process = ProcessByName(0)
            Dim ProcessMainWindowHandle As IntPtr = FoundProcess.MainWindowHandle

            For Each ProcThread As ProcessThread In FoundProcess.Threads
                Dim OpenProcessThread As IntPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, False, CType(ProcThread.Id, UInteger))

                If OpenProcessThread = IntPtr.Zero Then
                    Exit For
                End If

                SuspendedThreads.Add(OpenProcessThread)
                SuspendThread(OpenProcessThread)
            Next
        Else
            Console.WriteLine("No active process found - " + ActiveProcessName)
        End If
    End Sub

    'Resume all suspended process threads
    Public Shared Sub ResumeProcessThreads()
        If SuspendedThreads IsNot Nothing Then
            For Each SuspendedThread In SuspendedThreads
                If SuspendedThread <> IntPtr.Zero Then
                    ResumeThread(SuspendedThread)
                End If
            Next
        End If
    End Sub

End Class
