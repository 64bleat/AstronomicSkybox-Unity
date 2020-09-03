using System.Collections.Generic;
using UnityEngine;

namespace Astronomy
{
    /// <summary>
    /// Generates star and constellation GameObjects from files containing star
    /// and constellation data
    /// </summary>
    public class StarGenerator : MonoBehaviour
    {
        [Header("Star Construction")]
        [Tooltip("Path to a star data file in a Resources folder.")]
        public string starDataPath = "Sky/StellarData";
        [Tooltip("Generated stars and constellations will be placed as children to this Transform.")]
        public Transform starContainerPrefab;
        [Tooltip("Generated stars will use this template.")]
        public MeshRenderer starMeshTemplate;
        [Tooltip("Generated stars will be set to this material.")]
        public Material starMaterial;
        [Tooltip("Generated stars will be generated in a sphere of this radius.")]
        public float starDistance = 15;
        [Tooltip("The average radius, in degrees, of star quads on the distance sphere.")]
        public float starScale = 0.075f;
        [Header("Constellation Construction")]
        public string constellationDataPath = "Sky/ConstellationData";
        [Tooltip("Sub-containers of individual constellations.")]
        public GameObject constellationTemplate;
        [Tooltip("Constellations are made of multiple LineRenderers.")]
        public LineRenderer constellationLineTemplate;

        /// <summary> triangle mapping for quads </summary>
        private static readonly int[] tQuad = new int[] { 1, 0, 3, 3, 2, 1 };

        /// <summary> raw mesh data to be compiled into individual meshes </summary>
        private struct RawVert
        {
            internal Vector3 vertex;
            internal Vector3 normal;
            internal Vector2 uv;
            internal Color color;
        }

        /// <summary> uv coordinates for star quads </summary>
        private static readonly Vector2[] quadUV = new Vector2[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0)
        };

        /// <summary>
        /// starts the star and constellation GameObject generation.
        /// Set it to a button in the inspector.
        /// </summary>
        public void Generate()
        {
            // Initialization
            StarData[] sData = StarLoader.LoadStarData(starDataPath);
            ConstellationData[] cData = StarLoader.LoadConstellationData(constellationDataPath);

            RemoveGeneratedItems();

            // Generate stars
            foreach (Mesh mesh in GenerateStarMeshes(sData, starScale, starDistance))
            {
                MeshRenderer mr = Instantiate(starMeshTemplate, starContainerPrefab, false);
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = starMaterial;
                mr.gameObject.name = mesh.name;
            }

            GenerateConstellations(sData, cData);
        }


        /// <summary> removes existing generated GameObjects </summary>
        private void RemoveGeneratedItems()
        {
            foreach (Transform child in starContainerPrefab.GetComponentsInChildren<Transform>())
                if (child != null && child != starContainerPrefab)
                    DestroyImmediate(child.gameObject);
        }

        /// <summary> 
        /// Generates an array of meshes from an array of processed star data.
        /// Each star appears as an inward facing quad around a provided radius.
        /// </summary>
        /// <param name="stars"> the set of star data to be turned into meshes </param>
        /// <param name="starScale"> The average size of stars on the star sphere</param>
        /// <param name="starDistance"> distance from the center of the mesh</param>
        /// <returns> an array of star meshes </returns>
        private static Mesh[] GenerateStarMeshes(StarData[] stars, float starScale = 0.2f, float starDistance = 15f)
        {
            int starCount = 10000;
            Vector3 starOffset = new Vector3(0, 0, starDistance);
            Mesh[] meshes = new Mesh[Mathf.CeilToInt((float)stars.Length / starCount)];
            RawVert[] rawVerts = new RawVert[stars.Length * 4];

            // Generate raw mesh data
            for(int s = 0; s < stars.Length; s++)
            {
                float quadScale = starScale * Mathf.Max(0.25f, Mathf.Pow(Mathf.Pow(stars[s].lum, 0.125f), 2));
                Quaternion quadCoordinates = Quaternion.Euler(stars[s].dec, stars[s].ra, 0);
                Vector3 quadNormal = quadCoordinates * new Vector3(0, 0, -1);
                Color quadColor = new Color(
                    Mathf.Clamp01(stars[s].mag),
                    Mathf.Clamp01(stars[s].lum),
                    Mathf.Clamp01(stars[s].ci));
                Vector3[] quadVerts = new Vector3[]{
                    quadCoordinates * Quaternion.Euler( quadScale,  quadScale, 0) * starOffset,
                    quadCoordinates * Quaternion.Euler(-quadScale,  quadScale, 0) * starOffset,
                    quadCoordinates * Quaternion.Euler(-quadScale, -quadScale, 0) * starOffset,
                    quadCoordinates * Quaternion.Euler( quadScale, -quadScale, 0) * starOffset};

                // Vertices
                for(int v = s * 4, e = v + 4; v < e; v++)
                {
                    rawVerts[v].vertex = quadVerts[v - s * 4];
                    rawVerts[v].normal = quadNormal;
                    rawVerts[v].uv = quadUV[v - s * 4];
                    rawVerts[v].color = quadColor;
                }
            }

            //Generate final meshes
            for(int m = 0; m < meshes.Length; m++)
            {
                int rStart = m * starCount * 4;
                int rEnd = Mathf.Min(rawVerts.Length, rStart + starCount * 4);
                int rLength = rEnd - rStart;
                Vector3[] mVertices = new Vector3[rLength];
                Vector3[] mNormals = new Vector3[rLength];
                Vector2[] mUV = new Vector2[rLength];
                Color[] mColor = new Color[rLength];
                int[] mTriangles = new int[rLength / 4 * 6];

                // Verteces
                for (int v = 0; v < rLength; v++)
                {
                    mVertices[v] = rawVerts[rStart + v].vertex;
                    mNormals[v] = rawVerts[rStart + v].normal;
                    mUV[v] = rawVerts[rStart + v].uv;
                    mColor[v] = rawVerts[rStart + v].color;
                }

                // Triangles
                for (int t = 0; t < mTriangles.Length; t++)
                    mTriangles[t] = t / 6 * 4 + tQuad[t % 6];

                // Meshes
                meshes[m] = new Mesh() {
                    name = "StarMesh" + m,
                    vertices = mVertices,
                    normals = mNormals,
                    uv = mUV,
                    colors = mColor,
                    triangles = mTriangles
                };

                meshes[m].RecalculateBounds();
                meshes[m].RecalculateNormals();
                meshes[m].RecalculateTangents();
            }

            return meshes;
        }
        
        /// <summary> Generates a set of constellations as GameObjects </summary>
        /// <param name="sData"> provides needed position data for hip ids </param>
        /// <param name="cData"> provides lines of hip ids associated with constellations</param>
        private void GenerateConstellations(StarData[] sData, ConstellationData[] cData)
        {
            HashSet<int> wantedHips = new HashSet<int>();
            Dictionary<int, Vector3> hipPositions = new Dictionary<int, Vector3>();
            Vector3 starOffset = new Vector3(0, 0, starDistance);

            // Get hips that need position data.
            foreach (ConstellationData constellation in cData)
                foreach (List<int> line in constellation.hipLines)
                    foreach (int hip in line)
                        wantedHips.Add(hip);

            // Get hip positions
            foreach (StarData star in sData)
                if (wantedHips.Contains(star.hip))
                    hipPositions.Add(star.hip, Quaternion.Euler(star.dec, star.ra, 0) * starOffset);

            // Generate constellations
            foreach (ConstellationData constellation in cData)
            {
                GameObject container = Instantiate(constellationTemplate, starContainerPrefab, false);

                container.name = constellation.name;

                foreach (List<int> line in constellation.hipLines)
                {
                    LineRenderer lr = Instantiate(constellationLineTemplate, container.transform, false);
                    Vector3[] positions = new Vector3[line.Count];

                    for (int i = 0; i < line.Count; i++)
                        if (hipPositions.TryGetValue(line[i], out Vector3 p))
                            positions[i] = p;

                    lr.positionCount = line.Count;
                    lr.SetPositions(positions);
                }
            }
        }
    }
}
