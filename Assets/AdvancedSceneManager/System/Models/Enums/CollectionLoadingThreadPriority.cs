using UnityEngine;

namespace AdvancedSceneManager.Models.Enums
{

    /// <summary>Wrapper for <see cref="ThreadPriority"/>, adds <see cref="CollectionLoadingThreadPriority.Auto"/>.</summary>
    /// <remarks><see cref="ThreadPriority"/>: <inheritdoc cref="ThreadPriority"/></remarks>
    public enum CollectionLoadingThreadPriority
    {

        /// <summary>Automatically decide <see cref="ThreadPriority"/> based on if loading screen is open.</summary>
        Auto = -2,

        /// <summary>Lowest thread priority.</summary>
        Low = ThreadPriority.Low,

        /// <summary>Below normal thread priority.</summary>
        BelowNormal = ThreadPriority.BelowNormal,

        /// <summary>Normal thread priority.</summary>
        Normal = ThreadPriority.Normal,

        /// <summary>Highest thread priority.</summary>
        High = ThreadPriority.High,

    }

}
