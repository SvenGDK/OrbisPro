Imports SharpDX.XInput
Imports System.Threading

Public Class SystemImageViewer

    'Controller input
    Private MainController As Controller
    Private RemoteController As Controller
    Private CTS As New CancellationTokenSource()
    Public PauseInput As Boolean = True

End Class
