﻿using InspireMe.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;

namespace InspireMe.Areas.Meeting.Hubs
{


    public class ValidateConnection : AuthorizeAttribute
    {

    }
    [Authorize(Policy = "CanConnectMeetingsHub")]
    public class MeetingHub : Hub
    {
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly BookingsTable bookingsTable;
        private readonly IStringLocalizer<MeetingHub> _localizer;

        public MeetingHub(IDatabaseConnectionFactory connectionFactory, UserManager<IdentityUser> userManager, IStringLocalizer<MeetingHub> localizer)
        {
            _userManager = userManager;
            _connectionFactory = connectionFactory;
            bookingsTable = new BookingsTable(_connectionFactory);
            _localizer = localizer;
        }

        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var meetingid = httpContext.Request.Query["meetingid"];
            var meeting = bookingsTable.FindBookingByIdAsync(Guid.Parse(meetingid)).Result;
            var user = _userManager.GetUserAsync(Context.User).Result;
            if (meeting != null)
            {
                if (!(meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour) && meeting.IsEnded == true)
                {
                    Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Bulunamadı"]).Wait();
                    Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                }
                else if (!(meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour) && meeting.IsStarted == true && !(_userManager.IsInRoleAsync(user, "Supervisor").Result))
                {
                    if (meeting.CustomerRTCId != null) {
                        Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Hatası"]).Wait();
                        Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                    }
                    bookingsTable.ConnectCustomertoMeetingAsync(Context.ConnectionId, meeting.Id).Wait();
                    Groups.AddToGroupAsync(Context.ConnectionId, meeting.Id.ToString());
                    if (meeting.SupervisorRTCId != null) { 
                    Clients.Client(Context.ConnectionId).SendAsync("StartWebRtC", true).Wait();
                    }
                }
                else if (!(meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour) && (_userManager.IsInRoleAsync(user, "Supervisor").Result))
                {
                    if (meeting.SupervisorRTCId != null)
                    {
                        Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Hatası"]).Wait();
                        Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                    }
                    bookingsTable.StartMeetingAsync(meeting.Id).Wait();
                    bookingsTable.ConnectSupervisortoMeetingAsync(Context.ConnectionId, meeting.Id).Wait();
                    Groups.AddToGroupAsync(Context.ConnectionId, meeting.Id.ToString());
                    if (meeting.CustomerRTCId != null)
                    {
                        Clients.Client(Context.ConnectionId).SendAsync("StartWebRtC", true).Wait();
                    }
                }
                else if (meeting.Date == DateOnly.FromDateTime(DateTime.Now) && meeting.Hour == DateTime.Now.Hour)
                {
                    if (_userManager.IsInRoleAsync(user, "Supervisor").Result)
                    {
                        if (meeting.SupervisorRTCId != null)
                        {
                            Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Hatası"]).Wait();
                            Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                        }
                        bookingsTable.StartMeetingAsync(meeting.Id).Wait();
                        bookingsTable.ConnectSupervisortoMeetingAsync(Context.ConnectionId, meeting.Id).Wait();
                        Groups.AddToGroupAsync(Context.ConnectionId, meeting.Id.ToString());
                        if (meeting.CustomerRTCId != null)
                        {
                            Clients.Client(Context.ConnectionId).SendAsync("StartWebRtC", true).Wait();
                        }
                    }
                    else
                    {
                        if (meeting.CustomerRTCId != null)
                        {
                            Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Hatası"]).Wait();
                            Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                        }
                        bookingsTable.StartMeetingAsync(meeting.Id).Wait();
                        bookingsTable.ConnectCustomertoMeetingAsync(Context.ConnectionId, meeting.Id).Wait();
                        Groups.AddToGroupAsync(Context.ConnectionId, meeting.Id.ToString());
                        if (meeting.SupervisorRTCId != null)
                        {
                            Clients.Client(Context.ConnectionId).SendAsync("StartWebRtC", true).Wait();
                        }
                    }
                }
                else
                {
                    Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Bulunamadı"]).Wait();
                    Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
                }
            }
            else
            {
                Clients.Client(Context.ConnectionId).SendAsync("ShowErrorMessage", _localizer["Toplantı Bulunamadı"]).Wait();
                Clients.Client(Context.ConnectionId).SendAsync("EndMeeting").Wait();
            }
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            var meeting = bookingsTable.FindBookingByConnectionId(Context.ConnectionId).Result;
            if (meeting != null)
            {
                bookingsTable.DisconnectConnectfromMeetingAsync(Context.ConnectionId).Wait();
                Groups.RemoveFromGroupAsync(Context.ConnectionId, meeting.Id.ToString()).Wait();
                meeting = bookingsTable.FindBookingByIdAsync(meeting.Id).Result;
                if (meeting.CustomerRTCId == null && meeting.SupervisorRTCId == null)
                {
                    bookingsTable.EndMeetingAsync(meeting.Id).Wait();
                }
                else
                {
                    Clients.Group(meeting.Id.ToString()).SendAsync("OtherLostConnection", true).Wait();
                }
            }
            return base.OnDisconnectedAsync(exception);
        }
        public async Task SendMessage(string message)
        {
            var meeting = bookingsTable.FindBookingByConnectionId(Context.ConnectionId).Result;
            if (meeting != null) { 
            var user = _userManager.GetUserAsync(Context.User).Result;
            if (meeting.ChatHistory!= null) {
                var updatedChat = meeting.ChatHistory + "\n" + user.UserName + ": " + message;
                bookingsTable.UpdateChatHistoryMeetingAsync(updatedChat, meeting.Id).Wait();
            }
            else
            {
                var updatedChat = user.UserName + ": " + message;
                bookingsTable.UpdateChatHistoryMeetingAsync(updatedChat,meeting.Id).Wait();
            }
            await Clients.Group(meeting.Id.ToString()).SendAsync("ReceiveMessage", message);
            }
        }
        public async Task ConnectWebRtc(string sdpjson)
        {
            var meeting = bookingsTable.FindBookingByConnectionId(Context.ConnectionId).Result;
            if (meeting != null)
            {
                await Clients.Group(meeting.Id.ToString()).SendAsync("InitiateRemoteRtc", sdpjson);
            }
        }
        public async Task AnswerRTC(string sdpjson)
        {
            var meeting = bookingsTable.FindBookingByConnectionId(Context.ConnectionId).Result;
            if (meeting != null)
            {
                await Clients.Group(meeting.Id.ToString()).SendAsync("AnswerRTC", sdpjson);
            }
        }
    }
}
