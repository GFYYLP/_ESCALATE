using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private SpriteRenderer sr;  
    private Graphic graphic;  //for ui canvas

    private float displayRatio = 0f;
    private float prevRatio    = 0f;

    [SerializeField] private Color baseColor  = Color.white;
    [SerializeField] private Color flashColor = Color.white;

    private float flashTimer    = 0f;
    private bool isStrongFlash = false;
    private float scalePopTimer = 0f;
    const float FLASH_DURATION  = 0.12f;
    const float POP_DURATION    = 0.1f;
    const float TICK_THRESHOLD  = 0.19f;  
    
    static readonly Color[] flickerColors = {
        new Color(1f, 0f, 0.2f),   
        new Color(0f, 1f, 0.8f),  
        new Color(0.8f, 0f, 1f),  
        new Color(1f, 0.8f, 0f),  
        new Color(0f, 0.8f, 1f),   
        Color.white,
    };

    void Start()
    {
        originalScale    = transform.localScale;
        originalPosition = transform.localPosition;
        sr = GetComponent<SpriteRenderer>();
        graphic = GetComponent<Graphic>();
    }

    //returns current percent.
    public int UpdateBar(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        // detect events
        bool hitFull    = ratio >= 1f && prevRatio < 1f;
        bool consumed   = ratio < 0.4f && prevRatio > 0.5f;
        bool chargeTick = Mathf.Floor(ratio / TICK_THRESHOLD)
                        > Mathf.Floor(prevRatio / TICK_THRESHOLD)
                        && ratio < 1f;

        if (hitFull || consumed)
            TriggerFlash(strong: true);
        else if (chargeTick)
            TriggerFlash(strong: false);

        prevRatio    = ratio;
        displayRatio = ratio;

        ApplyScale(ratio);
        UpdateVisuals();

        return Mathf.RoundToInt(ratio * 100f);
    }

    void TriggerFlash(bool strong)
    {
        flashTimer = FLASH_DURATION * (strong ? 1f : 0.6f);
        isStrongFlash = strong;
        if (strong) scalePopTimer = POP_DURATION;
    }

    void UpdateVisuals()
    {
        flashTimer    = Mathf.Max(0f, flashTimer    - Time.deltaTime);
        scalePopTimer = Mathf.Max(0f, scalePopTimer - Time.deltaTime);

        if (flashTimer > 0f)
        {
            //rapid color index cycling, faster on strong flash
            float cycleRate = isStrongFlash ? 24f : 14f;
            int idx = Mathf.FloorToInt(Time.time * cycleRate) % flickerColors.Length;
            SetColor(flickerColors[idx]);
        }
        else
        {
            SetColor(baseColor);
        }

        //scale pop
        if (scalePopTimer > 0f)
        {
            float popT = scalePopTimer / POP_DURATION;
            float popY = 1f + 0.4f * Mathf.Sin(popT * Mathf.PI);
            transform.localScale = new Vector3(
                transform.localScale.x,
                originalScale.y * popY,
                originalScale.z
            );
        }
        else
        {
            transform.localScale = new Vector3(
                transform.localScale.x,
                originalScale.y,
                originalScale.z
            );
        }
    }

    void ApplyScale(float ratio)
    {
        float scaledWidth = originalScale.x * ratio;
        transform.localScale = new Vector3(
            scaledWidth,
            transform.localScale.y,   // preserve any active pop
            originalScale.z
        );
        transform.localPosition = new Vector3(
            originalPosition.x - (originalScale.x - scaledWidth) * 0.5f,
            originalPosition.y,
            originalPosition.z
        );
    }
    
    public void SetColor(Color color)
    {
        if (sr != null)
            sr.color = color;

        if (graphic != null)
            graphic.color = color;
    }
}