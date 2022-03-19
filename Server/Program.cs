using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Pinglingle.Server;
using Pinglingle.Server.Hubs;
using Pinglingle.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddCommandLine(args);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

if (builder.Configuration["ConnectionString"] is { Length: > 0 } connectionString)
{
    builder.Services.AddDbContext<MyContext>(
        options => options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<MyContext>(
        options => options.UseInMemoryDatabase("Pinglingle"));
}

builder.Services.AddHostedService<DigestService>();
builder.Services.AddHostedService<PingService>();

var app = builder.Build();

// Apply database migrations, if using a real database.
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (configuration["ConnectionString"] is { Length: > 0 })
    {
        var db = scope.ServiceProvider.GetRequiredService<MyContext>();
        db.Database.Migrate();
    }
}

app.UseResponseCompression();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapHub<PingHub>("/pinghub");
app.MapFallbackToFile("index.html");

app.Run();
