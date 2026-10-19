Shader "FoggyRoad/Optimization/Graph/515f8966b1d702f4a8c97ca412a9a4e9/Tree"
{
Properties
{
[NoScaleOffset]_MainTex("Base Map", 2D) = "white" {}
_Color("Color Tint", Color) = (1, 1, 1, 1)
[ToggleUI]_HueVariationKwToggle("Enable Hue Variation", Float) = 1
[ToggleUI]_OldHueVarBehavior("Use Old Hue Variation Behavior", Float) = 0
_HueVariationColor("Hue Variation Color", Color) = (1, 0.5, 0, 0.2)
[ToggleUI]_NormalMapKwToggle("Enable Normal Map", Float) = 1
[Normal][NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
[ToggleUI]EFFECT_EXTRA_TEX("Enable Extra Map", Float) = 1
[NoScaleOffset]_ExtraTex("Smoothness (R), Metallic (G), AO (B)", 2D) = "white" {}
_Glossiness("Smoothness", Range(0, 1)) = 0.5
_AO_Remap("AO Remap", Vector) = (0, 1, 0, 0)
[ToggleUI]_WindQuality("Wind Enabled", Float) = 1
[Toggle(EFFECT_BILLBOARD)]EFFECT_BILLBOARD("Is Billboard", Float) = 0
[KeywordEnum(Flip, Mirror, None)]BACKFACE_NORMAL_MODE("Backface Normal Mode", Float) = 1
[HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
[HideInInspector]_QueueControl("_QueueControl", Float) = -1
[HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
}
SubShader
{
Tags
{
"RenderPipeline"="UniversalPipeline"
"RenderType"="Opaque"
"UniversalMaterialType" = "Lit"
"Queue"="Geometry"
"DisableBatching"="False"
"ShaderGraphShader"="true"
"ShaderGraphTargetId"="UniversalLitSubTarget"
}
Pass
{
    Name "Universal Forward"
    Tags
    {
        "LightMode" = "UniversalForward"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _FORWARD_PLUS
#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_NORMAL_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TANGENT_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_SHADOW_COORD
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_CULLFACE
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_FORWARD
#define _FOG_FRAGMENT 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 staticLightmapUV;
#endif
#endif
#if defined(DYNAMICLIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 dynamicLightmapUV;
#endif
#endif
#if !defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 sh;
#endif
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 probeOcclusion;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 fogFactorAndVertexLight;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 shadowCoord;
#endif
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float FaceSign;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 staticLightmapUV : INTERP0;
#endif
#endif
#if defined(DYNAMICLIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 dynamicLightmapUV : INTERP1;
#endif
#endif
#if !defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 sh : INTERP2;
#endif
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 probeOcclusion : INTERP3;
#endif
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 shadowCoord : INTERP4;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS : INTERP5;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP6;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP7;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 fogFactorAndVertexLight : INTERP8;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_positionWS_NormalWSx : INTERP9;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_normalWS_NormalWSy : INTERP10;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_TangentWS_NormalWSz : INTERP11;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS : INTERP12;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.packed_positionWS_NormalWSx.xyz = input.positionWS;
output.packed_positionWS_NormalWSx.w = input.NormalWS.x;
output.packed_normalWS_NormalWSy.xyz = input.normalWS;
output.packed_normalWS_NormalWSy.w = input.NormalWS.y;
output.packed_TangentWS_NormalWSz.xyz = input.TangentWS;
output.packed_TangentWS_NormalWSz.w = input.NormalWS.z;
output.BitangentWS.xyz = input.BitangentWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.packed_positionWS_NormalWSx.xyz;
output.NormalWS.x = input.packed_positionWS_NormalWSx.w;
output.normalWS = input.packed_normalWS_NormalWSy.xyz;
output.NormalWS.y = input.packed_normalWS_NormalWSy.w;
output.TangentWS = input.packed_TangentWS_NormalWSz.xyz;
output.NormalWS.z = input.packed_TangentWS_NormalWSz.w;
output.BitangentWS = input.BitangentWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Not_float(float In, out float Out)
{
    Out = !In;
}

void Unity_Or_float(float A, float B, out float Out)
{
    Out = A || B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

struct Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float
{
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 WorldSpacePosition;
float FaceSign;
half4 uv0;
};

void SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(float Enable_Normal_Map, UnityTexture2D Normal_Map, float3 Interpolated_Tangent_WS, float3 Interpolated_Bitangent_WS, float3 Interpolated_Normal_WS, float4 Backside_Normal_Transform_TS, float Transform_Backside_Normals, Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float IN, out float3 NormalWS_1, out float3 NormalTS_2)
{
float _Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean = Enable_Normal_Map;
UnityTexture2D _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D = Normal_Map;
float4 _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.tex, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.samplerstate, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4);
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.r;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.g;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.b;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_A_7_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.a;
float3 _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3 = float3(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float);
float _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float;
Unity_DotProduct_float3(_Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float);
float _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean;
Unity_Comparison_Equal_float(_DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float, float(0), _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean);
float3 _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3;
Unity_Branch_float3(_Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean, float3(0, 0, 1), _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3);
float _Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean = Transform_Backside_Normals;
float _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean;
Unity_Not_float(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean);
float _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean = max(0, IN.FaceSign.x);
float _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean;
Unity_Or_float(_Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean, _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean, _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean);
float4 _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4 = Backside_Normal_Transform_TS;
float4 _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4;
Unity_Branch_float4(_Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean, float4(1, 1, 1, 1), _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4, _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4);
float3 _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3, (_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3);
float _Split_d262d084d8a24f75a887851e71acd55c_R_1_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[0];
float _Split_d262d084d8a24f75a887851e71acd55c_G_2_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[1];
float _Split_d262d084d8a24f75a887851e71acd55c_B_3_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[2];
float _Split_d262d084d8a24f75a887851e71acd55c_A_4_Float = 0;
float3 _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3 = Interpolated_Tangent_WS;
float3 _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_R_1_Float.xxx), _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3, _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3);
float3 _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3 = Interpolated_Bitangent_WS;
float3 _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_G_2_Float.xxx), _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3);
float3 _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3;
Unity_Add_float3(_Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3, _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3);
float3 _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3 = Interpolated_Normal_WS;
float3 _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_B_3_Float.xxx), _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3);
float3 _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3;
Unity_Add_float3(_Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3);
float3 _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3 = TransformWorldToTangentDir(_Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3.xyz - IN.WorldSpacePosition, tangentTransform, false);
}
float3 _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3, _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3);
float3 _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3 = TransformTangentToWorldDir(_Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3.xyz, tangentTransform, false).xyz + IN.WorldSpacePosition;
}
float3 _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3;
Unity_Branch_float3(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3);
float3 _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
Unity_Branch_float3(_Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3, _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3);
float3 _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3 = TransformWorldToTangentDir(_Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3.xyz, tangentTransform, false);
}
NormalWS_1 = _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
NormalTS_2 = _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
}

void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
{
    Out = A - B;
}

void Unity_Normalize_float4(float4 In, out float4 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

struct Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float FaceSign;
};

void SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float Vector1_B7478AA2, float3 Vector3_F863C863, float Vector1_2E103E32, float Boolean_1A7045BA, Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float IN, out float Opacity_1, out float3 NormalTangentSpace_2, out float3 NormalWorldSpace_3)
{
float _Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean = Boolean_1A7045BA;
float _IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean = max(0, IN.FaceSign.x);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M0_1_Vector4 = float4(UNITY_MATRIX_M[0].r, UNITY_MATRIX_M[1].r, UNITY_MATRIX_M[2].r, UNITY_MATRIX_M[3].r);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4 = float4(UNITY_MATRIX_M[0].g, UNITY_MATRIX_M[1].g, UNITY_MATRIX_M[2].g, UNITY_MATRIX_M[3].g);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M2_3_Vector4 = float4(UNITY_MATRIX_M[0].b, UNITY_MATRIX_M[1].b, UNITY_MATRIX_M[2].b, UNITY_MATRIX_M[3].b);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4 = float4(UNITY_MATRIX_M[0].a, UNITY_MATRIX_M[1].a, UNITY_MATRIX_M[2].a, UNITY_MATRIX_M[3].a);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M0_1_Vector4 = float4(UNITY_MATRIX_I_V[0].r, UNITY_MATRIX_I_V[1].r, UNITY_MATRIX_I_V[2].r, UNITY_MATRIX_I_V[3].r);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M1_2_Vector4 = float4(UNITY_MATRIX_I_V[0].g, UNITY_MATRIX_I_V[1].g, UNITY_MATRIX_I_V[2].g, UNITY_MATRIX_I_V[3].g);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M2_3_Vector4 = float4(UNITY_MATRIX_I_V[0].b, UNITY_MATRIX_I_V[1].b, UNITY_MATRIX_I_V[2].b, UNITY_MATRIX_I_V[3].b);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4 = float4(UNITY_MATRIX_I_V[0].a, UNITY_MATRIX_I_V[1].a, UNITY_MATRIX_I_V[2].a, UNITY_MATRIX_I_V[3].a);
float4 _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4;
Unity_Subtract_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4, _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4, _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4);
float4 _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4);
float4 _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4;
Unity_Normalize_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4, _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4);
float _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float;
Unity_DotProduct_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float);
float4 _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, (_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float.xxxx), _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4);
float4 _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4;
Unity_Subtract_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4, _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4);
float4 _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4, _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4);
float _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float;
Unity_DotProduct_float3((_Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4.xyz), IN.WorldSpaceNormal, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float);
float _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float);
float _Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float = Vector1_B7478AA2;
float _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float;
Unity_Multiply_float_float(_Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float, 0.0625, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float);
float _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float;
Unity_Subtract_float(_Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float, _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float);
float _Split_dc240df11f72a383b02ff5d20177297b_R_1_Float = IN.ObjectSpaceNormal[0];
float _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float = IN.ObjectSpaceNormal[1];
float _Split_dc240df11f72a383b02ff5d20177297b_B_3_Float = IN.ObjectSpaceNormal[2];
float _Split_dc240df11f72a383b02ff5d20177297b_A_4_Float = 0;
float _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float);
float _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float);
float _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float);
float _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float;
Unity_Lerp_float(_Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float, _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float, _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float);
float _Add_facc917224db51879999a44d20c9e100_Out_2_Float;
Unity_Add_float(_Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float, float(0.05), _Add_facc917224db51879999a44d20c9e100_Out_2_Float);
float _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float;
Unity_Saturate_float(_Add_facc917224db51879999a44d20c9e100_Out_2_Float, _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float);
float _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float = Vector1_2E103E32;
float _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float;
Unity_Multiply_float_float(_Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float);
float _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float;
Unity_Branch_float(_IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float, float(0), _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float);
float _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
Unity_Branch_float(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float);
float3 _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3 = Vector3_F863C863;
float _Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[0];
float _Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[1];
float _Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[2];
float _Split_233978cea2bd558992e45bf1b593c9f6_A_4_Float = 0;
float3 _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float.xxx), IN.WorldSpaceTangent, _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3);
float3 _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float.xxx), IN.WorldSpaceBiTangent, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3);
float3 _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3;
Unity_Add_float3(_Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3, _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3);
float4 _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, float4(-0.1, -0.1, -0.1, -1), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4);
float4 _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4;
Unity_Multiply_float4_float4((_Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float.xxxx), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4, _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4);
float3 _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3;
Unity_Add_float3(_Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3, (_Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4.xyz), _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3);
float3 _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3;
Unity_Normalize_float3(_Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3);
float3 _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3 = TransformWorldToTangentDir(_Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3.xyz, tangentTransform, false);
}
float3 _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3, _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3, _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3);
float3 _Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3 = TransformTangentToWorldDir(_Property_867e845d3b5417839226e67526ede381_Out_0_Vector3.xyz, tangentTransform, false).xyz;
}
float3 _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3;
Unity_Normalize_float3(_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3);
float3 _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3, _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3);
Opacity_1 = _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
NormalTangentSpace_2 = _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
NormalWorldSpace_3 = _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
float3 TangentWS;
float3 BitangentWS;
float3 NormalWS;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
description.TangentWS = IN.WorldSpaceTangent;
description.BitangentWS = IN.WorldSpaceBiTangent;
description.NormalWS = IN.WorldSpaceNormal;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalWS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean = _NormalMapKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BumpMap);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4 = float4(float(-1), float(-1), float(-1), float(1));
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(BACKFACE_NORMAL_MODE_FLIP)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4;
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, -1, 1);
#else
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, 1, 1);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpacePosition = IN.WorldSpacePosition;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.FaceSign = IN.FaceSign;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.uv0 = IN.uv0;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3;
SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(_Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean, _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D, IN.TangentWS, IN.BitangentWS, IN.NormalWS, _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4, 1, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.FaceSign = IN.FaceSign;
float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3;
SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float(8), _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
Unity_NormalStrength_float(_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3, float(2.5), _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean = EFFECT_EXTRA_TEX;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_ExtraTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.tex, _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.samplerstate, _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_R_4_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.r;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_G_5_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.g;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_B_6_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.b;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_A_7_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.a;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_fe447a1f61e3441782dffccf2d3f78c8_Out_0_Float = _Glossiness;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float;
Unity_Branch_float(_Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean, _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_R_4_Float, _Property_fe447a1f61e3441782dffccf2d3f78c8_Out_0_Float, _Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float;
Unity_Remap_float(_Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float, float2 (0, 1), float2 (0, 0.8), _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float = IN.VertexColor[0];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_G_2_Float = IN.VertexColor[1];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_B_3_Float = IN.VertexColor[2];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_A_4_Float = IN.VertexColor[3];
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_B_6_Float, _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float, _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float;
Unity_Branch_float(_Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean, _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float, _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float, _Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float2 _Property_45cdca50a3f0484194361f90edf3ac74_Out_0_Vector2 = _AO_Remap;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float;
Unity_Remap_float(_Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float, float2 (0, 1), _Property_45cdca50a3f0484194361f90edf3ac74_Out_0_Vector2, _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float);
#endif
surface.BaseColor = _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
surface.NormalWS = _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float;
surface.Occlusion = _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 unnormalizedNormalWS = input.normalWS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
const float renormFactor = 1.0 / length(unnormalizedNormalWS);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// use bitangent on the fly like in hdrp
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// This                is explained in section 2.2 in "surface gradient based bump mapping framework"
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent = renormFactor * bitang;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpacePosition = input.positionWS;
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif

#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "GBuffer"
    Tags
    {
        "LightMode" = "UniversalGBuffer"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma multi_compile_instancing
#pragma multi_compile_fog
#pragma instancing_options renderinglayer
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ USE_LEGACY_LIGHTMAPS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_fragment _ DEBUG_DISPLAY
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_NORMAL_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TANGENT_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_SHADOW_COORD
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_CULLFACE
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_GBUFFER
#define _FOG_FRAGMENT 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 staticLightmapUV;
#endif
#endif
#if defined(DYNAMICLIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 dynamicLightmapUV;
#endif
#endif
#if !defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 sh;
#endif
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 probeOcclusion;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 fogFactorAndVertexLight;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 shadowCoord;
#endif
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float FaceSign;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 staticLightmapUV : INTERP0;
#endif
#endif
#if defined(DYNAMICLIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 dynamicLightmapUV : INTERP1;
#endif
#endif
#if !defined(LIGHTMAP_ON)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 sh : INTERP2;
#endif
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 probeOcclusion : INTERP3;
#endif
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 shadowCoord : INTERP4;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS : INTERP5;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP6;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP7;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 fogFactorAndVertexLight : INTERP8;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_positionWS_NormalWSx : INTERP9;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_normalWS_NormalWSy : INTERP10;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_TangentWS_NormalWSz : INTERP11;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS : INTERP12;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.fogFactorAndVertexLight.xyzw = input.fogFactorAndVertexLight;
output.packed_positionWS_NormalWSx.xyz = input.positionWS;
output.packed_positionWS_NormalWSx.w = input.NormalWS.x;
output.packed_normalWS_NormalWSy.xyz = input.normalWS;
output.packed_normalWS_NormalWSy.w = input.NormalWS.y;
output.packed_TangentWS_NormalWSz.xyz = input.TangentWS;
output.packed_TangentWS_NormalWSz.w = input.NormalWS.z;
output.BitangentWS.xyz = input.BitangentWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if defined(LIGHTMAP_ON)
output.staticLightmapUV = input.staticLightmapUV;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
output.dynamicLightmapUV = input.dynamicLightmapUV;
#endif
#if !defined(LIGHTMAP_ON)
output.sh = input.sh;
#endif
#if defined(USE_APV_PROBE_OCCLUSION)
output.probeOcclusion = input.probeOcclusion;
#endif
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
output.shadowCoord = input.shadowCoord;
#endif
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.fogFactorAndVertexLight = input.fogFactorAndVertexLight.xyzw;
output.positionWS = input.packed_positionWS_NormalWSx.xyz;
output.NormalWS.x = input.packed_positionWS_NormalWSx.w;
output.normalWS = input.packed_normalWS_NormalWSy.xyz;
output.NormalWS.y = input.packed_normalWS_NormalWSy.w;
output.TangentWS = input.packed_TangentWS_NormalWSz.xyz;
output.NormalWS.z = input.packed_TangentWS_NormalWSz.w;
output.BitangentWS = input.BitangentWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Not_float(float In, out float Out)
{
    Out = !In;
}

void Unity_Or_float(float A, float B, out float Out)
{
    Out = A || B;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

struct Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float
{
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 WorldSpacePosition;
float FaceSign;
half4 uv0;
};

void SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(float Enable_Normal_Map, UnityTexture2D Normal_Map, float3 Interpolated_Tangent_WS, float3 Interpolated_Bitangent_WS, float3 Interpolated_Normal_WS, float4 Backside_Normal_Transform_TS, float Transform_Backside_Normals, Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float IN, out float3 NormalWS_1, out float3 NormalTS_2)
{
float _Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean = Enable_Normal_Map;
UnityTexture2D _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D = Normal_Map;
float4 _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.tex, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.samplerstate, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4);
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.r;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.g;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.b;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_A_7_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.a;
float3 _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3 = float3(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float);
float _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float;
Unity_DotProduct_float3(_Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float);
float _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean;
Unity_Comparison_Equal_float(_DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float, float(0), _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean);
float3 _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3;
Unity_Branch_float3(_Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean, float3(0, 0, 1), _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3);
float _Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean = Transform_Backside_Normals;
float _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean;
Unity_Not_float(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean);
float _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean = max(0, IN.FaceSign.x);
float _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean;
Unity_Or_float(_Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean, _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean, _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean);
float4 _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4 = Backside_Normal_Transform_TS;
float4 _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4;
Unity_Branch_float4(_Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean, float4(1, 1, 1, 1), _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4, _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4);
float3 _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3, (_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3);
float _Split_d262d084d8a24f75a887851e71acd55c_R_1_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[0];
float _Split_d262d084d8a24f75a887851e71acd55c_G_2_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[1];
float _Split_d262d084d8a24f75a887851e71acd55c_B_3_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[2];
float _Split_d262d084d8a24f75a887851e71acd55c_A_4_Float = 0;
float3 _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3 = Interpolated_Tangent_WS;
float3 _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_R_1_Float.xxx), _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3, _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3);
float3 _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3 = Interpolated_Bitangent_WS;
float3 _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_G_2_Float.xxx), _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3);
float3 _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3;
Unity_Add_float3(_Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3, _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3);
float3 _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3 = Interpolated_Normal_WS;
float3 _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_B_3_Float.xxx), _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3);
float3 _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3;
Unity_Add_float3(_Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3);
float3 _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3 = TransformWorldToTangentDir(_Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3.xyz - IN.WorldSpacePosition, tangentTransform, false);
}
float3 _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3, _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3);
float3 _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3 = TransformTangentToWorldDir(_Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3.xyz, tangentTransform, false).xyz + IN.WorldSpacePosition;
}
float3 _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3;
Unity_Branch_float3(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3);
float3 _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
Unity_Branch_float3(_Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3, _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3);
float3 _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3 = TransformWorldToTangentDir(_Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3.xyz, tangentTransform, false);
}
NormalWS_1 = _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
NormalTS_2 = _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
}

void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
{
    Out = A - B;
}

void Unity_Normalize_float4(float4 In, out float4 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

struct Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float FaceSign;
};

void SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float Vector1_B7478AA2, float3 Vector3_F863C863, float Vector1_2E103E32, float Boolean_1A7045BA, Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float IN, out float Opacity_1, out float3 NormalTangentSpace_2, out float3 NormalWorldSpace_3)
{
float _Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean = Boolean_1A7045BA;
float _IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean = max(0, IN.FaceSign.x);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M0_1_Vector4 = float4(UNITY_MATRIX_M[0].r, UNITY_MATRIX_M[1].r, UNITY_MATRIX_M[2].r, UNITY_MATRIX_M[3].r);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4 = float4(UNITY_MATRIX_M[0].g, UNITY_MATRIX_M[1].g, UNITY_MATRIX_M[2].g, UNITY_MATRIX_M[3].g);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M2_3_Vector4 = float4(UNITY_MATRIX_M[0].b, UNITY_MATRIX_M[1].b, UNITY_MATRIX_M[2].b, UNITY_MATRIX_M[3].b);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4 = float4(UNITY_MATRIX_M[0].a, UNITY_MATRIX_M[1].a, UNITY_MATRIX_M[2].a, UNITY_MATRIX_M[3].a);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M0_1_Vector4 = float4(UNITY_MATRIX_I_V[0].r, UNITY_MATRIX_I_V[1].r, UNITY_MATRIX_I_V[2].r, UNITY_MATRIX_I_V[3].r);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M1_2_Vector4 = float4(UNITY_MATRIX_I_V[0].g, UNITY_MATRIX_I_V[1].g, UNITY_MATRIX_I_V[2].g, UNITY_MATRIX_I_V[3].g);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M2_3_Vector4 = float4(UNITY_MATRIX_I_V[0].b, UNITY_MATRIX_I_V[1].b, UNITY_MATRIX_I_V[2].b, UNITY_MATRIX_I_V[3].b);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4 = float4(UNITY_MATRIX_I_V[0].a, UNITY_MATRIX_I_V[1].a, UNITY_MATRIX_I_V[2].a, UNITY_MATRIX_I_V[3].a);
float4 _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4;
Unity_Subtract_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4, _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4, _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4);
float4 _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4);
float4 _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4;
Unity_Normalize_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4, _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4);
float _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float;
Unity_DotProduct_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float);
float4 _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, (_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float.xxxx), _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4);
float4 _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4;
Unity_Subtract_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4, _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4);
float4 _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4, _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4);
float _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float;
Unity_DotProduct_float3((_Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4.xyz), IN.WorldSpaceNormal, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float);
float _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float);
float _Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float = Vector1_B7478AA2;
float _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float;
Unity_Multiply_float_float(_Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float, 0.0625, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float);
float _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float;
Unity_Subtract_float(_Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float, _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float);
float _Split_dc240df11f72a383b02ff5d20177297b_R_1_Float = IN.ObjectSpaceNormal[0];
float _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float = IN.ObjectSpaceNormal[1];
float _Split_dc240df11f72a383b02ff5d20177297b_B_3_Float = IN.ObjectSpaceNormal[2];
float _Split_dc240df11f72a383b02ff5d20177297b_A_4_Float = 0;
float _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float);
float _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float);
float _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float);
float _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float;
Unity_Lerp_float(_Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float, _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float, _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float);
float _Add_facc917224db51879999a44d20c9e100_Out_2_Float;
Unity_Add_float(_Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float, float(0.05), _Add_facc917224db51879999a44d20c9e100_Out_2_Float);
float _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float;
Unity_Saturate_float(_Add_facc917224db51879999a44d20c9e100_Out_2_Float, _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float);
float _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float = Vector1_2E103E32;
float _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float;
Unity_Multiply_float_float(_Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float);
float _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float;
Unity_Branch_float(_IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float, float(0), _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float);
float _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
Unity_Branch_float(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float);
float3 _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3 = Vector3_F863C863;
float _Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[0];
float _Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[1];
float _Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[2];
float _Split_233978cea2bd558992e45bf1b593c9f6_A_4_Float = 0;
float3 _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float.xxx), IN.WorldSpaceTangent, _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3);
float3 _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float.xxx), IN.WorldSpaceBiTangent, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3);
float3 _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3;
Unity_Add_float3(_Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3, _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3);
float4 _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, float4(-0.1, -0.1, -0.1, -1), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4);
float4 _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4;
Unity_Multiply_float4_float4((_Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float.xxxx), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4, _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4);
float3 _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3;
Unity_Add_float3(_Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3, (_Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4.xyz), _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3);
float3 _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3;
Unity_Normalize_float3(_Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3);
float3 _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3 = TransformWorldToTangentDir(_Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3.xyz, tangentTransform, false);
}
float3 _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3, _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3, _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3);
float3 _Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3 = TransformTangentToWorldDir(_Property_867e845d3b5417839226e67526ede381_Out_0_Vector3.xyz, tangentTransform, false).xyz;
}
float3 _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3;
Unity_Normalize_float3(_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3);
float3 _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3, _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3);
Opacity_1 = _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
NormalTangentSpace_2 = _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
NormalWorldSpace_3 = _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
float3 TangentWS;
float3 BitangentWS;
float3 NormalWS;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
description.TangentWS = IN.WorldSpaceTangent;
description.BitangentWS = IN.WorldSpaceBiTangent;
description.NormalWS = IN.WorldSpaceNormal;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 NormalWS;
float3 Emission;
float Metallic;
float Smoothness;
float Occlusion;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean = _NormalMapKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BumpMap);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4 = float4(float(-1), float(-1), float(-1), float(1));
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(BACKFACE_NORMAL_MODE_FLIP)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4;
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, -1, 1);
#else
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, 1, 1);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpacePosition = IN.WorldSpacePosition;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.FaceSign = IN.FaceSign;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.uv0 = IN.uv0;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3;
SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(_Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean, _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D, IN.TangentWS, IN.BitangentWS, IN.NormalWS, _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4, 1, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.FaceSign = IN.FaceSign;
float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3;
SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float(8), _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
Unity_NormalStrength_float(_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3, float(2.5), _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean = EFFECT_EXTRA_TEX;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_ExtraTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.tex, _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.samplerstate, _Property_8638437af72acc8192bde8413274eb39_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_R_4_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.r;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_G_5_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.g;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_B_6_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.b;
float _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_A_7_Float = _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_RGBA_0_Vector4.a;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_fe447a1f61e3441782dffccf2d3f78c8_Out_0_Float = _Glossiness;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float;
Unity_Branch_float(_Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean, _SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_R_4_Float, _Property_fe447a1f61e3441782dffccf2d3f78c8_Out_0_Float, _Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float;
Unity_Remap_float(_Branch_ae29a8e7441c41a5a4ab51bf4718cb17_Out_3_Float, float2 (0, 1), float2 (0, 0.8), _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float = IN.VertexColor[0];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_G_2_Float = IN.VertexColor[1];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_B_3_Float = IN.VertexColor[2];
float _Split_11016f2bf3d949d3a0be4f8f33b6061b_A_4_Float = IN.VertexColor[3];
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float;
Unity_Multiply_float_float(_SampleTexture2D_7473bd75346b4e8baf63c3982cf74ef9_B_6_Float, _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float, _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float;
Unity_Branch_float(_Property_48279be9b55d436faa7b1ab7b278a351_Out_0_Boolean, _Multiply_f184f4d5595349559fd9aa09763d75dc_Out_2_Float, _Split_11016f2bf3d949d3a0be4f8f33b6061b_R_1_Float, _Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float2 _Property_45cdca50a3f0484194361f90edf3ac74_Out_0_Vector2 = _AO_Remap;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float;
Unity_Remap_float(_Branch_f0a0b7043b014f6eabf500c706784350_Out_3_Float, float2 (0, 1), _Property_45cdca50a3f0484194361f90edf3ac74_Out_0_Vector2, _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float);
#endif
surface.BaseColor = _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
surface.NormalWS = _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
surface.Emission = float3(0, 0, 0);
surface.Metallic = float(0);
surface.Smoothness = _Remap_62ef5d76f1a54d1fad503b98b71db5f2_Out_3_Float;
surface.Occlusion = _Remap_3e096fa54daa4f4588efc48dc8f9c298_Out_3_Float;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 unnormalizedNormalWS = input.normalWS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
const float renormFactor = 1.0 / length(unnormalizedNormalWS);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// use bitangent on the fly like in hdrp
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// This                is explained in section 2.2 in "surface gradient based bump mapping framework"
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent = renormFactor * bitang;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpacePosition = input.positionWS;
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif

#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRGBufferPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ShadowCaster"
    Tags
    {
        "LightMode" = "ShadowCaster"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask 0

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_NORMAL_WS
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_SHADOWCASTER


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalWS;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalWS : INTERP0;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.normalWS.xyz = input.normalWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.normalWS = input.normalWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "MotionVectors"
    Tags
    {
        "LightMode" = "MotionVectors"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask RG

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 3.5
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_MOTION_VECTORS


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthOnly"
    Tags
    {
        "LightMode" = "DepthOnly"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On
ColorMask R

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "DepthNormals"
    Tags
    {
        "LightMode" = "DepthNormals"
    }

// Render State
Cull Back
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma multi_compile_instancing
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_NORMAL_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TANGENT_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_CULLFACE
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHNORMALS


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float FaceSign;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 TangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 NormalWS;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceBiTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentWS : INTERP0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_positionWS_NormalWSx : INTERP3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_normalWS_NormalWSy : INTERP4;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 packed_TangentWS_NormalWSz : INTERP5;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 BitangentWS : INTERP6;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.tangentWS.xyzw = input.tangentWS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.packed_positionWS_NormalWSx.xyz = input.positionWS;
output.packed_positionWS_NormalWSx.w = input.NormalWS.x;
output.packed_normalWS_NormalWSy.xyz = input.normalWS;
output.packed_normalWS_NormalWSy.w = input.NormalWS.y;
output.packed_TangentWS_NormalWSz.xyz = input.TangentWS;
output.packed_TangentWS_NormalWSz.w = input.NormalWS.z;
output.BitangentWS.xyz = input.BitangentWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.tangentWS = input.tangentWS.xyzw;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.packed_positionWS_NormalWSx.xyz;
output.NormalWS.x = input.packed_positionWS_NormalWSx.w;
output.normalWS = input.packed_normalWS_NormalWSy.xyz;
output.NormalWS.y = input.packed_normalWS_NormalWSy.w;
output.TangentWS = input.packed_TangentWS_NormalWSz.xyz;
output.NormalWS.z = input.packed_TangentWS_NormalWSz.w;
output.BitangentWS = input.BitangentWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Not_float(float In, out float Out)
{
    Out = !In;
}

void Unity_Or_float(float A, float B, out float Out)
{
    Out = A || B;
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
{
Out = A * B;
}

void Unity_Add_float3(float3 A, float3 B, out float3 Out)
{
    Out = A + B;
}

struct Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float
{
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float3 WorldSpacePosition;
float FaceSign;
half4 uv0;
};

void SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(float Enable_Normal_Map, UnityTexture2D Normal_Map, float3 Interpolated_Tangent_WS, float3 Interpolated_Bitangent_WS, float3 Interpolated_Normal_WS, float4 Backside_Normal_Transform_TS, float Transform_Backside_Normals, Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float IN, out float3 NormalWS_1, out float3 NormalTS_2)
{
float _Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean = Enable_Normal_Map;
UnityTexture2D _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D = Normal_Map;
float4 _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.tex, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.samplerstate, _Property_fbdf75d73fab4ab19d741189db36eefb_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.rgb = UnpackNormal(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4);
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.r;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.g;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.b;
float _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_A_7_Float = _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_RGBA_0_Vector4.a;
float3 _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3 = float3(_SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_R_4_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_G_5_Float, _SampleTexture2D_7b4a6ad1f8f0484ab1a0fa456cce6f1c_B_6_Float);
float _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float;
Unity_DotProduct_float3(_Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float);
float _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean;
Unity_Comparison_Equal_float(_DotProduct_940efe1fbe1c4282b6ef117cac6dae02_Out_2_Float, float(0), _Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean);
float3 _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3;
Unity_Branch_float3(_Comparison_333d81460a3445b88019a0a6aa388a7e_Out_2_Boolean, float3(0, 0, 1), _Vector3_a3a614da9af04ec39f1170d7b782558e_Out_0_Vector3, _Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3);
float _Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean = Transform_Backside_Normals;
float _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean;
Unity_Not_float(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean);
float _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean = max(0, IN.FaceSign.x);
float _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean;
Unity_Or_float(_Not_e84e7d137aa743dcb781370c69e25974_Out_1_Boolean, _IsFrontFace_dd03de9776a64dfdb64ef4ac2f305ae3_Out_0_Boolean, _Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean);
float4 _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4 = Backside_Normal_Transform_TS;
float4 _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4;
Unity_Branch_float4(_Or_8eb024ae76714797af7b79d1cca40050_Out_2_Boolean, float4(1, 1, 1, 1), _Property_f36e6c8eb7034a9ea61c2e81ad7cde3b_Out_0_Vector4, _Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4);
float3 _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3;
Unity_Multiply_float3_float3(_Branch_a6957c55b3b54f4d9e52907f1ec73825_Out_3_Vector3, (_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3);
float _Split_d262d084d8a24f75a887851e71acd55c_R_1_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[0];
float _Split_d262d084d8a24f75a887851e71acd55c_G_2_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[1];
float _Split_d262d084d8a24f75a887851e71acd55c_B_3_Float = _Multiply_cafe4baf3a264c5f8cf3d57e6b8d567e_Out_2_Vector3[2];
float _Split_d262d084d8a24f75a887851e71acd55c_A_4_Float = 0;
float3 _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3 = Interpolated_Tangent_WS;
float3 _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_R_1_Float.xxx), _Property_e209602fb20c4e6fb5e48428506a55c8_Out_0_Vector3, _Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3);
float3 _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3 = Interpolated_Bitangent_WS;
float3 _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_G_2_Float.xxx), _Property_dfd3caf7bc364c75b883f5ae2931ba9c_Out_0_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3);
float3 _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3;
Unity_Add_float3(_Multiply_025880703d8641548199db0fbf89c334_Out_2_Vector3, _Multiply_ee7cba1ca7b14dd8a840edaa0eaa988a_Out_2_Vector3, _Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3);
float3 _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3 = Interpolated_Normal_WS;
float3 _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_d262d084d8a24f75a887851e71acd55c_B_3_Float.xxx), _Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3);
float3 _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3;
Unity_Add_float3(_Add_1f0bd77dcc3845f694b4d6da8e4290a3_Out_2_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3);
float3 _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3 = TransformWorldToTangentDir(_Property_f23ce603413944bf9c093c0e930630c4_Out_0_Vector3.xyz - IN.WorldSpacePosition, tangentTransform, false);
}
float3 _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3;
Unity_Multiply_float3_float3((_Branch_54a3c1c6f0844592892b32f554e5ee8d_Out_3_Vector4.xyz), _Transform_6d8ec6000434465581f96711a08f7b9a_Out_1_Vector3, _Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3);
float3 _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3 = TransformTangentToWorldDir(_Multiply_0f4038e8d88c497a9848d40cea1db0a8_Out_2_Vector3.xyz, tangentTransform, false).xyz + IN.WorldSpacePosition;
}
float3 _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3;
Unity_Branch_float3(_Property_3aa3f350a9d14244a22ec09fbc3190d2_Out_0_Boolean, _Transform_8daf08c14e6f406abaafb4a5f390b4a3_Out_1_Vector3, _Multiply_4b7bc34646b04cb7807c181ddfe5eac9_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3);
float3 _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
Unity_Branch_float3(_Property_29c30d95951e4d9a9741deb69d673713_Out_0_Boolean, _Add_a78b467401cb4f2c90ea5876534cb2eb_Out_2_Vector3, _Branch_554037657d664172aaa8465f199bbeb1_Out_3_Vector3, _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3);
float3 _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3 = TransformWorldToTangentDir(_Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3.xyz, tangentTransform, false);
}
NormalWS_1 = _Branch_ad59d8af04f34879a7db1f47ac21d918_Out_3_Vector3;
NormalTS_2 = _Transform_2c7b5b9152be452e9b7e932fe1aac767_Out_1_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

void Unity_Subtract_float4(float4 A, float4 B, out float4 Out)
{
    Out = A - B;
}

void Unity_Normalize_float4(float4 In, out float4 Out)
{
    Out = normalize(In);
}

void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
{
    Out = dot(A, B);
}

void Unity_Subtract_float(float A, float B, out float Out)
{
    Out = A - B;
}

void Unity_Lerp_float(float A, float B, float T, out float Out)
{
    Out = lerp(A, B, T);
}

void Unity_Normalize_float3(float3 In, out float3 Out)
{
    Out = normalize(In);
}

struct Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float
{
float3 ObjectSpaceNormal;
float3 WorldSpaceNormal;
float3 WorldSpaceTangent;
float3 WorldSpaceBiTangent;
float FaceSign;
};

void SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float Vector1_B7478AA2, float3 Vector3_F863C863, float Vector1_2E103E32, float Boolean_1A7045BA, Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float IN, out float Opacity_1, out float3 NormalTangentSpace_2, out float3 NormalWorldSpace_3)
{
float _Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean = Boolean_1A7045BA;
float _IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean = max(0, IN.FaceSign.x);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M0_1_Vector4 = float4(UNITY_MATRIX_M[0].r, UNITY_MATRIX_M[1].r, UNITY_MATRIX_M[2].r, UNITY_MATRIX_M[3].r);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4 = float4(UNITY_MATRIX_M[0].g, UNITY_MATRIX_M[1].g, UNITY_MATRIX_M[2].g, UNITY_MATRIX_M[3].g);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M2_3_Vector4 = float4(UNITY_MATRIX_M[0].b, UNITY_MATRIX_M[1].b, UNITY_MATRIX_M[2].b, UNITY_MATRIX_M[3].b);
float4 _MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4 = float4(UNITY_MATRIX_M[0].a, UNITY_MATRIX_M[1].a, UNITY_MATRIX_M[2].a, UNITY_MATRIX_M[3].a);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M0_1_Vector4 = float4(UNITY_MATRIX_I_V[0].r, UNITY_MATRIX_I_V[1].r, UNITY_MATRIX_I_V[2].r, UNITY_MATRIX_I_V[3].r);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M1_2_Vector4 = float4(UNITY_MATRIX_I_V[0].g, UNITY_MATRIX_I_V[1].g, UNITY_MATRIX_I_V[2].g, UNITY_MATRIX_I_V[3].g);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M2_3_Vector4 = float4(UNITY_MATRIX_I_V[0].b, UNITY_MATRIX_I_V[1].b, UNITY_MATRIX_I_V[2].b, UNITY_MATRIX_I_V[3].b);
float4 _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4 = float4(UNITY_MATRIX_I_V[0].a, UNITY_MATRIX_I_V[1].a, UNITY_MATRIX_I_V[2].a, UNITY_MATRIX_I_V[3].a);
float4 _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4;
Unity_Subtract_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M3_4_Vector4, _MatrixSplit_b5ca2b6820cdfd88a8b5b10ba4800a5d_M3_4_Vector4, _Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4);
float4 _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_67a925d79578ce8590cf7f7b77108ac0_Out_2_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4);
float4 _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4;
Unity_Normalize_float4(_MatrixSplit_95c7f4ab32b7cc8d89b7189865e59607_M1_2_Vector4, _Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4);
float _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float;
Unity_DotProduct_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, _Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float);
float4 _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_4a5b184b2306478e828043b7f9812776_Out_1_Vector4, (_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float.xxxx), _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4);
float4 _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4;
Unity_Subtract_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, _Multiply_4a2d92fc54bc9b8a982f507f917ebe40_Out_2_Vector4, _Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4);
float4 _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4;
Unity_Normalize_float4(_Subtract_3d078cab7011858dac9c9fe195b7cb5d_Out_2_Vector4, _Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4);
float _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float;
Unity_DotProduct_float3((_Normalize_f7abcf79af5ff786abc573767d618257_Out_1_Vector4.xyz), IN.WorldSpaceNormal, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float);
float _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _DotProduct_7c678a8fda53b788be93aba8643749c9_Out_2_Float, _Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float);
float _Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float = Vector1_B7478AA2;
float _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float;
Unity_Multiply_float_float(_Property_ddc55060fabbba869d0b4b4c35dc2494_Out_0_Float, 0.0625, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float);
float _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float;
Unity_Subtract_float(_Multiply_c45cc1b14f86ee8790229fdf0f6fcb9f_Out_2_Float, _Multiply_e54d0b2fea4c3f80ad2d9ba0f851f8df_Out_2_Float, _Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float);
float _Split_dc240df11f72a383b02ff5d20177297b_R_1_Float = IN.ObjectSpaceNormal[0];
float _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float = IN.ObjectSpaceNormal[1];
float _Split_dc240df11f72a383b02ff5d20177297b_B_3_Float = IN.ObjectSpaceNormal[2];
float _Split_dc240df11f72a383b02ff5d20177297b_A_4_Float = 0;
float _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float;
Unity_Multiply_float_float(_DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _DotProduct_96c37779ec605f87934e893b7e7257e1_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float);
float _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float;
Unity_Multiply_float_float(_Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_ecb14e669f2fb387ab8e42809dc7ba94_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float);
float _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float;
Unity_Multiply_float_float(_Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_4286e2b453a91a8c9a14f6a3f6251ebf_Out_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float);
float _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float;
Unity_Lerp_float(_Subtract_f46345177c8dad87be0692ca59fa318b_Out_2_Float, _Split_dc240df11f72a383b02ff5d20177297b_G_2_Float, _Multiply_7fead86f9c9e9f85b863feb74a533bd0_Out_2_Float, _Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float);
float _Add_facc917224db51879999a44d20c9e100_Out_2_Float;
Unity_Add_float(_Lerp_bce4d28c74de4a80acb24d83ed41dc4e_Out_3_Float, float(0.05), _Add_facc917224db51879999a44d20c9e100_Out_2_Float);
float _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float;
Unity_Saturate_float(_Add_facc917224db51879999a44d20c9e100_Out_2_Float, _Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float);
float _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float = Vector1_2E103E32;
float _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float;
Unity_Multiply_float_float(_Saturate_a0518b5e10acfb899da41b59dce8ea65_Out_1_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float);
float _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float;
Unity_Branch_float(_IsFrontFace_583b4a2805ca49aaa3bae43b578b7c1c_Out_0_Boolean, _Multiply_331b16e954bf0581b010707e674b8b89_Out_2_Float, float(0), _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float);
float _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
Unity_Branch_float(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Branch_05b32228d67049288f0fb240f92ffe97_Out_3_Float, _Property_bed41e99c0d25d8d80a011cfb9f77cb2_Out_0_Float, _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float);
float3 _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3 = Vector3_F863C863;
float _Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[0];
float _Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[1];
float _Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float = _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3[2];
float _Split_233978cea2bd558992e45bf1b593c9f6_A_4_Float = 0;
float3 _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_R_1_Float.xxx), IN.WorldSpaceTangent, _Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3);
float3 _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3;
Unity_Multiply_float3_float3((_Split_233978cea2bd558992e45bf1b593c9f6_G_2_Float.xxx), IN.WorldSpaceBiTangent, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3);
float3 _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3;
Unity_Add_float3(_Multiply_d7ee41bacbb8ee8aa78031fd9f647d58_Out_2_Vector3, _Multiply_7abe0b7030aca58baee7e200f6eb20ef_Out_2_Vector3, _Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3);
float4 _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4;
Unity_Multiply_float4_float4(_Normalize_6525e04726dcf3868e7af7f77ed898e2_Out_1_Vector4, float4(-0.1, -0.1, -0.1, -1), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4);
float4 _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4;
Unity_Multiply_float4_float4((_Split_233978cea2bd558992e45bf1b593c9f6_B_3_Float.xxxx), _Multiply_91f2ddb3a241708caa02e46c0911b10d_Out_2_Vector4, _Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4);
float3 _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3;
Unity_Add_float3(_Add_49731b94e4e73b838fcf4621ed12b3c5_Out_2_Vector3, (_Multiply_4bce3cf9895f828298a1d1c5c9c69f90_Out_2_Vector4.xyz), _Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3);
float3 _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3;
Unity_Normalize_float3(_Add_4ce1e742d4d23589b657483bfb4ca6e8_Out_2_Vector3, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3);
float3 _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3 = TransformWorldToTangentDir(_Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3.xyz, tangentTransform, false);
}
float3 _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Transform_77d1caddbcfb888d91c90ef23934b7d7_Out_1_Vector3, _Property_867e845d3b5417839226e67526ede381_Out_0_Vector3, _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3);
float3 _Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3;
{
float3x3 tangentTransform = float3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3 = TransformTangentToWorldDir(_Property_867e845d3b5417839226e67526ede381_Out_0_Vector3.xyz, tangentTransform, false).xyz;
}
float3 _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3;
Unity_Normalize_float3(_Transform_3a5f67d6e979e88c908d521e864c0a7e_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3);
float3 _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
Unity_Branch_float3(_Property_c04b1e419210bd839c4dc8ee87e0bf76_Out_0_Boolean, _Normalize_f1903518923e9f818051d767f5bb83a6_Out_1_Vector3, _Normalize_80563966540b40a5977d8380b43098f3_Out_1_Vector3, _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3);
Opacity_1 = _Branch_871bc73529f6e28d908cd14c73bd2500_Out_3_Float;
NormalTangentSpace_2 = _Branch_dc6c2dbf5c5d4784ae385be26492978c_Out_3_Vector3;
NormalWorldSpace_3 = _Branch_323699bda0267a8a8444faff960f0c7d_Out_3_Vector3;
}

void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
float3 TangentWS;
float3 BitangentWS;
float3 NormalWS;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
description.TangentWS = IN.WorldSpaceTangent;
description.BitangentWS = IN.WorldSpaceBiTangent;
description.NormalWS = IN.WorldSpaceNormal;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 NormalWS;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean = _NormalMapKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_BumpMap);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4 = float4(float(-1), float(-1), float(-1), float(1));
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(BACKFACE_NORMAL_MODE_FLIP)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = _Vector4_4ac15dfdb58f4e61a8ac3884f9362747_Out_0_Vector4;
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, -1, 1);
#else
float4 _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4 = float4(1, 1, 1, 1);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.WorldSpacePosition = IN.WorldSpacePosition;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.FaceSign = IN.FaceSign;
_SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2.uv0 = IN.uv0;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3;
float3 _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3;
SG_SpeedTree8InterpolatedNormals_b04441f4a83ad224d92d547d170c366b_float(_Property_131865e462904bcf9e759c793cbe3bae_Out_0_Boolean, _Property_b5c30ef39afc548fbcb9d8a6de4be9bb_Out_0_Texture2D, IN.TangentWS, IN.BitangentWS, IN.NormalWS, _BackfaceNormalMode_4d39237bcc0a4a739e43d00dd5e365b1_Out_0_Vector4, 1, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalWS_1_Vector3, _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceNormal = IN.WorldSpaceNormal;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceTangent = IN.WorldSpaceTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.WorldSpaceBiTangent = IN.WorldSpaceBiTangent;
_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2.FaceSign = IN.FaceSign;
float _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3;
float3 _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3;
SG_SpeedTree8Billboard_89c32740b629abb41bf9b65e3a64c373_float(float(8), _SpeedTree8InterpolatedNormals_ed9a4e01eae347baa5a20ead6bc235a2_NormalTS_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_Opacity_1_Float, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalTangentSpace_2_Vector3, _SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
Unity_NormalStrength_float(_SpeedTree8Billboard_66414e3b15df728d844ff42cba2cbac2_NormalWorldSpace_3_Vector3, float(2.5), _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3);
#endif
surface.NormalWS = _NormalStrength_c3dbe229c65a46878b501ce189ac2dce_Out_2_Vector3;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    output.TangentWS = input.TangentWS;
output.BitangentWS = input.BitangentWS;
output.NormalWS = input.NormalWS;

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 unnormalizedNormalWS = input.normalWS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
const float renormFactor = 1.0 / length(unnormalizedNormalWS);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// use bitangent on the fly like in hdrp
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal = normalize(mul(output.WorldSpaceNormal, (float3x3) UNITY_MATRIX_M));           // transposed multiplication by inverse matrix to handle normal scale
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
// This                is explained in section 2.2 in "surface gradient based bump mapping framework"
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceBiTangent = renormFactor * bitang;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpacePosition = input.positionWS;
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif

#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Meta"
    Tags
    {
        "LightMode" = "Meta"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
#pragma shader_feature _ EDITOR_VISUALIZATION
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_INSTANCEID
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_META
#define _FOG_FRAGMENT 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord1 : INTERP1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord2 : INTERP2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS : INTERP4;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.texCoord1.xyzw = input.texCoord1;
output.texCoord2.xyzw = input.texCoord2;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.texCoord1 = input.texCoord1.xyzw;
output.texCoord2 = input.texCoord2.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
float3 Emission;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
surface.BaseColor = _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
surface.Emission = float3(0, 0, 0);
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/LightingMetaPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "SceneSelectionPass"
    Tags
    {
        "LightMode" = "SceneSelectionPass"
    }

// Render State
Cull Off

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENESELECTIONPASS 1
#define ALPHA_CLIP_THRESHOLD 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    






    #if UNITY_UV_STARTS_AT_TOP
    #else
    #endif


#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "ScenePickingPass"
    Tags
    {
        "LightMode" = "Picking"
    }

// Render State
Cull Back

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_DEPTHONLY
#define SCENEPICKINGPASS 1
#define ALPHA_CLIP_THRESHOLD 1


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS : INTERP2;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
surface.BaseColor = _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/SelectionPickingPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
Pass
{
    Name "Universal 2D"
    Tags
    {
        "LightMode" = "Universal2D"
    }

// Render State
Cull Back
Blend One Zero
ZTest LEqual
ZWrite On

// Debug
// <None>

// --------------------------------------------------
// Pass

HLSLPROGRAM

// Pragmas
#pragma target 2.0
#pragma vertex vert
#pragma fragment frag

// Keywords
// PassKeywords: <None>
#pragma shader_feature_local _ EFFECT_BILLBOARD
#pragma shader_feature_local BACKFACE_NORMAL_MODE_FLIP BACKFACE_NORMAL_MODE_MIRROR BACKFACE_NORMAL_MODE_NONE

#if defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_0
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_1
#elif defined(EFFECT_BILLBOARD) && defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_2
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_3
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_4
#elif defined(EFFECT_BILLBOARD) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_5
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_6
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_7
#elif defined(_LOD_FADE_CROSSFADE) && defined(BACKFACE_NORMAL_MODE_NONE)
#define KEYWORD_PERMUTATION_8
#elif defined(BACKFACE_NORMAL_MODE_FLIP)
#define KEYWORD_PERMUTATION_9
#elif defined(BACKFACE_NORMAL_MODE_MIRROR)
#define KEYWORD_PERMUTATION_10
#else
#define KEYWORD_PERMUTATION_11
#endif


// Defines

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMALMAP 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define _NORMAL_DROPOFF_WS 1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_NORMAL
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TANGENT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD1
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define ATTRIBUTES_NEED_COLOR
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_POSITION_WS
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_TEXCOORD0
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#define VARYINGS_NEED_COLOR
#endif

#define FEATURES_GRAPH_VERTEX
/* WARNING: $splice Could not find named fragment 'PassInstancing' */
#define SHADERPASS SHADERPASS_2D


// custom interpolator pre-include
/* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

// --------------------------------------------------
// Structs and Packing

// custom interpolators pre packing
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */

struct Attributes
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionOS : POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 normalOS : NORMAL;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 tangentOS : TANGENT;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0 : TEXCOORD0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1 : TEXCOORD1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2 : TEXCOORD2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3 : TEXCOORD3;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : COLOR;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : INSTANCEID_SEMANTIC;
#endif
#endif
};
struct Varyings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};
struct SurfaceDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 WorldSpaceViewDirection;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 NDCPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float2 PixelPosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 VertexColor;
#endif
};
struct VertexDescriptionInputs
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceNormal;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpaceTangent;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 ObjectSpacePosition;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv2;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 uv3;
#endif
};
struct PackedVaryings
{
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 positionCS : SV_POSITION;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 texCoord0 : INTERP0;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float4 color : INTERP1;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 float3 positionWS : INTERP2;
#endif
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint instanceID : CUSTOM_INSTANCE_ID;
#endif
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
#endif
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
#endif
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
 FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
#endif
};

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
PackedVaryings PackVaryings (Varyings input)
{
PackedVaryings output;
ZERO_INITIALIZE(PackedVaryings, output);
output.positionCS = input.positionCS;
output.texCoord0.xyzw = input.texCoord0;
output.color.xyzw = input.color;
output.positionWS.xyz = input.positionWS;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}

Varyings UnpackVaryings (PackedVaryings input)
{
Varyings output;
output.positionCS = input.positionCS;
output.texCoord0 = input.texCoord0.xyzw;
output.color = input.color.xyzw;
output.positionWS = input.positionWS.xyz;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
output.instanceID = input.instanceID;
#endif
#if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
#endif
#if (defined(UNITY_STEREO_INSTANCING_ENABLED))
output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
output.cullFace = input.cullFace;
#endif
return output;
}
#endif

// --------------------------------------------------
// Graph

// Graph Properties
CBUFFER_START(UnityPerMaterial)
float4 _MainTex_TexelSize;
float4 _Color;
float _HueVariationKwToggle;
float _OldHueVarBehavior;
float4 _HueVariationColor;
float _NormalMapKwToggle;
float4 _BumpMap_TexelSize;
float EFFECT_EXTRA_TEX;
float4 _ExtraTex_TexelSize;
float _Glossiness;
float _WindQuality;
float2 _AO_Remap;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END


// Object and Global properties
SAMPLER(SamplerState_Linear_Repeat);
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_ExtraTex);
SAMPLER(sampler_ExtraTex);

// Graph Includes
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTreeCommon.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/Nature/SpeedTree8Wind.hlsl"
#include_with_pragmas "Packages/com.unity.shadergraph/ShaderGraphLibrary/LODDitheringTransition.hlsl"

// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif

// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif

// Graph Functions

void Unity_Comparison_Equal_float(float A, float B, out float Out)
{
    Out = A == B ? 1 : 0;
}

struct Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float
{
float3 ObjectSpacePosition;
half4 uv2;
};

void SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(float3 _ObjectSpacePosition, float Is_Billboard, Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float IN, out float3 ObjectSpacePositionOut_1)
{
float4 _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4 = IN.uv2;
float _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean = Is_Billboard;
float3 _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
SpeedTreeLODTransition_float(IN.ObjectSpacePosition, _UV_f4a1125310d54961a6958ee8ff2c4f7c_Out_0_Vector4, _Property_98e96a1ad39749298b605f999db7dbe2_Out_0_Boolean, _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3);
ObjectSpacePositionOut_1 = _SpeedTreeLODTransitionCustomFunction_c0cddf2ae9c6488e89f3ce0712ca5e55_ObjectSpacePositionOut_4_Vector3;
}

void Unity_Branch_float(float Predicate, float True, float False, out float Out)
{
    Out = Predicate ? True : False;
}

void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
{
    Out = A - B;
}

struct Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float
{
float3 ObjectSpaceNormal;
half4 uv0;
half4 uv1;
half4 uv2;
half4 uv3;
};

void SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(float3 _ObjectSpacePosition, float Vector1_C2E02832, float Boolean_DCF9EE01, Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float IN, out float3 AnimatedVertexObjectSpacePosition_1, out float3 ObjectSpaceMotionVector_2)
{
float3 _Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3 = _ObjectSpacePosition;
float4 _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4 = IN.uv0;
float4 _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4 = IN.uv1;
float4 _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4 = IN.uv2;
float4 _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4 = IN.uv3;
float _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float = Vector1_C2E02832;
float _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean = Boolean_DCF9EE01;
float3 _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 0, _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3);
float3 _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3;
SpeedTreeWind_float(_Property_dd3b56d268ef49848fa7ea5e3d644838_Out_0_Vector3, IN.ObjectSpaceNormal, _UV_9586e5a1d6fb6382907ce7dd6245e18a_Out_0_Vector4, _UV_ccafab26e74d6e84b38b1ba76ec10590_Out_0_Vector4, _UV_0e834b0ecae37b80815c131f5e4576fb_Out_0_Vector4, _UV_64599243973fef8391cbf70583bd5624_Out_0_Vector4, _Property_77aa19db1b78d68d867b66ccec3069e3_Out_0_Float, _Property_78c7a7f38324c58e86950b170a523db6_Out_0_Boolean, 1, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3);
float3 _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
Unity_Subtract_float3(_SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3, _SpeedTreeWindCustomFunction_ea9204275f494656bceb7d02427a9379_newPosition_4_Vector3, _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3);
AnimatedVertexObjectSpacePosition_1 = _SpeedTreeWindCustomFunction_e8db48382e03a3829c390786fc0fa96f_newPosition_4_Vector3;
ObjectSpaceMotionVector_2 = _Subtract_4457e4221c3e40ed864992d9fd451d30_Out_2_Vector3;
}

void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
{
Out = A * B;
}

void Unity_Add_float(float A, float B, out float Out)
{
    Out = A + B;
}

void Unity_Fraction_float(float In, out float Out)
{
    Out = frac(In);
}

void Unity_Multiply_float_float(float A, float B, out float Out)
{
Out = A * B;
}

void Unity_Saturate_float(float In, out float Out)
{
    Out = saturate(In);
}

void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, out float4 Out, float Opacity)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Branch_float4(float Predicate, float4 True, float4 False, out float4 Out)
{
    Out = Predicate ? True : False;
}

void Unity_Maximum_float(float A, float B, out float Out)
{
    Out = max(A, B);
}

void Unity_Divide_float(float A, float B, out float Out)
{
    Out = A / B;
}

void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}

struct Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float
{
float3 WorldSpaceViewDirection;
float4 VertexColor;
float2 NDCPosition;
half4 uv0;
};

void SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(UnityTexture2D Base_Map, float4 Color_Tint, float Enable_Hue_Variation, float4 Hue_Variation_Color, float Use_Old_Hue_Variation_Behavior, float Is_Billboard, float Crossfade, Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float IN, out float3 Modified_Color_1, out float Modified_Alpha_4, out float3 Original_Color_2, out float Original_Alpha_3)
{
float _Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean = Enable_Hue_Variation;
float _Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean = Use_Old_Hue_Variation_Behavior;
float4 _Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4 = Color_Tint;
UnityTexture2D _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D = Base_Map;
float4 _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4 = SAMPLE_TEXTURE2D(_Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.tex, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.samplerstate, _Property_9739be6f931c48a2ab08fb60abd88eac_Out_0_Texture2D.GetTransformedUV(IN.uv0.xy) );
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_R_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.r;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_G_5_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.g;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_B_6_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.b;
float _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_A_7_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.a;
float4 _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4;
Unity_Multiply_float4_float4(_Property_3447ed3cbe7e4c0ca03d34219340dbda_Out_0_Vector4, _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4);
float4 _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4 = Hue_Variation_Color;
float _Split_1e71fee3241d42eea8e7ee1371975d5c_R_1_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[0];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_G_2_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[1];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_B_3_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[2];
float _Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float = _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4[3];
float3 _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3;
{
// Converting Position from Object to AbsoluteWorld via world space
float3 world;
world = TransformObjectToWorld(float3 (0, 0, 0).xyz);
_Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3 = GetAbsolutePositionWS(world);
}
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[0];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[1];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float = _Transform_16910a20060c43f889cace1d074fd2ca_Out_1_Vector3[2];
float _Split_afb69a2ae3904da0b6b18f0a8fed3415_A_4_Float = 0;
float _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float;
Unity_Add_float(_Split_afb69a2ae3904da0b6b18f0a8fed3415_R_1_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_G_2_Float, _Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float);
float _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float;
Unity_Add_float(_Add_0bc75546351b442e9e40b2e2e104d829_Out_2_Float, _Split_afb69a2ae3904da0b6b18f0a8fed3415_B_3_Float, _Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float);
float _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float;
Unity_Fraction_float(_Add_de0b04c2427447f38fb590f8c1b2d313_Out_2_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float);
float _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float;
Unity_Multiply_float_float(_Split_1e71fee3241d42eea8e7ee1371975d5c_A_4_Float, _Fraction_740ba08223a24b0d9fcea47fcbe15b39_Out_1_Float, _Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float);
float _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float;
Unity_Saturate_float(_Multiply_7316ba902de94fdca1789a790def91ff_Out_2_Float, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4;
Unity_Lerp_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, (_Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float.xxxx), _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4);
float4 _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4;
Unity_Blend_Overlay_float4(_Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Property_44dfa3cd977d45adb4d44efa3fae33be_Out_0_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Saturate_2692c4746b544ace91301f7dc416c003_Out_1_Float);
float4 _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4;
Unity_Branch_float4(_Property_80c510042dc848db99c93f2d10c93a45_Out_0_Boolean, _Lerp_3f3e8e2911e4473b9baaded23518dc75_Out_3_Vector4, _Blend_de4f34f2bb7a46769d53aa730fdbebda_Out_2_Vector4, _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4);
float _Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[0];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[1];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[2];
float _Split_8b255101be0e4c0686ecfb357b4e08c6_A_4_Float = _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4[3];
float _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float;
Unity_Maximum_float(_Split_8b255101be0e4c0686ecfb357b4e08c6_R_1_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_G_2_Float, _Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float);
float _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float;
Unity_Maximum_float(_Maximum_172119ade14f4f779d4ae5d9d20db683_Out_2_Float, _Split_8b255101be0e4c0686ecfb357b4e08c6_B_3_Float, _Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float);
float _Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[0];
float _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[1];
float _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[2];
float _Split_d9b6fd95965b407abd03352c64bd95d4_A_4_Float = _Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4[3];
float _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float;
Unity_Maximum_float(_Split_d9b6fd95965b407abd03352c64bd95d4_R_1_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_G_2_Float, _Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float);
float _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float;
Unity_Maximum_float(_Maximum_e25c31be13314b87ae27a198895b64a2_Out_2_Float, _Split_d9b6fd95965b407abd03352c64bd95d4_B_3_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float);
float _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float;
Unity_Divide_float(_Maximum_c521d868294841f4b71b21734f9cf047_Out_2_Float, _Maximum_4315d9c44c34415fbb4d00a6c0e720f6_Out_2_Float, _Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float);
float _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float;
Unity_Multiply_float_float(_Divide_e673cbf0a11048c092ecf1e1fbc09be8_Out_2_Float, 0.5, _Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float);
float _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float;
Unity_Add_float(_Multiply_8b827c41892549ebb55b1a4907a92bf2_Out_2_Float, float(0.5), _Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float);
float4 _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4;
Unity_Multiply_float4_float4(_Branch_eea3f9e8c5df4bf79dc691911a8e7d45_Out_3_Vector4, (_Add_b86fceb0db8d4af9bc21dfe417af2843_Out_2_Float.xxxx), _Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4);
float4 _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4;
Unity_Saturate_float4(_Multiply_4bb9f6177dff47bbb3c65a7fc5da20af_Out_2_Vector4, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4);
float4 _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4;
Unity_Branch_float4(_Property_4ec1fadc986743f2b9b3be9ad07b5c23_Out_0_Boolean, _Saturate_4ebc8100309e4882b91c688956b0477c_Out_1_Vector4, _Multiply_37087b60e9d043069303dd34abafccdd_Out_2_Vector4, _Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4);
float _Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean = Crossfade;
float _Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean = Is_Billboard;
float _Split_8e113e5414194688aa2c165814b6360b_R_1_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[0];
float _Split_8e113e5414194688aa2c165814b6360b_G_2_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[1];
float _Split_8e113e5414194688aa2c165814b6360b_B_3_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[2];
float _Split_8e113e5414194688aa2c165814b6360b_A_4_Float = _SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4[3];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_R_1_Float = IN.VertexColor[0];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_G_2_Float = IN.VertexColor[1];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_B_3_Float = IN.VertexColor[2];
float _Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float = IN.VertexColor[3];
float _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float;
Unity_Multiply_float_float(_Split_d5fe6b04dd9747b6a9e6d27930d2ffcb_A_4_Float, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float);
float _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float;
Unity_Branch_float(_Property_321d0864f8e24c789d8ead6ed475e3c3_Out_0_Boolean, _Split_8e113e5414194688aa2c165814b6360b_A_4_Float, _Multiply_e8c48c2857c946f78c6f01261fe2553c_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float);
float4 _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
float _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float;
LODDitheringTransitionSG_float(IN.WorldSpaceViewDirection, _ScreenPosition_23cc09dad8e948a6b2c20b60e8ebb8e3_Out_0_Vector4, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float);
float _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float;
Unity_Multiply_float_float(_Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _LODDitheringTransitionSGCustomFunction_6837a7eb54884986933856bdbf84238d_multiplyAlpha_0_Float, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float);
float _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Unity_Branch_float(_Property_c660a337893a4106a47253f4fdaa173d_Out_0_Boolean, _Multiply_07757bf1a334469cb891cd448c68d81a_Out_2_Float, _Branch_5bd4b0e2a77b4a918e51059017f603e4_Out_3_Float, _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float);
Modified_Color_1 = (_Branch_38536d5bf09446bc9e20abbd05f134e7_Out_3_Vector4.xyz);
Modified_Alpha_4 = _Branch_88ac64f4ac3b4739937bd51278da2174_Out_3_Float;
Original_Color_2 = (_SampleTexture2D_f0368572efa94c5ebedc97b7f89e54d4_RGBA_0_Vector4.xyz);
Original_Alpha_3 = _Split_8e113e5414194688aa2c165814b6360b_A_4_Float;
}

// Custom interpolators pre vertex
/* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */

// Graph Vertex
struct VertexDescription
{
float3 Position;
float3 Normal;
float3 Tangent;
};

VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
VertexDescription description = (VertexDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(1);
#else
float _IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_a2d6b567e832e38482216a529cd0fc49_Out_0_Float, float(1), _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.ObjectSpacePosition = IN.ObjectSpacePosition;
_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a.uv2 = IN.uv2;
float3 _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3;
SG_SpeedTree8LODTransition_74ce720b09dcad14690aca1cb7b6f2dc_float(IN.ObjectSpacePosition, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a, _SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean = _WindQuality;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float = float(3);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float;
Unity_Branch_float(_Property_a3378a1543ec49078a16fce01b44c36e_Out_0_Boolean, _Float_9a9c3aa3ab5d40319f9542e4243c61a7_Out_0_Float, float(0), _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.ObjectSpaceNormal = IN.ObjectSpaceNormal;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv0 = IN.uv0;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv1 = IN.uv1;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv2 = IN.uv2;
_SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49.uv3 = IN.uv3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
float3 _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3;
SG_SpeedTree8Wind_e9398b7940890a74eafc240b5a593541_float(_SpeedTree8LODTransition_88f28a7e53bb47b5a643fbe81c26c07a_ObjectSpacePositionOut_1_Vector3, _Branch_d7957dd98d4f4cccb1a40c46fb164ff9_Out_3_Float, _Comparison_054b520ca780e68499b679259d69ce7d_Out_2_Boolean, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3, _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_ObjectSpaceMotionVector_2_Vector3);
#endif
description.Position = _SpeedTree8Wind_70054e3bf5213b839168ebc624dedb49_AnimatedVertexObjectSpacePosition_1_Vector3;
description.Normal = IN.ObjectSpaceNormal;
description.Tangent = IN.ObjectSpaceTangent;
return description;
}

// Custom interpolators, pre surface
#ifdef FEATURES_GRAPH_VERTEX
Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
{
return output;
}
#define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
#endif

// Graph Pixel
struct SurfaceDescription
{
float3 BaseColor;
};

SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
SurfaceDescription surface = (SurfaceDescription)0;
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
UnityTexture2D _Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D = UnityBuildTexture2DStructNoScale(_MainTex);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4 = _Color;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean = _HueVariationKwToggle;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float4 _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4 = _HueVariationColor;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean = _OldHueVarBehavior;
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(EFFECT_BILLBOARD)
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(1);
#else
float _IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean;
Unity_Comparison_Equal_float(_IsBillboard_cb7c164715707985a259eacd43901498_Out_0_Float, float(1), _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
#if defined(_LOD_FADE_CROSSFADE)
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(1);
#else
float _LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float = float(0);
#endif
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
float _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean;
Unity_Comparison_Equal_float(_LODFADECROSSFADE_a31a96006f1a008fa94162300147c0d4_Out_0_Float, float(1), _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean);
#endif
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
Bindings_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.VertexColor = IN.VertexColor;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.NDCPosition = IN.NDCPosition;
_SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da.uv0 = IN.uv0;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float;
float3 _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3;
float _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float;
SG_SpeedTree8ColorAlpha_1b4ecad27a9bc714e8d3af3ffb8a368c_float(_Property_60ea7522b0e6488ab3c19199b512b948_Out_0_Texture2D, _Property_a86f6a00f7a06485a579699fcd040ddc_Out_0_Vector4, _Property_0b5fa7423ffe4455acdb86240164b6e2_Out_0_Boolean, _Property_5fb42de5200efa8dba23dfa8af048bbb_Out_0_Vector4, _Property_2086b2c956b74b75b05d4b5647cdbc58_Out_0_Boolean, _Comparison_021296dda19bac8b934042f30313fb73_Out_2_Boolean, _Comparison_c09c03ac23961d85b7a462e3fb87b3c5_Out_2_Boolean, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedAlpha_4_Float, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalColor_2_Vector3, _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_OriginalAlpha_3_Float);
#endif
surface.BaseColor = _SpeedTree8ColorAlpha_638ceae238f14da591f4e9f4158337da_ModifiedColor_1_Vector3;
return surface;
}

// --------------------------------------------------
// Build Graph Inputs
#ifdef HAVE_VFX_MODIFICATION
#define VFX_SRP_ATTRIBUTES Attributes
#define VFX_SRP_VARYINGS Varyings
#define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
#endif
VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
{
    VertexDescriptionInputs output;
    ZERO_INITIALIZE(VertexDescriptionInputs, output);

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceNormal =                          input.normalOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpaceTangent =                         input.tangentOS.xyz;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.ObjectSpacePosition =                        input.positionOS;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 =                                        input.uv0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv1 =                                        input.uv1;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv2 =                                        input.uv2;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv3 =                                        input.uv3;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif

    return output;
}
SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

#ifdef HAVE_VFX_MODIFICATION
#if VFX_USE_GRAPH_VALUES
    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
#endif
    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */

#endif

    





#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
#endif


    #if UNITY_UV_STARTS_AT_TOP
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #else
#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

    #endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.NDCPosition.y = 1.0f - output.NDCPosition.y;
#endif


#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.uv0 = input.texCoord0;
#endif

#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7) || defined(KEYWORD_PERMUTATION_8) || defined(KEYWORD_PERMUTATION_9) || defined(KEYWORD_PERMUTATION_10) || defined(KEYWORD_PERMUTATION_11)
output.VertexColor = input.color;
#endif

#if UNITY_ANY_INSTANCING_ENABLED
#else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
#endif
#if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
#else
#define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
#endif
#undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN

        return output;
}

// --------------------------------------------------
// Main

#include "Assets/SceneOptimization/Shaders/TreeGraphVaryings.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBR2DPass.hlsl"

// --------------------------------------------------
// Visual Effect Vertex Invocations
#ifdef HAVE_VFX_MODIFICATION
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
#endif

ENDHLSL
}
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "UnityEditor.ShaderGraphLitGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
FallBack "Hidden/Shader Graph/FallbackError"
}