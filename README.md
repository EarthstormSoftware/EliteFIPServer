# EliteFIPServer

Elite FIP Server is a .NET app which uses [EliteJournalReader](https://github.com/MagicMau/EliteJournalReader) to 
read Elite Dangerous game information, and then feed it to [Matric](https://matricapp.com) via the Matric 
Integration API.

This allows Matric to reflect the current game state in the UI, with particular respect to button/toggle state 
(such as Landing Gear or Lights), but also other information like current target data. Using a custom touch
screen display rather than a traditional keyboard increases immersion and lowers the requirement to remember
all the many key bindings you might need.

Current build is available here: [v1.1.0](https://github.com/EarthstormSoftware/EliteFIPServer/releases/tag/v1.1.0)

## Runtime Pre-requisites
- Elite FIP Server is a .Net 5.0 application and requires the appropriate [runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime) 
  to be installed 

- [Matric and the MatricIntegration.dll](https://matricapp.com). Elite FIP Server only supports integration
  with Matric, and the MatricIntegration.dll is currently provided in the beta release zip file for 
  convienience. The intention is to remove it from a future package as it is included with Matric server.

## Build Pre-Requisites
Aside from various libraries which VS will highlight if missing, and which are available via Nu Package Manager,
EliteFIPServer requires the following:

- [EliteJournalReader](https://github.com/MagicMau/EliteJournalReader)
- [EliteFIPProtocol](https://github.com/EarthstormSoftware/EliteFIPProtoco)
- [MatricIntegration.dll](https://matricapp.com)

---

## Usage
Use at own risk :)
1. Download the zip file, uncompress it to a suitable location.
2. Start Matric and connect a client.
3. Enable API Integration in Matric (Settings > API Integration > Enable 3rd party integration)
4. Run the EliteFIPServer.exe file
5. By default, the server will connect to Matric and start parsing game data


### Matric Authorisation
On first run the Server will request authorisation to connect to Matric and prompt for the PIN.
The PIN is provided by Matric in the authorisation window. Note it, and accept the auth request before entering 
the PIN into the Elite FIP Server prompt. The PIN will be saved for future use, and can be changed via the 
'Settings' panel.

### Matric Client Id
On the first run, Elite FIP Server will default to using the first client in the client list, and this
client is saved for future use so it will always be used if available, regardless of whether it is the first
in the list. If on subsequent runs that client is not available, the first in the list will be used, but will 
not be saved.

You can change the default client in the 'Settings' panel, using the Client Id string. A list of currently 
connected clients is provided and you can copy/paste the ID using the standard copy/paster hot keys. 
There is no mouse menu (yet).

### Immediate Start
In the 'Settings' panel the Immediate Start option determines if the server will connect to Matric and start
parsing game files immediately after it is started. This is the default. You can start and stop the processing
of data manually with the buttom in the main screen.

### Enable Logging
In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. When enabled, the log is located in the User AppData\Roaming\EliteFIPServer folder.
For example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer


---

# Current Features
EliteFIPServer should work with any Deck (a sample deck is available [here](https://community.matricapp.com/deck/344/elite-dangerous-fip)), and will toggle button state
to on/off or set text based on the Matric button name assigned. Therefore to use this with your own deck, simply 
set the button name for your button to match the one listed and it will be toggled based on Elite state.

### Standard Buttons

Elite Function | Matric Button name
-------------- | -----------
Landing Gear   | btnGear
Supercruise*   | btnSupercruise
Flight Assist  | btnFlightAssist
Hardpoints     | btnHardpoints
Cargo Scoop    | btnScoop
Ship Lights    | btnLights
Night Vision   | btnNVision
Silent Running | btnSilent
HUD Mode*      | btnHudMode
FSD Jump*     | btnJump

\* Supercruise will only illuminate when actually in Supercruise (after charging and the initial short FSD jump to get there)

\* HUD Mode integration will set button text to "Combat" or "Analysis" as appropriate - this is not configurable (yet).

\* FSD Jump indicator is also used when entering Supercruise, so will illuminate briefly at that point.

### Indicator Lights
These displays are intended for information only, to show simple status data where no button control is expected. 

Elite Status | Matric Button name
-------------- | -----------
Docked (at a station) | indDocked
Landed (on a planet) | indLanded
Shields Online | indShields
In wing | indInWing
Scooping fuel | indScoopingFuel
Mass locked | indFSDMassLock
FSD Charging | indFSDCharging
FSD Cooldown | indFSDCooldown
Low fuel | indLowFuel
Overheat | indOverheat
In danger | indInDanger
Being interdicted | indInterdiction


### Text Displays

These displays require a text button, of sufficient width and height to display the full text. If the text button
is not large enough for the content, the behaviour is 'undefined'.
Text size is per standard Matric setting, but each field is defined on a new line. 
The labels match the target field.

Elite Information | Matric Button name
-------------- | -----------
Target data (Ship type, Faction, Rank etc)   | txtTarget
Target labels  | txtTargetLabel
Status data (Legal state, Cargo weight, Fuel etc)   | txtStatus
Status labels  | txtStatusLabel


---
# Change History

### 1.1.0          
- Added Status text and labels
- Added Flight Assist, SuperCruise and FSD Jump button support
- Added indicator support for various status flags

### 1.0.0
- Initial Version


---
# Thanks to...
- The developers and contributors to EliteJournalReader
- AnarchyZG - the developer of Matric, both for the software and the support
-  The developers and contributors to all the other open tools and packages that made this feasible. 




