Shader "Custom/Unlit/CenterVertex"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.1
		_Height2Ground("Height2Ground，距离地面高度，停止下落和旋转", Range(0, 1)) = 0.1
		_MovePathCameraRT("_MovePathCameraRT，运动轨迹RT", 2D) = "black" {}
		_MovePathCameraRT2("_MovePathCameraRT2，运动轨迹RT", 2D) = "black" {}
		_ColorRatio("_ColorRatio，顶点的颜色值会除以此值", float) = 1000000
		_DropSpeed("_DropSpeed，下落速度", Range(0, 5)) = 1

		//[Header(Move)]
		_LifeLoopInterval("_LifeLoopInterval,生命循环一次持续的时长", float) = 20
		//_LifeLoopNum("_LifeLoopNum，生命循环次数，给轨迹顶点颜色值使用", float) = 0
		_ParticalYOff("_ParticalYOff,初始粒子y坐标的偏移", float) = 20
	}
	SubShader
	{
		//重新改回非透明2020-04-23，因为透明渲染需要保持前后顺序
		//Tags{ "RenderType" = "Opaque" }
		Tags{ "Queue" = "AlphaTest+100" "IgnoreProjector" = "False" "RenderType" = "TransparentCutout" }
		//改用透明渲染2020-04-18
		//Tags { "Queue" = "Transparent" "IgnorePorjector" = "True" "RenderType"="Transparent" }
		LOD 200

		Pass
		{
			Cull Off
			//改用透明渲染2020-04-18 重新改为非透明渲染2020-04-23
			//ZWrite Off
			//Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			uniform float4x4 MovePathCameraMatrixVP;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color: COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				//测试输出颜色
				fixed test : TEXCOORD2;
			};

			sampler2D _MainTex;
			fixed _Cutoff;
			float4 _MainTex_ST;
			float _Height2Ground;
			sampler2D _MovePathCameraRT;
			sampler2D _MovePathCameraRT2;
			float _ColorRatio;
			float _DropSpeed;
			float _LifeLoopInterval;
			//float _LifeLoopNum;
			float _ParticalYOff;
			
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

				v.vertex.y = v.vertex.y + _ParticalYOff;

				//此顶点创建的时间点
				float vct = v.color.y * _LifeLoopInterval + v.color.w * _ColorRatio * _LifeLoopInterval;
				float deltaT = _Time.y - vct;

				//先计算下落，为停止旋转做准备。
				//注意：这里的计算是以中心点为基准
				float4 worldPos = mul(_Object2World, v.vertex);

				//此顶点的初始y坐标
				float initWorldPosY = worldPos.y;

				worldPos.y -= deltaT * _DropSpeed;

				//计算高度差
				float heightDelta = worldPos.y - _Height2Ground;
				//转换到0-1范围
				float zero2One = saturate(heightDelta);
				//转换到0或1
				float zeroOrOne = ceil(zero2One);

				//此处根据顶点的uv信息，构造出一个基准为(0, 0, 0, 1)的点
				float2 transUV = v.uv - 0.5f;
				float4 fakeVert = float4(transUV.x, 0, transUV.y, 1);

				//旋转(斜45°，)
				float4 rotateParam = float4(-90, 15 * cos(deltaT), 60 * sin(deltaT), 0) * zeroOrOne;
				rotateParam.w = 1;
				float4 rotatedVert = mul(Rotation(rotateParam), fakeVert);
				//水平位置缓动
				float2 horizonNoise = float2(cos(deltaT), sin(deltaT)) / 5 * zeroOrOne;
				v.vertex.xz += horizonNoise;

				//注意：此处原来的写法是v.vertex = v.vertex + fakeVert;由于上边的fakeVert.w也是1，
				//v.vertex.w默认是1，这里相加后就成了2，导致游戏里的xyz坐标都变为时间值的一半
				v.vertex.xyz += rotatedVert.xyz;
				v.vertex.w = 1;
				
				//下落
				float4 realwWrldPos = mul(_Object2World, v.vertex);
				realwWrldPos.y -= deltaT * _DropSpeed;

				//重新计算高度差
				//以实际的顶点位置为基准计算(之前使用了上面计算出的heightDelta，导致叶子不会有立体旋转)
				float realHeightDelta = realwWrldPos.y - _Height2Ground;
				////转换到0-1范围
				//zero2One = saturate(heightDelta);
				////转换到0或1
				//zeroOrOne = ceil(zero2One);
				realwWrldPos.y = realHeightDelta * zeroOrOne + _Height2Ground;

				//此处开始使用基准点检测角色的运动轨迹
				half4 movePathUV = mul(MovePathCameraMatrixVP, worldPos);
				half2 centerUV = movePathUV.xy / movePathUV.w * 0.5 + 0.5;//由（-1，1）到（0，1）
#if UNITY_UV_STARTS_AT_TOP
				//dx
				centerUV.y = 1 - centerUV.y;
#endif
				//RT的尺寸会影响精度
				float4 movePathCol = tex2Dlod(_MovePathCameraRT, half4(centerUV, 0, 0));
				//转换到0或1
				float movePathColZeroOrOne = ceil(movePathCol.x) + ceil(movePathCol.z);
				movePathColZeroOrOne = saturate(movePathColZeroOrOne);
				movePathColZeroOrOne = ceil(movePathColZeroOrOne);
				
				movePathCol.xz = (movePathCol.xz - 0.5) * 2;//0，1转换到-1，1,进行方向表示

				//rt上顶点的创建时间
				float rtvct = movePathCol.y * _LifeLoopInterval + movePathCol.w * _ColorRatio * _LifeLoopInterval;

				float groundH = 0;
				float deltaH = initWorldPosY - groundH - _Height2Ground;
				float endT = vct + deltaH / _DropSpeed;
				if (endT <= rtvct)
				{
					realwWrldPos.xz += movePathColZeroOrOne * 2 * normalize(movePathCol.xz) * (1 - zeroOrOne);
				}

				//o.test = _ParticalYOff/40;

				o.vertex = mul(UNITY_MATRIX_VP, realwWrldPos);
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

				//改用透明渲染2020-04-18
				//重新改回非透明渲染2020-04-23
				clip(col.a - _Cutoff);
				

				return col;
				//return fixed4(i.test, i.test, i.test, 1);
			}
			ENDCG
		}
	}
}
