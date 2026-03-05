using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Telegram.Aportes.Modelos
{
    public class ChequeoEnvios
    {
        public ITelegramBotClient bot { get; set; }
        public long chatId { get; set; }
        public DateTime Hora { get; set; } = DateTime.Now;
        public bool MensajeEnviado { get; set; } = false;
        public int Cantidad { get; set; }
    }
}
