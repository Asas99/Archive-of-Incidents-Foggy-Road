// Gun batimi gokyuzu + gercek bulut fotografi
//
// AMAC: Mevcut Skybox/Procedural gorunumunu (gradyan + gunes diski) korumak,
// uzerine elde bulunan equirect bulut fotografini eklemek.
//
// ANAHTAR FIKIR: Bulut dokusu RENK olarak degil MASKE olarak kullanilir.
// Projedeki bulut fotograflari gri/gece tonunda; dogrudan karistirilirsa
// gun batimi sicakligini griye ceker. Bunun yerine dokunun LUMINANCE'i sadece
// bulut SEKLINI verir, rengi tamamen gun batimi paletinden gelir.
//
// POLARITE (kritik): fde7a0e3...png dosyasinda KOYU bolgeler YOGUN bulut,
// acik bolgeler bulut arasi acikliktir. Bu yuzden _CloudInvert varsayilan 1.
// Acik-bulut / koyu-gokyuzu tipi bir doku kullanilirsa 0 yapilir.
//
// SEVIYE (Levels): fotograflarin luminance araligi dar (~0.25-0.60). Ham haliyle
// esik ayari tutmuyor. _InBlack/_InWhite/_MaskGamma bu dar bandi 0-1'e yayar
// (Photoshop Levels ile ayni matematik).
//
// ZENIT: equirect projeksiyonda dir.y -> 1 iken tum UV satiri tek noktaya
// coker (kutup tekilligi) - doku orada ezilir. Tepede bulut-duzlemi
// projeksiyonuna gecilir, ikisi smoothstep ile harmanlanir.
//
// Gunes yonu sahnenin Directional Light'indan (GetMainLight) okunur.
// Bulut hareketi _WeatherWindVector / _WeatherFogSky global'lerinden gelir;
// bunlari 'Dynamic Weather & Atmosphere' objesi her kare yaziyor.
//
// Maliyet: tek draw call, 4 doku ornegi, birkac dot/pow. Skybox pikselleri
// sadece geometriyle ORTULMEYEN yerlerde calisir (Queue=Background, ZWrite Off).
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
Shader "Environment/Sunset Sky With Clouds"
{
    Properties
    {
        [Header(Bulut maskesi)]
        [NoScaleOffset] _CloudTex("Bulut dokusu (equirect)", 2D) = "black" {}
        _CloudRotation("Bulut donusu", Range(0, 360)) = 0
        _CloudInvert("Maske ters cevir (koyu bulut ise 1)", Range(0, 1)) = 1
        _InBlack("Seviye - siyah giris", Range(0, 1)) = 0.34
        _InWhite("Seviye - beyaz giris", Range(0, 1)) = 0.82
        _MaskGamma("Seviye - gamma", Range(0.2, 3)) = 1.0
        _CloudCoverage("Bulut kaplamasi", Range(0, 1)) = 0.55
        _CloudSoftness("Bulut kenar yumusakligi", Range(0.01, 0.6)) = 0.18
        _DetailBlend("Dagilim (ikinci katman)", Range(0, 1)) = 0.65
        _CloudScroll("Bulut kayma hizi", Range(0, 2)) = 0.35

        [Header(Zenit duzeltmesi)]
        _PlaneCurvature("Tepe egrilik", Range(0.05, 1)) = 0.30
        _PlaneScale("Tepe olcek", Range(0.05, 2)) = 0.55
        _PlaneCenter("Tepe doku merkezi", Vector) = (0.5, 0.72, 0, 0)
        _HorizonFade("Ufukta incelme", Range(0.02, 0.5)) = 0.18

        [Header(Gokyuzu gradyani)]
        [HDR] _ZenithColor("Zenit rengi", Color) = (0.22, 0.26, 0.36, 1)
        [HDR] _HorizonColor("Ufuk rengi", Color) = (0.74, 0.62, 0.52, 1)
        [HDR] _HorizonSunColor("Ufuk - gunes tarafi", Color) = (1.0, 0.62, 0.30, 1)
        _HorizonPower("Ufuk gecis keskinligi", Range(0.5, 8)) = 2.2
        _GroundColor("Yer rengi (ufuk alti)", Color) = (0.10, 0.09, 0.08, 1)

        [Header(Bulut aydinlatmasi)]
        [HDR] _CloudLitColor("Gunese bakan yuz", Color) = (1.0, 0.78, 0.52, 1)
        [HDR] _CloudShadowColor("Golgede kalan yuz", Color) = (0.26, 0.24, 0.30, 1)
        _CloudLitPower("Isima keskinligi", Range(1, 16)) = 3.0
        _CloudThickness("Kalinlik (koyu cekirdek)", Range(0.5, 6)) = 2.0
        _RimIntensity("Kenar parlamasi", Range(0, 3)) = 0.9

        [Header(Teshis)]
        _DebugMask("Maskeyi goster (0 kapali)", Range(0, 1)) = 0

        [Header(Gunes)]
        [HDR] _SunGlowColor("Gunes parilti rengi", Color) = (1.0, 0.66, 0.36, 1)
        _SunGlowPower("Parilti keskinligi", Range(4, 512)) = 48
        _SunGlowIntensity("Parilti siddeti", Range(0, 8)) = 1.4
        _SunDiskSize("Gunes diski boyutu", Range(0.0005, 0.05)) = 0.008
        _SunDiskIntensity("Gunes diski siddeti", Range(0, 30)) = 8

        _Exposure("Pozlama", Range(0, 8)) = 1.3
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);

            float _CloudRotation;
            float _CloudInvert;
            float _InBlack;
            float _InWhite;
            float _MaskGamma;
            float _CloudCoverage;
            float _CloudSoftness;
            float _DetailBlend;
            float _CloudScroll;

            float _PlaneCurvature;
            float _PlaneScale;
            float4 _PlaneCenter;
            float _HorizonFade;
            float _CloudThickness;
            float _RimIntensity;
            float _DebugMask;

            float4 _ZenithColor;
            float4 _HorizonColor;
            float4 _HorizonSunColor;
            float _HorizonPower;
            float4 _GroundColor;

            float4 _CloudLitColor;
            float4 _CloudShadowColor;
            float _CloudLitPower;

            float4 _SunGlowColor;
            float _SunGlowPower;
            float _SunGlowIntensity;
            float _SunDiskSize;
            float _SunDiskIntensity;

            float _Exposure;

            // DynamicWeatherSystem tarafindan yazilan global'ler
            float4 _WeatherWindVector;
            float4 _WeatherFogSky;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            // DynamicStormSkybox.shader:46-54 ile ayni yontem
            float2 DirectionToLatLong(float3 direction, float rotationDeg)
            {
                direction = normalize(direction);
                float2 uv;
                uv.x = atan2(direction.x, direction.z) * (0.5 / PI) + 0.5;
                uv.y = asin(clamp(direction.y, -1.0, 1.0)) / PI + 0.5;
                uv.x = frac(uv.x + rotationDeg / 360.0);
                // Kutup satirinda ornekleme tasmasin
                uv.y = clamp(uv.y, 0.001, 0.999);
                return uv;
            }

            // Tek ornek -> seviye duzeltilmis maske.
            // Polarite _CloudInvert ile secilir, ardindan Photoshop Levels
            // matematigi dar luminance bandini 0-1'e yayar.
            half MaskAt(float2 uv)
            {
                // DIKIS: lat-long'da uv.x, atan2 yuzunden bir piksel icinde
                // 1 -> 0 sicrar. Otomatik mip secimi bu sicramayi "cok buyuk
                // turev" sanip en dusuk mip'e duser; sonuc gokyuzunu bastan
                // asagi bolen dikey bir cizgidir. Turevden tam tur sicramasini
                // cikarip GRAD ile ornekliyoruz. Sicrama yoksa round() sifir
                // dondurur, yani normal pikselde hicbir sey degismez.
                float2 dx = ddx(uv);
                float2 dy = ddy(uv);
                dx.x -= round(dx.x);
                dy.x -= round(dy.x);

                half3 t = SAMPLE_TEXTURE2D_GRAD(_CloudTex, sampler_CloudTex, uv, dx, dy).rgb;
                half l = dot(t, half3(0.2126, 0.7152, 0.0722));

                // KRITIK: proje Linear renk uzayinda (m_ActiveColorSpace: 1) ve
                // doku sRGB isaretli. Ornek linearize gelir: gozle 0.25-0.60 olan
                // fotograf shader'a 0.05-0.30 olarak ulasir. Bu haliyle ters
                // cevirince maske 0.70-0.95'e sikisiyor, Levels sonrasi her yerde
                // 1'e yapisip TUM gokyuzunu duz bir bulut tabakasina cevirir
                // (varyasyon kalmaz, bulut "gorunmez" olur).
                // Fotografin GORUNEN tonlarini geri getiriyoruz; Levels degerleri
                // boylece Photoshop'ta okunan degerlerle ayni anlama gelir.
                l = pow(saturate(l), 0.4545);

                half m = lerp(l, 1.0 - l, _CloudInvert);
                m = saturate((m - _InBlack) / max(_InWhite - _InBlack, 1e-4));
                m = pow(m, 1.0 / max(_MaskGamma, 1e-4));
                return m;
            }

            // Iki katmani CARPARAK birlestirir: tek fotografin tekrar eden
            // silueti kirilir, bulutlar daha parcali dagilir.
            half MixMask(half a, half b)
            {
                return saturate(a * lerp(1.0, saturate(b * 1.5 + 0.25), _DetailBlend));
            }

            // Ikinci katman icin yonu Z ekseni etrafinda ~40 derece cevirir.
            //
            // NEDEN UV'DE DEGIL DE BURADA: ikinci katmani UV uzayinda
            // dondurup olceklemek dikis uretir - uv.x 1 -> 0 sicradiginda
            // dondurulmus koordinat 0.766 kadar kayar ve doku iki yanda
            // hizalanmaz (ekrandaki dikey kesik tam olarak buydu). Yon
            // vektorunu cevirmek kure uzerinde SUREKLIDIR; dikis olusamaz,
            // sadece dokunun kendi eslesmesi baska bir eksene oturur.
            float3 IkinciEksen(float3 d)
            {
                return float3(d.x * 0.766 - d.y * 0.643,
                              d.x * 0.643 + d.y * 0.766,
                              d.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);

                // ---------------------------------------------------------
                // 1) Bulut maskesi
                // ---------------------------------------------------------
                // Ruzgar yonune gore yavas kayma. Global sifirsa (hava sistemi
                // kapali) kayma da sifir olur, bulutlar durur.
                float2 wind = _WeatherWindVector.xz;
                float windLen = length(wind);
                float2 windDir = windLen > 0.0001 ? wind / windLen : float2(1, 0);
                float skyFlow = max(_WeatherFogSky.y, 0.0);

                // HIZ HESABI (varsayilanlarla: _CloudScroll 0.35, skyFlow 0.18)
                //   drift = 0.35 * 0.18 * 0.013 = 8.2e-4 UV/saniye
                //   tam tur (360 derece) = 1 / 8.2e-4 = ~1220 saniye = ~20 dakika
                //   acisal hiz = ~0.30 derece/saniye
                //   gorus alani (~60 derece) gecisi = ~3.5 dakika
                // Gercek bulut hareketi 0.2-0.4 derece/sn bandindadir; bu deger
                // tam ortasina oturur: sakin ama bakinca fark edilir.
                // Not: 0.0012 denendi, tam tur 3.7 SAAT ediyordu - durmus gibiydi.
                float2 drift = windDir * (_Time.y * _CloudScroll * skyFlow * 0.013);

                // Lat-long (ufuk civari dogru) ve bulut-duzlemi (tepede dogru)
                // projeksiyonlari harmanlanir. Kutup tekilligi boyle kapanir.
                float2 uvLL = DirectionToLatLong(dir, _CloudRotation);
                // Merkez ONEMLI: kaydirmasiz uvPL tepede (0,0)'a gider, yani
                // dokunun sol-alt kosesine - orada bulut degil PARLAK sis denizi
                // var. Ters cevrilince maske sifira dusuyor ve tam yukarida
                // "hic bulut yok" oluyordu. Merkez dokunun yogun bulutlu ust
                // bandina tasiniyor.
                float2 uvPL = (dir.xz / (max(dir.y, 0.0) + _PlaneCurvature)) * _PlaneScale
                              + _PlaneCenter.xy;

                // Ikinci katman: lat-long tarafinda YON cevrilir (dikissiz),
                // duzlem tarafinda UV dondurulebilir (uvPL zaten surekli,
                // sicramasi yok - orada dikis riski yoktur).
                float3 dir2 = IkinciEksen(dir);
                float2 uvLL2 = DirectionToLatLong(dir2, _CloudRotation + 137.0);

                float2 p = uvPL - _PlaneCenter.xy;
                float2 uvPL2 = float2(p.x * 0.766 - p.y * 0.643,
                                      p.x * 0.643 + p.y * 0.766) * 0.62
                               + float2(0.37, 0.55);

                // Ikinci katman 0.55x hizda akar -> hafif parallaks
                half maskeLL = MixMask(MaskAt(uvLL  + drift),
                                       MaskAt(uvLL2 + drift * 0.55));
                half maskePL = MixMask(MaskAt(uvPL  + drift),
                                       MaskAt(uvPL2 + drift * 0.55));

                float tepePay = smoothstep(0.55, 0.95, saturate(dir.y));
                half maske = lerp(maskeLL, maskePL, tepePay);

                // Kaplama SABIT 0.5 esigine bias olarak uygulanir; boylece
                // kaplama ve kenar yumusakligi birbirine karismaz.
                half maskeB = saturate(maske + (_CloudCoverage - 0.5));
                half yogunluk = smoothstep(0.5 - _CloudSoftness, 0.5 + _CloudSoftness, maskeB);

                // Ufka dogru dogal incelme (eskiden dir.y*8 ile sert kesiliyordu)
                yogunluk *= smoothstep(0.0, _HorizonFade, dir.y);

                // Teshis: 1 yapilinca ekranda sadece maske (siyah=gokyuzu,
                // beyaz=bulut) gorunur. Ayar yaparken neyin ne oldugunu
                // tahmin etmek yerine dogrudan gostermek icin.
                if (_DebugMask > 0.5)
                    return half4(yogunluk.xxx, 1.0);

                // ---------------------------------------------------------
                // 2) Taban gokyuzu gradyani
                // ---------------------------------------------------------
                Light mainLight = GetMainLight();
                float3 sunDir = -mainLight.direction;   // isigin GELDIGI yon
                float sunDot = dot(dir, sunDir);

                float yukseklik = saturate(dir.y);
                float ufukPay = pow(1.0 - yukseklik, _HorizonPower);

                // Ufkun gunes tarafi isitilir: sadece yatay duzlemdeki hizalanma
                float2 dirXZ = normalize(dir.xz + float2(0.0001, 0.0001));
                float2 sunXZ = normalize(sunDir.xz + float2(0.0001, 0.0001));
                float sunYon = saturate(dot(dirXZ, sunXZ));
                half3 ufukRengi = lerp(_HorizonColor.rgb, _HorizonSunColor.rgb, pow(sunYon, 2.0));

                half3 gokyuzu = lerp(_ZenithColor.rgb, ufukRengi, ufukPay);

                // Ufuk altina bakildiginda yer rengine gec
                gokyuzu = lerp(gokyuzu, _GroundColor.rgb, saturate(-dir.y * 6.0));

                // ---------------------------------------------------------
                // 3) Gunes parilti + disk (bulutlarin ARKASINDA)
                // ---------------------------------------------------------
                half parilti = pow(saturate(sunDot), _SunGlowPower) * _SunGlowIntensity;
                gokyuzu += _SunGlowColor.rgb * parilti;

                half disk = smoothstep(1.0 - _SunDiskSize, 1.0 - _SunDiskSize * 0.35, sunDot);
                gokyuzu += _SunGlowColor.rgb * disk * _SunDiskIntensity;

                // ---------------------------------------------------------
                // 4) Bulut rengi
                //
                // Eskiden renk SADECE gunes yonune bagliydi: tum bulutlar tek
                // duz renk oluyordu. Gunese bakinca gokyuzu de ayni tonda
                // oldugu icin bulut hic secilmiyordu (2. ekran goruntusu).
                //
                // Simdi maskenin KENDI degeri de renge giriyor: ince yerler
                // isigi gecirir (aydinlik), kalin cekirdek koyu kalir. Bulutu
                // "bulut" yapan sey bu ic ton farki.
                // ---------------------------------------------------------
                half isima = pow(saturate(sunDot * 0.5 + 0.5), _CloudLitPower);

                half derinlik = saturate((maskeB - 0.40) * 2.2);              // ic kalinlik
                half gecirgen = pow(1.0 - derinlik, _CloudThickness);         // isik gecirme

                half3 bulutRengi = lerp(_CloudShadowColor.rgb, _CloudLitColor.rgb,
                                        saturate(isima * (0.25 + 0.75 * gecirgen)));

                // Kenar parlamasi (silver lining): sadece bulut kenarindaki
                // gecis bandi ve gunese yakin yonlerde. Silueti belirginlestirir.
                half kenar = smoothstep(0.10, 0.45, yogunluk) *
                             (1.0 - smoothstep(0.45, 0.90, yogunluk));
                bulutRengi += _CloudLitColor.rgb * kenar *
                              pow(saturate(sunDot * 0.5 + 0.5), 6.0) * _RimIntensity;

                // ---------------------------------------------------------
                // 5) Birlestir: bulutlar gokyuzunun ve gunes pariltisinin onunde
                // ---------------------------------------------------------
                half3 sonuc = lerp(gokyuzu, bulutRengi, yogunluk);

                return half4(sonuc * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
