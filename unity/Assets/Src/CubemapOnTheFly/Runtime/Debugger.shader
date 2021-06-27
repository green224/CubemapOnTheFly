// キューブマップのデバッグ用マテリアル
Shader "CubemapOnTheFly/CubemapOnTheFly_Debugger"
{
	Properties
	{
		[MainTexture] _BaseMap("Albedo", Cube) = "white" {}
	}

	SubShader
	{
		Tags {
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
			"IgnoreProjector" = "True"
			"Queue" = "Background"
		}

//		Blend SrcAlpha OneMinusSrcAlpha
		Blend One Zero
		BlendOp Add
		ZWrite On
		ZTest LEqual
		Cull Back

		Pass
		{
			Tags{"LightMode" = "UniversalForward"}

			HLSLPROGRAM
			#pragma target 2.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			TEXTURECUBE(_BaseMap);	SAMPLER(sampler_BaseMap);


			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 positionOS	: POSITION;
			};

			struct Varyings
			{
				float4 posCS : SV_POSITION;
				float3 posOS : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.posCS = vertexInput.positionCS;
				output.posOS = input.positionOS.xyz;

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				return SAMPLE_TEXTURECUBE( _BaseMap, sampler_BaseMap, normalize(input.posOS) );
			}

			ENDHLSL
		}
	}

	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
