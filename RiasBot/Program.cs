using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot
{
    public class Program
    {
        public static void Main(string[] args) => new RiasBot().StartAsync().GetAwaiter().GetResult();
    }
}
