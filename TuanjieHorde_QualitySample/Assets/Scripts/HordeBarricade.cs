using UnityEngine;

// Authored mechanical travel with physical colliders and explicit support stops.
// The two beams share endpoints; a random contact impulse cannot open a central gap.
public class HordeBarricade : MonoBehaviour
{
    public const float Length=8.8f,Thickness=1.15f,Depth=1.65f;
    public const float PostCenter=7.5f,PostWidth=2.7f,PostHeight=5.5f,SupportClearance=.014f;
    public static readonly float Angle=SupportedAngle();
    static float SupportedAngle()
    {
        float x=PostCenter-PostWidth*.5f,y=PostHeight+SupportClearance-Thickness*.5f;
        return (Mathf.Atan2(y,x)+Mathf.Asin(Thickness*.5f/Mathf.Sqrt(x*x+y*y)))*Mathf.Rad2Deg;
    }
    public static float SupportedHeight(float angle)
    {
        float a=angle*Mathf.Deg2Rad;
        return PostHeight+SupportClearance-(PostCenter-PostWidth*.5f)*Mathf.Tan(a)+Thickness*.5f/Mathf.Cos(a);
    }
    public Rigidbody body;public int side;public bool settled,connected;
    HordeDirector world;BarContact contact;float releaseAt=999,fallAt=999;bool landed;Vector3 innerAtFall;float angleAtFall;
    public void Setup(HordeDirector owner,int direction)
    {
        world=owner;side=direction;gameObject.layer=HordeDirector.MetalLayer;
        HordeGeometry.Block("Bevelled gold beam",new Vector3(Length,Thickness,Depth),.09f,HordeMeshes.Metal,transform);
        var c=gameObject.AddComponent<BoxCollider>();c.size=new Vector3(Length,Thickness,Depth);
        body=gameObject.AddComponent<Rigidbody>();body.mass=900;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        contact=gameObject.AddComponent<BarContact>();contact.director=world;
        SetPose(new Vector3(0,SupportedHeight(0),20),0,false);
    }
    void SetPose(Vector3 inner,float angle,bool move)
    {
        var q=Quaternion.Euler(0,0,side*angle);var p=inner+q*new Vector3(side*Length*.5f,0,0);
        if(move){body.MovePosition(p);body.MoveRotation(q);}else{body.position=p;body.rotation=q;}
    }
    public Vector3 InnerTip=>body.position-body.rotation*new Vector3(side*Length*.5f,0,0);
    public float Tilt=>Mathf.Abs(Mathf.DeltaAngle(0,body.rotation.eulerAngles.z));
    public void Release(){releaseAt=Time.time;}
    public void Collapse(){fallAt=Time.time;innerAtFall=InnerTip;angleAtFall=Tilt;contact.falling=true;}
    public void Step(float now)
    {
        if(now>=fallAt)
        {
            float u=Mathf.Clamp01((now-fallAt)/.58f);float fall=u*u;
            SetPose(Vector3.Lerp(innerAtFall,new Vector3(0,Thickness*.5f,20),fall),Mathf.Lerp(angleAtFall,0,fall),true);
            if(u>=1 && !landed){landed=true;contact.falling=false;world.effects.Floor(new Vector3(side*4.4f,0,20),12);world.LogEvent("bar_floor",body.position,12);}
            if(u>=1 && now>=fallAt+.67f && !connected){connected=true;world.LogEvent("road_connected",new Vector3(side*8,Thickness,20),side);}
            return;
        }
        if(now<releaseAt)return;
        float a=Mathf.Clamp01((now-releaseAt)/.65f);float travel=a*a;
        float angle=Angle*travel;
        SetPose(new Vector3(0,SupportedHeight(angle),20),angle,true);
        if(a>=1 && !settled){settled=true;world.effects.Floor(new Vector3(0,0,20),5);world.LogEvent("triangle_supported",InnerTip,side);}
    }
}

public static class HordeGeometry
{
    public static GameObject Block(string name,Vector3 size,float bevel,Material material,Transform parent=null)
    {
        var vertices=new System.Collections.Generic.List<Vector3>();var indices=new System.Collections.Generic.List<int>();
        Vector3 half=size*.5f;Vector3 core=half-Vector3.one*bevel;
        for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1})
        {
            int u=(axis+1)%3,v=(axis+2)%3,offset=vertices.Count;
            float[] us={-half[u],-core[u],core[u],half[u]},vs={-half[v],-core[v],core[v],half[v]};
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                Vector3 p=Vector3.zero;p[axis]=sign*half[axis];p[u]=us[x];p[v]=vs[y];
                var inner=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                vertices.Add(inner+(p-inner).normalized*bevel);
            }
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {int a=offset+y*4+x,b=a+1,c=a+4,d=c+1;if(sign>0)indices.AddRange(new[]{a,b,c,b,d,c});else indices.AddRange(new[]{a,c,b,b,c,d});}
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var result=HordeMeshes.Model(name,mesh,Vector3.zero,material,parent);var props=new MaterialPropertyBlock();props.SetFloat("_MetalHeight",size.y);result.GetComponent<Renderer>().SetPropertyBlock(props);return result;
    }
}
