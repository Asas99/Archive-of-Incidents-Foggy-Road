// Gun batimi gokyuzu + PROSEDUREL bulutlar
//
// ------------------------------------------------------------------------
// NEDEN YENIDEN YAZILDI (eski surumun uc yapisal sorunu):
//
//   1) CAPRAZ BANTLAR. Eski surumde ikinci bulut katmani icin yon vektoru
//      Z EKSENI etrafinda 40 derece cevriliyordu (IkinciEksen fonksiyonu).
//      Z ekseni rotasyonu o katmanin "ekvatorunu" egiyor; gokyuzunde bu
//      egik bir bant sistemi demek ve carpim yoluyla tum bulutlara
//      damgalaniyordu. Kullanicinin gordugu capraz gecis buydu.
//
//   2) TEKRAR EDEN SILUET. Tek bir equirect FOTOGRAF maske olarak
//      kullaniliyordu; gokyuzu ayni siluet donup duruyordu, yeni bulut
//      olusmuyor / var olan dagilmiyordu.
//
//   3) RIJIT KAYMA. Tum UV tek parca kaydiriliyordu. Gercek bulutlar
//      kayarken SEKIL DEGISTIRIR.
//
// ------------------------------------------------------------------------
// YENI SISTEM (kaynaklar: Inigo Quilez "Dynamic 2D Clouds",
// Book of Shaders bolum 13 "Fractal Brownian Motion"):
//
//   - Maske PROSEDUREL fbm ile uretilir -> sonsuz cesitlilik, tekrar yok.
//   - Her OKTAV KENDI HIZINDA kayar (IQ): yuksek frekansli oktavlar daha
//     hizli. Bulutlar sadece kaymaz, kayarken evrilir. Gercek atmosferik
//     turbulans da olcege gore farkli hizda gelisir.
//   - DOMAIN WARPING: fbm(p + fbm(p + fbm(p))) -> girdapli, mermerimsi,
//     "random dagilmis" bulut alani. Aranan dagiliklik buradan gelir.
//   - KAPSAMA/KESKINLIK ayri: IQ'nun lineer esigi
//       m = 1/(b-a),  n = -a/(b-a),  sonuc = saturate(fbm*m + n)
//     a = kapsama, b = keskinlik. Birbirine karismaz.
//   - Projeksiyon TEK: bulut duzlemi (dir.xz / (dir.y + c)). Ufka dogru
//     dogal gerilme verir ve SUREKLIDIR - equirect'teki uv.x 1->0 sicramasi
//     olmadigi icin dikis matematiksel olarak olusamaz.
//
//   Fotograf dokusu OPSIYONEL detay katmani olarak korundu:
//   _PhotoDetail = 0 -> tamamen proseduel,  > 0 -> fotograf detayi karisir.
//
// Gunes yonu sahnenin Directional Light'indan (GetMainLight) okunur.
// Ruzgar sahnedeki hava sisteminden (_WeatherWindVector) ya da manuel
// _WindDirection acisindan gelir (_UseSceneWind ile secilir).
//
// Not: yorum ve loglarda Turkce karakter kullanilmiyor (proje kalibi).
Shader "Environment/Sunset Sky With Clouds"
{
    Properties
    {
        [Header(Bulut sekli)]
        _CloudScale("Bulut olcegi", Range(0.2, 8)) = 1.6
        [IntRange] _Octaves("Oktav sayisi (detay)", Range(1, 8)) = 5
        _Lacunarity("Lacunarity (frekans artisi)", Range(1.5, 3.5)) = 2.0
        _Gain("Gain (genlik dususu)", Range(0.2, 0.8)) = 0.5

        [Header(Dagilim  girdap)]
        _WarpStrength("Girdap gucu", Range(0, 2)) = 0.75
        _WarpScale("Girdap olcegi", Range(0.2, 4)) = 1.0

        [Header(Kapsama)]
        _Coverage("Kapsama", Range(0, 1)) = 0.45
        _Sharpness("Kenar keskinligi", Range(0.02, 1)) = 0.35

        [Header(Hareket)]
        [Toggle] _UseSceneWind("Sahne ruzgarini kullan", Float) = 1
        _WindDirection("Ruzgar yonu (derece)", Range(0, 360)) = 45
        _WindSpeed("Ruzgar hizi", Range(0, 6)) = 1.4
        _EvolveSpeed("Sekil degistirme hizi", Range(0, 6)) = 1.0
        _OctaveSpeedBias("Oktav hiz farki", Range(0, 2)) = 0.6

        [Header(Ikinci katman)]
        _Layer2Scale("2. katman olcegi", Range(0.2, 6)) = 2.6
        _Layer2Weight("2. katman agirligi", Range(0, 1)) = 0.45
        _Layer2Speed("2. katman hizi", Range(0, 3)) = 1.6

        [Header(Fotograf detayi  opsiyonel)]
        _PhotoDetail("Fotograf detayi (0 kapali)", Range(0, 1)) = 0
        [NoScaleOffset] _CloudTex("Bulut dokusu (equirect)", 2D) = "black" {}
        _CloudRotation("Doku donusu", Range(0, 360)) = 0
        _CloudInvert("Doku ters cevir", Range(0, 1)) = 1
        _InBlack("Seviye - siyah", Range(0, 1)) = 0.34
        _InWhite("Seviye - beyaz", Range(0, 1)) = 0.82

        [Header(Gorunum)]
        _PlaneCurvature("Tepe egrilik", Range(0.05, 1)) = 0.30
        _HorizonFade("Ufukta incelme", Range(0.02, 0.5)) = 0.18
        _CloudThickness("Kalinlik (koyu cekirdek)", Range(0.5, 6)) = 2.0
        _RimIntensity("Kenar parlamasi", Range(0, 3)) = 0.9

        [Header(Gokyuzu gradyani)]
        [HDR] _ZenithColor("Zenit rengi", Color) = (0.22, 0.26, 0.36, 1)
        [HDR] _HorizonColor("Ufuk rengi", Color) = (0.74, 0.62, 0.52, 1)
        [HDR] _HorizonSunColor("Ufuk - gunes tarafi", Color) = (1.0, 0.62, 0.30, 1)
        _HorizonPower("Ufuk gecis keskinligi", Range(0.5, 8)) = 2.2
        _GroundColor("Yer rengi (ufuk alti)", Color) = (0.10, 0.09, 0.08, 1)

        [Header(Bulut aydinlatmasi)]
        [HDR] _CloudLitColor("Gunese bakan yuz", Color) = (1.0, 0.78, 0.52, 1)
        [HDR] _CloudShadowColor("Golgede kalan yuz", Color) = (0.26, 0.24, 0.30, 1)
        _CloudLitPower("Isima keskinligi", Range(1, 16)) = 3.2

        [Header(Gunes)]
        [HDR] _SunGlowColor("Gunes parilti rengi", Color) = (1.0, 0.66, 0.36, 1)
        _SunGlowPower("Parilti keskinligi", Range(4, 512)) = 48
        _SunGlowIntensity("Parilti siddeti", Range(0, 8)) = 1.4
        _SunDiskSize("Gunes diski boyutu", Range(0.0005, 0.05)) = 0.008
        _SunDiskIntensity("Gunes diski siddeti", Range(0, 30)) = 8

        _Exposure("Pozlama", Range(0, 8)) = 1.3

        [Header(Teshis)]
        _DebugMask("Maskeyi goster (0 kapali)", Range(0, 1)) = 0
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
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);

            float _CloudScale, _Octaves, _Lacunarity, _Gain;
            float _WarpStrength, _WarpScale;
            float _Coverage, _Sharpness;
            float _UseSceneWind, _WindDirection, _WindSpeed, _EvolveSpeed, _OctaveSpeedBias;
            float _Layer2Scale, _Layer2Weight, _Layer2Speed;
            float _PhotoDetail, _CloudRotation, _CloudInvert, _InBlack, _InWhite;
            float _PlaneCurvature, _HorizonFade, _CloudThickness, _RimIntensity;

            float4 _ZenithColor, _HorizonColor, _HorizonSunColor, _GroundColor;
            float _HorizonPower;
            float4 _CloudLitColor, _CloudShadowColor;
            float _CloudLitPower;
            float4 _SunGlowColor;
            float _SunGlowPower, _SunGlowIntensity, _SunDiskSize, _SunDiskIntensity;
            float _Exposure, _DebugMask;

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

            // ==========================================================
            //  Prosedurel gurultu
            // ==========================================================
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            /// Bilinear value noise. Smoothstep ile yumusatilmis - bulut
            /// kenarlarinin organik olmasi icin kare izleri istemiyoruz.
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            /// Fractal Brownian Motion.
            ///
            /// KRITIK NOKTA (Inigo Quilez): her oktav KENDI HIZINDA kayar.
            /// Yuksek frekansli oktavlar daha hizli hareket eder; boylece
            /// bulut kutlesi kayarken ayni zamanda SEKIL DEGISTIRIR.
            /// Tum dokuyu tek parca kaydirmak (eski yontem) bulutlari rijit
            /// bir blok gibi suzulturuyordu.
            float Fbm(float2 p, float2 ruzgar, float zaman, int oktav)
            {
                float toplam = 0.0;
                float genlik = 0.5;
                float frekans = 1.0;
                float norm = 0.0;

                // Sabit ust sinirli dongu + break: dinamik dongu sinirlari
                // bazi platformlarda unroll edilemiyor ve derleyici uyarisi
                // veriyor. 8 sabit sinir her zaman derlenir.
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    if (i >= oktav) break;

                    // Oktav indeksi buyudukce hiz artar (Inigo Quilez):
                    // yuksek frekansli detay, alcak frekansli kutleden daha
                    // hizli hareket eder -> bulut kayarken sekil degistirir.
                    float hiz = 1.0 + i * _OctaveSpeedBias;
                    float2 kayma = ruzgar * zaman * _EvolveSpeed * hiz;

                    toplam += genlik * ValueNoise(p * frekans + kayma);
                    norm += genlik;

                    frekans *= _Lacunarity;
                    genlik *= _Gain;
                }

                return norm > 0.0 ? toplam / norm : 0.0;
            }

            /// Domain warping: fbm(p + fbm(p + fbm(p)))
            /// Duz fbm'i girdapli/mermerimsi hale getirir. Bulutlarin
            /// "random dagilmis" gorunmesini saglayan sey budur.
            float WarpedFbm(float2 p, float2 ruzgar, float zaman)
            {
                int anaOktav = (int)clamp(_Octaves, 1.0, 8.0);

                if (_WarpStrength < 0.001)
                    return Fbm(p, ruzgar, zaman, anaOktav);

                // Warp alani DUSUK oktavla uretilir (3). Girdabi olusturan sey
                // alcak frekansli akis; yuksek frekans eklemek gorsel olarak
                // fark yaratmazken maliyeti iki katina cikarirdi.
                // Tek seviye warp: fbm(p + w * q). Iki seviye (IQ'nun tam
                // formulu) daha zengin ama 5 fbm cagrisi demek; tek seviye
                // 3 cagriyla pratikte ayni dagiligi veriyor.
                float2 q;
                q.x = Fbm(p * _WarpScale, ruzgar, zaman, 3);
                q.y = Fbm(p * _WarpScale + float2(5.2, 1.3), ruzgar, zaman, 3);

                return Fbm(p + _WarpStrength * q, ruzgar, zaman, anaOktav);
            }

            // ==========================================================
            //  Fotograf detayi (opsiyonel)
            // ==========================================================
            float2 DirectionToLatLong(float3 direction, float rotationDeg)
            {
                direction = normalize(direction);
                float2 uv;
                uv.x = atan2(direction.x, direction.z) * (0.5 / PI) + 0.5;
                uv.y = asin(clamp(direction.y, -1.0, 1.0)) / PI + 0.5;
                uv.x = frac(uv.x + rotationDeg / 360.0);
                uv.y = clamp(uv.y, 0.001, 0.999);
                return uv;
            }

            half FotoMaske(float2 uv)
            {
                // Dikis duzeltmesi: uv.x 1 -> 0 sicradiginda otomatik mip
                // secimi en dusuk mip'e duser ve dikey bir serit olusur.
                // Turevden tam tur sicramasini cikariyoruz.
                float2 dx = ddx(uv), dy = ddy(uv);
                dx.x -= round(dx.x);
                dy.x -= round(dy.x);

                half3 t = SAMPLE_TEXTURE2D_GRAD(_CloudTex, sampler_CloudTex, uv, dx, dy).rgb;
                half l = dot(t, half3(0.2126, 0.7152, 0.0722));

                // Proje Linear renk uzayinda; doku sRGB isaretli oldugu icin
                // ornek linearize gelir. Fotografin GORUNEN tonlarini geri getir.
                l = pow(saturate(l), 0.4545);

                half m = lerp(l, 1.0 - l, _CloudInvert);
                return saturate((m - _InBlack) / max(_InWhite - _InBlack, 1e-4));
            }

            // ==========================================================
            half4 Frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);

                // ---------------------------------------------------------
                // 1) Ruzgar
                // ---------------------------------------------------------
                float2 ruzgar;
                if (_UseSceneWind > 0.5)
                {
                    float2 w = _WeatherWindVector.xz;
                    float len = length(w);
                    ruzgar = len > 0.0001 ? w / len : float2(1, 0);

                    // Sahne ruzgari (skyFlowSpeed) 0.18 gibi kucuk bir deger;
                    // dogrudan carpan olarak kullanilirsa bulutlar durur.
                    // Olcekleyip TABAN da biraktik: hava sistemi kapaliyken
                    // bile gokyuzu olu gorunmesin.
                    float akis = max(_WeatherFogSky.y, 0.0);
                    ruzgar *= max(akis * 5.5, 0.8);
                }
                else
                {
                    float a = radians(_WindDirection);
                    ruzgar = float2(cos(a), sin(a));
                }
                ruzgar *= _WindSpeed;

                // HIZ HESABI (varsayilanlar: ruzgar ~1.0, _WindSpeed 1.4,
                // _EvolveSpeed 1.0):
                //   en dusuk oktav : 1 gurultu birimi ~48 saniyede kayar
                //   en yuksek oktav: ~14 saniyede (oktav hiz farki 0.6)
                // Yani kutle sakin suzulurken ince detay gozle gorulur sekilde
                // evriliyor. Onceki katsayi 0.004 idi ve 1 birim 250 saniye
                // aliyordu - pratikte donmus gorunuyordu.
                float zaman = _Time.y * 0.015;

                // ---------------------------------------------------------
                // 2) Bulut duzlemi projeksiyonu
                //
                // dir.xz / (dir.y + c) -> ufka dogru dogal gerilme, ve SUREKLI.
                // Equirect'teki uv.x 1->0 sicramasi olmadigi icin dikis
                // olusamaz; eski surumdeki dikey kesik bu yuzden yok.
                // ---------------------------------------------------------
                float2 uv = (dir.xz / (max(dir.y, 0.0) + _PlaneCurvature)) * _CloudScale;

                // ---------------------------------------------------------
                // 3) Maske
                // ---------------------------------------------------------
                float ana = WarpedFbm(uv, ruzgar, zaman);

                // Ikinci katman: ayni sistem, farkli olcek ve hiz.
                // Offset veriliyor ki ayni deseni tekrar etmesin.
                float ikinci = Fbm(uv * _Layer2Scale + float2(31.4, 17.7),
                                   ruzgar * _Layer2Speed, zaman, 3);

                float ham = lerp(ana, ana * (0.55 + 0.9 * ikinci), _Layer2Weight);

                // Fotograf detayi (opsiyonel)
                if (_PhotoDetail > 0.001)
                {
                    half foto = FotoMaske(DirectionToLatLong(dir, _CloudRotation));
                    ham = lerp(ham, ham * (0.45 + 1.1 * foto), _PhotoDetail);
                }

                // Kapsama / keskinlik - IQ'nun lineer esigi.
                //   a = esigin alt ucu, b = ust ucu
                //   m = 1/(b-a), n = -a/(b-a)
                // Kapsama arttikca esik duser (daha cok bulut), keskinlik
                // gecis bandinin genisligini belirler. Ikisi birbirine karismaz.
                float a = 1.0 - _Coverage;
                float b = a + max(_Sharpness, 0.02);
                float m = 1.0 / max(b - a, 1e-4);
                float n = -a * m;

                half yogunluk = saturate(ham * m + n);

                // Ufka dogru dogal incelme
                yogunluk *= smoothstep(0.0, _HorizonFade, dir.y);

                if (_DebugMask > 0.5)
                    return half4(yogunluk.xxx, 1.0);

                // ---------------------------------------------------------
                // 4) Taban gokyuzu gradyani
                // ---------------------------------------------------------
                Light mainLight = GetMainLight();
                float3 sunDir = -mainLight.direction;   // isigin GELDIGI yon
                float sunDot = dot(dir, sunDir);

                float yukseklik = saturate(dir.y);
                float ufukPay = pow(1.0 - yukseklik, _HorizonPower);

                float2 dirXZ = normalize(dir.xz + float2(0.0001, 0.0001));
                float2 sunXZ = normalize(sunDir.xz + float2(0.0001, 0.0001));
                float sunYon = saturate(dot(dirXZ, sunXZ));
                half3 ufukRengi = lerp(_HorizonColor.rgb, _HorizonSunColor.rgb, pow(sunYon, 2.0));

                half3 gokyuzu = lerp(_ZenithColor.rgb, ufukRengi, ufukPay);
                gokyuzu = lerp(gokyuzu, _GroundColor.rgb, saturate(-dir.y * 6.0));

                // ---------------------------------------------------------
                // 5) Gunes parilti + disk (bulutlarin ARKASINDA)
                // ---------------------------------------------------------
                half parilti = pow(saturate(sunDot), _SunGlowPower) * _SunGlowIntensity;
                gokyuzu += _SunGlowColor.rgb * parilti;

                half disk = smoothstep(1.0 - _SunDiskSize, 1.0 - _SunDiskSize * 0.35, sunDot);
                gokyuzu += _SunGlowColor.rgb * disk * _SunDiskIntensity;

                // ---------------------------------------------------------
                // 6) Bulut rengi
                //
                // Maskenin KENDI degeri renge girer: ince yerler isigi gecirir
                // (aydinlik), kalin cekirdek koyu kalir. Bulutu "bulut" yapan
                // sey bu ic ton farkidir - tek duz renk olursa gokyuzunden
                // ayirt edilemez.
                // ---------------------------------------------------------
                half isima = pow(saturate(sunDot * 0.5 + 0.5), _CloudLitPower);

                half derinlik = saturate((ham - a) * m);                 // ic kalinlik
                half gecirgen = pow(1.0 - derinlik, _CloudThickness);    // isik gecirme

                half3 bulutRengi = lerp(_CloudShadowColor.rgb, _CloudLitColor.rgb,
                                        saturate(isima * (0.25 + 0.75 * gecirgen)));

                // Kenar parlamasi (silver lining): sadece gecis bandinda ve
                // gunese yakin yonlerde. Silueti belirginlestirir.
                half kenar = smoothstep(0.10, 0.45, yogunluk) *
                             (1.0 - smoothstep(0.45, 0.90, yogunluk));
                bulutRengi += _CloudLitColor.rgb * kenar *
                              pow(saturate(sunDot * 0.5 + 0.5), 6.0) * _RimIntensity;

                // ---------------------------------------------------------
                // 7) Birlestir
                // ---------------------------------------------------------
                half3 sonuc = lerp(gokyuzu, bulutRengi, yogunluk);

                return half4(sonuc * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
