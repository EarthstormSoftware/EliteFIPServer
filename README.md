# EliteFIPServer

Elite FIP Server is a .NET app which uses [EliteJournalReader](https://github.com/MagicMau/EliteJournalReader) to 
read Elite Dangerous game information, and then feed it to [Matric](https://matricapp.com) via the Matric 
Integration API.

This allows Matric to reflect the current game state in the UI, with particular respect to button/toggle state 
(such as Landing Gear or Lights), but also other information like current target data. Using a custom touch
screen display rather than a traditional keyboard increases immersion and lowers the requirement to remember
all the many key bindings you might need.

# Pre-requisites
Elite FIP Server is a .Net 5.0 application and requires the appropriate [runtime](https://dotnet.microsoft.com/download/dotnet/5.0/runtime)
to be installed 
