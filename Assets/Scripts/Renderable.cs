using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public abstract class Renderable : MonoBehaviour
{
    public abstract void Render(CommandBuffer commandBuffer);
}
