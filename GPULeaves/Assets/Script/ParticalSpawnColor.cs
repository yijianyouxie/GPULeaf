using UnityEngine;
using System.Collections;

public class ParticalSpawnColor : MonoBehaviour {

    public Material mat = null;
    private Mesh mesh;

    private static string LAYER_GPULEAF = "GPULeaf"; //8
    private int layer;

    //粒子数量
    //private int particalNum = 25;
    private int particalNum = 2;
    private Vector3[] verts = null;
    private Color[] colors = null;
    private Vector2[] uvs = null;
    private int[] triangle = null;
    private Vector3 initPoint = new Vector3(0, 0, 0);
    private float interval = 0.6f;
    private int column = 5;
    //位置坐标除以此值作为颜色值进行存储
    private float colorRatio = 10000;

	// Use this for initialization
	void Start () {
        layer = LayerMask.NameToLayer(ParticalSpawnColor.LAYER_GPULEAF);
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

        GenerateMesh();
	}

    private void GenerateMesh()
    {
        for(int i = 0;i < particalNum; i++)
        {
            Vector3 v = new Vector3(initPoint.x + i % column * interval, initPoint.y, initPoint.z + i/column * interval);
            int baseIndex = i * 4;
            Vector3 v0 = new Vector3(v.x - 0.5f, initPoint.y, v.z + 0.5f);
            Vector3 v1 = new Vector3(v.x + 0.5f, initPoint.y, v.z + 0.5f);
            Vector3 v2 = new Vector3(v.x + 0.5f, initPoint.y, v.z - 0.5f);
            Vector3 v3 = new Vector3(v.x - 0.5f, initPoint.y, v.z - 0.5f);
            //verts[baseIndex + 0] = v;
            //verts[baseIndex + 1] = v;
            //verts[baseIndex + 2] = v;
            //verts[baseIndex + 3] = v;
            verts[baseIndex + 0] = v0;
            verts[baseIndex + 1] = v1;
            verts[baseIndex + 2] = v2;
            verts[baseIndex + 3] = v3;

            colors[baseIndex + 0] = new Color(v.x / colorRatio, v.y / colorRatio, v.z / colorRatio, 1);
            colors[baseIndex + 1] = new Color(v.x / colorRatio, v.y / colorRatio, v.z / colorRatio, 1);
            colors[baseIndex + 2] = new Color(v.x / colorRatio, v.y / colorRatio, v.z / colorRatio, 1);
            colors[baseIndex + 3] = new Color(v.x / colorRatio, v.y / colorRatio, v.z / colorRatio, 1);

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
        //绘制mesh
        Graphics.DrawMesh(mesh, Matrix4x4.identity, mat, layer);
    }
}
