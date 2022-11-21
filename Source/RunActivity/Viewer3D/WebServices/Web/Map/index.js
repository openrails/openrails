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

var hr = new XMLHttpRequest;
var httpCodeSuccess = 200;
var xmlHttpRequestCodeDone = 4;

var locomotivMarker;
var map;
var latLongPrev = [0, 0];

function MapInit(latLong) {

    map = L.map('map').setView(latLong, 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    L.tileLayer('https://{s}.tiles.openrailwaymap.org/standard/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: ' | Map style: &copy; <a href="https://www.OpenRailwayMap.org">OpenRailwayMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    }).addTo(map);
}

function ApiMap() {

    hr.open("GET", "/API/MAP/", true);
    hr.send();
    hr.onreadystatechange = function () {
        if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
            var responseText = JSON.parse(hr.responseText);
            if (responseText.length > 0) {
                latLong = responseText.split(" ");
                if (typeof locomotivMarker !== 'undefined') {
                    if ((latLong[0] != latLongPrev[0]) && (latLong[1] != latLongPrev[1])) {
                        map.panTo(latLong);
                    }
                } else {
                    MapInit(latLong);
                }

                if ((latLong[0] != latLongPrev[0]) || (latLong[1] != latLongPrev[1])) {
                    if (typeof locomotivMarker !== 'undefined') {
                        locomotivMarker.removeFrom(map);
                    }
                    locomotivMarker = L.marker(
                        latLong,
                        { icon: myIcon }
                    ).addTo(map);
                    latLongPrev[0] = latLong[0];
                    latLongPrev[1] = latLong[1];
                }
            }
        }
    }
}

var myIcon = L.icon({
    iconUrl: 'locomotiv.png',
    iconSize: [29, 24],
    iconAnchor: [9, 21],
})
