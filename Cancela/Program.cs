using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cancela
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            ConfigureService.Configure();
        }

    }
}
