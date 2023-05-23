/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

/*
 * This Software was obtained from the MechJeb2 project (https://github.com/MuMech/MechJeb2) on 3/25/23
 * and was further modified as needed for compatibility with KSP2 and/or for incorporation into the
 * FlightPlan project (https://github.com/schlosrat/FlightPlan)
 * 
 * This work is relaesed under the same licenses noted above from the originating version.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using FlightPlan;

#nullable enable

namespace MechJebLib.Utils
{
    public static class Check
    {
        private static void DoCheck(bool b, string info = "")
        {
            if (!b)
            {
                if (info.Length > 0)
                    FlightPlanPlugin.Logger.LogError($"check failed: {info}");
                else
                    FlightPlanPlugin.Logger.LogError("check failed");
                throw new FailedCheck("check failed");
            }

        }

        public class FailedCheck : Exception
        {
            internal FailedCheck(string msg) : base(msg)
            {
            }
        }

        private static string GetTypeName<T>()
        {
            return typeof(T).FullName;
        }

        /*
         * Booleans
         */

        [Conditional("DEBUG")]
        public static void True(bool b)
        {
            DoCheck(b, "Test for True");
        }

        [Conditional("DEBUG")]
        public static void False(bool b)
        {
            DoCheck(!b, "Test for False");
        }

        /*
         * Null
         */

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : class
        {
            DoCheck(obj != null, "Test for null");
        }

        [Conditional("DEBUG")]
        public static void NotNull<T>(T? obj) where T : struct
        {
            DoCheck(obj != null, "Test for not null");
        }

        /*
         * Floats
         */

        [Conditional("DEBUG")]
        public static void Finite(double d)
        {
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        [Conditional("DEBUG")]
        public static void Positive(double d)
        {
            DoCheck(d > 0);
        }

        [Conditional("DEBUG")]
        public static void PositiveFinite(double d)
        {
            DoCheck(d > 0, "Test for positive");
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        [Conditional("DEBUG")]
        public static void NonNegative(double d)
        {
            DoCheck(d >= 0, "Test for Non-Negative");
        }

        [Conditional("DEBUG")]
        public static void NonNegativeFinite(double d)
        {
            DoCheck(d >= 0, "Test for Non-Negative");
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        [Conditional("DEBUG")]
        public static void Negative(double d)
        {
            DoCheck(d < 0, "Test for negative");
        }

        [Conditional("DEBUG")]
        public static void NegativeFinite(double d)
        {
            DoCheck(d < 0, "Test for negative");
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        [Conditional("DEBUG")]
        public static void NonPositive(double d)
        {
            DoCheck(d <= 0, "Test for Non-Positive");
        }

        [Conditional("DEBUG")]
        public static void NonPositiveFinite(double d)
        {
            DoCheck(d <= 0, "Test for Non-Positive");
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        [Conditional("DEBUG")]
        public static void Zero(double d)
        {
            DoCheck(d == 0, "Test for zero");
        }

        [Conditional("DEBUG")]
        public static void NonZero(double d)
        {
            DoCheck(d != 0, "Test for non-zero");
        }

        [Conditional("DEBUG")]
        public static void NonZeroFinite(double d)
        {
            DoCheck(d != 0, "Test for non-zero");
            DoCheck(d.IsFinite(), "Test for Finite");
        }

        /*
         * Vectors
         */

        /*
        [Conditional("DEBUG")]
        public static void Finite(Vector3d v)
        {
            DoCheck(IsFinite(v), "Test for Finite Vector3d");
        }
        */

        [Conditional("DEBUG")]
        public static void Finite(V3 v)
        {
            DoCheck(IsFinite(v), "Test for Finite V3");
        }

        [Conditional("DEBUG")]
        public static void NonZero(V3 v)
        {
            DoCheck(v != V3.zero, "Test for non-zero V3");
        }

        [Conditional("DEBUG")]
        public static void NonZeroFinite(V3 v)
        {
            DoCheck(v != V3.zero, "Test for non-zero V3");
            DoCheck(IsFinite(v), "Test for Finite V3");
        }

        /*
         * Arrays
         */

        [Conditional("DEBUG")]
        public static void CanContain(double[] arry, int len)
        {
            DoCheck(arry.Length >= len, $"Test for array size (length) >= {len}");
        }

        [Conditional("DEBUG")]
        public static void CanContain(IReadOnlyList<double> arry, int len)
        {
            DoCheck(arry.Count >= len, $"Test for array size (count) >= {len}");
        }
    }
}
