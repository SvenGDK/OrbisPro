# Update v0.1.1

This update is an important hotfix and recommended to install.<br>

## Changes
- Replaced libhook library with OrbisKeyboardHook
- Added missing L+R Shoulder button combo to reload Home in case the interface gets messed up
- Fixed: Crash when checking for games. Now checks if game folders exist before checking its content
- Fixed: Black screen (No setup video) when starting OrbisPro the first time on the ROG Ally
- Fixed: Disconnecting a WiFi network

## Other Internal Changes
- Added OrbisKeyboardHook
  - Used to hook the 'Home' key to return to the Home screen in games/applications
  - Also used to hook the 'Enter' and 'ESC' key in input boxes for confirmation or closing
