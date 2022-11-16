/**
 * DTG Angel Messages
 *  - (shared with the content and background scripts)
 *  - Defines supported state codes
 */
const messageStateCodes = {
	ERROR_IF_NOT_SET: "ERROR_IF_NOT_SET",
    WATCH_STARTED: "WATCH_STARTED",
    STATE_UPDATED: "STATE_UPDATED",
    WATCH_STOPPED: "WATCH_STOPPED",
	KEEP_ALIVE: "KEEP_ALIVE",
};

const boardStateCodes = {
	UNKNOWN_PAGE: "UNKNOWN_PAGE",
    LOST_VISABILITY: "LOST_VISABILITY",
	GAME_PENDING: "GAME_PENDING",
    GAME_IN_PROGRESS: "GAME_IN_PROGRESS",
	GAME_COMPLETED: "GAME_COMPLETED",
    PAGE_READ_ERROR: "PAGE_READ_ERROR",
};

const turnCodes = {
    WHITE : "WHITE",
    BLACK : "BLACK",
    NONE : "NONE",
};

const dgtStateCodes = {
    UNKNOWN : "UNKNOWN",
    ACTIVE : "ACTIVE",
};

function GetBlankMessage(messageSource, messageType) {

    if(typeof(window) !== "undefined"){
        windowLocation=window.location.toString();
    }else{
        windowLocation="Background";
    }

    blankMsg = {
        AngelPluginName: chrome.runtime.getManifest().name,
        AngelPluginVersion: chrome.runtime.getManifest().version,
        AngelMessageVersion: ANGEL_MESSAGE_VERSION,
        Source: messageSource,
        MessageType: messageType,
        MsgTimeMs: new Date().getTime(),
        Message: windowLocation,
        RemoteBoard: null
    };

    return blankMsg;
}

function getDefaultRemoteBoard() {
    // Setup Default Return Object
    return {
        PageUrl: window.location.toString(),
        CaptureTimeMs: new Date().getTime(),
        State: {
            Code: messageStateCodes.ERROR_IF_NOT_SET,
            Message: "",
        },
        BoardConnection: {
            BoardState: "",
            ConMessage: "",
        },
        Board: {
            IsWhiteOnBottom: true,
            Turn: "",
            LastMove: "",
            FenString: "",
            LastFenString: "",
            Clocks: {
                CaptureTimeMs: 0,
                WhiteClock: 0,
                BlackClock: 0,
            },
        },
    };
}