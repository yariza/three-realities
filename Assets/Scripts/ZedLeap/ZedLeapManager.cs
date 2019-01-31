using UnityEngine;
using UnityEngine.XR;

public class ZedLeapManager : MonoBehaviour
{
	#region Singleton

	static ZedLeapManager _instance;

	public static ZedLeapManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<ZedLeapManager>();
			}
			return _instance;
		}
	}

	#endregion

	#region Private fields

	int _WorldSpaceCenterCameraPosId;

	bool _isRoomScale;
	public bool isRoomScale
	{
		get { return _isRoomScale; }
	}

	Vector3 _centerCameraPosition;
	public Vector3 centerCameraPosition
	{
		get { return _centerCameraPosition; }
	}

	ZEDManager _manager;

	#endregion

	#region Unity events

	void Awake()
	{
		_instance = this;
		_WorldSpaceCenterCameraPosId = Shader.PropertyToID("_WorldSpaceCenterCameraPos");
	}

	void Start()
	{
		_manager = ZEDManager.Instance;
		_isRoomScale = XRDevice.model.ToLower().Contains("vive");
		// _manager.useTracking = !_isRoomScale;
	}

	void Update()
	{
		var manager = ZEDManager.Instance;
		var left = manager.GetLeftCameraTransform().position;
		var right = manager.GetRightCameraTransform().position;
		_centerCameraPosition = (left + right) * 0.5f;
		Shader.SetGlobalVector(_WorldSpaceCenterCameraPosId, _centerCameraPosition);
	}

	#endregion
}
