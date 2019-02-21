using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class ZedPointCloudRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Shader _pointCloudShader;

    [SerializeField, Range(0.1f, 2)]
    float _resolutionScale = 1;

    [SerializeField, Range(0, 0.1f)]
    float _particleSize = 0.05f;

    [SerializeField]
    PhysicsGrid _grid;

    #endregion

    #region Private fields

    Camera _camera;
    // Texture2D _xyzTexture;
    Texture2D _depthTexture;
    Texture2D _colorTexture;
    Material _material;
    bool _ready = false;
    int _numberOfPoints;
    sl.ZEDCamera _zed;

    int _inverseViewMatrixId;
    int _viewMatrixId;
    int _cameraPositionId;
	int _particleSizeId;

    Matrix4x4 _cameraMatrix;

	ZEDRenderingPlane _renderingPlane;
	bool _gotTextures = false;

    CommandBuffer _commandBuffer;

    #endregion

    #region Unity events

    void Awake()
    {
		_camera = GetComponent<Camera>();
		_renderingPlane = GetComponent<ZEDRenderingPlane>();
        _inverseViewMatrixId = Shader.PropertyToID("_InverseViewMatrix");
        _viewMatrixId = Shader.PropertyToID("_ViewMatrix");
        _cameraPositionId = Shader.PropertyToID("_CameraPosition");
		_particleSizeId = Shader.PropertyToID("_ParticleSize");

        if (_grid == null)
        {
            _grid = FindObjectOfType<PhysicsGrid>();
        }
		_material = new Material(_pointCloudShader);
    }

    void OnEnable()
    {
        ZEDManager.OnZEDReady += OnZedReady;
        ZEDManager.OnZEDDisconnected += OnZedDisconnected;

        if (_commandBuffer != null)
        {
            _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _commandBuffer);
        }
    }

    void OnDisable()
    {
        ZEDManager.OnZEDReady -= OnZedReady;
        ZEDManager.OnZEDDisconnected -= OnZedDisconnected;

        if (_commandBuffer != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _commandBuffer);
        }
    }

    void Update()
    {
        if (_material == null) return;

		if (!_gotTextures)
		{
			if (_renderingPlane.TextureEye != null)
			{
				_colorTexture = _renderingPlane.TextureEye;
				_depthTexture = _renderingPlane.Depth;
				_material.SetTexture("_ColorTex", _colorTexture);
				_material.SetTexture("_DepthTex", _depthTexture);
				_gotTextures = true;
			}
		}

        _material.SetMatrix(_inverseViewMatrixId, _camera.transform.localToWorldMatrix);
        _material.SetMatrix(_viewMatrixId, _camera.transform.worldToLocalMatrix);
        _material.SetVector(_cameraPositionId, _camera.transform.position);
		_material.SetFloat(_particleSizeId, _particleSize);

		_material.SetVector(_grid.idPhysicsGridSize, _grid.size);
		_material.SetVector(_grid.idPhysicsGridSizeInv, _grid.invSize);
		_material.SetTexture(_grid.idPhysicsGridPositionTex, _grid.positionTexture);
		_material.SetTexture(_grid.idPhysicsGridVelocityTex, _grid.velocityTexture);
    }

    // void OnRenderObject()
    // {
    // 	if (!_ready) return;
    // 	var curCamera = Camera.current;
    // 	var scene = !curCamera.stereoEnabled;
    // 	if (curCamera.stereoTargetEye != _camera.stereoTargetEye) return;

    // 	_material.SetMatrix(_inverseViewMatrixId, _camera.transform.localToWorldMatrix);
    // 	_material.SetMatrix(_viewMatrixId, _camera.transform.worldToLocalMatrix);
    // 	_material.SetPass(0);
    // 	Graphics.DrawProcedural(MeshTopology.Points, 1, _numberOfPoints);
    // }

    #endregion

    #region Zed events

    void OnZedReady()
    {
        _zed = sl.ZEDCamera.GetInstance();

        bool right = _camera.stereoTargetEye == StereoTargetEyeMask.Right;

		var plane = GetComponent<ZEDRenderingPlane>();

        // _xyzTexture = _zed.CreateTextureMeasureType(right ? sl.MEASURE.XYZ_RIGHT : sl.MEASURE.XYZ);
        // _colorTexture = _zed.CreateTextureImageType(right ? sl.VIEW.RIGHT : sl.VIEW.LEFT);
        // _depthTexture = _zed.CreateTextureMeasureType(right ? sl.MEASURE.DEPTH_RIGHT : sl.MEASURE.DEPTH);

        var pointWidth = Mathf.FloorToInt(_zed.ImageWidth * _resolutionScale);
        var pointHeight = Mathf.FloorToInt(_zed.ImageHeight * _resolutionScale);
        _numberOfPoints = pointWidth * pointHeight;

        // _material.SetTexture("_XYZTex", _xyzTexture);
        _material.SetVector("_TexelSize", new Vector4(1f / pointWidth, 1f / pointHeight, pointWidth, pointHeight));

        var handMask = GetComponent<HandMaskRenderer>();
        if (handMask != null)
        {
            _material.SetTexture("_HandMaskTex", handMask.texture);
        }

		_material.SetVector(_grid.idPhysicsGridSize, _grid.size);
		_material.SetVector(_grid.idPhysicsGridSizeInv, _grid.invSize);
		_material.SetTexture(_grid.idPhysicsGridPositionTex, _grid.positionTexture);
		_material.SetTexture(_grid.idPhysicsGridVelocityTex, _grid.velocityTexture);

        //Move the plane with the optical centers.
        float plane_distance = 0.15f;
        Vector4 opticalCenters = _zed.ComputeOpticalCenterOffsets(plane_distance);

        var position = right ? new Vector3(opticalCenters.z, -1.0f * opticalCenters.w, plane_distance)
                             : new Vector3(opticalCenters.x, -1.0f * opticalCenters.y, plane_distance);
        Matrix4x4 projMatrix = _zed.Projection;
        var near = 0.1f;
        var far = 500f;
        projMatrix[2, 2] = -(far + near) / (far - near);
        projMatrix[2, 3] = -(2.0f * far * near) / (far - near);

        var aspect = 16f / 9f;
        var fov = GetFOVYFromProjectionMatrix(projMatrix);
        float height = Mathf.Tan(0.5f * fov) * Vector3.Distance(position, Vector3.zero) * 2;
        var scale = new Vector3((height * aspect), height, 1);

        _cameraMatrix = Matrix4x4.TRS(position, Quaternion.identity, scale);
        // Debug.Log("position " + position.ToString("0.0000"));
        // Debug.Log("scale " + scale.ToString("0.0000"));
        _material.SetMatrix("_Position", _cameraMatrix);

        var topLeft = _camera.ViewportToScreenPoint(_camera.projectionMatrix * _cameraMatrix * new Vector3(-0.5f, 0.5f, 0));
        // Debug.Log(topLeft);
        var bottomRight = _camera.ViewportToScreenPoint(_camera.projectionMatrix * _cameraMatrix * new Vector3(0.5f, -0.5f, 0));
        // Debug.Log(bottomRight);

        _ready = true;

        _commandBuffer = new CommandBuffer();

        _commandBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Points, 1, _numberOfPoints);

        _camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _commandBuffer);
    }

    void OnZedDisconnected()
    {
        _ready = false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Gets the vertical field of view from the given projection matrix, to bypass a round number.
    /// </summary>
    /// <param name="projection">Projection matrix from a camera.</param>
    /// <returns></returns>
    float GetFOVYFromProjectionMatrix(Matrix4x4 projection)
    {
        return Mathf.Atan(1 / projection[1, 1]) * 2.0f;
    }


    #endregion
}
