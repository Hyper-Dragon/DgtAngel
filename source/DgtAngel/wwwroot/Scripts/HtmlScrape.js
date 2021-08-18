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
        return JSON.stringify(remoteBoard);
        ///return remoteBoard;
    }
}




//****************************************************************************************************
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

function RunLiveBoardScrape() {

    myPiecesArray = Array.from(document.getElementsByClassName("piece"));
    piecesStringOut = myPiecesArray.map(o => o.style.backgroundImage.split('/').pop().substring(0, 2) + o.className.replace("piece square-", "").replaceAll("0", "")).join(',');

    whiteClock = document.getElementsByClassName("clock-white")[0];
    blackClock = document.getElementsByClassName("clock-black")[0];

    isWhiteBottom = Array.from(document.getElementById('main-clock-top').parentElement.classList).includes('clock-black');


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

    return liveBoard = {
        "turn": turn,
        "whiteClock": ConvertTimeToMs(whiteClock.innerText),
        "blackClock": ConvertTimeToMs(blackClock.innerText),
        "clockCaptureTime": (new Date()).getTime(),
        "fenString": ConvertLivePieceStringToFen(piecesStringOut),
        "isWhiteBottom": isWhiteBottom,
        "lastMove": lastMove, //What about Figurine ??? getComputedStyle(document.getElementsByClassName('move-text-component')[50],"::Before") ???
        "boardState": boardState,
        "boardMessage": boardMessage
    }
}

function GetPageState() {

    remoteBoard = GetDefaultBoard();

    try {
        if (remoteBoard.PageUrl.includes("chess.com/live")) {
            var liveBoardScrape = RunLiveBoardScrape();

            remoteBoard.Board.FenString = liveBoardScrape.fenString;
            remoteBoard.Board.Turn = liveBoardScrape.turn;
            remoteBoard.Board.IsWhiteOnBottom = liveBoardScrape.isWhiteBottom;
            remoteBoard.Board.LastMove = liveBoardScrape.lastMove;
            remoteBoard.Board.Clocks.WhiteClock = liveBoardScrape.whiteClock;
            remoteBoard.Board.Clocks.BlackClock = liveBoardScrape.blackClock;
            remoteBoard.CaptureTimeMs = liveBoardScrape.clockCaptureTime;
            remoteBoard.BoardConnection.BoardState = liveBoardScrape.boardState;
            remoteBoard.BoardConnection.boardMessage = liveBoardScrape.boardMessage;

            if (remoteBoard.Board.Turn == "NONE") {
                if (remoteBoard.Board.LastMove == "") {
                    remoteBoard.State.Code = "GAME_PENDING";
                } else {
                    remoteBoard.State.Code = "GAME_COMPLETED";
                }
            } else {
                remoteBoard.State.Code = "GAME_IN_PROGRESS";
            }
        } else if (remoteBoard.PageUrl.includes("https://www.chess.com/play")) {
            remoteBoard.State.Code = "UNSUPPORTED_PAGE";
            remoteBoard.Board = null;
            remoteBoard.BoardConnection = null;
        } else {
            remoteBoard.State.Code = "UNKNOWN_PAGE";
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




/**************************************************************************************************************** */



function modifyDOM() {
    //You can play with your DOM here or check URL against your regex
    console.log('Tab script:');
    console.log(document.body);
    return document.body.innerHTML;
}

function piecesFromLiveBoardDOM() {
    pieces = document.getElementsByClassName('piece');
    myPiecesArray = Array.from(pieces);
    piecesStringOut = myPiecesArray.map(o => o.style.backgroundImage.split('/').pop().substring(0, 2) + o.className.replace("piece square-", "").replaceAll("0", "")).join(',');

    whiteClock = document.getElementsByClassName("clock-white")[0];
    blackClock = document.getElementsByClassName("clock-black")[0];
    isWhiteBottom = Array.from(document.getElementById('main-clock-top').parentElement.classList).includes('clock-black');
    turn = "NONE";

    if (whiteClock.outerHTML.includes("clock-playerTurn")) {
        turn = "WHITE";
    }
    else if (blackClock.outerHTML.includes("clock-playerTurn")) {
        turn = "BLACK";
    }

    retVal = turn + '|' + whiteClock.innerText + '|' + blackClock.innerText + '|' + piecesStringOut + '|' + isWhiteBottom;


    return retVal;
}

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

    retVal = '-';

    try {
        //We have permission to access the activeTab, so we can call chrome.tabs.executeScript:
        await chrome.tabs.executeScript({ code: '(' + GetRemoteBoardState + ')();' },
            (results) => {
                const lastErr = chrome.runtime.lastError;

                if (lastErr || results === undefined) {
                    // Just ignore - we dont have access to the tab
                    retVal = "UNDEFINED";
                } else {
                    retVal = results[0];
                    console.log('Outside: ' + retVal);
                }
            });

        //Sleep in loop
        for (let i = 0; i < 5; i++) {
            if (retVal != '-')
                break;

            //Need to wait for the script result 
            await sleep(500);
        }
    } catch (ex) {
        return 'ERROR:' + ex;
    }

    return retVal;
}


async function getPiecesHtml() {

    retVal = '-';

    try {
        //We have permission to access the activeTab, so we can call chrome.tabs.executeScript:
        await chrome.tabs.executeScript({ code: '(' + piecesFromLiveBoardDOM + ')();' },
            (results) => {
                const lastErr = chrome.runtime.lastError;

                if (lastErr || results === undefined) {
                    // Just ignore - we dont have access to the tab
                    retVal = "UNDEFINED";
                } else {
                    retVal = results[0];
                }
            });


        //Sleep in loop
        for (let i = 0; i < 5; i++) {
            if (retVal != '-')
                break;

            //Need to wait for the script result 
            await sleep(500);
        }
    } catch (ex) {
        return 'ERROR:' + ex;
    }

    //console.log('Outside: ' + retVal);
    return retVal;
}

function getPageHtml() {
    //We have permission to access the activeTab, so we can call chrome.tabs.executeScript:
    chrome.tabs.executeScript({
        code: '(' + modifyDOM + ')();' //argument here is a string but function.toString() returns function's code
    }, (results) => {
        //Here we have just the innerHTML and not DOM structure
        console.log('Popup script:')
        console.log(results[0]);
    });
}

