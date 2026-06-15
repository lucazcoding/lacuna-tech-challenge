using LACUNATECH_challenge.Helpers;
using LACUNATECH_challenge.Models;

namespace LACUNATECH_challenge.Services;

public class JobService
{
    private readonly LumaApiClient _api;
    private readonly SyncService _sync;

    public JobService(LumaApiClient api, SyncService sync)
    {
        _api = api;
        _sync = sync;
    }

    // ─── Loop principal de jobs ───────────────────────────────────
    public async Task<bool> RunJobLoopAsync(List<Probe> probes, string token)
    {
        while (true)
        {
            // 1. Pega uma tarefa
            var jobResponse = await _api.TakeJobAsync(token);

            // Sem mais tarefas — sai do loop
            if (jobResponse.Job == null)
            {
                Console.WriteLine("[Jobs] Nenhum job restante.");
                return false;
            }

            var job = jobResponse.Job;
            Console.WriteLine($"[Jobs] Job recebido: {job.Id} | Probe: {job.ProbeName}");

            // 2. Encontra a sonda correspondente pelo nome
            var probe = probes.FirstOrDefault(p => p.Name == job.ProbeName);
            if (probe == null)
                throw new Exception($"Sonda '{job.ProbeName}' não encontrada na lista!");

            // 3. Pega a hora sincronizada e o round-trip dessa sonda
            long probeNowTicks = _sync.GetProbeNow(probe.Id, probe.TimeDilationFactor);
            long roundTrip = _sync.GetRoundTrip(probe.Id);

            // 4. Codifica a hora no formato que a sonda espera
            string probeNowEncoded = TimestampEncoder.Encode(probeNowTicks, probe.Encoding);

            Console.WriteLine($"[Jobs] Respondendo | " +
                              $"ProbeNow: {probeNowEncoded} | " +
                              $"RoundTrip: {roundTrip}");

            // 5. Envia a resposta
            var checkResponse = await _api.CheckJobAsync(
                job.Id, probeNowEncoded, roundTrip, token);

            Console.WriteLine($"[Jobs] Resposta: {checkResponse.Code}");

            // Missão cumprida!
            if (checkResponse.Code == "Done")
                return true;

            // Algo deu errado — precisa recomeçar
            if (checkResponse.Code == "Fail")
                throw new Exception("[Jobs] Fail recebido — reiniciando...");
        }
    }
}