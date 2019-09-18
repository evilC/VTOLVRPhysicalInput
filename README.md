# VTOL VR Physical Input
A mod for [VTOL VR](https://store.steampowered.com/app/667970/VTOL_VR/) to allow using physical input devices (ie use a real joystick instead of the virtual reality joystick)  
Uses [MarshMello0/VTOLVR-ModLoader](https://github.com/MarshMello0/VTOLVR-ModLoader)  

Discussion: Contact me in the [VTOL VR Discord channel](https://discord.gg/WPvdZzG)

# Usage
1. Install the Mod Manager
1. Downoad a release zip from the [Releases Page](https://github.com/evilC/VTOLVRPhysicalInput/releases)  
**DO NOT use the green "Clone or Download" button on the main page!**
1. Unzip the contents to your VTOL VR folder
1. Open the Windows Joystick Control Panel (Click the Windows Start  button, type `Joy.cpl` and hit Enter)  
1. Edit `VTOLVRPhysicalInputSettings.xml` in the `VTOLVR\VTOLVR_ModLoader\mods` folder as appropriate
   There are currently no instructions on how to do so, as nobody else has expressed an interest in using it yet - feel free to contact me in the Discord channel linked above
   The supplied example file is configured for a Thrustmaster T.16000M / TWCS combo
1. Launch the game using the Mod Manager
1. Enable the mod

# Limitations
If there is one mapping to the virtual stick or the virtual throttle, then all input for that device must come from a physical device - the virtual device will no longer function at all
