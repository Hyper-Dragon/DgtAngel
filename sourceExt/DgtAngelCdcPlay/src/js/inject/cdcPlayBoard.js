/**
 * DTG Angel Page Scrape
 *  - Chess.com Play Board
 *
 */
function IsUrlValid(urlToTest) {
    if (
        urlToTest.includes("chess.com/game/live/") ||
        urlToTest.includes("chess.com/play/online")
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
            "On the play screen the board needs to be visible.";
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    } else {
        // Get all the pieces
        piecesStringOut = Array.from(document.getElementsByClassName("piece"))
            .map(
                (o) =>
                    o.style.backgroundImage.split("/").pop().substring(0, 2) +
                    o.className
                        .replace("piece ", "")
                        .replace("square-", "")
                        .replaceAll(" ", "")
                        .replaceAll("0", "")
            )
            .join(",");

        // Add them to the board
        piecesStringArray = piecesStringOut.split(",");
        while (piecesStringArray.length) {
            var piece = piecesStringArray.pop();

            if (piece.length == 6) {
                piece = piece.substring(2);
            }

            // The class name can be [piece][square] OR [square][piece]
            // Check if we are starting with a number and parse accordingly
            if (piece.match(/^\d/)) {
                board[8 - parseInt(piece[1])][parseInt(piece[0]) - 1] =
                    piece[2] + "" + piece[3];
            } else {
                board[8 - parseInt(piece[3])][parseInt(piece[2]) - 1] =
                    piece[0] + "" + piece[1];
            }
        }

        // Now the clocks....
        whiteClock = document.getElementsByClassName("clock-white")[0];
        blackClock = document.getElementsByClassName("clock-black")[0];
        turn = "NONE";

        // Use the clocks to detect the turn
        if (whiteClock.classList.contains("clock-player-turn")) {
            turn = turnCodes.WHITE;
        } else if (blackClock.classList.contains("clock-player-turn")) {
            turn = turnCodes.BLACK;
        }

        // Finally the DTG board status if we can get it
        if (
            document.getElementsByClassName("dgt-board-status-component")
                .length == 0
        ) {
            boardState = dgtStateCodes.UNKNOWN;
            boardMessage = "Not found on page";
        } else {
            boardState = dgtStateCodes.ACTIVE;
            boardMessage = document
                .getElementsByClassName("dgt-board-status-component")[0]
                .innerText.trim()
                .replaceAll("\n", "");
        }

        remoteBoard.Board.FenString = calculateFen(board);
        remoteBoard.Board.Turn = turn;
        remoteBoard.Board.IsWhiteOnBottom =
            whiteClock.classList.contains("clock-bottom");
        remoteBoard.Board.LastMove = "CALC";
        remoteBoard.Board.Clocks.WhiteClock = convertClockStringToMs(
            whiteClock.innerText
        );
        remoteBoard.Board.Clocks.BlackClock = convertClockStringToMs(
            blackClock.innerText
        );
        remoteBoard.Board.Clocks.CaptureTimeMs = new Date().getTime();
        remoteBoard.BoardConnection.BoardState = boardState;
        remoteBoard.BoardConnection.ConMessage = boardMessage;

        //Calculate the game state

        if (remoteBoard.Board.Turn == turnCodes.NONE) {
            remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        } else {
            remoteBoard.State.Code = boardStateCodes.GAME_IN_PROGRESS;
        }

        //if (remoteBoard.Board.Turn == turnCodes.NONE) {
        //    if (remoteBoard.Board.LastMove == "") {
        //        remoteBoard.State.Code = boardStateCodes.GAME_PENDING;
        //    } else {
        //        remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        //    }
        //} else {
        //    if (
        //        remoteBoard.Board.LastMove == "1-0" ||
        //        remoteBoard.Board.LastMove == "0-1" ||
        //        remoteBoard.Board.LastMove == "1/2-1-2"
        //    ) {
        //        remoteBoard.Board.Turn = turnCodes.NONE;
        //        remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        //    } else {
        //        remoteBoard.State.Code = boardStateCodes.GAME_IN_PROGRESS;
        //    }
        //}
    }

    return remoteBoard;
}
