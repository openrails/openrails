// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

    const uri = "ws://" + document.location.hostname + ':' + document.location.port + '/switchpanel';
    const connection = new WebSocket(uri, "json");

    connection.onopen = function (evt) {
        console.log("Websocket opened: " + evt.type);
        // connected, turn off the dark overlay
        document.getElementById("overlay").style.display = "none"; 
    };

    connection.onmessage = function (evt) {
        const json = JSON.parse(evt.data);
        console.log("Websocket message received: ", json);
        if (initStillToBeDone) {
            if (json.type == "init") {
                initReceived(json.data);
                initStillToBeDone = false;
            }
        }
        if (json.type == "update") {
            updateCells(json.data);
        }
    }

    connection.onerror = function (evt) {
        console.error("Websocket error: ", evt.type);
        // this will also fire the onclose after this
    }

    connection.onclose = function (evt) {
        console.log("WebSocket is closed: ", evt.type);
        // dark overlay displayed so that it's clear the connection has gone
        document.getElementById("overlay").style.display = "block"; 
        // try to reconnect after 1 second
        setTimeout(function () {
            websocket = createSocket();
        }, 1000);
    };

    setTimeout(function () {
        // close the socket if no connection established after 1 second
        // otherwise it will wait with the reconnect after a timeout of 2 minutes
        if (websocket.readyState != 1) {
            websocket.close();
        }
    }, 1000);

    return connection;
}

function sendButtonDown(userCommand) {

    console.log("sendButtonDown: " + userCommand);
    sendMessage("buttonDown", userCommand);
}

function sendButtonUp(userCommand) {

    console.log("sendButtonUp: " + userCommand);
    sendMessage("buttonUp", userCommand);
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

let websocket = createSocket();

//
// display
//

let rows = 0;
let cols = 0;
let cells = [];
let dataPrevious = [];

function initReceived(data) {

    rows = data.length;
    cols = data[0].length;

    // init an empty data previous
    for (let x = 0; x < rows; x++) {
        dataPrevious[x] = [];
        for (let y = 0; y < cols; y++) {
            dataPrevious[x].push({ Definition: {}, Status: {} });
        }
    }

    // create the table cells to display
    let tableSwitchPanel = document.getElementById("tableSwitchPanel");

    for (let x = 0; x < rows; x++) {
        let row = tableSwitchPanel.insertRow();
        cells[x] = [];
        for (let y = 0; y < cols; y++) {
            cells[x].push(row.insertCell());
        }
    }
    document.body.appendChild(tableSwitchPanel);

    // fill the cells with contents
    updateCells(data);
}

function updateCells(data) {

    for (let x = 0; x < rows; x++) {
        for (let y = 0; y < cols; y++) {
            updateOneCell(cells[x][y], data[x][y], dataPrevious[x][y]);
        }
    }
}

function updateOneCell(cell, cellData, cellDataPrevious) {

    let definition = cellData.Definition;
    let definitionPrevious = cellDataPrevious.Definition;
    let status = cellData.Status;

    if (!definitionEqual(definition, definitionPrevious)) {
        removeFromBlinkButtons(definitionPrevious.UserCommand);
        updateCellDefinition(cell, definition);
        copyDefinition(definitionPrevious, definition);
    }
    updateCellStatus(definition, status);
}

function updateCellDefinition(cell, definition) {

    cell.innerHTML = "";
    let div = document.createElement("div");
    switch (definition.NoOffButtons) {
        case 0:
            updateCellWithZeroButtons(div, definition);
            break;
        case 1:
            updateCellWithOneButton(div, definition);
            break;
        case 2:
            updateCellWithTwoButtons(div, definition);
            break;
    }
    cell.appendChild(div);
}

function updateCellWithZeroButtons(div, definition) {

    // empty non functioning button
    let switchButton = document.createElement("button");
    switchButton.setAttribute("id", definition.UserCommand[0]);
    switchButton.className = "btn0";
    div.appendChild(switchButton);

    let label = document.createElement("label");
    if (definition.Description) {
        label.innerHTML = definition.Description;
    } else {
        label.innerHTML = "___";
    }
    div.appendChild(label);
}

function updateCellWithOneButton(div, definition) {

    // one button
    let switchButton = document.createElement("button");
    switchButton.setAttribute("id", definition.UserCommand[0]);
    setEventListener(switchButton);
    switchButton.className = "btn1";
    div.appendChild(switchButton);

    let label = document.createElement("label");
    label.innerHTML = definition.Description;

    div.appendChild(label);
}

function updateCellWithTwoButtons(div, definition) {

    // two buttons (up and down functionality)
    let switchButton1 = document.createElement("button");
    switchButton1.setAttribute("id", definition.UserCommand[0]);
    setEventListener(switchButton1);
    switchButton1.className = "btn2";
    div.appendChild(switchButton1);

    let switchButton2 = document.createElement("button");
    switchButton2.setAttribute("id", definition.UserCommand[1]);
    setEventListener(switchButton2);
    switchButton2.className = "btn2";
    div.appendChild(switchButton2);

    let label = document.createElement("label");
    label.innerHTML = definition.Description;
    div.appendChild(label);
}

function updateCellStatus(definition, status) {

    let switchButton = document.getElementById(definition.UserCommand[0]);
    if (switchButton) {
        switchButton.innerHTML = status.Status;
        switchButton.style.background = status.Color;
        if (status.Blinking) {
            addToBlinkButtons(definition.UserCommand[0], status.Color);
        } else {
            removeFromBlinkButtons(definition.UserCommand[0])
        }
    }
}

function setEventListener(switchButton) {

    ["touchstart", "mousedown"].forEach(evt =>
        switchButton.addEventListener(evt,
            function (e) {
                e.preventDefault();
                sendButtonDown(this.id);
                this.style.borderRadius = "90px";
                this.parentElement.style.background = "black";
            })
    );
    ["touchend", "mouseup"].forEach(evt =>
        switchButton.addEventListener(evt,
            function (e) {
                e.preventDefault();
                sendButtonUp(this.id);
                this.style.borderRadius = "12px";
                this.parentElement.style.background = "";
            })
    )
}

function definitionEqual(definition1, definition2) {

    let equal = true;

    if (definition1.UserCommand == undefined) {
        equal = false;
    }
    if (equal) {
        if (definition2.UserCommand == undefined) {
            equal = false;
        }
    }

    if (equal) {
        if (definition1.NoOffButtons != definition2.NoOffButtons) {
            equal = false;
        }
    }

    if (equal) {
        for (let i = 0; i < definition1.NoOffButtons; i++) {
            if (definition1.UserCommand[i] !== definition2.UserCommand[i]) {
                equal = false;
            }
        }
    }

    if (equal) {
        equal = (definition1.Description === definition2.Description);
    }

    return equal;
}

function copyDefinition(definitionTo, definitionFrom) {

    definitionTo.Description = definitionFrom.Description;
    definitionTo.NoOffButtons = definitionFrom.NoOffButtons;
    definitionTo.UserCommand = [];

    if (definitionTo.NoOffButtons > 0) {
        for (let i = 0; i < definitionTo.NoOffButtons; i++) {
            definitionTo.UserCommand[i] = definitionFrom.UserCommand[i];
        }
    }
}

//
// blinking
//

let blinkButtons = {};

function blink() {

    let userCommand;
    let color;
    for ([userCommand, color] of Object.entries(blinkButtons)) {
        let switchButton = document.getElementById(userCommand);
        if (switchButton !== null) {
            switchButton.style.background = "white";
        }
    }
    setTimeout(function () {
        for ([userCommand, color] of Object.entries(blinkButtons)) {
            let switchButton = document.getElementById(userCommand)
            if (switchButton !== null) {
                switchButton.style.background = color;
            }
        }
    }, 400);
}

function addToBlinkButtons(userCommand, color) {

    if (!blinkButtons[userCommand]) {
        blinkButtons[userCommand] = color;
    }
}

function removeFromBlinkButtons(userCommand) {

    if (blinkButtons[userCommand]) {
        delete blinkButtons[userCommand];
    }
}

setInterval(blink, 1000);

//
// sleep time (expects milliseconds)
//

function sleep(time) {

    return new Promise((resolve) => setTimeout(resolve, time));
}

//
// main
//

// give the socket some time to establish connections
sleep(1000).then(() => {
    sendMessage("init", "")
});
