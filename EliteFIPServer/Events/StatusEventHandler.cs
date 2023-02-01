using EliteAPI.Abstractions.Events;
using EliteFIPProtocol;
using EliteFIPServer.Logging;
using System.Text.Json;

namespace EliteFIPServer {
    
    class StatusEventHandler {

        private IGameDataEvent Caller;

        public StatusEventHandler(IGameDataEvent caller) {
            Caller = caller;
        }

        public void HandleEvent(EliteAPI.Events.Status.Ship.StatusEvent currentStatusData, EventContext context) {

            Log.Instance.Info("Handling Status Event");
            StatusData statusData = new StatusData();           

            statusData.LastUpdate = DateTime.Now;

            // Original Status Flags
            if (currentStatusData.Available) {
                statusData.Docked = currentStatusData.Docked;
                statusData.Landed = currentStatusData.Landed;
                statusData.LandingGearDown = currentStatusData.Gear;
                statusData.ShieldsUp = currentStatusData.Shields;
                statusData.Supercruise = currentStatusData.Supercruise;
                statusData.FlightAssistOff = !currentStatusData.FlightAssist;
                statusData.HardpointsDeployed = currentStatusData.Hardpoints;
                statusData.InWing = currentStatusData.Winging;
                statusData.LightsOn = currentStatusData.Lights;
                statusData.CargoScoopDeployed = currentStatusData.CargoScoop;
                statusData.SilentRunning = currentStatusData.SilentRunning;
                statusData.ScoopingFuel = currentStatusData.Scooping;
                statusData.SrvHandbrake = currentStatusData.SrvHandbrake;
                statusData.SrvTurret = currentStatusData.SrvTurret;
                statusData.SrvUnderShip = currentStatusData.SrvNearShip;
                statusData.SrvDriveAssist = currentStatusData.SrvDriveAssist;
                statusData.FsdMassLocked = currentStatusData.MassLocked;
                statusData.FsdCharging = currentStatusData.FsdCharging;
                statusData.FsdCooldown = currentStatusData.FsdCooldown;
                statusData.LowFuel = currentStatusData.LowFuel;
                statusData.Overheating = currentStatusData.Overheating;                
                if (currentStatusData.HasLatLong) {
                    statusData.HasLatLong = true;
                    statusData.Latitude = currentStatusData.Latitude;
                    statusData.Longitude = currentStatusData.Longitude;
                }
                statusData.InDanger = currentStatusData.InDanger;
                statusData.BeingInterdicted = currentStatusData.InInterdiction;
                statusData.InMainShip = currentStatusData.InMothership;
                statusData.InFighter = currentStatusData.InFighter;
                statusData.InSRV = currentStatusData.InSrv;
                statusData.HudAnalysisMode = currentStatusData.AnalysisMode;
                statusData.NightVision = currentStatusData.NightVision;
                statusData.FsdJump = currentStatusData.FsdJump;
                statusData.AltitudeFromAverageRadius = currentStatusData.AltitudeFromAverageRadius;
                statusData.SrvHighBeam = currentStatusData.SrvHighBeam;

                statusData.OnFoot = currentStatusData.OnFoot;
                statusData.InTaxi = currentStatusData.InTaxi;
                statusData.InMulticrew = currentStatusData.InMultiCrew;
                statusData.OnFootInStation = currentStatusData.OnFootInStation;
                statusData.OnFootOnPlanet = currentStatusData.OnFootOnPlanet;
                statusData.AimDownSight = currentStatusData.AimDownSight;
                statusData.LowOxygen = currentStatusData.LowOxygen;
                statusData.LowHealth = currentStatusData.LowHealth;
                statusData.Cold = currentStatusData.Cold;
                statusData.Hot = currentStatusData.Hot;
                statusData.VeryCold = currentStatusData.VeryCold;
                statusData.VeryHot = currentStatusData.VeryHot;

                statusData.SystemPips = currentStatusData.Pips.System;
                statusData.EnginePips = currentStatusData.Pips.Engines;
                statusData.WeaponPips = currentStatusData.Pips.Weapons;
                statusData.FireGroup = currentStatusData.FireGroup;
                statusData.GuiFocus = currentStatusData.GuiFocus.ToString();
                statusData.FuelMain = currentStatusData.Fuel.FuelMain;
                statusData.FuelReservoir = currentStatusData.Fuel.FuelReservoir;
                statusData.Cargo = currentStatusData.Cargo;
                statusData.LegalState = currentStatusData.LegalState.ToString();
                statusData.Altitude = currentStatusData.Altitude;
                statusData.Heading = currentStatusData.Heading;
                statusData.BodyName = currentStatusData.Body;
                statusData.PlanetRadius = currentStatusData.BodyRadius;
                statusData.Balance = currentStatusData.Balance;
                statusData.DestinationSystem = currentStatusData.Destination.SystemId;
                statusData.DestinationBody = currentStatusData.Destination.BodyId;
                statusData.DestinationName = currentStatusData.Destination.Name;
                statusData.Oxygen = currentStatusData.Oxygen;
                statusData.Health = currentStatusData.Health;
                statusData.Temperature = currentStatusData.Temperature;
                statusData.SelectedWeapon = currentStatusData.SelectedWeapon.ToString();
                statusData.Gravity = currentStatusData.Gravity;


            }           
            Caller.GameDataEvent(GameEventType.Status, statusData);
            
        }

        public static FIPPacket CreateFIPPacket(StatusData statusData) {

            GameData gameData = new GameData();
            gameData.Type = GameEventType.Status.ToString();
            gameData.Data = JsonSerializer.Serialize(statusData);

            FIPPacket packet = new FIPPacket();
            packet.PacketType = PacketType.GameData.ToString();
            packet.Payload = JsonSerializer.Serialize(gameData);
            return packet;
        }
    }
}
