# Update v0.2.0

This is an important project upgrade and is not available through the internal Updater.<br>
All next updates will be available again in "OrbisPro Update" on the Home screen & "Check for Updates" in the settings.<br>
In order to use OrbisPro after this update you will need the [.NET Desktop Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

# Updates
- Project upgraded from .NET Framework 4.8 to .NET 8
  - In order to use OrbisPro with this update you will need the [.NET Desktop Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.
- Replaced FontAwesome.WPF with FontAwesome.Sharp
- Replaced libhook library with OrbisKeyboardHook
- Adjusted Welcome Message on first run
- OrbisPro now gets the display size and scales automatically (set 100% scaling factor in your display settings)
- Added a Wifi name & signal quality indicator on the Home screen (if connected)
- Added a battery level indicator on the Home screen (if battery present)
- Added a new system timer that keeps track of Wifi & battery status (if connected & present)
  - Updates every 45sec at the moment (will be raised in next update)
- Added missing L+R Shoulder button combo to reload Home (F1 on keyboard)
- New Display Settings
  - "Background Settings" has moved to "Display Settings"
  - Disable auto scaling & manually adjust the display resolution
- Suspend & Resume function adjusted for a better responsiveness
- Memory usage of OrbisPro will now be reduced when starting a game or application
- OrbisPro now restores the Home screen when the started game or application exited
- The Application Switcher now closes automatically if the started game or application will be killed
- Smoother game background switch (Updated CacheOption & CreateOptions of BitmapImage)

# Fixes
- Almost "no more" input lag for gamepads
  - A delay of 60ms is set (can be adjusted manually in \System\Settings.ini)
- Smoother Home screen navigation
  - Prevent animating to next item until last animation is done
  - Prevent also input to move to the next item until last animation is done
  - Those (minimal) delays should keep the Home screen clean
- Keyboard input fixes
  - Some windows did not have the correct keyboard key assignment
- Fixed: FocusVisualStyle of items in the Application Switcher
- Application Switcher adjustments & fixes
- Fixed: Crash when checking for games. Now checks if game folders exist before checking its content
- Fixed: Black screen (No setup video) when starting OrbisPro the first time on the ROG Ally
- Fixed: Disconnecting a WiFi network

# Internal Changes
- Removed unused code
- Replaced If/ElseIf with Select Case for keyboard input
- Removed additional thread for some animations
- Gamepad polling rate will not more be adjusted based on the monitor frequency
- Updated OrbisAudio
  - Adjust volume (MasterVolumeUp, MasterVolumeDown, MuteMasterVolume) utilities
  - GetCurrentMasterVolume utility
- Added OrbisDisplay
  - GetMonitorFrequency utility
  - SetScaling utility
- Updated OrbisNetwork
  - New GetWiFiSignalStrenght (with GetWiFiSignalImage) utility
- Updated ProcessUtils
  - Added ActiveProcess (handles start & exit of started game or application)
  - Added ActiveProcess_Exited event
- Updated OrbisPowerUtils
  - Added missing GetBatteryImage utility

# Note
- Wifi & Battery will show up (even if Wifi off & no battery) after the first setup but will disappear in 45sec.
- This will be fixed in the next update
