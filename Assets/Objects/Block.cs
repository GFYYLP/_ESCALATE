using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    public SpriteRenderer sr ;
    private Collider2D ownCollider;  //to avoid self-collision
    
    [SerializeField] private float groundAccel    = 100f;
    [SerializeField] private float airAccel       = 65f;
    [SerializeField] private float groundFriction = 80f;
    [SerializeField] private float airFriction    = 40f;
    [SerializeField] private float gravity         = 28f;
    [SerializeField] private float maxFallSpeed    = 16f;

    // --- Collision ---
    [SerializeField] private Vector2 colliderSize = new Vector2(0.9f, 1.8f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float stepHeight  = 0.15f;
    [SerializeField] private float groundProbe = 0.08f;

    // --- State ---
    private Vector2 velocity;
    private bool    onGround;
    // void ApplyHorizontal(float dt)
    // {
    //     if (isDashing) return;  //ignores current horizontal input while dashing

    //     float input = 0f;
    //     if (Input.GetKey(KeyCode.A)) input -= 1f;
    //     if (Input.GetKey(KeyCode.D)) input += 1f;

    //     if (input != 0f)
    //     {
    //         //accelerates towards target
    //         float accel  = onGround ? groundAccel : airAccel;
    //         float target = input * moveSpeed;
    //         velocity.x = Mathf.MoveTowards(velocity.x, target, accel * dt);
    //     }
    //     else
    //     {
    //         //applies friction when acceleration is not present
    //         float friction = onGround ? groundFriction : airFriction;
    //         velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
    //     }
    // }

    void ApplyVertical(float dt)
    {
        // Gravit
        velocity.y -= gravity * dt;
        if (velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;
    }

    // -------------------------------------------------------------------------
    // Move & collide
    // -------------------------------------------------------------------------

    void MoveAndCollide(float dt)
    {
        Vector2 pos = transform.position;

        MoveX(ref pos, velocity.x * dt);
        MoveY(ref pos, velocity.y * dt);

        //Landing resets vertical velocity and state
        bool wasOnGround = onGround;
        onGround = CheckGround(pos);
        if (onGround && velocity.y < 0f)
        {
            velocity.y = 0f;
        }

        WrapPosition(ref pos);
        transform.position = pos;
    }

    void MoveX(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.x += amount;
        if (!Overlaps(pos)) return;

        //cancels movement on collision
        pos.x      -= amount;
        velocity.x  = 0f;

        //WrapPosition(); //warp on world borders
    }

    void MoveY(ref Vector2 pos, float amount)
    {
        if (amount == 0f) return;
        pos.y += amount;
        if (!Overlaps(pos)) return;

        // Binary search for the exact edge — no sudden snap
        float sign = Mathf.Sign(amount);
        float step = Mathf.Abs(amount);
        pos.y -= amount;  // step back fully first

        // Walk forward in smaller increments until we're just at the surface
        for (int i = 0; i < 8; i++)
        {
            step *= 0.5f;
            pos.y += sign * step;
            if (Overlaps(pos))
                pos.y -= sign * step;
        }

        velocity.y = 0f;
    }

    // -------------------------------------------------------------------------
    // Frame setup
    // -------------------------------------------------------------------------
    private void Awake()
    {
        sr =  gameObject.GetComponent<SpriteRenderer>();
        ownCollider = gameObject.GetComponent<Collider2D>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Ground/wall state is already set from last frame's MoveAndCollide.
        // This means friction and jump both see the correct grounded state
        // without waiting an extra frame.
        //UpdateTimers(dt);
        //ApplyHorizontal(dt);   // friction runs here using last frame's onGround
        ApplyVertical(dt);     // jump fires here — before this frame's landing
        MoveAndCollide(dt);    // landing updates onGround for next frame
        
        if (transform.position.y < -10.0f)
        {
            Destroy(gameObject); // replace with pooling later
        }
    }

    void Start()
    {
        velocity = Vector2.zero;
        
    }


    // -------------------------------------------------------------------------
    // Public data
    // -------------------------------------------------------------------------
    public float Speed      => velocity.magnitude;


    // -------------------------------------------------------------------------
    // Collision helpers
    // -------------------------------------------------------------------------
    bool Overlaps(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, colliderSize, 0f, groundLayer);
        foreach (var hit in hits)
            if (hit != null && hit != ownCollider)
                return true;
        return false;
    }

    bool CheckGround(Vector2 pos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos + Vector2.down * groundProbe, colliderSize, 0f, groundLayer);
        foreach (var hit in hits)
            if (hit != null && hit != ownCollider)
                return true;
        return false;
    }

    int CheckWall(Vector2 pos)
    {
        const float wallCheckDist = 0.08f;
        if (Physics2D.OverlapBox(pos + Vector2.right * wallCheckDist, colliderSize, 0f, groundLayer) != null) return  1;
        if (Physics2D.OverlapBox(pos + Vector2.left  * wallCheckDist, colliderSize, 0f, groundLayer) != null) return -1;
        return 0;
    }

    void WrapPosition(ref Vector2 pos)
    {
        Camera cam = Camera.main;
        float width = cam.orthographicSize * 2f * cam.aspect;
        float halfW = width * 0.5f;
        float camX  = cam.transform.position.x;

        if (pos.x > camX + halfW) pos.x -= width;
        else if (pos.x < camX - halfW) pos.x += width;
    }
}
