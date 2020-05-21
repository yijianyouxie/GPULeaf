/********************************************************************
	v1.3 2020-04-20
    修改预创建buffer长度为1，单人坐骑路点最多为301；

    v1.2 2020-04-19
    Time.time替换为Time.timeSinceLevelLoad适配shader中的_Time.y
    注意切场景后清空状态，保证时间的准确

    v1.1
    1，增加创建点的时间信息，使用color的g通道表示.

    v1.0 2020-04-18    
    1，模仿GrassManager
*********************************************************************/
using UnityEngine;
using System.Collections.Generic;
using System;

namespace TLStudio.Shadow
{
    /// <summary>
    /// 场景草管理器
    /// </summary>
    public class GpuLeafManager : MonoBehaviour
    {
        #region====grass enable control
        private static bool hasInitGrassEnable = false;
        private static bool grassEnable = true;

        public static bool GetGrassEnabled()
        {
            if (hasInitGrassEnable)
            {
                return grassEnable;
            }

            //grassEnable = SystemCFG.Instance.GetInt("GrassEnable", "IsGrassEnable") >= 1;
            hasInitGrassEnable = true;
            return grassEnable;
        }

        #endregion====grass enable control


        #region====public method

        public int AddElement(Transform tr)
        {
            ElementData eld = null;
            int retIndex = -1;
            int listCount = elementList.Count;
            for (int i = 0; i < listCount; i++)
            {
                ElementData el = elementList[i];
                if (!el.used)
                {
                    eld = el;
                    retIndex = i;
                    break;
                }
            }

            if (retIndex == -1 && listCount == maxElementCount)
            {
                return retIndex;
            }

            if (null == eld)
            {
                eld = new ElementData();
                retIndex = elementList.Count;
                elementList.Add(eld);
            }

            eld.enabled = true;
            eld.parentTrans = tr;
            eld.used = true;
            eld.points.Clear();

            elementCount++;
            if (elementCount > elementBufferCount)
            {
                elementBufferCount += 10;
                DestroyBufferData();
                CreateBufferData();
            }

            return retIndex;
        }

        public void RemoveElement(int id)
        {
            if (id >= 0 && id < elementList.Count)
            {
                ElementData eld = elementList[id];
                eld.used = false;
                eld.parentTrans = null;
                eld.points.Clear();

                --elementCount;
            }
            else
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GrassManager, RemoveElement Error.id=." + id);
#endif
            }
        }

        public void EnableElement(int id, bool enabled)
        {
            if (id >= 0 && id < elementList.Count)
            {
                ElementData eld = elementList[id];
                eld.enabled = enabled;
                eld.points.Clear();
            }
            else
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GrassManager, RemoveElement Error.id=." + id);
#endif
            }
        }

        #endregion====public method

        private const int maxElementCount = 40;
        private List<ElementData> elementList = new List<ElementData>(maxElementCount);
        //元素个数
        private int elementCount = 0;
        //private int elementBufferCount = 10;
        //只允许主角产生树叶效果
        private int elementBufferCount = 1;

        [Header("角色前进时，向两边扩张的比例")]
        public float unit = 0.5f;
        //移动检测的最小距离
        public float minVertexDistance = 0.5f;
        //效果持续的时间
        public static float lifetime = 30f;
        [Header("运动轨迹材质")]
        public Material pathMaterial;
        [Header("落叶材质")]
        public Material leafMaterial;
        public static int movePathLayer1 = 9;
        public static int movePathLayer2 = 10;
        private Vector3 upDir = Vector3.up;
        
        [Header("脚底圈圈尺寸")]
        public float footCircleSize = 0.6f;
        public float footCircleSizeColor = 0.5f;

        //单个人物跑动路点的最大数（经验数据，当lefttime为0.8f时，pointsMaxNum最大为13，所以这里取值16；
        //之前是1.5s的时候此值应当适当变大，但是疏忽了，没有进行修正）
        //最新数据，天马坐骑，顶点数最多为301；无坐骑，最多为151；所以定为301(这个值对应的是生命周期是20s的情况)
        //每个路点对应四个顶点
        //目前暂时只支持主角有效果
        public static int singlePointsMaxNum = 301;
        //单个人物产生的mesh顶点数
        //现在需要增加跑过的路径点的颜色渐变，单个元素的顶点数量增加3倍为150
        //增加了脚底固定圈，单个元素的顶点数量增加5，由150变为155
        //修正，最初一个路点对应4个顶点，后来其实是改为了一个路点对应8个顶点，并不是原来的三倍。
        //此处lefttime改为0.8f后，pointsMaxNum最大为13，此值最大值是57，此处取值为60 + 脚底的5个。这种情况下三角形索引最大为228【（13-1）*18 + 12】
        //不需要这个值了，注释掉
        //private static int singleElementVNum = 60;//155;//没有坐骑是30
        //List<TrailPoint> points = new List<TrailPoint>(pointsMaxNum);
        //private List<TrailPoint> renderPoints = new List<TrailPoint>(pointsMaxNum);

        //List<Vector3> vertices = new List<Vector3>();
        //List<int> triangles = new List<int>();
        //List<Color> colors = new List<Color>();

        private int verticesIndex = 0;
        private int colorsIndex = 0;
        private int trianglesIndex = 0;
        private Vector3[] vertices;
        private int[] triangles;
        private Color[] colors;
        private Mesh mesh;
        //是否需要更新
        private bool needUpdate = true;

        [Range(0f, 5f)]
        [Header("停止跑动后，人物脚底的草压倒的速度")]
        public float pressSpeed = 0.8f;

        public Transform tr;
        private Vector3 trPos = new Vector3(0, 0, 0);
        private float trMoveSpeed = 0.1f;
        private float maxPosx = 24;

        //记录的颜色值需要除以此值，此值定义为100，那么最多可以支撑的时间是：100*100*lifetime大约是20万s，55.5小时
        public static float colorRatio = 100;
        //下降速度
        public static float dropSpeed = 1f;
        [Header("粒子的Y轴偏移，向上为正值")]
        public static float yOff = 20f;

        //相机旋转
        public Transform cameraTr;
        public Transform centerTr;
        private float rotateSpeed = 1;
        private float angle = 0f;
        private float maxRotAngle = 30f;
        private float minRotAngle = -5f;


        #region====多层RT====
        [Header("多层RT的层数")]
        public int RTLayerNum = 2;
        //RT相机列表
        private static List<MovePathCamera> cList = new List<MovePathCamera>(2);
        private List<RTMeshData> rtMeshDataList;
        #endregion====多层RT====

        private void Init()
        {
            //EventManager.RegistDelegate(DataEvent.StartChangeScene, ResetNeedUpdate);
            //EventManager.RegistDelegate(DataEvent.GrassManagerNeedUpdate, EnableNeedUpdate);

            //layer = 31;
            mesh = new Mesh();

            CreateBufferData();

            if (null != tr)
            {
                trPos = tr.position;
                AddElement(tr);
            }
        }
        private void OnDestroy()
        {
            DestroyBufferData();
            DestroyImmediate(mesh);
            mesh = null;

            if( null != elementList)
            {
                int listCount = elementList.Count;
                for (int i = 0; i < listCount; i++)
                {
                    ElementData eld = elementList[i];
                    if (null != eld)
                    {
                        eld.parentTrans = null;
                        eld.points.Clear();
                        eld.points = null;
                    }
                }
                elementList.Clear();
                elementList = null;
            }

            if(null != cList)
            {
                cList.Clear();
                cList = null;
            }

            if( null != rtMeshDataList)
            {
                rtMeshDataList.Clear();
                rtMeshDataList = null;
            }
        }
        /// <summary>
        /// 经验数据，单人场景跑动顶点数量为30以内。
        /// 故以此得出数据，按照初始10人跑动顶点数量为10*30
        /// 如果超出则按照每次增加10人的步长进行增加
        /// </summary>
        private void CreateBufferData()
        {
            vertices = new Vector3[elementBufferCount* singlePointsMaxNum * 4];
            triangles = new int[elementBufferCount * singlePointsMaxNum * 12];//elementBufferCount * singlePointsMaxNum * 4 / 2 * 6
            colors = new Color[elementBufferCount * singlePointsMaxNum * 4];
        }

        private void DestroyBufferData()
        {
            vertices = null;
            triangles = null;
            colors = null;
        }
        private void Awake()
        {
            Init();
        }

        private void ResetNeedUpdate(object[] param)
        {
            needUpdate = false;
        }

        private void EnableNeedUpdate(object[] param)
        {
            needUpdate = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!needUpdate)
            {
                return;
            }

            if (null == pathMaterial)
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GrassManager, Update. material is null.");
#endif
                return;
            }

            //键盘事件
            if (Input.anyKey)
            {
                foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKey(keyCode))
                    {
                        if (keyCode == KeyCode.W)
                        {
                            trPos.z += trMoveSpeed;
                            tr.position = trPos;
                        }
                        else if (keyCode == KeyCode.S)
                        {
                            trPos.z -= trMoveSpeed;
                            tr.position = trPos;
                        }
                        else if (keyCode == KeyCode.A)
                        {
                            trPos.x -= trMoveSpeed;
                            tr.position = trPos;
                        }
                        else if (keyCode == KeyCode.D)
                        {
                            trPos.x += trMoveSpeed;
                            tr.position = trPos;
                        }

                        if(trPos.x >= maxPosx )
                        {
                            trPos.x = maxPosx;
                        }
                        else if(trPos.x <= -maxPosx)
                        {
                            trPos.x = -maxPosx;
                        }
                        if(trPos.z >= maxPosx)
                        {
                            trPos.z = maxPosx;
                        }else if( trPos.z <= 0)
                        {
                            trPos.z = 0;
                        }
                        tr.position = trPos;
                    }
                }
            }

            //鼠标屏幕拖拽
            float _mouseX = Input.GetAxis("Mouse X");
            float _mouseY = Input.GetAxis("Mouse Y");
            if (Input.GetMouseButton(0))
            {
                //控制相机绕中心点(centerPoint)水平旋转
                cameraTr.transform.RotateAround(centerTr.position, Vector3.up, _mouseX * rotateSpeed);

                //记录相机绕中心点垂直旋转的总角度
                angle += _mouseY * rotateSpeed;

                //如果总角度超出指定范围，结束这一帧（！用于解决相机旋转到模型底部的Bug！）
                //（这样做其实还有小小的Bug，能发现的网友麻烦留言告知解决办法或其他更好的方法）
                if (angle > maxRotAngle || angle < minRotAngle)
                {
                    return;
                }

                //控制相机绕中心点垂直旋转(！注意此处的旋转轴时相机自身的x轴正方向！)
                cameraTr.transform.RotateAround(centerTr.position, cameraTr.transform.right, _mouseY * rotateSpeed);
            }

            mesh.Clear();
            verticesIndex = 0;
            colorsIndex = 0;
            trianglesIndex = 0;

            int listCount = elementList.Count;
            for (int m = 0; m < listCount; m++)
            {
                ElementData eld = elementList[m];
                if (null == eld || !eld.enabled || !eld.used || null == eld.points || null == eld.parentTrans)
                {
                    continue;
                }

                List<TrailPoint> renderPoints = eld.points;
                Vector3 pos = eld.parentTrans.position;
                while (renderPoints.Count > 0)
                {
                    if (Time.timeSinceLevelLoad - renderPoints[0].creationTime > lifetime)
                    {
                        renderPoints.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }

                if (renderPoints.Count == 0 ||
                    Vector3.Distance(renderPoints[renderPoints.Count - 1].pos, pos) > minVertexDistance)
                {
                    renderPoints.Add(new TrailPoint(pos, Time.timeSinceLevelLoad));
                }
                if (renderPoints.Count < 2)
                {
                    if (!eld.isOnlyOne)
                    {
                        eld.isOnlyOne = true;
                        eld.onlyOneTime = Time.timeSinceLevelLoad - 1/(2*pressSpeed) + 0.05f;
                    }
                    DrawFootCircle(pos, eld.parentTrans, (Time.timeSinceLevelLoad - eld.onlyOneTime) * pressSpeed);
                    //continue;
                }
                else
                {
                    eld.isOnlyOne = false;
                    eld.onlyOneTime = Time.timeSinceLevelLoad;

                    DrawFootCircle(pos, eld.parentTrans, 1);
                    //上一个点列表的结束索引
                    int lastListVertexIndex = verticesIndex;
                    float uvFactor = 1f / (renderPoints.Count - 1);
                    for (int i = 0; i < renderPoints.Count; i++)
                    {
                        TrailPoint point = renderPoints[i];
                        if (i == 0)
                        {
                            AddPoint(point, renderPoints[i + 1].pos - point.pos, 0, lastListVertexIndex, point.pos);
                            continue;
                        }

                        TrailPoint lastPoint = renderPoints[i - 1];
                        if (i == renderPoints.Count - 1)
                        {
                            AddPoint(point, point.pos - lastPoint.pos, 1f, lastListVertexIndex, point.pos);
                            break;
                        }

                        TrailPoint nextPoint = renderPoints[i + 1];
                        AddPoint(point, nextPoint.pos - lastPoint.pos, i * uvFactor, lastListVertexIndex, nextPoint.pos);
                    }
                }

            }

            SetShaderProperty();

            for (int i = trianglesIndex; i < triangles.Length; i++)
            {
                triangles[i] = 0;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            Graphics.DrawMesh(mesh, Matrix4x4.identity, pathMaterial, movePathLayer1);
        }

        void AddPoint(TrailPoint point, Vector3 direction, float uv, int lastListVertexIndex, Vector3 currPos)
        {
            float lifePercent = (Time.timeSinceLevelLoad - point.creationTime) / lifetime;
            float halfWidth = unit;// (1 - lifePercent) * unit;
                                   //剩余时间越长，那么此值越小
            float normalStrength = 1 - lifePercent;
            Vector2 dir = new Vector2();
            dir.x = direction.x * 0.5f + 0.5f;
            dir.y = direction.z * 0.5f + 0.5f;
            float currTime = Time.timeSinceLevelLoad;
            //生命未结束时，使用老的出生时间
            if(Time.timeSinceLevelLoad - point.creationTime < lifetime)
            {
                currTime = point.creationTime;
            }
            float lifeLoopNum = Mathf.FloorToInt(currTime / GpuLeafManager.lifetime);
            float remainder = currTime - lifeLoopNum * GpuLeafManager.lifetime;
            currTime = remainder / GpuLeafManager.lifetime;
            //Debug.LogError("====AddPoint,lifeLoopNum/255f:" + (lifeLoopNum / GpuLeafManager.colorRatio) + " :" + currTime + " Time:" + Time.timeSinceLevelLoad);
            Color normalStrengthColor = new Color(dir.x, currTime, dir.y, lifeLoopNum / GpuLeafManager.colorRatio);
            Color normalStrengthColor2 = new Color(dir.x, currTime, dir.y, lifeLoopNum / GpuLeafManager.colorRatio);
            //Color normalStrengthColor = new Color(dir.x, normalStrength * 0.9f, dir.y, 1f);
            //Color normalStrengthColor2 = new Color(dir.x, normalStrength * 0.2f, dir.y, 1f);
            Vector3 pos = point.pos;
            Vector3 right = Vector3.Cross(upDir, direction);

            //Debug.LogError("====pos:" + pos + "upDir:" + upDir + "direction:" + direction + "right:" + right);
            vertices[verticesIndex] = (pos - 2*right * halfWidth);//0
            verticesIndex++;
            vertices[verticesIndex] = (pos - right * halfWidth);//1
            //Debug.LogError("p1:" + vertices[verticesIndex - 1] + "p2:" + vertices[verticesIndex]);
            verticesIndex++;
            vertices[verticesIndex] = (pos + right * halfWidth);//2
            verticesIndex++;
            vertices[verticesIndex] = (pos + 2*right * halfWidth);//3
            //Debug.LogError("p3:" + vertices[verticesIndex - 1] + "p4:" + vertices[verticesIndex]);
            verticesIndex++;

            colors[colorsIndex] = (normalStrengthColor2);
            colorsIndex++;
            colors[colorsIndex] = (normalStrengthColor);
            colorsIndex++;
            colors[colorsIndex] = (normalStrengthColor);
            colorsIndex++;
            colors[colorsIndex] = (normalStrengthColor2);
            colorsIndex++;
            
            //不应当是verticesIndex - 1，修改为verticesIndex，因为verticesIndex从0开始
            //改：需要-1，因为上边执行++了
            int lastVert = verticesIndex - 1 - lastListVertexIndex;
            if (lastVert >= 7 )
            {
                int realVerticesIndex = verticesIndex - 1;
                triangles[trianglesIndex] = (realVerticesIndex - 3);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 2);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 6);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 6);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 7);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 3);
                trianglesIndex++;

                triangles[trianglesIndex] = (realVerticesIndex - 2);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 1);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 5);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 5);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 6);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 2);
                trianglesIndex++;

                triangles[trianglesIndex] = (realVerticesIndex - 1);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 4);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 4);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 5);
                trianglesIndex++;
                triangles[trianglesIndex] = (realVerticesIndex - 1);
                trianglesIndex++;
            }
        }

        void DrawFootCircle(Vector3 pos, Transform tr, float timeDelta)
        {
            if (true)
            {
                return;
            }
            vertices[verticesIndex] = (pos );//0
            verticesIndex++;
            Vector3 leftDis = -tr.right * footCircleSize;
            Vector3 forwardDis = tr.forward * footCircleSize;
            vertices[verticesIndex] = (pos + leftDis);//1
            verticesIndex++;
            vertices[verticesIndex] = (pos + forwardDis);//2
            verticesIndex++;
            vertices[verticesIndex] = (pos - leftDis);//3
            verticesIndex++;
            vertices[verticesIndex] = (pos - forwardDis);//4
            verticesIndex++;

            float currTime = Time.timeSinceLevelLoad / GpuLeafManager.colorRatio;

            Color centerColor = new Color(footCircleSizeColor, currTime, footCircleSizeColor, 1f);

            Vector2 dir1 = new Vector2();
            dir1.x = leftDis.x * 0.5f + 0.5f;
            dir1.y = leftDis.z * 0.5f + 0.5f;
            Color color1 = new Color(dir1.x, currTime, dir1.y, 1f);

            Vector2 dir2 = new Vector2();
            dir2.x = forwardDis.x * 0.5f + 0.5f;
            dir2.y = forwardDis.z * 0.5f + 0.5f;
            Color color2 = new Color(dir2.x, currTime, dir2.y, 1f);

            Vector2 dir3 = new Vector2();
            dir3.x = -leftDis.x * 0.5f + 0.5f;
            dir3.y = -leftDis.z * 0.5f + 0.5f;
            Color color3 = new Color(dir3.x, currTime, dir3.y, 1f);

            Vector2 dir4 = new Vector2();
            dir4.x = -forwardDis.x * 0.5f + 0.5f;
            dir4.y = -forwardDis.z * 0.5f + 0.5f;
            //Color color4 = new Color(dir4.x, 0.2f, dir4.y, 1f);
            Vector2 middleV = new Vector2();
            middleV.x = Mathf.Lerp(dir2.x, dir4.x, 0.3f);
            middleV.y = Mathf.Lerp(dir2.y, dir4.y, 0.3f);
            Color color4 = new Color(Mathf.Lerp(dir2.x, dir4.x, timeDelta), currTime, Mathf.Lerp(dir2.y, dir4.y, timeDelta), 1f);

            colors[colorsIndex] = centerColor;
            colorsIndex++;
            colors[colorsIndex] = (color1);
            colorsIndex++;
            colors[colorsIndex] = (color2);
            colorsIndex++;
            colors[colorsIndex] = (color3);
            colorsIndex++;
            colors[colorsIndex] = (color4);
            colorsIndex++;

            int realVerticesIndex = verticesIndex - 1;
            triangles[trianglesIndex] = (realVerticesIndex);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 3);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 4);
            trianglesIndex++;

            triangles[trianglesIndex] = (realVerticesIndex - 3);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 2);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 4);
            trianglesIndex++;

            triangles[trianglesIndex] = (realVerticesIndex - 2);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 1);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 4);
            trianglesIndex++;

            triangles[trianglesIndex] = (realVerticesIndex - 1);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex);
            trianglesIndex++;
            triangles[trianglesIndex] = (realVerticesIndex - 4);
            trianglesIndex++;
        }

        /// <summary>
        /// 增加运动轨迹相机
        /// </summary>
        /// <param name="c"></param>
        public static void AddMovePathCamera(MovePathCamera mpc)
        {
            if( null != mpc)
            {
                cList.Add(mpc);
            }
        }
        /// <summary>
        /// 设置shader属性
        /// </summary>
        public void SetShaderProperty()
        {
            if( null != leafMaterial)
            {
                if( null != cList && cList.Count > 0)
                {
                    MovePathCamera mpc = cList[0];
                    if(null != mpc)
                    {
                        Matrix4x4 matVP;
                        Camera c = mpc.GetCamera();
                        if(c != null)
                        {
                            matVP = GL.GetGPUProjectionMatrix(c.projectionMatrix, true) * c.worldToCameraMatrix;
                            leafMaterial.SetMatrix(ShaderPropty2ID.MovePathCameraMatrixVP, matVP);
                            RenderTexture rt = mpc.GetCameraRT();
                            if( null != rt)
                            {
                                leafMaterial.SetTexture(mpc.shaderProperty, rt);
                            }else
                            {
#if GAMEDEBUG
                                Games.TLBB.Log.LogSystem.Error("=========GpuLeafManager,SetShaderProperty rt is null.");
#endif
                            }
                        }
                        else
                        {
#if GAMEDEBUG
                            Games.TLBB.Log.LogSystem.Error("=========GpuLeafManager,SetShaderProperty c is null.");
#endif
                        }
                    }
                    else
                    {
#if GAMEDEBUG
                    Games.TLBB.Log.LogSystem.Error("=========GpuLeafManager,SetShaderProperty mpc is null.");
#endif
                    }
                }
                else
                {
#if GAMEDEBUG
                    Games.TLBB.Log.LogSystem.Error("=========GpuLeafManager,SetShaderProperty cList is null.");
#endif
                }
                leafMaterial.SetFloat(ShaderPropty2ID.LifeLoopInterval, GpuLeafManager.lifetime);
                leafMaterial.SetFloat(ShaderPropty2ID.ParticalYOff, GpuLeafManager.yOff);
            }
            else
            {
#if GAMEDEBUG
                Games.TLBB.Log.LogSystem.Error("=========GpuLeafManager,SetShaderProperty leafMaterial is null.");
#endif
            }
        }
    }

    [Serializable]
    public struct TrailPoint
    {
        public Vector3 pos;
        public float creationTime;

        public TrailPoint(Vector3 pos, float creationTime)
        {
            this.pos = pos;
            //this.creationTime = creationTime > 0 ? creationTime : 0.1f;
            this.creationTime = creationTime;
        }
    }

    public class ElementData
    {
        public Transform parentTrans;
        public bool enabled = true;//是否是激活状态
        public bool used = false;//是否使用状态
        public List<TrailPoint> points = new List<TrailPoint>(GpuLeafManager.singlePointsMaxNum);

        //是否只剩余了一个点
        public bool isOnlyOne = false;
        public float onlyOneTime = 0;
    }

    //单层RT上的mesh数据
    public class RTMeshData
    {
        private int verticesIndex = 0;
        private int colorsIndex = 0;
        private int trianglesIndex = 0;
        public Vector3[] vertices;
        public int[] triangles;
        public Color[] colors;
        public Mesh mesh;

        public RTMeshData(int elementBufferCount, int singlePointsMaxNum)
        {
            vertices = new Vector3[elementBufferCount * singlePointsMaxNum * 4];
            triangles = new int[elementBufferCount * singlePointsMaxNum * 12];//elementBufferCount * singlePointsMaxNum * 4 / 2 * 6
            colors = new Color[elementBufferCount * singlePointsMaxNum * 4];
        }
    }
}
