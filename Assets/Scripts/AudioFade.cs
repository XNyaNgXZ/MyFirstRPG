using UnityEngine;
using System.Collections;

public class AudioFade : MonoBehaviour
{
    [Header("Fade In")]
    public float fadeInDuration = 2f;
    public float targetVolume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;

        audioSource.volume = 0f;
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }
}