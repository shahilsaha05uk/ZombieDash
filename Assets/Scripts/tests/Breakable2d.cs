using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WSMGameStudio.Settings;

namespace WSMGameStudio.Behaviours
{
    public class Breakable2d : MonoBehaviour
    {
        public int mParticleCount;

        public BreakingForce breakingForce;
        public Piece[] brokenPieces;

        public bool breakOnCollision = false;
        public CollisionSettings collisionSettings;

        public bool RemoveBrokenPiecesFromScene = false;
        public RemoveSettings removeSettings;

        public UnityEvent OnBreak;

        private Collider2D _collider;
        private SpriteRenderer _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Break()
        {
            if (_collider != null)
                _collider.enabled = false;

            _renderer.enabled = false;

            int totalPieces = brokenPieces.Length;
            for (int i = 0; i < mParticleCount; i++)
            {
                int randIndex = Random.Range(0, totalPieces);

                Piece pieceClone = Instantiate(brokenPieces[randIndex], transform.position, transform.rotation);

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
                    rb.AddForce(pieceClone.transform.up * breakingForce.power, ForceMode2D.Impulse);
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
