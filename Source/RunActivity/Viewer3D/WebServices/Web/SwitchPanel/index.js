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

const elemButton = document.getElementById("buttonId");
const elemIframe = document.getElementById("iframeId");

elemIframe.hidden = true;

function openFullscreen() {
        
    if (elemIframe.requestFullscreen) {
        elemIframe.requestFullscreen();
    } else if (element.mozRequestFullScreen) {
        element.mozRequestFullScreen();                 // Firefox
    } else if (elemIframe.webkitRequestFullscreen) {
        elemIframe.webkitRequestFullscreen();           // Safari
    } else if (elemIframe.msRequestFullscreen) {
        elemIframe.msRequestFullscreen();               // IE11
    }
}

document.addEventListener('fullscreenchange', () => {
    
    if (document.fullscreenElement) {
        console.log('Entered fullscreen:');
        elemButton.hidden = true; 
        elemIframe.hidden = false;
    } else {
        console.log('Exited fullscreen.');
        elemButton.hidden = false; 
        elemIframe.hidden = true;
    }
});
