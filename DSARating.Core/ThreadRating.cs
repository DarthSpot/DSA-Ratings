namespace DSARatings.Core;

public class ThreadRating
{
    public Dictionary<int, int> Votes = new Dictionary<int, int>();

    public int VoteCount => Votes.Values.Sum();

    public double Rating => VoteCount > 0 ? Votes.Select(x => (double) (x.Key * x.Value)).Sum() / VoteCount : 0;

    public ThreadRating(int forum, int thread, string name)
    {
        Id = (forum, thread);
        Name = name;
    }

    public (int forum, int thread) Id { get; set; }

    public string Name { get; set; }
    public string Wiki { get; set; }

    public void Add(int voteId, int votes)
    {
        var voteVal = 6 - voteId;
        Votes.Add(voteVal, votes);
    }
}