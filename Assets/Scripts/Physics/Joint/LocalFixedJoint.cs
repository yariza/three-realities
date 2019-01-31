using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

[RequireComponent(typeof(Rigidbody))]
public class LocalFixedJoint : MonoBehaviour
{
	#region Serialized fields

	[SerializeField]
	Rigidbody _parent;
	[SerializeField]
	Rigidbody _connectedBody;

	[System.Serializable]
	struct Vector3Bool
	{
		public bool x;
		public bool y;
		public bool z;

		public Vector3Bool(bool x, bool y, bool z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	[SerializeField]
	Vector3Bool _fixPosition;
	[SerializeField]
	Vector3Bool _fixRotation;

	[SerializeField]
	bool _useInteractionManagerCallback;

	#endregion

	#region Private fields

	Rigidbody _rigidbody;
	Vector3 _localPosition;
	Vector3 _localEulerAngles;

	#endregion

	#region Unity events

	void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		if (_parent == null) return;
		var parent = _parent.transform;
		_localPosition = parent.InverseTransformPoint(_rigidbody.position);
		_localEulerAngles = parent.InverseTransformRotation(_rigidbody.rotation).eulerAngles;

	}

	void FixedUpdate()
	{
		if (_parent == null) return;
		var parent = _parent.transform;
		var rigidbody = _connectedBody != null ? _connectedBody : _rigidbody;

		var localPosition = parent.InverseTransformPoint(rigidbody.position);
		if (_fixPosition.x) localPosition.x = _localPosition.x;
		if (_fixPosition.y) localPosition.y = _localPosition.y;
		if (_fixPosition.z) localPosition.z = _localPosition.z;
		_rigidbody.position = parent.TransformPoint(localPosition);
		_localPosition = localPosition;

		var localEulerAngles = parent.InverseTransformRotation(rigidbody.rotation).eulerAngles;
		if (_fixRotation.x) localEulerAngles.x = _localEulerAngles.x;
		if (_fixRotation.y) localEulerAngles.y = _localEulerAngles.y;
		if (_fixRotation.z) localEulerAngles.z = _localEulerAngles.z;
		_rigidbody.rotation = parent.TransformRotation(Quaternion.Euler(localEulerAngles));
		_localEulerAngles = localEulerAngles;
	}
		
	#endregion

	#region Private fields

	void ApplyConstraint()
	{
		
	}
		
	#endregion
}
