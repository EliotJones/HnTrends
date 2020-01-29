namespace HnTrends.ConsoleIndexer
{
    using System;
    using System.Diagnostics;

    internal class MyConsoleListener : TraceListener
    {
        public static readonly MyConsoleListener Instance = new MyConsoleListener();

        private MyConsoleListener()
        {
        }

        public override void Write(string message)
        {
            Console.WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}