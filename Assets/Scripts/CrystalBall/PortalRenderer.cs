using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PortalRenderer : Renderable
{
    #region Serialized fields

    [SerializeField]
    Material _fillZMaterial;

    [SerializeField]
    Material _portalMaterial;

    [SerializeField]
    Renderable _target;

    [SerializeField]
    PortalRenderer _nextPortal;

    [SerializeField, Range(10, 120)]
    float _nextPortalWaitSeconds = 30;

    [SerializeField]
    AudioClip _portalSound;

    #endregion

    #region Fields

    Camera _camera;
    // CommandBuffer _commandBuffer;
    MeshFilter _meshFilter;

    #endregion

    #region Unity events

    private void Awake()
    {
        _camera = Camera.main;
        // _commandBuffer = new CommandBuffer();
        _meshFilter = GetComponent<MeshFilter>();
    }

    private void OnEnable()
    {
        var manager = RenderableManager.instance;
        manager.AddRenderable(this);
        manager.AddRenderable(_target);
    }

    private void OnDisable()
    {
        var manager = RenderableManager.instance;
        if (manager != null)
        {
            manager.RemoveRenderable(this);
        }
    }

    public void EnterPortal()
    {
        RenderableManager.instance.RemoveFirstRenderable();
        gameObject.SetActive(false);
        SoundManager.instance.PlayOneShot(_portalSound, 0.05f);
        if (_nextPortal != null)
        {
            // _nextPortal.gameObject.SetActive(true);
            RenderableManager.instance.SpawnPortalInSeconds(_nextPortal.gameObject, _nextPortalWaitSeconds);
        }
    }

    public override void Render(CommandBuffer commandBuffer)
    {
        commandBuffer.DrawProcedural(Matrix4x4.identity, _fillZMaterial, 0, MeshTopology.Triangles, 3, 1, null);
        commandBuffer.DrawMesh(_meshFilter.sharedMesh, transform.localToWorldMatrix, _portalMaterial, 0, 0, null);
    }

    private void Update()
    {
        // _commandBuffer.Clear();
        // Render(_commandBuffer);
    }

    #endregion
}
