using UnityEngine;

namespace TLStudio.Shadow
{
    /// <summary>
    /// 移动轨迹记录器
    /// </summary>
    public class MovePathCamera : MonoBehaviour
    {
        //RT的尺寸会影响精度
        public int textureSize = 256;

        private RenderTexture texture;

        [Header("CullingLayer")]
        public int layer = 9;
        [Header("shaderPropertyID")]
        public string shaderProperty = "_MovePathCameraRT";

        private Camera c;
        /// <summary>
        ///  默认格式为RGB565,如果不支持依次后退
        ///  需要rgba32格式4个通道
        /// </summary>
        private static RenderTextureFormat[] optionalFormats = new RenderTextureFormat[]
        {
            //RenderTextureFormat.RGB565, RenderTextureFormat.ARGB1555, RenderTextureFormat.ARGB4444,
            RenderTextureFormat.ARGB32
        };

        private RenderTextureFormat shadowTextureFormat = RenderTextureFormat.RGB565;

        void Start()
        {
            if (!GpuLeafManager.GetGrassEnabled())
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GrassManagerEnable is false.");
#endif
                return;
            }
            
            shadowTextureFormat = _chooseRenderFormat();
            texture = RenderTexture.GetTemporary(textureSize, textureSize, 0, shadowTextureFormat);
            c = GetComponent<Camera>();
            if(null == c)
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GrassCamera,Start: c is null.");
#endif
                return;
            }
            c.targetTexture = texture;
            c.cullingMask = (1 << layer);

            GpuLeafManager.AddMovePathCamera(this);
        }

        public Camera GetCamera()
        {
            return c;
        }

        public RenderTexture GetCameraRT()
        {
            return texture;
        }

        private void OnDestroy()
        {
            //切场景时会自动释放
            //if (null != grassMat)
            //{
            //    Destroy(grassMat);
            //}

            if (null != texture)
            {
                RenderTexture.ReleaseTemporary(texture);
            }
        }

        private static RenderTextureFormat _chooseRenderFormat()
        {
            for (int i = 0; i < optionalFormats.Length; ++i)
            {
                if (SystemInfo.SupportsRenderTextureFormat(optionalFormats[i]))
                {
                    return optionalFormats[i];
                }
            }

#if GAMEDEBUG
            Games.TLBB.Log.LogSystem.Error(
                "=========GrassCamera : can't find the supported format for render texture! use the Default format!");
#endif

            return RenderTextureFormat.Default;
        }
    }
}