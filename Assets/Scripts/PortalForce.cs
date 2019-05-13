using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalForce : MonoBehaviour
{
    #region Serialized fields

    [SerializeField, Range(0, 10)]
    float _verticalForce = 1f;

    [SerializeField, Range(0, 0.5f)]
    float _verticalMaxDistance = 0.1f;

    [SerializeField, Range(0.5f, 3f)]
    float _horizontalMaxDistance = 1.5f;

    [SerializeField, Range(0, 10)]
    float _horizontalForce = 1f;

    [SerializeField, Range(0, 10)]
    float _randomForce = 0.5f;

    #endregion

    #region Fields

    Transform _cameraTransform;
    Rigidbody _rigidbody;

    #endregion

    #region Unity events

    private void Awake()
    {
        _cameraTransform = Camera.main.transform;
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        var r = (_cameraTransform.position - _rigidbody.position);
        var n = r.normalized;

        if (Mathf.Abs(r.y) > _verticalMaxDistance)
        {
            _rigidbody.AddForce(new Vector3(0, r.y, 0) * _verticalForce, ForceMode.Acceleration);
        }

        var hr = new Vector2(r.x, r.z);
        var hl = hr.magnitude;
        var hn = hr.normalized;

        if (hl > _horizontalMaxDistance)
        {
            _rigidbody.AddForce(new Vector3(hr.x, 0, hr.y) * _horizontalForce, ForceMode.Acceleration);
        }

        _rigidbody.AddForce(Random.onUnitSphere * _randomForce, ForceMode.Acceleration);
    }

    #endregion
}
