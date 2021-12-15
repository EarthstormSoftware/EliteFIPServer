# Elite FIP Server - Panel Server

The Panel Server component of Elite FIP Server provides the ability to view Elite game data via a web browser or
other client that can display web pages, such as the Matric iFrame control. The data is provided via a built in web server
which serves static, user-editable, HTML files, and uses JavaScript to provide a mechansim to update the data on the page as
game data changes. 

Users can modify the HTML and Javascript to customise the display of the information to allow for their specific
use cases.


---

## Enabling the Panel Server

The Panel Server is disabled by default as a security precaution. To enable the panel server, open the Settings panel and 
enable the Panel Server check box. 

The Panel server attempts to use port 4545 by default. This can be changed if required. If it is changed, the user must also
update any URLS used to access the server, including in the JavaScript used to update the HTML pages.

If the port chosen is in use, the Panel Server will not start. Currently there is no way to determine this from the UI.
If you experience problems, enable logging and check if there are any issues shown:

In the 'Settings' panel the Enable Logging option will enable or disable logging. By default logging is turned 
off. When enabled, the log is located in the User AppData\Roaming\EliteFIPServer folder.
For example: c:\Users\MyUserName\AppData\Roaming\EliteFIPServer

---

## Checking the Panel Server is running

To firm the Panel Server is running, use a web browser to go to  
http://\{hostipaddress\}:\{panelserverport\}  

For example:  
http://192.168.169.100:4545

If the Panel Server is running, you will see:  
"Elite FIP Panel Server running" in the browser.

---

## Displaying Panels

To display the Panels, simply use a web browser, or other suitable client (Matric iFrame for example) and target the appropriate HTML page in the \{Elite FIP Server Folder\}\\wwwroot folder.

For example:  
http://192.168.169.100:4545/StatusPanel.html

## Available Panels

Default panels are provided as follows:

Panel Description | Panel FileName
-------------- | ----------- 
Status Panel (Misc Navigation Information) | StatusPanel.html 
Target Panel (Information on currently targeted ship) | TargetPanel.html

## Customising Panels

The default panels use a subset of the information available to demonstrate how to build Panels and have them updated. If you want to create your own panels, simply copy or edit 
the existing panel files and modify them as desired. 

The sample javaScript files provided demonstrate how to receive data updates. 
The data provided on each update corresponds to the Protocol here:  
[EliteFIPProtocol](https://github.com/EarthstormSoftware/EliteFIPProtocol)

Panel Server Data Update Name | Elite FIP Protocol Data
-------------- | ----------- 
StatusData  | StatusData
TargetData | ShipTargetedData

Note that some data points might only be available in specific versions of the game (for example some only in ED:Odyssey)
