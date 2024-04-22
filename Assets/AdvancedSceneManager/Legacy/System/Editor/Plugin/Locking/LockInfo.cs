#if ASM_PLUGIN_LOCKING

using System;
using UnityEngine;

namespace AdvancedSceneManager.Plugin.Locking
{

    /// <summary>An info class for locking objects. Has no effect by itself. See <see cref="LockUtility"/>.</summary>
    [Serializable]
    public class LockInfo
    {

        public static LockInfo Empty { get; } = new LockInfo();

        [SerializeField] private bool m_isEnabled;
        [SerializeField] private string m_by;
        [SerializeField] private string m_message;

        /// <summary>Gets if this lock is enabled.</summary>
        public bool isEnabled => m_isEnabled;

        /// <summary>Gets the author of this lock.</summary>
        public string by => m_by;

        /// <summary>Gets the message of this lock.</summary>
        public string message => m_message;

        /// <summary>Gets a tooltip string describing this lock.</summary>
        public string AsTooltip =>
            !isEnabled
            ? "Lock"
            : "Locked by: " + Environment.NewLine +
            (string.IsNullOrWhiteSpace(by) ? "(unspecified)" : by) + Environment.NewLine +
            Environment.NewLine +
            "Message:" + Environment.NewLine +
            (string.IsNullOrWhiteSpace(message) ? "(unspecified)" : message);

        /// <summary>Locks the associated object.</summary>
        public LockInfo Lock(string name = null, string message = null)
        {
            m_isEnabled = true;
            m_by = name;
            m_message = message;
            return this;
        }

        /// <summary>Unlocks the associated object.</summary>
        public LockInfo Unlock()
        {
            m_isEnabled = false;
            m_by = "";
            m_message = "";
            return this;
        }

    }

}

#endif
