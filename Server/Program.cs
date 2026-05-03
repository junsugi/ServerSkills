using System.Net;
using ServerCore;

namespace ServerSkills;

class Program
{
    public static Listener listner;
    static void Main(string[] args)
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 5555);
        
        listner = new Listener();
        listner.Init(endPoint, () => new Session());
    }
}