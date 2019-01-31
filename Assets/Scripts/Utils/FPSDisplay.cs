using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField]
    bool _show = true;

    float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Input.GetKeyDown(KeyCode.H))
        {
            _show = !_show;
        }
    }

    void OnGUI()
    {
        if (!_show) return;

        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 4 / 100);
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = h * 4 / 100;
        style.normal.textColor = Color.white;
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}
