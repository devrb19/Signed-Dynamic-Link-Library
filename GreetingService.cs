
namespace SignedDllPoc
{
    public class GreetingService
    {
        public string GetGreeting(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Developer";
            }

            return "Hello " + name +
                   "! This message comes from my custom signed DLL.";
        }
    }
}
