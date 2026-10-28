using UnityEngine;

public static class HordeProjectilePath
{
    static readonly Collider[] contacts=new Collider[32];
    public static Vector3 Velocity(float speed){return Vector3.forward*speed;}
    public static float Travel(float speed,float deltaTime,float travelled,float range)=>Mathf.Min(speed*deltaTime,Mathf.Max(0,range-travelled));
    public static bool FirstContact(PhysicsScene scene,Vector3 p,float distance,int mask,out Collider collider,out Vector3 point,out Vector3 normal)
    {
        collider=null;point=p+Vector3.forward*distance;normal=-Vector3.forward;
        int count=scene.OverlapSphere(p,.10f,contacts,mask,QueryTriggerInteraction.Collide);
        if(count>0){collider=contacts[0];point=p;return true;}
        if(scene.SphereCast(p,.10f,Vector3.forward,out var hit,distance,mask,QueryTriggerInteraction.Collide)){collider=hit.collider;point=hit.point;normal=hit.normal;return true;}
        return false;
    }
}
