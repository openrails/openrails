var PageNo = 0;
var hr = new XMLHttpRequest;


function HeadsUp () {
	hr.open("POST", "/API/HUD", true);
	hr.send("pageno="+PageNo);
	hr.onreadystatechange = function () {
		if (this.readyState == 4 && this.status == 200) {
			var obj = JSON.parse(hr.responseText);
			var Rows = obj.commonTable.nRows;
			var Cols = obj.commonTable.nCols;
			Str = "<table>";  
			var next = 0;
			for (var row = 0; row < obj.commonTable.nRows; ++row) {
				Str += "<tr>";
				for (var col=0; col < obj.commonTable.nCols; ++col) { 
					if (obj.commonTable.values[next] == null) {
						Str += "<td></td>";
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

			if (obj.nTables == 2) {
				var Rows = obj.extraTable.nRows;
				var Cols = obj.extraTable.nCols;
				next = 0;
				Str = "<table>";  
				for (var row = 0; row < obj.extraTable.nRows; ++row) {
					Str += "<tr>";
					for (var col=0; col < obj.extraTable.nCols; ++col) { 
						if (obj.extraTable.values[next] == null) {
							Str += "<td></td>";
						}
						else {
							Str += "<td>"  + obj.extraTable.values[next] + "</td>";
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

