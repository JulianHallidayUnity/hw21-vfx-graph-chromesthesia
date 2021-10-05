using UnityEngine;
using System;

namespace RhythmTool
{
    /// <summary>
    /// A Track that contains Chroma features
    /// </summary>
    public class ChromaTrack : Track<Chroma>
    {

    }

    /// <summary>
    /// Chroma features are closely related to pitch and represent the most prominent notes in the song.
    /// </summary>
    [Serializable]
    public class Chroma : Feature
    {
        /// <summary>
        /// The detected musical note.
        /// </summary>
        public Note note;
    }
    
    /// <summary>
    /// Musical notes.
    /// </summary>
    public enum Note
    {
        A,
        ASharp,
        B,
        C,
        CSHARP,
        D,
        DSHARP,
        E,
        F,
        FSHARP,
        G,
        GSHARP       
    }
}
