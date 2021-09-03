/**
 * DTG Angel Page Scrape
 *  - Chess.com Play Board
 * 
 */
function GetRemoteBoardState() {
    remoteBoard = getDefaultRemoteBoard();

    if (
        remoteBoard.PageUrl.includes("chess.com/game/live/") ||
        remoteBoard.PageUrl.includes("chess.com/play")
    ) {
        if (document.visibilityState == "hidden") {
            remoteBoard.State.Code = "LOST_VISABILITY";
            remoteBoard.State.Message =
                "You need to keep the board at least partially visible.";
            remoteBoard.Board = null;
            remoteBoard.BoardConnection = null;
        } else if (
            Array.from(document.getElementsByClassName("move-list-component"))
                .length == 0
        ) {
            remoteBoard.State.Code = "MOVE_LIST_MISSING";
            remoteBoard.State.Message =
                "Are you in focus mode? Switched the side tabs?";
            remoteBoard.Board = null;
            remoteBoard.BoardConnection = null;
        } else {
            // Get all the pieces
            piecesStringOut = Array.from(
                document.getElementsByClassName("piece")
            )
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

            // Setup a Blank Board
            var board = getBlankBoard();

            // Add them to the board
            piecesStringArray = piecesStringOut.split(",");
            while (piecesStringArray.length) {
                var piece = piecesStringArray.pop();
                board[8 - parseInt(piece[3])][parseInt(piece[2]) - 1] =
                    piece[0] + "" + piece[1];
            }

            // Now the last move
            lastMove = "";
            gameResult = document.getElementsByClassName("game-result");

            //If we have a result grab the text
            if (gameResult.length > 0) {
                lastMove = gameResult[0].innerText.trim();
            } else {
                //Get all the moves...
                moveList = Array.from(document.getElementsByClassName("move"));

                // ...and take the last one
                if (moveList.length > 0) {
                    movePop = moveList.pop();
                    nodePop = Array.from(
                        movePop.getElementsByClassName("node")
                    ).pop();

                    // Check for figurine notation
                    iconFont =
                        nodePop.getElementsByClassName("icon-font-chess");

                    if (iconFont.length > 0) {
                        lastMove =
                            iconFont[0].attributes["data-figurine"].value +
                            nodePop.innerText.trim();
                    } else {
                        lastMove = nodePop.innerText.trim();
                    }
                }
            }

            // Now the clocks....
            whiteClock = document.getElementsByClassName("clock-white")[0];
            blackClock = document.getElementsByClassName("clock-black")[0];

            // Use the clocks to detect the turn
            if (whiteClock.classList.contains("clock-player-turn")) {
                turn = "WHITE";
            } else if (blackClock.classList.contains("clock-player-turn")) {
                turn = "BLACK";
            } else {
                turn = "NONE";
            }

            // Finally the DTG board status if we can get it
            if (
                document.getElementsByClassName("dgt-board-status-component")
                    .length == 0
            ) {
                boardState = "UNKNOWN";
                boardMessage = "Not found on page";
            } else {
                boardState = "ACTIVE";
                boardMessage = document
                    .getElementsByClassName("dgt-board-status-component")[0]
                    .innerText.trim()
                    .replaceAll("\n", "");
            }

            remoteBoard.Board.FenString = calculateFen(board);
            remoteBoard.Board.Turn = turn;
            remoteBoard.Board.IsWhiteOnBottom =
                whiteClock.classList.contains("clock-bottom");
            remoteBoard.Board.LastMove = lastMove;
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
            "If you can see this something has changed OR the manifest has a config error!";
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    }

    return remoteBoard;
}
