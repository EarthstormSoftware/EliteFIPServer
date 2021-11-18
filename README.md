# EliteFIPServer

Elite FIP Server is a .NET app which uses [EliteJournalReader](https://github.com/MagicMau/EliteJournalReader) to 
read Elite Dangerous game information, and then feed it to [Matric](https://matricapp.com) via the Matric 
Integration API.

This allows Matric to reflect the current game state in the UI, with particular respect to button/toggle state 
(such as Landing Gear or Lights), but also other information like current target data. Using a custom touch
screen display rather than a traditional keyboard increases immersion and lowers the requirement to remember
all the many key bindings you might need.

Current build is available here: [v2.1.0](https://github.com/EarthstormSoftware/EliteFIPServer/releases/tag/v2.1.0)

If you are upgrading from a previous section, please double check the Runtime Pre-requisities and any upgrade notes as they might change from version to version.

---

## Runtime Pre-requisites
- Elite FIP Server is a .NET 6.0 application and requires the appropriate [runtime](https://dotnet.microsoft.com/download/dotnet/6.0/runtime) 
  to be installed 

- [Matric v2.x and the MatricIntegration.dll](https://matricapp.com)  
  Elite FIP Server supports integration with Matric v2.x via the MatricIntegration.dll provided in the Matric installation folder. 
  This DLL **must** be copied to the Elite FIP Server folder or an exception will occur.

---

## Build Pre-Requisites
Aside from various libraries which VS will highlight if missing, and which are available via Nu Package Manager,
EliteFIPServer requires the following:

- [EliteJournalReader](https://github.com/MagicMau/EliteJournalReader)
- [EliteFIPProtocol](https://github.com/EarthstormSoftware/EliteFIPProtoco)
- [MatricIntegration.dll](https://matricapp.com)

---

## Usage
Use at own risk :)
1. Either Download the zip file and uncompress it to a suitable location, or build from source
2. Copy the MatricIntegration.dll file from the Matric installtion folder to the Elite FIP Server folder
3. Start Matric and connect a client.
4. Enable API Integration in Matric (Settings > API Integration > Enable 3rd party integration). Please note that PIN authorisation is no longer supported.
5. Run the EliteFIPServer.exe file
6. By default, the server will connect to Matric and start parsing game data

### Matric Authorisation
Elite FIP Server v2 does not support Matric PIN authorisation. Please disable this in Matric.

### Matric Clients
Elite FIP Server no longer restricts users to a single Matric Client device - all connected Matric clients 
will receive game state updates

### Immediate Start
In the 'Settings' panel the Immediate Start option determines if the server will connect to Matric and start
parsing game files immediately after it is started. This is the default. You can start and stop the processing
of data manually with the buttom in the main screen.

### Enable Logging
In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. When enabled, the log is located in the User AppData\Roaming\EliteFIPServer folder.
For example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer

### Enable Custom Button Text
To have Elite FIP server change button text when game state changes, you have to update the ButtonTextConfig.json file in the same folder
where the Elite FIP Server is run from. A sample file is provided with all currently customisable buttons (the button names match those
described in the current feature section below). You should not change the button name, only the Off/On text for those buttons you want to customise,
and the flag to enable the update for that button.

For example, to have Elite FIP Server change the button state for the HudMode button, edit the following line:
```
{"ButtonName": "HudMode", "OffText":"Hud Mode", "OnText":"Hud Mode", "UpdateButtonText": false},
```
to something like 
```
{"ButtonName": "HudMode", "OffText":"Combat", "OnText":"Analysis", "UpdateButtonText": true},
```
Save the file and either restart Elite FIP Server or go to the Settings panel and click save without changing 
any settings (which triggers a reload of the config file).


---

## Upgrading from v1.x
- The Elite FIP Server ZIP file no longer contains the MatricIntegration.dll file, this file must be copied to the Elite FIP Server folder manually.

- Elite FIP Server v2.x includes changes that will cause some Matric decks/existing buttons to not function as expected.
When upgrading from v1, please review the button name information below and change your buttons in Matric accordingly.

---

# Current Features
EliteFIPServer should work with any Deck (a sample deck is available [here](https://community.matricapp.com/deck/435/ed-fip-test)), and will update Matric objects 
based on the Matric button name assigned. To use with your own deck, set the Name field of the Matric control to the listed below as appropriate, including the prefix to 
indicate the type of button, and that control will be updated based on Elite state. Examples are below.

### Button Types
Elite FIP Server can provide several different types of button update for the same data source, to allow flexibility in designing your own deck. The button type will determine
how Elite FIP Server changes the matric component, and is defined by a three letter prefix of the Name:

#### Indicator (ind)
Changes Matric button state to on or off depending on the state in Elite. This is what might be considered a typical or standard Matric button and should be used when you need a simple toggle button (like Landing Gear), or a current state (like Mass Locked).

#### Warning (wrn)
Sets the Matric button state to off while the corresponding game state is 'off'. When the game state is 'on', the Matric button state will be toggled on and off to provide 
a flashing effect. This should be used when you need a more visible alert of current state (for example, Overheat).  

#### Button (btn)
By default, this is the same as Indicator. For Button, you can enable an additional function which will also change the text of the button as well as the state, based on the current
game state. For example. when switching between Combat and Analysis HUD modes, the button text can be set to change to show "Combat" or "Analysis" to indicate the current mode. 
See Usage Instructions for how to enable this.

#### Switch (swt) 
For use with multi-position switches. All simple toggle controls support use of a 2-way multiposition switch, with position 1 being off and 2 being on.
Specific controls supporting more than 2-way switches might be added later - if you have a specific use case, please contact the developer.

#### Slider (sld) 
Used to create Guages. Specific values will depend on the button.

#### Text (txt)
A flat text field, used for lables and text information like target data. These are special cases and information is provided below.

### Button Support Matrix

Elite Data Point | Base Matric Button Name | Indicator | Warning | Button | Switch 
-------------- | ----------- | ----------- | -------------- | ----------- | ----------- 
Docked (at a station) | Docked | x | x | | 
Landed (on a planet) | Landed | x | x | | 
Landing Gear   | LandingGear | x | x | x | x  
Shields Down* | Shields | x | x | | 
Supercruise*   | Supercruise | x | x | x | x  
Flight Assist *  | FlightAssist | x | x | x | x  
Hardpoints     | Hardpoints | x | x | x | x  
In wing | InWing | x | x | | 
Ship Lights    | Lights | x | x | x | x  
Cargo Scoop    | CargoScoop | x | x | x | x  
Silent Running | SilentRunning | x | x | x | x  
Scooping fuel | ScoopingFuel | x | x | | 
SRV Handbrake | SrvHandbrake | x | x | x | x  
SRV Turret | SrvTurret | x | x | x | x  
SRV Under Ship | SrvUnderShip|  x | x | |
SRV Drive Assist | SrvDriveAssist | x | x | x | x  
Mass locked | FSDMassLock | x | x | | 
FSD Charging | FSDCharging | x | x | | 
FSD Cooldown | FSDCooldown | x | x | | 
Low fuel | LowFuel | x | x | | 
Overheat | Overheat | x | x | | 
In danger | InDanger | x | x | | 
Being interdicted | Interdiction | x | x | | 
In Main Ship | InMainShip | x | x | | 
In Fighter | InFighter | x | x | | 
In SRV | InSRV | x | x | | 
HUD Mode | HudMode | x | x | x | x  
Night Vision  | NightVision | x | x | x | x  
FSD Jump*     | FsdJump | x | x | x | x  
SRV High Beam | SrvHighBeam | x | x | x | x  


\* Unlike the other indicators Shields and Flight Assist are typically 'active' and you want to be warned/informed if they are not. 
In these cases, the default state is 'on' and the state will change and warn if they are off. See the example deck for ideas on how this can be handled gracefully in Matric.

\* Supercruise will only illuminate when actually in Supercruise (after charging and the initial short FSD jump to get there)

\* FSD Jump indicator is also used when entering Supercruise, so will illuminate briefly at that point.

#### Examples
To indicate if Landing Gear is deployed or not, set the Name field for the control in the Matric editor to: indLandingGear
To have a flashing warning when Landing gear is deployed, set the Name field for the control in the Matric editor to: wrnLandingGear
When using a 2-way Multi-position switch for Landing gear, set the Name field for the control in the Matric editor to: swtLandingGear

### Sliders and Text Fields

Elite Data Point | Base Matric Button Name | Slider | Text | Notes
-------------- | ----------- | ----------- | -------------- | -----------
Fuel (Reservoir) | FuelReservoir | x | x | Slider is a percentage, text is actual value from game. 
Target data (Ship type, Faction, Rank etc)   | Target |  | x | Custom panel showing target information in one Matric button
Target labels  | TargetLabel |  | x | Fixed label for Target panel
Status data (Legal state, Cargo weight, Fuel etc)   | Status  |  | x | Custom panel showing status information in one Matric button
Status labels  | StatusLabel|  | x | Fixed label for Status panel


Text displays require a text button, of sufficient width and height to display the full text. If the text button
is not large enough for the content, the behaviour is 'undefined'.
Text size is per standard Matric setting, but for text which combines multiple Elite data points in one display,  eeach field is defined on a new line. 


---
# Change History

### v2.1.0
- Added Odyssey status fields 
- Added Slider capability and first 'gauge' 

### v2.0.0
- Refactored code to simplify adding new features
- Updated .NET to 6.0 (current Long Term Support version)
- Updated minimum supported Matric level to v2 (required for other changes)
- Removed PIN authentication for Matric API
- Enabled updates to all connected Matric clients
- Renamed button identifiers for consistency (breaking change)
- Added new button types (Warning and Switch)
- Added additional Status indictors



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




