using UnityEngine;

public class HordeMob : MonoBehaviour
{
    public Rigidbody body;public CapsuleCollider capsule;public MeshFilter mesh;public MeshRenderer visual;HordeActorView actorView;
    public HordeDirector world;public int id;public bool alive=true;public float phase,walked,totalDistance,hitUntil,laneX;public int health=1;
    public Vector3 desiredVelocity;
    public int passageSide;
    float deadAt,restUntil,respawnAt;Vector3 oldPosition,impulse;MaterialPropertyBlock props;int lastPose=-1;bool passed;
    public void Setup(HordeDirector w,int index)
    {
        world=w;id=index;laneX=transform.position.x;gameObject.layer=HordeDirector.EnemyLayer;
        body=gameObject.AddComponent<Rigidbody>();body.mass=72;body.drag=.25f;body.angularDrag=1;body.constraints=RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.maxDepenetrationVelocity=2;body.solverIterations=8;body.solverVelocityIterations=2;
        capsule=gameObject.AddComponent<CapsuleCollider>();capsule.radius=.26f;capsule.height=1.84f;capsule.center=new Vector3(0,.92f,0);capsule.material=w.crowdFriction;
        var view=new GameObject("Visual");view.transform.SetParent(transform,false);actorView=view.AddComponent<HordeActorView>();actorView.Setup(w.models,"enemy");props=new MaterialPropertyBlock();
        phase=(index*.618f)%1;walked=phase*1.12f;oldPosition=transform.position;body.velocity=Vector3.back*(w.crowdSpeed+(id%7)*.035f);body.rotation=Quaternion.Euler(0,180,0);
    }
    public void Drive(float dt,float time)
    {
        if(!alive)
        {
            if(time<respawnAt)return;respawnAt=time+.4f;
            if(time-deadAt>1.8f && world.TryRespawn(out Vector3 spawn))
            {
                body.position=spawn;laneX=spawn.x;body.rotation=Quaternion.Euler(0,180,0);body.isKinematic=false;body.velocity=Vector3.back*world.crowdSpeed;body.angularVelocity=Vector3.zero;capsule.enabled=true;alive=true;health=1;passed=false;passageSide=0;actorView.gameObject.SetActive(true);oldPosition=spawn;impulse=Vector3.zero;
            }
            return;
        }
        Vector3 p=body.position;Vector3 target=world.CrowdTarget(p,id);Vector3 delta=target-p;delta.y=0;
        if(p.z<-14 && world.TryRespawn(out Vector3 recycle)){body.position=recycle;laneX=recycle.x;oldPosition=recycle;body.velocity=Vector3.back*world.crowdSpeed;return;}
        float speed=world.crowdSpeed+(id%7)*.035f;
        if(time<restUntil)speed*=.35f;
        Vector3 want=HordeCombatRules.Pursuit(p,target,speed);
        if(p.z<18.3f)
        {
            if(!passed){passed=true;passageSide=p.x<0?-1:1;world.RecordPass(p);}
        }
        // A ray detects a stopped neighbour before the body contacts it. Actual separation is handled by PhysX.
        if(Physics.SphereCast(p+Vector3.up*.95f,.23f,want.normalized,out var hit,.55f,1<<HordeDirector.EnemyLayer,QueryTriggerInteraction.Ignore))
        {
            var other=hit.rigidbody;if(other!=body && other!=null){float follow=Vector3.Dot(other.velocity,want.normalized);want=want.normalized*Mathf.Min(speed,Mathf.Max(0,follow)+Mathf.Max(0,hit.distance-.04f)*4);}
        }
        desiredVelocity=want;
        Vector3 horizontal=new Vector3(body.velocity.x,0,body.velocity.z);Vector3 acceleration=Vector3.ClampMagnitude((want-horizontal)*7,12);
        body.AddForce(acceleration,ForceMode.Acceleration);
        if(delta.sqrMagnitude>.05f){Quaternion q=Quaternion.LookRotation(delta.normalized,Vector3.up);body.MoveRotation(Quaternion.RotateTowards(body.rotation,q,200*dt));}
        float travelled=new Vector2(p.x-oldPosition.x,p.z-oldPosition.z).magnitude;totalDistance+=travelled;walked+=travelled;oldPosition=p;
        actorView.Pose(horizontal,walked,0,Mathf.Clamp01((hitUntil-time)/.06f));
    }
    public void Hit(Vector3 point,Vector3 direction,bool heavy)
    {
        if(!alive)return;hitUntil=Time.time+.075f;restUntil=Time.time+.12f;health-=heavy?2:1;
        world.effects.Impact(point,-direction,false);world.LogEvent("enemy_hit",point,heavy?2:1);
        if(health>0)return;
        Die(point,direction);
    }
    void Die(Vector3 point,Vector3 direction)
    {
        alive=false;deadAt=Time.time;capsule.enabled=false;body.velocity=Vector3.zero;body.isKinematic=true;actorView.gameObject.SetActive(false);world.deaths++;
        if(HordeIP.Available){HordeIP.Corpse(body.position,body.rotation,direction);return;}
        if(world.models!=null && world.models.zombie!=null)
        {
            var corpse=Instantiate(world.models.zombie,body.position,body.rotation);var anim=corpse.GetComponentInChildren<Animator>();if(anim!=null)anim.enabled=false;
            var rig=corpse.GetComponentsInChildren<Rigidbody>();foreach(var c in corpse.GetComponentsInChildren<Collider>()){c.enabled=true;c.isTrigger=false;c.gameObject.layer=HordeDirector.CorpseLayer;}
            if(rig.Length==0){var rb=corpse.AddComponent<Rigidbody>();rb.mass=60;var c=corpse.AddComponent<CapsuleCollider>();c.radius=.26f;c.height=1.8f;c.center=Vector3.up*.9f;rig=new[]{rb};corpse.layer=HordeDirector.CorpseLayer;}
            foreach(var rb in rig){rb.isKinematic=false;rb.velocity=direction*2.5f+Vector3.up*.8f;rb.angularVelocity=transform.right*2;}
            Destroy(corpse,.6f);return;
        }
        var root=new GameObject("Contact ragdoll "+id);root.AddComponent<RagdollExpiry>().lifetime=.6f;Rigidbody[] parts=new Rigidbody[8];
        for(int j=0;j<8;j++)
        {
            Vector3 local=HordeMeshes.Pivots[j];var go=HordeMeshes.Model("Body part "+j,HordeMeshes.BodyParts[j],Vector3.zero,HordeMeshes.Skin,root.transform);
            go.transform.position=body.position+body.rotation*Quaternion.Euler(0,180,0)*local;go.transform.rotation=body.rotation*Quaternion.Euler(0,180,0);go.layer=HordeDirector.CorpseLayer;
            var rb=go.AddComponent<Rigidbody>();rb.mass=j==0?28:6;rb.drag=.2f;rb.angularDrag=.5f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.maxDepenetrationVelocity=2;rb.solverIterations=10;parts[j]=rb;
            if(j==1){var c=go.AddComponent<SphereCollider>();c.radius=.18f;}
            else {var c=go.AddComponent<CapsuleCollider>();c.radius=j==0?.21f:(j<4?.10f:.105f);c.height=j==0?.6f:(j<4?.45f:.42f);}
            if(j>0)
            {
                var joint=go.AddComponent<CharacterJoint>();joint.anchor=Vector3.up*(j==1?-.15f:.20f);joint.connectedBody=parts[j>=6?j-2:0];joint.enablePreprocessing=false;joint.swingAxis=Vector3.forward;
                joint.lowTwistLimit=new SoftJointLimit{limit=-30};joint.highTwistLimit=new SoftJointLimit{limit=30};joint.swing1Limit=new SoftJointLimit{limit=j==1?28:65};joint.swing2Limit=new SoftJointLimit{limit=30};joint.enableProjection=true;joint.projectionDistance=.05f;
            }
            rb.velocity=direction*2.0f+Vector3.up*.55f;rb.angularVelocity=transform.right*Random.Range(1.5f,3.2f);
        }
        for(int a=0;a<parts.Length;a++)for(int b=a+1;b<parts.Length;b++)Physics.IgnoreCollision(parts[a].GetComponent<Collider>(),parts[b].GetComponent<Collider>());
        parts[0].AddForceAtPosition(direction*80,point,ForceMode.Impulse);
    }
}
