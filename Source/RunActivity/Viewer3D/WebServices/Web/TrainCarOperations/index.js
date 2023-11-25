// COPYRIGHT 2023 by the Open Rails project.
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

let initStillToBeDone = true;

function createSocket() {

    const uri = "ws://" + document.location.hostname + ':' + document.location.port + '/traincaroperations';
    const connection = new WebSocket(uri, "json");

    connection.onopen = function (evt) {
        console.log("websocket opened: " + evt.type);
    };

    connection.onmessage = function (evt) {
        const json = JSON.parse(evt.data);
        handleMessage(json);
    }

    connection.onerror = function (evt) {
        console.error("websocket error: ", evt.type);
        // this will also fire the onclose after this
    }

    connection.onclose = function (evt) {
        console.log("WebSocket is closed now: " + evt.type);
        // dark overlay displayed so that it's clear the connection has gone
        document.getElementById("overlay").style.display = "block";

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

function sendMessage(toSend) {

    console.log('sending message, readyState = ' + websocket.readyState);

    if (websocket.readyState === 1) {
        console.log('sending message, toSend = ' + toSend);
        websocket.send(toSend);
    } else {
        // No connection, refresh browser to reconnect
        location.reload();
    }
}

function handleMessage(json) {

    console.log("handleMessage: ", json);

    if (json.Type == "init") {
        var html = document.body;
        while (html.firstChild) {
            html.removeChild(html.firstChild);
        }
        var elemDiv = document.createElement('div');
        elemDiv.id = "overlay";
        document.body.appendChild(elemDiv);

        const tbl = document.createElement("table");
        const th = document.createElement("th");
        const thText = document.createTextNode("Train Car Operations");
        th.appendChild(thText);
        th.colSpan = json.Columns + 1;
        tbl.appendChild(th);

        const tblBody = document.createElement("tbody");
        for (let i = 0; i < json.Rows; i++) {
            const row = document.createElement("tr");
            for (let j = 0; j < json.Columns; j++) {
                const cell = document.createElement("td");
                const button = document.createElement("button");
                button.className = "button32";
                button.setAttribute("id", "button:" + i + ":" + j);
                setEventListener(button);
                cell.appendChild(button);
                row.appendChild(cell);
            }
            const cell = document.createElement("td");
            const label = document.createElement("label");
            label.setAttribute("id", "label:" + i);
            label.innerHTML = json.CarId[i];
            label.style.margin = "10px";
            cell.appendChild(label);
            row.appendChild(cell);
            tblBody.appendChild(row);
        }
        tbl.appendChild(tblBody);
        document.body.appendChild(tbl);
    }

    for (let i = 0; i < json.Operations.length; i++) {
        let row = json.Operations[i].Row;
        let column = json.Operations[i].Column;
        let id = "button:" + row + ":" + column;
        let button = document.getElementById(id);
        button.innerHTML = "<img src='" + json.Operations[i].Filename + "' />";
        button.disabled = !json.Operations[i].Enabled;
        button.style.cursor = json.Operations[i].Enabled ? "pointer" : "";
    }

    for (let i = 0; i < json.CarIdColor.length; i++) {
        let id = "label:" + i;
        let label = document.getElementById(id);
        label.style.color = json.CarIdColor[i];
    }

    // turn off the dark overlay if it's still on
    document.getElementById("overlay").style.display = "none";
}

function setEventListener(button) {

    button.addEventListener("click",
        function (e) {
            sendMessage(this.id);
        });
}

function sleep(time) {

    return new Promise((resolve) => setTimeout(resolve, time));
}

//
// main
//
let websocket = createSocket();
