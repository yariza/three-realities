using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SoftRepelEffector : MonoBehaviour
{
	[SerializeField]
	float _forceScale = 1.0f;
	[SerializeField]
	float _minimumForce = 5f;
	[SerializeField]
	float _maximumForce = 10f;

	Collider _collider;
	Dictionary<Collider, Rigidbody> _triggeredColliders = new Dictionary<Collider, Rigidbody>();

	void Awake()
	{
		_collider = GetComponent<Collider>();
		_collider.isTrigger = true;
	}

	void OnTriggerEnter(Collider c)
	{
		var rb = c.GetComponent<Rigidbody>();
		if (rb != null && !_triggeredColliders.ContainsKey(c))
		{
			_triggeredColliders.Add(c, rb);
		}
	}

	void OnTriggerExit(Collider c)
	{
		if (_triggeredColliders.ContainsKey(c))
		{
			_triggeredColliders.Remove(c);
		}
	}

	List<Collider> _toDestroy = new List<Collider>();

	void FixedUpdate()
	{
		_toDestroy.Clear();
		foreach (var pair in _triggeredColliders)
		{
			var c = pair.Key;
			var rb = pair.Value;

			Vector3 forceDir;
			float forceDistance;

			if (rb == null)
			{
				// object was destroyed, add it to the to destroy list
				_toDestroy.Add(c);
				continue;
			}

			if (Physics.ComputePenetration(c, rb.position, rb.rotation,
										   _collider, transform.position, transform.rotation,
										   out forceDir, out forceDistance))
			{
				rb.AddForce(forceDir * Mathf.Min(forceDistance * _forceScale + _minimumForce, _maximumForce), ForceMode.Acceleration);
			}
		}
		for (int i = 0; i < _toDestroy.Count; i++)
		{
			_triggeredColliders.Remove(_toDestroy[i]);
		}
	}
}
