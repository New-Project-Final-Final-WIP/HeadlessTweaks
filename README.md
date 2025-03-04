# HeadlessTweaks

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/). Adds some nice to have features to headless clients

## Usage
Adds various commands that you can send to the headless by messaging the headless account in Resonite
 - Try sending `/help` to the headless

You can also view the [list of commands here](CommandList.md) - Can be out of date



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

## Configuration
In your `rml_config` directory there will be a `HeadlessTweaks.json` file with a variety of options. Some chat commands will write to this file so please ensure your `HeadlessTweaks.json` file is writable by the user running your headless server or the user running your containers if running through Docker or Pterodactyl.

Here are some explinations for the options available.

### Discord Features
⚠ **Discord.NET Dependency** ⚠

[Discord.NET](https://discordnet.dev) libraries are a required dependency to use Discord features. Please install your Discord.NET DLLs in your `rml_libs` directory if `UseDiscordWebhook` is set to `true`.

You have the option to enable Discord integration for a Discord channel. A variety of events will get logged in Discord according to a webhook you specify. When configured, a variety of events will get logged to the channel such as session start/close, user joins/leaves, and world saves.

You can set `UseDiscordWebhook` to `true` in your config to enable these features. The options are as follows:

- `UseDiscordWebhook` - Set to `true` to enable Discord integration features
- `DiscordWebhookID` - Webhook ID taken from a Discord channel WebHook integration URL (`https://discord.com/api/webhooks/WEBHOOK_ID_HERE/`)
- `DiscordWebhookKey` - Webhook Key taken from a Discord channel WebHook integration URL (`https://discord.com/api/webhooks/WEBHOOK_ID_HERE/WEBHOOK_KEY_HERE`)
- `DiscordWebhookUsername` - Display name that will be attached to messages sent by this integration
- `DiscordWebhookAvatar` - HTTP or RESDB URL to Avatar/PFP image that will be attached to messages sent by this integration
- `DiscordWebhookThreadID` - The ID of a thread in the channel the Webhook is from to only send headless messages to that thread - Can be left unset
- `DiscordLinkToSession` - Set to `true` to add a link to open the session in the Session Started event message

### Auto Invitation Opt-Out
It's useful to auto-invite users when a headless spins up. However this can get kind of spammy. Users can opt-out of auto-invites with the `/optOut` chat command or by have their name manually entered in the `AutoInviteOptOut` list. The list simply takes user IDs. For example:
```json
"AutoInviteOptOut": [
    "U-badhaloninja",
    "U-Gareth48",
    "U-Engi"
]
```
### Permission Levels
User permission levels can be set on the console or with a chat command. However you can also set them in your config file. Whichever method you use, permissions will be written to `PermissionLevels` in your config file like so:
```json
"PermissionLevels": {
    "U-badhaloninja": "Administrator",
    "U-Cyro": "Owner",
    "U-Gareth48": "Moderator"
}
```

### Mapping Session IDs to Names
You can use the `/setSessionName` chat command to give sessions names. However you can also manually map session IDs to session names in your configuration file as demonstrated bellow:
```json
"SessionIdToName": {
    "S-U-headless:headlessexamplesession1": "The First Example",
    "S-U-headless:headlessexamplesession2": "The Second Example"
}
```

### Configuring Events
Events can be disabled using the configuration for `DiscordWebhookEnabledEvents`
```json
"DiscordWebhookEnabledEvents": {
    "HeadlessStart": true,
    "WorldSaved": false,
},
```

Event colors can be changed under `DiscordWebhookEventColors`
```json
"DiscordWebhookEventColors": {
    "WorldSaved": "[0;1;0;1]",
    "UserJoin": "[0;0;1;1; Linear]",
},
```

Here are the valid event names,
```
EngineStart / HeadlessStart
EngineStop / HeadlessStop

WorldCreated
WorldSaved
WorldDestroyed

UserJoin
UserLeft
```

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [HeadlessTweaks.dll](https://github.com/New-Project-Final-Final-WIP/HeadlessTweaks/releases/latest/download/HeadlessTweaks.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.


Optionally Uses [Discord.NET](https://github.com/discord-net/Discord.Net) for Discord Integration
 - [NuGet](https://www.nuget.org/packages/Discord.Net/)
 - The dlls should be put in the `rml_libs` folder
