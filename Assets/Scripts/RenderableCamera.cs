using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class RenderableCamera : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    CameraEvent _cameraEvent = CameraEvent.AfterForwardOpaque;

    [SerializeField]
    bool _trackZedCamera = false;

    [SerializeField]
    Vector3 _offset = new Vector3(0.0315f, 0f, 0.0575f);

    #endregion

    #region Fields

    Camera _camera;
    CommandBuffer _commandBuffer;
    Camera _mainCamera;

    #endregion

    #region Unity events

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _mainCamera = Camera.main;
        _commandBuffer = new CommandBuffer();
    }

    private void OnEnable()
    {
        _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
        CustomZedManager.OnZEDReady += ZedReady;
    }

    private void OnDisable()
    {
        if (_camera != null)
        {
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
        }
        CustomZedManager.OnZEDReady -= ZedReady;
    }

    private void Update()
    {
        _commandBuffer.Clear();
        RenderableManager.instance.Render(_commandBuffer);

        if (_trackZedCamera)
        {
            var manager = CustomZedManager.Instance;
            if (manager.IsZEDReady)
            {
                var mainCameraTransform = _mainCamera.transform;
                var pos = mainCameraTransform.TransformPoint(_offset);
                // _camera.projectionMatrix = manager.zedCamera.Projection;
                transform.SetPositionAndRotation(pos, manager.HMDSyncRotation);
            }

            // transform.SetPositionAndRotation(mainCameraTransform.position, mainCameraTransform.rotation);
        }
    }

    void ZedReady()
    {
        if (_trackZedCamera)
        {
            _camera.fieldOfView = sl.ZEDCamera.GetInstance().GetFOV() * Mathf.Rad2Deg;
            _camera.projectionMatrix = sl.ZEDCamera.GetInstance().Projection;
        }
    }

    #endregion
}
