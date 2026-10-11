namespace Auth.Api.Workers;

public sealed class EmailWorkerOptions
{
    public int PollSeconds { get; set; } = 5;
}
