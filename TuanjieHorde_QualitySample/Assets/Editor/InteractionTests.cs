using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;

public static class InteractionTests
{
    public static void Run()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var physics=scene.GetPhysicsScene();
        GameObject Target(string name,Vector3 p){var go=new GameObject(name);go.layer=30;SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=p;go.AddComponent<BoxCollider>();return go;}
        var near=Target("Near lateral enemy",new Vector3(2,1,2));var front=Target("Front enemy",new Vector3(0,1,8));var metal=Target("Metal blocker",new Vector3(0,1,5));Physics.SyncTransforms();
        void Assert(bool ok,string name){if(!ok)throw new System.Exception("FORWARD_TEST_FAILED "+name);Debug.Log("FORWARD_TEST_PASS "+name);}
        Assert(HordeProjectilePath.Velocity(82)==new Vector3(0,0,82),"fixed forward without inherited strafe velocity");
        Assert(HordeProjectilePath.FirstContact(physics,new Vector3(0,1,0),12,1<<30,out var collider,out var point,out var normal) && collider.gameObject==metal,"metal blocks a farther enemy");
        Object.DestroyImmediate(metal);Physics.SyncTransforms();
        Assert(HordeProjectilePath.FirstContact(physics,new Vector3(0,1,0),12,1<<30,out collider,out point,out normal) && collider.gameObject==front,"nearer lateral enemy does not attract fire");
        Assert(HordeProjectilePath.FirstContact(physics,new Vector3(0,1,8),1,1<<30,out collider,out point,out normal) && collider.gameObject==front,"muzzle inside collider is still a contact");
        Object.DestroyImmediate(front);Physics.SyncTransforms();
        Assert(!HordeProjectilePath.FirstContact(physics,new Vector3(0,1,0),12,1<<30,out collider,out point,out normal),"empty firing column misses instead of turning");
        foreach(float distance in new[]{.2f,.7f,2f,5.2f,10f})
        {
            Vector3 p=new Vector3(-2,0,distance),goal=new Vector3(-2,0,0);
            Assert(Vector3.Dot(HordeCombatRules.Pursuit(p,goal,1.55f),goal-p)>=0,"pursuit never retreats at distance "+distance);
        }
        var first=Target("First row",new Vector3(0,1,4));var second=Target("Second row",new Vector3(0,1,5));Physics.SyncTransforms();
        Assert(HordeProjectilePath.FirstContact(physics,new Vector3(0,1,0),10,1<<30,out collider,out point,out normal) && collider.gameObject==first,"one front target returned from dense rows");
        Assert(!HordeProjectilePath.FirstContact(physics,new Vector3(0,1,0),2,1<<30,out collider,out point,out normal),"finite travel does not hit out-of-range row");
        foreach(float speed in new[]{52f,82f}){
            float travelled=0;int steps=0;while(travelled<HordeCombatRules.ShotRange-.0001f && steps++<100)travelled+=HordeProjectilePath.Travel(speed,1f/60,travelled,HordeCombatRules.ShotRange);
            Assert(Mathf.Abs(travelled-HordeCombatRules.ShotRange)<.0001f,"slow and fast projectiles retain full exact range "+speed);
        }
        foreach(int side in new[]{-1,1})
        {
            var post=Target("Widened support",new Vector3(side*HordeBarricade.PostCenter,HordeBarricade.PostHeight*.5f,20)).GetComponent<BoxCollider>();
            post.size=new Vector3(HordeBarricade.PostWidth,HordeBarricade.PostHeight,HordeBarricade.Depth);
            var beam=Target("Falling beam",Vector3.zero).GetComponent<BoxCollider>();beam.size=new Vector3(HordeBarricade.Length,HordeBarricade.Thickness,HordeBarricade.Depth);
            float worst=0;
            for(int frame=0;frame<=240;frame++)
            {
                float angle=HordeBarricade.Angle*frame/240f;var rotation=Quaternion.Euler(0,0,side*angle);
                var position=new Vector3(0,HordeBarricade.SupportedHeight(angle),20)+rotation*new Vector3(side*HordeBarricade.Length*.5f,0,0);
                if(Physics.ComputePenetration(beam,position,rotation,post,post.transform.position,Quaternion.identity,out var direction,out var depth))worst=Mathf.Max(worst,depth);
            }
            Assert(worst<.001f,"widened pillar remains outside beam over 241 release poses side "+side);
            Object.DestroyImmediate(post.gameObject);Object.DestroyImmediate(beam.gameObject);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Directory.CreateDirectory("Validation");File.WriteAllText("Validation/forward_tests.json","{\"passed\":16,\"failed\":0,\"usesProductionProjectilePath\":true,\"singleTargetDamage\":true,\"supportPosesChecked\":482}");
    }
}



