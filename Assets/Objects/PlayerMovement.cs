using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // --- Movement ---
    [SerializeField] private float moveSpeed      = 9f;
    [SerializeField] private float groundAccel    = 100f;
    [SerializeField] private float airAccel       = 65f;
    [SerializeField] private float groundFriction = 80f;
    [SerializeField] private float airFriction    = 40f;

    // --- Jump ---
    [SerializeField] private float jumpForce         = 11f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime        = 0.15f;
    [SerializeField] private float jumpBufferTime    = 0.2f;

    // --- Wall jump ---
    // No special bounce state — just a strong horizontal kick on jump near wall.
    [SerializeField] private float wallJumpHSpeed    = 13f;
    [SerializeField] private float wallJumpVSpeed    = 11f;
    [SerializeField] private float wallCheckDist     = 0.08f;
    [SerializeField] private float wallCoyoteTime    = 0.1f;

    // --- Gravity ---
    [SerializeField] private float gravity         = 28f;
    [SerializeField] private float maxFallSpeed    = 16f;
    [SerializeField] private float fastFallGravity = 40f;
    [SerializeField] private float maxFastFall     = 24f;

    // --- Dash ---
    // Horizontal speed is never zeroed on landing — only downward velocity is.
    // This means a horizontal dash that lands gives you full dash speed carried forward,
    // which is what makes wavedash/hyperdash emerge without being explicitly coded.
    [SerializeField] private float dashSpeed      = 24f;
    [SerializeField] private float dashDuration   = 0.25f;
    // On dash end, horizontal speed decays toward moveSpeed naturally via friction.
    // We only hard-cap it to prevent indefinite hyperspeed.
    [SerializeField] private float dashEndHCap    = 20f;

    // --- Collision ---
    [SerializeField] private Vector2 colliderSize = new Vector2(0.9f, 1.8f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float stepHeight  = 0.15f;
    [SerializeField] private float groundProbe = 0.08f;
    [SerializeField] private float highCollideVal = 5.0f;
    private bool collided = false;
    private float collisionPos = 0.0f;  //horizontal collision point
    public event Action  onHighCollision;

    // --- State ---
    private Vector2 velocity;
    private bool    onGround;
    private bool    isDashing;
    private bool    dashUsed;
    private bool    isJumping;
    private float   dashTimer;
    private float   coyoteTimer;
    private float   jumpBufferTimer;


    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    void UpdateTimers(float dt)
    {
        //register jump input, count down if not used this frame
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

        // Refresh coyote while grounded, count down when airborne
        if (onGround){
            coyoteTimer = coyoteTime;
            dashUsed = false;
        }  
        else
            coyoteTimer -= dt;

        //dash decay
        if (isDashing){
            dashTimer -= dt;
            if (dashTimer <= 0f)
                EndDash();
        }
    }

    // -------------------------------------------------------------------------
    // Dash
    // -------------------------------------------------------------------------

    void TryDash()
    {
        if (!Input.GetMouseButtonDown(0) || isDashing || dashUsed) return;

        //dash direction
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir        = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.001f) return;
        velocity  = dir.normalized * dashSpeed;

        isDashing = true;
        dashUsed  = true;
        dashTimer = dashDuration;
        isJumping = false;
    }

    void EndDash()
    {
        isDashing = false;

        //cap horizontal speed
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);

        // Kill upward velocity so you don't float after an upward dash,
        // but preserve downward so diagonal-down dashes keep their arc.
        if (velocity.y > 0f) velocity.y = 0f;
    }

    // -------------------------------------------------------------------------
    // Horizontal
    // -------------------------------------------------------------------------

    void ApplyHorizontal(float dt)
    {
        if (isDashing) return;  //ignores current horizontal input while dashing

        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input -= 1f;
        if (Input.GetKey(KeyCode.D)) input += 1f;

        if (input != 0f)
        {
            //accelerates towards target
            float accel  = onGround ? groundAccel : airAccel;
            float target = input * moveSpeed;
            velocity.x = Mathf.MoveTowards(velocity.x, target, accel * dt);
        }
        else
        {
            //applies friction when acceleration is not present
            float friction = onGround ? groundFriction : airFriction;
            velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
        }
    }

    void ApplyVertical(float dt)
    {
        if (isDashing && !onGround) return;

        if (jumpBufferTimer > 0f)
        {
            // Normal jump
            if (coyoteTimer > 0f)
            {
                velocity.y      = jumpForce;

                // Reset timers and flags so that you can hold jump and have it cut off at the right height, 
                // and so that you can buffer another jump immediately after this one if you want to chain them.
                jumpBufferTimer = 0f;
                coyoteTimer     = 0f;
                isJumping       = true;
            }
        }

        // Variable jump height
        if (isJumping && Input.GetKeyUp(KeyCode.Space) && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            isJumping   = false;
        }

        // Gravity
        bool fastFall  = Input.GetKey(KeyCode.S) && velocity.y < 0f;
        float grav     = fastFall ? fastFallGravity : gravity;
        float cap      = fastFall ? maxFastFall     : maxFallSpeed;

        velocity.y -= grav * dt;
        if (velocity.y < -cap) velocity.y = -cap;
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
        //horizontal is kept intact
        // this allows for things like wavedash where you retain horizontal speed after a dash when you land.
        bool wasOnGround = onGround;
        onGround = CheckGround(pos);
        if (onGround && velocity.y < 0f)
        {
            //if (isDashing) EndDash();
            velocity.y = 0f;
            isJumping  = false;

            if (isDashing)
            {
                EndDash();
                dashUsed = false;  // refund so you can dash again after the jump
            }
        }

        // applies coyote if we walked off a ledge this frame
        if (wasOnGround && !onGround && !isDashing)
            coyoteTimer = coyoteTime;

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

        collided = true;
        collisionPos = transform.position.y;
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
        {
            pos.y -= sign * step;
            
            collided = true;
            collisionPos = transform.position.x;
        }
    }

    velocity.y = 0f;
}
    // -------------------------------------------------------------------------
    // Frame setup
    // -------------------------------------------------------------------------
    void Update()
    {
        float dt = Time.deltaTime;

        // Ground/wall state is already set from last frame's MoveAndCollide.
        // This means friction and jump both see the correct grounded state
        // without waiting an extra frame.
        UpdateTimers(dt);
        TryDash();
        ApplyHorizontal(dt);   // friction runs here using last frame's onGround
        ApplyVertical(dt);     // jump fires here — before this frame's landing
        MoveAndCollide(dt);    // landing updates onGround for next frame

        if (Speed > highCollideVal && collided)
        {
            onHighCollision?.Invoke();
            collided = false;
        }
        else
        {
            collided = false;
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
    public bool IsDashing   => isDashing;
    public float CollisionPos => collisionPos;

    // -------------------------------------------------------------------------
    // Collision helpers
    // -------------------------------------------------------------------------
    bool Overlaps(Vector2 pos)
    {
        return Physics2D.OverlapBox(pos, colliderSize, 0f, groundLayer) != null;
    }

    bool CheckGround(Vector2 pos)
    {
        return Physics2D.OverlapBox(pos + Vector2.down * groundProbe, colliderSize, 0f, groundLayer) != null;
    }

    int CheckWall(Vector2 pos)
    {
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