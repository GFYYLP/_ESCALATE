using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsBody
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
    [SerializeField] private float wallJumpHSpeed = 13f;
    [SerializeField] private float wallJumpVSpeed = 11f;
    [SerializeField] private float wallCheckDist  = 0.08f;
    [SerializeField] private float wallCoyoteTime = 0.1f;

    // --- Gravity ---
    // gravity and maxFallSpeed inherited from PhysicsBody
    [SerializeField] private float fastFallGravity = 40f;
    [SerializeField] private float maxFastFall     = 24f;
    [SerializeField] private float gravity     = 28f;
    [SerializeField] private float maxFallSpeed = 16f;

    // --- Dash ---
    [SerializeField] private float dashSpeed    = 24f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashEndHCap  = 20f;

    // --- Collision trigger ---
    [SerializeField] private float highCollideVal = 5f;
    public event Action<Vector2> onHighCollision;

    // --- State ---
    private bool  isDashing;
    private bool  dashUsed;
    private bool  isJumping;
    private float dashTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float lastDir = 1f;  // default facing right

    public override void UpdateVelocity(float dt)
    {
        UpdateTimers(dt);
        TryDash();
        ApplyHorizontal(dt);
        ApplyVertical(dt);
        
        if (Input.GetKey(KeyCode.R))
        {
            candidatePos = new Vector2(0f, 3f);
        }
        
        if (Input.GetKey(KeyCode.S))
        {
            onHighCollision?.Invoke(candidatePos);
        }
    }

    public override void OnImpact(float impactSpeed, PhysicsBody other)
    {
        if (impactSpeed > highCollideVal)
            onHighCollision?.Invoke(candidatePos);
    }

    public override void ApplyImpulse(Vector2 impulse)
    {
        base.ApplyImpulse(impulse);
        isJumping = false;
    }

    // -------------------------------------------------------------------------
    // Timers
    // -------------------------------------------------------------------------

    void UpdateTimers(float dt)
    {
        if (Input.GetKeyDown(KeyCode.Z))
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;

        if (onGround)
        {
            coyoteTimer = coyoteTime;
            dashUsed    = false;
        }
        else
            coyoteTimer -= dt;

        if (isDashing)
        {
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
        if (!Input.GetKeyDown(KeyCode.X) || isDashing || dashUsed) return;

        // Read 8-directional input from arrow keys or WASD
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;

        // Default to last horizontal direction if no input
        if (x == 0f && y == 0f)
            x = lastDir;

        Vector2 dir = new Vector2(x, y).normalized;

        // Skew diagonal to favour horizontal
        dir.y   *= 0.75f;
        velocity = dir.normalized * dashSpeed;

        isDashing = true;
        dashUsed  = true;
        dashTimer = dashDuration;
        isJumping = false;
    }

    void EndDash()
    {
        isDashing  = false;
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);
        if (velocity.y > 0f) velocity.y = 0f;
    }
    void ApplyHorizontal(float dt)
    {
        if (isDashing) return;

        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) input += 1f;

        if (input != 0f)
        {
            lastDir    = Mathf.Sign(input);
            float accel  = onGround ? groundAccel : airAccel;
            velocity.x   = Mathf.MoveTowards(velocity.x, input * moveSpeed, accel * dt);
        }
        else
        {
            float friction = onGround ? groundFriction : airFriction;
            velocity.x     = Mathf.MoveTowards(velocity.x, 0f, friction * dt);
        }
    }

    // -------------------------------------------------------------------------
    // Vertical
    // -------------------------------------------------------------------------

    void ApplyVertical(float dt)
    {
        if (isDashing && !onGround) return;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y      = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            isJumping       = true;
        }

        if (isJumping && Input.GetKeyUp(KeyCode.Z) && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            isJumping   = false;
        }

        bool  fastFall = Input.GetKey(KeyCode.DownArrow) && velocity.y < 0f;
        float grav     = fastFall ? fastFallGravity : gravity;
        float cap      = fastFall ? maxFastFall     : maxFallSpeed;

        velocity.y -= grav * dt;
        if (velocity.y < -cap) velocity.y = -cap;
    }
    
    // -------------------------------------------------------------------------
    // Public data
    // -------------------------------------------------------------------------
    public bool IsDashing => isDashing;
}
