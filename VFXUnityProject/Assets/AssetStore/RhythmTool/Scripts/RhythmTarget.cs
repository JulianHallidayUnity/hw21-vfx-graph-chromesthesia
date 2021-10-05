using UnityEngine;

namespace RhythmTool
{
    /// <summary>
    /// A RhythmTarget can be targeted by a RhythmPlayer.
    /// </summary>
    public abstract class RhythmTarget : ScriptableObject
    {
        /// <summary>
        /// Process a RhythmData object with a certain time frame.
        /// </summary>
        /// <param name="rhythmData">The RhythmData object.</param>
        /// <param name="start">The start time in seconds.</param>
        /// <param name="end">The end time in seconds.</param>
        public abstract void Process(RhythmData rhythmData, float start, float end);

        /// <summary>
        /// Indicate that the playback time has been reset to a different time and process a RhythmData.
        /// </summary>
        /// <param name="rhythmData">The rhyRhythmData object.</param>
        /// <param name="time">The new playback time.</param>
        public abstract void Reset(RhythmData rhythmData, float time);
    }
}
