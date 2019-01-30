// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PhotosphereSky" 
{
	Properties {
		_MainTex ("", 2D) = "white" {}
	}
	SubShader 
	{
		Tags { "Queue"="Background" "RenderType"="Background" }
		Cull Off ZWrite Off Fog { Mode Off }
		
		Pass {
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			#define PI 3.141592653589793

			uniform sampler2D _MainTex;

			struct v2f 
			{
    			float4 pos : SV_POSITION;
    			float2 uv : TEXCOORD0;
    			float3 dir : TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f OUT;

    			OUT.pos = UnityObjectToClipPos (float4(v.vertex.xyz,1.0));
    			OUT.uv = 
    			v.texcoord.xy;
    			
				OUT.dir = 
				v.vertex.xyz;
				//mul(_Object2World, v.vertex).xyz; 
               	//- _WorldSpaceCameraPos;
    			
    			return OUT; 
			}
			
			float4 frag(v2f IN) : COLOR
			{
				float3 d = -normalize(IN.dir);

				float2 uv = 
				float2(0.5+atan2(d.z,d.x)/(2*PI),0.5-asin(d.y)/PI);

				half4 col;
				
				//anti-seam conditions
				if ( uv.x < 0.001){
					col = tex2D(_MainTex, float2(0.0, uv.y) );
					
				}
				else if ( uv.x > 0.999){
					col = tex2D(_MainTex, float2(1.0, uv.y) );
				}
				else
				{
					col = 
					tex2D(_MainTex, uv );
				}
			   
			    return col; 
			}

			ENDCG
		}
	}
 	Fallback off
}