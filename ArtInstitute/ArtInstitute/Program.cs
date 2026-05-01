using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SistemskoProjekat;

class Program
{
    private static readonly ArtCache cache = new ArtCache(10); 
    private static readonly RequestQueue requestQueue = new RequestQueue();
    private static readonly HttpClient httpClient = new HttpClient();
    private static readonly object consoleLock = new object();

    static void Main(string[] args)
    {
        const int numThreads = 5;
        httpClient.DefaultRequestHeaders.Add("User-Agent", "SistemskoProjekat");
        for(int i = 0; i < numThreads; i++)
        {
            Thread worker = new Thread(WorkerLoop);
            worker.IsBackground = true;
            worker.Start();
        }

        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        
        try {
            listener.Start();
            Console.WriteLine("Server pokrenut na http://localhost:8080/");

            while (true)
            {
                var context = listener.GetContext();
                requestQueue.Enqueue(context);
            }
        }
        catch (Exception ex) {
            Log($"Greška: {ex.Message}");
        }
    }

    private static void WorkerLoop()
    {
        while (true)
        {
            var context = requestQueue.Dequeue();
            ProcessRequest(context);
        }
    }

    private static void SendResponse(HttpListenerContext context, string body, HttpStatusCode status)
    {
        try {
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            using var writer = new StreamWriter(context.Response.OutputStream);
            writer.Write(body);
        }
        catch (Exception ex) {
            Log($"Greška pri slanju: {ex.Message}");
        }
    }

    private static async Task<string> FetchFromApi(string query)
    {
        string url = $"https://api.artic.edu/api/v1/artworks/search?q={query}";
        try {
            return await httpClient.GetStringAsync(url);
        }
        catch {
            return "{\"error\": \"Problem sa Art Institute API\"}";
        }
    }

    private static void ProcessRequest(HttpListenerContext context)
    {
        string query = context.Request.QueryString["q"];
        if (string.IsNullOrEmpty(query))
        {
            SendResponse(context,"{\"error\": \"Nedostaje q parametar\"}",HttpStatusCode.BadRequest);
            return;
        }

        if(!cache.TryGet(query,out string result))
        {
            lock (string.Intern(query))
            {
                if (!cache.TryGet(query, out result))
                {
                    Log($"API poziv za: {query}");
                    result = FetchFromApi(query).GetAwaiter().GetResult();
                    cache.Add(query, result);
                }
            }
        }
        else
        {
            Log($"Rezultat iz keša za: {query}");
        }

        SendResponse(context,result,HttpStatusCode.OK);
    }

    public static void Log(string message)
    {
        lock (consoleLock) {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Nit {Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}