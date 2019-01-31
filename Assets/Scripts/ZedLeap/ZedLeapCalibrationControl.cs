using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZedLeapCalibrationControl : MonoBehaviour {

	[SerializeField]
	float _speed = 0.02f;

	[SerializeField]
	float _tiltSpeed = 3f;

	[SerializeField]
	List<Leap.Unity.CapsuleHand> _hands = new List<Leap.Unity.CapsuleHand>();

	[SerializeField]
	string _playerPrefKey = "leap_offset";

	[SerializeField, Range(0.5f, 10)]
	float _timeBeforeWrite = 2f;

	bool _showHands = false;

	float _lastChangeTime = 0;
	Vector3 _cachedLocalPosition;
	Quaternion _cachedLocalRotation;
	Vector3 _lastLocalPosition;
	Quaternion _lastLocalRotation;

	void Start()
	{
		Vector3 position = transform.localPosition;
		Quaternion rotation = transform.localRotation;

		if (GetSavedOffset(ref position, ref rotation))
		{
			_lastLocalPosition = _cachedLocalPosition = transform.localPosition = position;
			_lastLocalRotation = _cachedLocalRotation = transform.localRotation = rotation;
			Debug.Log("retrieved saved offset");
		}
	}

	// Update is called once per frame
	void Update ()
	{
		var direction = Vector3.zero;
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
		transform.localPosition += direction * _speed * Time.deltaTime;

		var tilt = 0f;
		if (Input.GetKey(KeyCode.Keypad7))
		{
			tilt -= 1;
		}
		if (Input.GetKey(KeyCode.Keypad9))
		{
			tilt += 1;
		}
		transform.Rotate(Vector3.right, tilt * _tiltSpeed * Time.deltaTime, Space.Self);

		if (Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			_showHands = !_showHands;
			// for (int i = 0; i < _hands.Count; i++)
			// {
				// _hands[i].drawHand = _showHands;
			// }
		}

		var curLocalPosition = transform.localPosition;
		var curLocalRotation = transform.localRotation;
		if (curLocalPosition != _cachedLocalPosition || curLocalRotation != _cachedLocalRotation)
		{
			if (_lastLocalPosition != curLocalPosition || _lastLocalRotation != curLocalRotation)
			{
				_lastLocalPosition = curLocalPosition;
				_lastLocalRotation = curLocalRotation;
				_lastChangeTime = Time.time;
			}
			else if (Time.time - _lastChangeTime > _timeBeforeWrite)
			{
				_cachedLocalPosition = transform.localPosition;
				_cachedLocalRotation = transform.localRotation;
				SaveOffset(_cachedLocalPosition, _cachedLocalRotation);
			}
		}
	}

	void SaveOffset(Vector3 position, Quaternion rotation)
	{
		string posXKey = _playerPrefKey + "_pos_x";
		PlayerPrefs.SetFloat(posXKey, position.x);
		string posYKey = _playerPrefKey + "_pos_y";
		PlayerPrefs.SetFloat(posYKey, position.y);
		string posZKey = _playerPrefKey + "_pos_z";
		PlayerPrefs.SetFloat(posZKey, position.z);

		string rotXKey = _playerPrefKey + "_rot_x";
		PlayerPrefs.SetFloat(rotXKey, rotation.x);
		string rotYKey = _playerPrefKey + "_rot_y";
		PlayerPrefs.SetFloat(rotYKey, rotation.y);
		string rotZKey = _playerPrefKey + "_rot_z";
		PlayerPrefs.SetFloat(rotZKey, rotation.z);
		string rotWKey = _playerPrefKey + "_rot_w";
		PlayerPrefs.SetFloat(rotWKey, rotation.w);

		PlayerPrefs.Save();
		Debug.Log("saved calibration offset");
	}

	bool GetSavedOffset(ref Vector3 position, ref Quaternion rotation)
	{
		float posX;
		string posXKey = _playerPrefKey + "_pos_x";
		if (!PlayerPrefs.HasKey(posXKey))
			return false;
		posX = PlayerPrefs.GetFloat(posXKey);

		float posY;
		string posYKey = _playerPrefKey + "_pos_y";
		if (!PlayerPrefs.HasKey(posYKey))
			return false;
		posY = PlayerPrefs.GetFloat(posYKey);

		float posZ;
		string posZKey = _playerPrefKey + "_pos_z";
		if (!PlayerPrefs.HasKey(posZKey))
			return false;
		posZ = PlayerPrefs.GetFloat(posZKey);

		float rotX;
		string rotXKey = _playerPrefKey + "_rot_x";
		if (!PlayerPrefs.HasKey(rotXKey))
			return false;
		rotX = PlayerPrefs.GetFloat(rotXKey);

		float rotY;
		string rotYKey = _playerPrefKey + "_rot_y";
		if (!PlayerPrefs.HasKey(rotYKey))
			return false;
		rotY = PlayerPrefs.GetFloat(rotYKey);

		float rotZ;
		string rotZKey = _playerPrefKey + "_rot_z";
		if (!PlayerPrefs.HasKey(rotZKey))
			return false;
		rotZ = PlayerPrefs.GetFloat(rotZKey);

		float rotW;
		string rotWKey = _playerPrefKey + "_rot_w";
		if (!PlayerPrefs.HasKey(rotWKey))
			return false;
		rotW = PlayerPrefs.GetFloat(rotWKey);

		position = new Vector3(posX, posY, posZ);
		rotation = new Quaternion(rotX, rotY, rotZ, rotW);
		return true;
	}
}
