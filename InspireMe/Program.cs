using InspireMe.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using InspireMe.Areas.Meeting.Hubs;
using InspireMe.Hubs;
using InspireMe.BackgroundTasks;
using FluentEmail.Smtp;
using System.Net.Mail;
using Dapper;
using System.Net;
using InspireMe.Data;

SqlMapper.AddTypeHandler(new DapperSqlDateOnlyTypeHandler());
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDapperStores(builder.Configuration.GetConnectionString("InspireMeDb")).AddDefaultTokenProviders();
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
});
var emailconf = builder.Configuration.GetSection("EmailSettings");
var sender = new SmtpClient(emailconf.GetValue<string>("server"))
{
    UseDefaultCredentials = false,
    Port = emailconf.GetValue<int>("port"),
    Credentials = new NetworkCredential(emailconf.GetValue<string>("username"), emailconf.GetValue<string>("password")),
    EnableSsl = true
};
builder.Services.AddFluentEmail(emailconf.GetValue<string>("FromMail")).AddSmtpSender(sender);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddAntiforgery();
builder.Services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

builder.Services.AddHostedService<AlertMeetingTimeHostedService>();
builder.Services.AddScoped<AlerttingIScopedProcessingService, AlertingScopedProcessingService>();
builder.Services.AddSignalR().AddHubOptions<MeetingHub>(opts =>
{
    opts.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("tr"),
        new CultureInfo("en-us")
    };

    options.DefaultRequestCulture = new RequestCulture(culture: "tr", uiCulture: "tr");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
    {
        var lang = context.GetRouteValue("lang");
        if (!(lang == null))
        {
            if (lang.ToString() == "en")
            {
                return new ProviderCultureResult("en-us");
            } 
       }
        return new ProviderCultureResult("tr");
    }));
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "CanConnectMeetingsHub",
        policyBuilder => policyBuilder.AddRequirements(
            new MeetingConnectionsRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, HasMeetingHandler>();
builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLocalization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<MeetingHub>("/Meetings/MeetingsHub");
    endpoints.MapHub<SiteNotificationConnection>("/NotificationsHub");
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{lang=tr}/{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
    name: "default",
    pattern: "{lang=tr}/{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
   name: "default",
   pattern: "{controller=Home}/{action=Index}/{id?}");
});


app.Run();
