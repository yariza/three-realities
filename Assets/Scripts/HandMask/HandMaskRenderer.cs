using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity;

[RequireComponent(typeof(Camera))]
public class HandMaskRenderer : MonoBehaviour
{
    [SerializeField]
    List<Renderer> _handMaskRenderers = new List<Renderer>();
    [SerializeField]
    Material _handMaskMaterial;

    [SerializeField]
    bool _useZedView;

    RenderTexture _texture;
    public RenderTexture texture
    {
        get { return _texture; }
    }

    Camera _camera;
    CommandBuffer _commandBuffer;
    List<RiggedHand> _riggedHands = new List<RiggedHand>();
    CustomZedManager _manager;

    void Awake()
    {
        _camera = GetComponent<Camera>();
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
        }
        _commandBuffer = new CommandBuffer();

    }

    void OnEnable()
    {
        _camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
    }

    void OnDisable()
    {
        if (_commandBuffer != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBuffer);
        }
    }

    private void Start()
    {
        if (_useZedView)
        {
            _manager = CustomZedManager.Instance;
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

    void Update()
    {
        _commandBuffer.Clear();
        if (_useZedView)
        {
            if (!_manager.IsZEDReady) return;
            var pos = _manager.HMDSyncPosition;
            var rot = _manager.HMDSyncRotation;
            if (rot.x == 0 && rot.y == 0 && rot.z == 0 && rot.w == 0)
            {
                return;
            }
            var hmdToZed = _manager.arRig.HmdToZEDCalibration;
            var cameraMat = Matrix4x4.TRS(pos, rot, Vector3.one) * Matrix4x4.TRS(hmdToZed.translation, hmdToZed.rotation, Vector3.one);
            var projection = GetZedProjection();
            // _commandBuffer.SetViewProjectionMatrices(cameraMat.inverse, projection);
            _commandBuffer.SetProjectionMatrix(projection);
            Debug.Log(cameraMat.inverse);
            Debug.Log(projection);
        }
        _commandBuffer.SetRenderTarget(new RenderTargetIdentifier(_texture));
        _commandBuffer.ClearRenderTarget(false, true, Color.black);
        for (int i = 0; i < _handMaskRenderers.Count; i++)
        {
            var renderer = _handMaskRenderers[i];
            if (!_riggedHands[i].IsTracked) continue;
            _commandBuffer.DrawRenderer(renderer, _handMaskMaterial);
        }
        _commandBuffer.SetRenderTarget(null as RenderTexture);
    }
}
