/**
 * DTG Angel Service Worker Helpers
 *  - Loaded from background.js
 */

function NotifyScreen(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    if (LOG_NOTIFY_ON_CONSOLE) {
        console.log(messageText);
    }
}

function NotifyScreenDebug(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    if (LOG_NOTIFY_DEBUG_ON_CONSOLE) {
        console.debug(messageText);
    }
}

var socket = null;

function checkSocketConnection() {
    if (socket == null) {
        socket = new WebSocket(CLIENT_URL);

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
        SocketSendMessage(GetBlankMessage(BACKGROUND_SOURCE_NAME, "KEEP_ALIVE"));
    }
}

function SocketSendMessage(messageObject) {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            socket.send(JSON.stringify(messageObject));
            NotifyScreenDebug("Update sent");
        } else {
            NotifyScreenDebug("No open socket to client");
        }
    } catch (err) {
        NotifyScreen("Send Error:", err.message);
    }
}
