function GetRemoteBoardState() {
    // Setup Default Return Object
    remoteBoard = {
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

    // Setup Blank Board
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

    try {
        if (remoteBoard.PageUrl.includes("chess.com/live")) {

            // Get all the pieces
            myPiecesArray = Array.from(document.getElementsByClassName("piece"));
            pieceString = myPiecesArray.map(o => o.style.backgroundImage.split('/').pop().substring(0, 2) + o.className.replace("piece square-", "").replaceAll("0", "")).join(',');

            // Add them to the board
            piecesStringArray = pieceString.split(',');
            while (piecesStringArray.length) {
                var piece = piecesStringArray.pop();
                board[(parseInt(piece[3]) - 1)][8 - parseInt(piece[2])] = piece[0] + "" + piece[1];
            }

            // Generate FEN String
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

            whiteClock = document.getElementsByClassName("clock-white")[0];
            blackClock = document.getElementsByClassName("clock-black")[0];
            turn = "NONE";

            if (whiteClock.outerHTML.includes("clock-playerTurn")) {
                turn = "WHITE";
            }
            else if (blackClock.outerHTML.includes("clock-playerTurn")) {
                turn = "BLACK";
            }

            moveList = Array.from(document.getElementsByClassName('move-text-component'));
            lastMove = "";

            if (moveList.length > 0) {
                lastMove = Array.from(document.getElementsByClassName('move-text-component')).pop().innerText;
            }

            if (document.getElementsByClassName('dgt-board-status-component').length == 0) {
                boardState = "UNKNOWN";
                boardMessage = "";
            } else {
                boardState = "ACTIVE";
                boardMessage = document.getElementsByClassName('dgt-board-status-component')[0].innerText.trim().replaceAll('\n', '');
            }

            //For the time conversion
            var w_mul = [3600000, 60000, 1000];
            wcConTime = 0;
            whiteClock.innerText.split(":").reverse().forEach(element => wcConTime += w_mul.pop() * parseFloat(element))

            var b_mul = [3600000.0, 60000.0, 1000.0];
            bcConTime = 0;
            blackClock.innerText.split(":").reverse().forEach(element => bcConTime += b_mul.pop() * parseFloat(element))

            remoteBoard.Board.FenString = result;
            remoteBoard.Board.Turn = turn;
            remoteBoard.Board.IsWhiteOnBottom = Array.from(document.getElementById('main-clock-top').parentElement.classList).includes('clock-black');
            remoteBoard.Board.LastMove = lastMove;
            remoteBoard.Board.Clocks.WhiteClock = wcConTime;
            remoteBoard.Board.Clocks.BlackClock = bcConTime;
            remoteBoard.CaptureTimeMs = (new Date()).getTime();
            remoteBoard.BoardConnection.BoardState = boardState;
            remoteBoard.BoardConnection.ConMessage = boardMessage;

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

/*****************************************************************************************************************/



function piecesFromPlayBoardDOM() {
    pieces = document.getElementsByClassName('piece');
    myPiecesArray = Array.from(pieces);
    piecesStringOut = myPiecesArray.map(o => o.className.replace("piece ", "").replace(" square-", "")).join(',');

    whiteClock = document.getElementsByClassName("clock-white")[0].innerText;
    blackClock = document.getElementsByClassName("clock-black")[0].innerText;

    retVal = whiteClock + '|' + blackClock + '|' + piecesStringOut;

    //console.log('Returning: ' + retVal);

    return retVal;
}



async function getGetRemoteBoardStateJson() {

    // Setup Default Return Object
    retVal = {
        "PageUrl": window.location.toString(),
        "CaptureTimeMs": (new Date()).getTime(),
        "State": {
            "Code": "RUNNING",
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

    try {
        //We have permission to access the activeTab, so we can call chrome.tabs.executeScript:
        await chrome.tabs.executeScript({ code: '(' + GetRemoteBoardState + ')();' },
            (results) => {
                const lastErr = chrome.runtime.lastError;

                if (lastErr || results === undefined) {
                    // Just ignore - we dont have access to the tab
                    retVal.BoardConnection = null;
                    retVal.Board = null;
                    retVal.State.Code = "UNKNOWN_PAGE";
                    retVal.State.Message = "Results undefined";
                } else {
                    retVal = results[0];
                }
            });

        //Sleep in loop
        for (let i = 0; i < 5; i++) {
            if (retVal.State.Code != 'RUNNING')
                break;

            //Need to wait for the script result 
            await sleep(500);
        }
    } catch (ex) {
        retVal.BoardConnection = null;
        retVal.Board = null;
        retVal.State.Code = "UNKNOWN_PAGE";
        retVal.State.Message = ex.message;
    }

    return JSON.stringify(retVal);
}
