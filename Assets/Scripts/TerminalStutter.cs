using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using UnityEngine;
using TMPro;

using System.Collections;
using UnityEngine;
using TMPro;

public class TerminalStutter : MonoBehaviour
{
    [TextArea(5, 100)]
    private string realText;
    private TMP_Text textBox;
    
    [SerializeField] private int glitchFrames = 3;
    [SerializeField] private float frameDelay = 0.04f;
    [SerializeField] private float minInterval = 1f;
    [SerializeField] private float maxInterval = 4f;
    
    private static readonly char[] garbageChars =
        "█▓▒░╬╫╪┼ŧ§¥µ#%&@?!*".ToCharArray();

    void Start()
    {
        textBox = GetComponent<TMP_Text>();
        realText = textBox.text;
        
        StartCoroutine(GlitchLoop());
    }

    IEnumerator GlitchLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(minInterval, maxInterval));
            yield return GlitchIn();
        }
    }

    IEnumerator GlitchIn()
    {
        for (int i = 0; i < glitchFrames; i++)
        {
            textBox.text = Garble(realText);
            yield return new WaitForSecondsRealtime(frameDelay);
        }

        textBox.text = realText;
    }

    string Garble(string source)
    {
        var chars = source.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] == '\n' || chars[i] == ' ') continue; //  layout
            if (Random.value < 0.5f)
                chars[i] = garbageChars[Random.Range(0, garbageChars.Length)];
        }
        return new string(chars);
    }
}