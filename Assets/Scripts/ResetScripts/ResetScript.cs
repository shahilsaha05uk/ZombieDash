using Interfaces;
using UnityEngine;

public struct ColliderSettings
{
    public bool bIsEnabled;
}
public struct RigidbodySettings
{
    public bool bIsKinematic;
    public Vector2 mVelocity;
    public float mMass;
    public float mAngularVelocity;
    public float mLinearVelocity;

    public float mRotation;
    public Vector2 mPosition;
    public Vector2 mCenterOfMass;

}
public struct TransformationSettings
{
    public Vector3 pos;
    public Vector3 scale;
    public Quaternion rot;
}

public struct SpriteRendererSettings
{
    public bool bIsEnabled;
}

public class ResetScript : MonoBehaviour, IResetInterface
{
    public bool bResetRigidbody;
    public bool bResetTransforms;
    public bool bResetCollider;
    public bool bResetSpriteRenderer;

    private Collider2D mCollider;
    private Rigidbody2D rb;
    private SpriteRenderer mSpriteRenderer;

    private ColliderSettings DefaultColliderSettings;
    private RigidbodySettings DefaultRigidbodySettings;
    private TransformationSettings DefaultTransformSettings;
    private SpriteRendererSettings DefaultSpriteRendererSettings;

    private void Awake()
    {
        GameManager.OnResetLevel += OnReset;

        // Store the default Transforms
        if (bResetTransforms)
        {
            var trans = transform;
            DefaultTransformSettings.pos = trans.position;
            DefaultTransformSettings.rot = trans.rotation;
            DefaultTransformSettings.scale = trans.localScale;
        }

        // Store the default Collider Settings
        if (bResetCollider)
        {
            TryGetComponent<Collider2D>(out mCollider);

            if (mCollider)
            {
                DefaultColliderSettings.bIsEnabled = mCollider.enabled;
            }
        }

        // Store the default Rigidbody Settings
        if (bResetRigidbody)
        {
            TryGetComponent<Rigidbody2D>(out rb);

            if (rb)
            {
                DefaultRigidbodySettings.bIsKinematic = rb.isKinematic;
                DefaultRigidbodySettings.mVelocity = rb.velocity;
                DefaultRigidbodySettings.mAngularVelocity = rb.angularVelocity;
                DefaultRigidbodySettings.mPosition = rb.position;
                DefaultRigidbodySettings.mRotation = rb.rotation;
                DefaultRigidbodySettings.mCenterOfMass = rb.centerOfMass;
                DefaultRigidbodySettings.mMass = rb.mass;
            }

        }

        // Store the default Sprite Renderer Settings
        if (bResetSpriteRenderer)
        {
            TryGetComponent<SpriteRenderer>(out mSpriteRenderer);

            if (mSpriteRenderer)
            {
                DefaultSpriteRendererSettings.bIsEnabled = mSpriteRenderer.enabled;
            }
        }
    }

    public void OnReset()
    {
        if (bResetTransforms)
        {
            transform.SetPositionAndRotation(DefaultTransformSettings.pos, DefaultTransformSettings.rot);
            transform.localScale = DefaultTransformSettings.scale;
        }

        if (bResetRigidbody && rb)
        {
            if (rb)
            {
                rb.velocity = DefaultRigidbodySettings.mVelocity;
                rb.angularVelocity = DefaultRigidbodySettings.mAngularVelocity;
                rb.position = DefaultRigidbodySettings.mPosition;
                rb.rotation = DefaultRigidbodySettings.mRotation;
                rb.centerOfMass = DefaultRigidbodySettings.mCenterOfMass;
                rb.isKinematic = DefaultRigidbodySettings.bIsKinematic;
                rb.mass = DefaultRigidbodySettings.mMass;
            }
        }

        if (bResetCollider)
        {
            if (mCollider != null) mCollider.enabled = DefaultColliderSettings.bIsEnabled;
        }

        if (bResetSpriteRenderer && mSpriteRenderer)
        {
            mSpriteRenderer.enabled = DefaultSpriteRendererSettings.bIsEnabled;
        }
    }
}