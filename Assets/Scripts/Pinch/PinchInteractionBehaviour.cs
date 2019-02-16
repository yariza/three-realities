using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Interaction;

[RequireComponent(typeof(InteractionBehaviour))]
public class PinchInteractionBehaviour : MonoBehaviour
{
    #region Serialized fields

    [SerializeField]
    PinchInteractionManager _pinchManager;
    public PinchInteractionManager pinchManager
    {
        get { return _pinchManager; }
    }

    [System.Serializable]
    public class PinchInteractionEvent : UnityEvent
    {}

    [System.Serializable]
    public class PinchInteractionEventWithController : UnityEvent<InteractionController>
    {}

    [SerializeField]
    PinchInteractionEvent _OnPinchBegin;
    [SerializeField]
    PinchInteractionEvent _OnPinchEnd;
    [SerializeField]
    PinchInteractionEvent _OnPinchStay;
    [SerializeField]
    PinchInteractionEventWithController _OnPerControllerPinchBegin;
    [SerializeField]
    PinchInteractionEventWithController _OnPerControllerPinchEnd;

    #endregion

    #region Public fields

    public Action<InteractionController> OnPerControllerPinchBegin = (c) => { };
    public Action<InteractionController> OnPerControllerPinchEnd = (c) => { };

    public Action OnPinchBegin = () => { };
    public Action OnPinchEnd = () => { };
    public Action OnPinchStay = () => { };

    public InteractionController pinchingController
    {
        get { return _pinchingControllers.Query().FirstOrDefault(); }
    }

	public ReadonlyHashSet<InteractionController> pinchingControllers
	{
		get { return _pinchingControllers; }
	}

    public bool IsBeingPinched
    {
        get { return _pinchingControllers.Count != 0; }
    }

    #endregion

    #region Private fields

    InteractionBehaviour _interaction;
    HashSet<InteractionController> _pinchingControllers = new HashSet<InteractionController>();
	List<InteractionController> _toRemoveControllers = new List<InteractionController>();

    #endregion

    #region Unity events

    private void Awake()
    {
        _interaction = GetComponent<InteractionBehaviour>();

        OnPinchBegin += _OnPinchBegin.Invoke;
        OnPinchEnd += _OnPinchEnd.Invoke;
        OnPinchStay += _OnPinchStay.Invoke;
        OnPerControllerPinchBegin += _OnPerControllerPinchBegin.Invoke;
        OnPerControllerPinchEnd += _OnPerControllerPinchEnd.Invoke;
    }

    private void OnEnable()
    {
        _interaction.OnPerControllerGraspBegin -= OnPerControllerGraspBegin;
        _interaction.OnPerControllerGraspBegin += OnPerControllerGraspBegin;
        _interaction.OnPerControllerGraspEnd -= OnPerControllerGraspEnd;
        _interaction.OnPerControllerGraspEnd += OnPerControllerGraspEnd;
        _interaction.OnGraspStay -= OnGraspStay;
        _interaction.OnGraspStay += OnGraspStay;

        if (_pinchManager == null) {
            _pinchManager = PinchInteractionManager.instance;

            if (_pinchManager == null) {
                Debug.LogError("Pinch Interaction Behaviours require an Pinch Interaction Manager. Please "
                            + "ensure you have an Pinch InteractionManager in your scene.");
                this.enabled = false;
            }
        }
    }

    private void OnDisable()
    {
        _interaction.OnPerControllerGraspBegin -= OnPerControllerGraspBegin;
        _interaction.OnPerControllerGraspEnd -= OnPerControllerGraspEnd;
        _interaction.OnGraspStay -= OnGraspStay;
    }

    #endregion

    #region Leap interaction events

    void OnPerControllerGraspBegin(InteractionController controller)
    {
		if (_pinchManager.IsPinching(controller))
		{
			_pinchingControllers.Add(controller);
			OnPerControllerPinchBegin(controller);

            if (_pinchingControllers.Count == 1)
            {
                OnPinchBegin();
            }
		}
    }

    void OnPerControllerGraspEnd(InteractionController controller)
    {
		if (_pinchingControllers.Contains(controller))
		{
			_pinchingControllers.Remove(controller);
			OnPerControllerPinchEnd(controller);

            if (_pinchingControllers.Count == 0)
            {
                OnPinchEnd();
            }
		}
    }

    void OnGraspStay()
    {
		_toRemoveControllers.Clear();
		foreach (var controller in _pinchingControllers)
		{
			if (!_pinchManager.IsPinching(controller))
			{
				_toRemoveControllers.Add(controller);
			}
		}
		for (int i = 0; i < _toRemoveControllers.Count; i++)
		{
			var controller = _toRemoveControllers[i];
			_pinchingControllers.Remove(controller);
			OnPerControllerPinchEnd(controller);
            if (_pinchingControllers.Count == 0)
            {
                OnPinchEnd();
            }
		}
		_toRemoveControllers.Clear();
        foreach (var controller in _interaction.graspingControllers)
        {
            if (_pinchManager.IsPinching(controller) && !_pinchingControllers.Contains(controller))
            {
                _pinchingControllers.Add(controller);
                OnPerControllerPinchBegin(controller);
                if (_pinchingControllers.Count == 1)
                {
                    OnPinchBegin();
                }
            }
        }
        if (_pinchingControllers.Count != 0)
        {
            OnPinchStay();
        }
    }

    #endregion
}
