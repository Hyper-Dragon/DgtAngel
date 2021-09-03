/**
 * DTG Angel Service Worker
 *
 */
try {
    importScripts(
        "src/js/shared/globals.js",
        "src/js/shared/messages.js",
        "src/js/shared/globalHelpers.js",
        "src/js/background/backgroundHelpers.js"
    );
} catch (e) {
    console.error(e);
}

var activeTabId = -1;
var lastUrl = "";

// Subscribe to service worker events
self.addEventListener("install", (event) => {
    NotifyScreen("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    NotifyScreen("DGT Angel Activated");
});

// Subscribe to tab events
//chrome.tabs.onActivated.addListener(onActivatedListener);
//chrome.tabs.onUpdated.addListener(onUpdatedListener);


//function onActivatedListener(tabId, changeInfo, tab) {
//    chrome.tabs.get(tabId.tabId, function (tab) {
//        NotifyScreen("New active tab: " + tab.id);
//        activeTabId = tab.id;
//        lastUrl = "";
//        //sendWatchStopped();
//    });
//}

//function onUpdatedListener(tabId, changeInfo, tab) {
//    if (changeInfo.status == "loading") {
//        //Can only get the url on loading
//        if (changeInfo.url == undefined) {
//            //not on a supported site
//            lastUrl = "";
//            sendWatchStopped();
//        } else if (changeInfo.lastUrl != changeInfo.url) {
//            //Detect page change on supported site
//            lastUrl = changeInfo.url;
//            sendWatchStopped();
//        }
//    }
//}









// Subscribe to chrome events
//chrome.runtime.onConnect.addListener(function (port) {
//    if (port.name == WRAPPER_PORT_NAME ) {
//        port.onMessage.addListener(function (request) {
//            try {
////                if (activeTabId == sender.tab.id) {
//                    if (hasSentStart == false) {
//                        sendWatchStarted(request.BoardScrapeMsg.RemoteBoard);
//                        NotifyScreen("WATCH STARTED");
//                        hasSentStart = true;
//                    }else{
//
//                    NotifyScreen("Trying to sending update...");
//                    SocketSendMessage(request.BoardScrapeMsg);
//                    }
//  //              }
//            } catch (err) {
//                hasSentStart = false;
//                NotifyScreen("ERROR:", err.message);
//            } finally {
//                return true; // Required to keep message port open
//            }
//        });
//    }
//});
