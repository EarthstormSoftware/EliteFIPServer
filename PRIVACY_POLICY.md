# Privacy Policy for Elite FIP Server

**Last updated:** 2026-09-30

This policy explains what Elite FIP Server ("the app") does and does not do with your data. The short version: the app runs entirely on your own PC and local network. It does not create accounts, does not send your data to the developer or any cloud service, and does not include advertising, analytics, or tracking.

## Who this policy covers

Elite FIP Server is developed by Earthstorm Software. This document describes the app's data handling for Microsoft Store submission and general use.

## What the app does

Elite FIP Server reads game state from a local, running copy of Elite Dangerous and makes that information available in two ways:

1. **Matric integration** — the app sends parsed game-state data (such as ship module status, target information, and cockpit indicator states) over your local network to the [Matric](https://matricapp.com) companion app, if you have it installed and configured. This integration is entirely local-network traffic between the app and Matric; it is not sent to the developer or to any third party.
2. **Panel/dashboard web server** — the app hosts a small local web server (by default reachable only at `http://127.0.0.1:<port>` on your own PC; optionally, if you enable it in settings, reachable from other devices on your local network) so you can view the same game-state data in a browser, e.g. on a tablet or a second screen used as a flight panel.

To do this, the app:

- Reads Elite Dangerous's local journal log files (the same files Elite Dangerous itself writes to your PC) to determine current game state.
- Reads a single Windows registry value (`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\...`) and, if needed, checks whether Matric is currently running, to locate an installed copy of the Matric application on your PC, if present. This information is not transmitted anywhere.
- If you turn on **Start with Windows** in Settings (it is off by default), registers the app to start when you sign in. The Microsoft Store version uses the standard Windows startup setting, which you can also change in Task Manager or Settings › Apps › Startup; other versions add a single entry under `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`, which is removed when you turn the option off. Apart from this, the app writes nothing to the registry.
- If you turn on logging in Settings (it is off by default), writes diagnostic log files to `%AppData%\EliteFIPServer\` on your PC (the Microsoft Store version may keep this folder in the app's own storage instead), rotated daily and deleted after 14 days, for troubleshooting purposes. Errors are also recorded in the Windows Event Log on your PC. These logs stay on your PC and are not automatically sent anywhere; you may be asked to share them manually if you report a bug.
- Stores your app preferences (such as panel server port, whether LAN access is enabled, and Matric button text) locally on your PC.

## What the app does not do

- It does not require or create a user account, sign-in, or Frontier account credentials.
- It does not transmit your game data, journal contents, logs, or settings to the developer or to any cloud or third-party service.
- It does not include advertising, analytics, crash-reporting services, or usage tracking.
- It does not load fonts, scripts, or other content from the internet.
- It does not sell or share your data, because it does not collect any data off your device in the first place.

## Third-party content

None. The dashboard's pages, scripts and fonts are all served by the app itself, so opening the dashboard in a browser does not contact Google or any other third party. The QR code shown for the dashboard's network address is also created on your PC.

## Children's privacy

Elite FIP Server is a companion utility for the game Elite Dangerous and is not directed at children. It does not knowingly collect personal information from anyone, regardless of age, because it does not collect personal information at all.

## Changes to this policy

If this policy changes, an updated version will be published at this same location with a revised "Last updated" date.

## Contact

Questions about this policy or the app's data handling can be sent to: support@earthstormsoftware.com
