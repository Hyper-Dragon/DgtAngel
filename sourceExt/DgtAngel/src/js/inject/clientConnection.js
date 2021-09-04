/**
 * DTG Angel Service Worker Helpers
 *  - Manages the socket connection for angelWrapper
 */

var hasSentStart = false;
var socket = null;

function checkSocketConnection() {
    if (socket == null) {
        try {
            NotifyScreen("Connecting to the Cherub client...");
            socket = new WebSocket(CLIENT_URL);

            socket.onerror = function (error) {
                NotifyScreen("Connecting to the Cherub client...FAILED");
            };

            socket.addEventListener("open", function (event) {
                NotifyScreen("Connecting to the Cherub client...OPEN");
            });

            socket.addEventListener("message", function (event) {
                NotifyScreenDebug("Sending Update...KEEP-ALIVE");
            });

            socket.addEventListener("close", function (event) {
                socket = null;
                hasSentStart = false;
                NotifyScreen("Cherub client connection...CLOSED");
            });
        } catch {
            NotifyScreen("Connecting to the Cherub client...FAILED");
        }
    } else {
        SocketSendMessage(GetBlankMessage(WRAPPER_SOURCE_NAME, "KEEP_ALIVE"));
    }
}

function sendWatchStarted(boardState) {
    let startedMsg = GetBlankMessage(WRAPPER_SOURCE_NAME, "WATCH_STARTED");
    startedMsg.RemoteBoard = boardState;
    hasSentStart = true;

    //echo the message out for the popup (if it is running)
    chrome.runtime.sendMessage({ BoardScrapeMsg: startedMsg });
    SocketSendMessage(startedMsg);
}

function sendWatchStopped() {
    if (hasSentStart == true) {
        hasSentStart = false;
        stopMsg = GetBlankMessage(WRAPPER_SOURCE_NAME, "WATCH_STOPPED");
        SocketSendMessage(stopMsg);

        //echo the message out for the popup (if it is running)
        chrome.runtime.sendMessage({ BoardScrapeMsg: stopMsg });
        NotifyScreen("WATCH STOPPED");
    }
}

function SocketSendMessage(messageObject) {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            socket.send(JSON.stringify(messageObject));
            NotifyScreenDebug("Sending Update...Sent");
        } else {
            NotifyScreenDebug("Sending Update...No open socket to client");
        }
    } catch (err) {
        NotifyScreen("Sending Update...Error:", err.message);
    }
}

//*****************************************************************/
//Keep trying to connect to the client and when we do keep it alive
//*****************************************************************/
setInterval(() => {
    checkSocketConnection();
}, CLIENT_CONNECT_KEEP_ALIVE_MS);