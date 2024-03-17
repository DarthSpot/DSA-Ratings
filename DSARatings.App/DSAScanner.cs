using System.Text.RegularExpressions;
using System.Web;
using DSARatings.Core;
using HtmlAgilityPack;

namespace DSARatings.App;

internal class DSAScanner : Scanner<DSAThreadIdentifier>
{
    private static string url = "https://dsaforum.de";
    private static string viewForum = "viewforum.php";
    private static string viewThread = "viewtopic.php";
    
    public DSAScanner(ScanOptions options) : base(options)
    {
    }
    
    public static string GetUrl(DSAThreadIdentifier id)
    {
        return $"{url}/{viewThread}?f={id.ForumId}&t={id.ThreadId}";
    }
    
    protected override List<DSAThreadIdentifier> GetThreads(ScanTarget target)
    {
        var res = new HashSet<int>();
        var regex = new Regex("\\./viewtopic\\.php\\?f=[0-9]+&amp;t=(?<topicID>[0-9]+)");
        var purl = url + "/" + viewForum + "?f=" + target.Id + "&start=";
        var counter = 0;
            
        var anyNew = true;
        var ids = new HashSet<int>();
        while (anyNew)
        {
            var curl = purl + counter;
            anyNew = PerformRetryAction(() =>
            {
                var tmpAnyNew = false;
                var doc = new HtmlDocument();
                doc.LoadHtml(Client.DownloadString(curl));
                var tls = doc.DocumentNode.Descendants("ul").Where(x => Equals(x.GetAttributeValue("class", ""), "topiclist topics")).ToList();

                var tl = tls.LastOrDefault();
                if (tl != null)
                {
                    var threads = tl.Descendants("a").Where(x => Equals(x.GetAttributeValue("class", ""), "topictitle")).Select(x =>
                    {
                        var val = x.GetAttributeValue("href", "");
                        var threadId = HttpUtility.ParseQueryString(new Uri(url + "/" + val).Query).Get("t");
                        if (!string.IsNullOrEmpty(threadId))
                        {
                            return int.Parse(threadId);
                        }

                        throw new Exception("Invalid URL");
                    }).ToList();
                    
                    foreach (var threadId in threads.Where(x => x > 0))
                    {
                        tmpAnyNew |= ids.Add(threadId);
                    }
                }

                return tmpAnyNew;
            });

            if (anyNew)
                counter += 25;
        }

        foreach (var id in ids)
            res.Add(id);
            

        return res.Select(x => new DSAThreadIdentifier(target.Id, x)).ToList();
    }

    protected override void PrintProgress(DSAThreadIdentifier item, int current, int total)
    {
        var percentage = (float)current * 100 / total;
        Console.Write($"\rScanne Forum [{item}] ({current} Threads): {(int)percentage}%    ");
    }

    protected override ThreadRating GetRating(DSAThreadIdentifier item)
    {
        var threadUrl = $"{url}/{viewThread}?f={item.ForumId}&t={item.ThreadId}";
        var doc = new HtmlDocument();
        doc.LoadHtml(Client.DownloadString(threadUrl));
        var poll = doc.DocumentNode.Descendants("fieldset")
            .SingleOrDefault(x => string.Equals(x.GetAttributeValue("class", ""), "polls"));
        if (poll == null) 
            return null;
        var rows = poll.Elements("dl").Where(x => x.GetAttributeValue("data-poll-option-id", 0) > 0).ToList();
        if (rows.Count != 5) 
            return null;
        var name = doc.DocumentNode.Descendants("h2")
            .Single(x => string.Equals(x.GetAttributeValue("class", ""), "topic-title")).InnerText;
        var res = new ThreadRating(item.ForumId, item.ThreadId, name);

        foreach (var row in rows)
        {
            var voteId = row.GetAttributeValue("data-poll-option-id", -1);
            var votes = int.Parse(row.Descendants("div").Single().InnerText);
            res.Add(voteId, votes);
        }

        var wiki = doc.DocumentNode.Descendants("div")
            .Where(x => string.Equals(x.GetAttributeValue("class", ""), "content"))
            .Skip(1)
            .First()
            .Elements("a")
            .FirstOrDefault(x => x.Attributes["href"].Value.Contains("wiki-aventurica.de"));

        if (wiki != null)
        {
            // If newline is in url, then something is wrong - just cut after first newline
            res.Wiki = wiki.Attributes["href"].Value.Split('\n').First().Replace("&amp;", "&");
        }

        return res;
    }

    
}