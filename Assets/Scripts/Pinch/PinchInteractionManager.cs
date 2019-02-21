using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using Leap.Unity.Interaction;

[RequireComponent(typeof(InteractionManager))]
public class PinchInteractionManager : MonoBehaviour
{
    #region Serialized fields

    [SerializeField, Range(0.01f, 1)]
    [Tooltip("1 is instant, 0.01 is slow")]
    float _hysteresis = 0.3f;

    [SerializeField, Range(0, 1)]
    float _pinchThreshold = 0.9f;

    #endregion

    #region Singleton Pattern (Optional)

    private static PinchInteractionManager s_instance;
    public static PinchInteractionManager instance
    {
        get
        {
            if (s_instance == null) { s_instance = FindObjectOfType<PinchInteractionManager>(); }
            return s_instance;
        }
        set { s_instance = value; }
    }

    #endregion

    #region Private fields

    InteractionManager _manager;
    public InteractionManager manager
    {
        get { return _manager; }
    }
    Dictionary<InteractionController, float> _pinchStrengths = new Dictionary<InteractionController, float>();
	List<InteractionController> _toRemoveControllers = new List<InteractionController>();

    #endregion

    #region Events

    public Action<InteractionController> OnControllerPinchBegin = (c) => {};
    public Action<InteractionController> OnControllerPinchEnd = (c) => {};
    public Action<InteractionController> OnControllerPinchStay = (c) => {};

    #endregion

    #region Unity events

    private void Awake()
    {
        if (s_instance == null) s_instance = this;

        _manager = GetComponent<InteractionManager>();
    }

    private void OnEnable()
    {
        _manager.OnPostPhysicalUpdate -= OnPostPhysicalUpdate;
        _manager.OnPostPhysicalUpdate += OnPostPhysicalUpdate;
    }

    private void OnDisable()
    {
        if (_manager != null)
        {
            _manager.OnPostPhysicalUpdate -= OnPostPhysicalUpdate;
        }
    }

    #endregion

    #region Leap interaction events

    void OnPostPhysicalUpdate()
    {
        _toRemoveControllers.Clear();
        foreach (var pair in _pinchStrengths)
        {
            var controller = pair.Key;
            if (!_manager.interactionControllers.Contains(controller))
            {
                _toRemoveControllers.Add(controller);
            }
        }
        for (int i = 0; i < _toRemoveControllers.Count; i++)
        {
            var controller = _toRemoveControllers[i];
            if (IsPinching(controller))
            {
                OnControllerPinchEnd(controller);
            }
            _pinchStrengths.Remove(controller);
        }
        _toRemoveControllers.Clear();

        foreach (var controller in _manager.interactionControllers)
        {
            var pinchRaw = controller.intHand.leapHand.PinchStrength;
            if (!_pinchStrengths.ContainsKey(controller))
            {
                _pinchStrengths.Add(controller, pinchRaw);
                if (pinchRaw > _pinchThreshold)
                {
                    OnControllerPinchBegin(controller);
                }
            }
            else
            {
                var pinchOld = _pinchStrengths[controller];
                var pinch = Mathf.Lerp(pinchOld, pinchRaw, _hysteresis);
                _pinchStrengths[controller] = pinch;
                if (pinchOld > _pinchThreshold && pinch <= _pinchThreshold)
                {
                    OnControllerPinchEnd(controller);
                }
                else if (pinchOld <= _pinchThreshold && pinch > _pinchThreshold)
                {
                    OnControllerPinchBegin(controller);
                }
                else
                {
                    OnControllerPinchStay(controller);
                }
            }
        }
    }

    #endregion

    #region Public methods

    public float GetPinchStrength(InteractionController controller)
    {
        if (_pinchStrengths.ContainsKey(controller))
        {
            return _pinchStrengths[controller];
        }
        else
        {
            return 0f;
        }
    }

    public bool IsPinching(InteractionController controller)
    {
        return GetPinchStrength(controller) > _pinchThreshold;
    }

    #endregion

}
