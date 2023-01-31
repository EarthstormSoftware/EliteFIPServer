"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets }).build();
const roundAccurately = (number, decimalPlaces) => Number(Math.round(number + "e" + decimalPlaces) + "e-" + decimalPlaces);

connection.on("StatusData", function (StatusData) {

    var data = JSON.parse(StatusData);
    if (data != null) {
        console.log(data);
        if (data.BodyName != null) { document.getElementById("BodyName").innerHTML = data.BodyName };
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
    }
});

connection.on("NavRouteData", function (NavRouteData) {

    var data = JSON.parse(NavRouteData);
    if (data != null) {
        console.log(data);
        if (data.SystemName != null) { document.getElementById("SystemName").innerHTML = data.SystemName };
    }
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

