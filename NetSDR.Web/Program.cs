using NetSDR.Client.Interfaces;
using NetSDR.Client.Tcp;
using NetSDR.Client.Udp;
using NetSDR.Simulator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<INetSdrClient, NetSdrTcpClient>();
builder.Services.AddSingleton<ITcpNetworkClient, TcpNetworkClient>();
builder.Services.AddSingleton<IUdpDataReceiver, UdpDataReceiver>();
builder.Services.AddSingleton<ITcpSimulatorService, TcpSimulatorService>();
builder.Services.AddSingleton<IUdpSimulatorService, UdpSimulatorService>();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", context =>
{
    context.Response.Redirect("/NetSdrControl");
    return Task.CompletedTask;
});

app.Services.GetRequiredService<ITcpSimulatorService>().Start();
app.Services.GetRequiredService<IUdpSimulatorService>().Start();

app.Run();
