using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Astronomy
{
    public static class StarLoader
    {
        /// <summary> Loads useable star data from CSV files. </summary>
        /// <param name="starDataPath">the path to a star data file in a <b>Resources</b> folder </param>
        /// <returns> an array of starData, one entry per star</returns>
        public static StarData[] LoadStarData(string starDataPath)
        {
            StarData[] starData = null;

            if (Resources.Load<TextAsset>(starDataPath) is TextAsset starText && starText)
            {
                starData = (from line in starText.text.Split('\n')
                            let data = line.Split(',')
                            where data.Length > 33
                            let hip = ParseFloat(data[1]) //Coluns currently hardwired.
                            let ra = ParseFloat(data[7])
                            let dec = ParseFloat(data[8])
                            let dist = ParseFloat(data[9])
                            let mag = ParseFloat(data[13])
                            let ci = ParseFloat(data[17])
                            let lum = ParseFloat(data[33])
                            where ra != null
                                 && dec != null
                                 && dist != null
                                 && mag != null
                                 && ci != null
                                 && lum != null
                            orderby mag ascending
                            select new StarData()
                            {
                                hip = (int)(hip ?? -1),         // -1 if invalid
                                ra = (float)((ra ?? 0) * 15),   // 24h to 360deg
                                dec = (float)(dec ?? 0),
                                dist = (float)(dist ?? 0),
                                mag = (float)(mag ?? 0),
                                ci = (float)((ci ?? 0) / 200000d + 0.5d), // Map to 01 
                                lum = (float)((lum ?? 0) / Math.Pow(1d + (dist ?? 0), 2) / 1.75d)
                            }).ToArray();

                Resources.UnloadAsset(starText);
            }

            return starData ?? new StarData[0];
        }

        /// <summary> Load useable constellation data from text files </summary>
        /// <remarks>file format:<code>
        /// ConstellationName1
        /// 10231,31231,12323 (these are star HIP IDs)
        /// 12312,1231,231231,231231
        /// +ConstellationName2
        /// 31234,213423,2342
        /// +ConstellationName3
        /// and so on...
        /// </code></remarks>
        /// <param name="constellationDataPath"></param>
        /// <returns></returns>
        public static ConstellationData[] LoadConstellationData(string constellationDataPath)
        {
            List<ConstellationData> constellations = new List<ConstellationData>();

            if (Resources.Load<TextAsset>(constellationDataPath) is TextAsset cText && cText)
            {
                ConstellationData constellation = default;

                foreach (string conSet in cText.text.Split('+'))
                {
                    string[] lines = conSet.Split('\n');

                    constellation.name = lines[0];
                    constellation.hipLines = new List<List<int>>();

                    for (int i = 1; i < lines.Length; i++)
                    {
                        List<int> line = new List<int>();

                        foreach (string hipstring in lines[i].Split(','))
                            if (int.TryParse(hipstring, out int hip))
                                line.Add(hip);

                        constellation.hipLines.Add(line);
                    }

                    constellations.Add(constellation);
                }

                Resources.UnloadAsset(cText);
            }

            return constellations.ToArray();
        }

        /// <summary> Compact float parser </summary>
        /// <param name="s"> the string to parse </param>
        /// <returns> parsed float, or null if s could not be parsed </returns>
        private static float? ParseFloat(string s) => float.TryParse(s, out float f) ? (float?)f : null;
    }
}
