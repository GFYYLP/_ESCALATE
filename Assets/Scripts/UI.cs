using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CounterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text heightText;
    [SerializeField] private Player player;

    public void Update()
    {
        int heightVal = Mathf.Max((int)player.transform.position.y, 0);
        heightText.text = (heightVal).ToString();
    }

}
