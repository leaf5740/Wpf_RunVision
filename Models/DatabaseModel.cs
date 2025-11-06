using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wpf_RunVision.Models
{
    public class DatabaseModel
    {
        public string? Ip { set; get; }
        public string? Port { set; get; }
        public string? Password { set; get; }
        public string? LibraryName { set; get; }
        public string? CodeTableName { set; get; }
        public string? DataTableName { set; get; }

    }
}
