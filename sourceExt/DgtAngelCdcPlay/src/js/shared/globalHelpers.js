/**
 * DTG Angel Global Helper Methods
 * 
 */

 function NotifyScreen(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText }, function (response) {
        if (chrome.runtime.lastError) {
            // Do nothing - trap error if popup isn't visible
        }
    });

    if (LOG_NOTIFY_ON_CONSOLE) {
        console.log(messageText);
    }
}

function NotifyScreenDebug(messageText) {
    chrome.runtime.sendMessage({ WorkerMessage: messageText }, function (response) {
        if (chrome.runtime.lastError) {
            // Do nothing - trap error if popup isn't visible
        }
    });

    if (LOG_NOTIFY_DEBUG_ON_CONSOLE) {
        console.debug(messageText);
    }
}

function NotifyLog(messageText) {
    console.log(messageText);
}