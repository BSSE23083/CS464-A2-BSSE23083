using UnityEngine;

// Easing curves. If your Lecture 05 project already has an Ease.cs with OutBack,
// DELETE this file and use yours (two classes named Ease = compile error).
public static class Ease
{
    public static float OutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }

    public static float OutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
