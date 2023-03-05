Imports System.Runtime.InteropServices
Imports System.Text

Namespace INI
    ''' <summary>
    ''' Create a New INI file to store or load data
    ''' </summary>
    Public Class IniFile
        Public path As String

        <DllImport("kernel32")>
        Private Shared Function WritePrivateProfileString(section As String, key As String, val As String, filePath As String) As Long
        End Function
        <DllImport("kernel32")>
        Private Shared Function GetPrivateProfileString(section As String, key As String, def As String, retVal As StringBuilder, size As Integer, filePath As String) As Integer
        End Function

        ''' <summary>
        ''' INIFile Constructor.
        ''' </summary>
        ''' <PARAM name="INIPath"></PARAM>
        Public Sub New(INIPath As String)
            path = INIPath
        End Sub

        ''' <summary>
        ''' Write Data to the INI File
        ''' </summary>
        ''' <PARAM name="Section"></PARAM>
        ''' Section name
        ''' <PARAM name="Key"></PARAM>
        ''' Key Name
        ''' <PARAM name="Value"></PARAM>
        ''' Value Name
        Public Sub IniWriteValue(Section As String, Key As String, Value As String)
            WritePrivateProfileString(Section, Key, Value, path)
        End Sub

        ''' <summary>
        ''' Read Data Value From the Ini File
        ''' </summary>
        ''' <PARAM name="Section"></PARAM>
        ''' <PARAM name="Key"></PARAM>
        ''' <PARAM name="Path"></PARAM>
        ''' <returns></returns>
        Public Function IniReadValue(Section As String, Key As String) As String
            Dim temp As New StringBuilder(255)
            Dim i As Integer = GetPrivateProfileString(Section, Key, "", temp, 255, path)
            Return temp.ToString()

        End Function
    End Class
End Namespace