using System;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public Sound[] sounds;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        foreach (Sound sound in sounds)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;

            // Apply the settings if it has been set.
            if (PlayerPrefs.HasKey("MusicVol"))
            {
                if (sound.name == "Game_Music")
                    sound.source.volume = sound.volume * PlayerPrefs.GetFloat("MusicVol");
                else
                    sound.source.volume = sound.volume * PlayerPrefs.GetFloat("SFXVol");
            }
        }
    }

    private void Start()
    {
        Play("Game_Music");
    }

    public void ApplyVolumeSettings()
    {
        if (!PlayerPrefs.HasKey("MusicVol")) return;

        foreach (Sound sound in sounds)
        {
            if (sound.name == "Game_Music")
                sound.source.volume = sound.volume * PlayerPrefs.GetFloat("MusicVol");
            else
                sound.source.volume = sound.volume * PlayerPrefs.GetFloat("SFXVol");
        }
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogError("Sound " + name + " was not found !!");
            return;
        }

        s.source.Play();
    }

    public void PlayWithPosition(string name, Vector3 soundPos)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogError("Sound " + name + " was not found !!");
            return;
        }

        // Create the sound object wih position
        GameObject soundObject = new GameObject(name);
        soundObject.transform.position = soundPos;

        // Add auodio to it
        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = s.clip;
        source.volume = s.volume;
        source.pitch = s.pitch;
        source.loop = s.loop;

        // Set 3D attributes
        source.minDistance = 0.1f;
        source.maxDistance = 30f;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.Play();

        StartCoroutine(DestroyOnDelay(soundObject));
    }

    IEnumerator DestroyOnDelay(GameObject obj)
    {
        yield return new WaitForSeconds(2f);
        Destroy(obj);
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);

        if (s == null)
        {
            Debug.LogError("Sound " + name + " was not found !!");
            return;
        }

        s.source.Stop();
    }
}