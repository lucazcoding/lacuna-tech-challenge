namespace LACUNATECH_challenge.Models;

// Guarda o estado do relógio de cada sonda
public class ProbeClockState
{
    public long TimeOffset { get; set; } = 0;    // D acumulado
    public long LastRoundTrip { get; set; } = 0; // último atraso de rede
    
    public long SyncedAtTicks { get; set; } = 0;
}