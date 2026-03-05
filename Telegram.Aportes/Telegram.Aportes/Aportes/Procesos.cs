using ManejoDeTelegram;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;
using System.Text.RegularExpressions;
using Telegram.Aportes.Control;
using Telegram.Aportes.Modelos;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.dts.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Telegram.Aportes.Aportes
{
    public class Procesos
    {
        private ConcurrentDictionary<string, List<Message>> _mediaGroups = new();
        private ConcurrentDictionary<string, CancellationTokenSource> _mediaGroupTimers = new();
        private ConcurrentDictionary<long, ChequeoEnvios> _controlEnvios = new();
        //private ConcurrentDictionary<string, ChequeoEnvios> _controlEnvios = new();

        private PeriodicTimer _timer;
        public ITelegramBotClient _botEnvio;
        public long _chatIdCanal = 0;
        public string _PathMultimedia = "";

        private ManejoDeTelegram.Mensajes EnvioDeMensajes = new ManejoDeTelegram.Mensajes();


        public async Task Start(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            long chatId = 0;

            if (mensaje.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                chatId = mensaje.Message.From.Id;
            }
            else if (mensaje.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                chatId = mensaje.CallbackQuery.From.Id;
            }
            //chatId = mensaje.Message.From.Id;

            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();

            if (usuario != null)
            {
                if (UsuarioVerificado(chatId))
                {
                    usuario.ingresar_texto = 1;
                    usuario.ingresar_multimedia = 0;
                    db.SaveChanges();
                    await TextoDelMensaje(bot, ct, chatId);
                }
                else
                {
                    await PreguntaSoyMayor18(bot, ct, chatId);
                }
            }
            else
            {
                usuario nuevoUsuario = new usuario();
                nuevoUsuario.telegram_id = chatId;
                nuevoUsuario.consentimiento = 0;
                nuevoUsuario.sin_rostro = 0;
                nuevoUsuario.solo_mayor_edad = 0;
                nuevoUsuario.vendedora_contenido = 0;
                nuevoUsuario.ingresar_texto = 0;
                nuevoUsuario.ingresar_multimedia = 0;
                nuevoUsuario.fecha_ultimo_aporte = DateTime.Now;
                nuevoUsuario.vendedora_verificada = 0;
                db.usuarios.Add(nuevoUsuario);
                db.SaveChanges();

                await PreguntaSoyMayor18(bot, ct, chatId);
            }
        }

        public async Task RecibeCallback(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var res = JsonConvert.DeserializeObject<RecepcionConsentimiento>(mensaje.CallbackQuery.Data);
            switch (res.Consentimiento)
            {
                case 1:
                    if (UsuarioExistente(res.ChatId))
                    {
                        var usuario = db.usuarios.Where(x => x.telegram_id == res.ChatId).FirstOrDefault();
                        if (res.Respuesta == true)
                        {
                            usuario.consentimiento = 1;
                            db.SaveChanges();

                            await PreguntaSinRostro(bot, ct, res.ChatId);
                        }
                        else
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Para utilizar el bot debes tener el consentimiento explícito de tu pareja/novia/esposa. Si lo obtienes, puedes volver a iniciar el proceso de registro.");
                            EliminarUsuario(res.ChatId);
                        }
                    }
                    else
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Debes comenzar con el proceso de aporte nuevamente.");
                    }
                    break;
                case 2:
                    if (UsuarioExistente(res.ChatId))
                    {
                        var usuario = db.usuarios.Where(x => x.telegram_id == res.ChatId).FirstOrDefault();
                        if (res.Respuesta == true)
                        {
                            usuario.sin_rostro = 1;
                            db.SaveChanges();

                            await PreguntaSoloMayorEdad(bot, ct, res.ChatId);
                        }
                        else
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "No esta premitido publicar rostros. Debes comenzar con el proceso de aporte nuevamente.");
                            EliminarUsuario(res.ChatId);
                        }
                    }
                    else
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Debes comenzar con el proceso de aporte nuevamente.");
                    }
                    break;

                case 3:
                    if (UsuarioExistente(res.ChatId))
                    {
                        var usuario = db.usuarios.Where(x => x.telegram_id == res.ChatId).FirstOrDefault();
                        if (res.Respuesta == true)
                        {
                            usuario.solo_mayor_edad = 1;
                            usuario.ingresar_texto = 1;
                            db.SaveChanges();

                            var link = await GeneraLinkCanal(bot);
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, $"Aca tenes el link de acceso al canal: {link.InviteLink}");
                            await TextoDelMensaje(bot, ct, res.ChatId);
                        }
                        else
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Esta prohibido por ley el envio fotos o videos de menores de edad. Debes comenzar con el proceso de aporte nuevamente.");
                            EliminarUsuario(res.ChatId);
                        }
                    }
                    else
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Debes comenzar con el proceso de aporte nuevamente.");
                    }
                    break;
                case 4:
                    if (UsuarioExistente(res.ChatId))
                    {
                        var usuario = db.usuarios.Where(x => x.telegram_id == res.ChatId).FirstOrDefault();
                        if (res.Respuesta == true)
                        {
                            usuario.soy_mayor_10 = 1;
                            db.SaveChanges();

                            await PreguntaSoyVendedora(bot, ct, res.ChatId);
                        }
                        else
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Esta prohibido por ley el envio de fotos o videos a menores de edad.");
                            EliminarUsuario(res.ChatId);
                        }
                        break;
                    }
                    else
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Debes comenzar con el proceso de aporte nuevamente.");
                    }
                    break;
                case 5:
                    if (UsuarioExistente(res.ChatId))
                    {
                        var usuario = db.usuarios.Where(x => x.telegram_id == res.ChatId).FirstOrDefault();
                        if (res.Respuesta == true)
                        {
                            usuario.vendedora_contenido = 1;
                            usuario.sin_rostro = 1;
                            usuario.consentimiento = 1;
                            usuario.sin_rostro = 1;
                            usuario.solo_mayor_edad = 1;
                            db.SaveChanges();

                            var link = await GeneraLinkCanal(bot);
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, $"Aca tenes el link de acceso al canal: {link.InviteLink}");
                            await TextoDelMensaje(bot, ct, res.ChatId);

                            //await PreguntaConsentimiento(bot, ct, res.ChatId);
                        }
                        else
                        {
                            usuario.vendedora_contenido = 0;
                            db.SaveChanges();
                            await PreguntaConsentimiento(bot, ct, res.ChatId);
                        }
                        break;
                    }
                    else
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, res.ChatId, "Debes comenzar con el proceso de aporte nuevamente.");
                    }
                    break;
                case 999:
                    await Start(bot, ct, mensaje);
                    return;
                default:
                    break;
            }
        }

        public async Task RecibeTexto(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            long chatId = mensaje.Message.From.Id;
            if (UsuarioParaIngresarTexto(chatId))
            {
                var palabras = mensaje.Message.Text.Split(' ');
                if (palabras.Length <= 10)
                {
                    await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Tu descripción debe tener al menos 10 palabras. Por favor, intenta nuevamente.");
                    return;
                }

                if (ContieneUrl(mensaje.Message.Text))
                {
                    await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Tu descripción no puede contener URLs. Por favor, intenta nuevamente.");
                    return;
                }

                NuevoAporte(chatId, mensaje);

                //Dictionary<string, string> botones = new Dictionary<string, string>();
                //botones.Add("Volver a escribir la descripción", "{{\"ChatId\": {chatId}, \"Consentimiento\": 999, \"Respuesta\": true }}");
                
                


                //await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, "Ahora sí, podes enviar mínimo 3 imágenes o más. (SOLO imagenes)",botones);
                await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Ahora sí, podes enviar mínimo 3 imágenes o más. (SOLO imagenes)");
            } else
            {
                if (VendedoraParaIngresarTexto(chatId))
                {
                    var palabras = mensaje.Message.Text.Split(' ');

                    if (palabras.Length <= 10)
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Tu descripción debe tener al menos 10 palabras. Por favor, intenta nuevamente.");
                        return;
                    }
                    NuevoAporte(chatId, mensaje, true);

                    /*Dictionary<string, string> botones = new Dictionary<string, string>();
                    botones.Add("Volver a escribir la descripción", "{{\"ChatId\": {chatId}, \"Consentimiento\": 999, \"Respuesta\": true }}");

                    var filas = new List<List<InlineKeyboardButton>>();
                    filas.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("Volver a escribir la descripción", "{{\"ChatId\": {chatId}, \"Consentimiento\": 999, \"Respuesta\": true }}")
                    });

                    await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, "Ahora sí, podes enviar mínimo 2 imágenes o más. (SOLO imagenes)", botones, filas, true);*/

                    await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Ahora sí, podes enviar mínimo 2 imágenes o más. (SOLO imagenes)");
                }
                else
                {
                    await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Recuerda que para hacer un aporte debes ir al menú y luego start.");
                }
            }

        }

        public async Task RecibeAudio(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            NuevoAporte(mensaje.Message.Chat.Id,mensaje);
         
            string path = await DescargarAudio(bot, mensaje.Message);
            await GuardaMultimedia(mensaje.Message.Chat.Id, path, true);

            string tx = "🎧 ¡Audio recibido!\r\nLo revisaremos en breve y te avisaremos por acá cuando esté aprobado.";
            await EnvioDeMensajes.MensajeDeTexto(bot, CancellationToken.None, mensaje.Message.Chat.Id, tx);
        }

        public void NuevoAporte(long chatId, Update mensaje, bool esVendedora = false)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();

            var aportes = db.aportes.Where(x => x.telegram_id == chatId && x.estado == 0).ToArray();
            foreach (var aporteExistente in aportes)
            {
                db.aportes.Remove(aporteExistente);
            }
            db.SaveChanges();

            aporte nuevoAporte = new aporte();
            nuevoAporte.usuario_id = usuario.id;
            nuevoAporte.telegram_id = chatId;
            if (esVendedora)
            {
                string tx = "🔹✨ Perfil Verificado ✨🔹\r\n\r\n";
                tx += mensaje.Message.Text + "\r\n\r\n";
                tx += "#VendedoraContenido";
                nuevoAporte.mensaje = tx ;
            }
            else
            {
                nuevoAporte.mensaje = mensaje.Message.Text;
            }
            nuevoAporte.fecha = DateTime.Now;
            nuevoAporte.estado = 0;
            db.aportes.Add(nuevoAporte);

            usuario.ingresar_texto = 0;
            usuario.ingresar_multimedia = 1;

            db.SaveChanges();
        }

        public async Task RecibeMultimedia(ITelegramBotClient bot, CancellationToken ct, Update mensaje)
        {
            long chatId = mensaje.Message.From.Id;
            if (UsuarioParaIngresarMultimedia(chatId) || VendedoraParaIngresarMultimedia(chatId))
            {
                if (mensaje.Message.MediaGroupId != null)
                {
                    ProcesarMediaGroup(bot, mensaje.Message);
                }
                else
                {
                    if (mensaje.Message.Photo != null)
                    {
                        var mensajeEnviado = _controlEnvios.GetOrAdd(mensaje.Message.Chat.Id, _ => new ChequeoEnvios());
                        lock (mensajeEnviado)
                        {
                            mensajeEnviado.bot = bot;
                            mensajeEnviado.MensajeEnviado = true;
                            mensajeEnviado.Hora = DateTime.Now;
                            mensajeEnviado.chatId = mensaje.Message.Chat.Id;
                        }
                        string pathFoto = await DescargarFoto(bot, mensaje.Message, "", 1);
                        await GuardaMultimedia(mensaje.Message.From.Id, pathFoto);
                        await EnvioDeMensajes.MensajeDeTexto(bot, CancellationToken.None, mensaje.Message.Chat.Id, "¡Gracias por tu aporte!\r\nTu contenido será revisado y publicado en nuestras redes sociales en breve.");
                    }

                    if (mensaje.Message.Video != null)
                    {
                        var mensajeEnviado = _controlEnvios.GetOrAdd(mensaje.Message.Chat.Id, _ => new ChequeoEnvios());
                        lock (mensajeEnviado)
                        {
                            mensajeEnviado.bot = bot;
                            mensajeEnviado.MensajeEnviado = true;
                            mensajeEnviado.Hora = DateTime.Now;
                            mensajeEnviado.chatId = mensaje.Message.Chat.Id;
                        }
                        string pathVideo = await DescargarVideo(bot, mensaje.Message);
                        await GuardaMultimedia(mensaje.Message.From.Id, pathVideo);
                        await EnvioDeMensajes.MensajeDeTexto(bot, CancellationToken.None, mensaje.Message.Chat.Id, "¡Gracias por tu aporte!\r\nTu contenido será revisado y publicado en nuestras redes sociales en breve.");
                    }
                }
            }
        }

        public async Task TextoDelMensaje(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();

            if (usuario != null)
            {
                if (UsuarioDisponibleParaHacerAporte(chatId))
                {
                    await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, General.Mensaje("texto_aporte"));
                }
                else
                {
                    if (VendedoraDisponibleParaHacerAporte(chatId))
                    {
                        await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, General.Mensaje("texto_aporte"));
                    }
                    else
                    {
                        if (VendedoraSinVerificar(chatId))
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, General.Mensaje("audio_verificacion"));
                        }
                        else
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "Recuerda que para hacer un aporte debes ir al menú y luego start.");
                            EliminarUsuario(chatId);
                        }
                    }
                }
            }
            else
            {
                await EnvioDeMensajes.MensajeDeTexto(bot, ct, chatId, "No estás registrado para hacer un aporte debes ir al menú y luego start.");
            }

        }

        public bool UsuarioExistente(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool UsuarioVerificado(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool UsuarioDisponibleParaHacerAporte(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 0 && diferencia.TotalDays <= 5)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool VendedoraDisponibleParaHacerAporte(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 1 && usuario.vendedora_verificada == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public bool VendedoraSinVerificar(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 1 && usuario.vendedora_verificada == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool UsuarioParaIngresarTexto(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 0 && usuario.ingresar_texto == 1 && diferencia.TotalDays <= 5)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool VendedoraParaIngresarTexto(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 1 && usuario.ingresar_texto == 1 && usuario.vendedora_verificada == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool UsuarioParaIngresarMultimedia(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 0 && usuario.ingresar_multimedia == 1 && diferencia.TotalDays <= 5)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool VendedoraParaIngresarMultimedia(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                TimeSpan diferencia = DateTime.Now.Subtract(usuario.fecha_ultimo_aporte);
                if (usuario.soy_mayor_10 == 1 && usuario.consentimiento == 1 && usuario.sin_rostro == 1 && usuario.solo_mayor_edad == 1 && usuario.vendedora_contenido == 1 && usuario.ingresar_multimedia == 1 && usuario.vendedora_verificada == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void CierraAporte(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var aporte = db.aportes.Where(x => x.telegram_id == chatId && x.estado == 0).FirstOrDefault();
            if (aporte != null)
            {
                aporte.estado = 1;
                db.SaveChanges();
            }
        }

        public void RestableceUsuario(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            { usuario.ingresar_texto = 0;
                usuario.ingresar_multimedia = 0;
                db.SaveChanges();
            }
        }

        public void EliminarUsuario(long chatId)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var usuario = db.usuarios.Where(x => x.telegram_id == chatId).FirstOrDefault();
            if (usuario != null)
            {
                db.usuarios.Remove(usuario);
                db.SaveChanges();
            }
        }

        #region Preguntas

        public async Task PreguntaSoyMayor18(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            Dictionary<string, string> botones = new Dictionary<string, string>();
            botones.Add("✅ Sí", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 4, \"Respuesta\": true }");
            botones.Add("❌ No", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 4, \"Respuesta\": false }");

            await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, General.Mensaje("pregunta_mayor_18"), botones,null,true);
        }

        public async Task PreguntaSoyVendedora(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            Dictionary<string, string> botones = new Dictionary<string, string>();
            var linkCanal = await GeneraLinkCanal(bot);
            
            var filas = new List<List<InlineKeyboardButton>>();
            filas.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Soy vendedora de contenido", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 5, \"Respuesta\": true }"),
                });
            filas.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("Solo quiero hacer aportes", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 5, \"Respuesta\": false }"),
                });
            filas.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithUrl("Quiero solo ir al canal", linkCanal.InviteLink)
                });

            await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, General.Mensaje("pregunta_aporte_vendedora"), botones, filas);
        }

        public async Task PreguntaConsentimiento(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            Dictionary<string, string> botones = new Dictionary<string, string>();
            botones.Add("✅ Sí, tengo consentimiento.", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 1, \"Respuesta\": true }");
            botones.Add("❌ No lo tengo", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 1, \"Respuesta\": false }");

            await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, General.Mensaje("pregunta_consentimiento"), botones);
        }

        public async Task PreguntaSinRostro(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            Dictionary<string, string> botones = new Dictionary<string, string>();
            botones.Add("✅ Entiendo, no mandare fotos ni videos con rostro.", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 2, \"Respuesta\": true }");
            botones.Add("❌ Salir", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 2, \"Respuesta\": false }");
            await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, General.Mensaje("pregunta_sin_rostro"), botones);
        }

        public async Task PreguntaSoloMayorEdad(ITelegramBotClient bot, CancellationToken ct, long chatId)
        {
            Dictionary<string, string> botones = new Dictionary<string, string>();
            botones.Add("✅ Sí, todas las personas son mayores de edad", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 3, \"Respuesta\": true }");
            botones.Add("❌ Salir", "{\"ChatId\": " + Convert.ToString(chatId) + ", \"Consentimiento\": 3, \"Respuesta\": false }");
            await EnvioDeMensajes.EnviarTextoConBotones(bot, chatId, General.Mensaje("pregunta_solo_mayor_edad"), botones);
        }

        #endregion  

        void ProcesarMediaGroup(ITelegramBotClient bot, Message mensaje)
        {
            var groupId = mensaje.MediaGroupId;

            var lista = _mediaGroups.GetOrAdd(groupId, _ => new List<Message>());

            lock (lista)
            {
                lista.Add(mensaje);
            }

            // Si ya hay un timer, lo cancelamos
            if (_mediaGroupTimers.TryRemove(groupId, out var oldCts))
            {
                oldCts.Cancel();
            }

            // Creamos uno nuevo
            var cts = new CancellationTokenSource();
            _mediaGroupTimers[groupId] = cts;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(800, cts.Token);

                    if (_mediaGroups.TryRemove(groupId, out var mensajes))
                    {
                        int index = 1;
                        var mensajeEnviado = _controlEnvios.GetOrAdd(mensaje.Chat.Id, _ => new ChequeoEnvios());

                        foreach (var m in mensajes)
                        {
                            mensajeEnviado = _controlEnvios.GetOrAdd(mensaje.Chat.Id, _ => new ChequeoEnvios());
                            lock (mensajeEnviado)
                            {
                                mensajeEnviado.chatId = mensaje.Chat.Id;
                                mensajeEnviado.Cantidad++;
                            }
                            if (mensajeEnviado.Cantidad < 11)
                            {
                                if (m.Photo != null)
                                {
                                    var path = await DescargarFoto(bot, m, groupId, index++);
                                    await GuardaMultimedia(m.From.Id, path);
                                }

                                if (m.Video != null)
                                {
                                    var path = await DescargarVideo(bot, m);
                                    await GuardaMultimedia(m.From.Id, path);
                                }
                            }
                        }

                        // ✅ UNA SOLA VEZ
                        if (mensajeEnviado.MensajeEnviado == false)
                        {
                            await EnvioDeMensajes.MensajeDeTexto(bot, CancellationToken.None, mensaje.Chat.Id, General.Mensaje("aviso_revision_contenido"));

                            lock (mensajeEnviado)
                            {
                                mensajeEnviado.bot = bot;
                                mensajeEnviado.MensajeEnviado = true;
                                mensajeEnviado.Hora = DateTime.Now;
                                mensajeEnviado.chatId = mensaje.Chat.Id;
                            }
                        }
                        else
                        {
                            lock (mensajeEnviado)
                            {
                                mensajeEnviado.MensajeEnviado = true;
                                mensajeEnviado.Hora = DateTime.Now;
                                mensajeEnviado.chatId = mensaje.Chat.Id;
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {

                }
                finally
                {
                    _mediaGroupTimers.TryRemove(groupId, out _);
                }
            });
        }

        public async Task GuardaMultimedia(long chatId, string path, bool cerrar = false)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var aportes = db.aportes.Where(x => x.telegram_id == chatId && x.estado == 0).FirstOrDefault();

            if (aportes != null)
            {
                multimedium nuevoMultimedia = new multimedium();
                nuevoMultimedia.aporte_id = aportes.id;
                int orden = db.multimedia.Where(x => x.aporte_id == aportes.id).Max(x => (int?)x.orden) ?? 0;
                nuevoMultimedia.orden = orden + 1;
                nuevoMultimedia.path = path;
                nuevoMultimedia.tipo = 1;

                if (cerrar)
                {
                    aportes.estado = 1;
                }

                db.multimedia.Add(nuevoMultimedia);
                db.SaveChanges();
            }
        }

        async Task<string> DescargarFoto(ITelegramBotClient bot, Message msg, string groupId, int index)
        {
            long chatId = msg.Chat.Id;
            var photo = msg.Photo.Last(); // 👈 solo la mejor calidad

            var file = await bot.GetFile(photo.FileId);

            var fecha = DateTime.Now;
            string nombreImagen = $"IMG_{fecha.Year}{fecha.Month.ToString("D2")}{fecha.Day.ToString("D2")}_{fecha.Hour.ToString("D2")}{fecha.Minute.ToString("D2")}{fecha.Second.ToString("D2")}{fecha.Millisecond.ToString("D3")}.jpeg";
            var path = $"{_PathMultimedia}\\{chatId}";

            Directory.CreateDirectory(path);

            path += $"\\{nombreImagen}";

            await using var stream = File.OpenWrite(path);
            await bot.DownloadFile(file.FilePath, stream);

            return path;
        }

        public async Task<string> DescargarVideo(ITelegramBotClient bot, Message mensaje)
        {
            long chatId = mensaje.Chat.Id;
            var video = mensaje.Video;

            // Obtener info del archivo en Telegram
            var file = await bot.GetFile(video.FileId);

            // Crear nombre único
            var fecha = DateTime.Now;
            string nombreImagen = $"IMG_{fecha.Year}{fecha.Month.ToString("D2")}{fecha.Day.ToString("D2")}_{fecha.Hour.ToString("D2")}{fecha.Minute.ToString("D2")}{fecha.Second.ToString("D2")}{fecha.Millisecond.ToString("D3")}.mp4";
            var path = $"{_PathMultimedia}\\{chatId}";

            Directory.CreateDirectory(path);

            path += $"\\{nombreImagen}";

            // Descargar
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await bot.DownloadFile(file.FilePath, stream);
            }

            return path;
        }

        private async Task<string> DescargarAudio(ITelegramBotClient bot, Message mensaje)
        {
            long chatId = mensaje.Chat.Id;
            var file = await bot.GetFile(mensaje.Voice.FileId);

            // Crear nombre único
            var fecha = DateTime.Now;
            string nombreImagen = $"IMG_{fecha.Year}{fecha.Month.ToString("D2")}{fecha.Day.ToString("D2")}_{fecha.Hour.ToString("D2")}{fecha.Minute.ToString("D2")}{fecha.Second.ToString("D2")}{fecha.Millisecond.ToString("D3")}.ogg";
            var path = $"{_PathMultimedia}\\{chatId}";

            Directory.CreateDirectory(path);

            path += $"\\{nombreImagen}";

            using var stream = new FileStream(path, FileMode.Create);
            await bot.DownloadFile(file.FilePath, stream);

            return path;
        }

        public async Task EnviaLasFotos(long Aportes_id)
        {
            ApplicationDbContext db = new ApplicationDbContext();

            var aportes = db.aportes.Where(x => x.estado == 4).ToArray();
            foreach (var a in aportes)
            {
                List<string> dirFisica = new List<string>();
                var multimedia = db.multimedia.Where(x => x.aporte_id == a.id).OrderBy(x => x.orden).ToArray();
                foreach (var m in multimedia)
                {
                    dirFisica.Add(m.path + "");
                }

                a.estado = 5;
                db.SaveChanges();

                await EnvioDeMensajes.imagenesAgrupadas(_botEnvio, _chatIdCanal, dirFisica, a.mensaje);
            }
        }

        #region Control cada 2 segundos
        public void ControlarEnviosAbiertos()
        {
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            _ = EjecutarControlEnviosAbiertos();
        }

        private async Task EjecutarControlEnviosAbiertos()
        {
            while (await _timer.WaitForNextTickAsync())
            {
                LimpiaEnviosAbiertos();
                ChequeaAprobaciones();
                ChequeaEstadosEnCero();
            }
        }

        private void LimpiaEnviosAbiertos()
        {
            foreach (var item in _controlEnvios)
            {
                if ((DateTime.Now - item.Value.Hora).TotalSeconds > 5)
                {
                    var chat = item.Value;
                    EnvioDeMensajes.MensajeDeTexto(chat.bot, CancellationToken.None, item.Value.chatId, "📩 Cuando tu aporte sea publicado, te enviaremos una confirmación por este medio.");
                    CierraAporte(item.Value.chatId);
                    RestableceUsuario(item.Value.chatId);
                    _controlEnvios.TryRemove(item.Key, out _);
                }
            }
        }

        private void ChequeaEstadosEnCero()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var aportes = db.aportes.Where(x => x.estado == 0).ToArray();
            
            foreach (var aporte in aportes)
            {
                DateTime fechaHasta = DateTime.Now.AddMinutes(-10);
                if (DateTime.Now - aporte.fecha > TimeSpan.FromMinutes(5))
                {
                    string tx = "Algo salió mal con tu mensaje.\r\nHace click sobre \"Menú\" y luego sobre \"Comenza con el aporte\".";
                    EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, (long)aporte.telegram_id, General.Mensaje("algo_salio_mal"));
                    aporte.estado = 5;
                    db.SaveChanges();
                }
            }
        }

        public bool ContieneUrl(string texto)
        {
            string patron = @"\b([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(\/[^\s]*)?\b";
            return Regex.IsMatch(texto, patron, RegexOptions.IgnoreCase);
        }

        public void ChequeaAprobaciones()
        {
            if (_botEnvio == null || _chatIdCanal == 0)
            {
                return;
            }

            ApplicationDbContext db = new ApplicationDbContext();
            var aportes = db.aportes.Where(x => x.estado == 2 || x.estado == 3 || x.estado == 31 || x.estado == 32 || x.estado == 4 || x.estado == 40 || x.estado == 41).ToArray();
            foreach (var aporte in aportes)
            {
                string tx = "";
                switch (aporte.estado)
                {
                    case 2: // Abrobado
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, General.Mensaje("aporte_aprobado"));
                        aporte.estado = 4;
                        db.SaveChanges();
                        return;
                    case 3: // Rechazada
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, General.Mensaje("aporte_rechazado"));
                        aporte.estado = 5;
                        db.SaveChanges();
                        break;
                    case 31: // Rechazada
                        tx = "⚠️ Aviso ⚠️\r\n" +
                             "Tu aporte fue rechazado porque no cumple con los requisitos (las fotos tienen rostros).\r\nPodés corregirlo y volver a intentarlo 😊\r\n" +
                             "Recuerda que para hacer otro aporte debes ir al menú y luego start";
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, tx);
                        aporte.estado = 5;
                        db.SaveChanges();
                        break;
                    case 32: // Rechazada
                        tx = "⚠️ Aviso ⚠️\r\n" +
                             "Tu aporte fue rechazado porque no cumple con los requisitos (post con menos de 3 fotos).\r\nPodés corregirlo y volver a intentarlo 😊\r\n" +
                             "Recuerda que para hacer otro aporte debes ir al menú y luego start";
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, tx);
                        aporte.estado = 5;
                        db.SaveChanges();
                        break;
                    case 4: // Publicando
                        EnviaLasFotos(aporte.id);
                        break;
                    case 40:
                        tx = "🎉 ¡Tu audio de verificación fue aprobado!\r\n🙌 Felicitaciones, ahora eres una vendedora de contenido verificada.\r\n Toca Menu y luego Comenzar con el aporte.";
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, tx);

                        var usuario = db.usuarios.Where(x => x.telegram_id == aporte.telegram_id).FirstOrDefault();
                        usuario.vendedora_verificada = 1;

                        aporte.estado = 5;
                        db.SaveChanges();
                        break;
                    case 41:
                        tx = "⚠️ No pudimos validar tu audio de verificación.\r\nPor favor, envía nuevamente el audio diciendo la frase indicada de forma clara.";
                        EnvioDeMensajes.MensajeDeTexto(_botEnvio, CancellationToken.None, aporte.telegram_id.Value, tx);
                        
                        aporte.estado = 5;
                        db.SaveChanges();
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        private string BuscaTexto(string clave)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var texto = db.mensajes_sistemas.Where(x => x.clave == clave).Select(x => x.texto).FirstOrDefault();

            if (texto != null)
            {
                return texto;
            }
            else
            {
                return "";
            }
        }

        private async Task<ChatInviteLink> GeneraLinkCanal(ITelegramBotClient bot)
        {
            ChatInviteLink invitacion = await bot.CreateChatInviteLink(_chatIdCanal, expireDate: DateTime.UtcNow.AddHours(1), memberLimit: 1);
            return invitacion;
        }
    }
}
