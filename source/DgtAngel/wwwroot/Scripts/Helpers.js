﻿//document.getElementById("test").addEventListener('click', () => {
//    console.log("Popup DOM fully loaded and parsed");


function playAudioFromBkg(audioId) {
    document.getElementById(audioId).play();
}

function sleep(ms) {
    return new Promise((resolve) => {
        setTimeout(resolve, ms);
    });
}

function writeToConsole(msg) {
    console.log('Blazor:' + msg)
}