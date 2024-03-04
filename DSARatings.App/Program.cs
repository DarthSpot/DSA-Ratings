using System.Reflection;
using System.Text;
using System.Web;
using DSARatings.Core;
using Microsoft.Extensions.Configuration;

namespace DSARatings.App;

class Program
{
    static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false);
        var config = builder.Build();
        var options = config.GetSection("ScanOptions").Get<ScanOptions>();

        var scanner = new DSAScanner(options);
        var result = scanner.GetRatings();
            
        OutputToCSV(result, options);
            
        Console.WriteLine("Fertig! Drücken Sie eine beliebige Taste zum beenden...");
        Console.ReadKey();
    }
        
    private static void OutputToCSV(List<ThreadRating> ratings, ScanOptions options)
    {
        var path = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        var dir = new DirectoryInfo(Path.Combine(path, $"Bewertungszusammenfassung_{DateTime.Now:dd-MM-yyyy_hh_mm_ss}"));
        dir.Create();
        var map = ratings.ToLookup(x => x.Id.forum);
                
        foreach (var forum in options.Subforums)
        {
            var file = new FileInfo(Path.Combine(dir.FullName, forum.Name + ".csv"));

            var header = "Name;Bewertung;Stimmen;Link;Wiki\n";
            File.AppendAllText(file.FullName, header, Encoding.UTF8);
            if (map.Contains(forum.Id))
            {
                var rows = map[forum.Id].OrderBy(x => x.Name)
                    .Select(x => $"{HttpUtility.HtmlDecode(x.Name)};{x.Rating:F};{x.VoteCount};{DSAScanner.GetUrl(new DSAThreadIdentifier(x.Id.forum, x.Id.thread))};{HttpUtility.HtmlEncode(x.Wiki)}")
                    .ToArray();
                    
                File.AppendAllLines(file.FullName, rows, Encoding.UTF8);
            }
        }
    }
}