using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class HordeFracture:MonoBehaviour
{
    public Vector3 size;public string resource;int hitCount;Material cracks;
    struct Piece{public Mesh mesh;public Vector3 center;public float mass;}
    static readonly Dictionary<string,Piece[]> cache=new Dictionary<string,Piece[]>();
    public void Damage(Vector3 worldPoint,int stage)
    {
        if(stage<=hitCount)return;hitCount=stage;
        if(cracks==null){cracks=HordeMeshes.Mat("Dark gold fracture interior",new Color(.10f,.058f,.009f));cracks.SetFloat("_VertexColors",0);cracks.SetFloat("_Unlit",.85f);}
        var rng=new System.Random(731+stage*93);Vector3 center=transform.InverseTransformPoint(worldPoint);center.x=Mathf.Clamp(center.x,-size.x*.35f,size.x*.35f);center.y=Mathf.Clamp(center.y,-size.y*.36f,size.y*.36f);center.z=-size.z*.5f-.011f;
        for(int k=0;k<7+stage*3;k++){
            float angle=k*Mathf.PI*2/(7+stage*3);Vector3 last=center;var points=new List<Vector3>{last};
            for(int j=1;j<7;j++){
                float radius=j/6f;Vector3 p=center+new Vector3(Mathf.Cos(angle)*size.x*(.40f+stage*.13f)*radius,Mathf.Sin(angle)*size.y*(.24f+stage*.08f)*radius,0);
                p.x+=(float)(rng.NextDouble()-.5)*.18f;p.y+=(float)(rng.NextDouble()-.5)*.18f;p.x=Mathf.Clamp(p.x,-size.x*.495f,size.x*.495f);p.y=Mathf.Clamp(p.y,-size.y*.495f,size.y*.495f);points.Add(p);
                if(j==3 || j==5){var twig=p+new Vector3((float)(rng.NextDouble()-.5)*.35f,(float)(rng.NextDouble()-.5)*.45f,0);twig.x=Mathf.Clamp(twig.x,-size.x*.495f,size.x*.495f);twig.y=Mathf.Clamp(twig.y,-size.y*.495f,size.y*.495f);Line(new[]{p,twig},.013f);}
                last=p;
            }
            Line(points.ToArray(),.019f+stage*.004f);
        }
    }
    void Line(Vector3[] points,float width){var go=new GameObject("Branching stress crack");go.transform.SetParent(transform,false);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=cracks;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=width;l.endWidth=.006f;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
    static Piece[] Pieces(string name)
    {
        if(cache.TryGetValue(name,out var found))return found;
        var bytes=Resources.Load<TextAsset>("Fractures/"+name);using(var reader=new BinaryReader(new MemoryStream(bytes.bytes))){
            var pieces=new Piece[reader.ReadInt32()];for(int i=0;i<pieces.Length;i++){
                Vector3 center=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());float volume=reader.ReadSingle();int count=reader.ReadInt32();var v=new Vector3[count];var normals=new Vector3[count];var ids=new int[count];
                for(int k=0;k<count;k++)v[k]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());for(int k=0;k<count;k++){normals[k]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());ids[k]=k;}
                var m=new Mesh{name="Fractured gold cell"};m.vertices=v;m.normals=normals;m.triangles=ids;m.RecalculateBounds();pieces[i]=new Piece{mesh=m,center=center,mass=Mathf.Clamp(volume*80,2,28)};
            }cache[name]=pieces;return pieces;
        }
    }
    public static void Break(string resource,Vector3 center)
    {
        int i=0;foreach(var piece in Pieces(resource)){
            var go=HordeMeshes.Model("Gold fracture chunk",piece.mesh,center+piece.center,HordeMeshes.Metal);go.layer=HordeDirector.CorpseLayer;
            var c=go.AddComponent<BoxCollider>();c.center=piece.mesh.bounds.center;c.size=piece.mesh.bounds.size*.85f;
            var rb=go.AddComponent<Rigidbody>();rb.mass=piece.mass;rb.drag=.28f;rb.angularDrag=.7f;rb.maxDepenetrationVelocity=.8f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var radial=new Vector3(piece.center.x,.12f,piece.center.z).normalized;rb.velocity=radial*(.7f+(i%5)*.10f)+Vector3.up*.18f;rb.angularVelocity=new Vector3((i%3)-1,.6f,(i%7)*.2f)*1.3f;
            go.AddComponent<GoldFragmentExpiry>();i++;
        }
    }
}
public class GoldFragmentExpiry:MonoBehaviour
{
    float born;Vector3 scale;
    void Start(){born=Time.time;scale=transform.localScale;}
    void Update(){float age=Time.time-born;if(age>1.8f){transform.localScale=scale*Mathf.Clamp01((2.4f-age)/.6f);if(age>2.4f)Destroy(gameObject);}}
}
