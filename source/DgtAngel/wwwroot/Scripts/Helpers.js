//document.getElementById("test").addEventListener('click', () => {
//    console.log("Popup DOM fully loaded and parsed");


function addIndexToContextMenu() {
    chrome.contextMenus.removeAll();
    chrome.contextMenus.create({
        title: "About DgtAngel",
        contexts: ["browser_action"],
        onclick: function () {
            window.open("index.html", "dgtangelidx");
        }
    });
}

function playAudioFromBkg(audioId) {
    document.getElementById(audioId).play();
}

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}

function writeToConsole(msg) {
    console.log('changeme:'+msg)
}

function writeDebugToConsole(msg) {
    console.log(msg)
}

function writeInfoToConsole(msg) {
    console.info(msg)
}

function writeWarningToConsole(msg) {
    console.warn(msg)
}

function writeErrorToConsole(msg) {
    console.error(msg)
}