using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicsBody : MonoBehaviour
{
    [HideInInspector] public Vector2 candidatePos;
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool    onGround;
    [HideInInspector] public bool    isKinematic;  // true = immovable
    [HideInInspector] public bool    pendingDestroy;
    [HideInInspector] public Vector2 prevPos;
    [HideInInspector] public float restitution = 0.5f;
    
    [SerializeField]  private float highCollideVal = 0.1f;
    
    public bool nearBlock;
    public Vector2 nearNormal; 
    
    private RippleManager rippleManager;
    private BoxCollider2D collider;
    public event Action<Vector2> onHighCollision;
    public Vector2 size;

    protected virtual void Awake()
    {
        collider    = GetComponent<BoxCollider2D>();
        size  = new Vector2(
            collider.size.x * transform.localScale.x,
            collider.size.y * transform.localScale.y
        );
        
        rippleManager = FindObjectOfType<RippleManager>();
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

    public virtual void UpdateProximity(float overlapX, float overlapY)
    {
        
    }

    public void OnImpact(float impactSpeed, PhysicsBody other)
    {
        //velocity.y = 0;
        
        if (impactSpeed > highCollideVal)
            onHighCollision?.Invoke(candidatePos);
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
        if (Speed > 0.2)  rippleManager.AddRipple(candidatePos, Speed);
        
        if (Input.GetKey(KeyCode.S))
        {
            onHighCollision?.Invoke(candidatePos);
        }
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