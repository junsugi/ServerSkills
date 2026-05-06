using System.Net;
using ServerCore;
using ServerSkills.Login;

namespace ServerSkills;

class Program
{
    public static Listener listner;
    static void Main(string[] args)
    {
        IAccountRepository accountRepository = new FakeAccountRepository();
        IAccountService accountService = new AccountService(accountRepository);
        
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);
        
        listner = new Listener();
        listner.Init(endPoint, () => new ClientSession(accountService));

        Console.WriteLine($"Listening on {endPoint.Address}:{endPoint.Port}");
        while (true)
        {
            
        }
    }
}