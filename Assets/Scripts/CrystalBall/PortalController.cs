using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour
{
    #region Serialized fields

    [SerializeField, Range(0, 0.3f)]
    float _distance;

    // [SerializeField]
    // GameObject _disableObject;

    #endregion

    #region Fields

    Camera _camera;
    Transform _cameraTransform;
    PortalRenderer _portalRenderer;

    #endregion

    #region Unity events

    private void Awake()
    {
        _camera = Camera.main;
        _cameraTransform = _camera.transform;
        _portalRenderer = GetComponent<PortalRenderer>();
    }

    private void Update()
    {
        if (Vector3.Distance(_cameraTransform.position, transform.position) < _distance &&
            Vector3.Dot(_cameraTransform.forward, transform.position - _cameraTransform.position) > 0)
        {
            // _disableObject.SetActive(false);
            // gameObject.SetActive(false);
            _portalRenderer.EnterPortal();
        }
    }

    #endregion
}
