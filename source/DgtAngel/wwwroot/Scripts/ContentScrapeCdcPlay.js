//function addLoadEvent(a) {
//    var b = window.onload;
//    "function" != typeof window.onload ? window.onload = a : window.onload = function () {
//        b && b(),
//            a()
//    }
//}
//
//function init() {
//    console.log("Scrape chess.com/play Loaded....");
//    console.log(GetPageState());
//}

//******************************************************************************************************************************************************************************/

function RunBoardScrape() {

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


//******************************************************************************************************************************************************************************/


//addLoadEvent(init);