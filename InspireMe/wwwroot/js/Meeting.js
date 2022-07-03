"use strict";

let stream = null;
async function getConnectedDevices(type) {
    const devices = await navigator.mediaDevices.enumerateDevices();
    return devices.filter(device => device.kind === type)
}

function updateCameraList(cameras) {
    const listElement = document.querySelector('select#availableCameras');
    listElement.innerHTML = '';
    cameras.map(camera => {
        const cameraOption = document.createElement('option');
        cameraOption.label = camera.label;
        cameraOption.value = camera.deviceId;
    }).forEach(cameraOption => listElement.add(cameraOption));
}


async function openCamera() {
    var cameraId = $("#availableCameras").val();
    const constraints = {
        'audio': { 'echoCancellation': true },
        'video': {
            'deviceId': cameraId,
            'width': { 'min': 1280 },
            'height': { 'min': 720 }
        }
    }

    return await navigator.mediaDevices.getUserMedia(constraints);
}




function LoadChatHistory(history) {
    var ks = history.split(/\r?\n/);
    ks.forEach(message => {
        var li = document.createElement("li");
        document.getElementById("messagesList").appendChild(li);
        li.textContent = message;
    });
}



var username = $("#remote_UserName").html();
var meetingid = $("#meetingid").html();
const remoteVideo = document.querySelector('remoteVideo');
const videoElement = document.querySelector('localVideo');

navigator.mediaDevices.getUserMedia({ video: true, audio: true }).then(x => {
    stream = x;
    videoElement.srcObject = x;
    getConnectedDevices('videoinput').then(y => {
        updateCameraList(y);
    }).catch(error => {
        console.log('Error :', error)
    });
}).catch(error => {
    console.log('Error :', error)
});

navigator.mediaDevices.addEventListener('devicechange', event => {
    navigator.mediaDevices.getUserMedia({ video: true, audio: true }).then(x => {
        stream = x;
        videoElement.srcObject = x;
        getConnectedDevices('videoinput').then(y => {
            updateCameraList(y);
            $("#CameraModal").modal('show');
        }).catch(error => {
            console.log('Error :', error)
        });
    }).catch(error => {
        console.log('Error :', error)
    });
});



var connection = new signalR.HubConnectionBuilder().withUrl("/Meetings/MeetingsHub?meetingid=" + meetingid).build();
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    li.textContent = `${username}:  ${message}`;
});

connection.on("EndMeeting", function (isending) {
    if (isending) {
        connection.stop();
        document.getElementById("sendButton").disabled = true;
    }
});

connection.on("ShowErrorMessage", function (message) {
    $("<div>" + message + "</div>").dialog({
        resizable: false,
        modal: true,
    });
});

connection.on("OtherLostConnection", function (isshowing) {
    if (isshowing) {
        $("#WaitModal").modal('show');
    }
});

const configuration = { 'iceServers': [{ 'urls': 'stun:74.125.142.127:19302' }] }
const peerConnection = new RTCPeerConnection(configuration);


connection.on("StartWebRtC", async function (sdpjson) {
    $("#WaitModal").modal('show');
    const offer = await peerConnection.createOffer();
    await peerConnection.setLocalDescription(offer);
    connection.invoke("ConnectWebRtc", JSON.stringify(offer)).catch(function (err) {
        return console.error(err.toString());
    });
});

connection.on("InitiateRemoteRtc", async function (sdpjson) {
    $("#WaitModal").modal('hide');
    $("#CameraModal").modal('show');
    const offer = JSON.parse(sdpjson);
    await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
    const answer = await peerConnection.createAnswer();
    await peerConnection.setLocalDescription(answer);
    connection.invoke("AnswerRTC", JSON.stringify(answer)).catch(function (err) {
        return console.error(err.toString());
    });
});

connection.on("AnswerRTC", async function (sdpjson) {
    $("#WaitModal").modal('hide');
    $("#CameraModal").modal('show');
    const answer = JSON.parse(sdpjson);
    await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
});
connection.start().then(function () {
    $("#WaitModal").modal('show');
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var message = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});



document.getElementById("SaveCamButton").addEventListener("click", async function (event) {
    if ($("#availableCameras").val() !== null) {
        const stream = await openCamera();
        videoElement.srcObject = stream;
        const senders = peerConnection.getSenders();
        senders.forEach((sender) => peerConnection.removeTrack(sender));
        stream.getTracks().forEach(track => {
            peerConnection.addTrack(track, stream);
        });
    }
});



peerConnection.addEventListener('track', async (event) => {
    const [remoteStream] = event.streams;
    remoteVideo.srcObject = remoteStream;
});

document.getElementById("MuteAudio").addEventListener("click", function (event) {
    stream.getTracks().forEach((track) => {
        if (track.kind === "audio") {
            track.enabled = !track.enabled;
            if (track.enabled) {
                $("#MuteAudio").html('<i style="color:red;" class="fa-solid fa-volume"></i>');
            }
            else {
                $("#MuteAudio").html('<i style="color:red;" class="fa-solid fa-volume-slash"></i>');
            }
        }
    });

});

document.getElementById("StopVideo").addEventListener("click", function (event) {
    stream.getTracks().forEach((track) => {
        if (track.kind === "video") {
            track.enabled = !track.enabled;
            if (track.enabled) {
                $("#StopVideo").html('<i style="color:red;" class="fa-solid fa-video"></i>');
            }
            else {
                $("#StopVideo").html('<i style="color:red;" class="fa-solid fa-video-slash"></i>');
            }
        }
    });

});

