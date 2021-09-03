/**
 * DTG Angel Global Helper Methods
 * 
 */

 function NotifyScreen(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    if (LOG_NOTIFY_ON_CONSOLE) {
        console.log(messageText);
    }
}

function NotifyScreenDebug(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText });
    if (LOG_NOTIFY_DEBUG_ON_CONSOLE) {
        console.debug(messageText);
    }
}