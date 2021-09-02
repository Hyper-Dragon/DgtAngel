try {
    importScripts("src/inject/messages.js");
} catch (e) {
    console.log(e);
}

var socket = null;
var hasSentStart = false;
var currentTabId = -1;
var activeTabId = -1;

function onUpdatedListener(tabId, changeInfo, tab) {
    chrome.tabs.get(tabId.tabId, function (tab) {
        console.log("New active tab: " + tab.id);
        activeTabId = tab.id;
    });
}

// Subscribe to tab events
chrome.tabs.onActivated.addListener(onUpdatedListener);

function checkSocketConnection() {
    if (socket == null) {
        hasSentStart = false;
        socket = new WebSocket("ws://localhost:37964/ws");

        socket.addEventListener("open", function (event) {
            chrome.runtime.sendMessage({
                WorkerMessage: "Client Connection OPEN",
            });
            console.log("Connection to Cherub OPEN");
        });

        socket.addEventListener("message", function (event) {
            chrome.runtime.sendMessage({ WorkerMessage: "Keep-Alive" });
            console.log("PONG CAME BACK", event.data);
        });

        socket.addEventListener("close", function (event) {
            socket = null;
            chrome.runtime.sendMessage({
                WorkerMessage: "Client Connection CLOSED",
            });
            console.log("Connection to Cherub CLOSED");
        });

        socket.addEventListener("error", function (event) {
            socket = null;
            chrome.runtime.sendMessage({
                WorkerMessage: "Client Connection ERROR",
            });
            console.log("Connection to Cherub ERROR");
        });
    } else {
        keepAliveMsg = GetBlankMessage("ANGEL:SERVICE", "KEEP_ALIVE");
        keepAliveJson = JSON.stringify(keepAliveMsg);
        socket.send(keepAliveJson);
    }
}

function sendWatchStarted(boardState) {
    let startedMsg = GetBlankMessage("ANGEL:SERVICE", "WATCH_STARTED");
    startedMsg.RemoteBoard = boardState;

    chrome.runtime.sendMessage({ BoardScrapeMsg: startedMsg });

    let startedMsgJson = JSON.stringify(startedMsg);
    socket.send(startedMsgJson);
}

function sendWatchStopped(boardState) {
    let stoppedMsg = GetBlankMessage("ANGEL:SERVICE", "WATCH_STOPPED");
    stoppedMsg.RemoteBoard = boardState;

    chrome.runtime.sendMessage({ BoardScrapeMsg: stoppedMsg });

    let stoppedMsgJson = JSON.stringify(stoppedMsg);
    socket.send(stoppedMsgJson);
}

self.addEventListener("install", (event) => {
    console.log("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    console.log("DGT Angel Activated");
});

setInterval(() => {
    try {
        checkSocketConnection();
    } catch {
        socket = null;
        chrome.runtime.sendMessage({ WorkerMessage: "Connect Failed" });
        console.log("Connect failed");
    }
}, 5000);

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            if (currentTabId != sender.tab.id) {
                if (hasSentStart == true) {
                    hasSentStart = false;
                    chrome.runtime.sendMessage({
                        WorkerMessage: "Watch Stopped",
                    });
                    console.log("Watch Stopped");
                    sendWatchStopped(request.BoardScrapeMsg.RemoteBoard);
                }
            }

            if (hasSentStart == false) {
                currentTabId = sender.tab.id;
                sendWatchStarted(request.BoardScrapeMsg.RemoteBoard);
                chrome.runtime.sendMessage({ WorkerMessage: "Watch Started" });
                console.log("Watch Started");
                hasSentStart = true;
            }

            chrome.runtime.sendMessage({ WorkerMessage: "Update Sent" });
            updateMsgJson = JSON.stringify(request.BoardScrapeMsg);
            socket.send(updateMsgJson);
        }
    } catch (err) {
        hasSentStart = false;
        chrome.runtime.sendMessage({ WorkerMessage: err.message });
        console.log("ERROR:", err.message);
    } finally {
        return true; // Required to keep message port open
    }
});
