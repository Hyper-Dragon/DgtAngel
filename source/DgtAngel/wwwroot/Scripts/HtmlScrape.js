//document.getElementById("test").addEventListener('click', () => {
//    console.log("Popup DOM fully loaded and parsed");

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

    whiteClock = document.getElementsByClassName("clock-white")[0].innerText;
    blackClock = document.getElementsByClassName("clock-black")[0].innerText;

    retVal = whiteClock + '|' + blackClock + '|' + piecesStringOut;

    console.log('Returning: ' + retVal);

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
        //We have permission to access the activeTab, so we can call chrome.tabs.executeScript:
        await chrome.tabs.executeScript({ code: '(' + piecesFromLiveBoardDOM + ')();' },
            (results) => {
                retVal = results[0];
                //console.log('Inside: ' + retVal);
            });


        //Sleep in loop
        for (let i = 0; i < 5; i++) {
            if (retVal != '-')
                break;

            //Need to wait for the script result 
            await sleep(500);
        }
    } catch(ex) {
        return 'ERROR:'+ex;
    }

    console.log('Outside: ' + retVal);
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

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}

function writeToConsole(msg) {
    console.log('Blazor:'+msg)
}