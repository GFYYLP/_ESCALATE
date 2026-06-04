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
    [SerializeField] private float warpChargeCap = 20f;
    
    //restitution response
    
    
    //state 
    private bool  isDashing;
    private bool  dashUsed;
    private bool canWarp;
    private bool canReflect;
    private float warpCharge;
    private bool  isJumping;
    private float dashTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float reflectVal=0f;
    private Vector2 reflectDir = default;
    private float lastDir = 1f;
    private Vector2 dirVal = default;
    private Vector2 preWarpPos = default;

    public override void UpdateVelocity(float dt)
    {
        float reflectCondition = ((nearBlock) ? 0.3f : 0f)
            + ((isDashing) ? 0.7f : 0f);
        reflectVal = reflectCondition;
        
        // Read 8-directional input from arrow keys or WASD
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;

        // Default to last horizontal direction if no input
        if (x == 0f && y == 0f)
            x = lastDir;
        
        //flip sprite
        //if (x != 0f)
            visual.localScale = new Vector3(Mathf.Sign(-x), 1f, 1f);

        dirVal = new Vector2(x, y).normalized;
        dirVal.y   *= 0.75f;
        
        UpdateTimers(dt);
        //TryLatch();
        TryDash();
        TryReflect();
        TryWarp();
        
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
            candidatePos = new Vector2(0f, 1.5f);
        }
        
    }

    public void LateUpdate()
    {
        preWarpPos = default; //reset warping state;
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


        velocity = dirVal.normalized * dashSpeed;

        isDashing = true;
        dashUsed  = true;
        dashTimer = dashDuration;
         //isJumping = false;
        
    }
    

    void EndDash()
    {
        isDashing  = false;
        velocity.x = Mathf.Clamp(velocity.x, -dashEndHCap, dashEndHCap);
        if (velocity.y > 0f) velocity.y = 0f;
    }
    
    public void TryLatch()
    {
        Block block = collidedBody as Block;
        if (block == null) return;

        if (collidedToSide)
        {
            // hold direction required for side contact
            float inputX = Input.GetKey(KeyCode.RightArrow) ?  1f :
                Input.GetKey(KeyCode.LeftArrow)  ? -1f : 0f;
            Vector2 delta  = candidatePos - block.candidatePos;
            bool holding = Mathf.Floor(dirVal.x) == -Mathf.Sign(delta.x);

            if (holding)
                //inherit block's movement
                velocity.y = block.velocity.y;

        }
        else if (onGround)
        {
            //automatic top contact
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
        canReflect = false; 
        //reflect trigger conditions
        if (!nearBlock || !isDashing) return;
        canReflect = true;
        
        if (!Input.GetKeyDown(KeyCode.Z)) return;

        // Reflect dash velocity off the contact normal
        //Vector2 reflected = Vector2.Reflect(velocity, nearNormal);
        velocity          *= 3.0f;  
        isDashing         = false;
        dashUsed          = false;  // refund dash
        nearBlock         = false;
        isJumping         = true;
        
        //apply grid vfx
        //rippleManager.AddPointRipple(candidatePos, Speed*4.0f);
    }
    
    void TryWarp()
    {
        warpCharge += Speed;
        
        if (!Input.GetKeyDown(KeyCode.C) || warpCharge < warpChargeCap) return;

        Vector2 target = candidatePos + dirVal * 6f;
        
        Vector2 safePos = target;

        preWarpPos = candidatePos;
        candidatePos       = safePos;

        warpCharge = 0f;
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
    public Vector2 PreWarpPos => preWarpPos;
    
    public bool DashUsed  => dashUsed;
    public bool CanReflect => canReflect;
    public bool CanWarp(){
        if (warpCharge < warpChargeCap) return false;
        return true;
    }

    public float WarpProgress()
    {
        return warpCharge / warpChargeCap;
    }
}
