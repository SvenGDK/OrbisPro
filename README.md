# OrbisPro
Media center and game launcher for Windows in the style of PS4's UI.<br>
The goal of this project is to build an entire Shell replacement for Windows with the focus on gaming.<br>
This Shell replacement can be used on any Desktop, Laptop or Portable device that runs Windows 10 or 11 x64.<br>
Beta releases do not disable any Windows features like the Taskbar or Explorer, they're both still accessible in any case.

<img width="360" alt="OrbisProBetaScreenshot1" src="https://github.com/SvenGDK/OrbisPro/assets/84620/85f51a20-eabb-4159-89ef-29cf17455751">
<img width="360" alt="OrbisProBetaScreenshot2" src="https://github.com/SvenGDK/OrbisPro/assets/84620/937f572f-4f64-4072-baf3-a84f851388f3">
<img width="360" alt="OrbisProBetaScreenshot3" src="https://github.com/SvenGDK/OrbisPro/assets/84620/06bd3293-bed5-4048-b2f0-a86e909ecc1d">
<img width="360" alt="OrbisProBetaScreenshot5" src="https://github.com/SvenGDK/OrbisPro/assets/84620/1342ecf8-4900-4720-bc62-d53c25605f86">

## Read before using
OrbisPro is on a BETA stage and some bugs may occur while running the application.<br>
Pleas read the Wiki on how to use OrbisPro: https://github.com/SvenGDK/OrbisPro/wiki/First-Run

## Requirements
Minimal
- CPU: AMD Zen 2 / Intel Skylake with 4 cores and 8 threads or more (x64, AVX2 support recommended)
- GPU: AMD HD 5000 series or newer / NVIDIA GTX 400 or newer / Intel HD Graphics 500 or newer (OpenGL 4.3 support recommended)
- RAM: 8 GB RAM or better
- OS: Windows 10 x64 or newer
- [.NET Desktop Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

Recommended
- CPU: AMD Zen 2 / Intel Skylake with 6 cores and 12 threads, 8 cores or more (x64, AVX2 support)
- GPU: AMD RX 400 / NVIDIA GTX 900 or newer (With Vulkan API support)
- RAM: 8 GB Dual Channel (2x4 GB)
- OS: Windows 10 or 11 x64
- [.NET Desktop Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

Current display limitation: 100% scaling (no zoom).

## Current Available Core Features
- System Setup
- Keyboard Support
- Gamepad Support
  - OrbisPro uses XInput from SharpDX
  - The button layout changes depending on which device you use
- Video Backgrounds
- User Interface animations and interactions
- Wifi & Battery indicator
- Add / Remove Device Detection
  - Currently only used for USB drives & Disc drives
  - Detects if a USB or a disc get inserted or removed and adds them to Home or File Explorer
- Notification Pop-Ups
- Game Disc Detection
  - PS1, PS2 & PC-Engine disc games can currently be started from Home
- Emulator Integration
  - ePSXe, PCSX2, RPCS3, Vita3k, Mednafen, Fusion and Dolphin are currently available
  - BIOS files need to be added manually. Read the Wiki to know how to.
  - Latest Firmwares for PS3 & Vita can be downloaded & installed using OrbisPro
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
- System Message Window
  - Shows information, warnings or errors of OrbisPro
- Application & Game Libary
- File Explorer
  - "Add to Game Library" option
  - "Add to Apps Library" option
  - "Play Media" for video files
  - "Copy" & "Delete" option
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
  
## Font Note
OrbisPro will use the [SST font family](https://font.download/font/sst) ONLY if installed.<br>
It is NOT included in the repository nor in the final archive.

## ROG Ally Notes
- Control Mode needs to be set to "Gamepad"

## Used Libraries
- [DiscUtils](https://github.com/discutils/discutils)
- [Font-Awesome-WPF](https://github.com/charri/Font-Awesome-WPF)
- [InTheHand.Net.Bluetooth](https://github.com/inthehand/32feet/)
- [LibVLCSharp](https://code.videolan.org/videolan/LibVLCSharp)
- [Managed Native Wifi](https://github.com/emoacht/ManagedNativeWifi)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [SharpDX](https://github.com/sharpdx/SharpDX)
