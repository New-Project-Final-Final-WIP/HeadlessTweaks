# Commands

This is a list of commands that can be used in-game.

## Categories

* [Common](#Common)
* [Headless Management](#HeadlessManagement)
* [Debug](#Debug)
* [Moderation](#Moderation)
* [World Management](#WorldManagement)

## Commands

### Common

* [help](#help)
* [optOut](#optOut)
* [markAllRead](#markAllRead)
* [reqInvite](#reqInvite)
* [getSessionOrb](#getSessionOrb)
* [worlds](#worlds)

### Headless Management

* [shutdown](#shutdown)
* [message](#message)
* [contacts](#contacts)
* [addContact](#addContact)
* [removeContact](#removeContact)

### Debug

* [throwErr](#throwErr)
* [throwErrAsync](#throwErrAsync)

### Moderation

* [setPerm](#setPerm)
* [getPerm](#getPerm)

### World Management

* [users](#users)
* [saveWorld](#saveWorld)
* [startWorldTemplate](#startWorldTemplate)
* [startWorld](#startWorld)
* [worldTemplates](#worldTemplates)
* [startWorldUrl](#startWorldUrl)
* [startWorldOrb](#startWorldOrb)
* [role](#role)
* [roleSelf](#roleSelf)
* [addWorld](#addWorld)
* [removeWorld](#removeWorld)
* [listRosterWorlds](#listRosterWorlds)
* [listWorlds](#listWorlds)
* [closeWorld](#closeWorld)
* [setSessionAccessLevel](#setSessionAccessLevel)
* [hideFromListing](#hideFromListing)
* [setSessionName](#setSessionName)

## Command Details

### help

* **Permission:** None
* **Syntax:** /help [?command]
* **Description:** Shows this help message
* **Category:** Common

### optOut

* **Permission:** None
* **Syntax:** /optOut 
* **Description:** Toggles opt out of auto-invites
* **Category:** Common

### markAllRead

* **Permission:** None
* **Syntax:** /markAllRead 
* **Description:** Marks all messages as read
* **Category:** Common

### reqInvite

* **Permission:** None
* **Syntax:** /reqInvite [?world name...]
* **Description:** Requests an invite to a world
* **Category:** Common
* **Aliases:** requestInvite

### getSessionOrb

* **Permission:** None
* **Syntax:** /getSessionOrb [?world name...]
* **Description:** Get session orb
* **Category:** Common

### worlds

* **Permission:** None
* **Syntax:** /worlds 
* **Description:** List all worlds
* **Category:** Common

### shutdown

* **Permission:** Owner
* **Syntax:** /shutdown 
* **Description:** Shutdown Headless
* **Category:** Headless Management

### message

* **Permission:** Owner
* **Syntax:** /message [user] [message...]
* **Description:** Message a user
* **Category:** Headless Management

### contacts

* **Permission:** Owner
* **Syntax:** /contacts 
* **Description:** List headless contacts
* **Category:** Headless Management

### addContact

* **Permission:** Owner
* **Syntax:** /addContact [user]
* **Description:** Add contact
* **Category:** Headless Management

### removeContact

* **Permission:** Owner
* **Syntax:** /removeContact [user]
* **Description:** Remove contact
* **Category:** Headless Management

### throwErr

* **Permission:** Owner
* **Syntax:** /throwErr 
* **Description:** Throw Error
* **Category:** Debug

### throwErrAsync

* **Permission:** Owner
* **Syntax:** /throwErrAsync 
* **Description:** Throw Error Asynchronously
* **Category:** Debug

### setPerm

* **Permission:** Moderator
* **Syntax:** /setPerm [user] [level]
* **Description:** Sets a user's permission level
* **Category:** Moderation

### getPerm

* **Permission:** Moderator
* **Syntax:** /getPerm [?user]
* **Description:** Get user permission level
* **Category:** Moderation

### users

* **Permission:** Moderator
* **Syntax:** /users [?world name...]
* **Description:** List users in a world
* **Category:** World Management

### saveWorld

* **Permission:** Moderator
* **Syntax:** /saveWorld [?world name...]
* **Description:** Saves a world
* **Category:** World Management

### startWorldTemplate

* **Permission:** Moderator
* **Syntax:** /startWorldTemplate [template name] [?SessionAccessLevel]
* **Description:** Start a new world from a template
* **Category:** World Management
* **Aliases:** startTemplateWorld

### startWorld

* **Permission:** Moderator
* **Syntax:** /startWorld [world name] [?SessionAccessLevel]
* **Description:** Start a world
* **Category:** World Management

### worldTemplates

* **Permission:** Moderator
* **Syntax:** /worldTemplates 
* **Description:** List world templates
* **Category:** World Management

### startWorldUrl

* **Permission:** Moderator
* **Syntax:** /startWorldUrl [record url] [?SessionAccessLevel]
* **Description:** Start a world from a url
* **Category:** World Management

### startWorldOrb

* **Permission:** Moderator
* **Syntax:** /startWorldOrb [?SessionAccessLevel]
* **Description:** Start a world from a world orb
* **Category:** World Management

### role

* **Permission:** Administrator
* **Syntax:** /role [user] [role name] [?world name...]
* **Description:** Set role for a user in a world
* **Category:** World Management

### roleSelf

* **Permission:** Administrator
* **Syntax:** /roleSelf [role name] [?world name...]
* **Description:** Set own role
* **Category:** World Management

### addWorld

* **Permission:** Moderator
* **Syntax:** /addWorld [world name] [?world url]
* **Description:** Add a world to the world roster list
* **Category:** World Management

### removeWorld

* **Permission:** Moderator
* **Syntax:** /removeWorld [?world name...]
* **Description:** Remove a world from the world roster list
* **Category:** World Management

### listRosterWorlds

* **Permission:** Moderator
* **Syntax:** /listRosterWorlds 
* **Description:** List all worlds in the world roster list
* **Category:** World Management

### listWorlds

* **Permission:** Moderator
* **Syntax:** /listWorlds 
* **Description:** List all worlds in the world roster and in the world presets
* **Category:** World Management

### closeWorld

* **Permission:** Moderator
* **Syntax:** /closeWorld [?world name...]
* **Description:** Close a world
* **Category:** World Management

### setSessionAccessLevel

* **Permission:** Moderator
* **Syntax:** /setSessionAccessLevel [access level] [?hidden] [?world name...]
* **Description:** Set the access level of a session
* **Category:** World Management

### hideFromListing

* **Permission:** Moderator
* **Syntax:** /hideFromListing [hidden] [?world name...]
* **Description:** Sets whether the session should be hidden from listing or not
* **Category:** World Management

### setSessionName

* **Permission:** Moderator
* **Syntax:** /setSessionName [?target world...]
* **Description:** Set the name of a session
* **Category:** World Management
* **Aliases:** setWorldName, worldName, sessionName

