using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZedParticleRenderer : MonoBehaviour
{
    #region Serialized fields

    #endregion

    #region Fields

    CustomZedManager _manager;

    #endregion

    #region Unity events

    private void Awake()
    {
        _manager = CustomZedManager.Instance;


    }

    #endregion
}
