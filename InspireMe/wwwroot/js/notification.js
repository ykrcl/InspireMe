"use strict";

var notifconnection = new signalR.HubConnectionBuilder().withUrl("/NotificationsHub").build();


notifconnection.on("ShowNotification", function (message) {
    var html = '<div class="alert alert-info" alert-dismissable page-alert">';
    html += '<button type="button" class="close"><span aria-hidden="true">×</span><span class="sr-only">Close</span></button>';
    html += message;
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