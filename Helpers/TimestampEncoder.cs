namespace LACUNATECH_challenge.Helpers;

public class TimestampEncoder
{
    public static string Encode(long ticks, string encoding)
    {
        return encoding switch
        {
            "Iso8601" => new DateTimeOffset(ticks, TimeSpan.Zero)
                .ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz"),

            "Ticks" => ticks.ToString(),

            "TicksBinary" => Convert.ToBase64String(
                BitConverter.GetBytes(ticks)),

            "TicksBinaryBigEndian" => Convert.ToBase64String(
                ReverseBytes(
                    BitConverter.GetBytes(ticks))),

            _ => throw new Exception($"Encoding desconhecido: {encoding}")
        };
    }

    private static byte[] ReverseBytes(byte[] bytes)
    {
        Array.Reverse(bytes);
        return bytes;
    }
    
    
    

}