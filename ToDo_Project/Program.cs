using Microsoft.EntityFrameworkCore;
using Data.Repositories;
using Models.Domain.Observers;
using Models.Domain.Patterns.Observers;
using Models.Domain.Patterns.Strategies.Notifications;
using DomainTaskFactory = Models.Domain.Patterns.Factories.TaskFactory;
using ReminderTaskScheduler = Services.TaskScheduler;
using Services;
using ToDo_Project.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Connect postgreSQL database
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var botToken = builder.Configuration["BotSettings:Token"]
    ?? throw new InvalidOperationException("BotSettings:Token is not configured.");

builder.Services.AddSingleton(new TelegramBotService(botToken));
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<DomainTaskFactory>();

builder.Services.AddScoped<INotificationStrategy, TelegramNotificationStrategy>();

builder.Services.AddScoped<ITaskObserver, NotificationObserver>();
builder.Services.AddScoped<ITaskObserver, LoggerObserver>();
builder.Services.AddScoped<ITaskObserver, AnalyticsObserver>();

builder.Services.AddHostedService<ReminderTaskScheduler>();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "tasks-alias",
    pattern: "Tasks",
    defaults: new { controller = "Home", action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tasks}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
