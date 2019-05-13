using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    #region Singleton

    static SoundManager _instance;
    public static SoundManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();
            }
            return _instance;
        }
    }

    #endregion

    #region Fields

    AudioSource _audioSource;

    #endregion

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    #region Public methods

    public void PlayOneShot(AudioClip clip, float volume)
    {
        _audioSource.PlayOneShot(clip, volume);
    }

    #endregion
}
