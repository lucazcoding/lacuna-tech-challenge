using LACUNATECH_challenge.Models;
using LACUNATECH_challenge.Services;

namespace LACUNATECH_challenge.Workers;

public class LumaWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; // ← único campo
    private readonly ProbeClockStore _store;

    private const string Username = "lucazcode";
    private const string Email = "lucazcode@gmail.com";
    
    // private const string StartEndpoint = "/api/start";    // nível 1
    private const string StartEndpoint = "/api/start/2";

    public LumaWorker(IServiceScopeFactory scopeFactory, ProbeClockStore store) // ← único parâmetro
    {
        _scopeFactory = scopeFactory;
        _store = store;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[Worker] Iniciando missão Luma...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Cria um escopo novo a cada tentativa
                using var scope = _scopeFactory.CreateScope();

                // Resolve os serviços dentro do escopo
                var api  = scope.ServiceProvider.GetRequiredService<LumaApiClient>();
                var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
                var jobs = scope.ServiceProvider.GetRequiredService<JobService>();

                await RunMissionAsync(api, sync, jobs, stoppingToken);

                Console.WriteLine("[Worker] 🎉 Missão concluída com sucesso!");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker] ⚠️ Reiniciando: {ex.Message}");
                Console.WriteLine($"[Worker] 📍 Onde quebrou: {ex.StackTrace}");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    // ← agora recebe os serviços como parâmetro
    private async Task RunMissionAsync(
        LumaApiClient api,
        SyncService sync,
        JobService jobs,
        CancellationToken stoppingToken)
    {
        _store.Clear();
        
        Console.WriteLine("[Worker] Autenticando...");
        var startResponse = await api.StartAsync(Username, Email, StartEndpoint);

        if (startResponse.Code != "Success")
            throw new Exception($"Start falhou: {startResponse.Message}");

        var token = startResponse.AccessToken!;
        Console.WriteLine("[Worker] Token obtido!");

        Console.WriteLine("[Worker] Buscando sondas...");
        var probeResponse = await api.GetProbesAsync(token);

        if (probeResponse.Code != "Success")
            throw new Exception($"GetProbes falhou: {probeResponse.Code}");

        var probes = probeResponse.Probes!;
        Console.WriteLine($"[Worker] {probes.Count} sonda(s) encontrada(s).");

        Console.WriteLine("[Worker] Sincronizando relógios...");
        await sync.SyncAllAsync(probes, token);
        Console.WriteLine("[Worker] Todos os relógios sincronizados!");

        Console.WriteLine("[Worker] Iniciando loop de jobs...");
        bool done = await jobs.RunJobLoopAsync(probes, token);

        if (!done)
            throw new Exception("Jobs acabaram sem receber Done — reiniciando.");
    }
}