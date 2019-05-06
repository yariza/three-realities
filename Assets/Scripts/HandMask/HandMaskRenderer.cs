using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity;

// [RequireComponent(typeof(Camera))]
public class HandMaskRenderer : MonoBehaviour
{
    [SerializeField]
    List<Renderer> _handMaskRenderers = new List<Renderer>();
    [SerializeField]
    Material _handMaskMaterial;

    [SerializeField]
    bool _useZedView;

    [SerializeField]
    Vector3 _zedOffsetVector;

    [SerializeField, Range(0, 1)]
    float _velocityHandExtrudeScale = 0f;
    [SerializeField, Range(0, 0.1f)]
    float _baseExtrude = 0.03f;

    [SerializeField, ReadOnly("texture")]
    RenderTexture _texture;
    public RenderTexture texture
    {
        get { return _texture; }
    }

    Camera _camera;
    CommandBuffer _commandBuffer;
    List<RiggedHand> _riggedHands = new List<RiggedHand>();
    CustomZedManager _manager;
    List<Vector3> _prevPalmPositions = new List<Vector3>();

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = Camera.main;
        }
        var width = _camera.pixelWidth;
        var height = _camera.pixelHeight;

        if (_useZedView)
        {
            width = 1280 / 2;
            height = 720 / 2;
        }

        var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.R8, 0);

        _texture = new RenderTexture(descriptor);

        var right = _camera.stereoTargetEye == StereoTargetEyeMask.Right;
        Shader.SetGlobalTexture(right ? "_HandMaskRight" : "_HandMaskLeft", _texture);

        for (int i = 0; i < _handMaskRenderers.Count; i++)
        {
            _riggedHands.Add(_handMaskRenderers[i].gameObject.GetComponentInParent<RiggedHand>());
            _prevPalmPositions.Add(Vector3.zero);
        }
        _commandBuffer = new CommandBuffer();
        _handMaskMaterial = new Material(_handMaskMaterial);
    }

    void OnEnable()
    {
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
    }

    void OnDisable()
    {
        if (_commandBuffer != null && _camera != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
        }
    }

    private void Start()
    {
        if (_useZedView)
        {
            _manager = CustomZedManager.Instance;
            CustomZedManager.OnZEDReady += OnZedReady;
        }
    }

    private Matrix4x4 GetZedProjection(float near = 0.1f, float far = 500f)
    {
        //float near = mainCamera.nearClipPlane;
        //float far = mainCamera.farClipPlane;

        Matrix4x4 newmat = _manager.zedCamera.Projection;
        newmat[2, 2] = -(far + near) / (far - near);
        newmat[2, 3] = -(2.0f * far * near) / (far - near);
        var projectionMatrix = newmat;
        return projectionMatrix;
    }

    Matrix4x4 _zedOffset;
    Matrix4x4 _zedProjection;

    void OnZedReady()
    {
        _zedProjection = GetZedProjection();
    }

    void Update()
    {
        _commandBuffer.Clear();
        if (_useZedView)
        {
            if (!_manager.IsZEDReady) return;
            // var pos = _manager.HMDSyncPosition;
            // var rot = _manager.HMDSyncRotation;
            // if (rot.x == 0 && rot.y == 0 && rot.z == 0 && rot.w == 0)
            // {
            //     return;
            // }

            // var hmdToZedMat = Matrix4x4.Translate(-hmdToZed.translation + _manager.zedCamera.GetCalibrationParameters);
            // Debug.Log(hmdToZed.translation);
            var projection = GetZedProjection();
            _commandBuffer.SetViewProjectionMatrices(Matrix4x4.Translate(_zedOffsetVector) * _camera.worldToCameraMatrix, _zedProjection);
            // _commandBuffer.SetProjectionMatrix(projection);
            // Debug.Log(cameraMat.inverse);
            // Debug.Log(projection);
        }
        _commandBuffer.SetRenderTarget(new RenderTargetIdentifier(_texture));
        _commandBuffer.ClearRenderTarget(false, true, Color.black);
        for (int i = 0; i < _handMaskRenderers.Count; i++)
        {
            var renderer = _handMaskRenderers[i];
            if (!_riggedHands[i].IsTracked) continue;
            var hand = _riggedHands[i];
            var handPos = hand.GetPalmPosition();
            var speed = ((handPos - _prevPalmPositions[i]) / Time.deltaTime).magnitude;
            var extrude = _baseExtrude + speed * _velocityHandExtrudeScale;
            _prevPalmPositions[i] = handPos;
            _handMaskMaterial.SetFloat("_Extrude", extrude);
            _commandBuffer.DrawRenderer(renderer, _handMaskMaterial);
        }
        _commandBuffer.SetRenderTarget(null as RenderTexture);
    }
}
