using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// [RequireComponent(typeof(Camera))]
public class ZedPointCloudRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Shader _pointCloudShader;

    [SerializeField, Range(0.1f, 2)]
    float _resolutionScale = 1;

    [SerializeField, Range(0, 0.1f)]
    float _particleSize = 0.05f;

    [SerializeField, Range(0, 1f)]
    float _particleSizeBump = 0.1f;

    [SerializeField]
    PhysicsGrid _grid;

    [SerializeField]
    HandMaskRenderer _handMask;

    [SerializeField]
    Vector3 _offset = Vector3.zero;

    [SerializeField]
    CameraEvent _cameraEvent = CameraEvent.AfterForwardOpaque;

    #endregion

    #region Private fields

    Texture2D _depthTexture;
    Texture2D _depthRightTexture;
    // Texture2D _normalTexture;
    // Texture2D _normalRightTexture;
    Texture2D _colorTexture;
    Texture2D _colorRightTexture;

    Camera _camera;
    Material _material;
    int _numberOfPoints;

    int _particleSizeId;
    int _particleSizeBumpId;

    Matrix4x4 _cameraMatrix;

    CommandBuffer _commandBuffer;
    CameraEvent _memoizedCameraEvent;

    Vector3 _cameraPosition;
    Vector3 _cameraScale;
    CustomZedManager _manager;

    const int NUM_EYES = 2;
    Matrix4x4[] _planeMatrices = new Matrix4x4[NUM_EYES];
    Matrix4x4[] _transformMatrices = new Matrix4x4[NUM_EYES];

    private float aspect = 16.0f / 9.0f;

    #endregion

    #region Unity events

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        _particleSizeId = Shader.PropertyToID("_ParticleSize");
        _particleSizeBumpId = Shader.PropertyToID("_ParticleSizeBump");

        if (_grid == null)
        {
            _grid = FindObjectOfType<PhysicsGrid>();
        }
        _material = new Material(_pointCloudShader);

        _manager = CustomZedManager.Instance;
        _commandBuffer = new CommandBuffer();
    }

    void OnEnable()
    {
        if (_manager.IsZEDReady)
        {
            OnZedReady();
        }
        CustomZedManager.OnZEDReady += OnZedReady;

        if (_commandBuffer != null)
        {
            _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
            _memoizedCameraEvent = _cameraEvent;
        }
    }

    void OnDisable()
    {
        CustomZedManager.OnZEDReady -= OnZedReady;

        if (_commandBuffer != null && _camera != null)
        {
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
        }
    }

    void Update()
    {
        if (!_manager.IsZEDReady) return;

        if (_manager.HMDSyncRotation.x == 0 &&
            _manager.HMDSyncRotation.y == 0 &&
            _manager.HMDSyncRotation.z == 0 &&
            _manager.HMDSyncRotation.w == 0)
            return;

        var mat = Matrix4x4.TRS(_camera.transform.position, _manager.HMDSyncRotation, Vector3.one);

        _material.SetMatrix("_Transform", mat);
        Vector3 pos0 = _pos0 + new Vector3(-1f * _offset.x, _offset.y, _offset.z);
        Vector3 pos1 = _pos1 + new Vector3( 1f * _offset.x, _offset.y, _offset.z);

        _planeMatrices[0] = Matrix4x4.TRS(pos0, Quaternion.identity, _scale0);
        _planeMatrices[1] = Matrix4x4.TRS(pos1, Quaternion.identity, _scale1);
        _material.SetMatrixArray("_PlaneMatrices", _planeMatrices);

        _transformMatrices[0] = mat * _planeMatrices[0];
        _transformMatrices[1] = mat * _planeMatrices[1];

        _material.SetMatrixArray("_TransformMatrices", _transformMatrices);

        _material.SetFloat(_particleSizeId, _particleSize);
        _material.SetFloat(_particleSizeBumpId, _particleSizeBump);

        if (_grid != null)
        {
            _material.SetVector(_grid.idPhysicsGridSize, _grid.size);
            _material.SetVector(_grid.idPhysicsGridSizeInv, _grid.invSize);
            _material.SetTexture(_grid.idPhysicsGridPositionTex, _grid.positionTexture);
            _material.SetTexture(_grid.idPhysicsGridVelocityTex, _grid.velocityTexture);
        }

        _material.SetTexture("_DepthTextureLeft", _depthTexture);
        _material.SetTexture("_DepthTextureRight", _depthRightTexture);
        // _material.SetTexture("_NormalTextureLeft", _normalTexture);
        // _material.SetTexture("_NormalTextureRight", _normalRightTexture);
        _material.SetTexture("_ColorTextureLeft", _colorTexture);
        _material.SetTexture("_ColorTextureRight", _colorRightTexture);

        // if (_memoizedCameraEvent != _cameraEvent)
        // {
        //     _camera.RemoveCommandBuffer(_memoizedCameraEvent, _commandBuffer);
        //     _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
        //     _memoizedCameraEvent = _cameraEvent;
        // }
    }

    // private void OnRenderObject()
    // {
        // _material.SetPass(0);
        // Graphics.DrawProcedural(MeshTopology.Points, 1, _numberOfPoints);
    // }

    #endregion

    Vector3 _pos0, _pos1;
    Vector3 _scale0, _scale1;

    #region Zed events

    void OnZedReady()
    {
        var zedCamera = _manager.zedCamera;

        var pointWidth = Mathf.FloorToInt(zedCamera.ImageWidth * _resolutionScale);
        var pointHeight = Mathf.FloorToInt(zedCamera.ImageHeight * _resolutionScale);
        _numberOfPoints = pointWidth * pointHeight;

        _material.SetVector("_TexelSize", new Vector4(1f / pointWidth, 1f / pointHeight, pointWidth, pointHeight));

        if (_handMask == null)
        {
            _handMask = GetComponent<HandMaskRenderer>();
        }
        if (_handMask != null)
        {
            _material.SetTexture("_HandMaskTex", _handMask.texture);
        }

        if (_grid != null)
        {
            _material.SetVector(_grid.idPhysicsGridSize, _grid.size);
            _material.SetVector(_grid.idPhysicsGridSizeInv, _grid.invSize);
            _material.SetTexture(_grid.idPhysicsGridPositionTex, _grid.positionTexture);
            _material.SetTexture(_grid.idPhysicsGridVelocityTex, _grid.velocityTexture);
        }

        _depthTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.DEPTH);
        _depthRightTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.DEPTH_RIGHT);
        // _normalTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.NORMALS);
        // _normalRightTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.NORMALS_RIGHT);
        _colorTexture = zedCamera.CreateTextureImageType(sl.VIEW.LEFT);
        _colorRightTexture = zedCamera.CreateTextureImageType(sl.VIEW.RIGHT);

		float plane_distance =0.15f;
		Vector4 opticalCenters = zedCamera.ComputeOpticalCenterOffsets(plane_distance);

        var pos0 = new Vector3(opticalCenters.x, -1.0f * opticalCenters.y,plane_distance);
        var pos1 = new Vector3(opticalCenters.z, -1.0f * opticalCenters.w,plane_distance);

        var projMatrix = zedCamera.Projection;
        var fovY = GetFOVYFromProjectionMatrix(projMatrix);

        var scale0 = scale(pos0, fovY);
        var scale1 = scale(pos1, fovY);

        _pos0 = pos0;
        _pos1 = pos1;
        _scale0 = scale0;
        _scale1 = scale1;

        pos0 += new Vector3(-1f * _offset.x, _offset.y, _offset.z);
        pos1 += new Vector3( 1f * _offset.x, _offset.y, _offset.z);

        _planeMatrices[0] = Matrix4x4.TRS(pos0, Quaternion.identity, scale0);
        _planeMatrices[1] = Matrix4x4.TRS(pos1, Quaternion.identity, scale1);

        _commandBuffer.Clear();
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Points, 1, _numberOfPoints);
        _memoizedCameraEvent = _cameraEvent;
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

    private Vector3 scale(Vector3 position, float fov)
    {
        float height = Mathf.Tan(0.5f * fov) * Vector3.Distance(position, Vector3.zero) * 2;
        return new Vector3((height * aspect), height, 1);
    }

    #endregion
}
