using System;

namespace RhythmTool
{
    /// <summary>
    /// A Track that contains Onset Features.
    /// </summary>
    public class OnsetTrack : Track<Onset>
    {
        
    }
    
    /// <summary>
    /// An Onset is the start of a note in a song.
    /// </summary>
    [Serializable]
    public class Onset : Feature
    {
        /// <summary>
        /// The strength or prominence of an onset.
        /// </summary>
        public float strength;
    }
}