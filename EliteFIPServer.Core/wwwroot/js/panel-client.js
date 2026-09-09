"use strict";

window.panelConnection = new signalR.HubConnectionBuilder()
    .withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
    .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: function () { return 5000; }
    })
    .configureLogging(signalR.LogLevel.Error)
    .build();

var panelConnectionStarting = false;
var panelReconnectTimer = null;

function setPanelConnectionState(state) {
    document.documentElement.dataset.panelConnection = state;
    var label = document.getElementById("connection");
    if (label) {
        label.textContent = state;
    }
}

window.panelConnection.onreconnecting(function () {
    setPanelConnectionState("reconnecting");
});

window.panelConnection.onreconnected(function () {
    setPanelConnectionState("connected");
});

window.panelConnection.onclose(function () {
    setPanelConnectionState("closed");
    schedulePanelConnection();
});

function schedulePanelConnection() {
    if (panelConnectionStarting || window.panelConnection.state === signalR.HubConnectionState.Connected || panelReconnectTimer !== null) {
        return;
    }

    panelReconnectTimer = window.setTimeout(function () {
        panelReconnectTimer = null;
        startPanelConnection();
    }, 1000);
}

function startPanelConnection() {
    if (panelConnectionStarting || window.panelConnection.state !== signalR.HubConnectionState.Disconnected) {
        return;
    }

    panelConnectionStarting = true;
    setPanelConnectionState("connecting");
    window.panelConnection.start()
        .then(function () {
            setPanelConnectionState("connected");
        })
        .catch(function (error) {
            setPanelConnectionState("error");
            console.error(error.toString());
            schedulePanelConnection();
        })
        .finally(function () {
            panelConnectionStarting = false;
        });
}

window.startPanelConnection = startPanelConnection;

window.setInterval(function () {
    if (window.panelConnection.state === signalR.HubConnectionState.Connected) {
        window.panelConnection.invoke("Ping").catch(function (error) {
            console.warn("Panel heartbeat failed", error);
            window.panelConnection.stop();
        });
    } else {
        schedulePanelConnection();
    }
}, 5000);
