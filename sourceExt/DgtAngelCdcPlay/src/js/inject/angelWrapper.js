/**
 * DTG Angel Generic injection script
 *  - Calls GetRemoteBoardState() in the scrape file added by the manifest
 *
 */

window.addEventListener("beforeunload", function (e) {
    sendWatchStopped();
});

//Track the last FEN seen
lastFen = "-";
currentFen = "-";

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

                if (
                    updateMsg.RemoteBoard != null &&
                    updateMsg.RemoteBoard.Board != null
                ) {
                    updateMsg.RemoteBoard.Board.LastFenString = lastFen;
                }

                if (currentFen == "-") {
                    lastFen = "-";
                    currentFen = updateMsg.RemoteBoard.Board.FenString;
                } else if (
                    updateMsg.RemoteBoard.Board.FenString != currentFen
                ) {
                    lastFen = currentFen;
                    currentFen = updateMsg.RemoteBoard.Board.FenString;
                }
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
                lastFen = "-";
                currentFen = "-";
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
            lastFen = "-";
            currentFen = "-";
        }

        //echo the message out for the popup (if it is running)
        chrome.runtime.sendMessage({ BoardScrapeMsg: updateMsg });

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
