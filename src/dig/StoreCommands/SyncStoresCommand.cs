namespace dig;

internal sealed class SyncStoresCommand()
{
    [Argument(0, Name = "uri", Description = "The uri of the remote list.", Default = "https://api.datalayer.storage/mirrors/v1/list_all")]
    public string Uri { get; init; } = string.Empty;

    [Option("s", "subscribe-only", Description = "Only subscribe, do not mirror, each store.")]
    public bool SubscribeOnly { get; init; }

    [Option("p", "prune", Description = "Remove any mirrors/subscriptions that are not in the remote list.")]
    public bool Prune { get; init; }

    [Option("f", "fee", Default = 0UL, ArgumentHelpName = "MOJOS", Description = "Default fee to use for each mirror transaction.")]
    public ulong Fee { get; init; } = 0UL;

    [Option("r", "reserve", Default = 300000001UL, ArgumentHelpName = "MOJOS", Description = "The amount to reserve with each mirror coin.")]
    public ulong Reserve { get; init; } = 300000001UL;

    [CommandTarget]
    public async Task<int> Execute(StoreSyncService syncService)
    {
        // pass CancellationToken.None as we want this to run as long as it takes
        var result = await syncService.SyncStores(Uri, Reserve, !SubscribeOnly, Prune, Fee, CancellationToken.None);
        if (result.message is not null)
        {
            Console.WriteLine("The data layer appears busy. Try again later.\n\t{0}", result.message);
        }

        Console.WriteLine($"Added {result.addedCount} and removed {result.removedCount} stores.");

        return 0;
    }
}
