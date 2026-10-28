using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.IO;
using System.Collections.Generic;

public class HordeDirector : MonoBehaviour
{
    public const int GroundLayer=8,EnemyLayer=9,MetalLayer=10,HeroLayer=11,CorpseLayer=12,NavigationLayer=13,BossLayer=14;
    public HordeEffects effects;public PhysicMaterial crowdFriction;public float unlockTime=999;public int deaths;
    public Camera mainCamera;public HordeModelSet models;bool leftLaneLit,rightLaneLit;float supplyAt;
    public bool soak;bool noImages;public int crowdRecycled;int gateSerial,gateRecycled,gateConsumed,plus99;float min99Gap=999,bossAttackAt=-1,bossAttackPose;bool bossImpact;HordeActorView bossView;
    public float LeftOpen=>leftBreak+.67f;public float RightOpen=>rightBreak+.67f;
    public const int MaxPhysicalCrowd=720;
    Material blueGlass,goldGlass,worldFont;float maxBeamGap;int maxActive;bool benchmark;
    readonly List<float> frameMs=new List<float>();
    HordeBarricade[] barricades=new HordeBarricade[2];HordeDistantCrowd distant;
    readonly List<float> stepMs=new List<float>();float startupMs;int trianglePasses,blockedSidePasses,peakBodies;float closestEnemy=999;
    public float PlayerX=>playerX;public bool HasDefeated=>defeated;
    public Vector3 BossPosition=>boss==null?new Vector3(0,0,60):boss.position;
    const float GateSpacing=1.9f,GateSpeed=12.6f,GateX=7.5f,GateWidth=2.7f;
    const int CrowdColumns=22;
    [Header("尸潮与射击调节")]
    [Range(.3f,5)]public float crowdSpeed=1.55f;
    [Range(40,150)]public float bulletSpeed=82;
    [Range(.03f,.3f)]public float fireInterval=.10f;
    readonly List<HordeMob> mobs=new List<HordeMob>();readonly List<Hero> heroes=new List<Hero>();readonly List<Gate> gates=new List<Gate>();readonly List<Bullet> bullets=new List<Bullet>();
    readonly List<EventItem> events=new List<EventItem>();readonly List<Sample> samples=new List<Sample>();
    readonly List<CombatSample> combatSamples=new List<CombatSample>();int retreatCommands;
    [Serializable]class CombatSample {public float t;public int leftForecourt,rightForecourt,leftPassSurvivors,nearPlayer,retreatCommands;}
    [Serializable]class CombatReport {public int retreatCommands;public CombatSample[] samples;}
    Pillar left,right;Rigidbody[] bars=new Rigidbody[2];
    GameObject gold,release,plinth;TextMesh goldText;int goldHP=3;float leftBreak=999,rightBreak=999;bool upgraded,defeated;float[] starterTimes={.1333f,.8666f,1.6000f};int starterIndex;
    Rigidbody boss;MeshRenderer bossRenderer;int bossHP=1000,bossHits;GameObject bossHPFill,bossHPBack;Font font,uiFont;bool interactive;float playerX,shake,defeatTime;int totalShots,metalHits,awards;
    bool capture;string captureDir;int captureFrame,captureWidth=720,captureHeight=1280,captureFPS=30;float duration=31;RenderTexture target;Texture2D pixels;Material endShade;
    float sampleAt,bossStepAt,bossWalked,bossFlashUntil;Vector3 bossPrevious;MeshFilter bossMesh;MaterialPropertyBlock bossProps;int sessionStart,respawnSlot;
    class Hero {public Transform root;public HordeActorView view;public Transform muzzle;public float next,kick,walked,born;public Rigidbody rb;public Vector3 last,joinFrom,smoothVelocity;public bool joining;}
    class Bullet {public Vector3 p,v;public float travelled;public bool heavy;public int id;}
    class Pillar {public GameObject go;public TextMesh text;public int hp;public int side;public float damageRemainder;}
    class Gate {public Transform root;public Rigidbody rb;public Collider trigger;public Renderer[] visuals;public float z,usedAt;public int side,serial;public bool used,unlocked;}
    [Serializable]public class EventItem {public string kind;public float t;public Vector3 p;public float value;public Vector3 v;public int id;}
    [Serializable]public class Sample {public float t;public int alive,moved,deepOverlapPairs;public float meanTravel,maxOverlap;public Vector3 bossPosition,leftBarPosition,rightBarPosition;public int squad,postLeft,postRight,bossHealth,movingNow,gateConsumed,gateRecycled,crowdRecycled;public float min99Gap;}
    [Serializable]public class Report {public string engine="Tuanjie 1.10.2 / 2022.3.62t14";public int framesPerSecond,crowd,totalShots,enemyDeaths,metalHits,plus1,bossHits,plus99,gateConsumed,gateRecycled,crowdRecycled;public string bulletRule="world +Z, first contact, no target search";public float duration;public EventItem[] events;public Sample[] samples;}
    [Serializable]class PerformanceReport {public float startupMs,fixedStepMedianMs,fixedStepP95Ms,frameMedianMs,frameP95Ms,closestEnemyToLead,maxBeamJointGap;public int physicalAgents,farInstancedAgents,trianglePasses,closedLanePasses;public float[] finalBeamTilt;public bool[] connected;public long managedBytes;}

    void Awake()
    {
        var startup=System.Diagnostics.Stopwatch.StartNew();
        var preview=GameObject.Find("Editor Preview");if(preview!=null){preview.SetActive(false);Destroy(preview);}
        UnityEngine.Random.InitState(2081);sessionStart=Environment.TickCount;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
        Time.fixedDeltaTime=1f/60;Time.maximumDeltaTime=.1f;Physics.defaultSolverIterations=10;Physics.defaultSolverVelocityIterations=3;Physics.defaultContactOffset=.012f;
        Physics.IgnoreLayerCollision(MetalLayer,NavigationLayer,true);Physics.IgnoreLayerCollision(CorpseLayer,NavigationLayer,true);
        Physics.IgnoreLayerCollision(BossLayer,NavigationLayer,true);
        Physics.IgnoreLayerCollision(EnemyLayer,CorpseLayer,true);
        Physics.IgnoreLayerCollision(CorpseLayer,CorpseLayer,true);
        string[] args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length;i++){if(args[i]=="--capture" && i+1<args.Length){capture=true;captureDir=args[++i];}if(args[i]=="--soak")soak=true;if(args[i]=="--no-images")noImages=true;if(args[i]=="--duration" && i+1<args.Length)duration=float.Parse(args[++i],System.Globalization.CultureInfo.InvariantCulture);}
        benchmark=Array.IndexOf(args,"--benchmark")>=0;
        int fpsArg=Array.IndexOf(args,"--fps");if(fpsArg>=0 && fpsArg+1<args.Length)captureFPS=Mathf.Clamp(int.Parse(args[fpsArg+1]),30,60);
        if(Array.IndexOf(args,"--full-hd")>=0){captureWidth=1080;captureHeight=1920;}
        HordeMeshes.Init();font=Font.CreateDynamicFontFromOSFont("Arial",96);uiFont=Font.CreateDynamicFontFromOSFont("Microsoft YaHei",18);
        blueGlass=new Material(Shader.Find("Horde/Gate Glass"));blueGlass.SetColor("_BaseColor",new Color(.08f,.42f,1,.38f));
        goldGlass=new Material(Shader.Find("Horde/Gate Glass"));goldGlass.SetColor("_BaseColor",new Color(1,.67f,.08f,.38f));
        worldFont=new Material(Shader.Find("Horde/World Text"));worldFont.mainTexture=font.material.mainTexture;
        Font.textureRebuilt+=RefreshFontTexture;
        crowdFriction=new PhysicMaterial("Crowd sliding contact"){dynamicFriction=.03f,staticFriction=.03f,bounciness=0,frictionCombine=PhysicMaterialCombine.Minimum};
        var groundMat=new PhysicMaterial("Ground friction"){dynamicFriction=.65f,staticFriction=.7f,bounciness=.1f};
        HordeTexturedEnvironment.Create();
        var floor=new GameObject("Physical road surface");floor.layer=GroundLayer;var fc=floor.AddComponent<BoxCollider>();fc.size=new Vector3(18,.4f,240);fc.center=new Vector3(0,-.2f,55);fc.material=groundMat;
        foreach(int side in new[]{-1,1})
        {
            var wall=new GameObject("Road edge collider "+side);wall.layer=GroundLayer;var c=wall.AddComponent<BoxCollider>();c.center=new Vector3(side*9.05f,.375f,35);c.size=new Vector3(.2f,.75f,160);
            var laneWall=new GameObject("Crowd lane edge "+side);laneWall.layer=NavigationLayer;var lc=laneWall.AddComponent<BoxCollider>();lc.center=new Vector3(side*6.05f,1,90);lc.size=new Vector3(.12f,2,140);
        }
        var cam=new GameObject("Portrait gameplay camera");mainCamera=cam.AddComponent<Camera>();cam.tag="MainCamera";cam.AddComponent<AudioListener>();mainCamera.fieldOfView=35.15f;mainCamera.nearClipPlane=.2f;mainCamera.farClipPlane=250;mainCamera.backgroundColor=new Color(.37f,.50f,.64f);mainCamera.clearFlags=CameraClearFlags.SolidColor;mainCamera.allowHDR=true;
        var cd=cam.AddComponent<UniversalAdditionalCameraData>();cd.renderPostProcessing=true;cd.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,35,0);sun.intensity=1.5f;sun.color=new Color(1,.99f,.94f);sun.shadows=LightShadows.Soft;sun.shadowBias=.02f;sun.shadowNormalBias=.12f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.42f,.50f);RenderSettings.ambientEquatorColor=new Color(.23f,.27f,.33f);RenderSettings.ambientGroundColor=new Color(.10f,.12f,.15f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.004f;RenderSettings.fogColor=mainCamera.backgroundColor;
        var vol=new GameObject("Gameplay colour and bloom").AddComponent<Volume>();vol.isGlobal=true;vol.profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=vol.profile.Add<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.1f);var tone=vol.profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
        var exposure=vol.profile.Add<ColorAdjustments>();exposure.postExposure.Override(-.35f);
        effects=new GameObject("Synchronized contact effects").AddComponent<HordeEffects>();effects.silent=capture;effects.Setup();
        left=CreatePillar(-1,3);right=CreatePillar(1,600);
        for(int j=0;j<2;j++)
        {
            int side=j==0?-1:1;var bar=new GameObject("Supported gold beam "+side);barricades[j]=bar.AddComponent<HordeBarricade>();barricades[j].Setup(this,side);bars[j]=barricades[j].body;
        }
        gold=HordeIP.Create("heavy",null);gold.name="Golden battle car unlock";gold.transform.position=new Vector3(0,3.279f,19.1f);gold.transform.localScale=Vector3.one*1.3f;gold.layer=MetalLayer;var gc=gold.AddComponent<BoxCollider>();gc.center=new Vector3(0,.5f,0);gc.size=new Vector3(1.4f,1.3f,1.3f);
        plinth=HordeGeometry.Block("Raised gold unlock plinth",new Vector3(3.15f,3.285f,2.55f),.08f,HordeMeshes.Metal);plinth.transform.position=new Vector3(0,1.6425f,19.1f);plinth.layer=MetalLayer;plinth.AddComponent<BoxCollider>().size=new Vector3(3.15f,3.285f,2.55f);
        goldText=Label("3",new Vector3(0,.85f,17.78f),1.0f);
        var unlockFracture=plinth.AddComponent<HordeFracture>();unlockFracture.size=new Vector3(3.15f,3.285f,2.55f);unlockFracture.resource="plinth";
        release=HordeIP.Create("heavy",null);release.transform.position=gold.transform.position;release.SetActive(false);
        for(int row=0;row<16;row++)for(int col=0;col<CrowdColumns;col++)
        {
            Vector3 p=new Vector3(-5.65f+col*.53f+(row%2)*.14f+UnityEngine.Random.Range(-.035f,.035f),.025f,21.3f+row*.69f+UnityEngine.Random.Range(-.15f,.15f));
            if(new Vector2(p.x,p.z-60).magnitude<1.7f)continue;
            var go=new GameObject("Continuous zombie "+mobs.Count);go.transform.position=p;var mob=go.AddComponent<HordeMob>();mob.Setup(this,mobs.Count);mobs.Add(mob);
        }

        distant=new HordeDistantCrowd();
        var bossGo=new GameObject("Giant actor");bossGo.transform.position=new Vector3(0,.025f,60);bossGo.layer=BossLayer;
        boss=bossGo.AddComponent<Rigidbody>();boss.mass=12000;boss.solverIterations=24;boss.solverVelocityIterations=8;boss.drag=.05f;boss.constraints=RigidbodyConstraints.FreezeRotation;boss.interpolation=RigidbodyInterpolation.Interpolate;boss.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        var bc=bossGo.AddComponent<CapsuleCollider>();bc.center=new Vector3(0,.9f,0);bc.height=1.8f;bc.radius=.75f;bc.material=crowdFriction;
        var hurt=new GameObject("Giant full-height bullet hurtbox");hurt.layer=BossLayer;hurt.transform.SetParent(bossGo.transform,false);var hurtbox=hurt.AddComponent<CapsuleCollider>();hurtbox.center=Vector3.up*3.2f;hurtbox.height=6.4f;hurtbox.radius=1.05f;hurtbox.isTrigger=true;
        var bossVisual=new GameObject("Visual");bossVisual.transform.SetParent(bossGo.transform,false);bossVisual.transform.localScale=Vector3.one*3.5f;bossView=bossVisual.AddComponent<HordeActorView>();bossView.Setup(models,"boss");bossPrevious=boss.position;

        bossHPBack=HordeMeshes.Box("Boss health background",Vector3.zero,new Vector3(3.5f,.25f,.035f),HordeMeshes.Black,false);bossHPFill=HordeMeshes.Box("Boss health remaining",Vector3.zero,new Vector3(3.4f,.17f,.04f),HordeMeshes.Mat("Boss red health",new Color(.9f,.07f,.06f)),false);
        SpawnHero();
        boss.rotation=Quaternion.Euler(0,180,0);
        for(int side=-1;side<=1;side+=2)for(int i=0;i<80;i++)CreateGate(side,i);
        if(capture && !benchmark)
        {
            Directory.CreateDirectory(captureDir);Time.captureFramerate=captureFPS;target=new RenderTexture(captureWidth,captureHeight,24,RenderTextureFormat.ARGB32);target.Create();mainCamera.targetTexture=target;mainCamera.enabled=false;pixels=new Texture2D(captureWidth,captureHeight,TextureFormat.RGB24,false);
        }
        UpdateCamera();startupMs=(float)startup.Elapsed.TotalMilliseconds;Debug.Log("HORDE_READY "+mobs.Count+" physical agents, "+HordeDistantCrowd.Count+" instanced distant agents; setup ms "+startupMs);
    }
    Pillar CreatePillar(int side,int hp)
    {
        var go=HordeGeometry.Block("Gold supporting pillar "+side,new Vector3(2.7f,5.5f,1.65f),.09f,HordeMeshes.Metal);go.transform.position=new Vector3(side*7.5f,2.75f,20);go.layer=MetalLayer;go.AddComponent<BoxCollider>().size=new Vector3(2.7f,5.5f,1.65f);
        var p=new Pillar{go=go,hp=hp,side=side,text=Label(hp.ToString(),new Vector3(side*7.5f,1.35f,19.10f),1.30f)};return p;
    }
    TextMesh Label(string text,Vector3 p,float size,Transform parent=null)
    {
        var go=new GameObject("Number "+text);go.transform.SetParent(parent,false);go.transform.localPosition=p;var tm=go.AddComponent<TextMesh>();tm.text=text;tm.font=font;tm.fontStyle=FontStyle.Bold;tm.fontSize=96;tm.characterSize=size*.1f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;tm.color=Color.white;tm.GetComponent<Renderer>().sharedMaterial=worldFont;return tm;
    }
    void RefreshFontTexture(Font changed){if(changed==font && worldFont!=null)worldFont.mainTexture=font.material.mainTexture;}
    void OnDestroy(){Font.textureRebuilt-=RefreshFontTexture;}
    Vector3 Formation(int index,int count)
    {
        if(count==1)return new Vector3(playerX,.02f,5);
        float centre=Mathf.Clamp(playerX,-7.15f,7.15f);
        return new Vector3(centre+(index%3-1)*1.16f,.02f,5-(index/3)*1.20f);
    }
    void SpawnHero(Vector3? from=null)
    {
        var go=new GameObject("Squad member "+heroes.Count);go.layer=HeroLayer;go.transform.position=from??new Vector3(playerX,.02f,5);
        var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.Interpolate;var col=go.AddComponent<CapsuleCollider>();col.radius=.21f;col.height=.72f;col.center=Vector3.up*.36f;col.isTrigger=false;go.AddComponent<HeroContact>().world=this;
        var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);var view=visual.AddComponent<HordeActorView>();view.Setup(models,upgraded?"heavy":"starter");
        heroes.Add(new Hero{root=go.transform,rb=rb,view=view,muzzle=view.muzzle,next=Time.time+.10f+(heroes.Count%4)*.019f,last=go.transform.position,born=Time.time,joinFrom=go.transform.position,joining=from.HasValue});
    }
    void CreateGate(int side,int i)
    {
        float z=24+i*GateSpacing;var root=new GameObject((side<0?"+1":"+99")+" pooled gate "+i);root.transform.position=new Vector3(side*GateX,0,z);
        var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,.82f,0);trigger.size=new Vector3(GateWidth,1.65f,.08f);
        var receiver=root.AddComponent<GateTrigger>();receiver.world=this;receiver.index=gates.Count;receiver.side=side;
        var model=HordeIP.Create(side<0?"gate1":"gate99",root.transform);model.transform.localRotation=Quaternion.Euler(0,180,0);model.transform.localScale=new Vector3(GateWidth/1.7f,1.1f,1);
        foreach(var v in root.GetComponentsInChildren<Renderer>())v.shadowCastingMode=ShadowCastingMode.Off;
        gates.Add(new Gate{root=root.transform,rb=rb,trigger=trigger,z=z,side=side,serial=gateSerial++,visuals=root.GetComponentsInChildren<Renderer>()});
    }
    void Gates(float t,float dt)
    {
        if(!leftLaneLit && barricades[0].connected){leftLaneLit=true;effects.LaneUnlock(new Vector3(-8,HordeBarricade.Thickness,20),true);LogEvent("lane_unlock",new Vector3(-8,0,20),-1);}
        if(!rightLaneLit && barricades[1].connected){rightLaneLit=true;effects.LaneUnlock(new Vector3(8,HordeBarricade.Thickness,20),false);LogEvent("lane_unlock",new Vector3(8,0,20),1);}
        foreach(var g in gates)
        {
            if(!g.unlocked && (g.side<0?leftLaneLit:rightLaneLit))g.unlocked=true;
            if(!g.unlocked)continue;
            g.z-=GateSpeed*dt;float deck=HordeBarricade.Thickness*Mathf.Clamp01((2.4f-Mathf.Abs(g.z-20))/1.5f);g.rb.MovePosition(new Vector3(g.side*GateX,deck,g.z));
            if(g.side>0 && !g.used)foreach(var h in heroes){float dx=Mathf.Max(0,Mathf.Abs(h.rb.position.x-g.side*GateX)-GateWidth*.5f),dz=Mathf.Max(0,Mathf.Abs(h.rb.position.z-g.z)-.25f);min99Gap=Mathf.Min(min99Gap,new Vector2(dx,dz).magnitude);}
            if(g.z<-15 || (g.used && t-g.usedAt>.12f))
            {
                float max=22;foreach(var other in gates)if(other.side==g.side)max=Mathf.Max(max,other.z);
                g.z=max+GateSpacing;g.serial=gateSerial++;g.used=false;g.trigger.enabled=true;foreach(var v in g.visuals)v.enabled=true;g.rb.position=new Vector3(g.side*GateX,0,g.z);gateRecycled++;LogEvent("gate_recycle",g.rb.position,g.side);
            }
        }
    }
    public void CrossGate(int index,int side,Collider actor)
    {
        if(defeated || !gates[index].unlocked || gates[index].used)return;
        var h=heroes.Find(x=>x.root.gameObject==actor.gameObject);if(h==null || h.joining || Mathf.Abs(actor.transform.position.z-gates[index].rb.position.z)>.5f)return;
        var g=gates[index];g.used=true;g.usedAt=Time.time;g.trigger.enabled=false;foreach(var v in g.visuals)v.enabled=false;
        int amount=side<0?1:99;gateConsumed++;if(side<0)awards++;else plus99++;
        events.Add(new EventItem{kind="gate_consume",t=Time.time,p=g.root.position,value=amount,id=g.serial});
        effects.GateConsume(g.root.position,side<0);LogEvent(side<0?"plus1":"plus99",g.root.position,amount);
        for(int i=0;i<amount;i++)SpawnHero(g.root.position+Vector3.up*.04f);
    }

    public Vector3 CrowdTarget(Vector3 p,int id)
    {
        float lane=mobs[id].laneX;
        if(p.z>18.3f)
        {
            int side=lane<0?-1:1;
            if(leftBreak<Time.time && rightBreak>Time.time)side=1;
            if(rightBreak<Time.time && leftBreak>Time.time)side=-1;
            // Above the choke point, steer to the part of the opening with standing headroom.
            float opening=side<0?-(4.45f+(id%3)*.15f):4.05f+(id%5)*.28f;
            float x=Mathf.Lerp(lane,opening,Mathf.Clamp01((26-p.z)/3));
            if(boss!=null && Mathf.Abs(p.z-boss.position.z)<2.6f && Mathf.Abs(x-boss.position.x)<1.6f)x=boss.position.x+(p.x<boss.position.x?-1:1)*1.7f;
            return new Vector3(x,0,p.z-4);
        }
        // Once through the beam, both streams follow the actual squad position.
        return new Vector3(playerX,0,5);
    }
    public void RecordPass(Vector3 p)
    {
        trianglePasses++;if((p.x<0 && leftBreak<Time.time) || (p.x>0 && rightBreak<Time.time))blockedSidePasses++;
        LogEvent("triangle_pass",p,p.x<0?-1:1);
    }
    public bool TryRespawn(out Vector3 spawn)
    {
        for(int k=0;k<CrowdColumns;k++)
        {
            int slot=respawnSlot++%CrowdColumns;spawn=new Vector3(-5.56f+slot*.53f,.04f,31.3f+(slot%2)*.18f);
            if(!Physics.CheckCapsule(spawn+Vector3.up*.26f,spawn+Vector3.up*1.58f,.28f,(1<<EnemyLayer)|(1<<BossLayer),QueryTriggerInteraction.Ignore)){crowdRecycled++;return true;}
        }
        spawn=Vector3.zero;return false;
    }
    void Supply(float t)
    {
        if(t<supplyAt)return;supplyAt=t+.45f;
        for(int col=0;col<CrowdColumns && mobs.Count<MaxPhysicalCrowd;col++)
        {
            Vector3 p=new Vector3(-5.56f+col*.53f,.04f,31.3f+(col%2)*.18f);
            if(Physics.CheckCapsule(p+Vector3.up*.26f,p+Vector3.up*1.58f,.28f,1<<EnemyLayer,QueryTriggerInteraction.Ignore))continue;
            var go=new GameObject("Source zombie "+mobs.Count);go.transform.position=p;var mob=go.AddComponent<HordeMob>();mob.Setup(this,mobs.Count);mobs.Add(mob);LogEvent("crowd_source",p,1);
        }
    }

    float LeadX(float t)
    {
        float[] times={0,3.65f,4.25f,5.50f,6.10f,10.06f,12.55f,15.7f,16.2f,17.75f,18.15f,21.35f,21.85f,24.35f,24.72f,25.7f,26.15f,10000};
        float[] xs={0,0,-5.2f,-5.2f,-8,-8,7.3f,7.3f,4.5f,4.5f,7.3f,7.3f,4.5f,4.5f,7.3f,7.3f,4.5f,4.5f};
        // Once the post actually breaks, stop returning to its firing column.
        // This is recorded player movement, not a gate immunity or a reward cap.
        if(t>rightBreak)return Mathf.Lerp(7.3f,4.5f,Mathf.SmoothStep(0,1,(t-rightBreak)/.45f));
        if(t>=24.72f && right.hp>0)return 7.3f;
        for(int i=1;i<times.Length;i++)if(t<times[i])return Mathf.Lerp(xs[i-1],xs[i],Mathf.SmoothStep(0,1,(t-times[i-1])/(times[i]-times[i-1])));
        return 5.5f;
    }
    void FixedUpdate()
    {
        var timer=System.Diagnostics.Stopwatch.StartNew();
        float t=Time.time,dt=Time.fixedDeltaTime;if(!interactive)playerX=LeadX(t);
        foreach(var bar in barricades)bar.Step(t);
        foreach(var mob in mobs)mob.Drive(dt,t);
        foreach(var mob in mobs)if(mob.alive && mob.body.position.z<18.3f && Vector3.Dot(mob.desiredVelocity,new Vector3(playerX-mob.body.position.x,0,5-mob.body.position.z))<-.001f)retreatCommands++;
        Supply(t);
        DriveBoss(t,dt);
        int count=heroes.Count;
        for(int i=0;i<count;i++)
        {
            var h=heroes[i];if(defeated)continue;
            Vector3 p=Formation(i,count);float join=Mathf.Clamp01((t-h.born)/.32f);
            if(h.joining && join<1)p=Vector3.Lerp(h.joinFrom,p,Mathf.SmoothStep(0,1,join))+Vector3.up*Mathf.Sin(join*Mathf.PI)*.28f;else h.joining=false;
            // Stable slot ownership and lag prevent formation flipping when crossing the centre.
            p=Vector3.SmoothDamp(h.rb.position,p,ref h.smoothVelocity,h.joining?.07f:.10f,18,dt);h.rb.MovePosition(p);h.rb.MoveRotation(Quaternion.identity);
            bool fire=false;if(i==0 && !upgraded && starterIndex<starterTimes.Length && t>=starterTimes[starterIndex]){starterIndex++;fire=true;}
            if(upgraded && t>=h.next && !h.joining){fire=true;h.next=Mathf.Max(h.next+(count==1?.08f:.085f),t+dt*.1f);}
            if(fire)Fire(h);
        }
        Gates(t,dt);
        for(int i=bullets.Count-1;i>=0;i--)
        {
            var b=bullets[i];float travel=HordeProjectilePath.Travel(b.v.magnitude,dt,b.travelled,HordeCombatRules.ShotRange);Vector3 next=b.p+Vector3.forward*travel;
            bool contact=HordeProjectilePath.FirstContact(gameObject.scene.GetPhysicsScene(),b.p,(next-b.p).magnitude,(1<<EnemyLayer)|(1<<BossLayer)|(1<<MetalLayer),out var collider,out var point,out var normal);
            if(contact){effects.Trace(b.p,point,t,b.heavy);Hit(collider,point,normal,b);events.Add(new EventItem{kind="projectile_hit",t=t,p=point,v=b.v,id=b.id,value=collider.gameObject.layer});bullets.RemoveAt(i);}
            else {b.p=next;b.travelled+=travel;if(b.travelled>=HordeCombatRules.ShotRange-.0001f)bullets.RemoveAt(i);}
        }
        if(capture && t>=sampleAt){Measure();sampleAt=t+.5f;}
        if(t>1 && stepMs.Count<60000)stepMs.Add((float)timer.Elapsed.TotalMilliseconds);
    }
    void DriveBoss(float t,float dt)
    {
        Vector3 bp=boss.position;
        float opening=leftBreak<Time.time?4.9f:-4.9f;
        Vector3 goal=bp.z>18.0f?new Vector3(bp.z>38?0:opening,0,16.7f):new Vector3(playerX,0,5);
        Vector3 dir=goal-bp;dir.y=0;
        float speed=t<19?2.0f:(t<25?1.6f:3.2f);Vector3 velocity=new Vector3(boss.velocity.x,0,boss.velocity.z);
        if(bossAttackAt<0 && bp.z<8.7f && !soak && bossHP>0){bossAttackAt=t;LogEvent("boss_windup",bp,1);bossView.Trigger("Attack");}
        if(bossAttackAt>=0)
        {
            float age=t-bossAttackAt;bossAttackPose=age<.45f?-Mathf.SmoothStep(0,1,age/.45f):Mathf.Clamp01((age-.45f)/.20f);
            if(age>=.65f && !bossImpact){bossImpact=true;Vector3 impact=new Vector3(bp.x,0,bp.z-2.6f);effects.Slam(impact);LogEvent("boss_slam",impact,5);foreach(var h in heroes)if(new Vector2(h.rb.position.x-impact.x,h.rb.position.z-impact.z).magnitude<5){Defeat(impact);break;}}
            speed=0;
        }
        if(soak && bp.z<13)speed=0;
        if(bossHP>0)boss.AddForce(Vector3.ClampMagnitude((dir.normalized*speed-velocity)*8,45),ForceMode.Acceleration);
        if(velocity.sqrMagnitude>.05f && bossAttackAt<0)boss.MoveRotation(Quaternion.RotateTowards(boss.rotation,Quaternion.LookRotation(velocity.normalized),100*dt));
        if(right.hp<=0 && t>bossStepAt && bp.y<.15f && bossAttackAt<0 && Physics.SphereCast(bp+Vector3.up*.4f,.3f,dir.normalized,out var hit,1.5f,1<<MetalLayer,QueryTriggerInteraction.Ignore)){boss.AddForce(Vector3.up*4.2f,ForceMode.VelocityChange);bossStepAt=t+1.2f;}
    }
    void Fire(Hero h)
    {
        Vector3 muzzle=h.muzzle.position;var b=new Bullet{p=muzzle,v=HordeProjectilePath.Velocity(upgraded?82:52),heavy=upgraded,id=++totalShots};bullets.Add(b);
        effects.Shoot(muzzle,heroes.Count,upgraded);events.Add(new EventItem{kind="shot",t=Time.time,p=muzzle,v=b.v,id=b.id,value=upgraded?1:0});h.kick=1;h.view.Trigger("Fire");
    }

    void Hit(Collider collider,Vector3 point,Vector3 normal,Bullet b)
    {
        var go=collider.gameObject;Vector3 direction=b.v.normalized;
        if(go==gold || go==plinth)
        {
        goldHP--;goldText.text=goldHP.ToString();effects.Impact(point,normal,true);plinth.GetComponent<HordeFracture>().Damage(point,3-goldHP);LogEvent("gold_hit",point,goldHP);
            if(goldHP<=0){unlockTime=Time.time;gold.SetActive(false);plinth.SetActive(false);goldText.gameObject.SetActive(false);release.SetActive(true);StartCoroutine(ReleaseBars());HordeFracture.Break("plinth",plinth.transform.position);effects.Fracture();LogEvent("gold_fracture",plinth.transform.position,1);}
            return;
        }
        if(go==left.go){HitPost(left,point,normal);return;}if(go==right.go){HitPost(right,point,normal);return;}
        if(collider.attachedRigidbody==boss)
        {
            if(bossHP<=0)return;
            bossHP=Mathf.Max(0,bossHP-1);bossHits++;bossFlashUntil=Time.time+.028f;effects.Impact(point,normal,false);LogEvent("boss_hit",point,bossHP);
            if(bossHP==0){bossAttackAt=-1;bossAttackPose=0;bossView.Trigger("Death");boss.constraints=RigidbodyConstraints.None;boss.AddTorque(Vector3.right*160,ForceMode.Impulse);LogEvent("boss_dead",boss.position,0);}return;
        }
        var mob=go.GetComponent<HordeMob>();if(mob!=null){mob.Hit(point,Vector3.forward,b.heavy);events.Add(new EventItem{kind="direct_enemy_damage",t=Time.time,p=point,id=b.id,value=mob.id});return;}
        bool metal=go.layer==MetalLayer;effects.Impact(point,normal,metal);if(metal)metalHits++;LogEvent(metal?"bar_hit":"corpse_hit",point,1);
        if(collider.attachedRigidbody!=null && !collider.attachedRigidbody.isKinematic)collider.attachedRigidbody.AddForceAtPosition(Vector3.forward*(metal?9:4),point,ForceMode.Impulse);
    }
    void HitPost(Pillar p,Vector3 point,Vector3 normal)
    {
        if(p.hp<=0)return;p.damageRemainder+=p.side>0?.55f:1;int damage=Mathf.FloorToInt(p.damageRemainder);p.damageRemainder-=damage;p.hp=Mathf.Max(0,p.hp-damage);p.text.text=p.hp.ToString();metalHits++;effects.Impact(point,normal,true);LogEvent("post_hit",point,p.hp);
        var fracture=p.go.GetComponent<HordeFracture>();if(fracture==null){fracture=p.go.AddComponent<HordeFracture>();fracture.size=new Vector3(2.7f,5.5f,1.65f);fracture.resource="post";}
        fracture.Damage(point,p.side<0?3-p.hp:Mathf.Clamp(1+(600-p.hp)/210,1,3));
        if(p.hp==0)
        {
            StartCoroutine(BreakPillar(p));
        }
    }
    System.Collections.IEnumerator BreakPillar(Pillar p)
    {
        yield return new WaitForSeconds(.14f);p.go.SetActive(false);p.text.gameObject.SetActive(false);int j=p.side<0?0:1;barricades[j].Collapse();
        if(p.side<0)leftBreak=Time.time;else rightBreak=Time.time;HordeFracture.Break("post",p.go.transform.position);effects.Fracture();LogEvent("post_break",p.go.transform.position,p.side);shake=.045f;
    }

    void Shatter(Vector3 center,Vector3 size,int count)
    {
        for(int k=0;k<count;k++)
        {
            Vector3 p=center+Vector3.Scale(UnityEngine.Random.insideUnitSphere,size*.45f);var bit=HordeMeshes.Box("Physical yellow fragment",p,Vector3.one*UnityEngine.Random.Range(.10f,.25f),HordeMeshes.Metal);bit.layer=CorpseLayer;var rb=bit.AddComponent<Rigidbody>();rb.mass=.2f;rb.velocity=(p-center).normalized*2+Vector3.up;rb.angularVelocity=UnityEngine.Random.insideUnitSphere*5;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;Destroy(bit,2.2f);
        }
    }
    System.Collections.IEnumerator ReleaseBars(){yield return new WaitForSeconds(.74f);foreach(var bar in barricades)bar.Release();}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))Time.timeScale=Time.timeScale==0?1:0;
        if(Input.GetKeyDown(KeyCode.Tab))interactive=!interactive;
        if(interactive)playerX=Mathf.Clamp(playerX+Input.GetAxisRaw("Horizontal")*Time.deltaTime*7,-8,8);
        if(Input.GetKeyDown(KeyCode.R))UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        if(Input.GetKeyDown(KeyCode.Escape) && !capture)Application.Quit();
        float t=Time.time;
        if(endShade!=null)endShade.SetColor("_BaseColor",new Color(0,0,0,.60f*Mathf.SmoothStep(0,1,(t-defeatTime-.65f)/.65f)));
        if(goldHP<=0 && !upgraded)
        {
            float a=Mathf.Clamp01((t-unlockTime-.40f)/.72f);release.transform.position=Vector3.Lerp(new Vector3(0,3.279f,19.1f),new Vector3(0,.03f,5),Mathf.SmoothStep(0,1,a))+Vector3.up*Mathf.Sin(a*Mathf.PI)*2;
            release.transform.localScale=Vector3.one*Mathf.Lerp(1.3f,.60f,a);
            if(a>=1)
            {
                upgraded=true;release.SetActive(false);var h=heroes[0];Destroy(h.view.gameObject);var visual=new GameObject("Visual");visual.transform.SetParent(h.root,false);h.view=visual.AddComponent<HordeActorView>();h.view.Setup(models,"heavy");h.muzzle=h.view.muzzle;h.next=Time.time+.015f;effects.Upgrade(h.root.position);LogEvent("upgrade",h.root.position,1);
            }
        }
        foreach(var h in heroes)
        {
            Vector3 velocity=(h.rb.position-h.last)/Mathf.Max(Time.deltaTime,.0001f);h.walked+=new Vector2(h.rb.position.x-h.last.x,h.rb.position.z-h.last.z).magnitude;h.last=h.rb.position;h.kick=Mathf.Max(0,h.kick-Time.deltaTime*11);h.view.Pose(velocity,h.walked,h.kick);
        }
        UpdateCamera();Vector3 d=boss.position-bossPrevious;bossWalked+=new Vector2(d.x,d.z).magnitude;bossPrevious=boss.position;
        float duck=Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,3,Mathf.Abs(boss.position.z-20)));bossView.Pose(boss.velocity,bossWalked,0,Mathf.Clamp01((bossFlashUntil-t)/.028f)*.18f,bossAttackPose,duck);

        Vector3 hp=boss.position+Vector3.up*7;bossHPBack.SetActive(t>21);bossHPFill.SetActive(t>21);bossHPBack.transform.position=hp;bossHPBack.transform.rotation=mainCamera.transform.rotation;
        float frac=Mathf.Clamp01(bossHP/1000f);bossHPFill.transform.position=hp+mainCamera.transform.right*(frac-1)*1.68f-mainCamera.transform.forward*.028f;bossHPFill.transform.rotation=mainCamera.transform.rotation;bossHPFill.transform.localScale=new Vector3(3.35f*frac,.16f,.04f);
    }
    void UpdateCamera()
    {
        float x=playerX*.38f;
        mainCamera.transform.position=new Vector3(x,22.27f,-26.55f);mainCamera.transform.LookAt(new Vector3(x,0,20.32f));
        if(!defeated && heroes.Count>1)
        {
            // Rear rows project wider than the leader; reserve a margin for the entire vehicle body.
            float left=-100,right=100,tangent=Mathf.Tan(mainCamera.fieldOfView*Mathf.Deg2Rad*.5f)*mainCamera.aspect;
            foreach(var h in heroes)
            {
                float depth=Vector3.Dot(h.rb.position-mainCamera.transform.position,mainCamera.transform.forward);
                float halfWidth=depth*tangent*.90f;
                left=Mathf.Max(left,h.rb.position.x+.74f-halfWidth);right=Mathf.Min(right,h.rb.position.x-.74f+halfWidth);
            }
            if(left<=right)x=Mathf.Clamp(x,left,right);
            mainCamera.transform.position=new Vector3(x,22.27f,-26.55f);mainCamera.transform.LookAt(new Vector3(x,0,20.32f));
        }
        if(shake>0){mainCamera.transform.position+=UnityEngine.Random.insideUnitSphere*shake;shake=Mathf.Max(0,shake-Time.deltaTime*.4f);}
    }
    void Measure()
    {
        var combat=new CombatSample{t=Time.time,retreatCommands=retreatCommands};
        foreach(var m in mobs)if(m.alive)
        {
            Vector3 p=m.body.position;
            if(p.z<18.3f && p.z>5 && p.x<0)combat.leftForecourt++;
            if(m.passageSide<0){combat.leftPassSurvivors++;events.Add(new EventItem{kind="left_survivor_sample",t=Time.time,p=p,id=m.id});}
            if(p.z<17.0f && p.z>5 && p.x>=0)combat.rightForecourt++;
            if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(playerX,5))<3)combat.nearPlayer++;
        }
        combatSamples.Add(combat);
        if(!defeated)foreach(var m in mobs)if(m.alive)closestEnemy=Mathf.Min(closestEnemy,Vector2.Distance(new Vector2(m.body.position.x,m.body.position.z),new Vector2(playerX,5)));
        maxBeamGap=Mathf.Max(maxBeamGap,Vector3.Distance(barricades[0].InnerTip,barricades[1].InnerTip));
        int alive=0,moved=0,pairs=0;float total=0,max=0;var buckets=new Dictionary<Vector2Int,List<HordeMob>>();
        foreach(var m in mobs){if(m.alive)alive++;if(m.totalDistance>.35f)moved++;total+=m.totalDistance;if(!m.alive)continue;var p=m.body.position;var cell=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z));if(!buckets.TryGetValue(cell,out var list)){list=new List<HordeMob>();buckets[cell]=list;}list.Add(m);}
        foreach(var m in mobs)
        {
            if(!m.alive)continue;Vector3 p=m.body.position;var cell=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.z));
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)if(buckets.TryGetValue(cell+new Vector2Int(x,z),out var near))foreach(var n in near)
            {if(n.id<=m.id)continue;Vector3 d=n.body.position-p;if(new Vector2(d.x,d.z).sqrMagnitude>.56f*.56f)continue;if(Physics.ComputePenetration(m.capsule,m.body.position,m.body.rotation,n.capsule,n.body.position,n.body.rotation,out var separation,out float overlap)){max=Mathf.Max(max,overlap);if(overlap>.04f)pairs++;}}
        }
        samples.Add(new Sample{t=Time.time,alive=alive,moved=moved,meanTravel=total/mobs.Count,deepOverlapPairs=pairs,maxOverlap=max,movingNow=mobs.FindAll(m=>m.alive && new Vector2(m.body.velocity.x,m.body.velocity.z).magnitude>.1f).Count,gateConsumed=gateConsumed,gateRecycled=gateRecycled,crowdRecycled=crowdRecycled,min99Gap=min99Gap,squad=heroes.Count,postLeft=left.hp,postRight=right.hp,bossHealth=bossHP,bossPosition=boss.position,leftBarPosition=bars[0].position,rightBarPosition=bars[1].position});
    }
    public void LogEvent(string kind,Vector3 p,float value){events.Add(new EventItem{kind=kind,t=Time.time,p=p,value=value});}
    public void TouchHero(Collider other){}
    void Defeat(Vector3 point)
    {
        if(defeated || soak)return;
        defeated=true;defeatTime=Time.time;
        var shade=GameObject.CreatePrimitive(PrimitiveType.Quad);shade.name="Final scene hold";Destroy(shade.GetComponent<Collider>());shade.transform.SetParent(mainCamera.transform,false);shade.transform.localPosition=Vector3.forward*.3f;
        float height=Mathf.Tan(mainCamera.fieldOfView*Mathf.Deg2Rad*.5f)*.6f;shade.transform.localScale=new Vector3(height*mainCamera.aspect*1.01f,height*1.01f,1);
        endShade=new Material(Shader.Find("Horde/Glow"));endShade.renderQueue=4000;endShade.SetColor("_BaseColor",Color.clear);shade.GetComponent<Renderer>().sharedMaterial=endShade;
        for(int i=0;i<heroes.Count;i++)
        {
            var h=heroes[i];var col=h.root.GetComponent<CapsuleCollider>();col.isTrigger=false;col.height=.72f;col.center=Vector3.up*.36f;col.radius=.25f;
            h.rb.isKinematic=false;h.rb.mass=65;h.rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Vector3 launch=(h.rb.position-point).normalized*4+Vector3.up*4;launch.x=Mathf.Min(-1.2f,launch.x);h.rb.velocity=launch;h.rb.angularVelocity=h.root.right*3;h.view.Trigger("Death");
        }
        effects.Floor(new Vector3(playerX,0,5),8);LogEvent("defeat",heroes[0].root.position,bossHP);
    }
    void LateUpdate()
    {
        if(HordeIPEnvironment.Instance!=null)HordeIPEnvironment.Instance.Draw();
        if(distant!=null)distant.Draw(mainCamera,Time.time,boss.position);
        foreach(var b in bullets)effects.Trace(b.p-Vector3.forward*Mathf.Min(b.travelled,b.heavy?2.15f:2.0f),b.p,Time.time,b.heavy);
        HordeContactShadows.Draw(mainCamera);
        if(benchmark){if(Time.time>1)frameMs.Add(Time.unscaledDeltaTime*1000);if(Time.time>=duration){SaveReport();Debug.Log("HORDE_BENCHMARK_DONE");Application.Quit();}return;}
        if(!capture)return;if(noImages){if(Time.time>=duration){SaveReport();Debug.Log("HORDE_CAPTURE_DONE no-images");Application.Quit();}return;}
        var request=new RenderPipeline.StandardRequest{destination=target};
        RenderPipeline.SubmitRenderRequest(mainCamera,request);
        CaptureFrame();
    }
    void CaptureFrame()
    {
        RenderTexture previous=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,captureWidth,captureHeight),0,0);pixels.Apply(false);RenderTexture.active=previous;
        File.WriteAllBytes(Path.Combine(captureDir,"frame_"+captureFrame.ToString("D4")+".png"),pixels.EncodeToPNG());captureFrame++;
        if(captureFrame%30==0)Debug.Log("CAPTURE "+captureFrame+" time "+Time.time.ToString("F2"));
        if(captureFrame>=Mathf.RoundToInt(duration*captureFPS)){SaveReport();Debug.Log("HORDE_CAPTURE_DONE "+captureFrame);Application.Quit();}
    }
    void SaveReport()
    {
        stepMs.Sort();var perf=new PerformanceReport{startupMs=startupMs,fixedStepMedianMs=stepMs.Count>0?stepMs[stepMs.Count/2]:0,fixedStepP95Ms=stepMs.Count>0?stepMs[(int)((stepMs.Count-1)*.95f)]:0,closestEnemyToLead=closestEnemy,maxBeamJointGap=maxBeamGap,physicalAgents=mobs.Count,farInstancedAgents=HordeDistantCrowd.Count,trianglePasses=trianglePasses,closedLanePasses=blockedSidePasses,finalBeamTilt=new[]{barricades[0].Tilt,barricades[1].Tilt},connected=new[]{barricades[0].connected,barricades[1].connected},managedBytes=GC.GetTotalMemory(false)};
        frameMs.Sort();if(frameMs.Count>0){perf.frameMedianMs=frameMs[frameMs.Count/2];perf.frameP95Ms=frameMs[(int)((frameMs.Count-1)*.95f)];}
        string performanceDir=capture?captureDir:Application.persistentDataPath;Directory.CreateDirectory(performanceDir);File.WriteAllText(Path.Combine(performanceDir,"performance_report.json"),JsonUtility.ToJson(perf,true));
        File.WriteAllText(Path.Combine(performanceDir,"combat_report.json"),JsonUtility.ToJson(new CombatReport{retreatCommands=retreatCommands,samples=combatSamples.ToArray()},true));
        var r=new Report{framesPerSecond=captureFPS,crowd=mobs.Count,totalShots=totalShots,enemyDeaths=deaths,metalHits=metalHits,plus1=awards,bossHits=bossHits,plus99=plus99,gateConsumed=gateConsumed,gateRecycled=gateRecycled,crowdRecycled=crowdRecycled,duration=Time.time,events=events.ToArray(),samples=samples.ToArray()};string dir=capture?captureDir:Application.persistentDataPath;Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"simulation_report.json"),JsonUtility.ToJson(r,true));
    }
    void OnGUI()
    {
        if(capture)return;GUI.skin.font=uiFont;GUI.Label(new Rect(15,12,680,30),"空格：暂停 / 继续    Tab：切换手动    A、D：移动    R：重播");
    }
}

public class GateTrigger : MonoBehaviour
{
    public HordeDirector world;public int index,side;
    void OnTriggerEnter(Collider other){if(other.gameObject.layer==HordeDirector.HeroLayer)world.CrossGate(index,side,other);}
}
public class HeroContact : MonoBehaviour
{
    public HordeDirector world;
    void OnTriggerEnter(Collider other){world.TouchHero(other);}
}


