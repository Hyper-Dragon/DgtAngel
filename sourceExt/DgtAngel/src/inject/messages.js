function GetBlankMessage(messageSource, messageType) {
    blankMsg = {
        AngelVersion: "0.0.3",
        Source: messageSource,
        MessageType: messageType,
        MsgTimeMs: new Date().getTime(),
        Message: window.location.toString(),
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