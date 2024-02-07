# Preload Manager
Lightweight preload patcher utility

> **_IMPORTANT_**: If used incorrectly you can really break plugins, I am not responsible for you breaking something in your pack. This should really only be used by modpack creators and mod developers who want an easy way to disable a plugin if it exists.

## Why this exists
I had a request for my other plugins to remove LethalConfig as a dependency from the Thunderstore package to support a single use case.

Both other plugins have LethalConfig as a soft dependency so it can be removed if wanted, however, there is one use case where manually uninstalling it doesn't help.

That use case is when a modpack creator publishes a modpack to Thunderstore with LethalConfig removed, when that modpack is installed via a mod manager, all dependencies are also downloaded even when not listed in the modpack.

This means a modpack creator who wants a specific experience, with a dependency removed, doesn't have a way to do that other than sharing a profile code.

Now obviously the best way to fix this is to have Thunderstore implement some way to ignore a specific dependency in the modpack config, but that doesn't exist right now from what I understand.

So I decided to write a small preload patching utility which lets anyone disable a plugin from loading if they want to.

## Features
- Disables the awake method in plugins that are configured in the config file
- Easy usage by other mod developers who don't want to learn how to do this themselves
  - Feel free to fork it if you want to use this as a base for your own preload patcher (GitHub link can be found in the link at the top right)
- Pattern protection so no one can just use wildcards to disable all mods
- Ability to add a json file with plugin names if desired
  - See guide below

## Potential Future Features
- Allow a mod developer to...
  - Load their own methods at preload
  - Run their own methods for every plugin load event
  - I'll probably think of more later

## Compatibility
This should be compatible with any mod, however, if you are trying to disable a mod which has its own preload patcher - you likely will have a bad time...

## JSON Information
To make things easier I have created an extremely fast and efficient parallel recursive file search for any files named exactly ```PreloadManager.json``` in the Config or Plugin folder.

Using an exact file name ensures that it doesn't attempt to read just any json file, it will also ensure the least amount of required processing.

This also means anyone can make this mod a dependency for their mod and just add a single json file in their mods zip file to disable some other mod (or mods) for what ever reason.

<details>
<summary>JSON format</summary>

The format is very simple and is just a single string array you can learn more [Here](https://www.microfocus.com/documentation/silk-performer/205/en/silkperformer-205-webhelp-en/GUID-0847DE13-2A2F-44F2-A6E7-214CD703BF84.html)

```json
[
  "ModName",
  "SomeOtherModName",
  "AnotherOne"
]
```

</details>

## Configuration
The config file is named ```PXC.PreloadManager.cfg``` and will be located in the normal Config directory (also accessible via the mod manager config tab)

<details>
<summary>Disable Preloader</summary>

Want to disable this preloader entirely? Set this to true and it will just harmlessly return when initialized.

```cfg
## This will disable this preloader entirely
# Setting type: Boolean
# Default value: false
Disable Preloader = false
```

</details>

<details>
<summary>Disable Json Search</summary>

This will disable the parallel recursive file search for any files named exactly ```PreloadManager.json``` in the Config or Plugin folder. I use a very fast and performant method which searches even a modpack with 200+ mods extremely fast and efficiently (average time between 0.10 and 0.20 seconds).

```cfg
## Disable searching for any 'PreloadManager.json' files in the config and plugin directories
# Setting type: Boolean
# Default value: false
Disable Json Search = false
```

</details>

<details>
<summary>Mod List</summary>

This is where a modpack creator can list the mod names to disable, or path to a specific json file using the built in internal location/path variables (This can be used with 'Disable Json Search' if you don't want the file search but still want to use a json file). If this config item is empty (default) then this preloader will basically disable it self during initialization if there is also no json file found in the json search.

> **_NOTE_**: The name used is the name from the _BepInPlugin_ attribute found in every mod, more information on this is in "How to find the correct mod name to use" below

<details>
<summary>How to find the correct mod name to use</summary>

The name used is the name from the **_BepInPlugin_** attribute found in every mod, you can get this name from either of the following methods

#### Console/Log file

If you have the console open, or go and open the log located at "%appdata%\..\LocalLow\ZeekerssRBLX\Lethal Company\Player.log" and look for the line below. This line shows that **_ModName_** is the correct name to use.

```cfg
[Info   :   BepInEx] Loading [ModName 1.1.3]
```

#### Source tab in Thunderstore

Looking at the source of a mod you want to search for the line that starts with "_[BepInPlugin(_" (example below) and when you find it you want to take the middle attribute, this one shows **_ModName_** is the correct name to use (Note: Sometimes mods will use the same name for the first and second attribute).

```cfg
[BepInPlugin("PXC.ModName", "ModName", "1.1.3")]
```

---

</details>

```cfg
## This is the list of mods to patch separated by semi-colons (;) and/or a path to a json file
## 
## Internal path variables:
## %ConfigPath% = Config directory
## %PluginPath% = Plugin directory
# Setting type: String
# Default value: 
Mod List = 
```

#### Example
This example shows disabling one specific mod and then all mods found in a json file in the Config directory

```cfg
## This is the list of mods to patch separated by semi-colons (;) and/or a path to a json file
## 
## Internal path variables:
## %ConfigPath% = Config directory
## %PluginPath% = Plugin directory
# Setting type: String
# Default value: 
Mod List = ModName;%ConfigPath%\DisableMods.json
```

</details>

## Other Mods
[![ShipLootPlus](https://gcdn.thunderstore.io/live/repository/icons/PXC-ShipLootPlus-1.0.0.png.128x128_q95.png 'ShipLootPlus')](https://thunderstore.io/c/lethal-company/p/PXC/ShipLootPlus/)
[![EnhancedSpectator](https://gcdn.thunderstore.io/live/repository/icons/PXC-EnhancedSpectator-1.0.2.png.128x128_q95.png 'EnhancedSpectator')](https://thunderstore.io/c/lethal-company/p/PXC/EnhancedSpectator/)
[![PrideSuits](https://gcdn.thunderstore.io/live/repository/icons/PXC-PrideSuits-1.0.2.png.128x128_q95.jpg 'PrideSuits')](https://thunderstore.io/c/lethal-company/p/PXC/PrideSuits/)
[![PrideSuitsAnimated](https://gcdn.thunderstore.io/live/repository/icons/PXC-PrideSuitsAnimated-1.0.1.png.128x128_q95.jpg 'PrideSuitsAnimated')](https://thunderstore.io/c/lethal-company/p/PXC/PrideSuitsAnimated/)
[![PrideCosmetics](https://gcdn.thunderstore.io/live/repository/icons/PXC-PrideCosmetics-1.0.2.png.128x128_q95.png 'PrideCosmetics')](https://thunderstore.io/c/lethal-company/p/PXC/PrideCosmetics/)