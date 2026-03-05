using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telegram.Aportes.Modelos
{
    public class RecepcionAprobacion
    {
        public long aporte_id { get; set; }
        public bool aprobado { get; set; }
        public int tipoRechazo { get; set; }
    }
}
