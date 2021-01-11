using System;
using UnityEngine;

namespace Astronomy
{
    public class StarPositioner : MonoBehaviour
    {
        [Header("Rotators")]
        public Transform moonRotator;
        public Transform sunRotator;
        public Transform equatorialCoordinates;
        public Transform eclipticCoordinates;
        public Transform ambientCluster;
        [Header("Lighting")]
        public Material starMaterial;
        public Light sunLight;
        public Light moonLight;
        public Light[] ambientLights;
        [Tooltip("Maximum brightness of the sun")]
        public float sunIntensity = 1f;
        public float moonIntensity = 0.25f;
        public float ambientIntensity = 0.02f;
        public Color dayColor = new Color(1, 1, 1);
        public Color duskColor = new Color(1f, 0.9f, 0.8f);
        public Color dayFogColor;
        public Color nightFogColor;
        [Header("Location")]
        public float latitude = 48f;
        public float longitude = -122f;

        private DateTime lastTime;

        private void OnValidate()
        {
            SetSky(lastTime = TimeManager.currentTime.ToUniversalTime());
        }

        private void Awake()
        {
            SetSky(lastTime = TimeManager.currentTime.ToUniversalTime());
        }

        private void Update()
        {
            DateTime sampleTime = TimeManager.currentTime.ToUniversalTime();

            if(lastTime != sampleTime)
                SetSky(lastTime = sampleTime);
        }

        public void SetSky(DateTime time)
        {
            // Rotations
            if (equatorialCoordinates && equatorialCoordinates.gameObject.activeSelf)
                equatorialCoordinates.rotation = StarMath.HorizontalFromEquatorial(0, 0, latitude, longitude, time);
            if (eclipticCoordinates && eclipticCoordinates.gameObject.activeSelf)
                eclipticCoordinates.rotation = StarMath.HorizontalFromEcliptic(0, 0, latitude, longitude, time);
            if(sunRotator && sunRotator.gameObject.activeSelf)
                sunRotator.rotation = StarMath.SolarPosition(time, latitude, longitude);
            if(moonRotator && moonRotator.gameObject.activeSelf)
                moonRotator.rotation = StarMath.LunarPosition(time, latitude, longitude);
            if(ambientCluster && ambientCluster.gameObject.activeSelf)
                ambientCluster.rotation = Quaternion.Euler(0, sunRotator.rotation.y, 0);

            // Sunlight
            if (sunRotator && sunLight)
            {
                float sunAlt = Vector3.Angle(sunLight.transform.rotation * transform.forward, transform.up) - 90f;
                float sunBrightness = sunIntensity;

                if (sunLight.gameObject.activeSelf)
                    sunBrightness *= Mathf.Clamp01(sunAlt / 15f);
                else
                    sunBrightness *= 0;

                sunLight.color = Color.Lerp(duskColor, dayColor, Mathf.Clamp01(sunBrightness / 0.5f));
                sunLight.intensity = sunBrightness;

                if (RenderSettings.fog)
                    RenderSettings.fogColor = Color.Lerp(nightFogColor, dayFogColor, Mathf.Clamp01(sunBrightness / 0.5f));

                // Moonlight
                if (moonLight)
                {
                    float moonAlt = Vector3.Angle(moonRotator.rotation * transform.forward, transform.up) - 90f;

                    moonLight.intensity = (1f - sunBrightness / sunIntensity) * Mathf.Clamp01((-moonAlt + 5f) / 5f) * (1f - Quaternion.Angle(moonRotator.rotation, sunLight.transform.rotation) / 180f) * moonIntensity;
                }

                // Starlight
                if (starMaterial)
                    starMaterial.SetFloat("_Scale", Mathf.Pow(1f - Mathf.Clamp01(sunBrightness * 10), 2));

                // AmbientLight
                if (ambientLights.Length != 0)
                {
                    float ambintensity = Mathf.Pow(Mathf.Clamp01((sunAlt + 12f) / 8f), 2f) * ambientIntensity;
                    for (int i = 0; i < ambientLights.Length; i++)
                        ambientLights[i].intensity = ambintensity * (i == 0 ? 2 : 1);
                }
            }
        }
    }
}
