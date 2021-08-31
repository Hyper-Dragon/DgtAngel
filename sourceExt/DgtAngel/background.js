const delay = (ms) => new Promise((res) => setTimeout(res, ms));
var socket = null;
var openedSocket = false;
var keepAliveTimeMs = 5000;
var reconnectTimeMs = 5000;

function GetBlankMessage(messageType) {
    blankMsg = {
        Source: "ANGEL:WORKER",
        MessageType: messageType,
        Message: "",
        RemoteBoard: null,
    };

    return blankMsg;
}

async function runUpdates() {
    while (true) {
        try {
            socket = new WebSocket("ws://localhost:37964/ws");

            while (socket.readyState == WebSocket.CONNECTING) {
                console.log("Connecting to the Cherub client");
                await delay(500);
            }

            // Listen for messages
            socket.addEventListener("message", function (event) {
                console.log("PONG CAME BACK", event.data);
            });

            socket.addEventListener("close", function (event) {
                console.log("WEBSOCKET_CLOSE: connection closed ", event.data);
            });

            socket.addEventListener("error", function (event) {
                console.log("WEBSOCKET_ERROR: Error", event.data);
            });

            while (socket.readyState == WebSocket.OPEN) {
                keepAliveMsg = GetBlankMessage("KEEP_ALIVE");
                keepAliveJson = JSON.stringify(keepAliveMsg);
                socket.send(keepAliveJson);
                await delay(5000);
            }
        } catch {
            console.log("Connect failed");
        }

        await delay(keepAliveTimeMs);
    }
}

runUpdates();
