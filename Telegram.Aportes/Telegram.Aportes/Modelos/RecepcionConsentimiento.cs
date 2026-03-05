using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Aportes.Modelos
{
    public class RecepcionConsentimiento
    {
        public long ChatId { get; set; }
        public int Consentimiento { get; set; }
        public bool Respuesta { get; set; } = false;
    }
}
