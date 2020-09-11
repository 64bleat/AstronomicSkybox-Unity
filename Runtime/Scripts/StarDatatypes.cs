using System.Collections.Generic;

namespace Astronomy
{
    /// <summary> information pertianing to a single star </summary>
    [System.Serializable]
    public struct StarData
    {
        /// <summary> Hipparcos catalog ID </summary>
        public int hip;
        /// <summary> right ascension in degrees </summary>
        public float ra;
        /// <summary> declination in degrees
        public float dec;
        /// <summary> distance in parsecs </summary>
        public float dist;
        /// <summary> apparent visual magnitude </summary>
        public float mag;
        /// <summary> color index (blue magnitude - visual magnitude) </summary>
        public float ci;
        /// <summary> luminosity as a multiple of solar luminosity </summary>
        public float lum;
    }

    /// <summary> information pertaining to a single constellation </summary>
    [System.Serializable]
    public struct ConstellationData
    {
        /// <summary> common name of the constellation </summary>
        public string name;
        /// <summary> list of star-HIP-ID lines composing the constellation </summary>
        public List<List<int>> hipLines;
    }
}
