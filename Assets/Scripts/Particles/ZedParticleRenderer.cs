using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ZedParticleRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Shader _kernelShader = null;

    [SerializeField]
    Material _material = null;

    [SerializeField, Range(0.1f, 2)]
    float _resolutionScale = 0.5f;

    [SerializeField, Range(0, 1)]
    float _speed = 1f;

    [SerializeField]
    float _scale = 1f;

    #region Emitter Parameters

    [SerializeField, Range(0, 1), Header("Emitter Parameters")]
    float _throttle = 1.0f;

    public float throttle
    {
        get { return _throttle; }
        set { _throttle = value; }
    }

    #endregion

    #region Particle Life Parameters

    [SerializeField, Header("Particle Life Parameters")]
    float _life = 4.0f;

    public float life
    {
        get { return _life; }
        set { _life = value; }
    }

    [SerializeField, Range(0, 1)]
    float _lifeRandomness = 0.6f;

    public float lifeRandomness
    {
        get { return _lifeRandomness; }
        set { _lifeRandomness = value; }
    }

    #endregion

    [SerializeField]
    float _randomSeed = 12345.6789f;

    [SerializeField]
    int _batchSize = 2000;

    #endregion

    #region Fields

    Vector2Int _particleResolution;
    // Kvant.Spray.BulkMesh _bulkMesh;

    CustomZedManager _manager;

    RenderTexture _colorBuffer1;
    RenderTexture _colorBuffer2;
    RenderTexture _positionBuffer1;
    RenderTexture _positionBuffer2;

    Texture2D _xyzTexture;
    Texture2D _colorTexture;

    Material _kernelMaterial;
    MaterialPropertyBlock _propertyBlock;
    Camera _camera;
    CommandBuffer _commandBuffer;


    int _idPositionBuffer;
    int _idColorBuffer;
    int _idXYZTex;
    int _idColorTex;

    int _idCameraViewMat;

    int _idLifeParams;
    int _idConfig;
    int _idScaleMin;
    int _idScaleMax;
    int _idRandomSeed;
    int _idUVOffset;

    int _kernelInitParticle;
    int _kernelUpdateParticle;

    static float deltaTime
    {
        get
        {
            var isEditor = !Application.isPlaying || Time.frameCount < 2;
            return isEditor ? 1.0f / 10 : Time.deltaTime;
        }
    }

    #endregion

    #region Unity events

    private void Awake()
    {
        _manager = CustomZedManager.Instance;
        CustomZedManager.OnZEDReady += OnZedReady;

        _idPositionBuffer = Shader.PropertyToID("_PositionBuffer");
        _idColorBuffer = Shader.PropertyToID("_ColorBuffer");
        _idXYZTex = Shader.PropertyToID("_XYZTex");
        _idColorTex = Shader.PropertyToID("_ColorTex");

        _idCameraViewMat = Shader.PropertyToID("_CameraViewMat");
        _idLifeParams = Shader.PropertyToID("_LifeParams");
        _idConfig = Shader.PropertyToID("_Config");

        _idScaleMin = Shader.PropertyToID("_ScaleMin");
        _idScaleMax = Shader.PropertyToID("_ScaleMax");
        _idRandomSeed = Shader.PropertyToID("_RandomSeed");

        _idUVOffset = Shader.PropertyToID("_UVOffset");

        _kernelMaterial = CreateMaterial(_kernelShader);
        _propertyBlock = new MaterialPropertyBlock();

        _camera = Camera.main;
        _commandBuffer = new CommandBuffer();
    }

    private void OnEnable()
    {
        // _particleBuffer = new ComputeBuffer(_maxParticles, sizeof(float) * 4);

        // const int groupSize = 512;
        // int groupsX = (_particleBuffer.count + groupSize - 1) / groupSize;

        // _particleKernel.SetInt(_idMaxParticles, _maxParticles);
        // _particleKernel.SetVector(_idConfig, new Vector4(_throttle, _randomSeed, Time.deltaTime, Time.time));

        // _particleKernel.SetBuffer(_kernelInitParticle, _idParticleBuffer, _particleBuffer);
        // _particleKernel.Dispatch(_kernelInitParticle, groupsX, 1, 1);

        // _particleKernel.SetBuffer(_kernelUpdateParticle, _idParticleBuffer, _particleBuffer);

        // _propertyBlock.SetBuffer(_idParticleBuffer, _particleBuffer);

        _camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
    }

    private void OnDisable()
    {
        // _particleBuffer.Release();
        if (_camera != null)
        {
            _camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _commandBuffer);
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

        UpdateKernelShader();
        SwapBuffersAndInvokeKernels();

        _propertyBlock.SetTexture(_idPositionBuffer, _positionBuffer2);
        _propertyBlock.SetTexture(_idColorBuffer, _colorBuffer2);
        _propertyBlock.SetFloat(_idScaleMin, _scale);
        _propertyBlock.SetFloat(_idScaleMax, _scale);
        _propertyBlock.SetFloat(_idRandomSeed, _randomSeed);

        _commandBuffer.Clear();
        var numParticles = _particleResolution.x * _particleResolution.y;
        for (int i = 0; i < numParticles; i += _batchSize)
        {
            _propertyBlock.SetInt("_InstanceOffset", i);
            // Graphics.DrawProcedural(MeshTopology.Triangles, 3, Mathf.Min(_batchSize, numParticles - i));
            _commandBuffer.DrawProcedural(Matrix4x4.identity, _material, 0,
                MeshTopology.Triangles, 3, Mathf.Min(_batchSize, numParticles - i), _propertyBlock);
        }
    }

    private void OnRenderObject()
    {
        // _material.SetPass(0);
        // _material.SetTexture(_idPositionBuffer, _positionBuffer2);
        // _material.SetTexture(_idColorBuffer, _colorBuffer2);
        // _material.SetFloat(_idScaleMin, _scale);
        // _material.SetFloat(_idScaleMax, _scale);
        // _material.SetFloat(_idRandomSeed, _randomSeed);

        // var numParticles = _particleResolution.x * _particleResolution.y;
        // for (int i = 0; i < numParticles; i += _batchSize)
        // {
        //     _material.SetInt("_InstanceOffset", i);
        //     Graphics.DrawProcedural(MeshTopology.Triangles, 3, Mathf.Min(_batchSize, numParticles - i));
        // }
    }

    #endregion

    #region Zed event

    void OnZedReady()
    {
        var zedCamera = _manager.zedCamera;
        _xyzTexture = zedCamera.CreateTextureMeasureType(sl.MEASURE.XYZ);
        _colorTexture = zedCamera.CreateTextureImageType(sl.VIEW.LEFT);

        var zedWidth = zedCamera.ImageWidth;
        var zedHeight = zedCamera.ImageHeight;

        _particleResolution = new Vector2Int(
            Mathf.FloorToInt(zedWidth * _resolutionScale),
            Mathf.FloorToInt(zedHeight * _resolutionScale)
        );

        _colorBuffer1 = CreateBuffer(RenderTextureFormat.ARGB32);
        _colorBuffer2 = CreateBuffer(RenderTextureFormat.ARGB32);
        _positionBuffer1 = CreateBuffer(RenderTextureFormat.ARGBFloat);
        _positionBuffer2 = CreateBuffer(RenderTextureFormat.ARGBFloat);

        var pos = _manager.HMDSyncPosition;
        var rot = _manager.HMDSyncRotation;
        if (rot.x == 0 && rot.y == 0 &&
            rot.z == 0 && rot.w == 0)
            rot = Quaternion.identity;

        _kernelMaterial.SetMatrix(_idCameraViewMat, Matrix4x4.TRS(pos, rot, Vector3.one));
        _kernelMaterial.SetTexture(_idXYZTex, _xyzTexture);
        _kernelMaterial.SetTexture(_idColorTex, _colorTexture);

        Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 0);
        Graphics.Blit(null, _colorBuffer2, _kernelMaterial, 1);
    }

    #endregion

    #region Private methods

    void UpdateKernelShader()
    {
        var m = _kernelMaterial;

        var invLifeMax = 1.0f / Mathf.Max(_life, 0.01f);
        var invLifeMin = invLifeMax / Mathf.Max(1 - _lifeRandomness, 0.01f);
        m.SetVector(_idLifeParams, new Vector2(invLifeMin, invLifeMax));

        m.SetVector(_idConfig, new Vector4(_throttle, _randomSeed, deltaTime, Time.time));
    }

    Material CreateMaterial(Shader shader)
    {
        var material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        return material;
    }

    RenderTexture CreateBuffer(RenderTextureFormat format)
    {
        var buffer = new RenderTexture(_particleResolution.x, _particleResolution.y, 0, format);
        buffer.hideFlags = HideFlags.DontSave;
        buffer.filterMode = FilterMode.Point;
        buffer.wrapMode = TextureWrapMode.Repeat;
        return buffer;
    }

    void SwapBuffersAndInvokeKernels()
    {
        // Swap the buffers.
        var tempPosition = _positionBuffer1;
        var tempColor = _colorBuffer1;

        _positionBuffer1 = _positionBuffer2;
        _colorBuffer1 = _colorBuffer2;

        _positionBuffer2 = tempPosition;
        _colorBuffer2 = tempColor;

        var pos = _manager.HMDSyncPosition;
        var rot = _manager.HMDSyncRotation;
        var hmdToZed = _manager.arRig.HmdToZEDCalibration;
        var cameraMat = Matrix4x4.TRS(pos, rot, Vector3.one) * Matrix4x4.TRS(hmdToZed.translation, hmdToZed.rotation, Vector3.one);

        _kernelMaterial.SetVector(_idUVOffset, new Vector2(Random.Range(0, 1f / _particleResolution.x), Random.Range(0, 1f / _particleResolution.y)));
        _kernelMaterial.SetMatrix(_idCameraViewMat, cameraMat);
        _kernelMaterial.SetTexture(_idPositionBuffer, _positionBuffer1);
        _kernelMaterial.SetTexture(_idColorBuffer, _colorBuffer1);
        Graphics.Blit(null, _positionBuffer2, _kernelMaterial, 2);

        _kernelMaterial.SetTexture(_idPositionBuffer, _positionBuffer2);
        Graphics.Blit(null, _colorBuffer2, _kernelMaterial, 3);
    }

    #endregion
}
