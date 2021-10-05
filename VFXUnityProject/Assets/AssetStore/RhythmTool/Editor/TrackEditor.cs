using System;
using UnityEngine;
using UnityEditor;

namespace RhythmTool
{
    public enum FeatureEditMode
    {
        Delta,
        Absolute,
        List
    }

    [CustomEditor(typeof(Track), true)]
    public class TrackEditor : Editor
    {
        private static Texture headerIcon;

        private EditorState state;

        private SerializedProperty _name;
        private SerializedProperty _features;

        private FeatureEditMode featureEditMode;

        private bool changedTimestamp;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        private void OnEnable()
        {
            if (headerIcon == null)
                headerIcon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(target));

            state = RhythmToolWindow.instance.state;

            _name = serializedObject.FindProperty("_name");
            _features = serializedObject.FindProperty("_features");

            featureEditMode = (FeatureEditMode)EditorPrefs.GetInt("featureEditMode");
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt("featureEditMode", (int)featureEditMode);
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                featureEditMode = (FeatureEditMode)EnumToolbar(featureEditMode);
                GUILayout.FlexibleSpace();
            }

            serializedObject.UpdateIfRequiredOrScript();

            Event currentEvent = Event.current;

            bool mouseUp = currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.Ignore;

            if(changedTimestamp && mouseUp)
            {
                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { target, FeatureSelection.instance }, "Move Features");

                changedTimestamp = false;
                state.refresh = true;
            }

            if (FeatureSelection.count == 0)
                EditorGUILayout.HelpBox("Select Features to edit them.", MessageType.Info);
            else
            {
                EditorGUILayout.HelpBox(string.Format("Editing {0} {1} Features", FeatureSelection.count, _features.type), MessageType.None);

                if (featureEditMode == FeatureEditMode.List)
                    ListEdit();
                else
                    MultiEdit();
            }
                        
            if(!changedTimestamp)
                serializedObject.ApplyModifiedProperties();            
        }

        private void MultiEdit()
        {
            var selectedFeatures = FeatureSelection.indices;

            var property = _features.GetArrayElementAtIndex(selectedFeatures[0]);
            var end = property.GetEndProperty(true);

            property.Next(true);

            int indentLevel = EditorGUI.indentLevel;
            int depth = property.depth;

            while (!SerializedProperty.EqualContents(property, end))
            {
                PropertyValue startValue = new PropertyValue(property);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.indentLevel = indentLevel + property.depth - depth;

                    bool enterChildren = EditorGUILayout.PropertyField(property, false);

                    if (check.changed)
                    {
                        PropertyValue value = new PropertyValue(property);

                        if (featureEditMode == FeatureEditMode.Delta || property.name == "timestamp")
                            value -= startValue;

                        if (property.name == "timestamp")
                            changedTimestamp = true;

                        ApplyChanges(property.propertyPath, value);
                    }

                    property.NextVisible(enterChildren);
                }
            }

            EditorGUI.indentLevel = indentLevel;
        }

        private void ApplyChanges(string propertyPath, PropertyValue value)
        {
            var selectedFeatures = FeatureSelection.indices;

            string index = selectedFeatures[0].ToString();

            for (int i = 1; i < selectedFeatures.Count; i++)
            {
                string targetIndex = selectedFeatures[i].ToString();
                string targetPath = propertyPath.Replace(index, targetIndex);

                SerializedProperty property = serializedObject.FindProperty(targetPath);

                PropertyValue propertyValue = new PropertyValue(property);

                if (featureEditMode == FeatureEditMode.Delta || property.name == "timestamp")
                    propertyValue += value;
                else
                    propertyValue = value;

                propertyValue.ApplyValue(property);
            }
        }
        
        private void ListEdit()
        {
            int indentLevel = EditorGUI.indentLevel;

            var selectedFeatures = FeatureSelection.indices;

            int maxFeatureCount = Mathf.Min(selectedFeatures.Count, 10);

            for(int i = 0; i < maxFeatureCount; i++)
            {
                int index = selectedFeatures[i];

                SerializedProperty property = _features.GetArrayElementAtIndex(index);
                var end = property.GetEndProperty(true);

                EditorGUILayout.LabelField(property.type + " " + index, EditorStyles.boldLabel);

                property.Next(true);

                int depth = property.depth;

                while (!SerializedProperty.EqualContents(property, end))
                {
                    EditorGUI.indentLevel = indentLevel + property.depth - depth;

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        bool enterChildren = EditorGUILayout.PropertyField(property);

                        if (check.changed && property.name == "timestamp")
                            changedTimestamp = true;

                        property.NextVisible(enterChildren);
                    }
                }

                EditorGUI.indentLevel = indentLevel;

                Rect rect = EditorGUILayout.GetControlRect(false, 1);
                rect.x = 0;
                rect.width = Screen.width;

                EditorGUI.DrawRect(rect, Color.gray);
            }

            if(maxFeatureCount < selectedFeatures.Count)            
                EditorGUILayout.LabelField(string.Format("{0} features omitted", selectedFeatures.Count - maxFeatureCount));            
        }
        
        protected override void OnHeaderGUI()
        {
            GUIStyle style = "In bigTitle";
            style.margin.bottom = 0;

            using (new GUILayout.HorizontalScope(style, GUILayout.Height(45)))
            {
                GUILayout.Box(headerIcon, GUIStyle.none, GUILayout.Width(32), GUILayout.Height(32));

                GUILayout.Space(5);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    _name.stringValue = EditorGUILayout.DelayedTextField(_name.stringValue);

                    if (check.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        target.name = _name.stringValue;
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static int EnumToolbar(Enum value, GUIStyle style = null)
        {
            string[] names = Enum.GetNames(value.GetType());
            int selected = Convert.ToInt32((object)value);

            return GUILayout.Toolbar(selected, names, style);
        }
    }

    public struct PropertyValue
    {
        public float floatValue;
        public int intValue;
        public int enumValueIndex;
        public Vector3 vector3Value;
        public string stringValue;

        public PropertyValue(SerializedProperty property) : this()
        {
            SerializedPropertyType propertyType = property.propertyType;

            if (propertyType == SerializedPropertyType.Float)
                floatValue = property.floatValue;

            if (propertyType == SerializedPropertyType.Integer)
                intValue = property.intValue;

            if (propertyType == SerializedPropertyType.Enum)
                enumValueIndex = property.enumValueIndex;

            if (propertyType == SerializedPropertyType.Vector3)
                vector3Value = property.vector3Value;

            if (propertyType == SerializedPropertyType.String)
                stringValue = property.stringValue;
        }

        public void ApplyValue(SerializedProperty property)
        {
            SerializedPropertyType propertyType = property.propertyType;

            if (propertyType == SerializedPropertyType.Float)
                property.floatValue = floatValue;

            if (propertyType == SerializedPropertyType.Integer)
                property.intValue = intValue;

            if (propertyType == SerializedPropertyType.Enum)
            {
                int enumCount = property.enumNames.Length;

                int index = enumValueIndex % enumCount;

                if (index < 0)
                    index += enumCount;

                property.enumValueIndex = index;
            }

            if (propertyType == SerializedPropertyType.Vector3)
                property.vector3Value = vector3Value;

            if (propertyType == SerializedPropertyType.String)
                property.stringValue = stringValue;
        }

        public static PropertyValue operator +(PropertyValue a, PropertyValue b)
        {
            return new PropertyValue()
            {
                floatValue = a.floatValue + b.floatValue,
                intValue = a.intValue + b.intValue,
                enumValueIndex = a.enumValueIndex + b.enumValueIndex,
                vector3Value = a.vector3Value + b.vector3Value,
                stringValue = a.stringValue
            };
        }

        public static PropertyValue operator -(PropertyValue a, PropertyValue b)
        {
            return new PropertyValue()
            {
                floatValue = a.floatValue - b.floatValue,
                intValue = a.intValue - b.intValue,
                enumValueIndex = a.enumValueIndex - b.enumValueIndex,
                vector3Value = a.vector3Value - b.vector3Value,
                stringValue = a.stringValue
            };
        }
    }
}