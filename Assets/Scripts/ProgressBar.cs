using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    public event Action<Vector2> onHighCollision;
    
    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    // Update is called once per frame
    public int UpdateBar(float ratio)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f);

        float scaledWidth = originalScale.x * (percent / 100f);

        transform.localScale =
            new Vector3(
                scaledWidth,
                originalScale.y,
                originalScale.z
            );

        transform.localPosition =
            new Vector3(
                originalPosition.x
                - (originalScale.x - scaledWidth) * 0.5f,
                originalPosition.y,
                originalPosition.z
            );
        
        return percent;
    }

    void applyFlicker()
    {
        StartCoroutine(Flicker());
    }

    IEnumerator Flicker()
    {

        yield return null;
    }
}