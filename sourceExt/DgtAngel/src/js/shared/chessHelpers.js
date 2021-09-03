/**
 * Generic Chess Functions
 *  - Fen Calculation
 *  - Clock Conversion
 */
function getBlankBoard() {
    return [
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
        ["em", "em", "em", "em", "em", "em", "em", "em"],
    ];
}

function calculateFen(board) {
    result = "";
    for (let y = 0; y < board.length; y++) {
        let empty = 0;
        for (let x = 0; x < board[y].length; x++) {
            let c = board[y][x][0]; 
            if (c == "w" || c == "b") {
                if (empty > 0) {
                    result += empty.toString();
                    empty = 0;
                }
                if (c == "w") {
                    result += board[y][x][1].toUpperCase(); 
                } else {
                    result += board[y][x][1].toLowerCase(); 
                }
            } else {
                empty += 1;
            }
        }
        if (empty > 0) {
            result += empty.toString();
        }
        if (y < board.length - 1) {
            result += "/";
        }
    }

    return result;
}

function convertClockStringToMs(clockTxt) {
    //For the time conversion
    mulMs = [3600000, 60000, 1000];
    conTime = 0;
    clockTxt
        .split(":")
        .reverse()
        .forEach(
            (element) => (conTime += mulMs.pop() * parseFloat(element.trim()))
        );

    return conTime;
}