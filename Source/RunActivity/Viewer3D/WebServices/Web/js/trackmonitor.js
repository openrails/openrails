var PageNo = 0;
var hr = new XMLHttpRequest;


function TrackMonitor () {
    hr.open("POST", "/API/TRACKMONITOR", true);
    hr.send("pageno=" + PageNo);
	hr.onreadystatechange = function () {
		if (this.readyState == 4 && this.status == 200) {
			var obj = JSON.parse(hr.responseText);
			var Str = obj.str;  
			common.innerHTML = Str;
		}
	}
}

