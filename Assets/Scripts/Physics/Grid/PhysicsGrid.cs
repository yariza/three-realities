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

    int _kernelInit;
	int _kernelStartGrab;
	int _kernelEndGrab;
	int _kernelUpdateVelocity;
	int _kernelUpdatePosition;

	List<InteractionController> _pinchBeginControllers = new List<InteractionController>();
	List<InteractionController> _pinchEndControllers = new List<InteractionController>();

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
	}

	void OnDisable()
	{
		var manager = PinchInteractionManager.instance;
		if (manager == null) return;
		manager.OnControllerPinchBegin -= OnControllerPinchBegin;
		manager.OnControllerPinchEnd -= OnControllerPinchEnd;
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
		_computeShader.SetFloat("_Time", Time.time);

        const int groupSizeDim = 8;
        int groupsX = (_resolution.x + groupSizeDim - 1) / groupSizeDim;
        int groupsY = (_resolution.y + groupSizeDim - 1) / groupSizeDim;
        int groupsZ = (_resolution.z + groupSizeDim - 1) / groupSizeDim;
        _computeShader.Dispatch(_kernelInit, groupsX, groupsY, groupsZ);
    }

    void Update()
    {
		for (int i = _pinchBeginControllers.Count - 1; i >= 0; i--)
		{


			_pinchBeginControllers.RemoveAt(i);
		}

		for (int i = _pinchEndControllers.Count - 1; i >= 0; i--)
		{


			_pinchEndControllers.RemoveAt(i);
		}
    }

	#region PinchInteractionManager events

	void OnControllerPinchBegin(InteractionController controller)
	{
		_pinchBeginControllers.Add(controller);
		Debug.Log("pinch begin " + (controller.isLeft ? "left" : "right"));
	}

	void OnControllerPinchEnd(InteractionController controller)
	{
		_pinchEndControllers.Add(controller);
		Debug.Log("pinch end " + (controller.isLeft ? "left" : "right"));
	}

	#endregion
}
