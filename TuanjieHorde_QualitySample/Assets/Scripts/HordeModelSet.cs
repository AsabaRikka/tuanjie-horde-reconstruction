using UnityEngine;

[CreateAssetMenu(menuName="Horde/Model replacements")]
public class HordeModelSet : ScriptableObject
{
    [Tooltip("Optional visual prefabs. Root at feet, forward +Z. Physics and shooting stay on the actor root.")]
    public GameObject starter,upgraded,zombie,boss;
    public RuntimeAnimatorController humanoidController;
    public GameObject Prefab(string role){return role=="starter"?starter:role=="heavy"?upgraded:role=="boss"?boss:zombie;}
}

// Visuals can be replaced without changing collision, gate or projectile code.
public class HordeActorView : MonoBehaviour
{
    IPActorView ip;
    public MeshFilter fallback;public Animator animator;public Transform muzzle;
    Mesh deform,slamMesh;Vector3[] rest,posed,bossRest;Renderer[] renderers;MaterialPropertyBlock block;
    float cycle,moveBlend;string role;bool external;int poseIndex=-1;
    public void Setup(HordeModelSet models,string type)
    {
        if(HordeIP.Available){ip=gameObject.AddComponent<IPActorView>();ip.Setup(type);muzzle=ip.muzzle;return;}
        role=type;var prefab=models!=null?models.Prefab(type):null;external=prefab!=null;
        if(external)
        {
            var view=Instantiate(prefab,transform);view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;
            foreach(var c in view.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var r in view.GetComponentsInChildren<Rigidbody>())r.isKinematic=true;
            animator=view.GetComponentInChildren<Animator>();
            if(animator!=null){animator.applyRootMotion=false;if(animator.runtimeAnimatorController==null && models.humanoidController!=null)animator.runtimeAnimatorController=models.humanoidController;}
            foreach(var t in view.GetComponentsInChildren<Transform>())if(t.name=="Muzzle")muzzle=t;
        }
        else
        {
            var mesh=HordeMeshes.Load(type);var obj=HordeMeshes.Model("Replaceable "+type,mesh,Vector3.zero,HordeMeshes.Skin,transform);fallback=obj.GetComponent<MeshFilter>();
            if(type=="enemy" || type=="boss")obj.transform.localRotation=Quaternion.Euler(0,180,0);
            else {deform=Instantiate(mesh);fallback.sharedMesh=deform;rest=mesh.vertices;posed=new Vector3[rest.Length];}
            if(type=="boss"){slamMesh=Instantiate(mesh);bossRest=mesh.vertices;posed=new Vector3[bossRest.Length];}
        }
        renderers=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();
        if(muzzle==null){muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(transform,false);muzzle.localPosition=new Vector3(.11f,type=="heavy"?1.23f:1.4f,.96f);}
    }
    bool Has(string name,AnimatorControllerParameterType type){if(animator==null)return false;foreach(var p in animator.parameters)if(p.name==name && p.type==type)return true;return false;}
    void Number(string name,float value){if(Has(name,AnimatorControllerParameterType.Float))animator.SetFloat(name,value);}
    public void Trigger(string name){if(ip!=null){ip.Trigger(name);return;}if(Has(name,AnimatorControllerParameterType.Trigger))animator.SetTrigger(name);}
    public void Pose(Vector3 velocity,float distance,float recoil,float flash=0,float attack=0,float duck=0)
    {
        if(ip!=null){ip.Pose(velocity,distance,recoil,flash,attack,duck);return;}
        float speed=new Vector2(velocity.x,velocity.z).magnitude;
        Number("Speed",speed);Number("MoveX",velocity.x);Number("Attack",attack);Number("Duck",duck);
        if(!external)
        {
            if(role=="enemy" || role=="boss")
            {
                var poses=role=="boss"?HordeMeshes.BossWalk:HordeMeshes.Walk;int frame=Mathf.FloorToInt(distance/(role=="boss"?3.6f:1.12f)*poses.Length)%poses.Length;
                if(frame!=poseIndex){fallback.sharedMesh=poses[frame];poseIndex=frame;}
                if(role=="boss" && (Mathf.Abs(attack)>.001f || duck>.001f))
                {
                    float windup=Mathf.Clamp01(-attack),strike=Mathf.Clamp01(attack);
                    for(int i=0;i<bossRest.Length;i++)
                    {
                        Vector3 p=bossRest[i];float side=p.x<0?-1:1;
                        if(Mathf.Abs(p.x)>.28f && p.y>.8f && p.y<1.45f)
                        {
                            var shoulder=new Vector3(side*.27f,1.31f,.02f);
                            float arm=attack<0?windup*125:Mathf.Lerp(125,40,strike);
                            p=shoulder+Quaternion.Euler(arm,0,side*windup*8)*(p-shoulder);
                        }
                        if(bossRest[i].y>.8f)
                        {
                            var hip=new Vector3(0,.8f,0);p=hip+Quaternion.Euler(windup*12-strike*62,0,0)*(p-hip);p.y-=strike*.24f;
                        }
                        else {p.y-=strike*.24f*Mathf.Clamp01(p.y/.8f);p.z+=Mathf.Sin(p.y/.8f*Mathf.PI)*strike*.15f;}
                        if(duck>0 && Mathf.Abs(attack)<.001f)
                        {
                            if(p.y>.8f){var hip=new Vector3(0,.8f,0);p=hip+Quaternion.Euler(-55*duck,0,0)*(p-hip);p.y-=.38f*duck;}
                            else {p.y*=1-.475f*duck;p.z+=Mathf.Sin(p.y/.42f*Mathf.PI)*.12f*duck;}
                        }
                        posed[i]=p;
                    }
                    slamMesh.vertices=posed;slamMesh.RecalculateNormals();slamMesh.RecalculateBounds();fallback.sharedMesh=slamMesh;poseIndex=-1;
                }
            }
            else
            {
                cycle=distance/1.20f*Mathf.PI*2;moveBlend=Mathf.MoveTowards(moveBlend,speed>.15f?1:0,Time.deltaTime*9);
                float sideMove=Mathf.Abs(velocity.x)>.1f?Mathf.Sign(velocity.x):0;
                for(int i=0;i<rest.Length;i++)
                {
                    Vector3 p=rest[i];float side=p.x<0?-1:1;float s=Mathf.Sin(cycle+(side<0?0:Mathf.PI))*moveBlend;
                    if(p.y<.82f)
                    {
                        var hip=new Vector3(side*.14f,.80f,0);var knee=new Vector3(side*.16f,.44f,0);
                        if(p.y<.45f)p=knee+Quaternion.Euler(Mathf.Max(0,-s)*27,0,0)*(p-knee);
                        p=hip+Quaternion.Euler(sideMove==0?s*27:0,0,sideMove==0?0:-s*sideMove*26)*(p-hip);
                        p.y+=Mathf.Max(0,s)*.04f;
                    }
                    else {p.x+=Mathf.Clamp(velocity.x/8,-1,1)*(p.y-.8f)*.055f;p.z-=recoil*.065f*Mathf.Clamp01((p.y-.75f)/.4f);}
                    posed[i]=p;
                }
                deform.vertices=posed;deform.RecalculateBounds();
            }
        }
        block.SetFloat("_Flash",flash);foreach(var r in renderers)r.SetPropertyBlock(block);
    }
}
