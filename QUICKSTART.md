## Setup OrbisPro without the system setup
- Create an 'Settings.ini' file in the "\Config" folder and add :

[Setup]
Done=True

- Create a 'GameList.ini' file in the "\Games" folder and follow this structure to add some games:

[PLATFORM]
PLATFORMGame1=COMPLETE_FILE_PATH
PLATFORMGame2=
PLATFORMGame3=

Optional :
- Add ;ShowOnMainMenu to show the game on the main menu (Not working atm - on by default)
- Add ;ShowInLibrary to show the game also in the library (Not working atm - on by default)
- Both can be combined ;ShowOnMainMenu;ShowInLibrary -> Game1=COMPLETE_FILE_PATH;ShowOnMainMenu;ShowInLibrary

### Example
[PS1]
PS1Game1=C:\Games\PS1\CrashBandicoot1.cue;ShowOnMainMenu;ShowInLibrary
PS1Game2=C:\Games\PS1\CrashBandicoot2.cue;ShowOnMainMenu
PS1Game3=C:\Games\PS1\CrashBandicoot3.cue

[PS2]
PS2Game1=C:\Games\PS2\MetalGearSolid2.iso;ShowOnMainMenu;ShowInLibrary
PS2Game2=C:\Games\PS2\MetalGearSolid3.iso;ShowOnMainMenu

[PS3]
PS3Game1=C:\Games\PS3\MetalGearSolid4\PS3_GAME\EBOOT.BIN;ShowOnMainMenu;ShowInLibrary
PS3Game2=C:\Games\PS3\MetalGearHDCollection\PS3_GAME\EBOOT.BIN;ShowOnMainMenu
PS3Game3=C:\Games\PS13\CrashBandicoot3\PS3_GAME\EBOOT.BIN