# FF14 Discord Relay

FF14 Discord Relay is a small Windows utility that reads selected FFXIV chat
lines from local ACT log files and forwards them to a Discord webhook.

It does not hook the Discord client, modify FFXIV, read game memory, or alter
network packets. The relay works only with local ACT log files and a webhook URL
provided by the user.

## Features

- Reads selected ACT FFXIV chat channels.
- Can forward only the current player's own messages.
- Detects the current character name from ACT log data.
- Updates the nickname while running when the logged-in character changes.
- Sends messages through a Discord webhook.
- Suppresses Discord mentions by default.
- Queues messages when Discord rate limits are hit, then sends the backlog as
  grouped messages after the retry delay.
- Stores settings encrypted under the current Windows user's AppData folder.
- Supports single-instance behavior and restores the settings window when a
  second copy is launched.

## Privacy

Webhook URLs and settings are stored locally in encrypted form:

```text
%APPDATA%\FF14DiscordRelay\config.enc
```

Do not publish or share your own `config.enc`, ACT logs, webhook URL, character
names, or screenshots that include private chat.

## Requirements

- Windows x64
- Advanced Combat Tracker with FFXIV log output
- Discord webhook URL

Self-contained ZIP builds include the .NET runtime. Framework-dependent builds
require the matching Microsoft .NET Desktop Runtime.

## ACT Input

Auto detection checks the running ACT process and common ACT configuration files
under:

```text
%APPDATA%\Advanced Combat Tracker\Config
```

The relay watches ACT log lines such as:

```text
00|timestamp|code|name|line|...
[time] ChatLog 00:code:name:line
```

Nickname detection prefers ACT network line `02`:

```text
02|timestamp|actorId|characterName|...
```

If that line is not available, recent chat senders from the selected channels
are used as fallback candidates.

## Discord Output

The restricted default message template is:

```text
[{name} say] : {message}
```

Unlocked mode allows custom templates using:

```text
{channel}
{code}
{name}
{message}
```

## Building

Build:

```powershell
dotnet build -c Release
```

Self-contained folder publish:

```powershell
dotnet publish -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o publish\win-x64-share-friendly --force
```

Single-file publish:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishReadyToRun=false -o publish\win-x64-portable --force
```

## Distribution Notes

Unsigned Windows executables can trigger antivirus, SmartScreen, or Discord file
warnings. Prefer publishing releases through a stable project page such as
GitHub Releases and include SHA256 hashes for downloadable files. For broader
distribution, use a trusted code-signing certificate.
