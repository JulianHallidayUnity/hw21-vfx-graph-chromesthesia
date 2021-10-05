using UnityEngine;

namespace RhythmTool
{
    public class FeatureCreator<T> where T : Feature, new()
    {
        public virtual T Create(float timestamp, float value)
        {
            T feature = new T()
            {
                timestamp = timestamp
            };

            return feature;
        }
    }

    public class OnsetCreator : FeatureCreator<Onset>
    {
        public override Onset Create(float timestamp, float value)
        {
            Onset feature = base.Create(timestamp, value);

            feature.strength = value * 10;

            return feature;
        }
    }

    public class ValueCreator : FeatureCreator<Value>
    {
        public override Value Create(float timestamp, float value)
        {
            Value feature = base.Create(timestamp, value);

            feature.value = value * 10;

            return feature;
        }
    }

    public class ChromaCreator : FeatureCreator<Chroma>
    {
        public override Chroma Create(float timestamp, float value)
        {
            Chroma feature = base.Create(timestamp, value);

            value = Mathf.Clamp(value, 0, 1);

            feature.note = (Note)Mathf.FloorToInt(value * 12);

            feature.length = .5f;

            return feature;
        }
    }
}
