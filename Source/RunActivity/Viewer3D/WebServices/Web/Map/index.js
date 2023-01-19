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

const hr = new XMLHttpRequest;
const httpCodeSuccess = 200;
const xmlHttpRequestCodeDone = 4;

var locomotiveMarker;
var map;
var latLonPrev = [0, 0];

function MapInit(latLon) {

    map = L.map('map').setView(latLon, 13);
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

            let latLonObj = JSON.parse(hr.responseText);

            if (latLonObj != null) {

                let latLon = [latLonObj.Lat, latLonObj.Lon];

                if (typeof locomotiveMarker == 'undefined') {
                    // init
                    MapInit(latLon);
                    locomotiveMarker = L.marker(
                        latLon,
                        { icon: myIcon }
                    ).addTo(map);
                } else {
                    if ((latLon[0] != latLonPrev[0]) || (latLon[1] != latLonPrev[1])) {
                        // changed
                        map.panTo(latLon);
                        locomotiveMarker.setLatLng(latLon).update();
                    }
                }
                latLonPrev[0] = latLon[0];
                latLonPrev[1] = latLon[1];
            }
        }
    }
}

var myIcon = L.icon({
    iconUrl: 'locomotive.png',
    iconSize: [29, 24],
    iconAnchor: [9, 21],
})
