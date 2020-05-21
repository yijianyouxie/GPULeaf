Shader "Custom/Unlit/Leaf"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			//旋转
			float4x4 Rotation(float4 rotaion)
			{
				float radX = radians(rotaion.x);
				float radY = radians(rotaion.y);
				float radZ = radians(rotaion.z);

				float sinX = sin(radX);
				float cosX = cos(radX);
				float sinY = sin(radY);
				float cosY = cos(radY);
				float sinZ = sin(radZ);
				float cosZ = cos(radZ);

				return float4x4(cosY * cosZ, -cosY * sinZ, sinY, 0.0,
								cosX * sinZ + sinX * sinY * cosZ, cosX * cosZ - sinX * sinY * sinZ, -sinX * cosY, 0.0,
								sinX * sinZ - cosX * sinY * cosZ, sinX * cosZ + cosX * sinY * sinZ, cosX * cosY, 0.0,
								0.0, 0.0, 0.0, 1.0);
			}

			v2f vert (appdata v)
			{
				v2f o;
				float4 vv = float4(v.vertex.x, v.vertex.y, v.vertex.z, 1);
				v.vertex = mul(Rotation(float4(_Time.w * 10, _Time.w*10, _Time.w * 10, 1)), vv);

				float4 worldPos = mul(_Object2World, v.vertex);
				//worldPos.y -= _Time.y/2;

				//worldPos = mul(Rotation(float4(0, _Time.w, 0, 1)), worldPos);


				o.vertex = mul(UNITY_MATRIX_VP, worldPos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);


				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);

				clip(col.a - 0.1);

				return col;
			}
			ENDCG
		}
	}
}
