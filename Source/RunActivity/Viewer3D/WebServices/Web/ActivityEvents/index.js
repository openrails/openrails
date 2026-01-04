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

function createSocket() {

    const uri = "ws://" + document.location.hostname + ':' + document.location.port + '/activityevents';
    const connection = new WebSocket(uri, "json");

    connection.onopen = function (evt) {
        console.log("websocket opened: " + evt.type);

        // dark overlay disabled
        document.body.style.backgroundColor = "rgba(0, 0, 0, 0.0)";
    };

    connection.onmessage = function (evt) {
        const json = JSON.parse(evt.data);
        handleMessage(json);
    }

    connection.onerror = function (evt) {
        console.error("websocket error:", evt.type);
        // this will also fire the onclose after this
    }

    connection.onclose = function (evt) {
        console.log("WebSocket is closed now: " + evt.type);

        // dark overlay displayed so that it's clear the connection has gone
        document.body.style.backgroundColor = "rgba(0, 0, 0, 0.3)";

        let el = document.body.firstChild;
        while (el) {
            el.style.backgroundColor = "rgba(0, 0, 0, 0.0)";
            el = el.nextSibling;
        }

        // try to reconnect after 1 second with a timeout of 1 second
        sleep(1000);
        setTimeout(function () {
            websocket = createSocket();
        }, 1000);
    };

    setTimeout(function () {
        // close the socket if no connection established after 3 seconds
        // otherwise it will wait with the reconnect after a timeout of 2 minutes
        if (websocket.readyState != 1) {
            websocket.close();
        }
    }, 3000);

    return connection;
}

function handleMessage(json) {

    console.log("handleMessage: ", json);

    if (json.Type == "init") {
        // clear window
        while (document.body.firstChild) {
            document.body.removeChild(document.body.firstChild);
        }
    }

    if (json.Type == "init") {
        const checkboxDiv = document.createElement('div');
        checkboxDiv.id = "checkboxDiv";
        checkboxDiv.title = json.BeepHelpTranslated;
        document.body.appendChild(checkboxDiv);

        const beepLabel = document.createElement("label");
        beepLabel.innerHTML = json.BeepTranslated;
        checkboxDiv.appendChild(beepLabel);

        const beepCheckbox = document.createElement("input");
        beepCheckbox.type = "checkbox";
        beepCheckbox.checked = localStorage.getItem(storageBeepKey) == "true" ? true : false;
        sendMessage("beep", beepCheckbox.checked);
        beepCheckbox.addEventListener('change', (event) => {
            handleBeepCheckbox(event.currentTarget.checked);
        })
        checkboxDiv.appendChild(beepCheckbox);
    }

    const header = document.createElement("label");
    header.innerHTML = "<BR>" + json.Header + ":<BR>";
    document.body.appendChild(header);

    const textArea = document.createElement('textarea');
    textArea.readOnly = true;
    const text = document.createTextNode(json.Text);
    textArea.appendChild(text);
    document.body.appendChild(textArea);

    window.scrollTo(0, document.body.scrollHeight);
}

function handleBeepCheckbox(isBeepCeckboxChecked) {

    localStorage.setItem(storageBeepKey, isBeepCeckboxChecked);
    sendMessage("beep", isBeepCeckboxChecked);    
}

function sendMessage(type, toSend) {

    console.log('sending message, readyState = ' + websocket.readyState);
    if (websocket.readyState === 1) {
        websocket.send(JSON.stringify({
            type: type,
            data: toSend
        }));
    } else {
        // No connection, refresh browser to reconnect
        location.reload();
    }
}

function sleep(time) {

    return new Promise((resolve) => setTimeout(resolve, time));
}

//
// main
//
let websocket = createSocket();
