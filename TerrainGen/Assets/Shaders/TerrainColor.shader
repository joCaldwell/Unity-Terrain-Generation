Shader "Custom/HeightBasedColorShader"
{
    Properties
    {
        _MaxHeight ("Max Height", Float) = 1.0
        _NoiseScale ("Noise Scale", Float) = 1.0
        _ColorLow ("Low Height Color", Color) = (0, 0, 1, 1)  // Blue for low heights
        _ColorHigh ("High Height Color", Color) = (1, 1, 0, 1) // Yellow for high heights
    }
    SubShader
    {
        Tags { "LightMode"="UniversalForward" "RenderType"="Opaque" }
        LOD 100
        ZWrite On
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha

        pass 
        {
            HLSLPROGRAM

			#pragma vertex Vertex
			#pragma fragment Fragment

            #include "ForwardPassLighting.hlsl"

			ENDHLSL
		}
    }
}