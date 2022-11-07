/**
 * DTG Angel Page Scrape
 *  - Chess.com Live Board
 *
 */
function IsUrlValid(urlToTest) {
    if (urlToTest.includes("chess.com/live")) {
        return true;
    } else {
        return false;
    }
}

function GetRemoteBoardState() {
    // Setup Default Return Object + blank board
    remoteBoard = getDefaultRemoteBoard();
    var board = getBlankBoard();

    // Get all the pieces
    myPiecesArray = Array.from(document.getElementsByClassName("piece"));
    pieceString = myPiecesArray
        .map(
            (o) =>
                o.style.backgroundImage.split("/").pop().substring(0, 2) +
                o.className.replace("piece square-", "").replaceAll("0", "")
        )
        .join(",");

    // Add them to the board
    piecesStringArray = pieceString.split(",");
    while (piecesStringArray.length) {
        var piece = piecesStringArray.pop();
        board[8 - parseInt(piece[3])][parseInt(piece[2]) - 1] =
            piece[0] + "" + piece[1];
    }

    //Now..the move list
    moveList = Array.from(
        document.getElementsByClassName("move-text-component")
    );
    lastMove = "";

    if (moveList.length > 0) {
        lastMove = Array.from(
            document.getElementsByClassName("move-text-component")
        )
            .pop()
            .innerText.trim();
    }

    // Now the clocks....
    whiteClock = document.getElementsByClassName("clock-white")[0];
    blackClock = document.getElementsByClassName("clock-black")[0];
    turn = "NONE";

    // Use the clocks to detect the turn
    if (whiteClock.outerHTML.includes("clock-playerTurn")) {
        turn = turnCodes.WHITE;
    } else if (blackClock.outerHTML.includes("clock-playerTurn")) {
        turn = turnCodes.BLACK;
    }

    // Finally the DTG board status if we can get it
    if (
        document.getElementsByClassName("dgt-board-status-component").length ==
        0
    ) {
        boardState = dgtStateCodes.UNKNOWN;
        boardMessage = "";
    } else {
        boardState = dgtStateCodes.ACTIVE;
        boardMessage = document
            .getElementsByClassName("dgt-board-status-component")[0]
            .innerText.trim()
            .replaceAll("\n", "");
    }

    remoteBoard.Board.FenString = calculateFen(board);
    remoteBoard.Board.Turn = turn;
    remoteBoard.Board.IsWhiteOnBottom = Array.from(
        document.getElementById("main-clock-top").parentElement.classList
    ).includes("clock-black");
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
    if (remoteBoard.Board.Turn == turnCodes.NONE) {
        if (remoteBoard.Board.LastMove == "") {
            remoteBoard.State.Code = boardStateCodes.GAME_PENDING;
        } else {
            remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        }
    } else {
        if (
            remoteBoard.Board.LastMove == "1-0" ||
            remoteBoard.Board.LastMove == "0-1" ||
            remoteBoard.Board.LastMove == "1/2-1-2"
        ) {
            remoteBoard.Board.Turn = turnCodes.NONE;
            remoteBoard.State.Code = boardStateCodes.GAME_COMPLETED;
        } else {
            remoteBoard.State.Code = boardStateCodes.GAME_IN_PROGRESS;
        }
    }

    return remoteBoard;
}
