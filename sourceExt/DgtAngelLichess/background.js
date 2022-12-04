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
