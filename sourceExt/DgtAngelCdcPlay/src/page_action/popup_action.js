//Set initial values
document.getElementById("ver").innerText=chrome.runtime.getManifest().version;

document.getElementById("show-msg-button").onclick = function (event) {
    elemSection = document.getElementById("messageSection");
    if (elemSection.hidden) {
        elemSection.hidden = false;
    } else {
        elemSection.hidden = true;
    }
};

document.getElementById("show-con-out-button").onclick = function (event) {
    elemSection2 = document.getElementById("conOutSection");
    if (elemSection2.hidden) {
        elemSection2.hidden = false;
    } else {
        elemSection2.hidden = true;
    }
};

chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        if (request.BoardScrapeMsg != undefined) {
            document.getElementById("SourceMsg").innerText = "-";
            document.getElementById("MessageTime").innerText = "-";
            document.getElementById("MessageTypeMsg").innerText = "-";
            document.getElementById("MessageMsg").innerText = "-";
            document.getElementById("RemoteBoardStateCode").innerText = "-";
            document.getElementById("RemoteBoardStateMessage").innerText = "-";
            document.getElementById("RemoteBoardFenString").innerText = "-";
            document.getElementById("RemoteBoardTurn").innerText = "-";
            document.getElementById("RemoteBoardIsWhiteOnBottom").innerText =
                "-";
            document.getElementById("RemoteBoardLastMove").innerText = "-";
            document.getElementById("RemoteBoardWhiteClock").innerText = "-";
            document.getElementById("RemoteBoardBlackClock").innerText = "-";
            document.getElementById("RemoteBoardCaptureTimeMs").innerText = "-";
            document.getElementById("RemoteBoardBoardState").innerText = "-";
            document.getElementById("RemoteBoardConMessage").innerText = "-";

            document.getElementById("SourceMsg").innerText =
                request.BoardScrapeMsg.Source;
            document.getElementById("MessageTime").innerText =
                request.BoardScrapeMsg.MsgTimeMs;
            document.getElementById("MessageTypeMsg").innerText =
                request.BoardScrapeMsg.MessageType;
            document.getElementById("MessageMsg").innerText =
                request.BoardScrapeMsg.Message;

            if (request.BoardScrapeMsg.RemoteBoard != null) {
                document.getElementById("RemoteBoardStateCode").innerText =
                    request.BoardScrapeMsg.RemoteBoard.State.Code;
                document.getElementById("RemoteBoardStateMessage").innerText =
                    request.BoardScrapeMsg.RemoteBoard.State.Message;

                if (
                    request.BoardScrapeMsg.RemoteBoard.State.Code.startsWith(
                        "GAME_"
                    )
                ) {
                    document.getElementById("RemoteBoardFenString").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.FenString;
                    document.getElementById("RemoteBoardTurn").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.Turn;
                    document.getElementById("RemoteBoardIsWhiteOnBottom").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.IsWhiteOnBottom;
                    document.getElementById("RemoteBoardLastMove").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.LastMove;
                    document.getElementById("RemoteBoardWhiteClock").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.Clocks.WhiteClock;
                    document.getElementById("RemoteBoardBlackClock").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.Clocks.BlackClock;
                    document.getElementById("RemoteBoardCaptureTimeMs").innerText =
                        request.BoardScrapeMsg.RemoteBoard.Board.Clocks.CaptureTimeMs;
                    document.getElementById("RemoteBoardBoardState").innerText =
                        request.BoardScrapeMsg.RemoteBoard.BoardConnection.BoardState;
                    document.getElementById("RemoteBoardConMessage").innerText =
                        request.BoardScrapeMsg.RemoteBoard.BoardConnection.ConMessage;
                }
            }
        } else {
            document.getElementById("FromWorker").innerText =
                request.WorkerMessage;
        }
    } catch (err) {
        document.getElementById("FromWorker").innerText = "ERROR " + err;
        console.log("ERROR " + err);
    } finally {
        return true; // Required to keep message port open
    }
});
