using UnityEngine;

public static class ColorUtils
{
    public static Color HSVLerp(Color a, Color b, float t)
    {
        float a_h, a_s, a_v;
        float b_h, b_s, b_v;
        Color.RGBToHSV(a, out a_h, out a_s, out a_v);
        Color.RGBToHSV(b, out b_h, out b_s, out b_v);
        float h = Mathf.Lerp(a_h, b_h, t);
        float s = Mathf.Lerp(a_s, b_s, t);
        float v = Mathf.Lerp(a_v, b_v, t);
        return Color.HSVToRGB(h, s, v);
    }
}
