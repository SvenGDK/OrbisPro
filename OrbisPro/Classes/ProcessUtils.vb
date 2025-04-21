Imports System.Runtime.InteropServices

Public Class ProcessUtils

    Public Shared WithEvents ActiveProcess As Process
    Private Shared _SuspendedThreads As List(Of IntPtr)

    Public Shared Property SuspendedThreads As List(Of IntPtr)
        Get
            Return _SuspendedThreads
        End Get
        Set(Value As List(Of IntPtr))
            _SuspendedThreads = Value
        End Set
    End Property

    Private Declare Function SuspendThread Lib "kernel32.dll" (hThread As IntPtr) As UInteger
    Private Declare Function ResumeThread Lib "kernel32.dll" (hThread As IntPtr) As UInteger
    Private Declare Function OpenThread Lib "kernel32.dll" (dwDesiredAccess As ThreadAccess, bInheritHandle As Boolean, dwThreadId As UInteger) As IntPtr

    Private Declare Function SetForegroundWindow Lib "user32.dll" (hWnd As IntPtr) As Boolean
    Private Declare Function ShowWindow Lib "user32.dll" (handle As IntPtr, nCmdShow As Integer) As Boolean
    Private Declare Function GetActiveWindow Lib "user32.dll" Alias "GetActiveWindow" () As IntPtr
    Private Declare Function GetForegroundWindow Lib "user32.dll" () As IntPtr
    Private Declare Function BringWindowToTop Lib "user32.dll" (hwnd As IntPtr) As Boolean
    Private Declare Function IsIconic Lib "user32.dll" (handle As IntPtr) As Boolean

    <DllImport("KERNEL32.DLL", EntryPoint:="SetProcessWorkingSetSize", SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
    Friend Shared Function SetProcessWorkingSetSize(pProcess As IntPtr, dwMinimumWorkingSetSize As Integer, dwMaximumWorkingSetSize As Integer) As Boolean
    End Function

    <DllImport("KERNEL32.DLL", EntryPoint:="GetCurrentProcess", SetLastError:=True, CallingConvention:=CallingConvention.StdCall)>
    Friend Shared Function GetCurrentProcess() As IntPtr
    End Function

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
        If ProcessesByName.Length > 0 Then
            Dim MainProcess As Process = ProcessesByName(0)
            Dim NewHandle As IntPtr = MainProcess.MainWindowHandle
            If NewHandle <> IntPtr.Zero Then
                If ShowWindow(NewHandle, 9) Then Debug.WriteLine("Sucess ShowWindow - 9 (Restore)")
                If SetForegroundWindow(NewHandle) Then Debug.WriteLine("Success SetForegroundWindow")
            End If
        End If
    End Sub

    'Suspend all process threads
    Public Shared Sub PauseProcessThreads(ActiveProcessName As String)
        'Get all associated processes for the ActiveProcessName
        Dim AllProcesses As List(Of Process) = Process.GetProcessesByName(ActiveProcessName).ToList()
        If AllProcesses.Count > 0 Then

            SuspendedThreads = New List(Of IntPtr)

            For Each FoundProcesses As Process In AllProcesses
                For Each ProcThread As ProcessThread In FoundProcesses.Threads
                    Dim OpenProcessThread As IntPtr = OpenThread(ThreadAccess.SUSPEND_RESUME, False, CType(ProcThread.Id, UInteger))
                    If OpenProcessThread = IntPtr.Zero Then
                        Continue For
                    Else
                        SuspendedThreads.Add(OpenProcessThread)
                        Dim SuspendThreadResult As UInteger = SuspendThread(OpenProcessThread)
                        Debug.WriteLine($"SuspendThreadResult: {SuspendThreadResult}")
                    End If
                Next
            Next
        Else
            Debug.WriteLine("No active process found - " + ActiveProcessName)
        End If
    End Sub

    'Resume all suspended process threads
    Public Shared Sub ResumeProcessThreads()
        If SuspendedThreads IsNot Nothing Then
            For Each SuspendedThread In SuspendedThreads
                If SuspendedThread <> IntPtr.Zero Then
                    Dim ResumeThreadResult As UInteger = ResumeThread(SuspendedThread)
                End If
            Next
        End If
    End Sub

    Private Shared Sub ActiveProcess_Exited(sender As Object, e As EventArgs) Handles ActiveProcess.Exited
        ActiveProcess = Nothing

        'Clear SuspendedThreads if game was force closed in a suspended state
        SuspendedThreads?.Clear()

        'Restore MainWindow and clear StartedGameExecutable
        System.Windows.Application.Current.Dispatcher.BeginInvoke(Sub()
                                                                      For Each Win In System.Windows.Application.Current.Windows()
                                                                          If Win.ToString = "OrbisPro.MainWindow" Then
                                                                              CType(Win, MainWindow).StartedGameExecutable = String.Empty
                                                                              CType(Win, MainWindow).WindowState = WindowState.Normal
                                                                              CType(Win, MainWindow).Topmost = True
                                                                              CType(Win, MainWindow).ReturnAnimation()
                                                                              CType(Win, MainWindow).Activate()
                                                                              CType(Win, MainWindow).Topmost = False
                                                                              Exit For
                                                                          End If
                                                                      Next
                                                                  End Sub)

    End Sub

End Class
