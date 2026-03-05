using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Aportes
{
    public class Configuracion
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string BotToken { get; set; } = string.Empty;
        public string BotControl { get; set; } = string.Empty;  
        public long ChatIdCanal { get; set; } = 0;
        public string PathMultimedia { get; set; } = string.Empty;  
        public long ChatIdAdmin { get; set; } = 0;
    }
}
