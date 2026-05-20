using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : PhysicsBody
{
    [SerializeField] private float gravity     = 5f;
    [SerializeField] private float maxFallSpeed = 35f;
    
    

    public override void UpdateVelocity(float dt)
    {
        if (!isKinematic)
        {
            velocity.y -= gravity * dt;
            velocity.y  = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        if (candidatePos.y < -10f && velocity.y <= maxFallSpeed) pendingDestroy = true;
    }
}