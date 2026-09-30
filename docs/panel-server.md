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
the LAN URL to use on the other device. Windows may ask you to allow Elite FIP Server through the firewall.

### Troubleshooting

If the port chosen is in use, the Panel Server will not start and its status returns to Stopped.
If you experience problems, enable logging and check if there are any issues shown:

In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. When enabled, the log is located in the User AppData\Roaming\EliteFIPServer folder.
For example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer

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
move, and resize widgets. Named layouts use `?layout=name`, for example
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

An optional layout name can identify a particular third-party instance, for example
`Dashboard.html?widget=target&layout=left-target`. These URLs can be used directly in a browser source, web view, or
iframe. The selected widget always fills the available viewport and does not require interaction.

The former `CockpitDashboard.html`, `NavDashboard.html`, and `CombatDashboard.html` URLs remain as compatibility
redirects to the corresponding views.

## Customising Panels

The default panels use a subset of the information available to demonstrate how to build Panels and have them updated. If you want to create your own panels, simply copy or edit 
the existing panel files and modify them as desired. 

The sample JavaScript files provided demonstrate how to receive data updates. 
The sample panels use the updates below; the data provided on each update corresponds to the Protocol here:  
[EliteFIPProtocol](https://github.com/EarthstormSoftware/EliteFIPProtocol)

Panel Server Data Update Name | Elite FIP Protocol Data
-------------- | ----------- 
StatusData  | StatusData
TargetData | ShipTargetedData
LocationData | LocationData
NavRouteData | NavigationData
PreviousNavRoute | NavigationData
JumpData | JumpData


The unified dashboard also receives further updates (for example LoadoutData, CargoData, MaterialsData, MissionData,
StationData, SystemData, ExplorationData, DockingData and CombatData). See `CockpitDashboard.js` for how it uses them.

Note that some data points might only be available in specific versions of the game (for example some only in ED:Odyssey)
