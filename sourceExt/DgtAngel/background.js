/**
 * 
 * DTG Angel Service Worker
 * 
 */

try {
    importScripts("src/js/shared/messages.js", "src/js/background/backgroundHelpers.js");
} catch (e) {
    console.error(e);
}

var hasSentStart = false;
var activeTabId = -1;
var lastUrl = "";

function onActivatedListener(tabId, changeInfo, tab) {
    chrome.tabs.get(tabId.tabId, function (tab) {
        console.debug("New active tab: " + tab.id);
        activeTabId = tab.id;
        lastUrl = "";
        sendWatchStopped();
    });
}

function onUpdatedListener(tabId, changeInfo, tab) {
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

function sendWatchStarted(boardState) {
    let startedMsg = GetBlankMessage("ANGEL:SERVICE", "WATCH_STARTED");
    startedMsg.RemoteBoard = boardState;

    chrome.runtime.sendMessage({ BoardScrapeMsg: startedMsg });
    SocketSendMessage(startedMsg);
}

function sendWatchStopped() {
    if (hasSentStart == true) {
        hasSentStart = false;
        stopMsg=GetBlankMessage("ANGEL:SERVICE", "WATCH_STOPPED");
        SocketSendMessage(stopMsg);
        chrome.runtime.sendMessage({ BoardScrapeMsg: stopMsg });
        NotifyScreen("WATCH STOPPED");
    }
}

// Subscribe to service worker events
self.addEventListener("install", (event) => {
    NotifyScreen("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    NotifyScreen("DGT Angel Activated");
});

// Subscribe to tab events
chrome.tabs.onActivated.addListener(onActivatedListener);
chrome.tabs.onUpdated.addListener(onUpdatedListener);

// Subscribe to chrome events
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        if (activeTabId == sender.tab.id) {
            if (hasSentStart == false) {
                sendWatchStarted(request.BoardScrapeMsg.RemoteBoard);
                NotifyScreen("WATCH STARTED");
                hasSentStart = true;
            } else {
                NotifyScreen("Trying to sending update...");
                SocketSendMessage(request.BoardScrapeMsg);
            }
        }
    } catch (err) {
        hasSentStart = false;
        NotifyScreen("ERROR:", err.message);
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
        NotifyScreen("Client Connect Failed");
    }
}, 5000);
