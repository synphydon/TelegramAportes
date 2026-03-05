using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Aportes.Aportes
{
    public class TelegramBot
    {
        public string _botToken = "";
        public ITelegramBotClient _bot;
        public long _chatIdCanal = 0;

        public Procesos procesos = new Procesos();
        private ManejoDeTelegram.Mensajes EnvioDeMensajes = new ManejoDeTelegram.Mensajes();

        public string PathMultimedia = "";

        public async Task AbrirBotyEscuchar()
        {
            var botClient = new TelegramBotClient(_botToken);

            var me = await botClient.GetMe();
            Console.WriteLine($"Bot conectado como @{me.Username}");
            using var cts = new CancellationTokenSource();

            _bot = botClient;

            procesos._botEnvio = botClient;
            procesos._chatIdCanal = _chatIdCanal;
            procesos._PathMultimedia = PathMultimedia;

            botClient.StartReceiving(
                updateHandler: async (iBot, update, ct) =>
                {
                    
                    /*if (update.Type == UpdateType.ChannelPost)
                    {
                        var mensaje = update.ChannelPost;

                        Console.WriteLine("Mensaje desde canal");
                        Console.WriteLine(mensaje.Chat.Id);
                        Console.WriteLine(mensaje.Text);
                    }*/
                    

                    if ((update.Type == UpdateType.Message && update.Message?.Text != null) || (update.Type == UpdateType.Message && update.Message?.Photo != null) || (update.Type == UpdateType.Message && update.Message?.Video != null || update.Message?.Voice != null))
                    {
                        await reciveMensaje(update, ct);
                    }

                    if (update.Type == UpdateType.CallbackQuery)
                    {
                        await reciveCallback(update, ct);
                    }
                },
                errorHandler: async (bot, exception, ct) =>
                {
                    Console.WriteLine($"Error: {exception.Message}");
                    await Task.CompletedTask;
                },
                cancellationToken: cts.Token
            );

            procesos.ControlarEnviosAbiertos();

            await Task.Delay(Timeout.Infinite);

            cts.Cancel();
        }

        public async Task reciveMensaje(Update mensaje, CancellationToken ct)
        {
            if ((mensaje.Message.Photo != null && mensaje.Message.MediaGroupId != null) || (mensaje.Message.Video != null && mensaje.Message.MediaGroupId != null))
            {
                await procesos.RecibeMultimedia(_bot, ct, mensaje);
            }
            else
            {
                await procesos.RecibeMultimedia(_bot, ct, mensaje);
            }

            if (mensaje.Message.Voice != null)
            {
                await procesos.RecibeAudio(_bot, ct, mensaje);
            }

            if (mensaje.Message.Text != null)
            {
                switch (mensaje.Message.Text.ToLower())
                {
                    case "/start":
                        await procesos.Start(_bot, ct, mensaje);
                        return;
                    case "hola":
                        await EnvioDeMensajes.MensajeDeTexto(_bot, ct, mensaje.Message.From.Id, "👋 Hola");
                        break;
                    default:
                        await procesos.RecibeTexto(_bot, ct, mensaje);
                        break;
                }
            }
        }

        public async Task reciveCallback(Update mensaje, CancellationToken ct)
        {
            await procesos.RecibeCallback(_bot, ct, mensaje);
        }
    }
}
