using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SHK.Astronomy
{
    [CustomEditor(typeof(AstronomicPositioner))]
    public class AstronomicPositionerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("GenerateStars"))
            {
                AstronomicPositioner t = (AstronomicPositioner)target;
                t.GenerateStarMeshes();
            }
        }
    }
}
