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

let hr = new XMLHttpRequest;
let httpCodeSuccess = 200;
let xmlHttpRequestCodeDone = 4;

function ApiTime() {
	// "/API/TIME" /API is a prefix hard-coded into the WebServer class
	hr.open("GET", "/API/TIME", true);
	hr.send();
	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
            let vso = JSON.parse(hr.responseText);
            if (vso != null) // Can happen using IEv11
            {
                let secondsSinceMidnight = vso;
                let daysInteger = Math.floor(secondsSinceMidnight / (60 * 60 * 24));
                let daysFraction = secondsSinceMidnight - daysInteger * (60 * 60 * 24);
                let hoursInteger = Math.floor(daysFraction / (60 * 60));
                let hoursFraction = daysFraction - hoursInteger * (60 * 60);
                let minutesInteger = Math.floor(hoursFraction / 60);
                let minutesFraction = hoursFraction - (minutesInteger * 60);
                let secondsInteger = Math.floor(minutesFraction);
                let secondsFraction = minutesFraction - secondsInteger;
                let centisecondsInteger = Math.floor(secondsFraction * 100);
                
                time.innerHTML = secondsSinceMidnight.toFixed(2);

                hours.innerHTML = ((hoursInteger < 10) ? "0" : "") + hoursInteger;
                minutes.innerHTML = ((minutesInteger < 10) ? "0" : "") + minutesInteger;
                seconds.innerHTML = ((secondsInteger < 10) ? "0" : "") + secondsInteger;
                centiseconds.innerHTML = ((centisecondsInteger < 10) ? "0" : "") + centisecondsInteger;
            }
		}
	}
}
