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

function ApiCabControls() {
	// "/API/CABCONTROLS" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", "/API/CABCONTROLS/", true);
	hr.send();
	hr.onreadystatechange = function () {
        if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
            if (hr.responseText != "") {
                console.log(hr.responseText);
                var jso = JSON.parse(hr.responseText);
                if (jso != null) // Can happen using IEv11
                {
                    // Replaces any content (innerHTML) of the element with id=markup with the data from the jso JavaScript Object
                    markup.innerHTML = prepareData(jso);
                }
            }
		}
	}
}

// Extracts the data for each control and builds a table record for it.
// | Control | Min | Value | Max | Scale |
function prepareData(jso)
{
	let data = "";
	for(let i = 0; i < jso.length; i++) {
		let control = jso[i];
		data += "<tr>";

		// TypeName
		// Replace "_" with " "
		let typeName = control.TypeName.split("_").join(" ");
		// Capitalise each word
		let words = typeName.toLowerCase().split(" ");
		for (let i = 0; i < words.length; i++) {
			words[i] = words[i][0].toUpperCase() + words[i].substr(1);
		}
		let value = words.join(" ");
		data += "<td>" + value + "</td>";
		
		// MinValue
		let range = control.MaxValue - control.MinValue;
		data += "<td>" + control.MinValue + "</td>";

		// Value
		value = control.RangeFraction * (range) + control.MinValue;
		let decimalPlaces = 4;
		if (range > 0.01)
			decimalPlaces -= 1;
		if (range > 0.1)
			decimalPlaces -= 1;
		if (range > 2)
			decimalPlaces -= 1;
		if (range > 10)
			decimalPlaces -= 1;
		if (range == 0)
			value = "-";
		else
			value = value.toFixed(decimalPlaces);
		data += "<td>" + value + "</td>";

		// MaxValue
		data += "<td>" + control.MaxValue + "</td>";

		// Scale
		let scale = 0;
		if (value != "-")
			scale = control.RangeFraction * 100;
		data += "<td class='scale'><div class='gauge' style=' width: " + scale + "px;'>&nbsp;</div></td>";

		data += "</tr>";
	}
	return data;
}

// Sends the setting for a control.
function sendValue(value, typeName)
{
    // GET to fetch data, POST to send it
    // "/API/APISAMPLE" /API is a prefix hard-coded into the WebServer class
    hr.open("POST", "/API/CABCONTROLS", true);    // true to send asynchronously (without waiting)
    hr.setRequestHeader("Content-Type", "application/json");    // Default is ;charset=UTF-8
    hr.send
    ( JSON.stringify
        (   [   { "TypeName": typeName
             // , "ControlIndex": intData.value // If provided, used in rendering cab view
                , "Value": value
				}
            ]
        )
    );
}
