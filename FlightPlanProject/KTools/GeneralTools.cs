using System;
using System.Text.RegularExpressions;
using KSP.Sim.Maneuver;
using KSP.Game;

namespace FlightPlan.KTools;

public static class GeneralTools
{
    public static GameInstance Game => GameManager.Instance == null ? null : GameManager.Instance.Game;

    public static double Current_UT => Game.UniverseModel.UniversalTime;

    /// <summary>
    /// Converts a string to a double, if the string contains a _number. Else returns -1
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static double GetNumberString(string str)
    {
        string _number = Regex.Replace(str, "[^0-9.]", "");

        return _number.Length > 0 ? double.Parse(_number) : -1;
    }

    public static int ClampInt(int value, int min, int max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }

    public static Vector3d CorrectEuler(Vector3d euler)
    {
        Vector3d _result = euler;
        if (_result.x > 180)
        {
            _result.x -= 360;
        }
        if (_result.y > 180)
        {
            _result.y -= 360;
        }
        if (_result.z > 180)
        {
            _result.z -= 360;
        }

        return _result;
    }

    public static double RemainingStartTime(ManeuverNodeData node)
    {
        double _dt = node.Time - GeneralTools.Game.UniverseModel.UniversalTime;
        return _dt;
    }

    public static Guid CreateGuid()
    {
        return Guid.NewGuid();
    }

}
