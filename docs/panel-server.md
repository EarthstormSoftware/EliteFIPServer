# Elite FIP Server - Panel Server

The Panel Server component of Elite FIP Server provides the ability to view Elite game data via a web browser or
other client that can display web pages, such as the Matric iFrame control. The data is provided via a built in web server
which serves static, user-editable, HTML files, and uses JavaScript to provide a mechanism to update the data on the page as
game data changes. 

Users can modify the HTML and JavaScript to customise the display of the information to allow for their specific
use cases.


---

## Enabling the Panel Server

The Panel Server is disabled by default as a security precaution. To enable the panel server, either start it manually via the button
on the Status tab (or the Open dashboard button), or enable it to start automatically via the Settings tab.

The Panel Server uses port 4545 by default. You can change this in the Settings tab. If you do, update any bookmarks or
URLs that include the port; the provided pages themselves don't need changing.

### Access from other devices

By default the Panel Server only accepts connections from the PC it runs on (`http://127.0.0.1:4545`). To use the
dashboard on a tablet, phone or another PC, turn on **Allow LAN access** in the Settings tab. The Status tab then shows
the LAN URL to use on the other device; select the QR code button next to it and scan the code with the device's
camera to open it without typing. Windows may ask you to allow Elite FIP Server through the firewall.

### Adding the dashboard to a tablet's home screen

Open the dashboard view and layout you want on the tablet, then use the browser's **Add to Home Screen** (or
**Install app**) option. The icon reopens that same view and layout. On an iPad or iPhone it opens full screen,
like an app. Most Android browsers only allow full-screen web apps from secure (HTTPS) sites, so there the icon
opens the dashboard in a normal browser tab; use the dashboard's fullscreen button instead.

The dashboard needs no internet connection: its fonts and scripts are all served by Elite FIP Server.

### Troubleshooting

If the port chosen is in use, the Panel Server will not start and its status returns to Stopped.

The Panel Server only accepts connections from the pages it serves itself; a web page hosted anywhere else
cannot connect to it. Without Allow LAN access, it must also be opened as `localhost` or `127.0.0.1`.
If you experience problems, enable logging and check if there are any issues shown:

In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. Select 'Open log folder' in the 'Settings' panel to open the folder that holds the log.
It is normally the User AppData\Roaming\EliteFIPServer folder,
for example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer.
The Microsoft Store version may keep it in the app's own storage under AppData\Local\Packages instead,
so the button is the easiest way to find it.

---

## Checking the Panel Server is running

To confirm the Panel Server is running, use a web browser on the same PC to go to  
http://127.0.0.1:4545

From another device (with Allow LAN access turned on), use the PC's IP address, for example:  
http://192.168.169.100:4545

If the Panel Server is running, you will see the Elite FIP Server landing page,
with links to the dashboard, its views and single-panel embeds.

---

## Displaying Panels

To display the Panels, simply use a web browser, or other suitable client (Matric iFrame for example) and target the appropriate HTML page in the Core-owned `EliteFIPServer.Core/wwwroot` folder or the deployed UI output web root.

For example:  
http://192.168.169.100:4545/InfoPanel.html

## Available Panels

Default panels are provided as follows:

Panel Description | Panel FileName
-------------- | ----------- 
Status Panel (Misc general information) | StatusPanel.html 
Target Panel (Information on currently targeted ship) | TargetPanel.html
Info Panel (Extended information including Status, target and current route visualisation) | InfoPanel.html
Navigation Panel (Status and navigation information including current route visualisation) | NavPanel.html
Route Panel (Simple current route & previous route visualisation) | RoutePanel.html
Unified dashboard, complete cockpit view | Dashboard.html#cockpit
Unified dashboard, navigation view with route visualization | Dashboard.html#navigation
Unified dashboard, combat view | Dashboard.html#combat

The dashboard is a single page with directly addressable hash views. Switching between views does not reload the
document, so fullscreen state and the SignalR connection are retained. Use its Configure button to enable or disable,
move, and resize widgets. Widgets snap to a 12-column grid with rows about a twelfth of the screen high; dropping or
resizing a widget onto others pushes them down, and each widget has a minimum size so its readings stay legible.
While arranging, Done (✓) keeps the changes and Cancel (↶) discards them; the ☰ button opens layout settings, to switch
to, copy or rename a named layout, choose the presentation mode and theme, or reset the current view. Named layouts use `?layout=name`, for example
`Dashboard.html?layout=right-monitor#navigation`. Settings and fullscreen preferences are stored separately for each
layout. Passive display mode is scroll-free; interactive tablet mode provides touch ordering and expandable detail.

Any consolidated widget can also be embedded by itself. Widget-only URLs force passive display mode, remove all
dashboard navigation and controls, and adapt their information density to the host viewport:

Widget | Direct URL
-------|-----------
Ship | `Dashboard.html?widget=ship`
Location | `Dashboard.html?widget=location`
Target | `Dashboard.html?widget=target`
Activity | `Dashboard.html?widget=activity`
Route | `Dashboard.html?widget=route`
Exploration | `Dashboard.html?widget=exploration`
Exobiology | `Dashboard.html?widget=exobiology`
Missions | `Dashboard.html?widget=missions`
Commander | `Dashboard.html?widget=commander`
Mining and Trade | `Dashboard.html?widget=trade`
Fleet Carrier | `Dashboard.html?widget=carrier`

The Exploration widget covers the current system: bodies scanned against the total from the discovery scan (FSS),
the estimated value of everything scanned and mapped so far, the most valuable body still to map, mapping progress,
and biological, geological and other signals found by the surface scanner. Values are estimates from the community's
formulas for Universal Cartographics payouts, including first-discovery and mapping bonuses; the game doesn't report
them. The Exobiology widget follows organic sampling: the species being sampled, samples taken, its estimated value,
and, live from your position, whether you are far enough from earlier samples for the genus's minimum spacing. It also
totals analysed species not yet sold (lost if you die) and compares species analysed on the current body with its
biological signals. Values are estimated Vista Genomics base payouts without the first-logged bonus, and are only
known for English game text. The Missions widget lists active missions, soonest to expire first, with destination,
reward and time left. All three appear on the Navigation view; select Configure and then + to add them to another view.

The Route widget labels each plotted jump with its distance and shows the jumps and light years left. The Ship
widget's Modules line counts damaged and powered-off modules; its module list shows health, power priority, ammo and
engineering. Module state comes from the game's Loadout event, so it updates on login, outfitting and repairs, not
during flight. The Ship widget's details also list materials fullest first against their grade's storage limit, your
suit loadout, and the ship locker.

The Commander widget (on the Combat view to start with) shows ranks with progress, superpower reputation, Powerplay,
engineers, and bounty vouchers and combat bonds not yet handed in; those totals are rebuilt from the journals at
startup and cleared on death. The Mining and Trade widget (Complete Cockpit view) shows the last prospector result,
tons refined and the rate this game session, session sales and profit, and what the last market you opened pays for
the cargo you hold now. The Fleet Carrier widget shows your carrier's tritium, balance, free space and a countdown to
its next jump; it updates when you open carrier management, and isn't on any view until you add it.

An optional layout name can identify a particular third-party instance, for example
`Dashboard.html?widget=target&layout=left-target`. These URLs can be used directly in a browser source, web view, or
iframe. The selected widget always fills the available viewport and does not require interaction.

The former `CockpitDashboard.html`, `NavDashboard.html`, and `CombatDashboard.html` URLs remain as compatibility
redirects to the corresponding views.

## Customising Panels

The default panels use a subset of the information available to demonstrate how to build Panels and have them updated.

Put your own panels, and edited copies of the built-in files, in the custom panels folder:
`Documents\EliteFIPServer\Panels`. The **Open panels folder** button in Settings creates and opens it. The Panel
Server serves the files in that folder as if they were in its own web folder:

- A file with the same path as a built-in file (for example `Dashboard.css` or `js/StatusPanel.js`) is used instead
  of the built-in one.
- Any other file is served alongside the built-in files, so `Documents\EliteFIPServer\Panels\MyPanel.html` opens at
  `http://127.0.0.1:4545/MyPanel.html`.
- Start with a copy of a built-in page from the app's `wwwroot` folder, or from
  [EliteFIPServer.Core/wwwroot](../EliteFIPServer.Core/wwwroot) in this repository.

The folder is outside the app's install folder, so your files survive updates, and it works for the Microsoft Store
version, whose install folder can't be changed. The Panel Server reads it when it starts: if you create the folder
while it's running, stop and start it once. After that, added or changed files show when you reload the page.

When the Panel Server starts, the Activity tab lists the built-in files your copies replace. Replaced files don't
get fixes from later updates, so delete copies you no longer need. Turn off **Custom panels** in Settings to go back to
the built-in pages without deleting anything. With Allow LAN access on, everything in the folder can be opened from
other devices on your network.

The sample JavaScript files provided demonstrate how to receive data updates. 
The sample panels use the updates below. Each update's data is the JSON form of the C# class named alongside, defined
in [GameStateModels.cs](../EliteFIPServer.Core/PanelServer/GameStateModels.cs) (formerly the separate EliteFIPProtocol
package; names and properties are unchanged).

Panel Server Data Update Name | Data class
-------------- | ----------- 
StatusData  | StatusData
TargetData | ShipTargetedData
LocationData | LocationData
NavRouteData | NavigationData
PreviousNavRoute | NavigationData
JumpData | JumpData
ReceivedTextData | ReceivedTextData


The unified dashboard also receives further updates (for example LoadoutData, CargoData, MaterialsData, MissionData,
MissionCollectionData, StationData, SystemData, ExplorationData, SystemExplorationData, ExobiologyData, DockingData,
CombatData, CommanderData, CombatEarningsData, MiningData, TradeData, CarrierData and OnFootData).
See `CockpitDashboard.js` for how it uses them. `SystemExplorationData` summarises the current system (discovery scan
progress and every scanned body with its estimated value, mapping state and signals), and `MissionCollectionData.Active`
lists active missions with their destinations, rewards and expiry times. The C# classes behind these messages are in
[EliteFIPServer.Core/PanelServer](../EliteFIPServer.Core/PanelServer).

Note that some data points might only be available in specific versions of the game (for example some only in ED:Odyssey)
