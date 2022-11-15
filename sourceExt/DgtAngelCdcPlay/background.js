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
    NotifyLog(e);
}

var activeTabId = -1;
var lastUrl = "";

// Subscribe to service worker events
self.addEventListener("install", (event) => {
    NotifyLog("DGT Angel Installing");
});

self.addEventListener("activate", (event) => {
    NotifyLog("DGT Angel Activated");
});


//chrome.tabs.onUpdated.addListener( function (tabId, changeInfo, tab) {
//    if (changeInfo.status === 'complete' && tab.active) {
//      setTimeout(() => {
//        chrome.tabs.executeScript(null,  
//            {func: () => {
//                console.log("NO HIDE SET");
//                //Object.defineProperty(document, 'visibilityState', {value: 'visible', writable: true});
//                //Object.defineProperty(document, 'hidden', {value: false, writable: true});
//                //document.dispatchEvent(new Event("visibilitychange"));
//            }}
//        );
//      }, 3000); 
//    }
//  })




// NOT REQUIRED IN THIS RELEASE
// ****************************
// Subscribe to tab events
//chrome.tabs.onActivated.addListener(onActivatedListener);
//chrome.tabs.onUpdated.addListener(onUpdatedListener);
//
//function onActivatedListener(tabId, changeInfo, tab) {
//    chrome.tabs.get(tabId.tabId, function (tab) {
//        NotifyLog("New active tab: " + tab.id);
//        activeTabId = tab.id;
//        lastUrl = "";
//    });
//}
//
//function onUpdatedListener(tabId, changeInfo, tab) {
//    if (changeInfo.status == "loading") {
//        //Can only get the url on loading
//        if (changeInfo.url == undefined) {
//            //not on a supported site
//            lastUrl = "";
//        } else if (changeInfo.lastUrl != changeInfo.url) {
//            //Detect page change on supported site
//            lastUrl = changeInfo.url;
//        }
//    }
//}
