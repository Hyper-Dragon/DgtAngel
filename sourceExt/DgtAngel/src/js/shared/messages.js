/**
 * 
 * DTG Angel Messages
 *  - (shared with the content and background scripts)
 * 
 */
function GetBlankMessage(messageSource, messageType) {

    if(typeof(window) !== "undefined"){
        windowLocation=window.location.toString();
    }else{
        windowLocation="Background";
    }

    blankMsg = {
        AngelVersion: chrome.runtime.getManifest().version,
        Source: messageSource,
        MessageType: messageType,
        MsgTimeMs: new Date().getTime(),
        Message: windowLocation,
        RemoteBoard: null,
    };

    return blankMsg;
}

function getDefaultRemoteBoard() {
    // Setup Default Return Object
    return {
        PageUrl: window.location.toString(),
        CaptureTimeMs: new Date().getTime(),
        State: {
            Code: "ERROR_IF_NOT_SET",
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
            Clocks: {
                CaptureTimeMs: 0,
                WhiteClock: 0,
                BlackClock: 0,
            },
        },
    };
}