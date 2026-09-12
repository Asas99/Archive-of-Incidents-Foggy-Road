// Kameraya takilir. Belirli layer'lardaki objeleri yakin mesafede keser.
// Sisli sahnede 90m oteyi zaten goremedigin icin bariyer/prop cizmek israf.
using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class FoggyRoad_LayerCulling : MonoBehaviour
{
    [Tooltip("Bariyerlerin bulundugu layer adi")]
    public string bariyerLayer = "Guardrail";
    [Tooltip("Bariyerler bu mesafeden sonra cizilmez (metre)")]
    public float bariyerMesafe = 95f;

    [Tooltip("Proplarin bulundugu layer adi (elektrik diregi vb.)")]
    public string propLayer = "Props";
    [Tooltip("Proplar bu mesafeden sonra cizilmez (metre)")]
    public float propMesafe = 140f;

    Camera cam;

    void OnEnable() { Uygula(); }
    void OnValidate() { Uygula(); }

    public void Uygula()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        float[] mesafeler = cam.layerCullDistances;
        if (mesafeler == null || mesafeler.Length != 32) mesafeler = new float[32];

        // 0 = kameranin kendi farClip'i kullanilir
        for (int i = 0; i < 32; i++) mesafeler[i] = 0f;

        int g = LayerMask.NameToLayer(bariyerLayer);
        if (g >= 0) mesafeler[g] = bariyerMesafe;

        int p = LayerMask.NameToLayer(propLayer);
        if (p >= 0) mesafeler[p] = propMesafe;

        cam.layerCullDistances = mesafeler;
        cam.layerCullSpherical = true;
    }
}
