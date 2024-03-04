using System.Net;
using System.Text;

namespace DSARatings.Core;

public abstract class Scanner<TThreadIdentifier> where TThreadIdentifier : IThreadIdentifier
{
    public ScanOptions Options { get; }
    protected WebClient Client { get; } = new();
    
    protected Scanner(ScanOptions options)
    {
        Options = options;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        Client.Encoding = Encoding.UTF8;
        Client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.0.4) Gecko/20060508 Firefox/1.5.0.4");
        Client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
    }
    
    protected abstract List<TThreadIdentifier> GetThreads(ScanTarget target);

    protected abstract ThreadRating GetRating(TThreadIdentifier item);
    
    public List<ThreadRating> GetRatings()
    {
        var results = new List<ThreadRating>();
        Console.WriteLine("Preparing Scanner...");
        var threads = Options.Subforums.SelectMany(GetThreads).ToList();
        var counter = 0;
        foreach (var item in threads)
        {
            try
            {
                var rating = PerformRetryAction(() => GetRating(item));
                if (rating != null)
                    results.Add(rating);
            }
            catch
            {
                return null;
            }

            PrintProgress(item, counter++, threads.Count);
        }

        return results;
    }

    protected abstract void PrintProgress(TThreadIdentifier item, int current, int total);

    protected T PerformRetryAction<T>(Func<T> action)
    {
        var parse = 0;
        var finished = false;
        var res = default(T);
        while (!finished && parse < Options.MaxRetry)
        {
            try
            {
                res = action();
                finished = true;
            }
            catch
            {
                parse++;
                Thread.Sleep(1000);
            }
        }
        if (!finished)
            throw new Exception("Something went wrong...");

        return res;
    }
}

public interface IThreadIdentifier
{
    
}