using System.Collections.Generic;
using System.Net;
using System.Threading;

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
            Monitor.Pulse(lockobj);
        }
    }

    public HttpListenerContext Dequeue()
    {
        lock (lockobj)
        {
            while(queue.Count == 0)
            {
                Monitor.Wait(lockobj);
            }

            return queue.Dequeue();
        }
    }
}