/**
 * DTG Angel Generic injection script
 *  - Calls GetRemoteBoardState() in the scrape file added by the manifest
 * 
 */
var port = chrome.runtime.connect({name: WRAPPER_PORT_NAME});

setInterval(() => {
    if (document.readyState === "complete") {
        updateMsg = GetBlankMessage(WRAPPER_SOURCE_NAME,"STATE_UPDATED");

        try {
            updateMsg.RemoteBoard = GetRemoteBoardState();
        } catch (err) {
            updateMsg.RemoteBoard = getDefaultRemoteBoard();
            updateMsg.RemoteBoard.State.Code = "PAGE_READ_ERROR";
            updateMsg.RemoteBoard.State.Message = err.message;
            updateMsg.RemoteBoard.Board = null;
            updateMsg.RemoteBoard.BoardConnection = null;
        }
        
        port.postMessage({ BoardScrapeMsg: updateMsg });
        
        //echo for popup
        chrome.runtime.sendMessage({ BoardScrapeMsg: updateMsg });
    
    } else {
        console.log("Document not ready");
    }
}, PAGE_POLL_DELAY_MS);

console.log("Watching Page...");

