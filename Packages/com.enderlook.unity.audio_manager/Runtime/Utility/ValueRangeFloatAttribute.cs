using System;

namespace Enderlook.Unity.AudioManager
{
    /// <summary>
    /// Attribute used to specify a valid range of values in <see cref="ValueFloat"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ValueRangeFloatAttribute : Attribute
    {
        /// <summary>
        /// Minimal allowed value.
        /// </summary>
        public readonly float Min;

        /// <summary>
        /// Maximum allowed value.
        /// </summary>
        public readonly float Max;

        /// <summary>
        /// Specify the allowed range of values for a <see cref="ValueFloat"/>.
        /// </summary>
        /// <param name="min">Minimal allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        public ValueRangeFloatAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}