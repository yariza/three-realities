using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsGrid : MonoBehaviour
{
	[SerializeField]
	Vector3Int _resolution = new Vector3Int(64, 64, 64);
	[SerializeField]
	Vector3 _size = new Vector3(2, 2, 2);

	ComputeBuffer _positionBuffer;
	ComputeBuffer _velocityBuffer;

	void Awake()
	{
		var numCells = _resolution.x * _resolution.y * _resolution.z;
		_positionBuffer = new ComputeBuffer(numCells, sizeof(float) * 3);
		_velocityBuffer = new ComputeBuffer(numCells, sizeof(float) * 3);
	}

	void Start()
	{

	}

	void Update()
	{

	}
}
