using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

[RequireComponent(typeof(LeapXRServiceProvider))]
public class ManualHeadOffsetCalibrationControl : MonoBehaviour
{
	[SerializeField]
	float _speed = 0.02f;

	[SerializeField]
	float _tiltSpeed = 3f;

	[SerializeField]
	bool _saveSettings = true;

	[SerializeField]
	string _playerPrefKey = "deviceoffset_control";


	[SerializeField, Range(0.5f, 10)]
	float _timeBeforeWrite = 2f;

	float _lastChangeTime = 0;
    Vector4 _lastSetting;
    Vector4 _cachedSetting;

    LeapXRServiceProvider _provider;

    void Start()
    {
        _provider = GetComponent<LeapXRServiceProvider>();

        Vector4 setting = new Vector4(_provider.deviceOffsetXAxis, _provider.deviceOffsetYAxis, _provider.deviceOffsetZAxis, _provider.deviceTiltXAxis);

        if (GetSavedOffset(ref setting))
        {
            _lastSetting = _cachedSetting = setting;
            Debug.Log("retrieved saved offset");
            _provider.deviceOffsetXAxis = setting.x;
            _provider.deviceOffsetYAxis = setting.y;
            _provider.deviceOffsetZAxis = setting.z;
            _provider.deviceTiltXAxis = setting.w;
        }
    }

    void Update()
    {
        var direction = Vector4.zero;
		if (Input.GetKey(KeyCode.Keypad4))
		{
			direction.x -= 1;
		}
		if (Input.GetKey(KeyCode.Keypad6))
		{
			direction.x += 1;
		}
		if (Input.GetKey(KeyCode.Keypad8))
		{
			direction.z += 1;
		}
		if (Input.GetKey(KeyCode.Keypad5))
		{
			direction.z -= 1;
		}
		if (Input.GetKey(KeyCode.KeypadPlus))
		{
			direction.y += 1;
		}
		if (Input.GetKey(KeyCode.KeypadMinus))
		{
			direction.y -= 1;
		}
		if (Input.GetKey(KeyCode.Keypad7))
		{
			direction.w -= 1;
		}
		if (Input.GetKey(KeyCode.Keypad9))
		{
			direction.w += 1;
		}

        Vector4 setting = new Vector4(_provider.deviceOffsetXAxis, _provider.deviceOffsetYAxis, _provider.deviceOffsetZAxis, _provider.deviceTiltXAxis);
        setting += new Vector4(
            direction.x * _speed,
            direction.y * _speed,
            direction.z * _speed,
            direction.w * _tiltSpeed
        ) * Time.deltaTime;
        _provider.deviceOffsetXAxis = setting.x;
        _provider.deviceOffsetYAxis = setting.y;
        _provider.deviceOffsetZAxis = setting.z;
        _provider.deviceTiltXAxis = setting.w;

		if (setting != _cachedSetting)
		{
			if (_lastSetting != setting)
			{
				_lastSetting = setting;
				_lastChangeTime = Time.time;
			}
			else if (Time.time - _lastChangeTime > _timeBeforeWrite)
			{
				_cachedSetting = setting;
				SaveOffset(setting);
			}
		}

    }

    bool GetSavedOffset(ref Vector4 setting)
    {
        float x;
        string xKey = _playerPrefKey + "_x";;
        if (!PlayerPrefs.HasKey(xKey))
            return false;
        x = PlayerPrefs.GetFloat(xKey);

        float y;
        string yKey = _playerPrefKey + "_y";
        if (!PlayerPrefs.HasKey(yKey))
            return false;
        y = PlayerPrefs.GetFloat(yKey);

        float z;
        string zKey = _playerPrefKey + "_z";
        if (!PlayerPrefs.HasKey(zKey))
            return false;
        z = PlayerPrefs.GetFloat(zKey);

        float w;
        string wKey = _playerPrefKey + "_w";
        if (!PlayerPrefs.HasKey(wKey))
            return false;
        w = PlayerPrefs.GetFloat(wKey);

        setting = new Vector4(x, y, z, w);
        return true;
    }

	void SaveOffset(Vector4 setting)
	{
		if (!_saveSettings) return;

        string xKey = _playerPrefKey + "_x";;
        PlayerPrefs.SetFloat(xKey, setting.x);
        string yKey = _playerPrefKey + "_y";
        PlayerPrefs.SetFloat(yKey, setting.y);
        string zKey = _playerPrefKey + "_z";
        PlayerPrefs.SetFloat(zKey, setting.z);
        string wKey = _playerPrefKey + "_w";
        PlayerPrefs.SetFloat(wKey, setting.w);

        PlayerPrefs.Save();
		Debug.Log("saved calibration offset");
    }
}
