using System;
using System.Collections.Generic;

namespace Telegram.dts.Models;

public partial class multimedium
{
    public long aporte_id { get; set; }

    public int orden { get; set; }

    public string? path { get; set; }

    public int? tipo { get; set; }
}
