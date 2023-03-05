# OrbisPro
Media center and game launcher for Windows in the style of PS4's UI.

## Read before using
OrbisPro is on a ALPHA stage, so many bugs can occur and you probably need to restart the application.</br>
This build is only optimized for FHD 1920x1080, other resolutions will be added on later builds.</br>
BIOS files are still required to use emulators like PS1, PS2, ... Read the 'BIOS Setup' below.</br>
The PS3 firmware can be downloaded and installed directly with RPCS3 (Thanks for this command!)</br>
A Steam Deck optimized version will be available too.

Mimimum Requirements (To play any games up to PS2 and similar) :
- 4 GB RAM
- 2 GB video memory
- Modern Dual Core CPU
- Windows 7, 8, 10 or 11 (x64 version is recommended)
- .NET Framework 4.8 (probably required in Win7+8 - not tested)

Recommended Requirements (To play modern games like Switch, PS3 & PS4) :
- 8 GB of Dual Channel (2x4 GB) RAM
- AMD RX 400 / NVIDIA GTX 900 or newer (With Vulkan API support)
- AMD Zen 2 / Intel Skylake with 6 cores and 12 threads, 8 cores or more (x64, AVX2 support)
- Windows 10 or 11 x64

Sidenote: Later builds will turn off most parts of OrbisPro when launching a game and enable only the 'Game Options Menu' & 'System Dialog' to save system resources.

## Current Available Core Features
- Very Basic setup
- Download of PS3 Firmware
- Keyboard & Gamepad support
  - XInput & DirectInput are available
- Animated user interface
- Supports video backgrounds
- USB (only for notifications atm) & Disc support (displaying on the main menu & booting)
- System-wide Notification Pop-Ups
- Detects physical game discs (PS1, PS2 & PC-Engine only atm)
- Game Emulators (ePSXe, PCSX2, RPCS3 & Mednafen only atm)
  - Some BIOS files need to be added manually (Read 'BIOS Setup' below)
- Game Installer (Not working atm - games need to be added in the 'File Explorer')
  - Set up your games in OrbisPro
  - Choose where to show the game [Main Menu / Library]
- Emulator Configurators (Not working atm)
  - Settings -> Emulator Settings
- Games can be booted from the Main Menu / Library / Disc
- Application/Game Libary
- File Explorer
- Audio Settings
  - Change notification volume (not working yet)
  - Enable/Disable background audio (not working yet)
  - Select a navigation sound pack (Changes the sound effects of the UI navigation)
- Background Settings (not working yet)
  - Turn video background on/off (Off will change the background to a color or image)
  - Use Custom Background on/off (Off will use the default background)
  - Choose your background (Can be a color, image or video)
- Emulator Settings (not working yet)
  - Show the installed emulators -> Here you can install new emulators or update existing ones (not working yet)
  - Different emulator configurators

## Game Support
- Supports PS1 & PS2 discs (Other game discs like Saturn, Dreamcast, ... will be supported too at a later point)
- Supports PS1, PS2, PS3 & PS4 (Nint. (including Switch), Sega backups, more will be added at a later point)
- Game emulators need to be configured manually atm (System\Emulators - they can be configured inside OrbisPro at a later point)

## FIRST Setup
- OPTIONAL: Connect a controller if you have one pre-configured
- Start "OrbisPro.exe"
- Press the 'Home' key (controllers need to remap the PS/Home Button to the keyboard home key)
- Continue in 'English'
- Setup the PS3 Emulator
- You can skip this part by hitting 'Select Backups' with the keyboard key 'X' or Cross/A on a controller
- After choosing 'Select Backups' the 'File Explorer' will start
- From here you can add your first backups by browsing to your backup files
- You can add a game by pressing the 'Space' key or Options/Start button on a controller, then choose 'Add to game library'
- After adding all your backups you can return by going all the way back with the key 'O' or Circle/B button on a controller

## BIOS Setup
- For PS1 games, place your BIOS files into "System\Emulators\ePSXe\bios"
- For PS2 games, place your BIOS files into "System\Emulators\PCSX2\bios"
- For PC-Engine games, place your SYSCARD*.PCE files into "System\Emulators\mednafen"

## Background Videos & Audio
- OrbisPro Alpha comes only with 2 included background videos (1 with and 1 without audio)
- You can set your own background video in the system settings
  - Settings -> Background
    - Use video background must be ON
    - Use custom background must be ON
    - Select your background video
  - Recommended codec: H264 - MPEG-4 AVC (.mp4)
- You can turn the video background audio on/off in the system settings
  - Settings -> Audio -> Background Video Audio ON/OFF (not working yet)

## Keyboard Shortcuts
- Cross (X) -> X
- Circle (O) -> O
- Triangle -> T
- PS Home Button -> Home
- Options -> Space
- Left -> Left arrow
- Right -> Right arrow
- Up -> Up arrow
- Down -> Down arrow

## Tested & Build On
- Intel i9-13900K
- NVIDIA GTX 3080
- 32 GB (2x16 GB) of Dual Channel RAM
- Windows 11 22H2 (Build 22621.1265) x64 
