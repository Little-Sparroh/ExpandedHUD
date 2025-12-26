# ExpandedHUD

A BepInEx mod for MycoPunk that enhances the HUD with additional displays and information overlays.

## Description

ExpandedHUD adds various HUD elements to improve gameplay awareness in MycoPunk, including weapon statistics, damage tracking, movement speed, altitude, health/XP displays, and utility features.

## Features

- **Gun Stats HUD**: Displays comprehensive weapon statistics including damage, fire rate, magazine size, reload time, range, recoil patterns, spread, and fire mode
- **Damage Meter HUD**: Real-time combat tracking with total damage, DPS calculations, kill counters, and core destruction metrics
- **Speedometer HUD**: Live player movement speed display in meters per second
- **Altimeter HUD**: Real-time altitude tracking display
- **Consumable Hotkeys**: Quick access hotkeys for consumable items
- **Health Display**: Real-time health percentage and value overlay on the HUD
- **XP Information**: Improved leveling display showing XP needed for next level
- **End Screen Stats**: Detailed mission completion statistics including damage dealt, enemies killed, elemental stacks, and more
- **RangeFinder HUD**: Real-time distance measurement display to objects in the player's line of sight

## Installation

### Prerequisites

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible

### Install via Thunderstore (Recommended)

1. Install the Thunderstore Mod Manager
2. Search for "ExpandedHUD" by Sparroh
3. Download and install the mod

### Manual Installation

1. Download the latest release from the [GitHub repository](https://github.com/Little-Sparroh/ExpandedHUD)
2. Extract the contents to your MycoPunk game directory
3. Place `ExpandedHUD.dll` in the `BepInEx/plugins/` folder

## Usage

The mod loads automatically when you start MycoPunk. HUD elements can be toggled on/off in the mod's configuration file located at `BepInEx/config/sparroh.expandedhud.cfg`.

## Help

* **Mod not loading?** Ensure BepInEx is properly installed and check the console for error messages
* **HUD elements not visible?** Check the configuration file to ensure displays are enabled
* **Performance issues?** Try disabling unused HUD elements in the config file
* **Incompatible with other mods?** Report issues on the GitHub repository

## Authors

- Sparroh
- DarkCactus (original UITweaks)
- funlennysub (BepInEx template)
- [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
