using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ZEDPlaneDetectionManager))]
public class ContinuousPlaneDetection : MonoBehaviour
{

    [SerializeField, Range(0.5f, 5f)]
    float _detectionInterval = 0.5f;

    ZEDPlaneDetectionManager _manager;
    Coroutine _detectionCoroutine;

    #region Monobehaviour functions

    private void Awake()
    {
        _manager = GetComponent<ZEDPlaneDetectionManager>();
    }

    private void OnEnable()
    {
        if (_detectionCoroutine == null)
        {
            _detectionCoroutine = StartCoroutine(PlaneDetectionCoroutine());
        }
    }

    private void OnDisable()
    {
        if (_detectionCoroutine != null)
        {
            StopCoroutine(_detectionCoroutine);
            _detectionCoroutine = null;
        }
    }

    #endregion

    IEnumerator PlaneDetectionCoroutine()
    {
        while (true)
        {
			_manager.DetectFloorPlane(true);

            yield return new WaitForSecondsRealtime(_detectionInterval);
        }
    }
}
