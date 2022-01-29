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

function ApiTrainDpu() {
	// GET to fetch data, POST to send it
	// "/API/APISAMPLE" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", `/API/TRAINDPUDISPLAY?normalText=${normalTextMode}`, true);
	hr.send();

	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
			var obj = JSON.parse(hr.responseText);
			if (obj != null) // Can happen using IEv11
			{
				Str = "<table>";
				var endIndexFirst = 0,
					endIndexLast = [],
					endIndexSymbol = [];

				var	newDataFirst = "",
					newDataLast = [],
					newDataSymbol = [],
					smallSymbolColor = [],
					stringColorFirst = "",
					stringColorLast = [];

				// Color codes
				var codeColor = ['???','??!','?!?','?!!','!??','!!?','!!!','%%%','$$$'];
				var Fence = "\u2590";

				// Table title
				var colspanValue = obj[0].LastCol.length + 5;
				Str += "<tr> <td colspan='" + colspanValue + "' style='text-align: center'>" + 'Train DPU Info' + "</td></tr>";
				Str += "<tr> <td colspan='" + colspanValue + "' class='separator'></td></tr>";

				// Customize data
				for (const data of obj) {
					if (data.FirstCol != "" && data.LastCol != null && data.SymbolCol != null) {
						Str += "<tr>";
						firstColor = false;
						let lastColor = [];
						let symbolColor = [];
						var n = 0;
						for (const dataCol of obj[0].LastCol) {
							lastColor[n] = false;
							symbolColor[n] = false;
							n++;
						}
						keyColor = false;

						// FirstCol
						if (data.FirstCol != null) {
							endIndexFirst = data.FirstCol.length;
							newDataFirst = data.FirstCol.slice(0, endIndexFirst - 3);
							stringColorFirst = data.FirstCol.slice(-3);
						}

						// LastCol
						if (data.LastCol != null) {
							n = 0;
							for (const dataCol of data.LastCol) {
								endIndexLast[n] = dataCol.length;
								newDataLast[n] = dataCol.slice(0, endIndexLast[n] - 3);
								stringColorLast[n] = dataCol.slice(-3);
								n++;
							}
						}

						// smallSymbol
						if (data.SymbolCol != null) {
							n = 0;
							for (const dataSymbol of data.SymbolCol) {
								endIndexSymbol[n] = dataSymbol.length;
								newDataSymbol[n] = dataSymbol.slice(0, endIndexSymbol[n] - 3);
								smallSymbolColor[n] = dataSymbol.slice(-3);
								n++;
							}
						}

						// detects color
						if (codeColor.indexOf(stringColorFirst) != -1) { firstColor = true; }
						//detect color inside array
						if (data.LastCol != null) {
							n = 0;
							for (const dataCol of data.LastCol) {
								if (codeColor.indexOf(stringColorLast[n]) != -1) { lastColor[n] = true; }
								n++;
							}
						}
						if (data.SymbolCol != null) {
							n = 0;
							for (const dataSymbol of data.SymbolCol) {
								if (codeColor.indexOf(smallSymbolColor[n]) != -1) { symbolColor[n] = true; }
								n++;
							}
						}

						if (data.FirstCol == null) {
							Str += "<td></td>";
						}
						else if (data.FirstCol == "Sprtr") {
							Str += "<td colspan='" + colspanValue + "' class='separator'></td>";
						}
						else {
							// first col = FirstCol data
							if (firstColor == true) {
								Str += "<td ColorCode=" + stringColorFirst + ">" + newDataFirst + "</td>";
							}
							else {
								Str += "<td>" + data.FirstCol + "</td>";
							}

							// second col = LastCol && SymbolCol data
							n = 0;
							if (data.LastCol != null) {
								for (const dataCol of data.LastCol) {
									if (symbolColor[n] == true) { // with color
										Str += "<td ColorCode=" + smallSymbolColor[n] + " width='16' style='text-align: left'>" + newDataSymbol[n] + "</td>";
									}
									else { // not color
										Str += "<td width='16' style='text-align: center'>" + data.SymbolCol[n] + "</td>";
									}
									if (lastColor[n] == true) { // with color
										if (newDataLast[n].indexOf("|") != -1) {
											newDataLast[n] = newDataLast[n].replace("|", "");// replace fence
										}
										Str += "<td ColorCode=" + stringColorLast[n] + ">" + newDataLast[n] + "</td>";
									}
									else { // not color
										if (data.FirstCol == obj[0].FirstCol) {
											Str += "<td style='text-align: center'>" + data.LastCol[n] + "</td>";
										}
										else {
											if (data.LastCol[n].indexOf("|") != -1) {
												data.LastCol[n] = data.LastCol[n].replace("|", "");// replace fence
											}
											Str += "<td style='text-align: left'>" + data.LastCol[n] + "</td>";
										}
									}
									n++
								}
							}

							// separator
							if (data.FirstCol == obj[0].FirstCol) {
								Str += "<tr> <td colspan='" + colspanValue + "' class='separator'></td></tr>";
							}
						}
						Str += "</tr>";
					}
				}
				// separator at bottom
				Str += "<tr> <td colspan='" + colspanValue + "' class='separator'></td></tr>";
				Str += "</table>";
				// space at bottom
				Str += "<tr> <td colspan='" + colspanValue + "' onclick='changeNormalTextMode()' style='text-align: center'><img src='/or_logo.png' height='16' width='16'></img></td> </tr>";
				Str += "</table>";
				TrainDpu.innerHTML = Str;
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