using MauiApp1.Interfaces;

namespace MauiApp1.Scripts
{
    public class TestScript : IScript
    {
        public string ScriptName => "Test Script";
        public string Description => "A simple test script to verify script discovery is working";
        public string Category => "Testing";
        public string Author => "";

        public void Execute()
        {
            Console.WriteLine("Hello from Test Script!");
            Console.WriteLine("This is a simple test to verify script discovery works.");
        }
    }
}
