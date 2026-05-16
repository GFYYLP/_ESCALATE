using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsBody
{
    //movement
    [SerializeField] private float moveSpeed      = 9f;
    [SerializeField] private float groundAccel    = 100f;
    [SerializeField] private float airAccel       = 65f;
    [SerializeField] private float groundFriction = 80f;
    [SerializeField] private float airFriction    = 40f;

    //jump 
    [SerializeField] private float jumpForce         = 11f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime        = 0.15f;
    [SerializeField] private float jumpBufferTime    = 0.2f;

    //gravity
    // gravity and maxFallSpeed inherited from PhysicsBody
    [SerializeField] private float fastFallGravity = 40f;
    [SerializeField] private float maxFastFall     = 24f;
    [SerializeField] private float gravity     = 28f;
    [SerializeField] private float maxFallSpeed = 16f;

    //dash
    [SerializeField] private float dashSpeed    = 24f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashEndHCap  = 20f;
    [SerializeField] private float reflectSensitivity    = 0.3f;
    
    //restitution response
    
    
    //state 
    private bool  isDashing;
    private bool  dashUsed;
    private bool  isJumping;
    private float dashTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float reflectVal=0f;
    private Vector2 reflectDir = default;
    private float lastDir = 1f;

    public override void UpdateVelocity(float dt)
    {
        float reflectCondition = ((nearBlock) ? 0.3f : 0f)
            + ((isDashing) ? 0.7f : 0f);
        reflectVal = reflectCondition;
        
        
        
        UpdateTimers(dt);
        TryDash();
        TryReflect();
        ApplyHorizontal(dt);
        ApplyVertical(dt);
        
        nearBlock = false;  // reset each frame, AABBOverlap sets it if close
        
        //cap velocity
        const float maxVelocity = 40f;
        velocity.x = Mathf.Clamp(velocity.x, -maxVelocity, maxVelocity);
        velocity.y = Mathf.Clamp(velocity.y, -maxVelocity*0.5f, maxVelocity*0.5f);  //less vertical freedom
        
        if (Input.GetKey(KeyCode.R))
        {
            velocity = new Vector2();
            candidatePos = new Vector2(0f, 2.5f);
        }
    }

    public override void UpdateProximity(float overlapX, float overlapY)
    {
        float proximity = (overlapX > overlapY)? Mathf.Abs(overlapX) : Mathf.Abs(overlapY);
        float reflectCondition = (isDashing) ? 1f : 0f;
        reflectVal = reflectCondition; //proximity * reflectSensitivity * ;
        
        reflectDir = new Vector2(-overlapX, overlapY).normalized;
    }
    

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
         //isJumping = false;
        
        //apply dash vfx
        rippleManager.AddPointRipple(candidatePos, Speed);
    }
    

    void EndDash()
    {
        isDashing  = false;
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);
        if (velocity.y > 0f) velocity.y = 0f;
    }
    
    public override void TryLatch(PhysicsBody collidedBody, bool isSide)
    {
        Block block = collidedBody as Block;
        if (block == null) return;

        if (isSide)
        {
            // Side contact — hold direction required
            float inputX = Input.GetKey(KeyCode.RightArrow) ?  1f :
                Input.GetKey(KeyCode.LeftArrow)  ? -1f : 0f;
            Vector2 delta  = candidatePos - block.candidatePos;
            bool holding = Mathf.Sign(inputX) == -Mathf.Sign(delta.x);

            if (holding)
                // Match block's vertical movement — ride it up or down
                velocity.y = block.velocity.y;

        }
        else if (onGround)
        {
            // Top contact — automatic, match horizontal
            velocity.x += block.velocity.x;
        }
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
    
    void TryReflect()
    {
        //reflect trigger conditions
        if (!nearBlock || !isDashing) return;
        if (!Input.GetKeyDown(KeyCode.Z)) return;

        // Reflect dash velocity off the contact normal
        Vector2 reflected = Vector2.Reflect(velocity, nearNormal);
        velocity          *= 3.0f;  
        isDashing         = false;
        dashUsed          = false;  // refund dash
        nearBlock         = false;
        isJumping         = true;
        
        //apply grid vfx
        rippleManager.AddPointRipple(candidatePos, Speed*4.0f);
    }
    

    void ApplyVertical(float dt)
    {
        if (isDashing && !onGround) return;

        //jumping window
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y      = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer     = 0f;
            isJumping       = true;
        }

        //apply jump
        if (isJumping && Input.GetKeyUp(KeyCode.Z) && velocity.y > 0f)
        {
            velocity.y *= jumpCutMultiplier;
            isJumping   = false;
        } else if (reflectVal > 0.2f && Input.GetKeyUp(KeyCode.Z) && velocity.y > 0f)
        {
            velocity += reflectDir;
            isJumping   = false;
        }

        //apply gravity
        bool  fastFall = Input.GetKey(KeyCode.DownArrow) && velocity.y < 0f;
        float grav     = fastFall ? fastFallGravity : gravity;
        float cap      = fastFall ? maxFastFall     : maxFallSpeed;

        velocity.y -= grav * dt;
        if (velocity.y < -cap) velocity.y = -cap;
    }
    
    // Public data
    public bool IsDashing => isDashing;
    public float ReflectVal => reflectVal;
}
