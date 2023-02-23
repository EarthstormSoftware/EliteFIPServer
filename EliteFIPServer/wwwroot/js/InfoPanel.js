"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
    .withAutomaticReconnect()
    .build();

const roundAccurately = (number, decimalPlaces) => Number(Math.round(number + "e" + decimalPlaces) + "e-" + decimalPlaces);

connection.on("StatusData", function (StatusData) {

    var data = JSON.parse(StatusData);
    if (data != null) {
        console.log(data);        
        if (data.LegalState != null) { document.getElementById("LegalState").innerHTML = data.LegalState };
        if (data.Cargo != null) { document.getElementById("Cargo").innerHTML = data.Cargo };
        if (data.FuelMain != null) { document.getElementById("FuelMain").innerHTML = roundAccurately(data.FuelMain, 2) };
        if (data.FuelReservoir != null) { document.getElementById("FuelReservoir").innerHTML = roundAccurately(data.FuelReservoir, 2) };
    }
});

connection.on("TargetData", function (TargetData) {

    var data = JSON.parse(TargetData);
    if (data != null) {
        console.log(data);

        document.getElementById("Ship").innerHTML = ""
        document.getElementById("PilotName").innerHTML = ""
        document.getElementById("PilotRank").innerHTML = ""
        document.getElementById("Faction").innerHTML = ""
        document.getElementById("LegalStatus").innerHTML = ""
        document.getElementById("Bounty").innerHTML = ""

        if (data.TargetLocked != null && data.TargetLocked != false) {
            if (data.Ship != null) { document.getElementById("Ship").innerHTML = data.Ship };
            if (data.PilotName != null) { document.getElementById("PilotName").innerHTML = data.PilotName };
            if (data.PilotRank != null) { document.getElementById("PilotRank").innerHTML = data.PilotRank };
            if (data.Faction != null) { document.getElementById("Faction").innerHTML = data.Faction };
            if (data.LegalStatus != null) {
                var legalStatusCell = document.getElementById("LegalStatus");
                if (data.LegalStatus == "Wanted") {
                    legalStatusCell.style.color = 'red';
                } else {
                    legalStatusCell.style.color = 'orange';
                }
                document.getElementById("LegalStatus").innerHTML = data.LegalStatus
            };
            if (data.Bounty != null) {
                if (data.Bounty == 0) {
                    document.getElementById("Bounty").innerHTML = "";
                } else {
                    document.getElementById("Bounty").innerHTML = data.Bounty;
                }
            }
        }
    }
});

connection.on("LocationData", function (LocationData) {

    var data = JSON.parse(LocationData);
    if (data != null) {
        console.log(data);
        if (data.SystemName != null) { document.getElementById("SystemName").innerHTML = data.SystemName };       
        if (data.StationName != null) { document.getElementById("StationName").innerHTML = data.StationName }; 
    }
});

connection.on("NavRouteData", function (NavRouteData) {

    var data = JSON.parse(NavRouteData);
    if (data != null) {
        console.log(data);
        if (data.NavRouteActive = true && data.Stops.length > 0) {
            console.log("Processing " + data.Stops.length + " stops");
            var routestring = ""
            for (var index in data.Stops) {                
                 routestring += data.Stops[index].SystemName + " > ";
            }
            document.getElementById("NavRoute").innerHTML = routestring.slice(0, -3);

        } else {
            document.getElementById("NavRoute").innerHTML = "No route available"
        }
        console.log("HTML: " + document.getElementById("NavRoute").innerHTML);
    }
});

connection.on("JumpData", function (JumpData) {

    var data = JSON.parse(JumpData);
    if (data != null) {
        console.log(data);
        if (data.OriginSystemName != null) { document.getElementById("OriginSystemName").innerHTML = data.OriginSystemName };
        if (data.DestinationSystemName != null) { document.getElementById("DestinationSystemName").innerHTML = data.DestinationSystemName };
        if (data.JumpDistance != null) { document.getElementById("JumpDistance").innerHTML = roundAccurately(data.JumpDistance,2) };
        if (data.FuelUsed != null) { document.getElementById("FuelUsed").innerHTML = roundAccurately(data.FuelUsed,2) };
    }
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

