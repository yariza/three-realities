using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ZedPlaneRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Material _material = null;

    [SerializeField]
    Mesh _quadMesh = null;

    [SerializeField]
    Vector3 _offset = new Vector3(0.0315f, 0, 0.115f);

    [SerializeField]
    CameraEvent _cameraEvent = CameraEvent.AfterForwardAlpha;

    #endregion

    #region Private fields

    CustomZedManager _manager;
    Camera _camera;
    CommandBuffer _commandBuffer;
    MaterialPropertyBlock _propertyBlock;

    const int NUM_EYES = 2;
    Matrix4x4[] _planeMatrices = new Matrix4x4[NUM_EYES];
    Matrix4x4[] _transformMatrices = new Matrix4x4[NUM_EYES];

    Texture2D _depthTexture;
    Texture2D _depthRightTexture;
    Texture2D _normalTexture;
    Texture2D _normalRightTexture;
    Texture2D _colorTexture;
    Texture2D _colorRightTexture;

    /// <summary>
    /// Aspect ratio of the textures. All the textures displayed should be in 16:9.
    /// </summary>
    private float aspect = 16.0f / 9.0f;

    #endregion

    #region Unity events

    private void Awake()
    {
        _manager = CustomZedManager.Instance;
        _camera = Camera.main;

        _commandBuffer = new CommandBuffer();
        _propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (_manager.IsZEDReady)
        {
            OnZedReady();
        }
        CustomZedManager.OnZEDReady += OnZedReady;
        _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
    }

    private void OnDisable()
    {
        CustomZedManager.OnZEDReady -= OnZedReady;
        if (_camera != null)
        {
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
        }
    }

    private void Update()
    {
        if (!_manager.IsZEDReady) return;

        if (_manager.HMDSyncRotation.x == 0 &&
            _manager.HMDSyncRotation.y == 0 &&
            _manager.HMDSyncRotation.z == 0 &&
            _manager.HMDSyncRotation.w == 0)
            return;

        var mat = Matrix4x4.TRS(_camera.transform.position, _manager.HMDSyncRotation, Vector3.one);

        // var rotMatrix = Matrix4x4.Rotate(_manager.HMDSyncRotation * Quaternion.Inverse(_camera.transform.rotation));

        _transformMatrices[0] = mat * _planeMatrices[0];
        _transformMatrices[1] = mat * _planeMatrices[1];

        _propertyBlock.SetMatrixArray("_TransformMatrices", _transformMatrices);
        _propertyBlock.SetTexture("_DepthTextureLeft", _depthTexture);
        _propertyBlock.SetTexture("_DepthTextureRight", _depthRightTexture);
        _propertyBlock.SetTexture("_NormalTextureLeft", _normalTexture);
        _propertyBlock.SetTexture("_NormalTextureRight", _normalRightTexture);
        _propertyBlock.SetTexture("_ColorTextureLeft", _colorTexture);
        _propertyBlock.SetTexture("_ColorTextureRight", _colorRightTexture);

        _commandBuffer.Clear();
        _commandBuffer.DrawMesh(_quadMesh, Matrix4x4.identity, _material, 0, -1, _propertyBlock);
        // Graphics.DrawMesh(_quadMesh, Matrix4x4.identity, _material, 0, null, 0, _propertyBlock);
    }

    #endregion

    #region Zed events

    void OnZedReady()
    {
        var zedCamera = _manager.zedCamera;
        _depthTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.DEPTH);
        _depthRightTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.DEPTH_RIGHT);
        _normalTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.NORMALS);
        _normalRightTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.NORMALS_RIGHT);
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

        pos0 += new Vector3(-1f * _offset.x, _offset.y, _offset.z);
        pos1 += _offset;

        _planeMatrices[0] = Matrix4x4.TRS(pos0, Quaternion.identity, scale0);
        _planeMatrices[1] = Matrix4x4.TRS(pos1, Quaternion.identity, scale1);
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

    /// <summary>
    /// Scales the canvas in front of the camera so that it fills the whole screen exactly.
    /// </summary>
    /// <param name="screen">Canvas object.</param>
    /// <param name="fov">Camera's vertical field of view. </param>
	private Vector3 scale(Vector3 position, float fov)
    {
		float height = Mathf.Tan(0.5f * fov) * Vector3.Distance(position, Vector3.zero) * 2;
		return new Vector3((height*aspect), height, 1);
    }

    #endregion
}
