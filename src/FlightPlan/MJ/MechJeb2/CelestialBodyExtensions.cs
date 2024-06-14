using KSP.Sim.impl;
using KSP.Sim;
using static Mono.Security.X509.X520;
using static UnityEngine.ParticleSystem;


namespace MuMech
{
    public static class CelestialBodyExtensions
    {
        public static double TerrainAltitude(this CelestialBodyComponent body, Vector3d worldPosition)
        {
            Position position = new Position(body.coordinateSystem, worldPosition);
            body.GetLatLonAltFromRadius(position, out double lat, out double lon, out double alt);
            body.GetAltitudeFromTerrain(position, out double terrainAlt, out double scenaryAlt);
            body.GetSurfacePosition(lat, lon, alt, out Vector3d surfacePosition);
            return alt;
        }

        public static double TerrainAltitude(this CelestialBodyComponent body, double lat, double lon)
        {
            // body.GetLatLonAltFromRadius(position, out double lat, out double lon, out double alt);
            body.GetSurfacePosition(lat, lon, 0, out Vector3d surfacePosition);
            Position position = new Position(body.coordinateSystem, surfacePosition);
            body.GetAltitudeFromTerrain(position, out double terrainAlt, out double scenaryAlt);

            return terrainAlt;
        }

        //The KSP drag law is dv/dt = -b * v^2 where b is proportional to the air density and
        //the ship's drag coefficient. In this equation b has units of inverse length. So 1/b
        //is a characteristic length: a ship that travels this distance through air will lose a significant
        //fraction of its initial velocity
        public static double DragLength(this CelestialBodyComponent body, Vector3d pos, double dragCoeff, double mass)
        {
            Position position = new Position(body.coordinateSystem, pos);
            double airDensity = body.GetDensity(body.GetPressure(body.GetAltitudeFromRadius(position)), body.GetTemperature(body.GetAltitudeFromRadius(position)));
                // FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(pos, body), FlightGlobals.getExternalTemperature(pos, body));

            if (airDensity <= 0) return double.MaxValue;

            //MechJebCore.print("DragLength " + airDensity.ToString("F5") + " " +  dragCoeff.ToString("F5"));

            return mass / (0.0005 * PhysicsSettings.DragMultiplier * airDensity * dragCoeff);
        }

        public static double DragLength(this CelestialBodyComponent body, double altitudeASL, double dragCoeff, double mass)
        {
            return body.DragLength(body.GetWorldSurfacePosition(0, 0, altitudeASL, body.coordinateSystem), dragCoeff, mass);
        }

        public static double RealMaxAtmosphereAltitude(this CelestialBodyComponent body)
        {
            return !body.hasAtmosphere ? 0 : body.atmosphereDepth;
        }

        //public static double GetSpeedOfSound(this CelestialBodyComponent body, double pressure, double density)
        //{
        //    return 0;
        //}

        public static double AltitudeForPressure(this CelestialBodyComponent body, double pressure)
        {
            if (!body.hasAtmosphere)
                return 0;
            double upperAlt = body.atmosphereDepth;
            double lowerAlt = 0;
            while (upperAlt - lowerAlt > 10)
            {
                double testAlt = (upperAlt + lowerAlt) * 0.5;
                double testPressure = body.GetPressure(testAlt); // FlightGlobals.getStaticPressure(testAlt, body);
                if (testPressure < pressure)
                {
                    upperAlt = testAlt;
                }
                else
                {
                    lowerAlt = testAlt;
                }
            }

            return (upperAlt + lowerAlt) * 0.5;
        }

        // Stock version throws an IndexOutOfRangeException when the body biome map is not defined
        //public static string GetExperimentBiomeSafe(this CelestialBodyComponent body, double lat, double lon)
        //{
        //    if (body.BiomeMap == null || body.BiomeMap.Attributes.Length == 0)
        //        return string.Empty;
        //    return ScienceUtil.GetExperimentBiomeLocalized(body, lat, lon);
        //}
    }
}
