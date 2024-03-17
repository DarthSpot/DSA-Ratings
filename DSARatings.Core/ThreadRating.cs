namespace DSARatings.Core;

public class ThreadRating
{
    public Dictionary<int, int> Votes = new();
    public int VoteCount => Votes.Values.Sum();
    public double Rating => VoteCount > 0 ? Votes.Select(x => (double) (x.Key * x.Value)).Sum() / VoteCount : 0;
    public ThreadIdentifier Id { get; set; }
    public string Name { get; set; }
    public string? Wiki { get; set; }
    
    public ThreadRating(int forum, int thread, string name)
    {
        Id = new ThreadIdentifier(forum, thread);
        Name = name;
    }

    public void Add(int voteId, int votes)
    {
        var voteVal = 6 - voteId;
        Votes.Add(voteVal, votes);
    }
}

public struct ThreadIdentifier(int ForumId, int ThreadId)
{
    public int ForumId { get; } = ForumId;
    public int ThreadId { get; } = ThreadId;
}