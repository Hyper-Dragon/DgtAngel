// ********************************************************************************
//       ___                       _        _ _       _
//      /  _>  ___ ._ _  ___  _ _ <_> ___  | | | ___ | | ___  ___  _ _  ___
//      | <_/\/ ._>| ' |/ ._>| '_>| |/ | ' |   |/ ._>| || . \/ ._>| '_><_-<
//      `____/\___.|_|_|\___.|_|  |_|\_|_. |_|_|\___.|_||  _/\___.|_|  /__/
//                                                      |_|
// ********************************************************************************

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}


function Queue() {
    this.elements = [];
}

Queue.prototype.enqueue = function (e) {
    this.elements.push(e);
};

// remove an element from the front of the queue
Queue.prototype.dequeue = function () {
    return this.elements.shift();
};

// check if the queue is empty
Queue.prototype.isEmpty = function () {
    return this.elements.length == 0;
};

// get the element at the front of the queue
Queue.prototype.peek = function () {
    return !this.isEmpty() ? this.elements[0] : undefined;
};

Queue.prototype.length = function () {
    return this.elements.length;
}

// ********************************************************************************
//                  ___    _                _
//                 / __> _| |_  ___  _ _  _| |_  _ _  ___
//                 \__ \  | |  <_> || '_>  | |  | | || . \
//                 <___/  |_|  <___||_|    |_|  `___||  _/
//                                                   |_|
// ********************************************************************************

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

// ********************************************************************************
//             ___         _  _        ___  _
//            | . | _ _  _| |<_> ___  | . \| | ___  _ _  ___  _ _
//            |   || | |/ . || |/ . \ |  _/| |<_> || | |/ ._>| '_>
//            |_|_|`___|\___||_|\___/ |_|  |_|<___|`_. |\___.|_|
//                                                 <___'
// ********************************************************************************

let audioPlaylist = new Queue();
let isAudioPlaying = false;
let areAudioEventListenersAdded = false;

function playAudioFromBkg(audioId) {
    //Can't setup the Event Listener in background.html so we have to do it here (security error)
    if (!areAudioEventListenersAdded) {
        allAudioTags = Array.from(document.getElementsByTagName('audio'));
        allAudioTags.forEach(item => item.addEventListener('ended', function () { playNext(); }));
    }

    //add the requested file to the playlist
    audioPlaylist.enqueue(audioId);

    //if no audio is playing then start
    //if audio is playing the next track will play automatically
    if (!isAudioPlaying) {
        isAudioPlaying = true;
        playNext();
    }
}

function playNext() {
    //if there are more tracks to play keep playing - otherwise flag audio as stopped
    if (!audioPlaylist.isEmpty()) {
        nextTrack = audioPlaylist.dequeue();
        document.getElementById(nextTrack).play();
    } else {
        isAudioPlaying = false;
    }
}

// ********************************************************************************
//                     | |   ___  ___  ___ <_>._ _  ___ 
//                     | |_ / . \/ . |/ . || || ' |/ . |
//                     |___|\___/\_. |\_. ||_||_|_|\_. |
//                               <___'<___'        <___'                             
// ********************************************************************************

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



