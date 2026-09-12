// Build icinde sol ustte FPS gosterir.
using UnityEngine;

public class FoggyRoad_FpsCounter : MonoBehaviour
{
    float birikim;
    int kare;
    float suanFps;
    float sonGuncelleme;
    GUIStyle stil;

    void Update()
    {
        birikim += Time.unscaledDeltaTime;
        kare++;
        if (birikim >= 0.5f)
        {
            suanFps = kare / birikim;
            birikim = 0f;
            kare = 0;
            sonGuncelleme = Time.unscaledTime;
        }
    }

    void OnGUI()
    {
        if (stil == null)
        {
            stil = new GUIStyle(GUI.skin.label);
            stil.fontSize = 26;
            stil.normal.textColor = Color.yellow;
            stil.fontStyle = FontStyle.Bold;
        }
        float ms = suanFps > 0.01f ? 1000f / suanFps : 0f;
        GUI.Label(new Rect(12, 8, 500, 40), suanFps.ToString("0.0") + " FPS   (" + ms.ToString("0.0") + " ms)", stil);
    }
}
