using UnityEngine;
using UnityEditor;

namespace Astronomy
{
    [CustomEditor(typeof(StarGenerator))]
    public class StarGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(GUILayout.Button("Generate Stars"))
                (target as StarGenerator).Generate();
        }
    }
}
