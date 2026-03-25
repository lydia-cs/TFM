using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Handles background music playlist and one-shot sound effects with ducking
public class AudioManager : MonoBehaviour
{
    // Music Playback
    public AudioSource musicSource;                 // For looping background music
    public List<AudioClip> musicPlaylist = new List<AudioClip>();
    private int currentMusicIndex = 0;
    private Coroutine musicCoroutine;
    [Range(0f, 1f)] public float musicVolume = 1f; // Normal music volume

    // Sound Effects
    public AudioSource sfxSource;                   // For one-shot sounds (dialogue)
    public float duckingVolume = 0.5f;              // Volume for music during SFX

    // Music Playlist Management
    /// Starts playing a playlist of background music
    public void PlayMusicPlaylist(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0)
        {
            Debug.LogWarning("No music clips to play.");
            return;
        }

        musicPlaylist = clips;
        currentMusicIndex = 0;

        if (musicCoroutine != null)
            StopCoroutine(musicCoroutine);

        musicCoroutine = StartCoroutine(PlayMusicSequence());
    }

    /// Plays music sequentially with fade-in/out between tracks
    private IEnumerator PlayMusicSequence()
    {
        float fadeDuration = 2f;

        while (true)
        {
            AudioClip clip = musicPlaylist[currentMusicIndex];
            if (clip != null)
            {
                // Fade in
                musicSource.clip = clip;
                musicSource.volume = 0f;
                musicSource.Play();

                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(0f, musicVolume, t / fadeDuration);
                    yield return null;
                }
                musicSource.volume = musicVolume;

                // Wait until near the end to start fade out
                yield return new WaitForSeconds(clip.length - fadeDuration);

                // Fade out
                t = 0f;
                float startVolume = musicSource.volume;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                    yield return null;
                }
            }

            // Move to next track
            currentMusicIndex = (currentMusicIndex + 1) % musicPlaylist.Count;
        }
    }

    // Sound Effects with Ducking
    /// Plays a one-shot sound effect with temporary music ducking
    public void PlaySFX(AudioClip clip)
    {
        StartCoroutine(PlayWithDucking(clip));
    }

    public void PauseDialogue()
    {
        if (sfxSource != null && sfxSource.isPlaying)
            sfxSource.Pause();
    }

    public void ResumeDialogue()
    {
        if (sfxSource != null)
            sfxSource.UnPause();
    }


    private IEnumerator PlayWithDucking(AudioClip clip)
    {
        // Lower music volume
        musicSource.volume = duckingVolume;

        // Play the SFX
        sfxSource.PlayOneShot(clip, musicVolume);
        //pitch

        // Wait until SFX finishes
        yield return new WaitForSeconds(clip.length);

        // Restore music volume
        musicSource.volume = musicVolume;
    }
}


