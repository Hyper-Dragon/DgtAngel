/**
 * 
 * DTG Angel Generic injection script
 *  - Calls GetRemoteBoardState() in the scrape file added by the manifest
 * 
 */
setInterval(() => {
    if (document.readyState === "complete") {
        updateMsg = GetBlankMessage("ANGEL:WATCHER","STATE_UPDATED");

        try {
            updateMsg.RemoteBoard = GetRemoteBoardState();
        } catch (err) {
            updateMsg.RemoteBoard = getDefaultRemoteBoard();
            updateMsg.RemoteBoard.State.Code = "PAGE_READ_ERROR";
            updateMsg.RemoteBoard.State.Message = err.message;
            updateMsg.RemoteBoard.Board = null;
            updateMsg.RemoteBoard.BoardConnection = null;
        }

        chrome.runtime.sendMessage({ BoardScrapeMsg: updateMsg });
    } else {
        console.log("Document not ready");
    }
}, 500);

console.log("Watching Page...");
