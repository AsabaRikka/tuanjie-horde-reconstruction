using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

public static class HordeMeshes
{
    public static Material Skin, Metal, Blue, Gold, Ice, Black;
    static Dictionary<string,Mesh> source=new Dictionary<string,Mesh>();
    public static Mesh[] Walk,BossWalk,HeavyRecoil,StarterRecoil;
    public static Mesh[] BodyParts;
    public static readonly Vector3[] Pivots={new Vector3(0,1.12f,0),new Vector3(0,1.61f,0),new Vector3(-.32f,1.22f,-.08f),new Vector3(.32f,1.22f,-.08f),new Vector3(-.15f,.64f,0),new Vector3(.15f,.64f,0),new Vector3(-.17f,.22f,0),new Vector3(.17f,.22f,0)};
    public static Material Mat(string name,Color color,float metal=0)
    {
        Material m=new Material(Shader.Find("Horde/Vertex Lit"));m.name=name;m.SetColor("_Tint",color);m.SetFloat("_Metallic",metal);m.enableInstancing=true;return m;
    }
    public static void Init()
    {
        if(Skin!=null && Walk!=null)return;
        Skin=Mat("Character vertex colours",Color.white);Metal=Mat("Brushed gold steel",new Color(.90f,.80f,.022f),.88f);Metal.SetFloat("_VertexColors",0);
        Blue=Mat("Blue gate",new Color(.04f,.42f,1));Gold=Mat("Golden gate",new Color(1,.71f,.035f));
        Gold.SetFloat("_VertexColors",0);
        Ice=Mat("Ice impact",new Color(.65f,1,1));Ice.SetFloat("_Flash",.8f);Black=Mat("Dark recess",new Color(.04f,.05f,.07f));
        if(HordeIP.Available){Walk=new Mesh[0];return;}
        var mesh=Load("enemy");Walk=new Mesh[24];Vector3[] rest=mesh.vertices;
        for(int f=0;f<Walk.Length;f++)
        {
            var pose=(Mesh)Object.Instantiate(mesh);pose.name="Enemy stride "+f;Vector3[] v=new Vector3[rest.Length];float phase=f*Mathf.PI*2/Walk.Length;
            for(int i=0;i<v.Length;i++)
            {
                Vector3 p=rest[i];float side=p.x<0?-1:1;float sn=Mathf.Sin(phase+(side<0?0:Mathf.PI));
                if(p.y<.82f)
                {
                    Vector3 hip=new Vector3(side*.15f,.8f,0),knee=new Vector3(side*.16f,.43f,0);
                    if(p.y<.45f)p=knee+Quaternion.Euler(Mathf.Max(0,-sn)*34,0,0)*(p-knee);
                    p=hip+Quaternion.Euler(sn*26,0,0)*(p-hip);
                }
                else if(Mathf.Abs(p.x)>.27f && p.y<1.48f)
                {Vector3 shoulder=new Vector3(side*.27f,1.31f,.02f);p=shoulder+Quaternion.Euler(-sn*13,0,side*2)*(p-shoulder);}
                else if(p.y>1.45f){Vector3 neck=new Vector3(0,1.42f,.03f);p=neck+Quaternion.Euler(0,Mathf.Sin(phase)*3,0)*(p-neck);}
                p.y+=Mathf.Abs(Mathf.Sin(phase))*.025f;v[i]=p;
            }
            pose.vertices=v;pose.RecalculateNormals();pose.RecalculateBounds();Walk[f]=pose;
        }
        BossWalk=new Mesh[24];var giant=Load("boss");var giantRest=giant.vertices;
        for(int f=0;f<BossWalk.Length;f++)
        {
            var pose=Object.Instantiate(giant);pose.name="Giant stride "+f;var v=new Vector3[giantRest.Length];float angle=f*Mathf.PI*2/BossWalk.Length;
            for(int i=0;i<v.Length;i++)
            {
                Vector3 p=giantRest[i];float side=p.x<0?-1:1;float swing=Mathf.Sin(angle+(side<0?0:Mathf.PI));
                // The coat sways; legs and exposed arms follow a separate stride.
                if(p.y<.80f && (Mathf.Abs(p.x)<.25f || p.y<.20f)){var pivot=new Vector3(side*.14f,.8f,0);p=pivot+Quaternion.Euler(swing*19,0,0)*(p-pivot);}
                else if(p.y<.9f)p.z+=swing*(.9f-p.y)*.06f;
                else if(Mathf.Abs(p.x)>.28f && p.y<1.45f){var pivot=new Vector3(side*.27f,1.31f,.02f);p=pivot+Quaternion.Euler(-swing*13,0,0)*(p-pivot);}
                else if(p.y>1.45f){var pivot=new Vector3(0,1.42f,0);p=pivot+Quaternion.Euler(2,Mathf.Sin(angle)*3,0)*(p-pivot);}
                p.y+=Mathf.Abs(Mathf.Sin(angle))*.018f;v[i]=p;
            }
            pose.vertices=v;pose.RecalculateNormals();pose.RecalculateBounds();BossWalk[f]=pose;
        }
        HeavyRecoil=RecoilPoses(Load("heavy"));StarterRecoil=RecoilPoses(Load("starter"));
        BodyParts=Partition(mesh);
    }
    static Mesh[] RecoilPoses(Mesh mesh)
    {
        var poses=new Mesh[8];var rest=mesh.vertices;
        for(int f=0;f<poses.Length;f++)
        {
            var pose=Object.Instantiate(mesh);pose.name=mesh.name+" recoil "+f;var v=new Vector3[rest.Length];float force=f/7f;
            for(int j=0;j<v.Length;j++){var p=rest[j];float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1.10f,p.y));p.z-=weight*force*.075f;p.y+=weight*force*.015f;v[j]=p;}
            pose.vertices=v;pose.RecalculateNormals();pose.RecalculateBounds();poses[f]=pose;
        }
        return poses;
    }
    public static Mesh Load(string name)
    {
        if(source.TryGetValue(name,out var cached))return cached;
        var data=Resources.Load<TextAsset>("HordeMeshes/"+name);if(data==null)throw new System.Exception("Missing mesh "+name);
        using(var r=new BinaryReader(new MemoryStream(data.bytes)))
        {
            if(r.ReadInt32()!=0x48524433)throw new System.Exception("Mesh format mismatch");int n=r.ReadInt32();var v=new Vector3[n];var norm=new Vector3[n];var c=new Color[n];
            for(int i=0;i<n;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)norm[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)c[i]=new Color(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            var t=new int[r.ReadInt32()];for(int i=0;i<t.Length;i++)t[i]=r.ReadInt32();
            Mesh m=new Mesh {name=name,indexFormat=IndexFormat.UInt32};m.vertices=v;m.normals=norm;m.colors=c;m.triangles=t;m.RecalculateBounds();if(name=="heavy")m=StandingHeavy(m,Load("starter"));source[name]=m;return m;
        }
    }
    static Mesh StandingHeavy(Mesh top,Mesh legs)
    {
        var v=new List<Vector3>();var c=new List<Color>();var ids=new List<int>();
        for(int pass=0;pass<2;pass++)
        {
            Mesh mesh=pass==0?top:legs;var vs=mesh.vertices;var cs=mesh.colors;var ts=mesh.triangles;
            for(int i=0;i<ts.Length;i+=3)
            {
                float y=(vs[ts[i]].y+vs[ts[i+1]].y+vs[ts[i+2]].y)/3;
                if(pass==0?y<.82f:y>=.82f)continue;
                for(int k=0;k<3;k++){ids.Add(v.Count);v.Add(vs[ts[i+k]]);c.Add(cs[ts[i+k]]);}
            }
        }
        var result=new Mesh{name="Standing heavy gunner",indexFormat=IndexFormat.UInt32};result.SetVertices(v);result.SetColors(c);result.SetTriangles(ids,0);result.RecalculateNormals();result.RecalculateBounds();return result;
    }
    static int Part(Vector3 p)
    {
        if(p.y>1.45f)return 1;
        if(p.y<.45f)return p.x<0?6:7;
        if(p.y<.82f)return p.x<0?4:5;
        if(Mathf.Abs(p.x)>.275f && p.y<1.45f)return p.x<0?2:3;
        return 0;
    }
    static Mesh[] Partition(Mesh mesh)
    {
        Mesh[] result=new Mesh[8];Vector3[] v=mesh.vertices;Color[] colors=mesh.colors;int[] ts=mesh.triangles;
        for(int j=0;j<8;j++)
        {
            var vv=new List<Vector3>();var cc=new List<Color>();var tt=new List<int>();
            for(int i=0;i<ts.Length;i+=3)
            {
                if(Part((v[ts[i]]+v[ts[i+1]]+v[ts[i+2]])/3)!=j)continue;
                for(int k=0;k<3;k++){tt.Add(vv.Count);vv.Add(v[ts[i+k]]-Pivots[j]);cc.Add(colors[ts[i+k]]);}
            }
            var m=new Mesh{name="Ragdoll part "+j};m.SetVertices(vv);m.SetColors(cc);m.SetTriangles(tt,0);m.RecalculateNormals();m.RecalculateBounds();result[j]=m;
        }
        return result;
    }
    public static GameObject Model(string name,Mesh mesh,Vector3 pos,Material mat,Transform parent=null)
    {
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=pos;
        o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;return o;
    }
    public static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,bool collider=true,Transform parent=null)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;
        o.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)Object.Destroy(o.GetComponent<Collider>());return o;
    }
}
