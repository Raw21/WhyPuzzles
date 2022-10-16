using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "ScriptableObjects/Sound effects")]
public class SoundEffects : ScriptableObject
{
    public SoundEffect puzzlePieceConnect;
    public SoundEffect puzzlePiecePutDown;
    public SoundEffect puzzlePiecePutInFinalPosition;


    public void PlayClip(AudioSource audio, SoundEffect soundEffects)
    {
        if (audio == null || !audio.enabled)
            return;

        if (audio.isPlaying)
            audio.Stop();

        var clip = soundEffects.clips[Random.Range(0, soundEffects.clips.Length)];
        
        audio.clip = clip;
        audio.volume = Random.Range(soundEffects.minVolume, soundEffects.maxVolume);
        audio.pitch = Random.Range(soundEffects.minPitch, soundEffects.maxPitch);
        audio.Play();
    }
}


[Serializable]
public class SoundEffect
{
    public AudioClip[] clips;
    public float minVolume = 0.8f;
    public float maxVolume = 1f;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
}
