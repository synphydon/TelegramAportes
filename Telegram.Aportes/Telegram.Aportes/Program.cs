using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Telegram.Aportes;
using Telegram.dts.Models;

Telegram.Aportes.Aportes.TelegramBot botAportes = new Telegram.Aportes.Aportes.TelegramBot();
Telegram.Aportes.Control.TelegramBot botControl = new Telegram.Aportes.Control.TelegramBot();

string env = "des";

/*
if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == null)
{
    Console.WriteLine("DOTNET_ENVIRONMENT no está definido, se usará 'des' por defecto.");
    Console.WriteLine("");
    env = "des";
}
else
{
    env = "";
}
*/

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Configuracion configuracion = new Configuracion();
configuracion.ConnectionString = config["CONNECTION_STRING"] ?? config["MySql:ConnectionString"];
configuracion.BotToken = config["TELEGRAM_BOT_TOKEN"] ?? config["Telegram:BotToken"];
configuracion.BotControl = config["TELEGRAM_BOT_CONTROL"] ?? config["Telegram:BotControl"];
configuracion.ChatIdCanal = Convert.ToInt64(config["CHAT_ID_CANAL"] ?? config["Telegram:ChatIdCanal"]);
configuracion.PathMultimedia = config["PATH_MULTIMEDIA"] ?? config["Telegram:PathMultimedia"];
configuracion.ChatIdAdmin = Convert.ToInt64(config["CHAT_ID_ADMIN"] ?? config["Telegram:ChatIdAdmin"]);

Telegram.dts.Clases.General.Conexion = configuracion.ConnectionString;

General.LeeMensajes();

botAportes._botToken = configuracion.BotToken;
botAportes.PathMultimedia = configuracion.PathMultimedia;
botAportes._chatIdCanal = configuracion.ChatIdCanal;
botAportes.AbrirBotyEscuchar();

botControl._botControl = configuracion.BotControl;
botControl.PathMultimedia = configuracion.PathMultimedia;
botControl._chatIdAdmin = configuracion.ChatIdAdmin;
botControl.AbrirBotyEscuchar();

await Task.Delay(3000);
Console.WriteLine("");
Console.WriteLine("Bots en ejecucion.");

Console.WriteLine("Presione CTRL+C para detener el bot.");
await Task.Delay(Timeout.Infinite);

Console.ReadLine();


