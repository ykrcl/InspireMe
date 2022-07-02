using InspireMe.Data;
using InspireMe.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using InspireMe.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using InspireMe.Hubs;
using FluentEmail.Core;

namespace InspireMe.BackgroundTasks
{
    internal interface AlerttingIScopedProcessingService
    {
        Task DoWork(CancellationToken stoppingToken);
    }

    internal class AlertingScopedProcessingService : AlerttingIScopedProcessingService
    {
        private int executionCount = 0;
        private readonly ILogger _logger;
        private readonly BookingsTable bookingsTable;
        private readonly IHubContext<SiteNotificationConnection> _NotificationhubContext;
        private readonly IUserConnectionManager _userConnectionManager;
        private readonly IFluentEmailFactory _emailFactory;
        private IDatabaseConnectionFactory _connectionFactory;
        private readonly IStringLocalizer<AlertingScopedProcessingService> _localizer;
        public AlertingScopedProcessingService(ILogger<AlertingScopedProcessingService> logger, IFluentEmailFactory emailFactory, IUserConnectionManager userConnectionManager, IDatabaseConnectionFactory connectionFactory, IHubContext<SiteNotificationConnection> NotificationhubContext, IStringLocalizer<AlertingScopedProcessingService> localizer)
        {
            _logger = logger;
            _localizer = localizer;
            _connectionFactory = connectionFactory;
            _NotificationhubContext = NotificationhubContext;
            bookingsTable = new BookingsTable(_connectionFactory);
            _userConnectionManager = userConnectionManager;
            _emailFactory = emailFactory;
        }

        public async Task DoWork(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                executionCount++;

                var upcomingmeetings = await bookingsTable.GetUpcomingMeetings();
                if (upcomingmeetings.Count() > 0)
                {
                    foreach(var meeting in upcomingmeetings)
                    {
                        try { 
                             var customerconnections = _userConnectionManager.GetUserConnections(meeting.Customer.Id);
                        foreach (var connection in customerconnections)
                        {
                            await _NotificationhubContext.Clients.Client(connection).SendAsync("ShowNotification", _localizer["Toplantınız Yaklaşıyor 5 Dakika İçinde Başlayacaktır Lütfen Toplantı Sayfasına Gidiniz."].Value);
                        }
                        var email1 = _emailFactory
                    .Create()
                    .To(meeting.Customer.Email)
                    .Subject(_localizer["Toplantı Yaklaşıyor"].Value)
                    .Body( _localizer["Toplantınız Yaklaşıyor 5 Dakika İçinde Başlayacaktır Lütfen Toplantı Sayfasına Gidiniz."].Value);

                        await email1.SendAsync();
                        var supervisorconnections = _userConnectionManager.GetUserConnections(meeting.Supervisor.Id);
                        foreach (var connection in supervisorconnections)
                        {
                            await _NotificationhubContext.Clients.Client(connection).SendAsync("ShowNotification", _localizer["Toplantınız Yaklaşıyor 5 Dakika İçinde Başlayacaktır Lütfen Toplantı Sayfasına Gidiniz."].Value);
                        }
                        var email2 = _emailFactory
                    .Create()
                    .To(meeting.Supervisor.Email)
                    .Subject(_localizer["Toplantı Yaklaşıyor"].Value)
                    .Body(_localizer["Toplantınız Yaklaşıyor 5 Dakika İçinde Başlayacaktır Lütfen Toplantı Sayfasına Gidiniz."].Value);

                        await email2.SendAsync();
                        }
                        catch { }
                    }
                }

                var timeOfDay = DateTime.Now.TimeOfDay;
                var nextFullHour = TimeSpan.FromHours(Math.Ceiling(timeOfDay.TotalHours));
                var delta = ((int)(nextFullHour - timeOfDay).Add(TimeSpan.FromMinutes(-3)).TotalMilliseconds);

                _logger.LogInformation(
                    "Meeting Notification Service is working. Count: {Count}", executionCount);

                await Task.Delay(delta, stoppingToken);
            }
        }
    }
    public class AlertMeetingTimeHostedService: BackgroundService
    {
        public IServiceProvider Services { get; }
        private readonly ILogger<AlertMeetingTimeHostedService> _logger;

        public AlertMeetingTimeHostedService(IServiceProvider services,
            ILogger<AlertMeetingTimeHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Notify Meeting Hosted Service running.");

            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Notify Meeting Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<AlerttingIScopedProcessingService>();

                await scopedProcessingService.DoWork(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Notify Meeting Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
