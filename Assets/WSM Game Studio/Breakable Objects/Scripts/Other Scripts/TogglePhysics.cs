using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WSMGameStudio.Behaviours
{
    public class TogglePhysics : MonoBehaviour
    {
        private Rigidbody2D _rigiBody;
        private Collider2D _collider;

        private void Awake()
        {
            _rigiBody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// Enable Physics and Collision
        /// </summary>
        public void Enable()
        {
            if (_rigiBody)
                _rigiBody.isKinematic = false;
            if (_collider)
                _collider.enabled = true;
        }

        /// <summary>
        /// Disable Physics and Collision
        /// </summary>
        public void Disable()
        {
            if (_rigiBody)
                _rigiBody.isKinematic = true;
            if (_collider)
                _collider.enabled = false;
        }
    }
}
