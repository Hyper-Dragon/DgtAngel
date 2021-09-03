/**
 * 
 * DTG Angel Service Worker Helpers
 *  - Loaded from background.js
 */

function NotifyScreen(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    //console.log(messageText);
}

function NotifyScreenDebug(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    //console.debug(messageText);
}



var socket = null;

function checkSocketConnection() {
    if (socket == null) {
        socket = new WebSocket("ws://localhost:37964/ws");

        socket.addEventListener("open", function (event) {
            NotifyScreen("Connection to Cherub OPEN");
        });

        socket.addEventListener("message", function (event) {
            NotifyScreenDebug("KEEP-ALIVE");
        });

        socket.addEventListener("close", function (event) {
            socket = null;
            hasSentStart = false;
            NotifyScreen("Connection to Cherub CLOSED");
        });
    } else {
        SocketSendMessage(GetBlankMessage("ANGEL:SERVICE", "KEEP_ALIVE"));
    }
}

function SocketSendMessage(messageObject) {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            socket.send(JSON.stringify(messageObject));
            NotifyScreenDebug("Update sent");
        }else{
            NotifyScreenDebug("No open socket to client");
        }
    } catch (err) {
        NotifyScreen("Send Error:", err.message);
    }
}