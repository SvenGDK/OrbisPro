Imports System.IO
Imports Newtonsoft.Json
Imports OrbisPro.OrbisUtils

Public Class OrbisFolders

    Private _Folders As List(Of Folder)

    Public Property Folders As List(Of Folder)
        Get
            Return _Folders
        End Get
        Set
            _Folders = Value
        End Set
    End Property

    Public Class Folder
        Private _Name As String

        Public Property Name As String
            Get
                Return _Name
            End Get
            Set
                _Name = Value
            End Set
        End Property
    End Class

    Public Shared Function CreateNewFolder(GroupName As String) As Boolean
        If File.Exists(FoldersPath) Then
            Dim FoldersListJSON As String = File.ReadAllText(FoldersPath)
            Dim FoldersList As OrbisFolders = JsonConvert.DeserializeObject(Of OrbisFolders)(FoldersListJSON)
            Dim NewFolder As New Folder() With {.Name = GroupName}

            'Return false if folder already exists
            For Each SavedFolder As Folder In FoldersList.Folders
                If SavedFolder.Name = GroupName Then
                    Return False
                    Exit For
                End If
            Next

            'Add selected folder to the FoldersListJSON
            FoldersList.Folders.Add(NewFolder)

            'Save
            Dim NewFoldersListJSON As String = JsonConvert.SerializeObject(FoldersList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
            File.WriteAllText(FoldersPath, NewFoldersListJSON)

            Return True
        Else
            Return False
        End If
    End Function

    Public Shared Sub RemoveFolder(GroupName As String)
        If File.Exists(FoldersPath) Then
            Dim FoldersListJSON As String = File.ReadAllText(FoldersPath)
            Dim FoldersList As OrbisFolders = JsonConvert.DeserializeObject(Of OrbisFolders)(FoldersListJSON)

            'Remove the folder from the FoldersListJSON
            For Each SavedFolder As Folder In FoldersList.Folders
                If SavedFolder.Name = GroupName Then
                    FoldersList.Folders.Remove(SavedFolder)
                    Exit For
                End If
            Next

            'Save
            Dim NewFoldersListJSON As String = JsonConvert.SerializeObject(FoldersList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
            File.WriteAllText(FoldersPath, NewFoldersListJSON)
        End If
    End Sub

    Public Shared Sub ChangeFolderNameOfAppGame(NewGroupName As String, AppGameTitle As String)
        If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

            If AppsList IsNot Nothing AndAlso GamesList IsNot Nothing Then
                'Set the group name for the selected app/game
                For Each RegisteredApp In AppsList.Apps()
                    If RegisteredApp.Name = AppGameTitle Then
                        RegisteredApp.Group = NewGroupName
                        Exit For
                    End If
                Next
                For Each RegisteredGame In GamesList.Games()
                    If RegisteredGame.Name = AppGameTitle Then
                        RegisteredGame.Group = NewGroupName
                        Exit For
                    End If
                Next
            End If

            'Save
            Dim NewAppsListJSON As String = JsonConvert.SerializeObject(AppsList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
            File.WriteAllText(AppLibraryPath, NewAppsListJSON)
            Dim NewGamesListJSON As String = JsonConvert.SerializeObject(GamesList, Formatting.Indented, New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore})
            File.WriteAllText(GameLibraryPath, NewGamesListJSON)
        End If
    End Sub

    Public Shared Function GetFolderContentNames(GroupName As String) As List(Of String)
        Dim ListOfMatches As New List(Of String)()

        If File.Exists(AppLibraryPath) AndAlso File.Exists(GameLibraryPath) Then
            Dim AppsListJSON As String = File.ReadAllText(AppLibraryPath)
            Dim AppsList As OrbisAppList = JsonConvert.DeserializeObject(Of OrbisAppList)(AppsListJSON)
            Dim GamesListJSON As String = File.ReadAllText(GameLibraryPath)
            Dim GamesList As OrbisGamesList = JsonConvert.DeserializeObject(Of OrbisGamesList)(GamesListJSON)

            If AppsList IsNot Nothing AndAlso GamesList IsNot Nothing Then
                For Each RegisteredApp In AppsList.Apps()
                    If RegisteredApp.Group = GroupName Then
                        ListOfMatches.Add(RegisteredApp.Name)
                    End If
                Next
                For Each RegisteredGame In GamesList.Games()
                    If RegisteredGame.Group = GroupName Then
                        ListOfMatches.Add(RegisteredGame.Name)
                    End If
                Next
            Else
                Return ListOfMatches
            End If

            Return ListOfMatches
        Else
            Return ListOfMatches
        End If
    End Function

End Class
