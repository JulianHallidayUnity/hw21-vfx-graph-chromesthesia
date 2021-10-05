using System;

namespace RhythmTool
{
    /// <summary>
    /// A Track that contains Beat Features.
    /// </summary>
    public class BeatTrack : Track<Beat>
    {

    }

    /// <summary>
    /// A Beat represents the rhythm of the song.
    /// </summary>
    [Serializable]
    public class Beat : Feature
    {
        /// <summary>
        /// The current BPM of the song.
        /// </summary>
        public float bpm;
    }
}