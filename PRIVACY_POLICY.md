# Privacy Policy for Elite FIP Server

**Last updated:** 2026-09-13

This policy explains what Elite FIP Server ("the app") does and does not do with your data. The short version: the app runs entirely on your own PC and local network. It does not create accounts, does not send your data to the developer or any cloud service, and does not include advertising, analytics, or tracking.

## Who this policy covers

Elite FIP Server is developed by Earthstorm Software. This document describes the app's data handling for Microsoft Store submission and general use.

## What the app does

Elite FIP Server reads game state from a local, running copy of Elite Dangerous and makes that information available in two ways:

1. **Matric integration** — the app sends parsed game-state data (such as ship module status, target information, and cockpit indicator states) over your local network to the [Matric](https://matricapp.com) companion app, if you have it installed and configured. This integration is entirely local-network traffic between the app and Matric; it is not sent to the developer or to any third party.
2. **Panel/dashboard web server** — the app hosts a small local web server (by default reachable only at `http://127.0.0.1:<port>` on your own PC; optionally, if you enable it in settings, reachable from other devices on your local network) so you can view the same game-state data in a browser, e.g. on a tablet or a second screen used as a flight panel.

To do this, the app:

- Reads Elite Dangerous's local journal log files (the same files Elite Dangerous itself writes to your PC) to determine current game state.
- Reads a single Windows registry value (`HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\...`) to locate an installed copy of the Matric application on your PC, if present. Nothing is written to the registry, and this information is not transmitted anywhere.
- Writes diagnostic log files to `%AppData%\EliteFIPServer\` on your PC, automatically rotated and deleted after 14 days, for troubleshooting purposes. These logs stay on your PC and are not automatically sent anywhere; you may be asked to share them manually if you report a bug.
- Stores your app preferences (such as panel server port and whether LAN access is enabled) locally on your PC using standard Windows application settings.

## What the app does not do

- It does not require or create a user account, sign-in, or Frontier account credentials.
- It does not transmit your game data, journal contents, logs, or settings to the developer or to any cloud or third-party service.
- It does not include advertising, analytics, crash-reporting services, or usage tracking.
- It does not sell or share your data, because it does not collect any data off your device in the first place.

## Third-party content

The web dashboard's stylesheet loads a font from Google Fonts (`fonts.googleapis.com`) when the dashboard page is opened in a browser. Loading this font causes your browser to make a direct request to Google, which may see the requesting device's IP address, subject to [Google's own privacy policy](https://policies.google.com/privacy). This only happens when you actively open the dashboard page in a browser; no other part of the app contacts Google or any other third party.

## Children's privacy

Elite FIP Server is a companion utility for the game Elite Dangerous and is not directed at children. It does not knowingly collect personal information from anyone, regardless of age, because it does not collect personal information at all.

## Changes to this policy

If this policy changes, an updated version will be published at this same location with a revised "Last updated" date.

## Contact

Questions about this policy or the app's data handling can be sent to: support@earthstormsoftware.com
