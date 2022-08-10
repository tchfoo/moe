<h1 align="center">Moe-Bot</h1>
<h4 align="center">A multi-purpose (private) Discord bot made using Discord.NET</h4>

Moe is a Discord bot made for my friend's private server. The main goal was to utilise Discord's new bot API features such as slash commands, message and user commands, modals, select menus, buttons, responses and ephemeral responses and so on. It turned out to be pretty usable, and while it might not be as feature-rich as the other multi-purpose bots out there, it could replace almost all of the bots on that small server.

The bot is self-hosted on a Raspberry Pi, and because it is made for a private server, it cannot be invited to other servers. However, you are free to take a look at its source code and run it yourself.

# Features

The bot has numerous features, many of which can be found in other bots, but there are some unique features as well.

- Moderation
  - Message purging
  - Logging to a specified channel (message remove, edit, voice channel leave/join/change, member join/leave, member ban/unban)
  - Modranks: a three-level permission system, where each command can only be used by specified moderator ranks "modranks".
- Leave messages
- Remembers roles after a user leaves, so the roles can be given back if the user joins again
- Applicable roles: users can apply and remove a set of roles from themselves
- Leveling: users gain XP and level up based on their chat activity
  - Also has No-XP feature to restrict users with a specified role from gaining XP
  - Optionally, a user can toggle the levelup message for themselves (if they find it annoying)
- Custom Commands: moderators can add custom commands to a server which prints message(s) out
  - Custom commands are not slash commands, they are simple text-based commands with a prefix which can be set in the settings.
- RNG: a simple random number generator
- User Info: prints information about the user's account and their server profile.
  - Also stores the date and time when the user first joined the server.
- Server Info: prints information about the server.
- Timezone converter
- Say: the bot repeats your message
- Embed: moderators can use a built-in embed builder and send an embed using the bot
- Message Pinning: users can pin messages to a specified pin channel
  - Works sort of like a starboard, except the message only requires a single message command to pin
  - Note: there is no limitation on who can pin messages
- Announce: administrators can create templates made out of an embed that later can be announced to a specified channel
  - Works sort of like the embed builder, except the embed is saved as a template
    - Placeholders can be placed in the embed with `$(placeholder name)`, which later can be filled upon running the announce command
    - Templates need to have a name and a channel to send into
    - Templates can also have an optional mention argument, where the bot mentions a specified role with the announcement
    - Templates can be optionally hidden, so they won't show up in the template list
- Snapshot: administrators can save the state of the server (name, picture, channel names, voice channel names, role names) and restore it later
  - A snapshot must have a name as identifier
  - The snapshot command always saves the server's name, the others are optional
- Settings: administrators can change the settings of the bot for the server
  - Pin channel: channel where the pinned messages get sent (using the Message Pinning feature)
  - Log channel: channel where to log certain events
  - Custom command prefix
  - Bot Administrator ranks: the Administrator "modrank" required to run dangerous commands
  - Bot Moderator ranks: the Moderator "modrank" required to run moderation commands
  - No-XP role: the role that cannot receive XP in the leveling system
  - Leave message: what the bot should say after a user leaves the server and in which channel
  - Time zone: set a default timezone for the timezone converter command

