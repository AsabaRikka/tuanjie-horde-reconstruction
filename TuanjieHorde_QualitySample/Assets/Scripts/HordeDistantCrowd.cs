using UnityEngine;
using UnityEngine.Rendering;

// Remote rows are presentation only. Physical agents are admitted at the near boundary.
// Shared stride meshes are instanced in 24 batches, not thousands of GameObjects.
public class HordeDistantCrowd
{
    public const int Count=24*180;
    readonly Matrix4x4[][] matrices=new Matrix4x4[24][];readonly int[] counts=new int[24];
    readonly Vector4[][] variations=new Vector4[24][];
    readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
    public HordeDistantCrowd(){for(int i=0;i<24;i++){matrices[i]=new Matrix4x4[256];variations[i]=new Vector4[256];}}
    public void Draw(Camera camera,float t,Vector3 boss)
    {
        System.Array.Clear(counts,0,counts.Length);
        for(int row=0;row<180;row++)for(int col=0;col<24;col++)
        {
            int id=row*24+col;float z=32.0f+Mathf.Repeat(row*.69f+(col%3)*.15f+Mathf.Sin(id*3.71f)*.22f-t*.65f,124.2f);float x=-5.52f+col*.47f+(row%2)*.15f+Mathf.Sin(id*1.37f)*.12f;
            if(Mathf.Abs(z-boss.z)<2.1f && Mathf.Abs(x-boss.x)<1.45f)x=boss.x+(x<boss.x?-1:1)*1.5f;
            int pose=(Mathf.FloorToInt(t*10)+id*7)%24;
            float size=.90f+Mathf.Repeat(Mathf.Sin(id*21.7f)*43758.5f,1)*.20f;
            float tint=.84f+Mathf.Repeat(Mathf.Sin(id*13.3f)*23758.3f,1)*.16f;
            variations[pose][counts[pose]]=new Vector4(tint,tint,tint,0);
            matrices[pose][counts[pose]++]=Matrix4x4.TRS(new Vector3(x,0,z),Quaternion.Euler(0,180+Mathf.Sin(id*2.1f)*13,0),Vector3.Scale(HordeIP.Scale("enemy_far"),new Vector3(.97f,size,.97f)));
        }
        for(int p=0;p<24;p++){block.SetVectorArray("_Variation",variations[p]);block.SetVector("_Anim",new Vector4(p*Mathf.PI*2/24,1,0,0));Graphics.DrawMeshInstanced(HordeIP.Mesh("enemy_far"),0,HordeIP.Material("enemy_far"),matrices[p],counts[p],block,ShadowCastingMode.On,true,0,camera);}
    }
}
