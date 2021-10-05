using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{   
    public class EditorState
    {
        public float start;

        public float length;

        public float pixelsPerSecond;

        public bool featureClicked;
        public bool trackClicked;
        public bool manipulatorClicked;

        public bool dragging;

        public bool refresh;

        public Vector2 mouseDownPosition;

        public float TimestampToPixels(float timestamp)
        {
            return (timestamp - start) * pixelsPerSecond;
        }

        public float PixelsToTimestamp(float pixels)
        {
            return (pixels / pixelsPerSecond) + start;
        }
    }

    public class ClipBoard
    {
        public static object value;
    }

    public class RhythmToolWindow : EditorWindow
    {
        public static RhythmToolWindow instance
        {
            get
            {
                GetInstance();
                return _instance;
            }
        }

        private static RhythmToolWindow _instance;

        public EditorState state { get; private set; }

        private RhythmData rhythmData;

        private AudioClip audioClip;

        private WaveformView waveFormView;
        
        private TimeRuler timeRuler;

        private List<TrackGUI> tracks;

        [SerializeField]
        private float start = 0;

        [SerializeField]
        private float length = 10;

        [SerializeField]
        private float volume = .2f;

        [SerializeField]
        private bool isPlaying;

        [SerializeField]
        private float playHead;

        [SerializeField]
        private bool autoScroll;

        [SerializeField]
        private bool waveform;

        private bool scrubbing;
        private bool panning;

        private double scrubTime;
        private float prevTime;

        private Vector2 contextClick;

        private List<Type> featureTypes;

        private float yPosition;

        private void OnEnable()
        {
            _instance = this;

            waveFormView = new WaveformView();

            timeRuler = new TimeRuler();

            tracks = new List<TrackGUI>();

            state = new EditorState();

            if (rhythmData != null)
                SetRhythmData(rhythmData);
            
            featureTypes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(Feature)))
                        featureTypes.Add(type);
                }
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            waveFormView.Dispose();

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnSelectionChange()
        {
            if (!state.featureClicked)
                FeatureSelection.Clear();
            
            if (Selection.activeObject == rhythmData)
                return;

            if (Selection.activeObject is RhythmData)
                SetRhythmData(Selection.activeObject as RhythmData);
        }

        private void Update()
        {
            if (isPlaying || Selection.activeObject is Track)
                Repaint();

            if (scrubbing && EditorApplication.timeSinceStartup - scrubTime > .05f)
                AudioUtil.volume = 0;
            else
                AudioUtil.volume = volume;
        }

        private void OnGUI()
        {
            if (rhythmData == null && tracks.Count > 0)
                Refresh();

            if (rhythmData != null && rhythmData.audioClip != audioClip)
                Refresh();

            if(audioClip == null && isPlaying)
            {
                AudioUtil.Stop();
                isPlaying = false;
            }

            start = Mathf.Max(start, -10);

            state.start = start;
            state.length = length;

            state.pixelsPerSecond = position.width / length;
            
            TimeAreaGUI();
            TracksGUI();

            Toolbar();            
            PlayHead();
            
            HandleMouseDown();
            HandleMouseDrag();
            HandleMouseUp();
            HandleScroll();

            HandleRepaint();
            
            if (rhythmData == null)
                return;

            HandleKeys();
            HandleCommands();
            HandleContextClick();
        }

        private void TimeAreaGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Rect rect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(rect, Styles.backgroundColor);

            rect = new Rect(0, Styles.toolbarHeight, position.width, Styles.headerHeight);
            EditorGUI.DrawRect(rect, Styles.headerColor);

            rect = new Rect(0, Styles.toolbarHeight, position.width, Styles.headerHeight - 1);

            timeRuler.SetRange(start, start + length);
            timeRuler.Draw(rect);

            rect = new Rect(0, Styles.toolbarHeight + Styles.headerHeight + 10, position.width, Styles.trackHeight);
            
            waveFormView.waveFormColor = Styles.waveFormColor;
            waveFormView.backgroundColor = Styles.backgroundColor;

            if (waveform)                
                waveFormView.Draw(rect, start, length);

            rect = new Rect(0, Styles.toolbarHeight + Styles.headerHeight, position.width, position.height - Styles.toolbarHeight - Styles.headerHeight);

            timeRuler.DrawMajorTicks(rect);

            rect = new Rect(0, Styles.toolbarHeight + Styles.headerHeight, position.width, 8);
            GUI.Box(rect, GUIContent.none, Styles.shadow);
        }

        private void TracksGUI()
        {
            using (new GUI.GroupScope(new Rect(0, Styles.toolbarHeight + Styles.headerHeight, position.width, position.height)))
            {
                Rect rect = new Rect(0, yPosition + 10, position.width, Styles.trackHeight);

                foreach (var trackGUI in tracks)
                {
                    trackGUI.OnGUI(rect, state);

                    Rect shadow = new Rect(rect.x, rect.y + rect.height, rect.width, Styles.trackMargin);
                    GUI.Box(shadow, GUIContent.none, Styles.shadow);

                    rect.y += Styles.trackHeight + Styles.trackMargin;
                }
            }
        }

        private void Toolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUI.DisabledGroupScope(audioClip == null))
                {
                    StartButton();
                    PlayButton();
                    EndButton();
                }

                VolumeSlider();

                Rect rect = EditorGUILayout.GetControlRect(false, Styles.toolbarHeight, GUIStyle.none, GUILayout.Width(1));
                EditorGUI.DrawRect(rect, Color.gray);

                EditorGUILayout.TextField(FormatTime(playHead), EditorStyles.toolbarTextField, GUILayout.Width(50));

                //rect = EditorGUILayout.GetControlRect(false, Styles.toolbarHeight, GUIStyle.none, GUILayout.Width(1));
                //EditorGUI.DrawRect(rect, Color.gray);

                //GUILayout.Space(10);

                autoScroll = GUILayout.Toggle(autoScroll, Styles.autoScrollButton, EditorStyles.toolbarButton);
                waveform = GUILayout.Toggle(waveform, Styles.waveformButton, EditorStyles.toolbarButton);

                GUILayout.FlexibleSpace();

                AssetButton();
            }
        }

        private static string FormatTime(float time)
        {
            return string.Format("{0:F2}", time).Replace('.', ':');
        }

        private void PlayButton()
        {
            bool playingToggle = GUILayout.Toggle(isPlaying, Styles.playButton, EditorStyles.toolbarButton);

            if (isPlaying != playingToggle)
                TogglePlay();
        }

        private void StartButton()
        {
            if (GUILayout.Button(Styles.startButton, EditorStyles.toolbarButton))
            {
                AudioUtil.time = 0;
                playHead = 0;
            }
        }

        private void EndButton()
        {
            if (GUILayout.Button(Styles.endButton, EditorStyles.toolbarButton))
            {
                AudioUtil.time = audioClip.length - .0001f;
                playHead = audioClip.length - .0001f;
            }
        }

        private void VolumeSlider()
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(100));
            rect.y -= 1;

            volume = GUI.HorizontalSlider(rect, volume, 0, 1);

            if (!scrubbing)
                AudioUtil.volume = volume;
        }

        private void AssetButton()
        {
            if (rhythmData == null)
                return;
            
            Rect rect = EditorGUILayout.GetControlRect(false, Styles.toolbarHeight, GUIStyle.none, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, Color.gray);                       

            if (GUILayout.Button(rhythmData.name, EditorStyles.label))
                Selection.activeObject = rhythmData;

            rect = EditorGUILayout.GetControlRect(false, Styles.toolbarHeight, GUIStyle.none, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, Color.gray);
        }

        private void HandleMouseDown()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown)
                return;

            if(currentEvent.button == 2)
            {
                panning = true;
            }

            if(currentEvent.button == 0)
            {
                if (audioClip == null)
                    return;

                Rect rect = new Rect(0, Styles.toolbarHeight, position.width, Styles.headerHeight);

                if (rect.Contains(Event.current.mousePosition))
                {
                    scrubbing = true;
                    currentEvent.Use();
                }
            }
        }

        private void HandleMouseDrag()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDrag)
                return;
            
            if(panning)
            {
                start -= currentEvent.delta.x / state.pixelsPerSecond;
                yPosition = yPosition + currentEvent.delta.y;

                float height = (Styles.trackHeight + Styles.trackMargin) * tracks.Count;

                yPosition = Mathf.Max(position.height - Styles.toolbarHeight - Styles.headerHeight - 10 - height, yPosition);
                yPosition = Mathf.Min(0, yPosition);
                
                currentEvent.Use();
            }

            if(scrubbing)
            {
                if (!AudioUtil.isPlaying)
                    AudioUtil.Play();

                scrubTime = EditorApplication.timeSinceStartup;

                SetPlayHead(currentEvent.mousePosition.x);
                currentEvent.Use();
            }
        }

        private void HandleMouseUp()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseUp && currentEvent.type != EventType.Ignore)
                return;

            if (!scrubbing && !panning)
                return;

            if(scrubbing)
            {
                AudioUtil.volume = volume;

                if (isPlaying)
                    AudioUtil.UnPause();
                else
                    AudioUtil.Pause();

                SetPlayHead(currentEvent.mousePosition.x);
            }

            prevTime = playHead;

            scrubbing = false;
            panning = false;

            currentEvent.Use();
        }

        private void HandleScroll()
        {
            var currentEvent = Event.current;

            if (currentEvent.type != EventType.ScrollWheel)
                return;

            float zoomFactor = .9f;

            if (currentEvent.delta.y > 0)
                zoomFactor = 1 / zoomFactor;
            
            if (length < .1f && zoomFactor < 1)
                return;

            if (length > 120 && zoomFactor > 1)
                return;

            float zoomIncrement = (length * zoomFactor) - length;

            length += zoomIncrement;
            start -= zoomIncrement * (currentEvent.mousePosition.x / position.width);

            if (Mathf.Abs(length - 10) < .0001f)
                length = 10;

            currentEvent.Use();
        }

        private void HandleRepaint()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.Repaint)
                return;

            Rect cursorRect = new Rect(0, 0, position.width, position.height);

            if (scrubbing)            
                EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.SlideArrow);

            if (panning)
                EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Pan);
            
            if (start < 0)
            {
                Rect rect = new Rect(0, Styles.toolbarHeight, -start * state.pixelsPerSecond, position.height);
                EditorGUI.DrawRect(rect, Styles.outOfBoundsColor);
            }

            if (audioClip == null)
                return;

            if (start + length > audioClip.length)
            {
                Rect rect = new Rect(position.width, Styles.toolbarHeight, state.TimestampToPixels(audioClip.length) - position.width, position.height);
                EditorGUI.DrawRect(rect, Styles.outOfBoundsColor);
            }
        }

        private void PlayHead()
        {
            if (audioClip == null)
                return;
            
            if (!scrubbing && isPlaying)
                playHead = AudioUtil.time;

            if (!scrubbing && autoScroll)
                start += playHead - prevTime;

            if (isPlaying && !AudioUtil.isPlaying)
                isPlaying = false;

            prevTime = playHead;
            
            float playHeadPos = (playHead - start) * (position.width / length);

            Rect rect = new Rect(playHeadPos, Styles.toolbarHeight, 1, position.height);

            EditorGUI.DrawRect(rect, Color.white);
        }       

        private void SetPlayHead(float x)
        {
            playHead = state.PixelsToTimestamp(x);
            playHead = Mathf.Clamp(playHead, 0, audioClip.length - .0001f);

            float time = playHead - .03f;

            if (Mathf.Abs(AudioUtil.time - time) > .02f)
                AudioUtil.time = Mathf.Max(time, 0);
        }

        private void HandleKeys()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.KeyDown)
                return;

            if (currentEvent.keyCode == KeyCode.Space)
            {
                TogglePlay();
                currentEvent.Use();
            }
        }

        private void TogglePlay()
        {
            isPlaying = !isPlaying;

            if (isPlaying)
                AudioUtil.Play();
            else
                AudioUtil.Pause();

            if (audioClip.length - playHead < .01f)
                playHead = 0;

            AudioUtil.time = playHead;
        }

        private void HandleContextClick()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.ContextClick)
                return;

            contextClick = currentEvent.mousePosition;

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Copy %c"), false, () => Sendcommand("Copy"));
            menu.AddItem(new GUIContent("Paste %v"), false, () => Sendcommand("Paste"));
            menu.AddItem(new GUIContent("Duplicate %d"), false, () => Sendcommand("Duplicate"));
            menu.AddItem(new GUIContent("Delete \t Delete"), false, () => Sendcommand("Delete"));

            menu.AddSeparator("");

            foreach (Type type in featureTypes)
                menu.AddItem(new GUIContent(string.Format("New Track / {0} Track", type.Name)), false, () => CreateTrack(type));

            if (Selection.activeObject is Track)
                menu.AddItem(new GUIContent("Add Feature"), false, () => Sendcommand("AddFeature"));

            menu.ShowAsContext();
        }

        private void Sendcommand(string command)
        {
            Event e = EditorGUIUtility.CommandEvent(command);

            e.mousePosition = contextClick;

            //20 px offset when sending command? Maybe from toolbar layout
            e.mousePosition += new Vector2(0, 20);

            SendEvent(e);
        }

        private void HandleCommands()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.ValidateCommand)
                currentEvent.Use();

            if (currentEvent.type != EventType.ExecuteCommand)
                return;

            switch (currentEvent.commandName)
            {
                case "Delete":
                case "SoftDelete":
                    DeleteTrack();
                    break;
                case "Cut":
                    break;
                case "Copy":
                    CopyTrack();
                    break;
                case "Paste":
                    PasteTrack();
                    break;
                case "Duplicate":
                    CopyTrack();
                    PasteTrack();
                    break;
                case "SelectAll":
                    break;               
            }

            currentEvent.Use();
        }

        private void CopyTrack()
        {
            Track track = Selection.activeObject as Track;

            if (track == null)
                return;

            ClipBoard.value = track;
        }

        private void PasteTrack()
        {
            Track track = ClipBoard.value as Track;

            if (track == null)
                return;

            Track copied = Instantiate(track);
            copied.hideFlags = track.hideFlags;

            Undo.RecordObject(rhythmData, "Paste Track");
            Undo.RegisterCreatedObjectUndo(copied, "Paste Track");

            rhythmData.tracks.Add(copied);
            AssetDatabase.AddObjectToAsset(copied, rhythmData);

            Refresh();
        }

        private void CreateTrack(Type featureType)
        {
            Type trackType = typeof(Track<>).MakeGenericType(featureType);
            var createTrack = trackType.GetMethod("Create");

            string name = string.Format("New {0} Track", featureType.Name);

            Track track = createTrack.Invoke(null, new object[] { name }) as Track;

            Undo.RecordObject(rhythmData, name);
            Undo.RegisterCreatedObjectUndo(track, name);

            int index = rhythmData.tracks.Count;

            if (Selection.activeObject is Track)
                index = rhythmData.tracks.IndexOf(Selection.activeObject as Track) + 1;

            rhythmData.tracks.Insert(index, track);

            AssetDatabase.AddObjectToAsset(track, rhythmData);

            Refresh();
        }

        private void DeleteTrack()
        {
            Track track = Selection.activeObject as Track;

            if (track == null)
                return;

            Undo.RecordObject(rhythmData, "Delete Track");

            rhythmData.tracks.Remove(track);

            Undo.DestroyObjectImmediate(track);

            Refresh();
        }

        private void OnUndoRedoPerformed()
        {
            Refresh();
        }

        private void Refresh()
        {
            audioClip = rhythmData == null ? null : rhythmData.audioClip;

            waveFormView.SetAudioClip(audioClip);

            if(audioClip != null)
                playHead = Mathf.Min(playHead, audioClip.length - .0001f);

            if (!isPlaying)
                AudioUtil.time = playHead;

            AudioUtil.audioCLip = audioClip;

            tracks.Clear();

            if (rhythmData == null)
                return;

            foreach (Track track in rhythmData)
                tracks.Add(Bindings<TrackGUI>.GetBinding(track));
        }
        
        private void SetRhythmData(RhythmData rhythmData)
        {
            AudioUtil.Stop();

            isPlaying = false;

            this.rhythmData = rhythmData;

            Refresh();
            Repaint();
        }
        
        [MenuItem("Window/RhythmTool", false)]
        public static void ShowWindow()
        {
            _instance = GetWindow<RhythmToolWindow>("♫ RhythmTool", true, typeof(SceneView));
        }

        public static void OpenRhythmData(RhythmData rhythmData)
        {
            instance.SetRhythmData(rhythmData);
            instance.Focus();
        }

        private static void GetInstance()
        {
            if (_instance == null)
                ShowWindow();
        }
    }    
}