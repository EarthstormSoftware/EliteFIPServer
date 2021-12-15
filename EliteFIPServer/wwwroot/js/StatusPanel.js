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

connection.start().catch(function (err) {
    return console.error(err.toString());
});

