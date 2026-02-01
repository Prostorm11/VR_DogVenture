using UnityEngine;
using UnityEditor;

namespace VRDogVenture.Dog
{
    [CustomEditor(typeof(DogSetupHelper))]
    public class DogSetupHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DogSetupHelper helper = (DogSetupHelper)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Create Dog Companion", GUILayout.Height(40)))
            {
                helper.SetupDog();
            }

            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Click 'Create Dog Companion' to create a dog with all required components.\n\n" +
                "The dog will:\n" +
                "- Follow the player on the right side\n" +
                "- React to correct/incorrect answers\n" +
                "- Bark to dismiss bees when you get points after being stung\n" +
                "- Wag its tail when happy\n\n" +
                "After creation, assign the player camera in the DogCompanion component.",
                MessageType.Info
            );
        }
    }
}
