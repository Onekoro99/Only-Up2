Shader "CandyKingdom/PastelSky" {
 Properties { _Top("Zenith",Color)=(.32,.62,.94,1) _Horizon("Horizon",Color)=(.84,.88,.98,1) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"} Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Top; float4 _Horizon;
 CBUFFER_END
 struct Attributes {float3 positionOS:POSITION;};
 struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
 Varyings vert(Attributes a){Varyings o;o.positionCS=TransformObjectToHClip(a.positionOS);o.direction=a.positionOS;return o;}
 half4 frag(Varyings i):SV_Target{float3 d=normalize(i.direction);float f=pow(saturate(d.y),.6);float3 color=lerp(_Horizon.rgb,_Top.rgb,f);float sun=pow(saturate(dot(d,normalize(float3(-.4,.6,.7)))),900);color+=float3(1,.83,.58)*sun*.35;return half4(color,1);}
 ENDHLSL }
 } }