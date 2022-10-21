using Microsoft.Extensions.DependencyInjection;
using Tinkoff.InvestApi;
using TinkoffAPIConsole;


var token = Environment.GetEnvironmentVariable("TOKEN");

ServiceCollection services = new();

services.AddInvestApiClient((_, settings) =>
{
    settings.AccessToken = token;
});
var serviceProvider = services.BuildServiceProvider();

var client = serviceProvider.GetRequiredService<InvestApiClient>();

MainMenu.PrintMainMenu(client).Wait();


 Console.ReadLine();



