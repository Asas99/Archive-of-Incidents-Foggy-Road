using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KarakterHareket : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float hareketHýzý = 12f;
    public float yerçekimi = -9.81f;
    public float zýplamaGücü = 3f;

    [Header("Kamera Ayarlarý")]
    public float fareHassasiyeti = 100f;
    public Transform oyuncuKamerasý;

    [Header("Zemin Kontrolü")]
    public Transform zeminKontrolNoktasý;
    public float zeminMesafesi = 0.4f;
    public LayerMask zeminMaskesi;

    private CharacterController karakterKontrolcüsü;
    private float xRotasyonu = 0f;
    private Vector3 hýzYönü;
    private bool yerdeMi;

    void Start()
    {
        // Karakter kontrolcüsünü al ve fare imlecini oyun ekranýna kilitle
        karakterKontrolcüsü = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        ZeminKontrolüYap();
        KameraDöndür();
        HareketEt();
        ZýplaVeDüþ();
    }

    private void ZeminKontrolüYap()
    {
        // Karakterin yerde olup olmadýðýný küçük bir küre aracýlýðýyla kontrol et
        yerdeMi = Physics.CheckSphere(zeminKontrolNoktasý.position, zeminMesafesi, zeminMaskesi);

        // Eðer yerdeysek ve aþaðý doðru düþme hýzý birikmiþse sýfýrla (küçük bir negatif deðerde tut)
        if (yerdeMi && hýzYönü.y < 0)
        {
            hýzYönü.y = -2f;
        }
    }

    private void KameraDöndür()
    {
        // Fare hareket girdilerini al
        float fareX = Input.GetAxis("Mouse X") * fareHassasiyeti * Time.deltaTime;
        float fareY = Input.GetAxis("Mouse Y") * fareHassasiyeti * Time.deltaTime;

        // Yukarý/aþaðý bakýþ açýsýný hesapla ve sýnýrlandýr
        xRotasyonu -= fareY;
        xRotasyonu = Mathf.Clamp(xRotasyonu, -90f, 90f);

        // Kamerayý yukarý/aþaðý döndür ve karakteri saða/sola döndür
        oyuncuKamerasý.localRotation = Quaternion.Euler(xRotasyonu, 0f, 0f);
        transform.Rotate(Vector3.up * fareX);
    }

    private void HareketEt()
    {
        // WASD veya Yön tuþlarý girdilerini al
        float yatayEksen = Input.GetAxis("Horizontal");
        float dikeyEksen = Input.GetAxis("Vertical");

        // Karakterin baktýðý yöne göre hareket vektörünü oluþtur
        Vector3 hareketYönü = transform.right * yatayEksen + transform.forward * dikeyEksen;

        // Hareketi uygula
        karakterKontrolcüsü.Move(hareketYönü * hareketHýzý * Time.deltaTime);
    }

    private void ZýplaVeDüþ()
    {
        // Karakter yerdeyse ve boþluk tuþuna basýlýrsa zýpla
        if (Input.GetButtonDown("Jump") && yerdeMi)
        {
            hýzYönü.y = Mathf.Sqrt(zýplamaGücü * -2f * yerçekimi);
        }

        // Yerçekimini uygula
        hýzYönü.y += yerçekimi * Time.deltaTime;
        karakterKontrolcüsü.Move(hýzYönü * Time.deltaTime);
    }
}