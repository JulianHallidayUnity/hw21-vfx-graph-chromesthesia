using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public abstract class TrackGUI
    {
        public abstract void OnGUI(Rect rect, EditorState state);
    }

    public class TrackGUI<T> : TrackGUI where T : Feature, new()
    {
        private Track<T> track;

        private FeatureCreator<T> featureCreator;

        private List<FeatureGUI> features;

        private int startIndex;
        private int endIndex;
        
        private Rect rect;

        private EditorState state;

        public TrackGUI(Track<T> track)
        {
            this.track = track;

            featureCreator = Bindings<FeatureCreator<T>>.GetBinding(track);

            features = new List<FeatureGUI>();

            Refresh();                
        }
        
        public override void OnGUI(Rect rect, EditorState state)
        {
            this.state = state;

            RefreshIfNecessary();

            startIndex = track.GetIntersectingIndex(state.start);
            endIndex = track.GetIndex(state.start + state.length);

            using (new GUI.GroupScope(rect))
            {
                rect.x = 0;
                rect.y = 0;
                
                DrawTrack(rect);

                rect.y += 2;
                rect.height -= 4;

                using (new GUI.GroupScope(rect))
                {
                    rect.x = 0;
                    rect.y = 0;

                    this.rect = rect;

                    for (int i = startIndex; i < endIndex; i++)
                        features[i].OnGUI(rect, state);

                    HandleMouseDown();
                    HandleMouseDrag();
                    HandleMouseUp();

                    HandleRepaint();
                }

                HandleCommands();
            }
        }

        private void Refresh()
        {
            features.Clear();

            for (int i = 0; i < track.count; i++)
                features.Add(Bindings<FeatureGUI>.GetBinding(track, i));
        }

        private void RefreshIfNecessary()
        {
            if (state.refresh == false || Selection.activeObject != track)
                return;

            Sort();
            Refresh();
            state.refresh = false;
        }
        
        private void DrawTrack(Rect rect)
        {
            Color color = Styles.trackColor;

            if (Selection.activeObject == track)
                color = Styles.selectedTrackColor;

            EditorGUI.DrawRect(rect, color);

            Rect text = new Rect(rect.x + 5, rect.y + 5, 100, 30);
            EditorGUI.LabelField(text, track.name, Styles.trackText);

            Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(track.name));

            Rect options = new Rect(text.x + textSize.x, rect.y + 5, 15, 15);
            EditorGUI.LabelField(options, "≡", Styles.trackText);
        }

        private void HandleMouseDown()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown)
                return;

            if (currentEvent.button > 1)
                return;

            if (!rect.Contains(currentEvent.mousePosition))
                return;
            
            Selection.activeObject = track;

            state.mouseDownPosition = currentEvent.mousePosition;                       
            state.trackClicked = true;

            if (!currentEvent.control && !currentEvent.shift)
                FeatureSelection.Clear();

            if (currentEvent.clickCount == 2)
                CreateFeature();
                        
            currentEvent.Use();
        }

        private void HandleMouseDrag()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDrag)
                return;

            if (Selection.activeObject != track)
                return;

            if (currentEvent.button != 0)
                return;

            if (!state.featureClicked && !state.manipulatorClicked && !state.trackClicked)
                return;

            Vector2 delta = currentEvent.mousePosition - state.mouseDownPosition;

            if (state.featureClicked)
            {
                if(!state.dragging)
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { track, FeatureSelection.instance }, "Move Features");

                foreach (int index in FeatureSelection.indices)
                    features[index].Move(delta, state);
            }
            
            if(state.manipulatorClicked)
            {
                if (!state.dragging)
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { track, FeatureSelection.instance }, "Manipulate Features");

                foreach (int index in FeatureSelection.indices)
                    features[index].Manipulate(delta, state);
            }
            
            state.dragging = true;

            currentEvent.Use();            
        }

        private void HandleMouseUp()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseUp && currentEvent.type != EventType.Ignore)
                return;

            if (Selection.activeObject != track)
                return;

            if (currentEvent.button != 0)
                return;

            if (!state.featureClicked && !state.trackClicked && !state.manipulatorClicked)
                return;

            if (state.dragging)
            {
                if (state.featureClicked || state.manipulatorClicked)
                {
                    Sort();
                    Refresh();
                }
                
                if (state.trackClicked)
                {
                    float selectionStart = state.PixelsToTimestamp(Mathf.Min(currentEvent.mousePosition.x, state.mouseDownPosition.x));
                    float selectionEnd = state.PixelsToTimestamp(Mathf.Max(currentEvent.mousePosition.x, state.mouseDownPosition.x));

                    SelectFeatures(selectionStart, selectionEnd);                    
                }
            }

            state.dragging = false;

            state.featureClicked = false;
            state.manipulatorClicked = false;
            state.trackClicked = false;
                        
            currentEvent.Use();
        }

        private void SelectFeatures(float start, float end)
        {
            List<T> selectedFeatures = new List<T>();

            track.GetIntersectingFeatures(selectedFeatures, start, end);

            if (!Event.current.control)
                FeatureSelection.Clear();

            foreach (T feature in selectedFeatures)
            {
                int index = track.GetIndex(feature);

                if (!FeatureSelection.Contains(index))
                    FeatureSelection.Add(index);
            }
        }

        private void HandleRepaint()
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.Repaint && currentEvent.type != EventType.Layout)
                return;

            if (Selection.activeObject != track)
                return;

            if (state.dragging && state.trackClicked)
            {
                rect.x = Mathf.Min(currentEvent.mousePosition.x, state.mouseDownPosition.x);
                rect.width = Mathf.Max(currentEvent.mousePosition.x, state.mouseDownPosition.x) - rect.x;
                GUI.Box(rect, GUIContent.none, "selectionRect");
            }
        }

        private void HandleCommands()
        {
            if (Selection.activeObject != track)
                return;

            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.ValidateCommand)
                currentEvent.Use();

            if (currentEvent.type != EventType.ExecuteCommand)
                return;

            string command = currentEvent.commandName;

            if (FeatureSelection.count > 0)
            {
                if (command == "Delete" || command == "SoftDelete")
                    DeleteFeatures();

                if (command == "Copy")
                    CopyFeatures();

                if (command == "Duplicate")
                    DuplicateFeatures();

                currentEvent.Use();
            }

            if (command == "Paste" && !(ClipBoard.value is Track))
            {
                float timestamp = state.PixelsToTimestamp(state.mouseDownPosition.x);

                PasteFeatures(timestamp);
                currentEvent.Use();
            }

            if (command == "AddFeature")
            {
                CreateFeature();

                currentEvent.Use();
            }

            Refresh();
        }

        private void Sort()
        {
            var selectedFeatures = FeatureSelection.indices;

            T[] sortedFeatures = new T[selectedFeatures.Count];

            for (int i = 0; i < selectedFeatures.Count; i++)
                sortedFeatures[i] = track[selectedFeatures[i]];

            track.Sort();

            for (int i = 0; i < sortedFeatures.Length; i++)
                selectedFeatures[i] = track.GetIndex(sortedFeatures[i]);
        }

        private void CreateFeature()
        {
            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { track, FeatureSelection.instance }, "Create Feature");

            List<T> selectedFeatures = new List<T>();

            foreach (int index in FeatureSelection.indices)
                selectedFeatures.Add(track[index]);

            FeatureSelection.Clear();

            float timestamp = state.PixelsToTimestamp(state.mouseDownPosition.x);
            
            float value = (rect.height - state.mouseDownPosition.y) / rect.height;

            T newFeature = featureCreator.Create(timestamp, value);

            track.Add(newFeature);

            FeatureSelection.Add(track.GetIndex(newFeature));

            foreach (T feature in selectedFeatures)
                FeatureSelection.Add(track.GetIndex(feature));

            Refresh();
        }

        private void DeleteFeatures()
        {
            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { track, FeatureSelection.instance }, "Delete Features");

            var selectedFeatures = FeatureSelection.indices;

            T[] toRemove = new T[selectedFeatures.Count];

            for (int i = 0; i < toRemove.Length; i++)
                toRemove[i] = track[selectedFeatures[i]];

            foreach (T feature in toRemove)
                track.Remove(feature);

            selectedFeatures.Clear();
        }

        private void CopyFeatures()
        {
            List<T> features = new List<T>();

            foreach (int index in FeatureSelection.indices)
                features.Add(track[index]);

            ClipBoard.value = features;
        }

        private void PasteFeatures(float timestamp)
        {
            List<T> features = ClipBoard.value as List<T>;

            if (features == null)
                return;

            if (features.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { track, FeatureSelection.instance }, "Paste Features");

            float delta = timestamp - features[0].timestamp;

            FeatureSelection.Clear();

            foreach (T feature in features)
            {
                string json = JsonUtility.ToJson(feature);

                T copy = JsonUtility.FromJson<T>(json);
                copy.timestamp += delta;
                track.Add(copy);

                FeatureSelection.Add(track.GetIndex(copy));
            }
        }

        private void DuplicateFeatures()
        {
            if (FeatureSelection.count == 0)
                return;

            float timestamp = track[FeatureSelection.indices[0]].timestamp;

            CopyFeatures();
            PasteFeatures(timestamp + .1f);
        }
    }
}
