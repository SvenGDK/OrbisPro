# OrbisPro
Media center and game launcher for Windows in the style of PS4's UI.<br>
The goal of this project is to build a type of Shell replacement for Windows with the focus on gaming.<br>
This type of Shell replacement can be used on any Desktop, Laptop or Portable device that runs Windows 10 or 11 x64.<br>
Beta releases do not disable any Windows features like the Taskbar or Explorer, they're both still accessible in any case.

<img width="360" alt="OrbisProBetaScreenshot1" src="https://github.com/SvenGDK/OrbisPro/assets/84620/85f51a20-eabb-4159-89ef-29cf17455751">
<img width="360" alt="OrbisProBetaScreenshot2" src="https://github.com/SvenGDK/OrbisPro/assets/84620/937f572f-4f64-4072-baf3-a84f851388f3">
<img width="360" alt="OrbisProBetaScreenshot3" src="https://github.com/SvenGDK/OrbisPro/assets/84620/06bd3293-bed5-4048-b2f0-a86e909ecc1d">
<img width="360" alt="OrbisProBetaScreenshot5" src="https://github.com/SvenGDK/OrbisPro/assets/84620/1342ecf8-4900-4720-bc62-d53c25605f86">

## Read before using
OrbisPro is on a BETA stage and some bugs may occur while running the application.<br>
Pleas read the Wiki on how to use/set up OrbisPro: https://github.com/SvenGDK/OrbisPro/wiki/First-Run

## Software Requirements
- [.NET Desktop Runtime 9.0.4 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.4-windows-x64-installer) or [.NET Desktop Runtime 9.0.4 x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.4-windows-x86-installer)
- Latest .NET Windows Updates
- .NET Framework 3.5 installed : ["Programs & Features" -> "Turn Windows features on or off"](https://github.com/user-attachments/assets/1971d7bc-e56c-4067-856b-020bb5f68618)
- DirectX End-User Runtime [DirectX End-User Runtime](https://download.microsoft.com/download/1/7/1/1718ccc4-6315-4d8e-9543-8e28a4e18c4c/dxwebsetup.exe)

## Hardware Requirements
<b>Minimal</b>
- CPU: AMD Zen 2 / Intel Skylake with 4 cores and 8 threads or more (x64, AVX2 support recommended)
- GPU: AMD HD 5000 series or newer / NVIDIA GTX 400 or newer / Intel HD Graphics 500 or newer (OpenGL 4.3 support recommended)
- RAM: 8 GB RAM or better
- OS: Windows 10 x64 or newer
- Display: 1920x1080 @ 60Hz with 100% scaling

<b>Recommended</b>
- CPU: AMD Zen 2 / Intel Skylake with 6 cores and 12 threads, 8 cores or more (x64, AVX2 support)
- GPU: AMD RX 400 / NVIDIA GTX 900 or newer (With Vulkan API support)
- RAM: 8 GB Dual Channel (2x4 GB)
- OS: Windows 10 or 11 x64
- Display: 1920x1080 @ 120Hz with 100% scaling
- [.NET Desktop Runtime 9.0.4 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.4-windows-x64-installer) or [.NET Desktop Runtime 9.0.4 x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.4-windows-x86-installer)

## Current Available Core Features
- System Setup
  - Checks for Updates on first Run
  - Choose drives that will be scanned for Games/Apps
  - Automatic detection of PC games (more in next update)
  - Automatic detection of PC applications
  - Customize the interface (User name, background, sound effects)
- Keyboard Support
- Gamepad Support
  - OrbisPro uses XInput from SharpDX
  - The button layout can be changed in the Settings
- Video Backgrounds
- User Interface animations and interactions
  - Background switching animations
- Home Screen Options
  - Add the selected app or game to a folder
  - Delete a selected folder
- Wifi & Battery indicator
- Add / Remove Device Detection
  - Currently only used for USB drives & Disc drives
  - Detects if a USB or a disc get inserted or removed and adds them to Home or File Explorer
- Notification Pop-Ups
- Game Disc Detection
  - PS1, PS2 & PC-Engine disc games can currently be started from Home
- Emulator Integration
  - ePSXe, PCSX2, RPCS3, shadPS4, Vita3k, Mednafen, Fusion and Dolphin are currently available
  - BIOS files need to be added manually. Please read the Wiki to know how to do this.
  - Latest Firmwares for PS3 & Vita can be downloaded & installed using OrbisPro
  - PS4 firmware files need to be added manually in the "System\Emulators\shadps4\user\sys_modules" folder
- Suspend & Resume a running game or application (like on game consoles)
- Return to HOME
  - Return to the Home screen from ANY running game or application
  - Press the "Back/Share" AND "Start/Options" buttons together (or the HOME key on a keyboard)
- Application / Window Switcher
  - Switch between running executables (like return to the game)
  - Kill running processes that have an active window
  - Press the 'Back/Share' button (or the 'O' key on a keyboard) to open the Switcher
- Bluetooth Management
  - Scan for Bluetooth devices
  - Pair/Unpair Bluetooth devices (supports PIN input)
  - Connect/Disconnect Bluetooth devices
- WiFi Network Management
  - Scan for available WiFi Networks
  - Connect/Disconnect secured & open WiFi Networks (supports password input)
  - Shows connection status of WiFi Networks
- Media Player
  - Can be started from the File Explorer
  - Uses VLC (LibVLCSharp)
  - https://code.videolan.org/videolan/LibVLCSharp#features
  - Still work in progress
- Image Viewer
  - Rotate images by 90°
  - Make images fullscreen or back to original size
- System Message Window
  - Shows information, warnings or errors of OrbisPro
- Application & Game Libary
- File Explorer
  - "Add to Game Library" option for executables & roms
  - "Add to Apps Library" option for executables
  - "Play Media" for video & image files
  - "Copy" & "Delete" option
  - "Install PKG File" option for PS4 games
  - "Start" option for executables
  - "Start with ..." option for ".bin", ".elf", ".iso" files
- PKG installer for PS4 games
  - The PKG installer checks game compatibility with the shadPS4 compatibility database and returns the current working state of the game in a dialog
    - You can accept the message and install the PKG or cancel the action
- Gamepad Input Tester
  - Shows pressed button, Left/Right Thumb Y/X values, Left/Right Trigger values
- Audio Settings
  - Change notification volume (not working yet)
  - Enable/Disable background audio
  - Select a navigation sound pack (Changes the sound effects of the UI navigation)
- Background & Display Settings
  - Enable/Disable background animation
  - Enable/Disable background music
  - Enable/Disable background switchting animation
  - Set a custom background
  - Set a custom display resolution or AutoScale
- Emulator Settings
  - Emulator setup for PS3 & PS Vita
  - Change any emulator settings directly inside OrbisPro (PS3 not working atm)
  - Settings -> Emulators
- Network Settings
  - Connect To Internet -> Enables/Disables Ethernet connection
  - Set Up Internet Connection -> Use for Wi-Fi connections
  - Set a custom download path
- Notification Settings
  - Enable/Disable notifications
  - Set the notification duration (in seconds)
- Account Management Settings
  - Change the Username
- Gamepad Settings
  - Adjust the polling rate of gamepad 1-4 (only 1 is currently working atm)
  - The gamepad input option
  - Set the gamepad button layout
  
## Font Note
OrbisPro will use the [SST font family](https://font.download/font/sst) ONLY if installed.<br>
It is NOT included in the repository nor in the final archive.

## Notes for portable consoles
- Set the Control Mode to "Gamepad" for the best experience on portable Windows devices
  - If "Gamepad" mode is disabled you might not be able to use button combinations or do anything in OrbisPro without any other controller or keyboard connected

## Used Libraries
- [DiscUtils](https://github.com/discutils/discutils)
- [Font-Awesome-WPF](https://github.com/charri/Font-Awesome-WPF)
- [InTheHand.Net.Bluetooth](https://github.com/inthehand/32feet/)
- [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp)
- [Managed Native Wifi](https://github.com/emoacht/ManagedNativeWifi)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [SharpDX](https://github.com/sharpdx/SharpDX)
