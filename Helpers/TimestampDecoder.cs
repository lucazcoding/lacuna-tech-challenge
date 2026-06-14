namespace LACUNATECH_challenge.Helpers;

public class TimestampDecoder
{
    public static long Decode(string value, string encoding)
    {
        return encoding switch
        {
            "Iso8601" => DateTimeOffset.Parse(value).UtcTicks,

            "Ticks" => long.Parse(value),

            "TicksBinary" => BitConverter.ToInt64(
                Convert.FromBase64String(value), 0),

            "TicksBinaryBigEndian" => BitConverter.ToInt64(
                ReverseBytes(
                    Convert.FromBase64String(value)), 0),

            _ => throw new Exception($"Encoding desconhecido: {encoding}")
        };
    }
    
    private static byte[] ReverseBytes(byte[] bytes)
    {
        Array.Reverse(bytes);
        return bytes;
    }
}