# VTOL VR Physical Input
A mod for [VTOL VR](https://store.steampowered.com/app/667970/VTOL_VR/) to allow using physical input devices  
Uses [MarshMello0/VTOLVR-ModLoader](https://github.com/MarshMello0/VTOLVR-ModLoader)  

# Usage
1. Install the Mod Manager
1. Downoad a release zip from the [Releases Page](https://github.com/evilC/VTOLVRPhysicalInput/releases)  
**DO NOT** use the green "Clone or Download" button on the main page!**
1. Unzip the contents to your VTOL VR folder
1. Open the Windows Joystick Control Panel (Click the Windows Start  button, type `Joy.cpl` and hit Enter)  
1. Edit `VTOLVRPhysicalInputSettings.xml` in the `VTOLVR\VTOLVR_ModLoader\mods` folder
1. In the `JoystickName` setting, change `Value` to match the name of your joystick as it appears in Joy.cpl
1. Launch the game using the Mod Manager
1. Enable the mod

# Limitations
This is currently just a Proof-of-Concept, it will only work with Pitch and Roll, and they will automatically be bound to the X and Y axes of your stick.  
Yaw will probably not work in-game
