namespace FlightPlan.KTools.UI;

public class StrTool
{
    static public string DurationToString(double secs)
    {
        string _prefix = "";
        if (secs < 0)
        {
            secs = -secs;
            _prefix = "- ";
        }

        if (secs > 21600) // 3600 * 6 = 21 600
        {
            int days = (int)(secs / 21600);
            secs = secs - days * 21600;
            _prefix += $"{days}d ";
        }


        try
        {
            TimeSpan t = TimeSpan.FromSeconds(secs);
            string _result = _prefix + string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D3}",
            t.Hours,
            t.Minutes,
            t.Seconds,
            t.Milliseconds);
            return _result;
        }
        catch (System.Exception)
        {
            return _prefix + $"{secs:n2} s";
        }
    }

    public const double AstronomicalUnit = 149597870700;
    // https://en.wikipedia.org/wiki/Parsec
    public static double Parsec { get; } = (648000 / Math.PI) * AstronomicalUnit;
    public static string DistanceToString(double meters)
    {
        string _sign = "";
        if (meters < 0)
        {
            _sign = "-";
            meters = -meters;
        }
        if (meters > (Parsec / 10))
        {
            return $"{_sign}{(meters / Parsec):n2} pc";
        }
        if (meters > (AstronomicalUnit / 10))
        {
            return $"{_sign}{(meters / AstronomicalUnit):n2} AU";
        }
        if (meters > (997))
        {
            return $"{_sign}{(meters / 1000):n2} km";
        }
        if (meters < 1)
        {
            return $"{_sign}{meters * 100:0} cm";
        }

        return _sign + meters.ToString("0") + " m";
    }

    static public string VectorToString(Vector3d vec)
    {
        return $"{vec.x:n2} {vec.y:n2} {vec.z:n2}";
    }

}
