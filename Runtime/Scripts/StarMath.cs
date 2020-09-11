using System;
using UnityEngine;

namespace Astronomy
{
    /// <summary>
    /// A collection of methods pertaining to celestial coordinate systems.
    /// </summary>
    public static class StarMath
    {
        /// <summary>
        /// Creates a horizontal coordinate projection of <b>ecliptic coordinates</b>
        /// </summary>
        /// <param name="eclipticLat"> ecliptic latitude in <b>degrees</b></param>
        /// <param name="eclipticLong"> ecliptic longitude in <b>degrees</b> </param>
        /// <param name="latitude"> latitude of horizontal projection in <b>degrees</b> </param>
        /// <param name="longitude"> longitude of horizontal projection in <b>degrees</b> </param>
        /// <param name="ut"> universal time of horizontal projection </param>
        /// <returns> a horizontal projection of ecliptic coordinates </returns>
        public static Quaternion HorizontalFromEcliptic(float eclipticLat, float eclipticLong, float latitude, float longitude, DateTime ut)
        {
            return Equatorial2Horizontal(latitude, longitude, ut)
               * Ecliptic2Equatorial(ut)
               * Quaternion.Euler(eclipticLat, eclipticLong, 0);
        }

        /// <summary>
        /// Creates a horizontal coordinate projection of <b>equatorial coordinates</b>
        /// </summary>
        /// <param name="declination"> equatorial declination in <b>degrees</b> </param>
        /// <param name="rightAscension"> equatorial right ascension in <b>degrees</b> </param>
        /// <param name="latitude"> latitude of horizontal projection in <b>degrees</b> </param>
        /// <param name="longitude"> longitude of horizontal projection in <b>degrees</b> </param>
        /// <param name="ut"> universal time of horizontal projection </param>
        /// <returns> a horizontal projection of equatorial coordinates </returns>
        public static Quaternion HorizontalFromEquatorial(float declination, float rightAscension, float latitude, float longitude, DateTime ut)
        {
            return Equatorial2Horizontal(latitude, longitude, ut)
                * Quaternion.Euler(declination, rightAscension, 0);
        }

        /// <summary>
        /// Creates a quaternion to convert ecliptic coordinates to <b>equatorial coordinates</b>
        /// </summary>
        /// <param name="ut">the universal time at which the conversion takes place</param>
        /// <returns> a quaternion that converts an excliptic coordinate quaternion to an equatorial coordinate quaternion </returns>
        public static Quaternion Ecliptic2Equatorial(DateTime ut)
        {
            return Quaternion.AngleAxis((float)ObliquityOfEcliptic(JulianDays2000Epoch(JulianDays(ut))), new Vector3(0, 0, -1));
        }

        /// <summary>
        /// Creates a quaternion to convert equatorial coordinates to <b>horizontal coordinates</b>
        /// </summary>
        /// <param name="latitude"> latitude of horizontal projection in <b>degrees</b></param>
        /// <param name="longitude"> longitude of horizontal projection in <b>degrees</b> </param>
        /// <param name="ut"> universal time of horizontal projection </param>
        /// <returns> a quaternion to convert equatorial coordinates to horizontal coordinates</returns>
        public static Quaternion Equatorial2Horizontal(float latitude, float longitude, DateTime ut)
        {
            return Quaternion.AngleAxis(270f - latitude, new Vector3(1, 0, 0))
                * Quaternion.AngleAxis((float)LocalHourAngle(JulianDays2000Epoch(JulianDays(ut)), longitude), new Vector3(0, -1, 0));
        }

        /// <summary>
        /// double modulus
        /// </summary>
        /// <param name="n">input value</param>
        /// <param name="m">modulus falue</param>
        /// <returns><c>n % m</c> (but for doubles)</returns>
        public static double Dmod(double n, double m)
        {
            return n < 0 ? n + Math.Ceiling(-n / m) * m : n - Math.Floor(n / m) * m;
        }

        /// <summary>
        /// Converts a gregorian date to its <b>Julian day</b> value
        /// </summary>
        /// <param name="ut">input universal date time</param>
        /// <returns> the julian day value of <c>dt</c></returns>
        public static double JulianDays(DateTime ut)
        {
            return (double)(1461L * (ut.Year + 4800L + (ut.Month - 14L) / 12L) / 4L
                + (367L * (ut.Month - 2L - 12L * ((ut.Month - 14L) / 12L))) / 12L
                - (3L * ((ut.Year + 4900L + (ut.Month - 14L) / 12L) / 100L)) / 4L)
                + ut.Day - 32075L
                + (ut.Hour - 12d) / 24d
                + ut.Minute / 1440d
                + ut.Second / 86400d;
        }

        /// <summary>
        /// Normalizes a Julian day value to the 2000 epoch
        /// </summary>
        /// <param name="julianDays"> Julian day value</param>
        /// <returns> Julian days since the 2000 epoch</returns>
        public static double JulianDays2000Epoch(double julianDays)
        {
            return julianDays - 2451545.0d;
        }

        /// <summary>
        /// Finds the <b>local hour angle</b> component of equatorial coordinates for a given time and position on Earth.
        /// </summary>
        /// <param name="julianDays2000Epoch"> time of conversion in julian days since 2000 epoch</param>
        /// <param name="longitude"> longitude component of position on earth in <b>degrees</b></param>
        /// <returns> the local hour angle given a specific time and place on earth</returns>
        public static double LocalHourAngle(double julianDays2000Epoch, double longitude = 0d)
        {
            return Dmod(280.46061837d + 360.98564736629d * julianDays2000Epoch + longitude, 360d);
        }

        /// <summary>
        /// The <b>Obliquity of the Ecliptic</b> the angle in which the equatorial coordinate system is offset from the ecliptic coordinate system.
        /// </summary>
        /// <param name="julianDays2000Epoch"> the time for which to find the Obliquity of the Ecliptic</param>
        /// <returns> The obliquity of the ecliptic at the privided time </returns>
        public static double ObliquityOfEcliptic(double julianDays2000Epoch)
        {
            return 23.4393d - 0.0000003563d * julianDays2000Epoch;
        }

        /// <summary>
        /// Gets the position of the sun for any given time and place on Earth.
        /// </summary>
        /// <param name="ut">universal time</param>
        /// <param name="latitude"> latitude coordinates of a point on Earth in <b>degrees</b></param>
        /// <param name="longitude"> longitude coordinates of a point on Earth in <b>degrees</b></param>
        /// <returns> the position of the sun in horizontal coordinates</returns>
        public static Quaternion SolarPosition(DateTime ut, float latitude, float longitude)
        {
            double julianDays = JulianDays2000Epoch(JulianDays(ut));
            double meanLongitude = 280.461d + 0.9856474d * julianDays;
            double meanAnamoly = (357.528d + 0.9856003d * julianDays) * Math.PI / 180d;
            double eclipticLongitude = meanLongitude + 1.915d * Math.Sin(meanAnamoly) + 0.020d * Math.Sin(2d * meanAnamoly);

            return HorizontalFromEcliptic(0, 180f + (float)eclipticLongitude, latitude, longitude, ut);
        }

        /// <summary>
        /// Gets the position of the moon for any given time and place on Earth.
        /// </summary>
        /// <param name="ut">universal time</param>
        /// <param name="latitude"> latitude coordinates of a point on Earth in <b>degrees</b></param>
        /// <param name="longitude"> longitude coordinates of a point on Earth in <b>degrees</b></param>
        /// <returns> the position of the moon in horizontal coordinates</returns>
        public static Quaternion LunarPosition(DateTime ut, float latitude, float longitude)
        {
            double j = JulianDays2000Epoch(JulianDays(ut));
            double d = j;// JulianDaysUnknown(time);                        
            double N = Dmod(125.1228d - 0.0529538083d * d, 360d) * Math.PI / 180d;
            double i = 5.1454d * Math.PI / 180d;
            double w = Dmod(318.0634d + 0.1643573223d * d, 360d) * Math.PI / 180d;
            double a = 60.2666d;
            double e = 0.0549d;
            double M = Dmod(115.3654d + 13.0649929509d * d, 360d) * Math.PI / 180d;
            double E = LunarEccentricAnamoly(M, e);
            double x = a * (Math.Cos(E) + e);
            double y = a * Math.Sqrt(1d + e * e) * Math.Sin(E);
            double r = Math.Sqrt(x * x + y * y);
            double v = Math.Atan2(y, x);
            // Rectangular Ecliptic Coordinates
            double xeclip = r * (Math.Cos(N) * Math.Cos(v + w) - Math.Sin(N) * Math.Sin(v + w) * Math.Cos(i));
            double yeclip = r * (Math.Sin(N) * Math.Cos(v + w) + Math.Cos(N) * Math.Sin(v + w) * Math.Cos(i));
            double zeclip = r * Math.Sin(v + w) * Math.Sin(i);

            {// Purturbations
                double eclipticLatitude = Math.Atan2(zeclip, Math.Sqrt(xeclip * xeclip + yeclip * yeclip));
                double eclipticLongitude = Math.Atan2(yeclip, xeclip);
                double ws = Dmod(282.9404d + 0.0000470935d * d, 360) * Math.PI / 180d;
                double Ms = Dmod(356.0470d + 0.9856002585d * d, 360) * Math.PI / 180d;
                double Mm = M;
                double Ls = ws + Ms;
                double Lm = N + w + M;
                double D = Lm - Ls;
                double F = Lm - N;

                eclipticLatitude += Math.PI / 180d * (
                    -1.274d * Math.Sin(Mm - 2d * D)
                    + 0.658d * Math.Sin(2d * D)
                    - 0.186d * Math.Sin(Ms)
                    - 0.059d * Math.Sin(2d * Mm - 2d * D)
                    - 0.057d * Math.Sin(Mm - 2d * D + Ms)
                    + 0.053d * Math.Sin(Mm + 2d * D)
                    + 0.046d * Math.Sin(2d * D - Ms)
                    + 0.041d * Math.Sin(Mm - Ms)
                    - 0.035d * Math.Sin(D)
                    - 0.031d * Math.Sin(Mm + Ms)
                    - 0.015d * Math.Sin(2d * F - 2d * D)
                    + 0.011d * Math.Sin(Mm - 4d * D));

                eclipticLatitude += Math.PI / 180d * (
                    -0.173d * Math.Sin(F - 2d * D)
                    - 0.055d * Math.Sin(Mm - F - 2d * D)
                    - 0.046d * Math.Sin(Mm + F - 2d * D)
                    + 0.033d * Math.Sin(F + 2d * D)
                    + 0.017d * Math.Sin(2d * Mm + F));

                r += -0.58 * Math.Cos(Mm - 2d * D)
                     - 0.46 * Math.Cos(2d * D);

                xeclip = r * Math.Cos(eclipticLongitude) * Math.Cos(eclipticLatitude);
                yeclip = r * Math.Sin(eclipticLongitude) * Math.Cos(eclipticLatitude);
                zeclip = r * Math.Sin(eclipticLatitude);
            }
            // Rectangular Equatorial Coordinates
            double oblecl = ObliquityOfEcliptic(j) * Math.PI / 180d;
            double xequat = xeclip;
            double yequat = yeclip * Math.Cos(oblecl) - zeclip * Math.Sin(oblecl);
            double zequat = yeclip * Math.Sin(oblecl) + zeclip * Math.Cos(oblecl);
            // Spherical Equatorial Coordinates
            double decl = Math.Atan2(zequat, Math.Sqrt(xequat * xequat + yequat * zequat));
            double ra = Math.Atan2(yequat, xequat);
            //Correct for distance of viewer from earth's center
            double mpar = Math.Asin(1d / r);
            double gclat = (latitude - 0.1924d * Math.Sin(2d * latitude)) * Math.PI / 180d;
            double rho = 0.99833d + 0.00167d * Math.Cos(2d * latitude);
            double ha = LocalHourAngle(j, longitude) * Math.PI / 180d - ra;
            double g = Math.Atan(Math.Tan(gclat) / Math.Cos(ha));
            float topRA = (float)((ra - mpar * rho * Math.Cos(gclat) * Math.Sin(ha) / Math.Cos(decl)) * 180d / Math.PI);
            float topDecl = (float)((decl - mpar * rho * Math.Sin(gclat) * Math.Sin(g - decl) / Math.Sin(g)) * 180d / Math.PI);

            return HorizontalFromEquatorial(-topDecl, 180f + topRA, latitude, longitude, ut);
        }

        public static double LunarEccentricAnamoly(double M, double e)
        {
            double E = M + e * Math.Sin(M) * (1d + e * Math.Cos(M));

            for (int i = 0; i < 2; i++)
                E -= (E - e * Math.Sin(E) - M) / (1d - e * Math.Cos(E));

            return E;
        }
    }
}
