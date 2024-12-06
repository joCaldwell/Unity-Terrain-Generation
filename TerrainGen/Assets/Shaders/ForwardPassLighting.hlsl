#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float4 _ColorLow;
float4 _ColorHigh;
float _MaxHeight;
float _NoiseScale;

struct Attributes
{
    float3 position : POSITION;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 pos : SV_POSITION;
    float3 posWorld : TEXCOORD0;
    float3 normalDir : TEXCOORD1;

    half3 lightAmt : TEXCOORD2;
};

float3 ObjectNormalToWorldSimple(float3 normal)
{
    // Directly use the 3x3 part of unity_ObjectToWorld for uniform scaling and rotation
    float3x3 objectToWorld3x3 = (float3x3)unity_ObjectToWorld;

    // Transform and normalize the normal
    return normalize(mul(objectToWorld3x3, normal));
}

Varyings Vertex(Attributes IN)
{
    Varyings OUT;
    VertexPositionInputs input;
    VertexNormalInputs inNormal;
    Light mainLight;
    input = GetVertexPositionInputs(IN.position);
    inNormal = GetVertexNormalInputs(IN.normal);
    OUT.pos = input.positionCS;
    OUT.posWorld = input.positionWS;
    OUT.normalDir = inNormal.normalWS;

    mainLight = GetMainLight(float4(TransformWorldToShadowCoord(input.positionCS).xyz, 1));
    OUT.lightAmt = LightingLambert(mainLight.color, mainLight.direction, inNormal.normalWS.xyz);
    return OUT;
}

float4 Fragment(Varyings OUT) : SV_Target
{
    float4 col = 0.5f;
    float3 finalColor = lerp(_ColorLow, _ColorHigh, OUT.posWorld.y / _MaxHeight);
    return float4(finalColor*OUT.lightAmt, 1.0f);
}