using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
    [HideInInspector] public Vector2 candidatePos;
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public Vector2 prevVelocity=Vector2.zero;
    [HideInInspector] public float   accel;
    [HideInInspector] public bool    onGround;
    [HideInInspector] public bool    isKinematic;  // true = immovable
    [HideInInspector] public bool    pendingDestroy;
    [HideInInspector] public Vector2 prevPos;
    
    [HideInInspector] public float   weight=0.55f;
    
    //afterimage
    [SerializeField] private AfterImage afterImagePrefab;
    [SerializeField] private float imgSpawnInterval = 0.05f;
    [SerializeField] private float imgLifetime = 0.3f;
    [SerializeField] private float imgSpeedThreshold = 10f;
    [SerializeField] private Color imgTint = new Color(1,1,1,0.8f);
    private float imgTimer;
    private SpriteRenderer bodySprite;
    
    //edge collision handling
    public bool nearBlock;
    public Vector2 nearNormal;

    public Vector2 Velocity
    {
        get => velocity;
        set
        {
            //accel = value.magnitude - velocity.magnitude;
            velocity = value;
        } 
    }


    private BoxCollider2D collider;
    public Vector2 size;

    protected virtual void Awake()
    {
        collider    = GetComponent<BoxCollider2D>();
        size  = new Vector2(
            collider.size.x * transform.localScale.x,
            collider.size.y * transform.localScale.y
        );
        
        bodySprite = GetComponent<SpriteRenderer>();
    }
    
    public float     Speed => velocity.magnitude;

    protected virtual void OnEnable()
    {
        candidatePos = transform.position;
        PhysicsManager.Instance.Register(this);
    }

    protected virtual void OnDisable() =>
        PhysicsManager.Instance.Unregister(this);
    
    public abstract void UpdateVelocity(float dt);

    public virtual void TryLatch(PhysicsBody collidedBody, bool isSide)
    {
        
    }

    public void UpdateGroundState(List<PhysicsBody> bodies)
    {
        onGround = false;
        const float probe = 0.08f;
        Vector2 feetPos = candidatePos + Vector2.down * probe;

        foreach (var other in bodies)
        {
            if (other == this) continue;
            Vector2 delta    = feetPos - other.candidatePos;
            float overlapX   = (size.x + other.size.x) * 0.5f - Mathf.Abs(delta.x);
            float overlapY   = (size.y + other.size.y) * 0.5f - Mathf.Abs(delta.y);
            if (overlapX > 0f && overlapY > 0f)
            {
                // Only grounded if the other body is meaningfully below us
                // and we're sitting on its top face specifically
                bool otherIsBelow = delta.y > 0f;
                bool restingOnTop = overlapX > overlapY;  // vertical contact
                if (otherIsBelow && restingOnTop)
                {
                    onGround = true;
                    return;
                }
            }
        }
        
        WrapPosition(); //wrap around screen edges
    }

    public void Update()
    {
        if (Speed > imgSpeedThreshold)
        {
            imgTimer += Time.deltaTime;

            if (imgTimer >= imgSpawnInterval)
            {
                SpawnAfterImage();
                imgTimer = 0f;
            }
        }
        else
        {
            imgTimer = 0f;
        }
    }
    
    void SpawnAfterImage()
    {
        var img = Instantiate(afterImagePrefab);

        img.Init(
            bodySprite.sprite,
            new Vector3(transform.position.x, transform.position.y, transform.position.z - 1f),
            transform.rotation,
            transform.localScale,
            imgTint,
            imgLifetime
        );
    }
    
    void WrapPosition()
    {
        Camera cam   = Camera.main;
        float width  = cam.orthographicSize * 2f * cam.aspect;
        float halfW  = width * 0.5f;
        float camX   = cam.transform.position.x;

        if (candidatePos.x > camX + halfW) candidatePos.x -= width;
        else if (candidatePos.x < camX - halfW) candidatePos.x += width;
    }
}