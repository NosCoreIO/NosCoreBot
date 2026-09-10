# NosCoreBot #

<p align="center">
  <img width="250px" src="https://github.com/NosCoreIO/NosCore.Packets/blob/15.0.1/icon.png"/>
</p>

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/5601a6d1e4224338b142c76bdeb8f9eb)](https://app.codacy.com/app/NosCoreIO/NosCoreBot?utm_source=github.com&utm_medium=referral&utm_content=NosCoreIO/NosCoreBot&utm_campaign=Badge_Grade_Settings)
[![.NET](https://github.com/NosCoreIO/NosCoreBot/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/NosCoreIO/NosCoreBot/actions/workflows/dotnet.yml)

# Special Thanks for Contributions #
<p align="left">
<a href="https://www.navicat.com"><img height="100px" src="https://user-images.githubusercontent.com/35202750/207230064-dcf23adc-9e96-4481-9a53-cd212f5bd60e.png"/></a>
</p>

## You want to contribute ? ##
[![Discord](https://i.gyazo.com/2115a3ecb258220f5b1a8ebd8c50eb8f.png)](https://discord.gg/Eu3ETSw)

## You like our work ? ##
<a href='https://github.com/sponsors/0Lucifer0' target='_blank'><img height='48' style='border:0px;height:46px;' src='https://i.gyazo.com/47b2ca2eb6e1ce38d02b04c410e1c82a.png' border='0' alt='Sponsor me!' /></a>
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/A3562BQV)
<a href='https://www.patreon.com/bePatron?u=6503887' target='_blank'><img height='46' style='border:0px;height:46px;' src='https://c5.patreon.com/external/logo/become_a_patron_button@2x.png' border='0' alt='Become a Patron!' /></a>

## Sponsor leaderboard ##
`/sponsors` shows the leaderboard. Once a day the bot refreshes it from the funding platforms, posts it to a channel and applies sponsor roles.

| Variable | Purpose |
|---|---|
| `GITHUB_SPONSORS_TOKEN` | PAT with `read:user`. Without it GitHub Sponsors is skipped |
| `GITHUB_SPONSORS_LOGIN` | maintainer login, defaults to `0Lucifer0` |
| `PATREON_ACCESS_TOKEN`, `PATREON_CAMPAIGN_ID` | creator token and campaign. Without them Patreon is skipped |
| `PATREON_NAMES_PUBLIC` | `true` to show Patreon names. They are anonymised otherwise, because the Patreon API exposes no privacy flag |
| `SPONSOR_GUILD_ID`, `SPONSOR_CHANNEL_ID` | where the report is posted and roles are applied |
| `SPONSOR_REPORT_HOUR` | UTC hour of the daily post, defaults to 9 |

The `Supporter`, `Gold Supporter`, `Legendary Supporter` and `Top Sponsor` roles are created on the first sync — the bot's own role has to sit above them for it to assign them. A sponsor only gets a role once someone links their accounts with `/link-sponsor <user> github <login>`, since no funding platform exposes a sponsor's Discord identity.

Sponsors who chose to stay private are listed as `Anonymous` with their amount. GitHub lifetime totals are estimated from tier price and start date, which is all its API exposes; Patreon reports exact lifetime totals. Ko-fi is not included — it has no read API, only webhooks.

## Warning! ##
We are not responsible of any damages caused by bad usage of our source. Please before asking questions or installing this source read this readme and also do a research, google is your friend. If you mess up when installing our source because you didnt follow it, we will laugh at you. A lot.

## Instructions to contribute ##

### Disclaimer ###
This project is a community project not for commercial use. The result is to learn and program together for prove the study.

### Legal ###
This is an independent and unofficial server for educational use ONLY. Using the Project might be against the TOS.

### Contribution is only possible with Visual Studio 2026 ###
We recommend usage of :
* [Roslynator extension](https://github.com/JosefPihrt/Roslynator).
* [Resharper](https://www.jetbrains.com/resharper/)
