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
    [SerializeField] private string realText;
    [SerializeField] private TMP_Text textBox;
    [SerializeField] private int glitchFrames = 3;
    [SerializeField] private float frameDelay = 0.04f;
    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 8f;
    
    private static readonly char[] garbageChars =
        "█▓▒░╬╫╪┼ŧ§¥µ#%&@?!*".ToCharArray();

    void Start()
    {
        textBox.text = realText;
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
            if (chars[i] == '\n' || chars[i] == ' ') continue; // preserve layout
            if (Random.value < 0.5f)
                chars[i] = garbageChars[Random.Range(0, garbageChars.Length)];
        }
        return new string(chars);
    }
}