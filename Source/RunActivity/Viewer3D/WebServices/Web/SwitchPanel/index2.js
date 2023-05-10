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
        console.log("websocket opened");
    };

    connection.onmessage = function (evt) {
        const json = JSON.parse(evt.data);
        console.log("websocket message received: ", json);
        if (initStillToBeDone) {
            if (json.type == "init") {
                initReceived(json.data);
                initStillToBeDone = false;
            }
        }
        if (json.type == "buttonClick") {
            buttonClickReceived(json.data);
        }
    }

    connection.onerror = function (evt) {
        console.error("websocket error", evt);
    }

    connection.onclose = function (event) {
        console.log("WebSocket is closed now");
    };

    return connection;
}

function sendButtonClick(userCommand) {

    console.log("sendButtonClick: " + userCommand);
    sendMessage("buttonClick", userCommand);
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

function buttonClickReceived(data) {

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
    if (definition.Button == 1) { // click
        switchButton.addEventListener("click",
            function () {
                sendButtonClick(definition.UserCommand[0]);
            });
    }
    if (definition.Button == 2) { // push
        ["touchstart", "mousedown"].forEach(evt =>
            switchButton.addEventListener(evt,
                function () {
                    sendButtonDown(definition.UserCommand[0]);
                })
        );
        ["touchend", "mouseup"].forEach(evt =>
            switchButton.addEventListener(evt,
                function () {
                    sendButtonUp(definition.UserCommand[0]);
                })
        )
    }
    switchButton.className = "btn1";
    div.appendChild(switchButton);

    let label = document.createElement("label");
    label.innerHTML = definition.Description;

    div.appendChild(label);
}

function updateCellWithTwoButtons(div, definition) {

    let switchButton1 = document.createElement("button");
    switchButton1.setAttribute("id", definition.UserCommand[0]);
    if (definition.Button == 1) { // click
        switchButton1.addEventListener("click",
            function () {
                sendButtonClick(definition.UserCommand[0]);
            });
    }
    if (definition.Button == 2) { // push
        ["touchstart", "mousedown"].forEach(evt =>
            switchButton1.addEventListener(evt,
                function () {
                    sendButtonDown(definition.UserCommand[0]);
                })
        );
        ["touchend", "mouseup"].forEach(evt =>
            switchButton1.addEventListener(evt,
                function () {
                    sendButtonUp(definition.UserCommand[0]);
                })
        )
    }
    switchButton1.className = "btn2";
    div.appendChild(switchButton1);

    let switchButton2 = document.createElement("button");
    switchButton2.setAttribute("id", definition.UserCommand[1]);
    if (definition.Button == 1) { // click
        switchButton2.addEventListener("click",
            function () {
                sendButtonClick(definition.UserCommand[1]);
            });
    }
    if (definition.Button == 2) { // push
        ["touchstart", "mousedown"].forEach(evt =>
            switchButton2.addEventListener(evt,
                function () {
                    sendButtonDown(definition.UserCommand[1]);
                })
        );
        ["touchend", "mouseup"].forEach(evt =>
            switchButton2.addEventListener(evt,
                function () {
                    sendButtonUp(definition.UserCommand[1]);
                })
        )
    }
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
        equal =
            (definition1.Button === definition2.Button) &&
            (definition1.Description === definition2.Description) &&
            (definition1.NoOffButtons === definition2.NoOffButtons);
    }

    return equal;
}

function copyDefinition(definitionTo, definitionFrom) {

    definitionTo.Button = definitionFrom.Button;
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
    }, 50);
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

setInterval(blink, 500);

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
