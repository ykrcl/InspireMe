"use strict";

var notifconnection = new signalR.HubConnectionBuilder().withUrl("/NotificationsHub").build();


notifconnection.on("ShowNotification", function (message) {
    var html = '<div class="alert alert-warning alert-dismissible fade show" role="alert">';
    html += message;
    html += '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>';
    html += '</div>';
    $(html).hide().prependTo('#notificationdiv').slideDown();
});

notifconnection.on("EndConnection", function (logout) {
    if (logout) { 
    notifconnection.stop();
        $('#notificationdiv').html("");
    }
});

notifconnection.start().catch(function (err) {
    return console.error(err.toString());
});