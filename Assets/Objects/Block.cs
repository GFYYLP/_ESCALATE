using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : PhysicsBody
{
    [SerializeField] private float gravity      = 28f;
    [SerializeField] private float maxFallSpeed = 16f;
    public SpriteRenderer sr;
    public bool pendingDestroy;

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable() => PhysicsManager.Instance.RegisterBlock(this);
    void OnDisable() => PhysicsManager.Instance?.UnregisterBlock(this);

    public void Step(float dt)
    {
        velocity.y   -= gravity * dt;
        velocity.y    = Mathf.Max(velocity.y, -maxFallSpeed);
        candidatePos += velocity * dt;

        if (candidatePos.y < -10f)
            pendingDestroy = true;
    }
}