
var element = document. getElementById("show-msg-button");
var elemSection = document. getElementById("messageSection");

var element2 = document. getElementById("show-con-out-button");
var elemSection2 = document. getElementById("conOutSection");

element.onclick=function(event) {
    if(elemSection.hidden){
        elemSection.hidden=false;
    }else{
        elemSection.hidden=true;
    }
}

element2.onclick=function(event) {
    if(elemSection2.hidden){
        elemSection2.hidden=false;
    }else{
        elemSection2.hidden=true;
    }
}


chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
    try {
        document.getElementById("SourceMsg").innerText= request.BoardScrapeMsg.Source;
        document.getElementById("MessageTypeMsg").innerText= request.BoardScrapeMsg.MessageType;
        document.getElementById("MessageMsg").innerText= request.BoardScrapeMsg.Message;

        document.getElementById("RemoteBoardFenString").innerText=request.BoardScrapeMsg.RemoteBoard.Board.FenString;            
        document.getElementById("RemoteBoardTurn").innerText=request.BoardScrapeMsg.RemoteBoard.Board.Turn;                 
        document.getElementById("RemoteBoardIsWhiteOnBottom").innerText=request.BoardScrapeMsg.RemoteBoard.Board.IsWhiteOnBottom;      
        document.getElementById("RemoteBoardLastMove").innerText=request.BoardScrapeMsg.RemoteBoard.Board.LastMove;             
        document.getElementById("RemoteBoardWhiteClock").innerText=request.BoardScrapeMsg.RemoteBoard.Board.Clocks.WhiteClock;    
        document.getElementById("RemoteBoardBlackClock").innerText=request.BoardScrapeMsg.RemoteBoard.Board.Clocks.BlackClock;    
        document.getElementById("RemoteBoardCaptureTimeMs").innerText=request.BoardScrapeMsg.RemoteBoard.Board.Clocks.CaptureTimeMs; 
        document.getElementById("RemoteBoardBoardState").innerText=request.BoardScrapeMsg.RemoteBoard.BoardConnection.BoardState;
        document.getElementById("RemoteBoardConMessage").innerText=request.BoardScrapeMsg.RemoteBoard.BoardConnection.ConMessage; 
    
    } catch {
        console.log("ERROR");
    } finally {
        return true; // Required to keep message port open
    }
});