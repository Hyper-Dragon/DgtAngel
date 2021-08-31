const delay = (ms) => new Promise((res) => setTimeout(res, ms));

var socket = null;
var currentTabId = -1;
var activeTabId = -1;

function GetBlankMessage(messageType) {
    blankMsg = {
        Source: "ANGEL:WORKER",
        MessageType: messageType,
        Message: "",
        RemoteBoard: null,
    };

    return blankMsg;
}


function onUpdatedListener(tabId, changeInfo, tab) {
    chrome.tabs.get(tabId.tabId, function(tab){
        console.log('New active tab: ' + tab.id);
        activeTabId = tab.id;
    });
}

// Subscribe to tab events
chrome.tabs.onActivated.addListener(onUpdatedListener);

function checkSocketConnection() {
    if (socket == null) {
        socket = new WebSocket("ws://localhost:37964/ws");

        socket.addEventListener("open", function (event) {
            console.log("Connection to Cherub OPEN");
        });

        socket.addEventListener("message", function (event) {
            console.log("PONG CAME BACK", event.data);
        });

        socket.addEventListener("close", function (event) {
            socket = null;
            console.log("Connection to Cherub CLOSED");
        });

        socket.addEventListener("error", function (event) {
            socket = null;
            console.log("Connection to Cherub ERROR");
        });
    } else {
        keepAliveMsg = GetBlankMessage("KEEP_ALIVE");
        keepAliveJson = JSON.stringify(keepAliveMsg);
        socket.send(keepAliveJson);
    }
}

function sendWatchStarted(boardState) {
    let startedMsg = GetBlankMessage("WATCH_STARTED");
    startedMsg.RemoteBoard = boardState;
    let startedMsgJson = JSON.stringify(startedMsg.RemoteBoard);
    socket.send(startedMsgJson);
}

function sendWatchStopped(boardState) {
    let stoppedMsg = GetBlankMessage("WATCH_STOPPED");
    stoppedMsg.RemoteBoard = boardState;
    let stoppedMsgJson = JSON.stringify(stoppedMsg);
    socket.send(stoppedMsgJson);
}

self.addEventListener("install", (event) => {
    console.log("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    console.log("DGT Angel Starting");

    setInterval(() => {
        try {
            checkSocketConnection();
        } catch {
            socket = null;
            console.log("Connect failed");
        }
    }, 5000);
});

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            if (currentTabId != sender.tab.id) {
                if (currentTabId != -1) {
                    console.log("Watch Stopped");
                    sendWatchStopped(request.BoardScrapeMsg.RemoteBoard);
                }

                currentTabId = sender.tab.id;
                sendWatchStarted(request.BoardScrapeMsg.RemoteBoard);
                console.log("Watch Started");
            }

            updateMsgJson = JSON.stringify(request.BoardScrapeMsg);
            socket.send(updateMsgJson);
        }
    } catch {
        console.log("ERROR");
    } finally {
        return true; // Required to keep message port open
    }
});
