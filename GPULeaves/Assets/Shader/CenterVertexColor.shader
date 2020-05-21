Shader "Custom/Unlit/CenterVertexColor"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Height2Ground("Height2Ground，距离地面高度，停止下落和旋转", Range(0, 1)) = 0.1
	}
	SubShader
	{
		//Tags{ "RenderType" = "Opaque" }
		//改用透明渲染
		Tags { "Queue" = "Transparent" "IgnorePorjector" = "True" "RenderType"="Transparent" }
		LOD 100

		Pass
		{
			Cull Off
			//改用透明渲染
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

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
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Height2Ground;
			
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

			float4x4 Rotation2()
			{
				float4x4 R_x =
				{
					1, 0, 0, 0,
					0, cos(_Time.y), -sin(_Time.y), 0,
					0, sin(_Time.y), cos(_Time.y), 0,
					0, 0, 0, 1,
				};

				return R_x;
			}

			v2f vert (appdata v)
			{
				v2f o;

				////先计算下落，为停止旋转做准备
				//float4 worldPos = mul(_Object2World, v.vertex);
				////worldPos.y -= _Time.y;

				////计算高度差
				//float heightDelta = worldPos.y - _Height2Ground;
				////转换到0-1范围
				//float zero2One = saturate(heightDelta);
				////转换到0或1
				//float zeroOrOne = ceil(zero2One);

				////此处根据顶点的uv信息，构造出一个基准为(0, 0, 0, 1)的点
				//float2 transUV = v.uv - 0.5f;
				//float4 fakeVert = float4(transUV.x, 0, transUV.y, 1);

				float4 center = float4(v.color.x * 10000, v.color.y * 10000, v.color.z * 10000, 0);

				float4 v2Center = v.vertex - center;
				v2Center.w = 1;
				//v.vertex = mul(Rotation2(), v2Center) + center;
				//float4 vv = float4(v.vertex.x, v.vertex.y, v.vertex.z, 1);
				v.vertex = mul(Rotation(float4(_Time.w * 10, 0, _Time.w * 10, 1)), v2Center) + center;

				//旋转
				//float4 rotateParam = float4(_Time.w * 10,0, 0, 0)/**zeroOrOne*/;
				//rotateParam.w = 1;
				//float4 rotatedVert = mul(Rotation(rotateParam), fakeVert);
				//float4 rotatedVert = mul(Rotation2(), fakeVert);
				//float4 rotatedVert = mul(Rotation(float4(0, 0, 0, 1)), fakeVert);

				//注意：此处原来的写法是v.vertex = v.vertex + fakeVert;由于上边的fakeVert.w也是1，
				//v.vertex.w默认是1，这里相加后就成了2，导致游戏里的xyz坐标都变为时间值的一半
				//v.vertex.xyz += rotatedVert.xyz;
				
				//下落
				float4 worldPos = mul(_Object2World, v.vertex);
				//worldPos.y -= _Time.y;

				////计算高度差
				//float heightDelta = worldPos.y - _Height2Ground;
				////转换到0-1范围
				//float zero2One = saturate(heightDelta);
				////转换到0或1
				//float zeroOrOne = ceil(zero2One);
				//worldPos.y = heightDelta * zeroOrOne + _Height2Ground;

				/*ceil(worldHeight)
				if (worldPos.y <= 0.1f)
				{
					worldPos.y = 0.1f;
				}*/

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

				//改用透明渲染
				//clip(col.a - 0.1);
				

				return col;
			}
			ENDCG
		}
	}
}
