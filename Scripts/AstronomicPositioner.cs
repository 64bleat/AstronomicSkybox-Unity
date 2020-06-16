using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using SHK.Tools;

namespace SHK.Astronomy
{
    [ExecuteInEditMode]
    public class AstronomicPositioner : MonoBehaviour
    {
        public Transform moonRotator;
        public Transform sunRotator;
        public Transform ambientCluster;
        public Transform starCluster;

        [Header("Lighting")]
        public Light sunLight;
        public float sunIntensity = 1f;
        public Color dayColor = new Color(1, 1, 1);
        public Color duskColor = new Color(1f, 0.9f, 0.8f);
        public Light moonLight;
        public float moonIntensity = 0.25f;
        public Light[] ambientLights;
        public float ambientIntensity = 0.02f;
        [Header("Location")]
        public float latitude = 48f;
        public float longitude = -122f;
        [Header("CoordinateSystems")]
        public Transform horizontalCoordinates;
        public Transform equatorialCoordinates;
        public Transform eclipticCoordinates;
        [Header("Star Construction")]
        public string starDataPath = "Sky/StellarData";
        public Material starMaterial;
        public float starScale = 0.075f;
        public GameObject starMeshTemplate;
        [Header("Constellation Construction")]
        public string constellationDataPath = "Sky/ConstellationData";
        //public TextAsset constellationData;
        public GameObject constellationTemplate;
        public GameObject constellationLineTemplate;

        private DateTime lastTime;

        /// <summary>
        /// To save time, mesh data must be calculated before being turned into a mesh
        /// </summary>
        public struct MeshPreData
        {
            public Vector3[] vertices;
            public Vector3[] normals;
            public Vector2[] uv;
            public Color[] colors;
            public int[] triangles;
        }

        private void OnValidate()
        {
            InitializeSky();
        }

        private void Awake()
        {
            InitializeSky();
        }

        public void InitializeSky()
        {
            SetSky(GameTime.currentTime);
        }

        void Update()
        {
            if (lastTime != GameTime.currentTime)
                SetSky(GameTime.currentTime.ToUniversalTime());

            lastTime = GameTime.currentTime;
        }

        public void SetSky(DateTime time)
        {
            // Rotations
            if (equatorialCoordinates && equatorialCoordinates.gameObject.activeSelf)
                equatorialCoordinates.rotation = HorizontalFromEquatorial(0, 0, latitude, longitude, time);
            if (eclipticCoordinates && eclipticCoordinates.gameObject.activeSelf)
                eclipticCoordinates.rotation = HorizontalFromEcliptic(0, 0, latitude, longitude, time);
            if(sunRotator && sunRotator.gameObject.activeSelf)
                sunRotator.rotation = SunPosition(time, latitude, longitude);
            if(moonRotator && moonRotator.gameObject.activeSelf)
                moonRotator.rotation = MoonPosition(time, latitude, longitude);
            if(ambientCluster && ambientCluster.gameObject.activeSelf)
                ambientCluster.rotation = Quaternion.Euler(0, sunRotator.rotation.y, 0);

            // Sunlight
            float sunAlt = Vector3.Angle(sunRotator.rotation * new Vector3(0, 0, 1), new Vector3(0, 1, 0)) - 90f;
            sunLight.intensity = Mathf.Clamp01(sunAlt / 15f) * sunIntensity;
            sunLight.color = Color.Lerp(duskColor, dayColor, sunLight.intensity / sunIntensity);
            starMaterial.SetFloat("_Scale", Mathf.Pow(1f - sunLight.intensity / sunIntensity, 2));

            // Moonlight
            float moonAlt = Vector3.Angle(moonRotator.rotation * new Vector3(0, 0, 1), new Vector3(0, 1, 0)) - 90f;
            moonLight.intensity = (1f - sunLight.intensity / sunIntensity) * Mathf.Clamp01((-moonAlt + 5f) / 5f) * (1f - Quaternion.Angle(moonRotator.rotation, sunRotator.rotation) / 180f) * moonIntensity;

            // AmbientLight
            float ambintensity = Mathf.Pow(Mathf.Clamp01((sunAlt + 12f) / 8f), 2f) * ambientIntensity;
            for (int i = 0; i < ambientLights.Length; i++)
                ambientLights[i].intensity = ambintensity * (i == 0 ? 2 : 1);
        }

        // Conversions
        public static Quaternion HorizontalFromEcliptic(float eclipticLat, float eclipticLong, float latitude, float longitude, DateTime time)
            => Equatorial2Horizontal(latitude, longitude, time) 
            * Ecliptic2Equatorial(time) 
            * Quaternion.Euler(eclipticLat, eclipticLong, 0);
        public static Quaternion HorizontalFromEquatorial(float declination, float rightAscension, float latitude, float longitude, DateTime time)
            => Equatorial2Horizontal(latitude, longitude, time) 
            * Quaternion.Euler(declination, rightAscension, 0);
        public static Quaternion Ecliptic2Equatorial(DateTime time)
            => Quaternion.AngleAxis((float)ObliquityOfEcliptic(JulianDays2000Epoch(JulianDays(time))), new Vector3(0, 0, -1));
        public static Quaternion Equatorial2Horizontal(float latitude, float longitude, DateTime time)
            => Quaternion.AngleAxis(270f - latitude, new Vector3(1, 0, 0)) 
            * Quaternion.AngleAxis((float)LocalHourAngle(JulianDays2000Epoch(JulianDays(time)), longitude), new Vector3(0, -1, 0));
        public static double Repeat(double n, double m)
            => n < 0 ? n + Math.Ceiling(-n / m) * m : n - Math.Floor(n / m) * m;
        public static double JulianDays(DateTime dt)
            => (double)(1461L * (dt.Year + 4800L + (dt.Month - 14L) / 12L) / 4L
                + (367L * (dt.Month - 2L - 12L * ((dt.Month - 14L) / 12L))) / 12L
                - (3L * ((dt.Year + 4900L + (dt.Month - 14L) / 12L) / 100L)) / 4L)
                + dt.Day - 32075L
                + (dt.Hour - 12d) / 24d
                + dt.Minute / 1440d
                + dt.Second / 86400d;
        public static double JulianDays2000Epoch(double julianDays)
            => julianDays - 2451545.0d;
        public static double LocalHourAngle(double julianDays, double longitude = 0d)
            => Repeat(280.46061837d + 360.98564736629d * julianDays + longitude, 360d);
        public static double ObliquityOfEcliptic(double julianDays)
            => 23.4393d - 0.0000003563d * julianDays;


        /// <summary>
        /// Creates star meshes, one quad per star.
        /// Extra data for the shader is stored in the colors array as a value between 0 and 1.
        ///     R: Magnitude
        ///     G: Luminosity
        ///     B: ColorIndex
        /// </summary>
        private static double? TryParse(string s) => double.TryParse(s, out double d) ? (double?)d : null;

        private static readonly int maxStarsPerMesh = 16383;
        private static readonly Vector3 starDistance = new Vector3(0, 0, 15);
        private static readonly Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
        };

        public void GenerateStarMeshes()
        {
            if (Resources.Load<TextAsset>(starDataPath) is TextAsset starData && starData)
            {
                var stars = (from line in starData.text.Split('\n')
                        let data = line.Split(',')
                        where data.Length > 33
                        let hip = TryParse(data[1])
                        let ra = TryParse(data[7])
                        let dec = TryParse(data[8])
                        let dist = TryParse(data[9])
                        let mag = TryParse(data[13])
                        let ci = TryParse(data[17])
                        let lum = TryParse(data[33])
                        where ra != null 
                            && dec != null 
                            && dist != null
                            && mag != null 
                            && ci != null 
                            && lum != null
                        select new { 
                            hip = (int)(hip ?? -1), 
                            ra = (float)((ra ?? 0) * 15),
                            dec = (float)(dec ?? 0),
                            dist = dist ?? 0, 
                            mag = mag ?? 0, 
                            ci = (float)((ci ?? 0) / 200000d + 0.5d),
                            lum = (float)((lum ?? 0) / Math.Pow(1d + (dist ?? 0), 2) / 1.75d)}
                        ).ToArray();

                Resources.UnloadAsset(starData);

                // Star Variables
                int starIndex = 0;
                int meshCount = stars.Length / maxStarsPerMesh + 1;
                GameObject[] starMeshGO = new GameObject[meshCount];
                MeshPreData[] starMeshData = new MeshPreData[meshCount];

                //clear old data
                foreach (Transform child in starCluster.GetComponentsInChildren<Transform>())
                    if (child != null && child != starCluster)
                        DestroyImmediate(child.gameObject);

                // Star Mesh Info Initialization
                for (int i = 0; i < meshCount; i++)
                {
                    starMeshGO[i] = Instantiate(starMeshTemplate, starCluster, false);
                    starMeshData[i].vertices = new Vector3[65535];
                    starMeshData[i].normals = new Vector3[65535];
                    starMeshData[i].uv = new Vector2[65535];
                    starMeshData[i].colors = new Color[65535];
                    starMeshData[i].triangles = new int[65535 / 2 * 3];
                }

                // Star Mesh Info Generation
                foreach(var star in stars)
                {
                    double magnitude = Math.Pow(star.lum, 0.125);
                    int meshObjectIndex = starIndex % meshCount;
                    int quadIndex = starIndex / meshCount * 4;
                    int triIndex = starIndex / meshCount * 6;
                    Quaternion equatorialPos = Quaternion.Euler(star.dec, star.ra, 0);
                    Vector3 normal = equatorialPos * new Vector3(0, 0, -1);
                    Color color = new Color(
                        Mathf.Clamp((float)magnitude, 0.0000001f, 1),
                        Mathf.Clamp(star.lum, 0.0000001f, 1),
                        Mathf.Clamp01(star.ci));
                    Vector3[] verts = new Vector3[]{
                        equatorialPos * Quaternion.Euler( starScale,  starScale, 0) * starDistance,
                        equatorialPos * Quaternion.Euler(-starScale,  starScale, 0) * starDistance,
                        equatorialPos * Quaternion.Euler(-starScale, -starScale, 0) * starDistance,
                        equatorialPos * Quaternion.Euler( starScale, -starScale, 0) * starDistance};
                    int[] tris = new int[] { 1, 0, 3, 3, 2, 1 };

                    // Vertices
                    for (int v = 0; v < 4; v++)
                    {
                        starMeshData[meshObjectIndex].vertices[quadIndex + v] = verts[v];
                        starMeshData[meshObjectIndex].normals[quadIndex + v] = normal;
                        starMeshData[meshObjectIndex].uv[quadIndex + v] = uv[v];
                        starMeshData[meshObjectIndex].colors[quadIndex + v] = color;
                    }

                    // Triangles
                    for (int t = 0; t < 6; t++)
                        starMeshData[meshObjectIndex].triangles[triIndex + t] = quadIndex + tris[t];

                    starIndex++;
                }

                // Star Mesh Compilation
                for (int i = 0; i < meshCount; i++)
                {
                    Mesh starMesh = new Mesh();
                    starMesh.name = "StarMesh" + i;
                    starMesh.vertices = starMeshData[i].vertices;
                    starMesh.normals = starMeshData[i].normals;
                    starMesh.colors = starMeshData[i].colors;
                    starMesh.uv = starMeshData[i].uv;
                    starMesh.triangles = starMeshData[i].triangles;
                    starMesh.RecalculateBounds();
                    starMesh.RecalculateNormals();
                    starMesh.RecalculateTangents();
                    starMeshGO[i].GetComponent<MeshFilter>().sharedMesh = starMesh;
                    starMeshGO[i].name = starMesh.name;
                }

                // Constellation Generation
                if (Resources.Load<TextAsset>(constellationDataPath) is TextAsset constellationData && constellationData)
                {
                    HashSet<int> wantedHips = GetConstellationHipIds(constellationData);
                    Dictionary<int, Vector3> hipLocations = new Dictionary<int, Vector3>();
                    Transform currentConstellation = null;

                    foreach (var star in stars)
                        if (wantedHips.Contains(star.hip) && !hipLocations.ContainsKey(star.hip))
                            hipLocations.Add(star.hip, Quaternion.Euler(star.dec, star.ra, 0) * new Vector3(0, 0, 15));

                    foreach (string line in constellationData.text.Split('\n'))
                    {
                        string[] items = line.Split(',');
                        Vector3[] verts = new Vector3[items.Length];

                        if (int.TryParse(items[0], out int temp))
                        {
                            for (int i = 0; i < items.Length; i++)
                                if (int.TryParse(items[i], out int HIP)
                                    && hipLocations.TryGetValue(HIP, out Vector3 position))
                                    verts[i] = position;

                            LineRenderer lr = Instantiate(constellationLineTemplate, currentConstellation, false).GetComponent<LineRenderer>();
                            lr.positionCount = verts.Length;
                            lr.SetPositions(verts);
                        }
                        else
                        {
                            currentConstellation = Instantiate(constellationTemplate, starCluster, false).transform;
                            currentConstellation.name = "Constellation " + items[0];
                        }
                    }

                    Resources.UnloadAsset(constellationData);
                }
            }
        }

        private static HashSet<int> GetConstellationHipIds(TextAsset file)
        {
            HashSet<int> hipIDs = new HashSet<int>();

            foreach (string line in file.text.Split('\n'))
                foreach (string item in line.Split(','))
                    if (int.TryParse(item, out int hip))
                        hipIDs.Add(hip);

            return hipIDs;
        }

        private static Quaternion SunPosition(DateTime time, float latitude, float longitude)
        {
            double julianDays = JulianDays2000Epoch(JulianDays(time));
            double meanLongitude = 280.461d + 0.9856474d * julianDays;
            double meanAnamoly = (357.528d + 0.9856003d * julianDays) * Math.PI / 180;
            double eclipticLongitude = meanLongitude + 1.915d * Math.Sin(meanAnamoly) + 0.020d * Math.Sin(2d * meanAnamoly);
            float eclipticLatitude = 0;

            return HorizontalFromEcliptic(eclipticLatitude, 180f + (float)eclipticLongitude, latitude, longitude, time);
        }

        private static Quaternion MoonPosition(DateTime time, float latitude, float longitude)
        {
            double j = JulianDays2000Epoch(JulianDays(time));
            double d = j;// JulianDaysUnknown(time);                        
            double N = Repeat(125.1228d - 0.0529538083d * d, 360d) * Math.PI / 180d; 
            double i = 5.1454d * Math.PI / 180d;                
            double w = Repeat(318.0634d + 0.1643573223d * d, 360d) * Math.PI / 180d; 
            double a = 60.2666d;                                
            double e = 0.0549d;                               
            double M = Repeat(115.3654d + 13.0649929509d * d, 360d) * Math.PI / 180d; 
            double E = LunarEccentricAnamoly(M, e);             
            double x = a * (Math.Cos(E) + e);                   
            double y = a * Math.Sqrt(1d + e * e) * Math.Sin(E); 
            double r = Math.Sqrt(x * x + y * y);                
            double v = Math.Atan2(y, x);                        
            // Rectangular Ecliptic Coordinates
            double xeclip = r * (Math.Cos(N) * Math.Cos(v + w) - Math.Sin(N) * Math.Sin(v + w) * Math.Cos(i));
            double yeclip = r * (Math.Sin(N) * Math.Cos(v + w) + Math.Cos(N) * Math.Sin(v + w) * Math.Cos(i));
            double zeclip = r * Math.Sin(v + w) * Math.Sin(i);
            // Purturbations
            {
                double eclipticLatitude = Math.Atan2(zeclip, Math.Sqrt(xeclip * xeclip + yeclip * yeclip));
                double eclipticLongitude = Math.Atan2(yeclip, xeclip);
                double ws = Repeat(282.9404d + 0.0000470935d * d, 360) * Math.PI / 180d;
                double Ms = Repeat(356.0470d + 0.9856002585d * d, 360) * Math.PI / 180d;
                double Mm = M;
                double Ls = ws + Ms;
                double Lm = N + w + M;
                double D = Lm - Ls;
                double F = Lm - N;

                eclipticLatitude += Math.PI / 180d * (
                    -1.274d * Math.Sin(Mm - 2d * D)
                    +0.658d * Math.Sin(2d * D)
                    -0.186d * Math.Sin(Ms)
                    -0.059d * Math.Sin(2d * Mm - 2d * D)
                    -0.057d * Math.Sin(Mm - 2d * D + Ms)
                    +0.053d * Math.Sin(Mm + 2d * D)
                    +0.046d * Math.Sin(2d * D - Ms)
                    +0.041d * Math.Sin(Mm - Ms)
                    -0.035d * Math.Sin(D)
                    -0.031d * Math.Sin(Mm + Ms)
                    -0.015d * Math.Sin(2d * F - 2d * D)
                    +0.011d * Math.Sin(Mm - 4d * D));

                eclipticLatitude += Math.PI / 180d * (
                    -0.173d * Math.Sin(F - 2d * D)
                    -0.055d * Math.Sin(Mm - F - 2d * D)
                    -0.046d * Math.Sin(Mm + F - 2d * D)
                    +0.033d * Math.Sin(F + 2d * D)
                    +0.017d * Math.Sin(2d * Mm + F));

                r += -0.58 * Math.Cos(Mm - 2d * D)
                     -0.46 * Math.Cos(2d * D);

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

            return HorizontalFromEquatorial(-topDecl, 180f + topRA, latitude, longitude, time);
        }

        private static double LunarEccentricAnamoly(double M, double e)
        {
            double E = M + e * Math.Sin(M) * (1d + e * Math.Cos(M));

            for (int i = 0; i < 2; i++)
                E -= (E - e * Math.Sin(E) - M) / (1d - e * Math.Cos(E));

            return E;
        }

        /* Old Methods
        public static double JulianDaysUnknown(DateTime dt)
        {
            double d = 367L * dt.Year
                     - 7L * (dt.Year + (dt.Month + 9L) / 12L) / 4L
                     - 3L * (dt.Year + (dt.Month - 9L) / 7L / 100L + 1L) / 4L
                     + 275L * dt.Month + dt.Day - 730515L;

            d += ((double)dt.Hour + ((double)dt.Minute + ((double)dt.Second + (double)dt.Millisecond / 1000d) / 60d) / 60d) / 24d;

            return d;
        }

        public static double JulianDaysCandidate(DateTime dt)
        {
            double Y = dt.Year;
            double M = dt.Month;
            double D = dt.Day;

            if (M <= 2)
            {
                M += 12;
                Y -= 1;
            }

            double A = Math.Floor(Y / 100);
            double B = Math.Floor(A / 4);
            double C = 2 - A + B;
            double E = 365.25 * (Y + 4716);
            double F = 30.6001 * (M + 1);
            double JD = C + D + E + F - 1524.5;
            JD += ((double)dt.Hour + ((double)dt.Minute + ((double)dt.Second + (double)dt.Millisecond / 1000d) / 60d) / 60d) / 24d;

            return JD;
        }
        */
    }
}
