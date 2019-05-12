using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderableManager : MonoBehaviour
{
    #region Static fields

    static RenderableManager s_instance;
    public static RenderableManager instance
    {
        get
        {
            if (s_instance == null)
            {
                s_instance = FindObjectOfType<RenderableManager>();
            }
            return s_instance;
        }
    }

    #endregion

    #region Serialized fields

    [SerializeField]
    Renderable _initialRenderable = null;

    #endregion

    #region Fields

    List<Renderable> _renderables = new List<Renderable>();

    #endregion

    #region Public methods

    public void RemoveFirstRenderable()
    {
        _renderables.RemoveAt(0);
    }

    public void AddRenderable(Renderable renderable)
    {
        _renderables.Add(renderable);
    }

    public void RemoveRenderable(Renderable renderable)
    {
        _renderables.Remove(renderable);
    }

    public void Render(CommandBuffer commandBuffer)
    {
        for (int i = 0; i < _renderables.Count; i++)
        {
            _renderables[i].Render(commandBuffer);
        }
    }

    #endregion

    #region Unity events

    private void Awake()
    {
        s_instance = this;
        _renderables.Insert(0, _initialRenderable);
    }

    #endregion
}
