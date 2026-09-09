"use strict";

const connection = window.panelConnection;

const roundAccurately = (number, decimalPlaces) => Number(Math.round(number + "e" + decimalPlaces) + "e-" + decimalPlaces);

connection.on("TargetData", function (TargetData) {

    var data = TargetData.Data;
    if (data != null) {
        console.log(data);

        document.getElementById("Ship").textContent = "";
        document.getElementById("PilotName").textContent = "";
        document.getElementById("PilotRank").textContent = "";
        document.getElementById("Faction").textContent = "";
        document.getElementById("LegalStatus").textContent = "";
        document.getElementById("Bounty").textContent = "";

        if (data.TargetLocked != null && data.TargetLocked != false) {
            if (data.Ship != null) { document.getElementById("Ship").textContent = data.Ship; }
            if (data.PilotName != null) { document.getElementById("PilotName").textContent = data.PilotName; }
            if (data.PilotRank != null) { document.getElementById("PilotRank").textContent = data.PilotRank; }
            if (data.Faction != null) { document.getElementById("Faction").textContent = data.Faction; }
            if (data.LegalStatus != null) {
                var legalStatusCell = document.getElementById("LegalStatus");
                if (data.LegalStatus == "Wanted") {
                    legalStatusCell.style.color = 'red';
                } else {
                    legalStatusCell.style.color = 'orange';
                }
                legalStatusCell.textContent = data.LegalStatus;
            }
            if (data.Bounty != null) {
                if (data.Bounty == 0) {
                    document.getElementById("Bounty").textContent = "";
                } else {
                    document.getElementById("Bounty").textContent = data.Bounty;
                }
            }
        }
    }
});

startPanelConnection();
