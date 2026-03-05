using System;
using System.Collections.Generic;

namespace Telegram.dts.Models;

public partial class aporte
{
    public long id { get; set; }

    public int? usuario_id { get; set; }

    public long? telegram_id { get; set; }

    public string? mensaje { get; set; }

    public DateTime? fecha { get; set; }

    public int? estado { get; set; }
}
