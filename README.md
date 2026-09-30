# EliteFIPServer

Elite FIP Server is a .NET app which uses [EliteAPI](https://github.com/Somfic/EliteAPI) to 
read Elite Dangerous game information, and feed it to [Matric](https://matricapp.com) via the Matric 
Integration API. It also makes game data available via a self-hosted web server, allowing the data 
to be viewed in a browser dashboard (for example on a second screen or tablet), and the display of that data to be customised.

The integration with Matric allows Matric to reflect the current game state in the UI, with particular respect to button/toggle state 
(such as Landing Gear or Lights), and also other information like current target data. Using a custom touch
screen display rather than a traditional keyboard increases immersion and lowers the requirement to remember
all the many key bindings you might need.

Elite FIP Server v4 is a WinUI 3 app and is available from the Microsoft Store. The older WPF app (v3 and earlier) is retired and no longer supported.

If you are upgrading from a previous release, please check the runtime prerequisites and startup notes before running the app.

---

## Runtime Pre-requisites
- Elite FIP Server is a .NET 10 application. Install both of these from the [.NET 10 download page](https://dotnet.microsoft.com/download/dotnet/10.0):
  - .NET Runtime 10 (x64)
  - ASP.NET Core Runtime 10 (x64), used by the built-in Panel Server

- [Matric v2.x](https://matricapp.com) (optional, only needed for Matric integration)  
  Elite FIP Server uses the MatricIntegration.dll that ships with Matric. It is not included with Elite FIP Server;
  the app finds it in your Matric installation folder automatically.

---

## Build Pre-Requisites
Aside from various libraries which Visual Studio will highlight if missing, and which are available via NuGet,
Elite FIP Server requires the following:

- [EliteAPI](https://github.com/Somfic/EliteAPI), built in Release, in a sibling folder (`..\EliteAPI`)
- [EliteFIPProtocol](https://github.com/EarthstormSoftware/EliteFIPProtocol), built in Release, in a sibling folder (`..\EliteFIPProtocol`)
- MatricIntegration.dll. This is Ex Machina's library and is not included in this repository. The build uses the first of:
  1. a path passed with `-p:MatricIntegrationPath=<path to MatricIntegration.dll>`
  2. a copy you place in `libs\Matric\MatricIntegration.dll` (gitignored)
  3. an installed [Matric Desktop](https://matricapp.com) (`C:\Program Files\Ex Machina\MATRIC Desktop`)

To build the app, run from the repository root:

```powershell
.\Build-UI.ps1
```

or build `EliteFIPServer.UI\EliteFIPServer.UI.csproj` directly. Every build increments the build number and updates
`EliteFIPServer.Version.props`, `EliteFIPServer.UI\BuildInfo.cs` and `EliteFIPServer.UI\Package.appxmanifest`.

Older versions of Elite FIP Server used the EliteJournalReader project to provide in-game events.

---

## Usage
Use at own risk :)
1. Install Elite FIP Server from the Microsoft Store, or build it from this repository.
2. Start Matric and connect a client.
3. Enable API Integration in Matric (Settings > API Integration > Enable 3rd party integration). Please note that PIN authorisation is no longer supported.
4. Start Elite FIP Server (`EliteFIPServer.exe` if you built it yourself).
5. Matric Integration is not enabled by default. You can start it from the Status tab, and set it to start automatically in Settings > Matric integration.
6. The Panel Server (which publishes game data via a built-in web server) can also be started from the Status tab, or with the Open dashboard button,
   and set to start automatically in the Settings tab.

Help for each screen is available from the ? button at the top right of the app, or by pressing F1.

### Matric Authorisation
Elite FIP Server v2 does not support Matric PIN authorisation. Please disable this in Matric.

### Matric Clients
Elite FIP Server no longer restricts users to a single Matric Client device - all connected Matric clients 
will receive game state updates

### Enable Logging
In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. When enabled, the log is located in the User AppData\Roaming\EliteFIPServer folder.
For example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer

### Matric Integration Settings
Settings > Matric integration lets you:
- start Matric Integration automatically when Elite FIP Server starts
- change the Matric API port and the retry interval between connection attempts
- switch all connected Matric clients to a page of their deck automatically when you enter a game state
- set up client profiles, which override deck and page settings for individual Matric devices (client IDs are shown on the Clients tab)
- customise button text (see below)

If Matric isn't running when Matric Integration starts, the connection attempt fails and Matric Integration returns to Stopped.
Start Matric, then start Matric Integration again from the Status tab.

### Autostart Panel Server
The Settings tab allows the Panel Server to be started when Elite FIP Server starts. See [Panel server](docs/panel-server.md) for more information.

### Enable Custom Button Text
Elite FIP Server can change the text of a button, as well as its state, when game state changes. To set this up, go to
Settings > Matric integration > Button text. For each supported button (the names match those in the feature section below),
set the Off and On text and tick Update to enable it. For example, for HudMode you could set the Off text to "Combat" and the
On text to "Analysis". Reset to defaults restores the original text.

Your button text is saved in `%AppData%\EliteFIPServer\MatricButtonTextConfig.json`.


---

## Known Issues

 - When you arrive at the final destination system of a planned route, the route information will disappear during the final hyperspace
   jump. This is working as intended, and is due to the sequence of events emitted by Elite Dangerous. Essentially Elite clears the route
   automatically during the last jump, before the arrival event in the final system, resulting in the route being cleared in FIP server.
   To mitigate this, Elite FIP server provides a 'Previous Route' which will show the completed route if desired. See 
   [Panel server](docs/panel-server.md) for more information
   

---

## Upgrading from v1.x

- Elite FIP Server v2.x and later include changes that will cause some Matric decks/existing buttons to not function as expected.
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
Used to create Gauges. Specific values will depend on the button.

#### Text (txt)
A flat text field, used for labels and text information like target data. These are special cases and information is provided below.

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
 | | | | |   
On Foot | OnFoot | x | x | | 
In Taxi | InTaxi | x | x | | 
In Multicrew | InMulticrew | x | x | | 
On Foot (Station) | OnFootInStation | x | x | | 
On Foot (Planet) | OnFootOnPlanet | x | x | | 
Aim Down Sight | AimDownSight | x | x | x | x 
Low Oxygen | LowOxygen | x | x | | 
Low Health | LowHealth | x | x | | 
Cold | Cold | x | x | | 
Hot | Hot | x | x | | 
Very Cold | VeryCold | x | x | | 
Very Hot | VeryHot | x | x | | 


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
Text size is per standard Matric setting, but for text which combines multiple Elite data points in one display, each field is defined on a new line. 


---
# Change History

### v4.0
- New WinUI 3 desktop app, available from the Microsoft Store
- Updated to .NET 10
- New unified browser dashboard with cockpit, navigation and combat views, passive display and tablet modes,
  named layouts, and single-widget embeds
- Much more game data on the dashboard (including loadout, cargo, materials, missions, stations, exploration and docking)
- Matric: automatic page switching, per-client profiles, and button text editing in the app
- Optional LAN access for the Panel Server
- MatricIntegration.dll is now found in the Matric installation folder instead of being copied next to the app

### v3.2.0
- Significant internal refactoring
- Refreshed user interface
- Improved Route and Location Handling (including new examples and visualisation in Panel Server)
- Bug fixes and tweaks


### v3.1.0
- Added installer and application icons
- Added first pass of Navigation Route and Location data to Panel Server
- Changed data source from EliteJournalReader to EliteAPI
- Bug fixes and various optimisations and quality of life improvements (inc. fixing InSrv state)

### v3.0.0
- Added Panel Server 

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
- Added additional Status indicators



### 1.1.0          
- Added Status text and labels
- Added Flight Assist, SuperCruise and FSD Jump button support
- Added indicator support for various status flags

### 1.0.0
- Initial Version


---
# Thanks to...
- Somfic and all the contributors to EliteAPI
- AnarchyZG - the developer of Matric, both for the software and the support
- The developers and contributors to EliteJournalReader
- The developers and contributors to all the other open tools and packages that made this feasible. 




