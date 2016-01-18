using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> test = new List<string> ();
            test.Add("X");
            test.Add("x");

            foreach(string s in test)
            {
                Console.WriteLine(s);
                
            }
            Console.Read();
        }
    }
}
