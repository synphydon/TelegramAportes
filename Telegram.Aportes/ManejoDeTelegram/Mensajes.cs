using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManejoDeTelegram
{
    public class Mensajes
    {
        public async Task imagenesAgrupadas(ITelegramBotClient bot, long chatId, List<string> imgFisicas, string? texto)
        {
            try
            {
                var grupos = imgFisicas
                .Select((img, i) => new { img, i })
                .GroupBy(x => x.i / 10)   // crea grupos de 10
                .Select(g => g.Select(x => x.img).ToList())
                .ToList();

                foreach (var grupo in grupos)
                {
                    var media = new List<IAlbumInputMedia>();
                    int cont = 0;
                    foreach (var path in grupo)
                    {
                        var stream = File.OpenRead(path);
                        NewMethod(media, path, stream, cont, texto);
                        cont++;
                    }
                    await bot.SendMediaGroup(chatId, media);
                    await Task.Delay(250);
                }

                 void NewMethod(List<IAlbumInputMedia> media, string path, FileStream stream, int cont, string? texto)
                {
                    if (cont == 0)
                    {
                        if (Path.GetExtension(path).ToLower() == ".jpeg")
                        {
                            media.Add(new InputMediaPhoto(InputFile.FromStream(stream, Path.GetFileName(path)))
                            {
                                Caption = texto
                            });
                        }

                        if (Path.GetExtension(path).ToLower() == ".mp4")
                        {
                            media.Add(new InputMediaVideo(InputFile.FromStream(stream, Path.GetFileName(path)))
                            {
                                Caption = texto
                            });
                        }

                        if (Path.GetExtension(path).ToLower() == ".ogg")
                        {
                            media.Add(new InputMediaAudio(InputFile.FromStream(stream, Path.GetFileName(path))));
                        }

                    }
                    else
                    {
                        if (Path.GetExtension(path).ToLower() == ".jpeg")
                        {
                            media.Add(new InputMediaPhoto(InputFile.FromStream(stream, Path.GetFileName(path))));
                        }

                        if (Path.GetExtension(path).ToLower() == ".mp4")
                        {
                            media.Add(new InputMediaVideo(InputFile.FromStream(stream, Path.GetFileName(path))));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MensajeDeTexto(bot, default, chatId, "❌ Ocurrió un error al enviar las imágenes, por favor intenta con otro álbum.");
            }
        }

        public async Task imagenesSinComprimir(ITelegramBotClient bot, long chatId, List<string> imgFisicas)
        {
            try
            {
                foreach (var path in imgFisicas)
                {
                    using var stream = File.OpenRead(path);

                    await bot.SendDocument(
                        chatId,
                        InputFile.FromStream(stream, Path.GetFileName(path)),
                        caption: Path.GetFileName(path)
                    );
                    await Task.Delay(500);
                }
            }
            catch (Exception ex) { }
        }

        public async Task MensajeDeTexto(ITelegramBotClient bot, CancellationToken ct, long chatId, string texto, bool html = false)
        {
            if (html == false)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: texto,
                    cancellationToken: ct
                );
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: texto,    
                    parseMode: ParseMode.Html,
                    cancellationToken: ct
                );
            }
        }

        public async Task EnviarTextoConBotones(ITelegramBotClient bot, long chatId, string texto, Dictionary<string, string> botones, InlineKeyboardMarkup? botonesEspeciales = null, bool html = false)
        {
            var filas = new List<List<InlineKeyboardButton>>();

            if (botonesEspeciales == null)
            {
                foreach (var btn in botones)
                {
                    filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData(btn.Key, btn.Value)
                    });
                }
                var markup = new InlineKeyboardMarkup(filas);

                if (html == false)
                {
                    await bot.SendMessage(chatId, texto, replyMarkup: markup);
                }
                else
                {
                    await bot.SendMessage(chatId, texto, replyMarkup: markup, parseMode: ParseMode.Html);
                }
            } else
            {
                if (html == false)
                {
                    await bot.SendMessage(chatId, texto, replyMarkup: botonesEspeciales);
                }
                else
                {
                    await bot.SendMessage(chatId, texto, replyMarkup: botonesEspeciales, parseMode: ParseMode.Html);
                }
            }

                
        }
    }
}

