using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Aportes.Modelos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.dts.Models;

namespace Telegram.Aportes.Control
{
    public class Procesos
    {
        private ConcurrentDictionary<string, List<Message>> _mediaGroups = new();
        private ConcurrentDictionary<string, CancellationTokenSource> _mediaGroupTimers = new();
        private ConcurrentDictionary<long, ChequeoEnvios> _controlEnvios = new();

        private ManejoDeTelegram.Mensajes EnvioDeMensajes = new ManejoDeTelegram.Mensajes();

        public async Task Start(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var aportes = db.aportes.Where(x => x.estado == 1).OrderBy(x => x.fecha).ToArray();

            if (aportes.Length == 0)
            {
                await EnvioDeMensajes.MensajeDeTexto(bot, new CancellationToken(), mensaje.Message.Chat.Id, "No hay aportes para revisar.");
                return;
            }

            foreach (var a in aportes)
            {
                List<string> dirFisica = new List<string>();
                var multimedia = db.multimedia.Where(x => x.aporte_id == a.id).OrderBy(x => x.orden).ToArray();
                foreach (var m in multimedia)
                {
                    dirFisica.Add(m.path + "");
                }

                await EnvioDeMensajes.imagenesAgrupadas(bot, mensaje.Message.Chat.Id, dirFisica, a.mensaje);

                bool esAudio = false;
                if (dirFisica.Count == 1)
                {
                    if (Path.GetExtension(dirFisica[0].ToLower()) == ".ogg")
                    {
                        esAudio = true;
                    }
                }


                var filas = new List<List<InlineKeyboardButton>>();
                if (!esAudio)
                {
                    filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Si", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": true, \"tipoRechazo\": 0" + "}"),
                        InlineKeyboardButton.WithCallbackData("❌ No", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": false, \"tipoRechazo\": 9" + "}"),
                    });
                    filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("❌ No, hay rostros", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": false, \"tipoRechazo\": 1" + "}"),
                    });
                        filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("❌ No, menos de 3 fotos", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": false, \"tipoRechazo\": 2" + "}"),
                    });
                } 
                else
                {
                    filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Si", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": true, \"tipoRechazo\": 1" + "}"),
                        InlineKeyboardButton.WithCallbackData("❌ No", "{" + $"\"aporte_id\": {a.id}, \"aprobado\": false, \"tipoRechazo\": 3" + "}"),
                    });
                }
                
                var markup = new InlineKeyboardMarkup(filas);

                Dictionary<string, string> botones = new Dictionary<string, string>();
                await EnvioDeMensajes.EnviarTextoConBotones(bot, mensaje.Message.Chat.Id, "El posteo esta correcto?", botones, markup);
            }
        }

        public async Task RecibeCallback(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var res = JsonConvert.DeserializeObject<RecepcionAprobacion>(mensaje.CallbackQuery.Data);

            if (res != null)
            {
                var aporte = db.aportes.Where(x => x.id == res.aporte_id).FirstOrDefault();
                if (aporte != null)
                {
                    if (res.aprobado)
                    {
                        switch (res.tipoRechazo)
                        {
                            case 0:
                                aporte.estado = 2;
                                db.SaveChanges();
                                break;
                            case 1:
                                aporte.estado = 40;
                                db.SaveChanges();
                                break;
                        }
                    }
                    else
                    {
                        switch (res.tipoRechazo)
                        {
                            case 1:
                                aporte.estado = 31;
                                db.SaveChanges();
                                break;
                            case 2:
                                aporte.estado = 32;
                                db.SaveChanges();
                                break;
                            case 3:
                                aporte.estado = 41;
                                db.SaveChanges();
                                break;
                            default:
                                aporte.estado = 3;
                                db.SaveChanges();
                                break;
                        }
                    }
                }
            }

            await EnvioDeMensajes.MensajeDeTexto(bot, new CancellationToken(), mensaje.CallbackQuery.Message.Chat.Id, "✅✅  Listo ✅✅");
        }
    }
}
