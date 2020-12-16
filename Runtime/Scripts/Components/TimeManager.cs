using System;
using UnityEngine;

namespace Astronomy
{
    public enum TimeUnit { Days, Hours, Minutes, Seconds }

    /// <summary>
    /// Manage game time and schedule Actions to pe taken at certain times.
    /// </summary>
    [ExecuteInEditMode]
    public class TimeManager : MonoBehaviour
    {
        public string viewVurrentTime;
        public float timeScale = 1;
        public bool isRealtime = true;
        public DateTimeInspector startDate;

        public static DateTime currentTime = new DateTime();
        public static DateTime CurrentUniversalTime => currentTime.ToUniversalTime();

        [System.Serializable]
        public struct DateTimeInspector
        {
            public int year;
            public int month;
            public int day;
            public float hour;

            public static implicit operator DateTime(DateTimeInspector d)
                => new DateTime(d.year, d.month, d.day).AddHours(d.hour);
        }

        private void OnValidate()
        {
            if (isRealtime)
                SetCurrentTime(DateTime.Now);
            else
                SetCurrentTime(startDate);

            viewVurrentTime = currentTime.ToLongTimeString();
        }

        private void Awake()
        {
            if (isRealtime)
                SetCurrentTime(DateTime.Now);
            else
                SetCurrentTime(startDate);
        }

        private void Update()
        {
            SetCurrentTime(currentTime.AddSeconds(Time.deltaTime * timeScale));

        }

        public static DateTime SetCurrentTime(DateTime t)
        {
            currentTime = t;

            return currentTime;
        }
    }
}
