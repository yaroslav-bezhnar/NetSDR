using System.Net;
using System.Net.Sockets;
using System.Text;
using NetSDR.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<NetSdrClient>();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

_ = Task.Run(async () =>
{
    var listener = new TcpListener(IPAddress.Loopback, 50000);
    listener.Start();
    Console.WriteLine("Test TCP server started on port 50000");

    while (true)
    {
        try
        {
            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Client connected");

            var stream = client.GetStream();
            var buffer = Encoding.ASCII.GetBytes("Hello client");
            await stream.WriteAsync(buffer, 0, buffer.Length);

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("TCP server error: " + ex.Message);
        }
    }
});

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

app.Run();
