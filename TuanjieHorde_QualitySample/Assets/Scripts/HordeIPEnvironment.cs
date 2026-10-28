using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class HordeIPEnvironment:MonoBehaviour
{
    public static HordeIPEnvironment Instance;
    Mesh tree;Material foliage;Matrix4x4[] trees;readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
    public static void Create(){Instance=new GameObject("IP winter bridge environment").AddComponent<HordeIPEnvironment>();Instance.Build(true);}
    public static void CreateTreesOnly(){Instance=new GameObject("Instanced background pines").AddComponent<HordeIPEnvironment>();Instance.Build(false);}
    void Build(bool includeDeck)
    {
        if(includeDeck){
        var road=HordeMeshes.Mat("Cold asphalt",new Color(.24f,.27f,.32f));road.SetFloat("_VertexColors",0);
        var snow=HordeMeshes.Mat("Blue forest floor",new Color(.32f,.45f,.59f));snow.SetFloat("_VertexColors",0);
        var concrete=HordeMeshes.Mat("Frosted parapet",new Color(.40f,.52f,.61f),.25f);concrete.SetFloat("_VertexColors",0);
        HordeMeshes.Box("Asphalt deck",new Vector3(0,-.10f,54),new Vector3(18,.18f,180),road,false,transform);
        foreach(int side in new[]{-1,1})
        {
            HordeMeshes.Box("Forest terrain",new Vector3(side*35,-.7f,55),new Vector3(52,.6f,190),snow,false,transform);
            HordeMeshes.Box("Bridge parapet",new Vector3(side*9.20f,.38f,54),new Vector3(.38f,.8f,180),concrete,false,transform);
            HordeMeshes.Box("Parapet highlight",new Vector3(side*9.20f,.80f,54),new Vector3(.42f,.09f,180),snow,false,transform);
            HordeMeshes.Box("Hazard rail",new Vector3(side*8.96f,.025f,54),new Vector3(.16f,.03f,180),HordeMeshes.Black,false,transform);
            for(int z=-32;z<145;z+=2){var stripe=HordeMeshes.Box("Yellow edge marking",new Vector3(side*8.96f,.047f,z),new Vector3(.17f,.025f,.84f),HordeMeshes.Gold,false,transform);stripe.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        }
        // Fine asphalt grain, generated once and sampled with world coordinates in the vertex material.
        road.SetFloat("_SurfaceGrain",.12f);concrete.SetFloat("_SurfaceGrain",.035f);
        }
        var v=new List<Vector3>();var c=new List<Color>();var triangles=new List<int>();
        var rng=new System.Random(785);
        System.Action<Vector3,Vector3,float,Color> branch=(basePoint,tip,radius,color)=>{
            Vector3 axis=(tip-basePoint).normalized,right=Vector3.Cross(axis,Vector3.right).normalized;if(right.sqrMagnitude<.1f)right=Vector3.forward;Vector3 up=Vector3.Cross(axis,right);
            int start=v.Count;for(int j=0;j<7;j++){float a=j*Mathf.PI*2/7;v.Add(basePoint+(right*Mathf.Cos(a)+up*Mathf.Sin(a))*radius);c.Add(color*(.86f+(float)rng.NextDouble()*.27f));}v.Add(tip);c.Add(color*1.15f);
            for(int j=0;j<7;j++){triangles.Add(start+j);triangles.Add(start+(j+1)%7);triangles.Add(start+7);}
        };
        for(int tier=0;tier<9;tier++)
        {
            float y=.50f+tier*.34f;float radius=(1-tier/10f)*1.0f;
            for(int j=0;j<9;j++){float a=j*Mathf.PI*2/9+tier*.67f;Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 end=direction*radius+Vector3.up*(y-.15f);branch(Vector3.up*(y+.16f),end,.22f,new Color(.13f,.29f,.39f));
                branch(end-direction*.22f+Vector3.up*.08f,end+Vector3.up*.3f,.22f,new Color(.38f,.54f,.69f));}
        }
        branch(Vector3.up*3,Vector3.up*4,.3f,new Color(.36f,.53f,.67f));
        tree=new Mesh{name="Branched frost pine"};tree.SetVertices(v);tree.SetColors(c);tree.SetTriangles(triangles,0);tree.RecalculateNormals();tree.RecalculateBounds();foliage=HordeMeshes.Mat("Winter pine vertex colour",Color.white);foliage.enableInstancing=true;
        var instances=new List<Matrix4x4>();for(int side=-1;side<=1;side+=2)for(int row=0;row<54;row++)for(int col=0;col<5;col++)
        {
            float x=side*(12.0f+col*3.2f+(float)rng.NextDouble()*2.2f);float z=-30+row*3.2f+(float)rng.NextDouble()*2.6f;float scale=.55f+(float)rng.NextDouble()*.55f;
            instances.Add(Matrix4x4.TRS(new Vector3(x,-.25f,z),Quaternion.Euler(0,(float)rng.NextDouble()*360,0),Vector3.one*scale));
        }
        trees=instances.ToArray();
    }
    public void Draw(){if(tree!=null)Graphics.DrawMeshInstanced(tree,0,foliage,trees,trees.Length,block,ShadowCastingMode.On,true);}
}
