using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using DSARatings.Core;
using Microsoft.Extensions.Configuration;

namespace DSARatings.App;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("-= DSA Rating Scanner =-");
        Console.Write("Reading configuration...");
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false);
        Console.WriteLine("success!");

        Console.WriteLine("Starting up Scanner...");
        var config = builder.Build();
        var options = config.GetSection("ScanOptions").Get<ScanOptions>();

        var scanner = new DSAScanner(options);
        var result = scanner.GetRatings();
        
        var path = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;
        var dir = new DirectoryInfo(Path.Combine(path, $"Bewertungszusammenfassung_{DateTime.Now:dd-MM-yyyy_hh_mm_ss}"));
        dir.Create();
        
        Console.WriteLine("Exporting Data to Json");
        File.WriteAllText(Path.Combine(dir.FullName, "scan.json"), JsonSerializer.Serialize(new {TimeStamp = DateTime.Now, Result = result}));
        Console.WriteLine("Exporting Data to CSV");
        OutputToCSV(dir, result, options);

        Console.WriteLine();
        Console.WriteLine("Fertig! Drücken Sie eine beliebige Taste zum beenden...");
        Console.ReadKey();
    }
        
    private static void OutputToCSV(DirectoryInfo dir, List<ThreadRating> ratings, ScanOptions options)
    {
        var map = ratings.ToLookup(x => x.Id.ForumId);
                
        foreach (var forum in options.Subforums)
        {
            var file = new FileInfo(Path.Combine(dir.FullName, forum.Name + ".csv"));

            var header = "Name;Bewertung;Stimmen;Link;Wiki\n";
            File.AppendAllText(file.FullName, header, Encoding.UTF8);
            if (map.Contains(forum.Id))
            {
                var rows = map[forum.Id].OrderBy(x => x.Name)
                    .Select(x => $"{HttpUtility.HtmlDecode(x.Name)};{x.Rating:F};{x.VoteCount};{DSAScanner.GetUrl(new DSAThreadIdentifier(x.Id.ForumId, x.Id.ThreadId))};{x.Wiki}")
                    .ToArray();
                    
                File.AppendAllLines(file.FullName, rows, Encoding.UTF8);
            }
        }
    }
}