function GetDefaultBoard() {

    var RemoteBoard = {
        "PageUrl": window.location.toString(),
        "CaptureTimeMs": (new Date()).getTime(),
        "State": {
            "Code": "ERROR_IF_NOT_SET",
            "Message": ""
        },
        "BoardConnection": {
            "BoardState": "",
            "ConMessage": ""
        },
        "Board": {
            "IsWhiteOnBottom": true,
            "Turn": "NONE",
            "LastMove": "",
            "FenString": "",
            "Clocks": {
                "WhiteClock": 0,
                "BlackClock": 0
            }
        }
    };

    return RemoteBoard;
}


function ConvertTimeToMs(timeStr) {
    var mul = [3600000.0, 60000.0, 1000.0];

    retVal = 0.0;
    timeStr.split(":").reverse().forEach(element => retVal += mul.pop() * parseFloat(element));

    return retVal;
}




//br88,bn78,bb68,bk58,bq48,bb38,bn36,br18,bp87,bp77,bp67,bp57,bp47,bp35,bp27,bp17,wp82,wp72,wp62,wp54,wp42,wp32,wp22,wp12,wr81,wn63,wb61,wk51,wq41,wb31,wn21,wr11
function ConvertLivePieceStringToFen(pieceString) {
    var board = [
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
        ['em', 'em', 'em', 'em', 'em', 'em', 'em', 'em'],
    ];

    pieces = pieceString.split(',');
    while (pieces.length) {
        var piece = pieces.pop();
        board[(parseInt(piece[3]) - 1)][8 - parseInt(piece[2])] = piece[0] + "" + piece[1];
    }

    return BoardToFen(board);
}


/**************************************************************************************************************** */


function BoardToFen(board) {
    let result = "";
    for (let y = 0; y < board.length; y++) {
        let empty = 0;
        for (let x = 0; x < board[y].length; x++) {
            let c = board[y][x][0];  // Fixed
            if (c == 'w' || c == 'b') {
                if (empty > 0) {
                    result += empty.toString();
                    empty = 0;
                }
                if (c == 'w') {
                    result += board[y][x][1].toUpperCase();  // Fixed
                } else {
                    result += board[y][x][1].toLowerCase();  // Fixed
                }
            } else {
                empty += 1;
            }
        }
        if (empty > 0)   // Fixed
        {
            result += empty.toString();
        }
        if (y < board.length - 1)  // Added to eliminate last '/'
        {
            result += '/';
        }
    }

    return result;
}


function GetPageState() {

    remoteBoard = GetDefaultBoard();

    try {
        if (remoteBoard.PageUrl.includes("chess.com")) {
            var liveBoardScrape = RunBoardScrape();

            remoteBoard.Board.FenString = liveBoardScrape.fenString;
            remoteBoard.Board.Turn = liveBoardScrape.turn;
            remoteBoard.Board.IsWhiteOnBottom = liveBoardScrape.isWhiteBottom;
            remoteBoard.Board.LastMove = liveBoardScrape.lastMove;
            remoteBoard.Board.Clocks.WhiteClock = liveBoardScrape.whiteClock;
            remoteBoard.Board.Clocks.BlackClock = liveBoardScrape.blackClock;
            remoteBoard.CaptureTimeMs = liveBoardScrape.clockCaptureTime;
            remoteBoard.BoardConnection.BoardState = liveBoardScrape.boardState;
            remoteBoard.BoardConnection.ConMessage = liveBoardScrape.boardMessage;

            if (remoteBoard.Board.Turn == "NONE") {
                if (remoteBoard.Board.LastMove == "") {
                    remoteBoard.State.Code = "GAME_PENDING";
                } else {
                    remoteBoard.State.Code = "GAME_COMPLETED";
                }
            } else {
                remoteBoard.State.Code = "GAME_IN_PROGRESS";
            }
        } else {
            remoteBoard.State.Code = "UNKNOWN_PAGE";
            remoteBoard.State.Message = "If you can see this the manifest has a config error!";
            remoteBoard.Board = null;
            remoteBoard.BoardConnection = null;
        }
    }
    catch (err) {
        remoteBoard.State.Code = "SCRIPT_SCRAPE_ERROR";
        remoteBoard.State.Message = err;
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    } finally {
        return remoteBoard;
    }
}


setInterval(myMethod, 1000);

function myMethod() {
    pageState = JSON.stringify(GetPageState());
    console.log(pageState);
    browser.runtime.sendMessage({ pageState });
}



//chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
//    console.log("msg recieved");
//    if (message === 'get-page-state') {
//        //stateOut = GetPageState();
//        console.log("send resp");
//        sendResponse(true);
//    }
//});