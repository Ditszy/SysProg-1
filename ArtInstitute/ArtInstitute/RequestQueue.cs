using System.Net;

namespace SistemskoProjekat;

public class RequestQueue
{
    private readonly Queue<HttpListenerContext> queue = new();
    private readonly object lockobj = new();

    public void Enqueue(HttpListenerContext context)
    {
        lock (lockobj)
        {
            queue.Enqueue(context);
            Monitor.Pulse(lockobj); // budi jednu nit koja ceka
        }
    }

    public void WakeUpAll()
    {
        lock (lockobj)
        {
            Monitor.PulseAll(lockobj); // budi sve niti radi gasenja
        }
    }

    public HttpListenerContext Dequeue(ref bool isRunning)
    {
        lock (lockobj)
        {
            while (queue.Count == 0 && isRunning)
            {
                Monitor.Wait(lockobj); // ceka signal ili gasenje
            }

            if (queue.Count > 0)
                return queue.Dequeue();
            
            return null; // vraca null ako se server gasi
        }
    }
}