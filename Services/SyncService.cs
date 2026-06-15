using LACUNATECH_challenge.Helpers;
using LACUNATECH_challenge.Models;

namespace LACUNATECH_challenge.Services;

public class SyncService
{
    private readonly LumaApiClient _api;
    private readonly ProbeClockStore _store; // ← NOVO

    private const long FiveMillisecondsTicks = 5 * 10_000;

    public SyncService(LumaApiClient api, ProbeClockStore store) // ← NOVO
    {
        _api = api;
        _store = store;
    }

    public async Task SyncAllAsync(List<Probe> probes, string token)
    {
        foreach (var probe in probes)
        {
            Console.WriteLine($"[Probe] {probe.Name} | Encoding: {probe.Encoding} | TimeDilationFactor: {probe.TimeDilationFactor ?? 1.0}");
            
            Console.WriteLine($"[Sync] Iniciando sincronização: {probe.Name}");
            await SyncProbeAsync(probe, token);
            Console.WriteLine($"[Sync] {probe.Name} sincronizada!");
        }
    }

    private async Task SyncProbeAsync(Probe probe, string token)
    {
        if (!_store.Contains(probe.Id))
            _store.Set(probe.Id, new ProbeClockState());

        const long RoundTripThreshold = 100 * 10_000; // 100ms — ajustável

        long bestOffset = 0;
        long bestRoundTrip = long.MaxValue;
        long bestSyncedAt = 0;
        int attempts = 0;
        const int MaxAttempts = 20;

        while (true)
        {
            var (syncData, t0, t3) = await _api.SyncAsync(probe.Id, token);

            if (syncData.Code == "ProbeUnreachable")
            {
                Console.WriteLine($"[Sync] {probe.Name} | ProbeUnreachable - aguardando 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                continue;
            }

            if (syncData.Code != "Success" || syncData.T1 is null || syncData.T2 is null)
                throw new Exception($"Sync falhou para {probe.Name}. Code: {syncData.Code}");

            long t1 = TimestampDecoder.Decode(syncData.T1, probe.Encoding);
            long t2 = TimestampDecoder.Decode(syncData.T2, probe.Encoding);

            long fator1 = t1 - t0;
            long fator2 = t2 - t3;
            long newOffset = (fator1 + fator2) / 2;
            long roundTrip = (t3 - t0) - (t2 - t1);

            Console.WriteLine($"[Sync] {probe.Name} | newOffset: {newOffset} ticks | roundTrip: {roundTrip} ticks");

            if (roundTrip < bestRoundTrip)
            {
                bestOffset = newOffset;
                bestRoundTrip = roundTrip;
                bestSyncedAt = DateTimeOffset.UtcNow.Ticks;
            }

            attempts++;

            if (roundTrip < RoundTripThreshold || attempts >= MaxAttempts)
                break;
        }

        var state = _store.Get(probe.Id);
        state.TimeOffset = bestOffset;
        state.LastRoundTrip = bestRoundTrip;
        state.SyncedAtTicks = bestSyncedAt;
    }
    
    public long GetProbeNow(string probeId, double? timeDilationFactor)
    {
        var state = _store.Get(probeId);

        long S = state.SyncedAtTicks;
        long D = state.TimeOffset;
        long M_agora = DateTimeOffset.UtcNow.Ticks;
        double F = timeDilationFactor ?? 1.0;

        long decorridoPraMim = M_agora - S;
        long decorridoPraSonda = (long)(decorridoPraMim / F);

        return (S + D) + decorridoPraSonda;
    }

    public long GetRoundTrip(string probeId) =>
        _store.Get(probeId).LastRoundTrip;
}