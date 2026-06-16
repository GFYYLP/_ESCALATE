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
    
    //edge collision handling
    [HideInInspector] public bool nearBlock;
    [HideInInspector] public Vector2 nearNormal;
    [HideInInspector] public PhysicsBody collidedBody=default;
    [HideInInspector] public bool collidedToSide=false;
    [HideInInspector] public float flipSign = 1;
    
    [SerializeField] public float   weight=0.55f;
    
    //afterimage
    [SerializeField] private AfterImage afterImagePrefab;
    [SerializeField] private float imgSpawnInterval = 0.05f;
    [SerializeField] private float imgLifetime = 0.3f;
    [SerializeField] private float imgSpeedThreshold = 10f;
    [SerializeField] private Color imgTint = new Color(1,1,1,0.8f);
    private float imgTimer;
    private SpriteRenderer bodySprite;
    [SerializeField] public Transform visual;
    
    

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
        // size  = new Vector2(
        //     collider.size.x * transform.localScale.x,
        //     collider.size.y * transform.localScale.y
        // );

        bodySprite = GetComponentInChildren<SpriteRenderer>();
    }
    
    public float     Speed => velocity.magnitude;

    protected virtual void OnEnable()
    {
        candidatePos = transform.position;
        PhysicsManager.Instance.Register(this);
    }

    protected virtual void OnDisable() =>
        PhysicsManager.Instance.Unregister(this);
    
    public abstract void UpdateVelocity(float dt, float corruptScore);

    public void UpdateGroundState(List<PhysicsBody> bodies)
    {
        onGround = false;
        const float probe = 0.08f;
        Vector2 feetPos = candidatePos + Vector2.down * probe;

        foreach (var other in bodies)
        {
            if (other == this || (this is Block && other.isKinematic)
                              || (other is Block && this.isKinematic)) continue;
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
                    break;
                }
            }
        }
        
        WrapPosition(); //wrap around screen edges
    }

    public void Update()
    {
        
        size = new Vector2(
            collider.size.x * transform.lossyScale.x,
            collider.size.y * transform.lossyScale.y
        );
        
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
        
        if (this is Player) UpdateVisual(Time.deltaTime);
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
    
    
    [SerializeField] private float stretchSensitivity = 0.01f;
    [SerializeField] private float maxStretch         = 1.1f;
    [SerializeField] private float minSquash          = 0.8f;
    [SerializeField] private float morphSmoothing     = 8f;

    private Vector3 targetScale = Vector3.one;

    void UpdateVisual(float dt)
    {
        float speed = velocity.magnitude;

        if (speed < 0.1f)
        {
            targetScale = Vector3.one;
        }
        else
        {
            Vector2 dir     = velocity.normalized;
            // Small additive stretch: never more than +/-maxStretchAmount from 1
            float amount    = Mathf.Min(speed * stretchSensitivity, maxStretch - 1f);
        
            float scaleX = 1f + amount * Mathf.Abs(dir.x) 
                           - amount * Mathf.Abs(dir.y) * 0.5f;
            float scaleY = 1f + amount * Mathf.Abs(dir.y)
                           - amount * Mathf.Abs(dir.x) * 0.5f;

            // Hard clamp
            scaleX = Mathf.Clamp(scaleX, 0.85f, 1.15f);
            scaleY = Mathf.Clamp(scaleY, 0.85f, 1.15f);

            targetScale = new Vector3(scaleX, scaleY, 1f);
        }
        
        Vector3 currentScale = visual.localScale;
        currentScale.x = Mathf.Abs(currentScale.x); // strip flip before lerping

        Vector3 smoothed = Vector3.Lerp(
            currentScale, targetScale, morphSmoothing * dt);

        visual.localScale = new Vector3(
            smoothed.x * flipSign,
            smoothed.y,
            smoothed.z);
    }
}