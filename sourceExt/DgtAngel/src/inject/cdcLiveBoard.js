function GetRemoteBoardState() {
    // Setup Default Return Object + blank board
    remoteBoard = getDefaultRemoteBoard();
    var board = getBlankBoard();

    if (remoteBoard.PageUrl.includes("chess.com/live")) {
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

        // Generate FEN String
        let result = calculateFen(board);

        whiteClock = document.getElementsByClassName("clock-white")[0];
        blackClock = document.getElementsByClassName("clock-black")[0];
        turn = "NONE";

        if (whiteClock.outerHTML.includes("clock-playerTurn")) {
            turn = "WHITE";
        } else if (blackClock.outerHTML.includes("clock-playerTurn")) {
            turn = "BLACK";
        }

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

        if (
            document.getElementsByClassName("dgt-board-status-component")
                .length == 0
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
                (element) => (wcConTime += w_mul.pop() * parseFloat(element))
            );

        var b_mul = [3600000.0, 60000.0, 1000.0];
        bcConTime = 0;
        blackClock.innerText
            .split(":")
            .reverse()
            .forEach(
                (element) => (bcConTime += b_mul.pop() * parseFloat(element))
            );

        remoteBoard.Board.FenString = result;
        remoteBoard.Board.Turn = turn;
        remoteBoard.Board.IsWhiteOnBottom = Array.from(
            document.getElementById("main-clock-top").parentElement.classList
        ).includes("clock-black");
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
            remoteBoard.State.Code = "GAME_IN_PROGRESS";
        }
    } else {
        remoteBoard.State.Code = "UNKNOWN_PAGE";
        remoteBoard.State.Message =
            "If you can see this the manifest has a config error!";
        remoteBoard.Board = null;
        remoteBoard.BoardConnection = null;
    }

    return remoteBoard;
}
