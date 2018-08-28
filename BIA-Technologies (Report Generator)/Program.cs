using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Data.Entity;
using System.Data.Linq;
using System.Configuration;
using System.Data.SQLite.Linq;

namespace BIA_Technologies__Report_Generator_
{
    class Program
    {
        static string Path = Directory.GetCurrentDirectory() + @"\reports";

        static void Main(string[] args)
        {
            
            while (true)
            {
                CommandHandler.Execute(Console.ReadLine());
            }
            
        }
    }
}
