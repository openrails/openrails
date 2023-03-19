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

function ApiHeadUpDisplay() {
	// GET to fetch data, POST to send it
	// "/API/APISAMPLE" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", "/API/HUD/"+pageNo, true);
	hr.send();
	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
			var obj = JSON.parse(hr.responseText);
			if (obj != null) // Can happen using IEv11
			{
				var rows = obj.commonTable.nRows;
				var cols = obj.commonTable.nCols;
				Str = "<table>";
				var codeColor = ['???','!!!','%%%','$$$'];
				var next = 0;
				var endIndex = 0;
				var newData = "";
				for (var row = 0; row < obj.commonTable.nRows; ++row) {
					Str += "<tr>";
					for (var col=0; col < obj.commonTable.nCols; ++col) { 
						if (obj.commonTable.values[next] != null) {
							endIndex = obj.commonTable.values[next].length;
							newData = obj.commonTable.values[next].slice(0, endIndex -3);
							stringColor = obj.commonTable.values[next].slice(-3);
						}
						if (obj.commonTable.values[next] == null) {
							Str += "<td></td>";
						}
						else if (codeColor.indexOf(stringColor) != -1) {
							Str += "<td ColorCode =" + stringColor + ">" + newData + "</td>";
						}
						else {
							Str += "<td>" + obj.commonTable.values[next] + "</td>";
						}
						++next;
					}
					Str += "</tr>";
				}
				Str += "</table>";
				HUDCommon.innerHTML = Str;

				// Clear HUDExtra when Common is selected
				if (obj.nTables == 1) {
					Str = "<table style='display:none'>";
					HUDExtra.innerHTML = Str;
				}

				if (obj.nTables == 2) {
					var rows = obj.extraTable.nRows,
						cols = obj.extraTable.nCols;

					next = 0;
					Str = "<table>";
					// Arrays used with .indexOf() function
					// Debug information
					var DebugColSpanTo10 = ['Build','CPU','GPU','Adapter'],
						DebugColSpanTo3 = ['Render process','Updater process','Loader process','Sound process','Total process'],
						DebugWidthTofix = ['primitives','Camera'],
					// Force information
						ForceColSpanTo15 = ['Wind Speed'],
						ForceColSpanTo4 = ['(Simple adhesion model)','Dynamic brake'],
						ForceColSpanTo3 = ['Axle out force','Loco Adhesion','Wagon Adhesion'],
					// Brake information
						BrakeColSpanTo2 = ['PlayerLoco','Brake Sys Vol'];

					for (var row = 0; row < obj.extraTable.nRows; ++row) {
						Str += "<tr>";
						var colspanmax = false,
							colspan10 = false,
							colspan3 = false,
							fixwidth = false,
							locomotivetype = false;
						
						for (var col=0; col < obj.extraTable.nCols; ++col) { 
							// Required variables for the text color management
							if (obj.extraTable.values[next] != null) {
								endIndex = obj.extraTable.values[next].length;
								newData = obj.extraTable.values[next].slice(0, endIndex -3);
								stringColor = obj.extraTable.values[next].slice(-3);
							}
							if (obj.extraTable.values[next] == null) {
								Str += "<td></td>";
							}
							// Changes the first row to title format
							else if (obj.extraTable.values[next].indexOf('INFORMATION') !== -1) {
								Str += "<th align='left' colspan='9' >" + obj.extraTable.values[next] + "</th>";
							}
							// Apply color
							else if (codeColor.indexOf(stringColor) != -1) {
								Str += "<td ColorCode =" + stringColor + ">" + newData + "</td>";
							}
							// Customized colspan
							else if (colspanmax || (locomotivetype && obj.extraTable.values[next].length > 5) ){
								locomotivetype = false;
								Str += "<td class ='td_nowrap' colspan='15' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (obj.extraTable.values[next].indexOf('Locomotive') !== -1){
								locomotivetype = true;
								Str += "<td colspan='2' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (colspan3 || BrakeColSpanTo2.indexOf(obj.extraTable.values[next]) !== -1){
								Str += "<td colspan='3' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (colspan10){
								Str += "<td class ='td_nowrap' colspan='10' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (fixwidth){
								Str += "<td class='td_nowrap' >" + obj.extraTable.values[next] + "</td>";
							}
							// Force info requires colspan into first col
							else if (obj.extraTable.values[next].includes(ForceColSpanTo15)) {
								Str += "<td colspan='15' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (ForceColSpanTo4.indexOf(obj.extraTable.values[next]) !== -1 || obj.extraTable.values[next].indexOf('===') !== -1){ // Locomotive info
								Str += "<td colspan='4' >" + obj.extraTable.values[next] + "</td>";
							}
							else if (ForceColSpanTo3.indexOf(obj.extraTable.values[next]) !== -1 ){
								Str += "<td colspan='3' >" + obj.extraTable.values[next] + "</td>";
							}
							else {
								// Apply colspan if required after first col
								if (obj.extraTable.values[next].indexOf('Memory') !== -1){
									colspanmax = true;
								}
								if (DebugColSpanTo10.indexOf(obj.extraTable.values[next]) !== -1){
									colspan10 = true;
								}
								if (DebugColSpanTo3.indexOf(obj.extraTable.values[next]) !== -1){
									colspan3 = true;
								}
								if (DebugWidthTofix.indexOf(obj.extraTable.values[next]) !== -1){
									fixwidth = true;
								}
								Str += "<td class='td_nowrap'>" + obj.extraTable.values[next].trim() + "</td>";
							}
							++next;
						}
						Str += "</tr>";
					}
					Str += "</table>";
					HUDExtra.innerHTML = Str;
				}
			}
		}
	}
}