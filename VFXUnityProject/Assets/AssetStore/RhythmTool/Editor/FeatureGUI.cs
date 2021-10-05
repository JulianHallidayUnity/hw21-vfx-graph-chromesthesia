using System;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public abstract class FeatureGUI
    {
        public abstract void OnGUI(Rect trackRect, EditorState state);

        public abstract void Move(Vector2 delta, EditorState state);

        public abstract void Manipulate(Vector2 delta, EditorState state);
    }

    public class FeatureGUI<T> : FeatureGUI where T : Feature
    {
        public Track<T> track { get; private set; }

        public T feature { get; private set; }

        public int index { get; private set; }

        public Rect rect { get; private set; }

        private float timestamp;

        private float length;

        public FeatureGUI(Track<T> track, int index)
        {
            this.track = track;
            this.index = index;

            feature = track[index];

            timestamp = feature.timestamp;
            length = feature.length;
        }

        public override void OnGUI(Rect trackRect, EditorState state)
        {
            rect = GetRect(trackRect, state);

            HandleMouseDown(state);

            Draw(); 
        }

        public override void Move(Vector2 delta, EditorState state)
        {
            feature.timestamp = timestamp + delta.x / state.pixelsPerSecond;
        }

        public override void Manipulate(Vector2 delta, EditorState state)
        {
            feature.length = length + delta.x / state.pixelsPerSecond;
        }

        private void HandleMouseDown(EditorState state)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown)
                return;

            if (currentEvent.button > 1)
                return;

            Rect clickRect = new Rect(rect.x - 3, rect.y - 3, rect.width + 6, rect.height + 6);
            
            if (!clickRect.Contains(currentEvent.mousePosition))
                return;

            if (Selection.activeObject != track)
                FeatureSelection.Clear();

            Selection.activeObject = track;
            
            Select();

            if (FeatureSelection.Contains(index))
                state.featureClicked = true;

            state.mouseDownPosition = currentEvent.mousePosition;

            currentEvent.Use();
        }
        
        private void Select()
        {
            Event currentEvent = Event.current;

            if (!FeatureSelection.Contains(index))
            {
                if (!currentEvent.control)
                    FeatureSelection.Clear();

                FeatureSelection.Add(index);
            }
            else if (currentEvent.control)
                FeatureSelection.Remove(index);
        }
        
        protected virtual Rect GetRect(Rect trackRect, EditorState state)
        {
            float x = (feature.timestamp - state.start) * state.pixelsPerSecond;
            float width = Math.Max(Styles.featureWidth, feature.length * state.pixelsPerSecond);

            return new Rect(x, 0, width, trackRect.height);
        }

        protected virtual void Draw()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            bool selected = Selection.activeObject == track && FeatureSelection.Contains(index);
            
            Styles.feature.Draw(rect, selected, selected, false, false);
        }
    }

    public class OnsetGUI : FeatureGUI<Onset>
    {
        public OnsetGUI(Track<Onset> track, int index) : base(track, index)
        {

        }

        protected override Rect GetRect(Rect trackRect, EditorState state)
        {
            Rect rect = base.GetRect(trackRect, state);

            float height = rect.height * feature.strength * .1f;

            height = Mathf.Round(height);

            rect.y = rect.height - height;
            rect.height = height;

            return rect;
        }
    }

    public class ValueGUI : FeatureGUI<Value>
    {
        public ValueGUI(Track<Value> track, int index) : base(track, index)
        {

        }

        protected override Rect GetRect(Rect trackRect, EditorState state)
        {
            Rect rect = base.GetRect(trackRect, state);

            float height = rect.height * feature.value * .1f;

            height = Mathf.Round(height);

            rect.y = rect.height - height;
            rect.height = height;

            return rect;
        }      
    }

    public class ChromaGUI : FeatureGUI<Chroma>
    {
        private Note note;

        private float noteHeight;

        public ChromaGUI(Track<Chroma> track, int index) : base(track, index)
        {
            note = feature.note;
        }
        
        protected override Rect GetRect(Rect trackRect, EditorState state)
        {
            Rect rect = base.GetRect(trackRect, state);

            noteHeight = rect.height / 12;

            rect.y = rect.height - noteHeight * ((int)feature.note + 1);
            rect.y += noteHeight / 2 - 2;
            rect.height = 5;

            return rect;
        }

        public override void Move(Vector2 delta, EditorState state)
        {
            base.Move(delta, state);

            int value = (int)note - Mathf.RoundToInt(delta.y / noteHeight);

            value %= 12;

            if (value < 0)
                value += 12;

            feature.note = (Note)value;
        }
    }
}
