using System.Globalization;
using UnityEngine;

namespace FlightPlan.KTools.UI;

/// <summary>
/// A set of simple tools for _colors that is missing in the main unity API
/// </summary>
public class ColorTools
{
    /// <summary>
    /// Convert a HSV color from rgb Color values.
    /// </summary>
    public static void ToHSV(Color col, out float h, out float s, out float v)
    {
        float _min, _max, _delta;

        float _r = col.r;
        float _g = col.g;
        float _b = col.b;

        _min = Mathf.Min(_r, Mathf.Min(_g, _b));
        _max = Mathf.Max(_r, Mathf.Max(_g, _b));
        v = _max;                // _v

        _delta = _max - _min;


        if (_delta != 0)
        {
            s = _delta / _max;        // _s
        }
        else
        {
            // _r = _g = _b = 0		// _s = 0, _v is undefined
            s = 0;
            h = 0;
            return;
        }

        if (_r == _max)
        {
            h = (_g - _b) / _delta;        // between yellow & magenta
        }
        else if (_g == _max)
        {
            h = 2 + (_b - _r) / _delta;    // between cyan & yellow
        }
        else
        {
            h = 4 + (_r - _g) / _delta;    // between magenta & cyan
        }

        h *= 60f / 360;             // 0-1

        if (h < 0)
            h += 1;
    }

    /// <summary>
    /// Convert a color from HSV values. any value is vetween 0 and one
    /// </summary>
    public static Color FromHSV(float hue, float saturation, float value, float alpha)
    {
        hue *= 360;
        //FlightPlanPlugin.Logger.LogDebug("hue : " + hue);

        int _hi = ((int)(Mathf.Floor(hue / 60f))) % 6;
        //FlightPlanPlugin.Logger.LogDebug("_hi : " + _hi);
        float _f = hue / 60f - Mathf.Floor(hue / 60f);
        //FlightPlanPlugin.Logger.LogDebug("_f : " + _f);

        float _v = value;
        float _p = value * (1 - saturation);
        float _q = value * (1 - _f * saturation);
        float _t = value * (1 - (1 - _f) * saturation);

        if (_hi == 0)
            return new Color(_v, _t, _p, alpha);
        else if (_hi == 1)
            return new Color(_q, _v, _p, alpha);
        else if (_hi == 2)
            return new Color(_p, _v, _t, alpha);
        else if (_hi == 3)
            return new Color(_p, _q, _v, alpha);
        else if (_hi == 4)
            return new Color(_t, _p, _v, alpha);
        else
            return new Color(_v, _p, _q, alpha);
    }

    /// <summary>
    /// Parse a color as a string 
    /// </summary>
    /// <param name="color">the string representing the color
    /// Under can be any of the predefined color by Unity 
    /// Or a html rgb color ex FF0000 is red
    /// </param>
    /// <returns>the parsed Color</returns>
    static public Color ParseColor(string color)
    {
        if (color == "") return Color.white;
        color = color.ToLower();
        switch (color)
        {
            case "black": return Color.black;
            case "blue": return Color.blue;
            case "clear": return Color.clear;
            case "cyan": return Color.cyan;
            case "gray": return Color.gray;
            case "green": return Color.green;
            case "grey": return Color.grey;
            case "magenta": return Color.magenta;
            case "red": return Color.red;
            case "white": return Color.white;
            case "yellow": return Color.yellow;
            case "orange": return new Color(1f, 0.76f, 0f);
            default:
                {
                    if (color.StartsWith("#"))
                        color = color.Substring(1);

                    while (color.Length < 6)
                        color += "0";

                    int r, g, b;

                    System.Int32.TryParse(color.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture.NumberFormat, out r);
                    System.Int32.TryParse(color.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture.NumberFormat, out g);
                    System.Int32.TryParse(color.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture.NumberFormat, out b);

                    return new Color(
                        ((float)r) / 255,
                        ((float)g) / 255,
                        ((float)b) / 255);
                }
        }
    }

    static public string FormatColorHtml(Color col)
    {
        int _r = (int)(col.r * 255);
        int _g = (int)(col.g * 255);
        int _b = (int)(col.b * 255);
        return string.Format("{0:X2}{1:X2}{2:X2}", _r, _g, _b);
    }

    // just a list of really differnts _colors  that can be used for unitary test
    // any color is far enough from the next one to be fully visible 
    static public Color[] GetRandomColorArray(int Nb, float saturation = 1)
    {

        float _delta = 1f / Nb;
        Color[] _colors = new Color[Nb];

        float _h = UnityEngine.Random.Range(0f, 1);

        for (int i = 0; i < Nb; i++)
        {
            _colors[i] = FromHSV(_h, saturation, 1, 1);
            _h += _delta;
        }

        return _colors;
    }

    // just a list of really differnts _colors  that can be used for unitary test
    // any color is far enough from the next one to be fully visible 
    static public Color[] GetRainbowColorArray(int Nb)
    {
        float _delta = 1f / Nb;
        Color[] _colors = new Color[Nb];

        float _h = 0;

        for (int i = 0; i < Nb; i++)
        {
            _colors[i] = FromHSV(_h, 1, 1, 1);
            _h += _delta;
        }

        return _colors;
    }

    static public Color RandomColor()
    {
        float _h = UnityEngine.Random.Range(0f, 1);
        //  float _v = UnityEngine.Random.Range(0.5f, 1);
        return FromHSV(_h, 1, 1, 1);

    }

    static public Color ChangeColorHSV(Color source, float deltaH, float deltaS, float deltaV)
    {
        float _h, _s, _v;
        ToHSV(source, out _h, out _s, out _v);

        _h += deltaH;
        _s += deltaS;
        _v += deltaV;

        return ColorTools.FromHSV(_h, _s, _v, source.a);
    }
}
