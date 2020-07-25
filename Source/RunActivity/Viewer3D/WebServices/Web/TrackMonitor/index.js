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
var hr = new XMLHttpRequest;

function ApiTrackMonitor() {
	// GET to fetch data, POST to send it
	// "/API/APISAMPLE" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", "/API/TRACKMONITOR", true);
	hr.send();
	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
			var obj = JSON.parse(hr.responseText);
			if (obj != null) // Can happen using IEv11
			{
                controlMode.innerHTML = modes[obj.controlMode];
                speedMpS.innerHTML = obj.speedMpS.toFixed(1);
                projectedSpeedMpS.innerHTML = obj.projectedSpeedMpS.toFixed(1);
                allowedSpeedMpS.innerHTML = obj.allowedSpeedMpS.toFixed(1);
                currentElevationPercent.innerHTML = obj.currentElevationPercent.toFixed(1);
                direction.innerHTML = obj.direction;
                cabOrientation.innerHTML = obj.cabOrientation;
                isOnPath.innerHTML = obj.isOnPath;
            }
		}
	}
}

modes = ["AUTO_SIGNAL", "AUTO_NODE", "MANUAL", "EXPLORER", "OUT_OF_CONTROL", "INACTIVE", "TURNTABLE", "UNDEFINED"];