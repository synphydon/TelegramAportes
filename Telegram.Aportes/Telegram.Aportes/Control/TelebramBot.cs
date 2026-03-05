using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace Telegram.Aportes.Control
{
    public class TelegramBot
    {
        public string _botControl = "";
        public string PathMultimedia = "";
        public long _chatIdAdmin = 0;

        public ITelegramBotClient _bot;

        private ManejoDeTelegram.Mensajes EnvioDeMensajes = new ManejoDeTelegram.Mensajes();


        private Procesos AccesoDatos = new Procesos();


        public async Task AbrirBotyEscuchar()
        {
            var botClient = new TelegramBotClient(_botControl);

            var me = await botClient.GetMe();
            Console.WriteLine($"Bot conectado como @{me.Username}");

            using var cts = new CancellationTokenSource();

            botClient.StartReceiving(
                updateHandler: async (iBot, update, ct) =>
                {
                    if (update.Message != null)
                    {
                        if (update.Message.Chat.Id != _chatIdAdmin)
                            return;
                    }

                    if (update.CallbackQuery != null)
                    {
                        if (update.CallbackQuery.Message.Chat.Id != _chatIdAdmin)
                            return;
                    }

                    _bot = iBot;
                    if ((update.Type == UpdateType.Message && update.Message?.Text != null) || (update.Type == UpdateType.Message && update.Message?.Photo != null))
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

            await Task.Delay(Timeout.Infinite);

            cts.Cancel();
        }

        public async Task reciveMensaje(Update mensaje, CancellationToken ct)
        {
            if (mensaje.Message.Text != null)
            {
                switch (mensaje.Message.Text.ToLower())
                {
                    case "/start":
                        await AccesoDatos.Start(_bot, ct, mensaje);
                        return;
                    case "hola":
                        await EnvioDeMensajes.MensajeDeTexto(_bot, ct, mensaje.Message.From.Id, "👋 Hola");
                        break;
                    default:
                        break;
                }
            }
        }

        public async Task reciveCallback(Update mensaje, CancellationToken ct)
        {
            await AccesoDatos.RecibeCallback(_bot, ct, mensaje);
        }
    }
}