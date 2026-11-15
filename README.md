<p align="center">
  <img width="200" alt="NosCore" src="https://raw.githubusercontent.com/NosCoreIO/NosCore.Packets/master/icon.png"/>
</p>

<h1 align="center">NosCoreBot</h1>

<p align="center">
  <a href="https://github.com/NosCoreIO/NosCoreBot/actions/workflows/dotnet.yml"><img alt="Build" src="https://github.com/NosCoreIO/NosCoreBot/actions/workflows/dotnet.yml/badge.svg?branch=master"/></a>
  <a href="https://discord.gg/Eu3ETSw"><img alt="Discord" src="https://img.shields.io/badge/Discord-join-5865F2?style=flat-square&logo=discord&logoColor=white"/></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"/></a>
</p>

The NosCore Discord bot: help and moderation commands, a points leaderboard, translator roles, and an hourly job that
publishes the latest parser input files when the client's data archives change.

## Commands

A command starts with `/` (`/help`), or follows a mention of the bot.

| Command | What it does |
|---|---|
| `/help` | Lists the commands |
| `/add-points`, `/info-points`, `/leaderboard` | Gives, shows and ranks members' points |
| `/delete-message <amount>` | Deletes the last messages |
| `/clear` | Replaces the channel with an empty copy that keeps its settings, permissions and webhooks |
| `/translation <language>` | In `translation-info` only: gives that language's translator role, or takes it back if you already have it |
| `/reset-i18n` | Empties the list of missing translations |

## Running

The bot reads its settings from the environment: `token` (the Discord bot token); `S3_ACCESS_KEY`,
`S3_SECRET_KEY` and `S3_BUCKET` for the bucket it publishes the parser input files to; and `S3_KEY`, the object
`reset-i18n` writes. `RUN_ONCE=true` runs the publishing job once and exits.

The points commands read `contribution.json` from the bucket and never create it, so upload one holding `[]` before
using them on a new bucket.

```bash
dotnet build
dotnet build/net11.0/NosCoreBot.dll
```

## Contributing

Questions, ideas and pull requests are welcome; most of the discussion happens on [Discord](https://discord.gg/Eu3ETSw).
Any editor that handles .NET 11 works: `dotnet build` and `dotnet test` run on Windows, Linux and macOS.
In Visual Studio, [Roslynator](https://github.com/dotnet/roslynator) and
[ReSharper](https://www.jetbrains.com/resharper/) help keep to the code style.

## Supporting NosCore

NosCore is a spare-time open source project. Sponsoring buys time spent on the emulator and the packages around it,
and gives the **Supporter** role on Discord.

<p>
  <a href="https://github.com/sponsors/0Lucifer0"><img alt="Sponsor on GitHub" src="https://img.shields.io/badge/GitHub-sponsor-EA4AAA?style=for-the-badge&logo=githubsponsors&logoColor=white"/></a>
  <a href="https://ko-fi.com/A3562BQV"><img alt="Support on Ko-fi" src="https://img.shields.io/badge/Ko--fi-support-FF5E5B?style=for-the-badge&logo=kofi&logoColor=white"/></a>
  <a href="https://www.patreon.com/bePatron?u=6503887"><img alt="Become a patron" src="https://img.shields.io/badge/Patreon-become_a_patron-F96854?style=for-the-badge&logo=patreon&logoColor=white"/></a>
</p>

Thanks to [Navicat](https://www.navicat.com) for supporting the project.

<a href="https://www.navicat.com"><img height="60" alt="Navicat" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>

## Legal

NosCore is an independent, unofficial project for educational use. It is not affiliated with, authorized, maintained,
sponsored or endorsed by Gameforge or any of its affiliates; NosTale is a trademark of Gameforge. Using it may be
against the terms of service of NosTale servers.

Released under the [MIT License](LICENSE).
