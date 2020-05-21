using UnityEngine;
using System.Collections;
using TLStudio.Shadow;

/// <summary>
/// 树叶生成器
/// </summary>
public class ParticleSpawn : MonoBehaviour {

    public Material mat = null;
    private Mesh mesh;

    private static string LAYER_GPULEAF = "GPULeaf"; //8
    private int layer;

    //粒子数量
    [Header("粒子数量")]
    public int particalNum = 1000;
    private Vector3[] verts = null;
    private Color[] colors = null;
    private Vector2[] uvs = null;
    private int[] triangle = null;
    private Vector3 initPoint = new Vector3(0f, 0f, 0f);
    //[Header("粒子的Y轴偏移，向上为正值")]
    //private float yOff = 20f;
    private float interval = 1.5f;
    private int column = 30;

    private struct Point
    {
        public float creationTime;

        public Point(float creationTime)
        {
            this.creationTime = creationTime;
        }
    }
    private Point[] points = null;

    // Use this for initialization
    void Start () {
        layer = LayerMask.NameToLayer(ParticleSpawn.LAYER_GPULEAF);
        //构造mesh
        //顶点
        verts = new Vector3[particalNum *4];
        //颜色
        colors = new Color[particalNum * 4];
        //uv
        uvs = new Vector2[particalNum *4];
        //三角形
        triangle = new int[particalNum * 6];

        mesh = new Mesh();

        points = new Point[particalNum];
        for(int i = 0; i < particalNum - 1; i++)
        {
            points[i] = new Point(Random.Range(Time.timeSinceLevelLoad - GpuLeafManager.lifetime, Time.timeSinceLevelLoad));
        }

        initPoint = this.transform.position;

        GenerateMesh();
	}

    private void OnDestroy()
    {
        verts = null;
        colors = null;
        uvs = null;
        triangle = null;

        points = null;

        DestroyImmediate(mesh);
        mesh = null;
    }

    private void GenerateMesh()
    {
        //Debug.LogError("====lifeLoopNum/255f:" + (lifeLoopNum / GpuLeafManager.colorRatio) + " currTime:" + currTime + " Time:" + Time.timeSinceLevelLoad);
        for (int i = 0;i < particalNum; i++)
        {
            Vector3 v = new Vector3(initPoint.x + i % column * interval, initPoint.y, initPoint.z + i/column * interval);
            int baseIndex = i * 4;
            //Vector3 v0 = new Vector3(v.x - 0.5f, initPoint.y, v.z + 0.5f);
            //Vector3 v1 = new Vector3(v.x + 0.5f, initPoint.y, v.z + 0.5f);
            //Vector3 v2 = new Vector3(v.x + 0.5f, initPoint.y, v.z - 0.5f);
            //Vector3 v3 = new Vector3(v.x - 0.5f, initPoint.y, v.z - 0.5f);
            verts[baseIndex + 0] = v;
            verts[baseIndex + 1] = v;
            verts[baseIndex + 2] = v;
            verts[baseIndex + 3] = v;
            //verts[baseIndex + 0] = v0;
            //verts[baseIndex + 1] = v1;
            //verts[baseIndex + 2] = v2;
            //verts[baseIndex + 3] = v3;

            float startTime = points[i].creationTime;
            float lifeLoopNum = Mathf.FloorToInt(startTime / GpuLeafManager.lifetime);
            float remainder = startTime - lifeLoopNum * GpuLeafManager.lifetime;
            float currTime = remainder / GpuLeafManager.lifetime;

            colors[baseIndex + 0] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
            colors[baseIndex + 1] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
            colors[baseIndex + 2] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
            colors[baseIndex + 3] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);

            uvs[baseIndex + 0] = new Vector2(0, 1);
            uvs[baseIndex + 1] = new Vector2(1, 1);
            uvs[baseIndex + 2] = new Vector2(1, 0);
            uvs[baseIndex + 3] = new Vector2(0, 0);

            int triangleIndex = i * 6;
            triangle[triangleIndex + 0] = baseIndex + 0;
            triangle[triangleIndex + 1] = baseIndex + 1;
            triangle[triangleIndex + 2] = baseIndex + 2;
            triangle[triangleIndex + 3] = baseIndex + 2;
            triangle[triangleIndex + 4] = baseIndex + 3;
            triangle[triangleIndex + 5] = baseIndex + 0;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangle;
    }
	
	// Update is called once per frame
	void Update () {
        if(null == mesh || null == mat)
        {
            Debug.LogError("===========mesh = " + mesh + "; mat = " + mat);
            return;
        }

        bool needGnerateMesh = false;
        for (int i = 0; i < particalNum - 1; i++)
        {
            float startTime = points[i].creationTime;
            //检测生成时间
            if (Time.timeSinceLevelLoad - startTime >= GpuLeafManager.lifetime)
            {
                points[i].creationTime = Time.timeSinceLevelLoad;

                int baseIndex = i * 4;

                startTime = points[i].creationTime;
                float lifeLoopNum = Mathf.FloorToInt(startTime / GpuLeafManager.lifetime);
                float remainder = startTime - lifeLoopNum * GpuLeafManager.lifetime;
                float currTime = remainder / GpuLeafManager.lifetime;

                colors[baseIndex + 0] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
                colors[baseIndex + 1] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
                colors[baseIndex + 2] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);
                colors[baseIndex + 3] = new Color(0, currTime, 0, lifeLoopNum / GpuLeafManager.colorRatio);

                needGnerateMesh = true;
            }
        }
        if (needGnerateMesh)
        {
            mesh.colors = colors;
        }

        //绘制mesh
        Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, layer);
    }
}
