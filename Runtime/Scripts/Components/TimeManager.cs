using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astronomy
{
    public enum TimeUnit { Days, Hours, Minutes, Seconds }

    /// <summary>
    /// Manage game time and schedule Actions to pe taken at certain times.
    /// </summary>
    public class TimeManager : MonoBehaviour
    {
        public string startTime;
        public bool startAtRealTime = true;
        public float initialTimeScale = 1f;

        public static DateTime currentTime = new DateTime();
        public static float timeScale = 1f;

        private void OnValidate()
        {
            if (!startAtRealTime)
            {
                if (!DateTime.TryParse(startTime, out DateTime result))
                    result = DateTime.Now;

                currentTime = result;
                startTime = result.ToString("s");
            }
        }

        private void Awake()
        {
            if (startAtRealTime || !DateTime.TryParse(startTime, out DateTime result))
                result = DateTime.Now;

            currentTime = result;
            timeScale = initialTimeScale;
        }

        private void Update()
        {
            currentTime = currentTime.AddSeconds(Time.deltaTime * timeScale);
        }

        public static void SetCurrentTime(DateTime t)
        {
            currentTime = t;
        }
    }
}
