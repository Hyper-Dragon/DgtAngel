try {
    importScripts("src/inject/messages.js");
} catch (e) {
    console.log(e);
}

var socket = null;
var hasSentStart = false;
var activeTabId = -1;
var lastUrl = "";

function onUpdatedListener(tabId, changeInfo, tab) {
    console.debug("Page Update: " + changeInfo.status + ":" + changeInfo.url);

    if (changeInfo.status == "loading") {
        //Can only get the url on loading
        if (changeInfo.url == undefined) {
            //not on a supported site
            lastUrl = "";
            sendWatchStopped();
        } else if (changeInfo.lastUrl != changeInfo.url) {
            //Detect page change on supported site
            lastUrl = changeInfo.url;
            sendWatchStopped();
        }
    }
}

function onActivatedListener(tabId, changeInfo, tab) {
    chrome.tabs.get(tabId.tabId, function (tab) {
        console.debug("New active tab: " + tab.id);

        activeTabId = tab.id;
        sendWatchStopped();
    });
}

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
            console.debug("PONG CAME BACK", event.data);
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

function sendWatchStopped() {
    if (hasSentStart == true) {
        hasSentStart = false;
        chrome.runtime.sendMessage({
            WorkerMessage: "Watch Stopped",
        });

        console.log("Watch Stopped");

        let stoppedMsg = GetBlankMessage("ANGEL:SERVICE", "WATCH_STOPPED");

        chrome.runtime.sendMessage({ BoardScrapeMsg: stoppedMsg });

        let stoppedMsgJson = JSON.stringify(stoppedMsg);
        socket.send(stoppedMsgJson);
    }
}

// Subscribe to service worker events
self.addEventListener("install", (event) => {
    console.log("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    console.log("DGT Angel Activated");
});

// Subscribe to tab events
chrome.tabs.onActivated.addListener(onActivatedListener);
chrome.tabs.onUpdated.addListener(onUpdatedListener);

// Subscribe to chrome events
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        if (socket != null && socket.readyState == WebSocket.OPEN) {
            if (activeTabId == sender.tab.id) {
                if (hasSentStart == false) {
                    sendWatchStarted(request.BoardScrapeMsg.RemoteBoard);
                    chrome.runtime.sendMessage({
                        WorkerMessage: "Watch Started",
                    });
                    console.log("Watch Started");
                    hasSentStart = true;
                } else {
                    chrome.runtime.sendMessage({
                        WorkerMessage: "Update Sent",
                    });
                    socket.send(JSON.stringify(request.BoardScrapeMsg));
                }
            }
        }
    } catch (err) {
        hasSentStart = false;
        chrome.runtime.sendMessage({ WorkerMessage: err.message });
        console.log("ERROR:", err.message);
    } finally {
        return true; // Required to keep message port open
    }
});

//Keep trying to connect to client
setInterval(() => {
    try {
        checkSocketConnection();
    } catch {
        socket = null;
        chrome.runtime.sendMessage({ WorkerMessage: "Connect Failed" });
        console.log("Connect failed");
    }
}, 5000);
