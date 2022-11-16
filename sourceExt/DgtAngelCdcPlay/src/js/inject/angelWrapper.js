/**
 * DTG Angel Generic injection script
 *  - Calls GetRemoteBoardState() in the scrape file added by the manifest
 *
 */

console.log("Always active set");
Object.defineProperty(document, "visibilityState", {
    value: "visible",
    writable: true,
});
Object.defineProperty(document, "hidden", { value: false, writable: true });
document.dispatchEvent(new Event("visibilitychange"));

window.addEventListener("beforeunload", function (e) {
    //if (hasSentStart == true) {
    sendWatchStopped();
    //}
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
                    "Unsupported page " + window.location.toString();
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
        chrome.runtime.sendMessage({BoardScrapeMsg: updateMsg}, function (response) {
            if (chrome.runtime.lastError) {
                // Do nothing - trap error if popup isn't visible
            }
        });

        try {
            if (socket != null && socket.readyState == WebSocket.OPEN) {
                if (
                    updateMsg.RemoteBoard.State.Code ==
                    boardStateCodes.UNKNOWN_PAGE
                ) {
                    //if (hasSentStart == true) {
                    //    sendWatchStopped();
                    //}
                } else {
                    if (hasSentStart == false) {
                        sendWatchStarted(updateMsg.RemoteBoard);
                        NotifyScreen("WATCH STARTED");
                    } else {
                        SocketSendMessage(updateMsg);
                    }
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
