
## How it works

**Wakeup Machine** can be useful for a remote worker who has a corporate computer in theirs local network. And he or she has to get access sometimes or on regular basis to his or her computer.
If your company does not have any mechanism to turn on a local computer, e.g. a router with Wake On Lan feature, or Home Assistant server you have only two options:
1. Ask a colleague in the office to turn your computer on
2. Just keep your computer always turned on

The first option is straight way to get hated.
The second one is wasting machine resource and even tiny (sometimes not) but unreasonable increasing power bills.
You can avoid both of them with remote managing power supply of your machine.

This is a conceptual schema of working Wakeup Machine:

![Wakeup Machine][schema]

**The idea is using one device inside local network as WakeUp Machine service.** Usually it's not a big problem because at least one machine (some server) in the network is always turned on.
If you don't have this probably WakeUp Machine is not your option or you should consider buying a cheap single-board computer (Raspberry PI or so one) and the using it as Wakeup Machine service.

<br/>

## Getting started

There are **three main steps** you as administrator have to complete: 

1. **Create your own Telegram bot.** It's UI for users to manage power supply theirs machines via text messages.
2. **Install Wakeup Machine service.** The service is an application that listens to user messages and executes theirs commands. 
3. **Configure service.** Administrator has to configure the service after its install. Add users and assign them to their computers.

### Create Telegram bot

Please, visit the official guide:
https://core.telegram.org/bots/features#botfather

After you create a new bot you get token like this `110201543:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw`. This is what you need store for the next step.

### Install Wakeup Machine

Currently there is only Windows installer. You can download from [release page](https://github.com/nomba/wakeup-machine/releases/latest). But the application is being developed with .NET 6 and it can be built almost on any platforms. [All .NET supported OS](https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md). You can do it yourself or just submit a request to me in [Github Issues](https://github.com/nomba/wakeup-machine/issues).

1. Download [latest installer](https://github.com/nomba/wakeup-machine/releases/latest).
2. Run `WakeUpMachine.Setup.*.exe`.
3. Specify the bot token on first page of installer:

![Wakeup Machine][installer]

**All application settings are stored in** `{ProgramData}/appSettings.json` initially created by installer. 

**WARNING: Currently upgrade mode (when app is already installed) doesn't work well. You should backup `appSettings.json` before run a new installer.**

### Configure Wakeup Machine

Administrator installs Wakeup Machine service and then has to configure it. Configuring consists of two steps:

1. Make corresponding changes `appSettings.json`
2. Apply changes by command `WakeUpMachine.Service.exe --configure`

CURRENT TRICK: Before run `WakeUpMachine.Service.exe --configure` you should stop Windows service `WakeUpMachine` and then restart it

#### Settings spec

**After installer did its job, administrator has to configure at least `Settings.Users` section in `appSettings.json`**

```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "EventLog": {
      "SourceName": "Wake Up Machine",
      "LogName": "Application",
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "Settings": {
    "BotToken": "110201543:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw",
    "PingTimeoutSec": 30,
    "ReassignUserMachine": false,
    "DefaultNetMask": "255.255.255.0",
    "Users": [
      {
        "TelegramUserId": 123456,
        "FullName": "John Ivanov",
        "Machine": {
          "Ip": "111.111.1.11",
          "Mac": "00-11-22-33-44-FF"
        }
      }
    ]
  }
}
```

1. `Logging` sets Windows event log. Currently avoid changing this section.
2. `Settings.BotToken` initially set by installer. Can be changed.
3. `Settings.PingTimeoutSec` timeout after `/wakeup` command to check computer status (Off or Online).
4. `Settings.ReassignUserMachine` if it's `true` all already assigned user will be reassigned with new settings. By default it's `false` all "old" user are skipped.
5. `Settings.DefaultNetMask` determines mask for local network. By default it equals `255.255.255.0`. It needs when calculate broadcast address for WakeOnLan packet.
6. `Settings.Users` main section. `TelegramUserId` is required. It can be get from the chat between corresponding user and bot while user is not assigned yet. User must send it to administrator. `FullName` is optional.
7. `Settings.Users.[].Machine`. `Ip` IP address user's computer. `Mac` MAC address of computer network adapter. Administrator can specify only IP if on configuring user's computer is turned on. In this case app detects MAC itself.

#### Administrator steps

1. Create Telegram bot.
2. Install Wakeup Machine service.
3. Share bot via Telegram with colleagues (users).
4. Send to users instructions (e.g. [how-to-configure-wake-on-lan-on-windows](https://www.unifiedremote.com/tutorials/how-to-configure-wake-on-lan-on-windows)) to check Wake On Lan (WOL) activated on their computers.
5. Get Telegram user ID from users who wants to use Wakeup Machine and has activated WOL. 
6. Make `User` object in `Users` section for each user.
7. Stop Windows service
8. Run `WakeUpMachine.Service.exe --configure`
9. Start Windows service

[schema]:/docs/wakeup-machine.drawio.svg
[installer]:/docs/wakeup-machine.installer.png
