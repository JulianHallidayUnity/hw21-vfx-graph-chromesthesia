using System.Collections.Generic;
using UnityEngine;

namespace RhythmTool
{
    public class FeatureSelection : ScriptableObject
    {
        public static FeatureSelection instance
        {
            get
            {
                GetInstance();

                return _instance;
            }
        }
        
        public static List<int> indices
        {
            get
            {
                return instance._indices;
            }
        }

        public static int count
        {
            get
            {
                return indices.Count;
            }
        }
        
        private static FeatureSelection _instance;

        [SerializeField]
        private List<int> _indices = new List<int>();
        
        public static void Add(int index)
        {
            indices.Add(index);
        }

        public static void Remove(int index)
        {
            indices.Remove(index);
        }

        public static void Clear()
        {
            indices.Clear();
        }

        public static bool Contains(int index)
        {
            return indices.Contains(index);
        }

        private static void GetInstance()
        {
            if (_instance == null)
                _instance = CreateInstance<FeatureSelection>();
        }
        
        private void OnEnable()
        {
            _instance = this;
        }       
    }
}