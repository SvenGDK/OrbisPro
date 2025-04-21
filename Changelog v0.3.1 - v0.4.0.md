# Update v0.4.0

# Feature Updates
- Setup now allows to choose more drives to be scanned for games & applications
- Added PS4 Emulator from shadPS4
- The Home menu now also loads saved PS4 games
- New Options menu for the Home screen
  - Add the selected app or game to a folder
  - Delete a selected folder
- Saved Apps, Games & Folders are now stored in a JSON formatted file
- New Gamepad Settings
  - Adjust the polling rate of gamepad 1-4 (only 1 is currently working atm)
  - "Test gamepad Input" option
  - Set the gamepad button layout
- The File Explorer now marks PS4 backups with a shadPS4 logo
- The File Explorer can now open images with the new System Image Viewer
  - The System Image Viewer can currently only rotate the image & make it fullscreen (and back)
- The File Explorer now allows to run backups with a specific emulator when opening the OPTIONS menu
- The File Explorer does now have an option to install PS4 PKG files into the shadps4\games directory
  - The install directory cannot be changed yet and is fixed in OrbisPro
- OrbisPro now loads saved folders on the Home screen
  - A folder can be created/deleted the same way as on the PS4 console
- Adde missing keyboard keys when using a keyboard only
- New PKG installer for PS4 games
  - The PKG installer checks game compatibility and returns the current working state of the game in a dialog
    - You can accept the message and install the PKG or cancel the action
- New Gamepad Input Tester
  - Shows pressed button, Left/Right Thumb Y/X values, Left/Right Trigger values

# Additions
- Added 12 new game icons & 12 new game backgrounds
- Added 2 new animated backgrounds for OrbisPro (Blue Bokeh Dust & Golden Dust)
- Added new button layouts
  - PS3
  - PS4
  - PS5
  - PS Vita
  - Steam (Newer Xbox style)
  - Steam Deck
  - Xbox 360
- Added a black keyboard layout

# Fixes
- Keyboard keys were still accepted in some windows when input was paused
  - Gamepads did not have this issue
- Missing gamepad buttons in some windows
- OrbisPro did still accept all kind of input when a Game/App was started from the File Explorer or Game Library
  - ReduceUsage() was missing too and has been added now
- Returning back to Home from the Game Library could break the focus on the main window
- The Library instantly deleted a Game/App without confirmation
  - A new confirmation dialog now appears to confirm the deletion
- Missing move sound in the Library
- Input was not paused when an exception message was raised
- Returning back to Home from the Web Browser could break the focus on the main window
- No more input possible when ReloadHome() was used
- Settings with checkboxes did save before visually checking/unchecking, this led to saving the previous state
- Fixed a crash that made the FileExplorer freezing when trying to open the first selected folder
- Fixed completing the Setup when hitting a specific key

# Internal Changes
- New "DirveListViewItem" class used for drives listing
- New "ExistingFolderListViewItem", "FolderContentListViewItem", "FolderContentSelectionListViewItem" classes used for folders
- New "OrbisAppList" & "OrbisGamesList" classes to handle new JSON formatted files
- New "OrbisFolders" class to handle folders
  - "CreateNewFolder", "RemoveFolder", "ChangeFolderNameOfAppGame" & "GetFolderContentNames" functions
- Check file extensions with ".ToLower()"
- GameStarter -> StartGame
  - Added option to start the game with a selected emulator
  - Added a Platform property to start ".bin", ".elf" & ".iso" files with the correct emulator
  - Added possibility to start .bin & .elf files with selected emulator
- New "OrbisPS4" class that currently handles shadPS4 compatibility
- Changed "AppPath" to "AppExecutableFilePath" in "OrbisStructures.AppDetails"
- Moved some functions to "OrbisUtils"
- "OrbisUtils" class changes :
  - New "CreateFolderImage" function that creates folder icons
    - Selects the first 4 available game/app icons from a group
  - New "GetFolderItemsCount" function to get the amount of folder items
  - New "GetFolderItems" function that returns the items for a given folder
- New "CreateNewFolderDialog", "ExistingFolders" & "SelectFolderContentDialog" windows
  - "ExistingFolders" window lists the option to create a new folder and already existing folders
  - "CreateNewFolderDialog" shows an overwiew of the folder, here you can set the folder name an content
  - "SelectFolderContentDialog" allows to select the content of a folder and returns the value to "CreateNewFolderDialog"
- Adjusted "SystemDialog" to show messages and request confirmation for specific actions
- The "FileExplorer" now uses an icon cache to load file extension icons
- The "FileExplorer" now checks the file extensions with ".ToLower()"
- The "FileExplorer" can now open ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff", ".gif", ".apng", ".heif" files with the new "SystemImageViewer"
- The "FileExplorer" now uses a new ShowSideSettingButtons function to show the required amount of option menu buttons
- The "FileExplorer"'s option menu now allows running  ".bin", ".elf", ".iso" files with different emulators
- New "FileExplorer" file extension icons
- The "MainWindow" does now have the missing shadow/black box below the game/app icon ("StartRect")
- Added "HomeGroupAppsCount" & "GroupAppsOnHome" variables to keep track on folder items
- Adjusted "HomeAnimation" to allow loading Home without sound effect (currently only used when reloading Home)
- A "RightMenu" (options menu) has been added to the "MainWindow"
- Adjusted "AddNewApp" function to allow adding folders on the Home screen
- "ReloadHome()" now reloads Home without sound effect and also removes visible folder items
- New "ShowGroupAppsOnHome" & "RemoveGroupAppsOnHome" functions to show or hide apps or games within a folder
  - Moving down on a folder will move the main Home icons up and allow browsing the folder items
    - To go back up to the main Home icons the focus needs to be on the FIRST item
  - Moving up on the FIRST folder item will restore the main Home icons
- Adjusted "MoveAppsLeft" & "MoveAppsRight" functions to show folder items when moving to a folder
  - Both functions now also allow browsing the folder items
- The "MainWindow" now also uses a new "ShowSideSettingButtons" function to show the required amount of option menu buttons
- The "MainWindow" does now have a new "EnsureHighestZIndex" function to ensure that the options menu and required buttons are always on top of other elements
- The "MainWindow" now checks directly if a battery or WiFi is available and saves it to "IsBatteryPresent" & "IsWiFiAvailable"
  - The "SystemTimer" that keeps track of battery/WiFi status will not start if "IsBatteryPresent" & "IsWiFiAvailable" is "False"
- The "MainWindow"'s "ChangeBackgroundImage" function now allows loading a background image from the "BackgroundPath" JSON property of a game/app
- Changed name of "SystemGallery" to "SystemImageViewer" for the new image viewer
- Do not set "PauseInput=False" twice when returning to a window because "Activate()" will do it in the returning window
- Renamed "Account Management" to "User Management" in "GeneralSettings"
- "GeneralSettings" does now load the new Gamepad & PS4 Emulator settings
- New "ChangePS4ConfigValue" & "GetTOMLValue" functions to read/change the PS4 emulator config file
- Adjusted "SetupApps" & "SetupGames", "GameLibrary" & "FileExplorer" windows to save Game/App infos with the new JSON formatted
- New "SetupDrives" window that lists connected drives that can be selected to be processed in the next "SetupGames" & "SetupApps" windows process
- "WelcomeToSetup" -> Removed the "extra" setup for the Asus ROG Ally and Welcome message when starting OrbisPro for the first time
- "GameLibrary" now also loads saved PS4 games
- Adjusted "PauseProcessThreads" function in "ProcessUtils" to suspend now all threads of each running process with the name of the active running process
  - Also do not exit the loop if "OpenProcessThread = IntPtr.Zero" but continue
- New "PKGInstaller" window that handles PS4 package installations
  - Currently without real file progression but an indeterminate progress bar until finished
  - Fetches shadPS4 compatibility status using "FetchShadPS4Compatibility" function
  - Successfully installed PKGs will be added to the GamesList.json
  - A reload of Home is required to show the newly installed package
- Moved "SystemDialog" to the new "Dialogs" folder -> "Dialogs\SystemDialog"

# Package & Library Updates
- LibVLCSharp v3.9.2 -> v3.9.3
- LibVLCSharp.WPF v3.9.2 -> v3.9.3
- ManagedNativeWifi v2.7.0 -> v2.7.1
- Microsoft.Web.WebView2 v1.0.3124.44 -> v1.0.3179.45
- System.Management v9.0.3 -> v9.0.4
- Added Tomlyn v0.19.0
- Added SharedMemory v2.3.2
- Added SharpZipLib v1.4.2
- Added GameArchives v0.1.138
- Added HtmlAgilityPack v1.8.10
- Added PARAM.SFO v1.0.0
- Added PS4_Tools v1.0.0

# Button/Key Combo Reminders
- To reload the Home Screen simply press L+R buttons together on a gamepad or the F1 key on a keyboard
- To get back to the Home Screen from a running game simply press START+SELECT buttons together on a gamepad or the HOME key on a keyboard
  - To return back to the running game simply press the START+SELECT buttons together or the HOME key again
- To open the Options/Side Menu on the Home Screen simply press START on a gamepad or the F2 key on a keyboard while focused on a Game/App

# Installation/Archive Changes
- Changed from .7z to a 7Z self-extracting .exe archive (SFX - 1,25Gb parts) due to GitHub upload size limitation (2GB)
- In order to install/extract simply download all 3 required files and run "OrbisPro Beta v0.4.exe" when downloaded :
  - OrbisPro Beta v0.4.exe
  - OrbisPro Beta v0.4.7z.001
  - OrbisPro Beta v0.4.7z.002
