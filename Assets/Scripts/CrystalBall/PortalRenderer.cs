using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PortalRenderer : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    Material _fillZMaterial;

    [SerializeField]
    Material _portalMaterial;

    [SerializeField]
    CameraEvent _cameraEvent = CameraEvent.BeforeForwardAlpha;

    #endregion

    #region Fields

    Camera _camera;
    CommandBuffer _commandBuffer;
    MeshFilter _meshFilter;

    #endregion

    #region Unity events

    private void Awake()
    {
        _camera = Camera.main;
        _commandBuffer = new CommandBuffer();
        _meshFilter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        _camera.AddCommandBuffer(_cameraEvent, _commandBuffer);
    }

    private void OnDisable()
    {
        if (_camera != null)
        {
            _camera.RemoveCommandBuffer(_cameraEvent, _commandBuffer);
        }
    }

    private void Update()
    {
        _commandBuffer.Clear();
        _commandBuffer.DrawProcedural(Matrix4x4.identity, _fillZMaterial, 0, MeshTopology.Triangles, 3, 1, null);
        _commandBuffer.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, _portalMaterial, 0, 0, null);
    }

    #endregion
}
