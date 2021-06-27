// キューブマップの面をレンダリングする際の、Blit用マテリアル
Shader "CubemapGenerator/CubemapGenerator_BlitMaterial"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags {
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

//		Blend SrcAlpha OneMinusSrcAlpha
		Blend One Zero
		BlendOp Add
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			Tags{"LightMode" = "UniversalForward"}

			HLSLPROGRAM
			#pragma target 2.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);


			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 posOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 posCS : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.posOS.xyz);
				output.posCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				return SAMPLE_TEXTURE2D(
					_MainTex, sampler_MainTex,
					float2( input.uv.x, input.uv.y )
				);
			}

			ENDHLSL
		}
	}

	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
