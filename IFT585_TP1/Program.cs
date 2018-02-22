using System;

namespace IFT585_TP1
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isComplete = false;

            Pipeline pipeline = new Pipeline();
            pipeline.Run();

            while (!isComplete)
            {
                Console.WriteLine("Do you want to process an other file?\t(y/n)");
                string answer = Console.ReadLine();

                if (answer == "y")
                {
                    pipeline.Reset();
                    pipeline.Run();
                }
                else if (answer == "n")
                {
                    isComplete = true;
                }
                else
                {
                    Console.WriteLine("This command is not accepted...");
                }
            }
                
        }
    }
}
