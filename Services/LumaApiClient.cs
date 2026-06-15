using LACUNATECH_challenge.Models;

namespace LACUNATECH_challenge;

using System.Net.Http.Headers;
using System.Net.Http.Json;

public class LumaApiClient
{
    private readonly HttpClient _http;

    // A factory injeta o HttpClient já configurado com a BaseAddress
    public LumaApiClient(HttpClient http)
    {
        _http = http;
    }

    // ─── 1. START ────────────────────────────────────────────────
    public async Task<StartResponse> StartAsync(string username, string email)
    {
        var body = new { username, email };

        var response = await _http.PostAsJsonAsync("/api/start", body);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StartResponse>();
    }

    // ─── 2. LIST PROBES ──────────────────────────────────────────
    public async Task<ProbeListResponse> GetProbesAsync(string token)
    {
        var request = BuildRequest(HttpMethod.Get, "/api/probe", token);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProbeListResponse>();
    }

    // ─── 3. SYNC ─────────────────────────────────────────────────
    public async Task<(SyncResponse data, long t0, long t3)> SyncAsync(
        string probeId, string token)
    {
        var request = BuildRequest(HttpMethod.Post, $"/api/probe/{probeId}/sync", token);

        long t0 = DateTimeOffset.UtcNow.Ticks;          // ← ANTES de enviar
        var response = await _http.SendAsync(request);
        long t3 = DateTimeOffset.UtcNow.Ticks;          // ← DEPOIS de receber

        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<SyncResponse>();
        return (data, t0, t3);
    }

    // ─── 4. TAKE JOB ─────────────────────────────────────────────
    public async Task<JobResponse> TakeJobAsync(string token)
    {
        var request = BuildRequest(HttpMethod.Post, "/api/job/take", token);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<JobResponse>();
    }

    // ─── 5. CHECK JOB ────────────────────────────────────────────
    public async Task<CheckJobResponse> CheckJobAsync(
        string jobId, string probeNow, long roundTrip, string token)
    {
        var request = BuildRequest(HttpMethod.Post, $"/api/job/{jobId}/check", token);

        request.Content = JsonContent.Create(new
        {
            probeNow,
            roundTrip
        });

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CheckJobResponse>();
    }

    // ─── HELPER PRIVADO ──────────────────────────────────────────
    // Monta uma requisição já com o token no header
    private HttpRequestMessage BuildRequest(
        HttpMethod method, string endpoint, string token)
    {
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}