using System;

namespace RhythmTool
{
    /// <summary>
    /// A Track that contains Value Features.
    /// </summary>
    public class ValueTrack : Track<Value>
    {
        
    }

    /// <summary>
    /// A Value Feature is a Feature with a simple float value.
    /// </summary>
    [Serializable]
    public class Value : Feature
    {
        /// <summary>
        /// The value.
        /// </summary>
        public float value;
    }
}