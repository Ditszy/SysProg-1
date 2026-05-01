using System.Collections.Generic;

namespace SistemskoProjekat;

public class ArtCache
{
    private readonly int maxSize;
    private readonly Dictionary<string,string> data = new();
    private readonly LinkedList<string> lruList = new();
    private readonly object lockObj = new();

    public ArtCache(int maxSize)
    {
        this.maxSize = maxSize;
    }

    public bool TryGet(string key, out string value)
    {
        lock (lockObj)
        {
            if(data.TryGetValue(key,out value))
            {
                //Pomeramo na pocetak liste jer je recently used
                lruList.Remove(key); 
                lruList.AddFirst(key);
                return true;
            }
            return false;
        }
    }

    public void Add(string key, string value)
    {
        lock (lockObj)
        {
            if (!data.ContainsKey(key))
            {
                if(data.Count >= maxSize)
                {
                    //Izbacujemo najstarijeg tj poslednjeg u listi
                    var najstariji = lruList.Last.Value; 
                    data.Remove(najstariji);
                    lruList.RemoveLast();
                }
                data.Add(key,value);
            }
            else
            {
                lruList.Remove(key);
            }
            lruList.AddFirst(key);
        }
    }
}