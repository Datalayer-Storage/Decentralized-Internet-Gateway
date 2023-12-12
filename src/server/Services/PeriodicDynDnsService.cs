internal sealed class PeriodicDynDnsService(StoreSyncService syncService,
                                            ILogger<PeriodicStoreSyncService> logger,
                                            IConfiguration configuration) : BackgroundService
{
    private readonly StoreSyncService _syncService = syncService;
    private readonly ILogger<PeriodicStoreSyncService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // default to once a day
            var delay = _configuration.GetValue("dig:DynDnsSyncIntervalMinutes", 1440);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(delay));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var mirrorListUri = _configuration.GetValue("dig:DataLayerStorageUri", "https://api.datalayer.storage/") + "mirrors/v1/list_all";
                var reserveAmount = _configuration.GetValue<ulong>("dig:AddMirrorAmount", 300000001);
                var addMirrors = _configuration.GetValue("dig:MirrorServer", true);
                var defaultFee = _configuration.GetValue<ulong>("dig:DefaultFee", 500000);

                await _syncService.SyncStores(mirrorListUri, reserveAmount, addMirrors, defaultFee, stoppingToken);

                _logger.LogInformation("Waiting {delay} minutes", delay);
            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            if (ex is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    _logger.LogError(innerException, "One or many tasks failed: {Message}", innerException.Message);
                }
            }
            else
            {
                _logger.LogError(ex, "{Message}", ex.Message);
            }
            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}
