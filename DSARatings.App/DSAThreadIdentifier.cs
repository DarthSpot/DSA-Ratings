using DSARatings.Core;

namespace DSARatings.App;

internal class DSAThreadIdentifier(int forumId, int threadId) : IThreadIdentifier
{
    public int ForumId { get; set; } = forumId;
    public int ThreadId { get; set; } = threadId;
}