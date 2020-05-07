var PageNo = 0;
var hr = new XMLHttpRequest;

function ApiSample() {
	hr.open("POST", "/API/APISAMPLE", true);
	hr.send("pageno=" + PageNo);
	hr.onreadystatechange = function () {
		if (this.readyState == 4 && this.status == 200) {
			var obj = JSON.parse(hr.responseText);
            strData.innerHTML = obj.strData;
            intData.innerHTML = obj.intData;
            dateData.innerHTML = obj.dateData;

            arrayData = obj.strArrayData;

            arrayMember0.innerHTML = obj.strArrayData[0];
            arrayMember1.innerHTML = obj.strArrayData[1];
            arrayMember2.innerHTML = obj.strArrayData[2];
            arrayMember3.innerHTML = obj.strArrayData[3];
            arrayMember4.innerHTML = obj.strArrayData[4];

            embeddedStr.innerHTML = obj.embedded.Str;
            embeddedNumb.innerHTML = obj.embedded.Numb;

		}
	}
}
