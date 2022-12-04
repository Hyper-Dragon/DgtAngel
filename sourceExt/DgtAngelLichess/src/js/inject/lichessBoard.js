/**
 * DTG Angel Page Scrape
 *  - Lichess
 *
 */
function IsUrlValid(urlToTest) {
    if (
        urlToTest.includes("lichess.org/") ||
        urlToTest.includes("lichess.org/")
    ) {
        /// console.log(urlToTest+" Test Returns TRUE");
        return true;
    } else {
        /// console.log(urlToTest+"URL Test Returns FALSE");
        return false;
    }
}

function GetRemoteBoardState() {
    // Setup Default Return Object + blank board
    remoteBoard = getDefaultRemoteBoard();
    var board = getBlankBoard();

    if (document.visibilityState == "hidden") {
        remoteBoard.State.Code = boardStateCodes.LOST_VISABILITY;
        remoteBoard.State.Message =
            "The board needs to be visible in the browser.";
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    } else {
        // Get all the pieces
        scanBoard = document.getElementsByTagName("cg-board")[0];
        sqSize = scanBoard.getBoundingClientRect().width / 8;
        pieces = scanBoard.getElementsByTagName("piece");
        isPlayerWhite = false;

        if (document.getElementsByClassName("ranks black").length == 0) {
            isPlayerWhite = true;
        }

        var piecesStringOut = [];

        for (const piece of pieces) {
            if (typeof piece !== "undefined") {
                translate = piece.style.transform
                    .split("(")[1]
                    .split(")")[0]
                    .replace(",", "")
                    .split("px");
                transX = parseFloat(translate[0].trim());
                transY = parseFloat(translate[1].trim());

                col = piece.className[0];
                ptype = piece.className[6];

                if (ptype == "k") {
                    if (piece.className[7] == "n") {
                        ptype = "n";
                    }
                }

                calccode =
                    col +
                    ptype +
                    Math.round(transX / sqSize) +
                    Math.round(transY / sqSize);
                piecesStringOut += calccode + ",";
            }
        }

        // Add them to the board
        piecesStringArray = piecesStringOut.split(",");
        while (piecesStringArray.length) {
            var piece = piecesStringArray.pop();

            if (piece.length > 0) {
                if (piece.length == 6) {
                    piece = piece.substring(2);
                }

                idx1 = parseInt(piece[3]);
                idx2 = parseInt(piece[2]);

                if (!isPlayerWhite) {
                    idx1 = 7 - idx1;
                    idx2 = 7 - idx2;
                }

                board[idx1][idx2] = piece[0] + "" + piece[1];
            }
        }

        // Now the clocks....
        //whiteClock = document.getElementsByClassName("clock-white")[0];
        //blackClock = document.getElementsByClassName("clock-black")[0];

        // Use the clocks to detect the turn
        turn = turnCodes.NONE;

        if (
            document
                .getElementsByClassName("rclock")[0]
                .classList.contains("running")
        ) {
            if (isPlayerWhite) {
                turn = turnCodes.BLACK;
            } else {
                turn = turnCodes.WHITE;
            }
        } else if (
            document
                .getElementsByClassName("rclock")[1]
                .classList.contains("running")
        ) {
            if (isPlayerWhite) {
                turn = turnCodes.WHITE;
            } else {
                turn = turnCodes.BLACK;
            }
        }

        boardState = dgtStateCodes.UNKNOWN;
        boardMessage = "Unavailable.";

        remoteBoard.Board.FenString = calculateFen(board);
        remoteBoard.Board.ClockTurn = turn;
        remoteBoard.Board.IsWhiteOnBottom = isPlayerWhite;

        remoteBoard.Board.Clocks.WhiteClock = 0;
        remoteBoard.Board.Clocks.BlackClock = 0; 

        if (isPlayerWhite) {
            remoteBoard.Board.Clocks.WhiteClock = convertClockStringToMs(
                document
                    .getElementsByClassName("rclock")[1]
                    .innerText.replaceAll("\n", "")
                    .split(".")[0]
            );
            remoteBoard.Board.Clocks.BlackClock = convertClockStringToMs(
                document
                    .getElementsByClassName("rclock")[0]
                    .innerText.replaceAll("\n", "")
                    .split(".")[0]
            );
        } else {
            remoteBoard.Board.Clocks.WhiteClock = convertClockStringToMs(
                document
                    .getElementsByClassName("rclock")[0]
                    .innerText.replaceAll("\n", "")
                    .split(".")[0]
            );
            remoteBoard.Board.Clocks.BlackClock = convertClockStringToMs(
                document
                    .getElementsByClassName("rclock")[1]
                    .innerText.replaceAll("\n", "")
                    .split(".")[0]
            );
        }

        remoteBoard.Board.Clocks.CaptureTimeMs = new Date().getTime();
        remoteBoard.BoardConnection.BoardState = boardState;
        remoteBoard.BoardConnection.ConMessage = boardMessage;

        //Calculate the game state
        if (
            remoteBoard.Board.FenString ==
            "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"
        ) {
            remoteBoard.State.Code = boardStateCodes.GAME_PENDING;
            remoteBoard.Board.ClockTurn = turnCodes.WHITE;
        } else if (
            remoteBoard.Board.Clocks.WhiteClock == 0 ||
            remoteBoard.Board.Clocks.BlackClock == 0
        ) {
            remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
            remoteBoard.Board.ClockTurn = turnCodes.NONE;
        } else if (remoteBoard.Board.ClockTurn == turnCodes.NONE) {
            remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        } else {
            remoteBoard.State.Code = boardStateCodes.GAME_IN_PROGRESS;
        }
    }

    return remoteBoard;
}
