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
// Based on original work by Dan Reynolds 2017-12-21

// Using XMLHttpRequest rather than fetch() as:
// 1. it is more widely supported (e.g. Internet Explorer and various tablets)
// 2. It doesn't hide some returning error codes
// 3. We don't need the ability to chain promises that fetch() offers.

var hr = new XMLHttpRequest;
var httpCodeSuccess = 200;
var xmlHttpRequestCodeDone = 4;
var normalTextMode = true;

function ApiTrainDriving() {
	// GET to fetch data, POST to send it
	// "/API/APISAMPLE" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", `/API/TRAINDRIVINGDISPLAY?normalText=${normalTextMode}`, true);
	hr.send();

	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
			var obj = JSON.parse(hr.responseText);
			if (obj != null) // Can happen using IEv11
			{
				Str = "<table>";
				var endIndexFirst = 0,
					endIndexLast = 0,
					endIndexKey = 0;

				var keyPressedColor = "",
					newDataFirst = "",
					newDataLast = "",
					smallSymbolColor = "",
					stringColorFirst = "",
					stringColorLast = "";

				// Color codes
				var codeColor = ['???','??!','?!?','?!!','!??','!!?','!!!','%%%','$$$'];

				// Table title
				Str += "<tr> <td colspan='5' style='text-align: center'>" + 'Train Driving Info' + "</td></tr>";
				Str += "<tr> <td colspan='5' class='separator'></td></tr>";

				// Customize data
				for (const data of obj) {
					Str += "<tr>";
					firstColor = false;
					lastColor = false;
					keyColor = false;
					symbolColor = false;

					// FirstCol
					if (data.FirstCol != null) {
						endIndexFirst = data.FirstCol.length;
						newDataFirst = data.FirstCol.slice(0, endIndexFirst -3);
						stringColorFirst = data.FirstCol.slice(-3);
					}

					// LastCol
					if (data.LastCol != null) {
						endIndexLast = data.LastCol.length;
						newDataLast = data.LastCol.slice(0, endIndexLast -3);
						stringColorLast = data.LastCol.slice(-3);
					}

					// keyPressed
					if (data.KeyPressed != null) {
						endIndexKey = data.KeyPressed.length;
						newDataKey = data.KeyPressed.slice(0, endIndexKey -3);
						keyPressedColor = data.KeyPressed.slice(-3);
					}

					// smallSymbol
					if (data.SymbolCol != null) {
						endIndexSymbol = data.SymbolCol.length;
						newDataSymbol = data.SymbolCol.slice(0, endIndexSymbol -3);
						smallSymbolColor = data.SymbolCol.slice(-3);
					}

					// detects color
					if (codeColor.indexOf(stringColorFirst) != -1) { firstColor = true; }
					if (codeColor.indexOf(stringColorLast) != -1) { lastColor = true; }
					if (codeColor.indexOf(keyPressedColor) != -1) { keyColor = true; }
					if (codeColor.indexOf(smallSymbolColor) != -1) { symbolColor = true; }

					if (data.FirstCol == null) {
						Str += "<td></td>";
					}
					else if (data.FirstCol == "Sprtr"){
						Str += "<td colspan='5' class='separator'></td>";
					}
					else{
						// first col  = key symbol
						if (keyColor == true){
							Str += "<td ColorCode=" + keyPressedColor + ">" + newDataKey + "</td>";
						}
						else{
							Str += "<td width='16'>" + data.KeyPressed + "</td>";
						}

						// second col = FirstCol data
						if(firstColor == true){
							Str += "<td ColorCode=" + stringColorFirst + ">" + newDataFirst + "</td>";
						}
						else{
							Str += "<td>" + data.FirstCol + "</td>";
						}

						// third col  = key symbol
						if (keyColor == true){
							Str += "<td ColorCode=" + keyPressedColor + ">" + newDataKey + "</td>";
						}
						else if (symbolColor == true){
							Str += "<td ColorCode=" + smallSymbolColor + ">" + newDataSymbol + "</td>";
						}
						else{
							Str += "<td width='16'>" + data.KeyPressed + "</td>";
						}

						// fourth col = LastCol data
						if(lastColor == true){
							Str += "<td ColorCode=" + stringColorLast + ">" + newDataLast + "</td>";
						}
						else{
							Str += "<td>" + data.LastCol + "</td>";
						}
					}
					Str += "</tr>";
				}
				Str += "</table>";
				// space at bottom
				Str += "<tr> <td colspan='5' onclick='changeNormalTextMode()' style='text-align: center'><img src='/or_logo.png' height='16' width='16'></img></td> </tr>";
                Str += "</table>";
				TrainDriving.innerHTML = Str;
			}
		}
	}
}

function changePageColor() {
	var buttonClicked = document.getElementById("buttonDN");
	var bodyColor = document.getElementById("body");

	if (buttonClicked.innerHTML == "Day"){
		buttonClicked.innerHTML = "Night";
		bodyColor.style.background = "black";
		bodyColor.style.color =	"white";
	}
	else if (buttonClicked.innerHTML == "Night"){
		buttonClicked.innerHTML = "Day"
		bodyColor.style.background = "white";
		bodyColor.style.color =	"black";
	}
};

function changeNormalTextMode() {
	normalTextMode = !normalTextMode;
};