var PageNo = 0;
var hr = new XMLHttpRequest;

function ApiTrainInfo() {
    hr.open("POST", "/API/TRAININFO", true);
    hr.send("pageno=" + PageNo);
    hr.onreadystatechange = function () {
        if (this.readyState == 4 && this.status == 200) {
            var obj = JSON.parse(hr.responseText);

            strTrainInfoData.innerHTML = obj.allowedSpeedMps;

        }
    }
}