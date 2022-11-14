/**
 * DTG Angel Service Worker Helpers
 *  - Manages the socket connection for angelWrapper
 */

var hasSentStart = false;
var socket = null;

function checkSocketConnection() {
    if (socket == null) {
        NotifyScreen("Connecting to the Cherub client...");

        //Test to see if the client is there before we try to open the websocket.
        //Not doing this first means that we end up with errors being raised on
        //the plugin extension page.
        fetch(CLIENT_TEST_URL)
            .then((response) => response.json())
            .then((data) => {
                try {

                    socket = new WebSocket(CLIENT_URL);

                    socket.onerror = function (error) {
                        NotifyScreen(
                            "Connecting to the Cherub client...FAILED"
                        );
                    };

                    socket.addEventListener("open", function (event) {
                        NotifyScreen("Connecting to the Cherub client...OPEN");
                    });

                    socket.addEventListener("message", function (event) {
                        //This is the pong comming back
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
            })
            .catch((error) => {
                NotifyScreen("Connecting to the Cherub client...FAILED");
            });
    } else {
        SocketSendMessage(
            GetBlankMessage(WRAPPER_SOURCE_NAME, messageStateCodes.KEEP_ALIVE)
        );
    }
}

function sendWatchStarted(boardState) {
    hasSentStart = true;
    startedMsg = GetBlankMessage(
        WRAPPER_SOURCE_NAME,
        messageStateCodes.WATCH_STARTED
    );
    startedMsg.RemoteBoard = boardState;
    SocketSendMessage(startedMsg);

    //echo the message out for the popup (if it is running)
    chrome.runtime.sendMessage({ BoardScrapeMsg: startedMsg });
    NotifyScreen("WATCH STARTED");
}

function sendWatchStopped() {
    if (hasSentStart == true) {
        hasSentStart = false;
        stopMsg = GetBlankMessage(
            WRAPPER_SOURCE_NAME,
            messageStateCodes.WATCH_STOPPED
        );
        SocketSendMessage(stopMsg);

        //echo the message out for the popup (if it is running)
        chrome.runtime.sendMessage({ BoardScrapeMsg: stopMsg });
        NotifyScreen("WATCH STOPPED");
    }
}

function SocketSendMessage(messageObject) {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            NotifyScreenDebug("Sending Update...");
            socket.send(JSON.stringify(messageObject));
            NotifyScreenDebug("Sending Update...Sent");
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
