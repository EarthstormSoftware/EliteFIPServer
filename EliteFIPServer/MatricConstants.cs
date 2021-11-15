using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteFIPServer {
    class MatricConstants {
        
        // Status Data
        public static string DOCKED = "Docked";
        public static string LANDED = "Landed";
        public static string LANDINGGEAR = "LandingGear";
        public static string SHIELDS = "Shields";
        public static string SUPERCRUISE = "Supercruise";
        public static string FLIGHTASSIST = "FlightAssist";
        public static string HARDPOINTS = "Hardpoints";
        public static string INWING = "InWing";
        public static string LIGHTS = "Lights";
        public static string CARGOSCOOP = "CargoScoop";
        public static string SILENTRUNNING = "SilentRunning";
        public static string SCOOPINGFUEL = "ScoopingFuel";
        public static string SRVHANDBRAKE = "SrvHandbrake";
        public static string SRVTURRET = "SrvTurret";
        public static string SRVUNDERSHIP = "SrvUnderShip";
        public static string SRVDRIVEASSIST = "SrvDriveAssist";
        public static string FSDMASSLOCK = "FSDMassLock";
        public static string FSDCHARGE = "FSDCharging";
        public static string FSDCOOLDOWN = "FSDCooldown";
        public static string LOWFUEL = "LowFuel";
        public static string OVERHEAT = "Overheat";
        public static string INDANGER = "InDanger";
        public static string INTERDICTION = "Interdiction";
        public static string INMAINSHIP = "InMainShip";
        public static string INFIGHTER = "InFighter";
        public static string INSRV = "InSRV";
        public static string HUDMODE = "HudMode";
        public static string NIGHTVISION = "NightVision";
        public static string FSDJUMP = "FsdJump";
        public static string SRVHIGHBEAM = "SrvHighBeam";

        // Extended Status data from Odyssey
        public static string ONFOOT = "OnFoot";
        public static string INTAXI = "InTaxi";
        public static string INMULTICREW = "InMulticrew";
        public static string ONFOOTINSTATION = "OnFootInStation";
        public static string ONFOOTONPLANET = "OnFootOnPlanet";
        public static string AIMDOWNSIGHT = "AimDownSight";
        public static string LOWOXYGEN = "LowOxygen";
        public static string LOWHEALTH = "LowHealth";
        public static string COLD = "Cold";
        public static string HOT = "Hot";
        public static string VERYCOLD = "VeryCold";
        public static string VERYHOT = "VeryHot";

        // Pure text data
        public static string FUELRESERVOIR = "FuelReservoir";


        // Status Text Displays
        public static string STATUS = "Status";
        public static string STATUS_LABEL = "StatusLabel";

        // Target Text Displays
        public static string TARGET = "Target";
        public static string TARGET_LABEL = "TargetLabel";


        // Button prefixes
        public static string BTN = "btn"; // Standard On/Off Button, where button text can change according to state
        public static string IND = "ind"; // Standard On/Off button, where button text is not changed by state 
        public static string WRN = "wrn"; // Standard On/Off button, where button text is not changed by state, but when 'on', button flashes 
        public static string SWT = "swt"; // Multi-position switch, button text is not changed by position
        public static string SLD = "sld"; // Sider where value can be set along a scale
        public static string TXT = "txt"; // Button containing only text for information, no state
        public static string PNL = "pnl"; // iFrame Panel where content is provided via HTTP request
    }
}
