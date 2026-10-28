using UnityEngine;

// Finite, soft-edged light elements; no gameplay RNG or collision state.
public class HordeUpgradeLight:MonoBehaviour
{
    float born,duration;Renderer[] renderers;MaterialPropertyBlock block;Vector3 scale;
    public static void Create(Vector3 p,bool recruit)
    {
        var root=new GameObject(recruit?"Recruit materialization":"Upgrade light crown");root.transform.position=p;
        var gold=Material(2,new Color(5.2f,1.50f,.35f,.9f));var disc=Material(1,new Color(4.5f,2.1f,.50f,1));var star=Material(3,new Color(4.7f,2.7f,.9f,1));
        float radius=recruit?1.10f:1.65f,height=recruit?3.1f:4.4f;
        var floor=new GameObject("Soft circular foot light");floor.transform.SetParent(root.transform,false);floor.transform.localPosition=Vector3.up*.06f;var mesh=new Mesh();
        mesh.vertices=new[]{new Vector3(-radius,0,-radius),new Vector3(-radius,0,radius),new Vector3(radius,0,radius),new Vector3(radius,0,-radius)};
        mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();floor.AddComponent<MeshFilter>().sharedMesh=mesh;floor.AddComponent<MeshRenderer>().sharedMaterial=disc;
        Line(root.transform,"Soft vertical crown",new[]{Vector3.up*.07f,Vector3.up*height},radius*2.0f,radius*1.20f,gold);
        for(int k=0;k<10;k++){
            float a=k*2.39996f;var v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius*.55f;float h=height*(.55f+.045f*k);
            Line(root.transform,"Rising gold filament",new[]{v+Vector3.up*.10f,v+Vector3.up*h},.055f,.008f,gold);
        }
        var points=new Vector3[72];for(int i=0;i<points.Length;i++){float a=i/(float)(points.Length-1)*Mathf.PI*4;points[i]=new Vector3(Mathf.Cos(a)*radius*.66f,.1f+i/(float)points.Length*height*.65f,Mathf.Sin(a)*radius*.66f);}
        Line(root.transform,"Spiral ascent",points,.018f,.008f,gold);
        for(int k=0;k<12;k++){
            float a=k*2.39996f;var v=new Vector3(Mathf.Cos(a)*radius*.85f,.3f+(k%5)*.48f,Mathf.Sin(a)*radius*.85f);
            Line(root.transform,"Four point glint",new[]{v,v+Vector3.up*.23f},.20f,.20f,star);
        }
        var effect=root.AddComponent<HordeUpgradeLight>();effect.duration=recruit?.50f:.72f;
    }
    static Material Material(float mode,Color color){var m=new Material(Shader.Find("Horde/Radiance"));m.SetFloat("_Mode",mode);m.SetColor("_BaseColor",color);return m;}
    static void Line(Transform root,string name,Vector3[] points,float from,float to,Material m){var go=new GameObject(name);go.transform.SetParent(root,false);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=m;l.positionCount=points.Length;l.SetPositions(points);l.startWidth=from;l.endWidth=to;l.textureMode=LineTextureMode.Stretch;}
    void Start(){born=Time.time;scale=transform.localScale;renderers=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();}
    void Update(){float t=(Time.time-born)/duration;float fade=Mathf.SmoothStep(1,0,Mathf.InverseLerp(.23f,1,t));transform.localScale=Vector3.Scale(scale,new Vector3(Mathf.Lerp(.85f,1.10f,t),Mathf.Lerp(.85f,1.15f,t),Mathf.Lerp(.85f,1.10f,t)));foreach(var r in renderers){Color c=r.sharedMaterial.GetColor("_BaseColor");c.a*=fade;block.SetColor("_BaseColor",c);r.SetPropertyBlock(block);}if(t>=1)Destroy(gameObject);}
    void OnDestroy(){if(renderers==null)return;var unique=new System.Collections.Generic.HashSet<Material>();foreach(var r in renderers)if(r!=null)unique.Add(r.sharedMaterial);foreach(var m in unique)Destroy(m);var mf=GetComponentInChildren<MeshFilter>();if(mf!=null)Destroy(mf.sharedMesh);}
}
