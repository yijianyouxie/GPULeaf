using UnityEngine;
using System.Collections;

public class ShaderPropty2ID
{
    internal static readonly int TimeX = Shader.PropertyToID("_TimeX");
    internal static readonly int Distortion = Shader.PropertyToID("_Distortion");
    internal static readonly int ScreenResolution = Shader.PropertyToID("_ScreenResolution");
    internal static readonly int HueShift = Shader.PropertyToID("_HueShift");
    internal static readonly int Sat = Shader.PropertyToID("_Sat");
    internal static readonly int Val = Shader.PropertyToID("_Val");
    internal static readonly int VignetteTex = Shader.PropertyToID("_VignetteTex");
    internal static readonly int Blur = Shader.PropertyToID("_Blur");
    internal static readonly int LUT = Shader.PropertyToID("_LUT");
    internal static readonly int Contribution = Shader.PropertyToID("_Contribution");

    internal static readonly int EdgeColor = Shader.PropertyToID("_EdgeColor");
    internal static readonly int NonEdgeColor = Shader.PropertyToID("_NonEdgeColor");
    internal static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
    internal static readonly int SampleRange = Shader.PropertyToID("_SampleRange");
    internal static readonly int Brightness = Shader.PropertyToID("_Brightness");
    internal static readonly int Saturation = Shader.PropertyToID("_Saturation");
    internal static readonly int Contrast = Shader.PropertyToID("_Contrast");

    internal static readonly int RampTex = Shader.PropertyToID("_RampTex");
    internal static readonly int RampOffset = Shader.PropertyToID("_RampOffset");
    internal static readonly int Parameter = Shader.PropertyToID("_Parameter");
    internal static readonly int Bloom = Shader.PropertyToID("_Bloom");

    internal static readonly int value = Shader.PropertyToID("_value");

    internal static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");
    internal static readonly int DistortTimeFactor = Shader.PropertyToID("_DistortTimeFactor");
    internal static readonly int DistortStrength = Shader.PropertyToID("_DistortStrength");

    //internal static readonly int GrassTex = Shader.PropertyToID("_GrassTex");
    //internal static readonly int GrassMatrix = Shader.PropertyToID("GrassMatrix");

    internal static readonly int MovePathCameraRT = Shader.PropertyToID("_MovePathCameraRT");
    internal static readonly int MovePathCameraRT2 = Shader.PropertyToID("_MovePathCameraRT2");
    internal static readonly int MovePathCameraMatrixVP = Shader.PropertyToID("MovePathCameraMatrixVP");
    internal static readonly int ColorRatio = Shader.PropertyToID("_ColorRatio");
    internal static readonly int DropSpeed = Shader.PropertyToID("_DropSpeed");
    internal static readonly int LifeLoopInterval = Shader.PropertyToID("_LifeLoopInterval");
    //internal static readonly int LifeLoopNum = Shader.PropertyToID("_LifeLoopNum");
    internal static readonly int ParticalYOff = Shader.PropertyToID("_ParticalYOff");
}
