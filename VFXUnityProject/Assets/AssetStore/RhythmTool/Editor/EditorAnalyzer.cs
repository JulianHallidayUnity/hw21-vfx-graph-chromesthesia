using System.IO;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public class EditorAnalyzer
    {
        private AudioClip audioClip;

        private RhythmAnalyzer analyzer;

        public EditorAnalyzer(AudioClip audioClip)
        {
            this.audioClip = audioClip;

            analyzer = GetAnalyzer();

            if (analyzer == null)
            {
                Debug.LogWarning("Could not find Analyzer.");
                return;
            }

            analyzer.Analyze(audioClip);

            EditorApplication.update += Update;
            EditorApplication.LockReloadAssemblies();
        }

        private void Update()
        {
            if (analyzer.isDone)
            {
                CreateAsset();
                Cleanup();

                return;
            }
            
            float completion = Mathf.Round(analyzer.progress * 1000) / 10;

            if (EditorUtility.DisplayCancelableProgressBar("Analyzing " + audioClip.name, completion + "%", analyzer.progress))
            {
                analyzer.Abort();
                Object.DestroyImmediate(analyzer.rhythmData);
                Cleanup();
            }
        }

        private void Cleanup()
        {
            EditorUtility.ClearProgressBar();

            EditorApplication.update -= Update;
            EditorApplication.UnlockReloadAssemblies();
        }

        private void CreateAsset()
        {
            string path = AssetDatabase.GetAssetPath(audioClip);
            path = Path.GetDirectoryName(path);

            string[] existingNames = AssetDatabase.FindAssets("t:RhythmData " + audioClip.name, new[] {path} );

            for (int i = 0; i < existingNames.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(existingNames[i]);
                existingNames[i] = Path.GetFileNameWithoutExtension(assetPath);
            }

            string name = ObjectNames.GetUniqueName(existingNames, audioClip.name);

            path = Path.Combine(path, name + ".asset");

            RhythmData asset = analyzer.rhythmData;

            AssetDatabase.CreateAsset(asset, path);

            foreach (Track track in asset)
                AssetDatabase.AddObjectToAsset(track, asset);

            EditorUtility.SetDirty(asset);
        }
        
        private static RhythmAnalyzer GetAnalyzer()
        {
            RhythmAnalyzer[] analyzers = Object.FindObjectsOfType<RhythmAnalyzer>();

            for (int i = 0; i < analyzers.Length; i++)
            {
                if (analyzers[i].gameObject.activeSelf)
                    return analyzers[i];
            }

            return null;
        }

        [MenuItem("Assets/RhythmTool/Analyze")]
        private static void Analyze()
        {
            AudioClip clip = Selection.activeObject as AudioClip;

            new EditorAnalyzer(clip);
        }

        [MenuItem("Assets/RhythmTool/Analyze", validate = true)]
        private static bool VerifyAnalyze()
        {
            return Selection.activeObject is AudioClip;
        }
    }
}