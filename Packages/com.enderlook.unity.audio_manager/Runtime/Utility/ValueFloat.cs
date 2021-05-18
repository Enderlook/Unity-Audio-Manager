using System;

using UnityEngine;

using Random = UnityEngine.Random;

namespace Enderlook.Unity.AudioManager
{
    [Serializable]
    internal struct ValueFloat
    {
        // Keep field names in sync with ValueFloatDrawer.

        [SerializeField]
        private bool isRandom;

        [SerializeField]
        private float a;

        [SerializeField]
        private float b;

        public ValueFloat(float value)
        {
            isRandom = false;
            a = value;
            b = default;
        }

        public ValueFloat(float min, float max)
        {
            isRandom = true;
            a = min;
            b = max;
        }

        public float Value {
            get {
                if (isRandom)
                    return Random.Range(a, b);
                return a;
            }
        }

#if UNITY_EDITOR
        internal void Clamp(int min, int max)
        {
            a = Mathf.Clamp(a, min, max);
            b = Mathf.Clamp(b, min, max);
        }
#endif
    }
}