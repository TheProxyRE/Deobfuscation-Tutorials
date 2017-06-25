using System;

namespace TestAppStrings
{
    class Program
    {
        static void Main(string[] args)
        {
            var password = Console.ReadLine();
            if (password == "Tutorial 1")
                Console.WriteLine("Well Done");
            else
                Console.WriteLine("Wrong Password");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
