# HeadlessTweaks

A [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader) mod for [Neos VR](https://neos.com/). Adds some nice to have features to headless clients

## Usage
Adds various commands that you can send to the headless by messaging the headless account in neos
 - try sending `/help` to the headless

Commands are restricted via permission levels,
The permission levels are:
```
None,
Moderator,
Administrator,
Owner
```

You can set a user's permission level in the headless console using
`setUserPermission <user-id> <permission-level>`

You can also assign permissions in game with the setPerm command
`/setPerm [user id] [level]`
With this command you can modify people's permissions to a level that is lower or equal to your own,
Minimum permission level required to use this command is Moderator



## Installation
1. Install [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader).
1. Place [HeadlessTweaks.dll](https://github.com/New-Project-Final-Final-WIP/HeadlessTweaks/releases/latest/download/HeadlessTweaks.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Neos logs.


Optionally Uses [Discord.NET](https://github.com/discord-net/Discord.Net) for Discord Integration
 - [NuGet](https://www.nuget.org/packages/Discord.Net/)
 - The dlls should be put in the `nml_libs` folder
