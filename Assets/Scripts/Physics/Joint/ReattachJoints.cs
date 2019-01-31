using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReattachJoints : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        var go = gameObject;
        var characters = GetComponents<CharacterJoint>();
        for (int i = 0; i < characters.Length; i++)
        {
            var from = characters[i];
            var to = go.AddComponent<CharacterJoint>();
            CopyJoint(from, to);
            CopyCharacterJoint(from, to);
            DestroyImmediate(from);
        }

        var hinges = GetComponents<HingeJoint>();
        for (int i = 0; i < hinges.Length; i++)
        {
            var from = hinges[i];
            var to = go.AddComponent<HingeJoint>();
            CopyJoint(from, to);
			CopyHingeJoint(from, to);
            DestroyImmediate(from);
        }

		var springs = GetComponents<SpringJoint>();
		for (int i = 0; i < springs.Length; i++)
		{
			var from = springs[i];
			var to = go.AddComponent<SpringJoint>();
			CopyJoint(from, to);
			CopySpringJoint(from, to);
			DestroyImmediate(from);
		}

		var fixeds = GetComponents<FixedJoint>();
		for (int i = 0; i < fixeds.Length; i++)
		{
			var from = fixeds[i];
			var to = go.AddComponent<FixedJoint>();
			CopyJoint(from, to);
			CopyFixedJoint(from, to);
			DestroyImmediate(from);
		}

		var configurables = GetComponents<ConfigurableJoint>();
		for (int i = 0; i < configurables.Length; i++)
		{
			var from = configurables[i];
			var to = go.AddComponent<ConfigurableJoint>();
			CopyJoint(from, to);
			CopyConfigurableJoint(from, to);
			DestroyImmediate(from);
		}
    }

    void CopyCharacterJoint(CharacterJoint from, CharacterJoint to)
    {
        to.enableProjection = from.enableProjection;
        to.highTwistLimit = from.highTwistLimit;
        to.lowTwistLimit = from.lowTwistLimit;
        to.projectionAngle = from.projectionAngle;
        to.projectionDistance = from.projectionDistance;
        to.swing1Limit = from.swing1Limit;
        to.swing2Limit = from.swing2Limit;
        to.swingAxis = from.swingAxis;
        to.swingLimitSpring = from.swingLimitSpring;
        to.twistLimitSpring = from.twistLimitSpring;
    }

    void CopyHingeJoint(HingeJoint from, HingeJoint to)
    {
        to.limits = from.limits;
        to.motor = from.motor;
        to.spring = from.spring;
        to.useLimits = from.useLimits;
        to.useMotor = from.useMotor;
        to.useSpring = from.useSpring;
    }

	void CopySpringJoint(SpringJoint from, SpringJoint to)
	{
		to.damper = from.damper;
		to.maxDistance = from.maxDistance;
		to.minDistance = from.minDistance;
		to.spring = from.spring;
		to.tolerance = from.tolerance;
	}

	void CopyFixedJoint(FixedJoint from, FixedJoint to)
	{
	}

	void CopyConfigurableJoint(ConfigurableJoint from, ConfigurableJoint to)
	{
		to.angularXDrive = from.angularXDrive;
		to.angularXLimitSpring = from.angularXLimitSpring;
		to.angularXMotion = from.angularXMotion;
		to.angularYLimit = from.angularYLimit;
		to.angularYMotion = from.angularYMotion;
		to.angularYZDrive = from.angularYZDrive;
		to.angularYZLimitSpring = from.angularYZLimitSpring;
		to.angularZLimit = from.angularZLimit;
		to.angularZMotion = from.angularZMotion;
		to.configuredInWorldSpace = from.configuredInWorldSpace;
		to.highAngularXLimit = from.highAngularXLimit;
		to.linearLimit = from.linearLimit;
		to.linearLimitSpring = from.linearLimitSpring;
		to.lowAngularXLimit = from.lowAngularXLimit;
		to.projectionAngle = from.projectionAngle;
		to.projectionDistance = from.projectionDistance;
		to.projectionMode = from.projectionMode;
		to.rotationDriveMode = from.rotationDriveMode;
		to.secondaryAxis = from.secondaryAxis;
		to.slerpDrive = from.slerpDrive;
		to.swapBodies = from.swapBodies;
		to.targetAngularVelocity = from.targetAngularVelocity;
		to.targetPosition = from.targetPosition;
		to.targetRotation = from.targetRotation;
		to.targetVelocity = from.targetVelocity;
		to.xDrive = from.xDrive;
		to.xMotion = from.xMotion;
		to.yDrive = from.yDrive;
		to.yMotion = from.yMotion;
		to.zDrive = from.zDrive;
		to.zMotion = from.zMotion;
	}

    void CopyJoint(Joint from, Joint to)
    {
        to.anchor = from.anchor;
        to.autoConfigureConnectedAnchor = from.autoConfigureConnectedAnchor;
        to.axis = from.axis;
        to.breakForce = from.breakForce;
        to.breakTorque = from.breakTorque;
        to.connectedAnchor = from.connectedAnchor;
        to.connectedBody = from.connectedBody;
        to.connectedMassScale = from.connectedMassScale;
        to.enableCollision = from.enableCollision;
        to.enablePreprocessing = from.enablePreprocessing;
        to.massScale = from.massScale;
    }
}
