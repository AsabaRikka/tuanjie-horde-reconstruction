using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

public static class HordeIP
{
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static bool? available;
    public static bool Available=>available??(available=Resources.Load<TextAsset>("IP/enemy")!=null).Value;
    public static Mesh Mesh(string role)
    {
        if(meshes.TryGetValue(role,out var m))return m;
        var bytes=Resources.Load<TextAsset>("IP/"+role);if(bytes==null)return null;
        using(var r=new BinaryReader(new MemoryStream(bytes.bytes)))
        {
            if(r.ReadInt32()!=0x49503535)throw new System.Exception("IP asset format");int n=r.ReadInt32();var v=new Vector3[n];var normals=new Vector3[n];var c=new Color[n];var uv=new Vector2[n];var part=new Vector2[n];var pivots=new List<Vector3>(n);
            for(int i=0;i<n;i++)v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)normals[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)c[i]=new Color(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)part[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<n;i++)pivots.Add(new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()));
            var ids=new int[r.ReadInt32()];for(int i=0;i<ids.Length;i++)ids[i]=r.ReadInt32();
            m=new Mesh{name="IP "+role,indexFormat=IndexFormat.UInt32};m.vertices=v;m.normals=normals;m.colors=c;m.uv=uv;m.uv2=part;m.SetUVs(2,pivots);m.triangles=ids;m.RecalculateTangents();m.RecalculateBounds();m.bounds=new Bounds(new Vector3(0,.95f,0),new Vector3(4,4,4));m.Optimize();meshes[role]=m;
        }
        return m;
    }
    public static Material Material(string role)
    {
        if(materials.TryGetValue(role,out var m))return m;
        bool car=role=="starter" || role=="heavy";
        m=new Material(Shader.Find("Horde/IP Actor")){name="IP textured "+role,enableInstancing=true};m.SetFloat("_Role",car?2:role=="boss"?1:0);m.SetFloat("_Textured",car?0:1);
        if(!car){string p=role=="enemy_far"?"enemy":role;foreach(var pair in new[]{new[]{"_BaseMap","BaseColor"},new[]{"_NormalMap","Normal"},new[]{"_RoughMap","Roughness"},new[]{"_MetalMap","Metallic"}})m.SetTexture(pair[0],Resources.Load<Texture2D>("IP/"+p+"_"+pair[1]));m.SetFloat("_NormalStrength",0);}
        if(role=="enemy_far")m.SetFloat("_CrowdAmbient",.78f);
        if(role.StartsWith("gate")){m.SetFloat("_Role",3);m.SetFloat("_Emission",.14f);}
        materials[role]=m;return m;
    }
    public static Vector3 Scale(string role)=>role=="enemy" || role=="enemy_far"?new Vector3(.69f,1,.74f):(role=="boss" || role.StartsWith("gate")?Vector3.one:Vector3.one*.60f);
    public static GameObject Create(string role,Transform parent)
    {
        var go=new GameObject("IP "+role);go.transform.SetParent(parent,false);go.transform.localScale=Scale(role);go.AddComponent<MeshFilter>().sharedMesh=Mesh(role);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=Material(role);HordeContactShadows.Register(go.transform,role);return go;
    }
    public static void Corpse(Vector3 position,Quaternion rotation,Vector3 direction)
    {
        var go=Create("enemy",null);go.name="Frozen jiangshi corpse";go.transform.position=position;go.transform.rotation=rotation;
        var c=go.AddComponent<CapsuleCollider>();c.radius=.28f;c.height=1.75f;c.center=Vector3.up*.875f;go.layer=HordeDirector.CorpseLayer;
        var rb=go.AddComponent<Rigidbody>();rb.mass=60;rb.velocity=direction*2.2f+Vector3.up*.45f;rb.angularVelocity=go.transform.right*2.8f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        go.AddComponent<IPCorpseFade>();Object.Destroy(go,.72f);
    }
}
public class IPActorView:MonoBehaviour
{
    string role;Renderer visual;MaterialPropertyBlock block;float phase,move,kick,flash,attack,duck,steer,steerVelocity;bool dead;Vector4 variation=Vector4.one;
    public Transform muzzle;
    public void Setup(string type)
    {
        role=type;var go=HordeIP.Create(type,transform);visual=go.GetComponent<Renderer>();block=new MaterialPropertyBlock();
        if(type=="enemy"){
            Vector3 p=transform.position;var mob=GetComponentInParent<HordeMob>();float identity=mob==null?0:mob.id;
            float seed=Mathf.Repeat(Mathf.Sin(p.x*31.7f+p.z*17.13f+identity*37.731f)*43758.5453f,1);
            go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(.94f+seed*.10f,.88f+seed*.22f,.94f+seed*.1f));
            go.transform.localRotation=Quaternion.Euler(0,(seed-.5f)*18,0);
            variation=new Vector4(.83f+seed*.18f,.88f+seed*.12f,.92f+seed*.08f,seed);
            block.SetVector("_Variation",variation);visual.SetPropertyBlock(block);
        }
        if(type=="starter" || type=="heavy"){var point=new GameObject("Muzzle");point.transform.SetParent(go.transform,false);point.transform.localPosition=new Vector3(0,1.205f,.85f);muzzle=point.transform;}
    }
    public void Trigger(string name){if(name=="Death")dead=true;}
    public void Pose(Vector3 velocity,float distance,float recoil,float hit,float a,float d)
    {
        if(dead)return;
        phase=distance/(role=="boss"?3.6f:role=="enemy"?1.12f:.848f)*Mathf.PI*2;move=Mathf.Clamp01(new Vector2(velocity.x,velocity.z).magnitude*2);kick=recoil;flash=hit;attack=a;duck=d;
        if(role=="starter" || role=="heavy")steer=Mathf.SmoothDamp(steer,Mathf.Clamp(velocity.x/8,-1,1)*.16f,ref steerVelocity,.10f);
        block.SetVector("_Variation",variation);block.SetVector("_Anim",new Vector4(phase,move,kick,attack));block.SetVector("_Pose",new Vector4(duck,flash,0,steer));visual.SetPropertyBlock(block);
    }
}
public class IPCorpseFade:MonoBehaviour
{
    float born;Renderer visual;MaterialPropertyBlock block;
    void Awake(){born=Time.time;visual=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
    void Update(){float age=Time.time-born;block.SetVector("_Pose",new Vector4(0,Mathf.Max(0,1-age*5),Mathf.Clamp01((age-.32f)/.4f),0));visual.SetPropertyBlock(block);}
}
