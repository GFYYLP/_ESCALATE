using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 originalPosition;
    
    private void Start()
    {
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    // Update is called once per frame
    public void UpdateBar(int percent)
    {
        float scaledWidth = originalScale.x * percent;

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
    }
}