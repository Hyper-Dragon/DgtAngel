
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

    //console.log('Returning: ' + retVal);

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

async function getPiecesHtml() {

    retVal = '-';

    try {


        //chrome.tabs.query({ currentWindow: true, active: true }, function (tabs) {
        //
        //    if (lastErr || tabs[0].url != "https://www.chess.com/live") {
        //        return "UNDEFINED";
        //    }
        //
        //    console.log(tabs[0].url);
        //});


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

