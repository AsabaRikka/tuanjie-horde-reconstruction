using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

// Soft ground contact is submitted in a shared batch; it has no colliders or physics callbacks.
public static class HordeContactShadows
{
    struct Entry { public Transform owner; public Vector2 radius; }
    static readonly List<Entry> entries=new List<Entry>();
    static readonly Matrix4x4[] matrices=new Matrix4x4[1023];
    static Mesh quad;static Material material;
    public static void Register(Transform owner,string role)
    {
        if(role.StartsWith("gate"))return;
        entries.Add(new Entry{owner=owner,radius=role=="boss"?new Vector2(.40f,.30f):role=="enemy"?new Vector2(.56f,.43f):new Vector2(.94f,1.1f)});
    }
    public static void Draw(Camera camera)
    {
        if(quad==null){
            quad=new Mesh{name="Shared soft contact quad"};quad.vertices=new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
            quad.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
            material=new Material(Shader.Find("Horde/Contact Shadow")){enableInstancing=true};
        }
        int count=0;
        for(int i=entries.Count-1;i>=0;i--){
            var e=entries[i];if(e.owner==null){entries.RemoveAt(i);continue;}
            if(!e.owner.gameObject.activeInHierarchy || e.owner.position.y>.4f)continue;
            var p=e.owner.position;p.y=.021f;var s=e.owner.lossyScale;
            matrices[count++]=Matrix4x4.TRS(p,Quaternion.Euler(0,e.owner.eulerAngles.y,0),new Vector3(e.radius.x*s.x,1,e.radius.y*s.z));
            if(count==1023){Submit(camera,count);count=0;}
        }
        if(count>0)Submit(camera,count);
    }
    static void Submit(Camera camera,int count)=>Graphics.DrawMeshInstanced(quad,0,material,matrices,count,null,ShadowCastingMode.Off,false,0,camera);
}
