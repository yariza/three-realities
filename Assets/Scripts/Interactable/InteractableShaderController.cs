using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

[RequireComponent(typeof(InteractionBehaviour))]
public class InteractableShaderController : MonoBehaviour
{
	#region Serialized fields

	[SerializeField]
	bool _overrideContactColor;
	[SerializeField]
	Color _contactColor = Color.blue;

	[SerializeField]
	bool _overrideGrabColor;
	[SerializeField]
	Color _grabColor = Color.yellow;

	[SerializeField]
	Collider _collider;

	[SerializeField]
	AnimationCurve _distanceClamp = new AnimationCurve(
		new Keyframe(0.0f, 0.02f),
		new Keyframe(0.2f, 0.15f)
	);

	#endregion

	#region Private fields

	Renderer[] _renderers;
	InteractionBehaviour _interaction;

	int _GrabId;
	int _ContactColorId;
	int _GrabColorId;
	int _ContactsLengthId;
	int _ContactsId;

	const int MAX_CONTACTS = 10;
	Vector4[] _contacts = new Vector4[MAX_CONTACTS];

	#endregion

	#region Unity events

	void Awake()
	{
		_renderers = GetComponentsInChildren<Renderer>();
		_interaction = GetComponent<InteractionBehaviour>();

		_GrabId = Shader.PropertyToID("_Grab");
		_ContactColorId = Shader.PropertyToID("_ContactColor");
		_GrabColorId = Shader.PropertyToID("_GrabColor");
		_ContactsLengthId = Shader.PropertyToID("_ContactsLength");
		_ContactsId = Shader.PropertyToID("_Contacts");
	}

	#if UNITY_EDITOR

	void OnValidate()
	{
		if (_collider == null)
		{
			_collider = GetComponent<Collider>();
		}
	}

	void Reset()
	{
		OnValidate();
	}

	#endif

	void Update()
	{
		int numContacts = 0;
		foreach (var controller in _interaction.hoveringControllers)
		{
			var fingers = controller.intHand.leapHand.Fingers;
			for (int i = 0; i < fingers.Count; i++)
			{
				var tip = GetFingerCollision(fingers[i]);
				_contacts[numContacts] = tip;
				numContacts++;
			}
		}

		var grab = _interaction.isGrasped ? 1f : 0f;

		for (int i = 0; i < _renderers.Length; i++)
		{
			var material = _renderers[i].material;
			material.SetFloat(_GrabId, grab);
			if (_overrideContactColor)
			{
				material.SetColor(_ContactColorId, _contactColor);
			}
			if (_overrideGrabColor)
			{
				material.SetColor(_GrabColorId, _grabColor);
			}
			material.SetInt(_ContactsLengthId, numContacts);
			material.SetVectorArray(_ContactsId, _contacts);
		}
	}

	#endregion

	#region Private methods

	Vector3 GetFingerCollision(Leap.Finger finger)
	{
		for (int i = 0; i < finger.bones.Length; i++)
		{
			var bone = finger.bones[i];
			var origin = bone.PrevJoint.ToVector3();
			var dir = bone.Direction.ToVector3();
			if (dir == Vector3.zero)
			{
				continue;
			}
			var ray = new Ray(origin, dir);
			RaycastHit hit;
			if (_collider.Raycast(ray, out hit, bone.Length))
			{
				return hit.point;
			}
		}

		{
			var tip = finger.TipPosition.ToVector3();
			var point = _collider.ClosestPoint(tip);
			var dir = point - tip;
			var min = _distanceClamp.keys[0].time;
			var max = _distanceClamp.keys[_distanceClamp.keys.Length - 1].time;
			var len = _distanceClamp.Evaluate(Mathf.Clamp(dir.magnitude, min, max));
			dir = Vector3.ClampMagnitude(dir, len);
			return tip + dir;
		}
	}

	#endregion
}
