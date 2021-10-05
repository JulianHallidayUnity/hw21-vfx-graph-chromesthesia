using System;

namespace RhythmTool
{
    /// <summary>
    /// Feature is the base type that holds information in a Track.
    /// </summary>
    public class Feature
    {
        /// <summary>
        /// The time in seconds at which this feature occurs.
        /// </summary>
        public float timestamp;

        /// <summary>
        /// The duration of this feature in seconds.
        /// </summary>
        public float length;
    }
}