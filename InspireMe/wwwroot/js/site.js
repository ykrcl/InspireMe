// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function registerphoneinput(element) {
    var id = element.getAttribute("id");
    var cleavePhone = new Cleave(('#'+id), {
        numericOnly: true,
        delimiters: [' ', ' ', ' '],
        blocks: [3, 3, 2, 2]
    });
}


function updatelogin() {
    $.ajax({
        url: "/Accounts/loginstatus/",
        type: 'GET',
    })
        .done(function (response) { //
            $('#LoginStatus').html(response);
        });
}
function loadpage(url) {
    $.ajax({
        url: url,
        type: "GET",
        success: function (response) {
            if (typeof response.redirect !== 'undefined') {
                loadpage(response.redirect);
            }
            else {
                $('#ContentPage').hide().html(response).fadeIn("normal");
                history.pushState({ url: url }, null, url);
            }
        }
    });
}


var currentUrl = document.location.href;
var pushState = history.pushState;
history.pushState = function () {
    pushState.apply(history, arguments);
    currentUrl = document.location.href;
};

var replaceState = history.pushState;
history.replaceState = function () {
    replaceState.apply(history, arguments);
    currentUrl = document.location.href;
};


$(document).ready(function () {
    history.pushState({ url: window.location.href }, null, window.location.href);
    $(document).on('click', 'a, p', {}, function (e) {
      if (typeof $(this).data('ajax') !== 'undefined') {
            var datatarget = $(this).data('target');
            var link = ($(this).attr("href"));
            var pushhistory = typeof $(this).data('nonstate') === 'undefined'
          var afterloading = $(this).data('ajaxafter');
          var beforeloading = $(this).data('ajaxbefore');
          if (typeof beforeloading !== "undefined") {
              eval(beforeloading)
          }
            $(("#" + datatarget)).hide().load(encodeURI(link), function () {
                $(("#" + datatarget)).fadeIn("normal");
                if (pushhistory) {
                    history.pushState({ url: link }, null, link);
                }
            });
          if (typeof afterloading !== "undefined") {
              eval(afterloading)
          }
            
            return false;
        }
        else {
            return true;
        }
    });


    $(document).on("submit", "form", function (event) {
        if (typeof $(this).data('ajaxform') !== 'undefined') {
            event.preventDefault(); //prevent default action 
            var post_url = $(this).attr("action"); //get form action url
            var request_method = $(this).attr("method");
            var parent = $(this).data("parentid"); //get form GET/POST method
            var form_data = $(this).serialize(); //Encode form elements for submission
            var confirm = $(this).attr('confirm');
            var afterloading = $(this).data('ajaxafter');
            var beforeloading = $(this).data('ajaxbefore');
            
            var formsubmit = false;
            if (typeof confirm !== typeof undefined && confirm !== false) {
                $("<div title='Onayla'>" + confirm + "</div>").dialog({
                    title: title,
                    resizable: false,
                    modal: true,
                    buttons: {
                        "Onayla": function () {
                            formsubmit = true;
                            $(this).dialog("close");
                        },
                        "Vazgeç": function () {
                            formsubmit = false;
                            $(this).dialog("close");
                        }
                    }
                });
            }
            else {
                formsubmit = true;
            }
            if (formsubmit) {
                if (typeof beforeloading !== "undefined") {
                    eval(beforeloading)
                }
                $.ajax({
                    url: post_url,
                    type: request_method,
                    data: form_data,
                    success: function (response) {
                        if (response.success === true || response.success === false) {
                            if (response.alert) {
                                $("<div>" + response.alert + "</div>").dialog({
                                    resizable: false,
                                    modal: true,
                                });
                            }
                            if (response.updatelogin) {
                                updatelogin();
                            }
                            if (typeof response.redirect !== 'undefined') {
                                if (typeof response.confirmredirect !== 'undefined') {
                                    $("<div></div>").html(response.confirmredirect).dialog({
                                        title: response.redirecttitle,
                                        resizable: false,
                                        modal: true,
                                        buttons: {
                                            "Onayla": function () {
                                                
                                                history.pushState({ url: response.redirect }, null, response.redirect);
                                                loadpage(response.redirect);
                                                $(this).dialog("close");
                                            },
                                            "Vazgeç": function () {
                                                $(this).dialog("close");
                                            }
                                        }
                                    });
                                }
                                else {
                                    history.pushState({ url: response.redirect }, null, response.redirect);
                                    loadpage(response.redirect);
                                }
                            }
                            if (typeof afterloading !== "undefined") {
                                eval(afterloading)
                            }
                        }
                        else {
                                $(('#' + parent)).animate({
                                    scrollTop: 0
                                }, 800);    
                                 $('#' + parent).html(response);
                        }
                    }
                });
            }
        }
    });



    $(window).on('popstate', function (e) {
        var state = e.originalEvent.state;
        if (state.url !== null && state.url !== '' && state.url !== undefined) {
            $.ajax({
                url: state.url,
                type: "GET",
                success: function (response) {
                    if (typeof response.redirect !== 'undefined') {
                        loadpage(response.redirect);
                    }
                    else {
                        $('#ContentPage').hide().html(response).fadeIn("normal");
                    }
                    logincheck();
                }
            });
        }
        currentUrl = state.url;
    });
});

   