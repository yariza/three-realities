using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Attachments;

[RequireComponent(typeof(AttachmentPointBehaviour))]
public class ContinuousSpatialMappingHandTracker : MonoBehaviour
{
    AttachmentPointBehaviour _attachment;
	int _HandLeftPositionId;
	int _HandRightPositionId;

    private void Awake()
    {
        _attachment = GetComponent<AttachmentPointBehaviour>();
		_HandLeftPositionId = Shader.PropertyToID("_HandLeftPosition");
		_HandRightPositionId = Shader.PropertyToID("_HandRightPosition");
    }

    void Update()
    {
		var pos = transform.position;
		var hand = _attachment.attachmentHand;
		var tracked = hand.isTracked;
		var chirality = hand.chirality;

		Shader.SetGlobalVector(
			chirality == Chirality.Left ? _HandLeftPositionId : _HandRightPositionId,
			new Vector4(pos.x, pos.y, pos.z, tracked ? 1 : 0)
		);
    }
}
