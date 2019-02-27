using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity;
using Leap.Unity.Interaction;

public class PhysicsGrid : MonoBehaviour
{
    [SerializeField]
    ComputeShader _computeShader;
	public ComputeShader computeShader
	{
		get { return _computeShader; }
	}

    [SerializeField]
    Vector3Int _resolution = new Vector3Int(64, 64, 64);
    public Vector3Int resolution
    {
        get { return _resolution; }
    }
    [SerializeField]
    Vector3 _size = new Vector3(2, 2, 2);
    public Vector3 size
    {
        get { return _size; }
    }

    public Vector3 invSize
    {
        get
        {
			return new Vector3(1f / _size.x, 1f / _size.y, 1f / _size.z);
        }
    }

	[SerializeField, Range(0, 0.3f)]
	float _grabDist = 0.1f;

	[SerializeField]
	float _targetSpringStrength = 0.1f;

	[SerializeField]
	float _clothSpringStrength = 3f;

	[SerializeField, Range(0, 10)]
	float _damping = 0.1f;

	[SerializeField]
    RenderTexture _positionTexture;
    public RenderTexture positionTexture
    {
        get { return _positionTexture; }
    }
	[SerializeField]
    RenderTexture _velocityTexture;
    public RenderTexture velocityTexture
    {
        get { return _velocityTexture; }
    }

    int _idPhysicsGridPositionTex;
    public int idPhysicsGridPositionTex
    {
        get { return _idPhysicsGridPositionTex; }
    }
    int _idPhysicsGridVelocityTex;
    public int idPhysicsGridVelocityTex
    {
        get { return _idPhysicsGridVelocityTex; }
    }
    int _idPhysicsGridResolution;
    public int idPhysicsGridResolution
    {
        get { return _idPhysicsGridResolution; }
    }
    int _idPhysicsGridSize;
    public int idPhysicsGridSize
    {
        get { return _idPhysicsGridSize; }
    }
    int _idPhysicsGridSizeInv;
    public int idPhysicsGridSizeInv
    {
        get { return _idPhysicsGridSizeInv; }
    }

	// int _idGrabStartPositions;
	int _idGrabCurPositions;
	int _idGrabDeltaPositions;
	int _idCurGrabIndex;
	int _idGrabDist;

	int _idTargetSpringStrength;
	int _idClothSpringStrength;
	int _idDamping;

	int _idTime;

    int _kernelInit;
	int _kernelStartGrab;
	int _kernelEndGrab;
	int _kernelUpdateVelocity;
	int _kernelUpdatePosition;

	int _groupsX;
	int _groupsY;
	int _groupsZ;

	List<InteractionController> _pinchBeginControllers = new List<InteractionController>();
	List<InteractionController> _pinchEndControllers = new List<InteractionController>();

	const int MAX_HANDS = 2;
	// Vector4[] _grabStartPositions = new Vector4[MAX_HANDS];
	Vector4[] _grabCurPositions = new Vector4[MAX_HANDS];
	Vector4[] _grabPrevPositions = new Vector4[MAX_HANDS];
	Vector4[] _grabDeltaPositions = new Vector4[MAX_HANDS];

    void Awake()
    {
        var descriptor = new RenderTextureDescriptor(
            _resolution.x, _resolution.y, RenderTextureFormat.ARGBFloat, 0
        );
        descriptor.enableRandomWrite = true;
        descriptor.dimension = TextureDimension.Tex3D;
        descriptor.volumeDepth = _resolution.z;
        descriptor.useMipMap = false;
        descriptor.sRGB = false;

        _positionTexture = new RenderTexture(descriptor);
        _positionTexture.wrapMode = TextureWrapMode.Repeat;
        _positionTexture.filterMode = FilterMode.Trilinear;
        _velocityTexture = new RenderTexture(descriptor);
        _velocityTexture.wrapMode = TextureWrapMode.Repeat;
        _velocityTexture.filterMode = FilterMode.Trilinear;

		_positionTexture.Create();
		_velocityTexture.Create();

        _idPhysicsGridPositionTex = Shader.PropertyToID("_PhysicsGridPositionTex");
        _idPhysicsGridVelocityTex = Shader.PropertyToID("_PhysicsGridVelocityTex");
        _idPhysicsGridResolution = Shader.PropertyToID("_PhysicsGridResolution");
        _idPhysicsGridSize = Shader.PropertyToID("_PhysicsGridSize");
		_idPhysicsGridSizeInv = Shader.PropertyToID("_PhysicsGridSizeInv");
		// _idGrabStartPositions = Shader.PropertyToID("_GrabStartPositions");
		_idGrabCurPositions = Shader.PropertyToID("_GrabCurPositions");
		_idGrabDeltaPositions = Shader.PropertyToID("_GrabDeltaPositions");
		_idCurGrabIndex = Shader.PropertyToID("_CurGrabIndex");
		_idGrabDist = Shader.PropertyToID("_GrabDist");
		_idTargetSpringStrength = Shader.PropertyToID("_TargetSpringStrength");
		_idClothSpringStrength = Shader.PropertyToID("_ClothSpringStrength");
		_idDamping = Shader.PropertyToID("_Damping");
		_idTime = Shader.PropertyToID("_Time");

        _kernelInit = _computeShader.FindKernel("Init");
		_kernelStartGrab = _computeShader.FindKernel("StartGrab");
		_kernelEndGrab = _computeShader.FindKernel("EndGrab");
		_kernelUpdateVelocity = _computeShader.FindKernel("UpdateVelocity");
		_kernelUpdatePosition = _computeShader.FindKernel("UpdatePosition");
    }

	void OnEnable()
	{
		var manager = PinchInteractionManager.instance;
		manager.OnControllerPinchBegin -= OnControllerPinchBegin;
		manager.OnControllerPinchBegin += OnControllerPinchBegin;
		manager.OnControllerPinchEnd -= OnControllerPinchEnd;
		manager.OnControllerPinchEnd += OnControllerPinchEnd;
		manager.OnControllerPinchStay -= OnControllerPinchStay;
		manager.OnControllerPinchStay += OnControllerPinchStay;
	}

	void OnDisable()
	{
		var manager = PinchInteractionManager.instance;
		if (manager == null) return;
		manager.OnControllerPinchBegin -= OnControllerPinchBegin;
		manager.OnControllerPinchEnd -= OnControllerPinchEnd;
		manager.OnControllerPinchStay -= OnControllerPinchStay;
	}

    void Start()
    {
		Shader.SetGlobalVector(_idPhysicsGridSize, _size);
		Shader.SetGlobalVector(_idPhysicsGridSizeInv, invSize);
		Shader.SetGlobalTexture(_idPhysicsGridPositionTex, _positionTexture);
		Shader.SetGlobalTexture(_idPhysicsGridVelocityTex, _velocityTexture);

        // // move this stuff to command buffer
        _computeShader.SetInts(_idPhysicsGridResolution, _resolution.x, _resolution.y, _resolution.z);
        _computeShader.SetVector(_idPhysicsGridSize, _size);
		_computeShader.SetVector(_idPhysicsGridSizeInv, invSize);
        _computeShader.SetTexture(_kernelInit, _idPhysicsGridPositionTex, _positionTexture);
        _computeShader.SetTexture(_kernelInit, _idPhysicsGridVelocityTex, _velocityTexture);

        const int groupSizeDim = 8;
        _groupsX = (_resolution.x + groupSizeDim - 1) / groupSizeDim;
        _groupsY = (_resolution.y + groupSizeDim - 1) / groupSizeDim;
        _groupsZ = (_resolution.z + groupSizeDim - 1) / groupSizeDim;
        _computeShader.Dispatch(_kernelInit, _groupsX, _groupsY, _groupsZ);

        _computeShader.SetTexture(_kernelStartGrab, _idPhysicsGridPositionTex, _positionTexture);
        _computeShader.SetTexture(_kernelStartGrab, _idPhysicsGridVelocityTex, _velocityTexture);

        _computeShader.SetTexture(_kernelEndGrab, _idPhysicsGridPositionTex, _positionTexture);
        _computeShader.SetTexture(_kernelEndGrab, _idPhysicsGridVelocityTex, _velocityTexture);

        _computeShader.SetTexture(_kernelUpdatePosition, _idPhysicsGridPositionTex, _positionTexture);
        _computeShader.SetTexture(_kernelUpdatePosition, _idPhysicsGridVelocityTex, _velocityTexture);

        _computeShader.SetTexture(_kernelUpdateVelocity, _idPhysicsGridPositionTex, _positionTexture);
        _computeShader.SetTexture(_kernelUpdateVelocity, _idPhysicsGridVelocityTex, _velocityTexture);

    }

    void Update()
    {
		// _computeShader.SetVectorArray(_idGrabStartPositions, _grabStartPositions);
		foreach (var controller in InteractionManager.instance.interactionControllers)
		{
			if (!PinchInteractionManager.instance.IsPinching(controller))
			{
				var index = controller.isRight ? 1 : 0;
				_grabCurPositions[index] = controller.GetPinchPosition();
			}
		}
		for (int i = 0; i < MAX_HANDS; i++)
		{
			_grabDeltaPositions[i] = _grabCurPositions[i] - _grabPrevPositions[i];
		}
		_computeShader.SetVectorArray(_idGrabDeltaPositions, _grabDeltaPositions);
		_computeShader.SetVectorArray(_idGrabCurPositions, _grabCurPositions);

		_computeShader.SetFloat(_idGrabDist, _grabDist);

		_computeShader.SetFloat(_idTargetSpringStrength, _targetSpringStrength);
		_computeShader.SetFloat(_idClothSpringStrength, _clothSpringStrength);

		var dt = Mathf.Clamp(Time.deltaTime, 1f / 200, 1f / 10);
		var smoothDt = Mathf.Clamp(Time.smoothDeltaTime, 1f / 200, 1f / 10);
		_computeShader.SetFloat(_idDamping, Mathf.Exp(-_damping * smoothDt));
		_computeShader.SetVector(_idTime, new Vector4(dt, 1f / dt, smoothDt, 1f / smoothDt));

		for (int i = _pinchBeginControllers.Count - 1; i >= 0; i--)
		{
			var controller = _pinchBeginControllers[i];
			_computeShader.SetInt(_idCurGrabIndex, controller.isRight ? 1 : 0);
			_computeShader.Dispatch(_kernelStartGrab, _groupsX, _groupsY, _groupsZ);

			_pinchBeginControllers.RemoveAt(i);
		}

		for (int i = _pinchEndControllers.Count - 1; i >= 0; i--)
		{
			var controller = _pinchEndControllers[i];
			_computeShader.SetInt(_idCurGrabIndex, controller.isRight ? 1 : 0);
			_computeShader.Dispatch(_kernelEndGrab, _groupsX, _groupsY, _groupsZ);

			_pinchEndControllers.RemoveAt(i);
		}

		_computeShader.Dispatch(_kernelUpdatePosition, _groupsX, _groupsY, _groupsZ);

		_computeShader.Dispatch(_kernelUpdateVelocity, _groupsX, _groupsY, _groupsZ);

		for (int i = 0; i < MAX_HANDS; i++)
		{
			_grabPrevPositions[i] = _grabCurPositions[i];
		}
    }

	#region PinchInteractionManager events

	void OnControllerPinchBegin(InteractionController controller)
	{
		_pinchBeginControllers.Add(controller);
		// Debug.Log("pinch begin " + (controller.isLeft ? "left" : "right"));
		var index = controller.isRight ? 1 : 0;
		var pinchPos = controller.GetPinchPosition();
		_grabPrevPositions[index] = _grabCurPositions[index] = pinchPos;
	}

	void OnControllerPinchStay(InteractionController controller)
	{
		var index = controller.isRight ? 1 : 0;
		var pinchPos = controller.GetPinchPosition();
		_grabCurPositions[index] = pinchPos;
	}

	void OnControllerPinchEnd(InteractionController controller)
	{
		_pinchEndControllers.Add(controller);
		// Debug.Log("pinch end " + (controller.isLeft ? "left" : "right"));
	}

	#endregion
}
