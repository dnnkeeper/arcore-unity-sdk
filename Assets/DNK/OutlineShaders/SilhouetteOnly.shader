// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Outlined/Silhouette Only" {
	Properties{
		[HDR]
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", Range(0.0, 1.0)) = 0.03
		//_OutlineOffset("Outline Offset", Vector) = (0, 0, 0)
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : POSITION;
		float4 color : COLOR;
	};

	uniform float _Outline;
	uniform float4 _OutlineColor;
	uniform half3 _OutlineOffset;

	v2f vert(appdata v) {
		// just make a copy of incoming vertex data but scaled according to normal direction
		v2f o;

		float3 vertex = v.vertex.xyz;
		
		
		float3 vertexWorldSpace = mul(unity_ObjectToWorld, vertex);

		vertexWorldSpace = vertexWorldSpace + normalize(vertexWorldSpace)*_Outline;

		//float colorPower = clamp( length(_WorldSpaceCameraPos - vertexWorldSpace) / 5, 0, 1 );

		vertex = mul(unity_WorldToObject, vertexWorldSpace);
		/*

		half3 Outline = 
			half3(_Outline, _Outline, _Outline);
			//mul(unity_WorldToObject, half3(_Outline, _Outline, _Outline));
			//half3(outline, outline, outline);
			//mul(unity_ObjectToWorld, half3(_Outline, _Outline, _Outline));
				vertex -= _OutlineOffset;
				vertex.x *= (Outline.x) + 1;
				vertex.y *= (Outline.y) + 1;
				vertex.z *= (Outline.z) + 1;
				//vertex *= Outline + 1;
				vertex += _OutlineOffset;
				*/
				
				o.pos = UnityObjectToClipPos(float4(vertex, v.vertex.w));
				o.color = _OutlineColor;
				//half4(vertexWorldSpace.rgb- _WorldSpaceCameraPos.rgb, 1);//
		return o;
	}
	ENDCG

		SubShader{
			Tags { "Queue" = "Geometry-1" }

			Pass {
				Name "BASE"
				
				Blend Zero One

				// uncomment this to hide inner details:
				//Offset -8, -8

				SetTexture[_OutlineColor] {
					ConstantColor(0,0,0,0)
					Combine constant
				}
			}

			// note that a vertex shader is specified here but its using the one above
			Pass {
				Name "OUTLINE"
				Tags { "LightMode" = "Always" }
				Cull Front
				//ZWrite Off
				// you can choose what kind of blending mode you want for the outline
				Blend SrcAlpha OneMinusSrcAlpha // Normal
				//Blend One One // Additive
				//Blend One OneMinusDstColor // Soft Additive
				//Blend DstColor Zero // Multiplicative
				//Blend DstColor SrcColor // 2x Multiplicative

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				half4 frag(v2f i) :COLOR {
					return i.color;
				}
				ENDCG
			}
		}

		Fallback "Diffuse"
}
