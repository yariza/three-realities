using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundLibrary : Singleton<SoundLibrary> {


    [SerializeField]
    bool _spatialize = true;

    [SerializeField]
    float _defaultVolume = 1f;

    [SerializeField]
    AudioClip[] _clips;

    [SerializeField]
    Transform[] _transforms;

    [SerializeField]
    int _numSources = 3;
    AudioSource[] _sources;

	int[] _numPlays;


	public override void Awake() {
        base.Awake();
        _sources = new AudioSource[_numSources];
		_numPlays = new int[_numSources];

        for (int i = 0; i < _numSources; i++)
        {
            GameObject go = new GameObject();
            go.hideFlags = HideFlags.HideInHierarchy;
            _sources[i] = go.AddComponent<AudioSource>();
            _sources[i].spatialize = _spatialize;
            _sources[i].spatialBlend = (_spatialize) ? 1f : 0f;
            _sources[i].playOnAwake = false;
        }
	}

    public int Play(string title, float volume = -1f, int transformIndex = -1, Vector3 offset = new Vector3(), bool relativePosition = true)
    {
        AudioSource source = null;
		int index = -1;
        for (int i = 0; i < _numSources; i++)
        {
            if (!_sources[i].isPlaying)
            {
                source = _sources[i];
				index = i;
				break;
            }
        }

        if (source == null)
        {
            Debug.Log("[SoundLibrary] No available Audio_sources on " + name + " to play \"" + title + "\"");
            return -1;
        }

        AudioClip clip = null;
        foreach (AudioClip c in _clips)
        {
            if (c.name == title)
            {
                clip = c;
                break;
            }
        }

        if (clip == null)
        {
            Debug.Log("[SoundLibrary] No clip of \"" + title + "\" attached to " + name);
            return -1;
        }

        Transform t = (transformIndex >= 0 && transformIndex < _transforms.Length) ? _transforms[transformIndex] : transform;
        source.transform.parent = (relativePosition) ? t : null;
        source.transform.position = (relativePosition) ? t.position + offset : offset;
		source.clip = clip;
		source.volume = (volume >= 0) ? volume : _defaultVolume;
		source.loop = false;
		source.spatialize = _spatialize;
		source.spatialBlend = (_spatialize) ? 1f : 0f;

		_numPlays[index]++;
		source.Play();

        return index;
    }

    public int PlayLoop(string title, float volume = -1f, float fadeTime = 0f, bool spatial = true, int transformIndex = -1, Vector3 offset = new Vector3(), bool relativePosition = true)
    {
		AudioSource source = null;
		int index = -1;
		for (int i = 0; i < _numSources; i++)
		{
			if (!_sources[i].isPlaying)
			{
				source = _sources[i];
				index = i;
				break;
			}
		}

		if (source == null)
		{
			Debug.Log("[SoundLibrary] No available Audio_sources on " + name + " to play \"" + title + "\"");
			return -1;
		}

		AudioClip clip = null;
        foreach (AudioClip c in _clips)
        {
            if (c.name == title)
            {
                clip = c;
                break;
            }
        }

        if (clip == null)
		{
			Debug.Log("[SoundLibrary] No clip of \"" + title + "\" attached to " + name);
			return -1;
        }

        Transform t = (transformIndex >= 0 && transformIndex < _transforms.Length) ? _transforms[transformIndex] : transform;
        source.transform.parent = (relativePosition) ? t : null;
        source.transform.position = (relativePosition) ? t.position + offset : offset;
        source.clip = clip;
        source.volume = (volume >= 0) ? volume : _defaultVolume;
        source.loop = true;
        source.spatialize = spatial;
        source.spatialBlend = (spatial) ? 1 : 0;

		_numPlays[index]++;
		source.Play();

        return index;
    }

	public int PlayCustom(AudioClip audio, float volume = -1f, int transformIndex = -1, Vector3 offset = new Vector3(), bool relativePosition = true, float pitch = 1f, bool forcePlay = false)
    {
		AudioSource source = null;
		int index = -1;
		for (int i = 0; i < _numSources; i++)
		{
			if (!_sources[i].isPlaying)
			{
				source = _sources[i];
				index = i;
				break;
			}
		}

		if (source == null)
        {
            if (forcePlay)
            {
                index = 0;
            }
            else
            {
                Debug.Log("[SoundLibrary] No available Audio_sources on " + name + " to play \"" + audio.name + "\"");
                return -1;
            }
        }

        Transform t = (transformIndex >= 0 && transformIndex < _transforms.Length) ? _transforms[transformIndex] : transform;
		source.transform.parent = (relativePosition)? t : null;
        source.transform.position = (relativePosition)? t.position + offset : offset;
		source.clip = audio;
        source.pitch = pitch;
		source.volume = (volume >= 0) ? volume : _defaultVolume;
		source.loop = false;
		source.spatialize = _spatialize;
		source.spatialBlend = (_spatialize) ? 1f : 0f;

		_numPlays[index]++;
		source.Play();

		return index;
    }

	public void FadeIn(int index, float fadeTime, float endVolume = -1f)
    {
        if (index >= 0 && index < _numSources)
        {
            StartCoroutine(Slide(index, fadeTime, endVolume));
        }
    }

    public void FadeOut(int index, float fadeTime)
    {
        if (index >= 0 && index < _numSources)
        {
            StartCoroutine(Slide(index, fadeTime, 0f));
        }
    }

    public bool AdjustVolume(int index, float volume)
    {
        if (index >= _numSources || index < 0)
        {
            Debug.Log("[SoundLibrary] Trying to access invalid index in SoundLibrary");
            return false;
        }

        AudioSource source = _sources[index];
        _sources[index].volume = volume;
        return true;
    }

    public void Stop(int index)
    {
		if (index >= 0 && index < _numSources)
		{
			_sources[index].Stop();
		}
		else
		{
			Debug.Log("[SoundLibrary] Index must be within range.");
		}
    }

	public bool HasStopped(int index)
	{
		if (index >= 0 && index < _numSources)
		{
			return !_sources[index].isPlaying;
		}
		else
		{
			Debug.Log("[SoundLibrary] Index must be within range.");
			return false;
		}
	}

	public int NumPlays(int index)
	{
		if (index >= 0 && index < _numSources)
		{
			return _numPlays[index];
		}
		else
		{
			Debug.Log("[SoundLibrary] Index must be within range.");
			return -1;
		}
	}

    public float GetLength(string title)
    {
        AudioClip clip = null;
        foreach (AudioClip c in _clips)
        {
            if (c.name == title)
            {
                clip = c;
                break;
            }
        }

        if (clip == null)
        {
            return -1f;
        }

        return clip.length;
    }

    IEnumerator Slide(int srcIdx, float fadeTime, float endVolume)
    {
        float step = (endVolume - _sources[srcIdx].volume) / fadeTime;

        for (float f = 0f; f <= fadeTime; f += .01f)
        {
            _sources[srcIdx].volume += step;
            yield return new WaitForSeconds(.01f);
        }

        _sources[srcIdx].volume = endVolume;

        if (endVolume == 0f)
        {
            _sources[srcIdx].Stop();
        }
    }
}
