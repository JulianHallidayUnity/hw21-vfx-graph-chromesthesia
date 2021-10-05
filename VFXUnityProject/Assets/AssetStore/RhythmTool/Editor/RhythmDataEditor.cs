using UnityEditor;
using UnityEditor.Callbacks;

namespace RhythmTool
{
    [CustomEditor(typeof(RhythmData))]
    public class RhythmDataEditor : Editor
    {
        private RhythmData rhythmData;

        private SerializedProperty audioClip;        

        private void OnEnable()
        {
            rhythmData = target as RhythmData;

            audioClip = serializedObject.FindProperty("audioClip");           
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(audioClip);

            EditorGUILayout.LabelField(rhythmData.tracks.Count + " tracks");

            serializedObject.ApplyModifiedProperties();
        }        

        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            RhythmData rhythmData = EditorUtility.InstanceIDToObject(instanceID) as RhythmData;

            if (rhythmData == null)
                return false;

            RhythmToolWindow.OpenRhythmData(rhythmData);

            return true;
        }
    }
}