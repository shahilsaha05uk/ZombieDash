using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WSMGameStudio.Settings;

namespace WSMGameStudio.Behaviours
{
    public class Breakable2d : MonoBehaviour
    {
        public BreakingForce breakingForce;
        public GameObject[] brokenPieces;

        public bool breakOnCollision = false;
        public CollisionSettings collisionSettings;

        public bool RemoveBrokenPiecesFromScene = false;
        public RemoveSettings removeSettings;

        public UnityEvent OnBreak;

        private Collider2D _collider;
        private MeshRenderer _renderer;
        private AudioSource _breakSFX;
        private Rigidbody2D _rigidBody;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<MeshRenderer>();
            _breakSFX = GetComponent<AudioSource>();
            _rigidBody = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Break()
        {
            if (_breakSFX != null)
                _breakSFX.Play();

            _rigidBody.isKinematic = true;

            //Disable collider to avoid collision with spawned broken pieces
            if (_collider != null)
                _collider.enabled = false;

            //Make object dissapear
            _renderer.enabled = false;

            foreach (var piece in brokenPieces)
            {
                GameObject pieceClone = Instantiate(piece, transform.position, transform.rotation);

                if (RemoveBrokenPiecesFromScene)
                {
                    RemoveFromScene removeScript = pieceClone.GetComponent<RemoveFromScene>();
                    removeScript.removeSettings = removeSettings;
                    removeScript.enabled = true;
                    removeScript.InvokeRemoveAction();
                }

                Rigidbody2D rb = pieceClone.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(transform.position, ForceMode2D.Impulse);
                }
            }

            if (OnBreak != null)
                OnBreak.Invoke();
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (breakOnCollision)
            {
                switch (collisionSettings.collisionMode)
                {
                    case Enums.CollisionMode.AllObjects:
                        Break();
                        break;
                    case Enums.CollisionMode.CheckAllowedTags:
                        if (collisionSettings.allowedTags.Contains(collision.gameObject.tag))
                            Break();
                        break;
                    case Enums.CollisionMode.CheckIgnoredTags:
                        if (!collisionSettings.ignoredTags.Contains(collision.gameObject.tag))
                            Break();
                        break;
                    case Enums.CollisionMode.CheckAllowedAndIgnoredTags:
                        if (collisionSettings.allowedTags.Contains(collision.gameObject.tag)
                            && !collisionSettings.ignoredTags.Contains(collision.gameObject.tag))
                            Break();
                        break;
                    case Enums.CollisionMode.None:
                        break;
                }
            }
        }
    }
}
