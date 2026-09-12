using UnityEngine;

/// <summary>
/// Oyunda otomatik olarak ekranın sol üst köşesinde FPS ve frame time gösterir.
/// Sahneye GameObject eklemek gerekmez. F3 ile gizlenip gösterilebilir.
/// </summary>
public sealed class FPSCounter : MonoBehaviour
{
    [Header("Güncelleme")]
    [SerializeField, Min(0.1f)] private float yenilemeAralığı = 0.25f;

    [Header("Görünüm")]
    [SerializeField] private Vector2 ekranBoşluğu = new Vector2(16f, 12f);
    [SerializeField, Min(10)] private int temelYazıBoyutu = 24;
    [SerializeField] private bool frameTimeGöster = true;

    private float geçenSüre;
    private int kareSayısı;
    private float güncelFps;
    private string gösterilecekMetin = "FPS: --";
    private bool görünür = true;

    private GUIStyle yazıStili;
    private GUIStyle gölgeStili;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OtomatikOluştur()
    {
        if (FindObjectOfType<FPSCounter>() != null)
        {
            return;
        }

        GameObject sayaçObjesi = new GameObject(nameof(FPSCounter));
        DontDestroyOnLoad(sayaçObjesi);
        sayaçObjesi.AddComponent<FPSCounter>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            görünür = !görünür;
        }

        kareSayısı++;
        geçenSüre += Time.unscaledDeltaTime;

        if (geçenSüre < yenilemeAralığı)
        {
            return;
        }

        güncelFps = kareSayısı / geçenSüre;
        float frameTimeMs = güncelFps > 0f ? 1000f / güncelFps : 0f;

        gösterilecekMetin = frameTimeGöster
            ? $"FPS: {güncelFps:0}\n{frameTimeMs:0.0} ms"
            : $"FPS: {güncelFps:0}";

        kareSayısı = 0;
        geçenSüre = 0f;
    }

    private void OnGUI()
    {
        if (!görünür)
        {
            return;
        }

        StilleriHazırla();

        Color fpsRengi = güncelFps >= 60f
            ? new Color(0.35f, 1f, 0.35f)
            : güncelFps >= 30f
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(1f, 0.3f, 0.3f);

        yazıStili.normal.textColor = fpsRengi;

        Rect alan = new Rect(ekranBoşluğu.x, ekranBoşluğu.y, 220f, 70f);
        Rect gölgeAlanı = new Rect(alan.x + 2f, alan.y + 2f, alan.width, alan.height);

        GUI.Label(gölgeAlanı, gösterilecekMetin, gölgeStili);
        GUI.Label(alan, gösterilecekMetin, yazıStili);
    }

    private void StilleriHazırla()
    {
        int ölçekliYazıBoyutu = Mathf.RoundToInt(
            temelYazıBoyutu * Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f));

        if (yazıStili != null && yazıStili.fontSize == ölçekliYazıBoyutu)
        {
            return;
        }

        yazıStili = new GUIStyle(GUI.skin.label)
        {
            fontSize = ölçekliYazıBoyutu,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };

        gölgeStili = new GUIStyle(yazıStili);
        gölgeStili.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
    }
}
