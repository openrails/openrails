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

const hr1 = new XMLHttpRequest;
const hr2 = new XMLHttpRequest;
const hr3 = new XMLHttpRequest;
const httpCodeSuccess = 200;
const xmlHttpRequestCodeDone = 4;

let map;
let initToBeDone = true;
let locomotiveMarker;
let arrowMarker;
let latLonPrev = [0, 0];
let directionDegPrev;
let locomotiveIcon;
let arrowIcon;

function ApiMapInit() {

    let orm;
    let ormMaxSpeed;
    let ormGauge;
    let ormElectrification;

    let trackLayerGroup;
    let namedLayerGroup;
    let restLayerGroup;

    // // retrieve map information for the layers

    let apiMapInitInfo;

    hr2.open("GET", "/API/MAP/INIT/", false);
    hr2.onreadystatechange = function () {
        if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
            apiMapInitInfo = JSON.parse(hr2.responseText);
        }
    }
    hr2.send();

    // init map and various layers

    map = L.map('map');

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org">OpenStreetMap</a> contributors'
    }).addTo(map);

    orm = L.tileLayer('https://{s}.tiles.openrailwaymap.org/standard/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors | Map style: &copy; <a href="https://www.OpenRailwayMap.org">OpenRailwayMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    });

    ormMaxSpeed = L.tileLayer('https://{s}.tiles.openrailwaymap.org/maxspeed/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors | Map style: &copy; <a href="https://www.OpenRailwayMap.org">OpenRailwayMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    });

    ormGauge = L.tileLayer('https://{s}.tiles.openrailwaymap.org/gauge/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors | Map style: &copy; <a href="https://www.OpenRailwayMap.org">OpenRailwayMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    });

    ormElectrification = L.tileLayer('https://{s}.tiles.openrailwaymap.org/electrification/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: 'Map data: &copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors | Map style: &copy; <a href="https://www.OpenRailwayMap.org">OpenRailwayMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
    });

    const baseLayers = {
        'standard': orm,
        'maximum speed': ormMaxSpeed,
        'gauge': ormGauge,
        'electrification': ormElectrification
    };

    layerControl = L.control.layers(baseLayers, null).addTo(map);

    if ((apiMapInitInfo.baseLayer == null) || (apiMapInitInfo.baseLayer == "standard")) {
        orm.addTo(map);
    }
    if (apiMapInitInfo.baseLayer == "maximum speed") {
        ormMaxSpeed.addTo(map);
    }
    if (apiMapInitInfo.baseLayer == "gauge") {
        ormGauge.addTo(map);
    }
    if (apiMapInitInfo.baseLayer == "electrification") {
        ormElectrification.addTo(map);
    }

    trackLayerGroup = L.layerGroup();
    namedLayerGroup = L.layerGroup();
    restLayerGroup = L.layerGroup();

    let locomotiveFile = apiMapInitInfo.typeOfLocomotive.concat("-locomotive-icon.png");
    locomotiveIcon = L.icon({
        iconUrl: locomotiveFile,
        iconSize: [30, 25],
        iconAnchor: [15, 10],
    });
    arrowIcon = L.icon({
        iconUrl: 'arrow.png',
        iconSize: [30, 50],
        iconAnchor: [15, 50],
    });

    for (const pointOnMap of apiMapInitInfo.pointOnApiMapList) {
        if (pointOnMap.typeOfPointOnApiMap == 0) { // track
            L.circle([pointOnMap.latLon.Lat, pointOnMap.latLon.Lon], {
                color: pointOnMap.color,
                fillColor: pointOnMap.color,
                fillOpacity: 1,
                radius: 1
            }).addTo(trackLayerGroup);
        }
        if (pointOnMap.typeOfPointOnApiMap == 1) { // named
            L.circle([pointOnMap.latLon.Lat, pointOnMap.latLon.Lon], {
                color: pointOnMap.color,
                fillColor: pointOnMap.color,
                fillOpacity: 1,
                radius: 3
            }).addTo(namedLayerGroup).bindPopup(pointOnMap.name);
        }
        if (pointOnMap.typeOfPointOnApiMap == 2) { // rest
            L.circle([pointOnMap.latLon.Lat, pointOnMap.latLon.Lon], {
                color: pointOnMap.color,
                fillColor: pointOnMap.color,
                fillOpacity: 1,
                radius: 3
            }).addTo(restLayerGroup).bindPopup(pointOnMap.name);
        }
    }

    for (const lineOnMap of apiMapInitInfo.lineOnApiMapList) {
        pointList = [[lineOnMap.latLonFrom.Lat, lineOnMap.latLonFrom.Lon], [lineOnMap.latLonTo.Lat, lineOnMap.latLonTo.Lon]];
        polyline = new L.Polyline(pointList, {
            color: 'red',
            weight: 1,
            opacity: 0.5,
            smoothFactor: 0
        });
        polyline.addTo(trackLayerGroup);
    }

    map.fitBounds([[apiMapInitInfo.latMin, apiMapInitInfo.lonMin], [apiMapInitInfo.latMax, apiMapInitInfo.lonMax]], null);

    layerControl.addOverlay(trackLayerGroup, 'track');
    layerControl.addOverlay(namedLayerGroup, 'named');
    layerControl.addOverlay(restLayerGroup, 'rest');

    if (apiMapInitInfo.overlayLayer != null) {
        for (const layer of apiMapInitInfo.overlayLayer) {
            if ((layer.name == "track") && (layer.show == 1)) {
                trackLayerGroup.addTo(map);
            }
            if ((layer.name == "named") && (layer.show == 1)) {
                namedLayerGroup.addTo(map);
            }
            if ((layer.name == "rest") && (layer.show == 1)) {
                restLayerGroup.addTo(map);
            }
        }
    }

    map.on('baselayerchange', function(e) {
        ApiSendLayerChange('baseLayerChange', e.name);
    });
    map.on('overlayadd', function (e) {
        ApiSendLayerChange('overlayAdd', e.name);
    });
    map.on('overlayremove', function (e) {
        ApiSendLayerChange('overlayRemove', e.name);
    });
}

function ApiMap() {

    hr1.open("GET", "/API/MAP/", false);
    hr1.onreadystatechange = function () {
        if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {

            let latLonObj = JSON.parse(hr1.responseText);

            if (latLonObj != null) {

                let directionDeg = latLonObj.directionDeg;
                let latLon = [latLonObj.LatLon.Lat, latLonObj.LatLon.Lon];

                if (initToBeDone) {
                    // init
                    ApiMapInit();
                    arrowMarker = L.marker(
                        latLon,
                        {
                            icon: arrowIcon,
                            rotationAngle: directionDeg
                        }
                    ).addTo(map);
                    locomotiveMarker = L.marker(
                        latLon,
                        {
                            icon: locomotiveIcon
                        }
                    ).addTo(map);
                    initToBeDone = false;                  
                } else {
                    if ((latLon[0] != latLonPrev[0]) || (latLon[1] != latLonPrev[1]) || (directionDeg != directionDegPrev)) {
                        // changed
                        map.panTo(latLon);
                        arrowMarker.setRotationAngle(directionDeg);
                        arrowMarker.setLatLng(latLon).update();
                        locomotiveMarker.setLatLng(latLon).update();
                    }
                }
                latLonPrev[0] = latLon[0];
                latLonPrev[1] = latLon[1];
                directionDegPrev = directionDeg;
            }
        }
    }
    hr1.send();
}

function ApiSendLayerChange(action, name) {

    hr3.open("POST", "/API/MAP", true);
    hr3.setRequestHeader("Content-Type", "application/json");
    hr3.send
        (JSON.stringify
            ({
                "LayerAction": action,
                "LayerName": name
            })
        )
}
