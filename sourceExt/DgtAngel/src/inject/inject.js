function GetRemoteBoardState() {
    // Setup Default Return Object
    remoteBoard = {
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
            Turn: "NONE",
            LastMove: "",
            FenString: "",
            Clocks: {
                CaptureTimeMs: 0,
                WhiteClock: 0,
                BlackClock: 0,
            },
        },
    };

    // Setup Blank Board
    var board = [
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
    ];

    try {
        if (
            remoteBoard.PageUrl.includes("chess.com/game/live/") ||
            remoteBoard.PageUrl.includes("chess.com/play")
        ) {
            if (
                Array.from(
                    document.getElementsByClassName(
                        "new-game-focus-mode-time-selector"
                    )
                ).length == 1
            ) {
                remoteBoard.State.Code = "UNKNOWN_PAGE";
                remoteBoard.State.Message =
                    "Do not use focus mode on the *Play* screen...there is no access to the last move!";
                remoteBoard.Board = null;
                remoteBoard.BoardConnection = null;
            } else {
                // Get all the pieces
                myPiecesArray = Array.from(
                    document.getElementsByClassName("piece")
                );
                piecesStringOut = myPiecesArray
                    .map(
                        (o) =>
                            o.style.backgroundImage
                                .split("/")
                                .pop()
                                .substring(0, 2) +
                            o.className
                                .replace("piece ", "")
                                .replace(" square-", "")
                                .replaceAll("0", "")
                    )
                    .join(",");

                // Add them to the board
                piecesStringArray = piecesStringOut.split(",");
                while (piecesStringArray.length) {
                    var piece = piecesStringArray.pop();
                    board[8 - parseInt(piece[3])][parseInt(piece[2]) - 1] =
                        piece[0] + "" + piece[1];
                }

                // Generate FEN String
                let result = "";
                for (let y = 0; y < board.length; y++) {
                    let empty = 0;
                    for (let x = 0; x < board[y].length; x++) {
                        let c = board[y][x][0]; // Fixed
                        if (c == "w" || c == "b") {
                            if (empty > 0) {
                                result += empty.toString();
                                empty = 0;
                            }
                            if (c == "w") {
                                result += board[y][x][1].toUpperCase(); // Fixed
                            } else {
                                result += board[y][x][1].toLowerCase(); // Fixed
                            }
                        } else {
                            empty += 1;
                        }
                    }
                    if (empty > 0) {
                        // Fixed
                        result += empty.toString();
                    }
                    if (y < board.length - 1) {
                        // Added to eliminate last '/'
                        result += "/";
                    }
                }

                moveList = Array.from(document.getElementsByClassName("move"));
                lastMove = "";

                if (moveList.length > 0) {
                    lastMoveRow = Array.from(
                        document.getElementsByClassName("move")
                    )
                        .pop()
                        .innerText.trim()
                        .split("\n");

                    if (lastMoveRow.length == 2) {
                        lastMove = lastMoveRow[1];
                    } else if (lastMoveRow.length == 3) {
                        lastMove = lastMoveRow[2];
                    }
                }

                // Now the clocks....
                whiteClock = document.getElementsByClassName("clock-white")[0];
                blackClock = document.getElementsByClassName("clock-black")[0];
                turn = "NONE";

                if (lastMove != "") {
                    if (whiteClock.classList.contains("clock-player-turn")) {
                        turn = "WHITE";
                    } else if (
                        blackClock.classList.contains("clock-player-turn")
                    ) {
                        turn = "BLACK";
                    } else {
                        turn = "ERROR";
                    }
                }

                if (
                    document.getElementsByClassName(
                        "dgt-board-status-component"
                    ).length == 0
                ) {
                    boardState = "UNKNOWN";
                    boardMessage = "";
                } else {
                    boardState = "ACTIVE";
                    boardMessage = document
                        .getElementsByClassName("dgt-board-status-component")[0]
                        .innerText.trim()
                        .replaceAll("\n", "");
                }

                //For the time conversion
                var w_mul = [3600000, 60000, 1000];
                wcConTime = 0;
                whiteClock.innerText
                    .split(":")
                    .reverse()
                    .forEach(
                        (element) =>
                            (wcConTime +=
                                w_mul.pop() * parseFloat(element.trim()))
                    );

                var b_mul = [3600000.0, 60000.0, 1000.0];
                bcConTime = 0;
                blackClock.innerText
                    .split(":")
                    .reverse()
                    .forEach(
                        (element) =>
                            (bcConTime +=
                                b_mul.pop() * parseFloat(element.trim()))
                    );

                remoteBoard.Board.FenString = result;
                remoteBoard.Board.Turn = turn;
                remoteBoard.Board.IsWhiteOnBottom =
                    whiteClock.classList.contains("clock-bottom");
                remoteBoard.Board.LastMove = lastMove;

                remoteBoard.Board.Clocks.WhiteClock = wcConTime;
                remoteBoard.Board.Clocks.BlackClock = bcConTime;
                remoteBoard.Board.Clocks.CaptureTimeMs = new Date().getTime();
                remoteBoard.BoardConnection.BoardState = boardState;
                remoteBoard.BoardConnection.ConMessage = boardMessage;

                if (remoteBoard.Board.Turn == "NONE") {
                    if (remoteBoard.Board.LastMove == "") {
                        remoteBoard.State.Code = "GAME_PENDING";
                    } else {
                        remoteBoard.State.Code = "GAME_COMPLETED";
                    }
                } else {
                    if (
                        remoteBoard.Board.LastMove == "1-0" ||
                        remoteBoard.Board.LastMove == "0-1" ||
                        remoteBoard.Board.LastMove == "1/2-1-2"
                    ) {
                        remoteBoard.Board.Turn = "NONE";
                        remoteBoard.State.Code = "GAME_COMPLETED";
                    } else {
                        remoteBoard.State.Code = "GAME_IN_PROGRESS";
                    }
                }
            }
        } else {
            remoteBoard.State.Code = "UNKNOWN_PAGE";
            remoteBoard.State.Message =
                "If you can see this the manifest has a config error!";
            remoteBoard.Board = null;
            remoteBoard.BoardConnection = null;
        }
    } catch (err) {
        remoteBoard.State.Code = "SCRIPT_SCRAPE_ERROR";
        remoteBoard.State.Message = err;
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    } finally {
        return remoteBoard;
    }
}

//**********************************************************************************/

const delay = (ms) => new Promise((res) => setTimeout(res, ms));

function GetBlankMessage(messageType) {
    blankMsg = {
        Source: "ANGEL",
        MessageType: messageType,
        Message: window.location.toString(),
        RemoteBoard: null,
    };

    return blankMsg;
}

console.log("Watching Page...");

setInterval(() => {
    if (document.readyState === "complete") {
        updateMsg = GetBlankMessage("STATE_UPDATED");
        updateMsg.RemoteBoard = GetRemoteBoardState();
        chrome.runtime.sendMessage({ BoardScrapeMsg: updateMsg });
    } else {
        console.log("Document not ready");
    }
}, 500);
