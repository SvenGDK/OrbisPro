# Update v0.3.1

- In order to use OrbisPro with this update you will need the [.NET Desktop Runtime 9.0.3 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.3-windows-x64-installer) or [.NET Desktop Runtime 9.0.3 x86](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.3-windows-x86-installer)

# Updates
- Upgrade to .NET 9
- Updated references & vulnerable packages :
  - CoreAudio v1.37.0 -> v1.40.0
  - FontAwesome.Sharp v6.3.0 -> v6.6.0
  - InTheHand.Net.Bluetooth v4.1.43 -> v4.2.0
  - LibVLCSharp v3.8.2 -> v3.9.2
  - ManagedNativeWifi v2.5.0 -> v2.7.0
  - Microsoft.Web.WebView2 v1.0.2420.47 -> 1.0.3124.44
  - System.Management v8.0.0 -> v9.0.3
  - System.Net.Http v4.3.0 -> v4.3.4
  - System.Text.RegularExpressions v4.3.0 -> v4.3.1
  - VideoLAN.LibVLC.Windows v3.0.20 -> v3.0.21

# Additions
- Added 19 new icons & 17 new backgrounds
- Add Asus ROG Ally X & MSI Claw models for later usage

# Fixes
- Fixed possible freezing and crashes when collecting games & applications
- Fixed possible crashes in the File Explorer
- Fixed a background change animation when moving to an app or game that has no background asset

# Internal Changes
- ISO Reader stability slightly improved
- Search only 1 list of game aliases to find icons & backgrounds
- Changed the "Home" loading behaviour
  - This fixes the bad placement of apps/games when loading/reloading "Home" or adding more
  - Input is now not possible until "Home" is fully loaded
- Improved game detection for games located in the "C:\" drive
  - Another future update will add the possibility to add games and applications from other drives
  - Reminder: Automatic detection currently only works on folders "C:\Program Files (x86)" and "C:\Games"
  - Exclusion list (including error reporters, loaders, launchers, ...) has been adapted
  - Now also loads the GOG Galaxy & REDlauncher if installed

# Other
- Removed "Media" from internal apps, this one was useless and the File Explorer should be used instead
