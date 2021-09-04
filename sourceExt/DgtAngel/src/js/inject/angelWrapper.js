/**
 * DTG Angel Generic injection script
 *  - Calls GetRemoteBoardState() in the scrape file added by the manifest
 *
 */

window.addEventListener("beforeunload", function (e) {
    sendWatchStopped();
});

setInterval(() => {
    if (document.readyState === "complete") {
        try {
            //Check if the current URL matches one that the loaded
            //js can handle...
            if (IsUrlValid(window.location.toString())) {
                updateMsg = GetBlankMessage(
                    PAGE_SOURCE_NAME,
                    messageStateCodes.STATE_UPDATED
                );

                //Get the board state
                updateMsg.RemoteBoard = GetRemoteBoardState();
            } else {
                //This is probably a manifest issue loading the wrong js
                updateMsg = GetBlankMessage(
                    WRAPPER_SOURCE_NAME,
                    messageStateCodes.STATE_UPDATED
                );
                updateMsg.RemoteBoard = getDefaultRemoteBoard();
                updateMsg.RemoteBoard.State.Code = boardStateCodes.UNKNOWN_PAGE;
                updateMsg.RemoteBoard.State.Message =
                    "The loaded plugin does not recognise the the page URL";
                updateMsg.RemoteBoard.Board = null;
                updateMsg.RemoteBoard.BoardConnection = null;
            }
        } catch (err) {
            updateMsg = GetBlankMessage(
                WRAPPER_SOURCE_NAME,
                messageStateCodes.STATE_UPDATED
            );
            updateMsg.RemoteBoard = getDefaultRemoteBoard();
            updateMsg.RemoteBoard.State.Code = boardStateCodes.PAGE_READ_ERROR;
            updateMsg.RemoteBoard.State.Message = err.message;
            updateMsg.RemoteBoard.Board = null;
            updateMsg.RemoteBoard.BoardConnection = null;
        }

        //echo the message out for the popup (if it is running)
        chrome.runtime.sendMessage({ BoardScrapeMsg: updateMsg });

        try {
            if (socket != null && socket.readyState == WebSocket.OPEN) {
                // if (activeTabId == sender.tab.id) {
                if (hasSentStart == false) {
                    sendWatchStarted(updateMsg.RemoteBoard);
                    NotifyScreen("WATCH STARTED");
                } else {
                    SocketSendMessage(updateMsg);
                }
            }
        } catch (err) {
            sendWatchStopped();
            NotifyScreen("ERROR:", err.message);
        } finally {
            return true; // Required to keep message port open
        }
    } else {
        console.log("Document not ready");
    }
}, PAGE_POLL_DELAY_MS);

console.log("Watching Page...");

//var port = chrome.runtime.connect({ name: WRAPPER_PORT_NAME });

//function onActivatedListener(tabId, changeInfo, tab) {
//    chrome.tabs.get(tabId.tabId, function (tab) {
//        console.debug("New active tab: " + tab.id);
//        activeTabId = tab.id;
//        lastUrl = "";
//        sendWatchStopped();
//    });
//}
//
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
