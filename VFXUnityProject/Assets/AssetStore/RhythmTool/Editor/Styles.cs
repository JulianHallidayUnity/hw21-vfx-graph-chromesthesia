using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public class Styles : MonoBehaviour
    {        
        public static readonly GUIContent playButton = EditorGUIUtility.IconContent("Animation.Play");
        public static readonly GUIContent startButton = EditorGUIUtility.IconContent("Animation.FirstKey");
        public static readonly GUIContent endButton = EditorGUIUtility.IconContent("Animation.LastKey");

        public static readonly GUIContent autoScrollButton = new GUIContent(Resources.Load<Texture2D>("AutoScroll"));
        public static readonly GUIContent waveformButton = new GUIContent(Resources.Load<Texture2D>("Waveform"));

        public static readonly GUIStyle rulerText = "AnimationTimelineTick";

        public static readonly GUIStyle shadow;

        public static readonly GUIStyle trackText;

        public static readonly Color headerColor = new Color(.85f, .85f, .85f);
        public static readonly Color backgroundColor = new Color(.663f, .663f, .663f);
        public static readonly Color waveFormColor = new Color(.57f, .57f, .57f);

        public static readonly Color outOfBoundsColor = new Color(0, 0, 0, .1f);

        public static readonly Color trackColor = new Color(0.550f, 0.550f, 0.550f, 0.5f);
        public static readonly Color selectedTrackColor = new Color(0.550f, 0.550f, 0.7f, 0.5f);

        public static readonly GUIStyle feature;

        public static readonly float toolbarHeight = EditorStyles.toolbar.fixedHeight - 1;
        public static readonly float headerHeight = 25;

        public static readonly float trackMargin = 5;
        public static readonly float trackHeight = 100;

        public static readonly float featureWidth = 4;

        static Styles()
        {
            trackText = GUIStyle.none;            
            trackText.normal.textColor = Color.white;

            feature = new GUIStyle();
            feature.normal.background = Resources.Load<Texture2D>("Feature");
            feature.active.background = Resources.Load<Texture2D>("SelectedFeature");
            feature.border = new RectOffset(1, 1, 1, 1);

            shadow = new GUIStyle();
            shadow.normal.background = Resources.Load<Texture2D>("Shadow");

            autoScrollButton.tooltip = "Enable or disable auto scrolling";
            waveformButton.tooltip = "Enable or disable waveform";

            if (!EditorGUIUtility.isProSkin)
                return;

            autoScrollButton.image = Resources.Load<Texture2D>("AutoScroll Dark");
            waveformButton.image = Resources.Load<Texture2D>("Waveform Dark");
            
            headerColor = new Color(.216f, .216f, .216f);
            backgroundColor = new Color(.157f, .157f, .157f);

            waveFormColor = new Color(1, .549f, 0);

            trackColor.a = .1f;
            selectedTrackColor.a = .2f;
        }
    }
}