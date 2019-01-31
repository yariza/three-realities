using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringJointController : MonoBehaviour
{
	SpringJoint m_spring;
	SpringJoint spring
	{
		get
		{
			if (m_spring == null)
			{
				m_spring = GetComponent<SpringJoint>();
			}
			return m_spring;
		}
	}

	public void SetTolerance(float tolerance)
	{
		spring.tolerance = tolerance;
	}
}
