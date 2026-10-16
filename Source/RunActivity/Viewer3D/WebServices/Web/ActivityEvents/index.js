// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.
//

'use strict';

//
// socket functions
//

const storageBeepKey = "OpenRails/activityevents/beep";
let webSocket;

function createSocket() {

    const uri = "ws://" + document.location.hostname + ':' + document.location.port + '/activityevents';
    webSocket = new WebSocket(uri, "json");

    webSocket.onopen = function (evt) {
        console.log("webSocket opened: " + evt.type);

        // dark overlay disabled
        document.body.style.backgroundColor = "rgba(0, 0, 0, 0.0)";
    };

    webSocket.onmessage = function (evt) {
        const json = JSON.parse(evt.data);
        handleMessage(json);
    }

    webSocket.onerror = function (evt) {
        console.log("webSocket error:", evt.type);
        // this will also fire the onclose
    }

    webSocket.onclose = function (evt) {
        console.log("WebSocket is closed now: " + evt.type);

        // dark overlay displayed so that it's clear the webSocket has gone
        document.body.style.backgroundColor = "rgba(0, 0, 0, 0.3)";

        let el = document.body.firstChild;
        while (el) {
            el.style.backgroundColor = "rgba(0, 0, 0, 0.0)";
            el = el.nextSibling;
        }

        // try to reconnect after 1 second with a timeout of 1 second
        sleep(1000);
        setTimeout(function () {
            createSocket();
        }, 1000);
    };
}

function handleMessage(json) {

    console.log("handleMessage: ", json);

    let sendBeepMessage = false;

    if (json.Type == "init") {
        // clear window
        while (document.body.firstChild) {
            document.body.removeChild(document.body.firstChild);
        }

        // beep checkbox
        const checkboxDiv = document.createElement('div');
        checkboxDiv.id = "checkboxDiv";
        checkboxDiv.title = json.BeepHelpTranslated;
        document.body.appendChild(checkboxDiv);

        const beepLabel = document.createElement("label");
        beepLabel.innerHTML = json.BeepTranslated;
        checkboxDiv.appendChild(beepLabel);

        const beepCheckbox = document.createElement("input");
        beepCheckbox.type = "checkbox";
        beepCheckbox.addEventListener('change', (event) => {
            handleBeepCheckbox(event.currentTarget.checked);
        })
        beepCheckbox.checked = localStorage.getItem(storageBeepKey) == "true" ? true : false;
        if (json.BeepHelpTranslated.length > 0) {
            checkboxDiv.appendChild(beepCheckbox);
        }

        sendMessage("beep", beepCheckbox.checked);
    }

    const header = document.createElement("label");
    header.innerHTML = "<BR>" + json.Header + ":<BR>";
    document.body.appendChild(header);

    const textDiv = document.createElement('div');
    textDiv.className = "div";
    const text = document.createTextNode(json.Text);
    textDiv.appendChild(text);
    document.body.appendChild(textDiv);

    window.scrollTo(0, document.body.scrollHeight);
}

function handleBeepCheckbox(isBeepCheckboxChecked) {

    localStorage.setItem(storageBeepKey, isBeepCheckboxChecked);
    sendMessage("beep", isBeepCheckboxChecked);
}

function sendMessage(type, toSend) {

    console.log("sending Beep message");
    if (webSocket.readyState === 1) {
        webSocket.send(JSON.stringify({
            type: type,
            data: toSend
        }));
    } else {
        // No webSocket, refresh browser to reconnect
        location.reload();
    }
}

function sleep(time) {

    return new Promise((resolve) => setTimeout(resolve, time));
}

//
// main
//
createSocket();
