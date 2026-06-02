using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusic : MonoBehaviour
{
    [Header("Music Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public float fadeInDuration = 2f;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        volume = SettingsManager.MusicVolume;
        _audioSource.loop = true;
        _audioSource.volume = 0f;
        _audioSource.Play();

        StartCoroutine(FadeIn());
    }

    public void ApplyVolume(float v)
    {
        volume = v;
        if (_audioSource != null) _audioSource.volume = v;
    }

    System.Collections.IEnumerator FadeIn()
    {
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, volume, timer / fadeInDuration);
            yield return null;
        }

        _audioSource.volume = volume;
    }
}