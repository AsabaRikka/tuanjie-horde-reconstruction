using UnityEngine;
using System.Collections.Generic;

public class HordeEffects : MonoBehaviour
{
    public bool silent;float lastGun=-1,lastImpact=-1;
    ParticleSystem sparks,ice,dust,flash,aura;Material glow,particleGlow,goldGlow,tracerGlow,starterTracer;AudioSource sound;
    AudioClip shot,heavyShot,metal,body,crash,chime,fracture,recruit;
    void Play(AudioClip clip,float volume)
    {
        if(silent)return;float now=Time.unscaledTime;
        if(clip==shot || clip==heavyShot){if(now-lastGun<.014f)return;lastGun=now;}
        if(clip==metal || clip==body){if(now-lastImpact<.018f)return;lastImpact=now;}
        sound.PlayOneShot(clip,volume);
    }
    class Beam {public LineRenderer line;public float until;}
    readonly List<Beam> beams=new List<Beam>();int beamIndex;
    public void Setup()
    {
        glow=new Material(Shader.Find("Horde/Glow"));glow.SetColor("_BaseColor",new Color(.20f,2.1f,4,1));
        goldGlow=new Material(Shader.Find("Horde/Glow"));goldGlow.SetColor("_BaseColor",new Color(3,1.6f,.15f,1));
        particleGlow=new Material(Shader.Find("Horde/Glow"));particleGlow.SetColor("_BaseColor",new Color(1.7f,1.7f,1.7f,1));
        var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);var px=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float r=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;px[y*32+x]=new Color(1,1,1,Mathf.Clamp01((1-r)*3));}tex.SetPixels(px);tex.Apply();particleGlow.SetTexture("_BaseMap",tex);
        tracerGlow=new Material(Shader.Find("Horde/Radiance"));tracerGlow.SetColor("_BaseColor",new Color(.28f,1.1f,1.9f,1));
        starterTracer=new Material(tracerGlow);starterTracer.SetColor("_BaseColor",new Color(2.6f,1.8f,.12f,1));
        sparks=Make("Metal contact sparks",new Color(1,.65f,.13f),.032f,.3f,5.5f,true);
        ice=Make("Ice contact chips",new Color(.52f,.9f,1),.075f,.38f,2.3f,true);
        dust=Make("Concrete dust",new Color(.5f,.55f,.6f,.45f),.15f,.52f,1.2f,false);
        flash=Make("Contact flash",new Color(.3f,.85f,1),.48f,.09f,0,false);flash.GetComponent<ParticleSystemRenderer>().renderMode=ParticleSystemRenderMode.Billboard;
        aura=Make("Gate materialization column",new Color(1,.64f,.12f),.22f,.65f,3,false);aura.GetComponent<ParticleSystemRenderer>().renderMode=ParticleSystemRenderMode.Billboard;
        sound=gameObject.AddComponent<AudioSource>();sound.spatialBlend=0;sound.volume=.55f;
        shot=Resources.Load<AudioClip>("Audio/starter");heavyShot=Resources.Load<AudioClip>("Audio/heavy");metal=Resources.Load<AudioClip>("Audio/metal");body=Resources.Load<AudioClip>("Audio/body");crash=Resources.Load<AudioClip>("Audio/floor");chime=Resources.Load<AudioClip>("Audio/upgrade");fracture=Resources.Load<AudioClip>("Audio/fracture");recruit=Resources.Load<AudioClip>("Audio/recruit");
        for(int i=0;i<512;i++)
        {
            var go=new GameObject("Pooled tracer "+i);go.transform.parent=transform;var lr=go.AddComponent<LineRenderer>();lr.sharedMaterial=tracerGlow;lr.positionCount=2;lr.startWidth=.27f;lr.endWidth=.15f;lr.enabled=false;lr.textureMode=LineTextureMode.Stretch;lr.numCapVertices=2;
            beams.Add(new Beam{line=lr});
        }
    }
    ParticleSystem Make(string name,Color color,float size,float life,float speed,bool collision)
    {
        var go=new GameObject(name);go.transform.parent=transform;var p=go.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=p.main;m.loop=false;m.playOnAwake=false;m.simulationSpace=ParticleSystemSimulationSpace.World;m.startLifetime=life;m.startSpeed=speed;m.startSize=size;m.startColor=color;m.gravityModifier=collision?1:.03f;m.maxParticles=3500;
        var em=p.emission;em.enabled=false;var sh=p.shape;sh.enabled=false;
        var col=p.collision;col.enabled=collision;col.type=ParticleSystemCollisionType.World;col.collidesWith=1<<HordeDirector.GroundLayer;col.bounce=.25f;col.dampen=.38f;col.quality=ParticleSystemCollisionQuality.Medium;
        var cr=p.colorOverLifetime;cr.enabled=true;Gradient g=new Gradient();g.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});cr.color=g;
        var r=p.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=particleGlow;r.renderMode=ParticleSystemRenderMode.Stretch;r.lengthScale=collision?1.8f:.3f;p.Play();return p;
    }
    AudioClip MakeSound(string name,float duration,float hz,float decay,float noise)
    {
        const int sr=24000;float[] data=new float[Mathf.RoundToInt(sr*duration)];System.Random r=new System.Random(name.GetHashCode());
        for(int i=0;i<data.Length;i++){float t=i/(float)sr;data[i]=(Mathf.Sin(t*hz*Mathf.PI*2)*.45f+((float)r.NextDouble()*2-1)*noise)*Mathf.Exp(-t*decay);}
        var c=AudioClip.Create(name,data.Length,1,sr,false);c.SetData(data,0);return c;
    }
    public void Trace(Vector3 from,Vector3 to,float now,bool heavy=true)
    {
        float length=heavy?2.15f:2.0f;if((to-from).magnitude>length)from=to-Vector3.forward*length;
        var b=beams[beamIndex++%beams.Count];b.line.sharedMaterial=heavy?tracerGlow:starterTracer;b.line.startWidth=heavy?.42f:.30f;b.line.endWidth=heavy?.34f:.22f;b.line.SetPosition(0,from);b.line.SetPosition(1,to);b.line.enabled=true;b.until=now+.001f;
    }
    public void Shoot(Vector3 muzzle,int count,bool heavy=false)
    {
        Play(heavy?heavyShot:shot,count>1?.28f:.70f);Burst(heavy?ice:sparks,muzzle,Vector3.forward,5,2.0f);var e=new ParticleSystem.EmitParams{position=muzzle+Vector3.forward*.08f,velocity=Vector3.zero,startColor=heavy?new Color(.52f,.93f,1):new Color(1,.89f,.66f),startSize=heavy?.43f:.40f,startLifetime=heavy?.045f:.15f};flash.Emit(e,1);
    }
    void Burst(ParticleSystem p,Vector3 pos,Vector3 normal,int count,float speed)
    {
        for(int i=0;i<count;i++){var e=new ParticleSystem.EmitParams();e.position=pos;e.velocity=(normal+Random.insideUnitSphere*.85f).normalized*Random.Range(speed*.4f,speed);p.Emit(e,1);}
    }
    public void Impact(Vector3 point,Vector3 normal,bool isMetal,bool heavy=false)
    {
        Burst(isMetal?sparks:ice,point+normal*.025f,normal,isMetal?12:10,isMetal?5.8f:2.8f);
        var e=new ParticleSystem.EmitParams{position=point+normal*.04f,velocity=Vector3.zero,startColor=isMetal?new Color(.48f,.88f,1):new Color(.66f,.89f,1),startSize=isMetal?.64f:.38f,startLifetime=isMetal?.075f:.06f};flash.Emit(e,1);
        if(isMetal)Burst(ice,point+normal*.05f,normal,6,3.5f);
        Play(isMetal?metal:body,isMetal?.42f:.43f);
    }
    public void Floor(Vector3 point,float force){Burst(dust,point,Vector3.up,36,2.5f);Play(crash,Mathf.Clamp01(force/7)*.6f);}
    public void Fracture(){Play(fracture,.85f);}
    public void IceWave(Vector3 point,float radius)
    {
        Circle(new Vector3(point.x,.08f,point.z),radius,.24f,glow,.055f);
        Burst(ice,point,Vector3.up,12,3.5f);
    }
    public void Upgrade(Vector3 point)
    {
        GateConsume(point,false);
    }
    public void GateConsume(Vector3 p,bool plus1)
    {
        Play(plus1?recruit:chime,plus1?.65f:.8f);HordeUpgradeLight.Create(p,plus1);
    }
    public void LaneUnlock(Vector3 p,bool blue)
    {
        Play(chime,.65f);Circle(p+Vector3.up*.05f,1.25f,.55f,blue?glow:goldGlow,.12f);
        var path=new GameObject("Road connection sweep");var sweep=path.AddComponent<LineRenderer>();sweep.sharedMaterial=blue?glow:goldGlow;sweep.positionCount=2;sweep.startWidth=sweep.endWidth=.13f;
        var animation=path.AddComponent<RoadConnectionSweep>();animation.start=p;animation.line=sweep;
        Column(p,4.2f,1.2f,.6f,blue?glow:goldGlow);
        for(int i=0;i<55;i++){var e=new ParticleSystem.EmitParams{position=p+new Vector3(Random.Range(-.65f,.65f),Random.Range(0,3.5f),Random.Range(-.3f,.3f)),velocity=Vector3.up*1.2f,startColor=blue?new Color(.22f,.8f,1):new Color(1,.62f,.1f),startSize=Random.Range(.2f,.55f),startLifetime=.5f};aura.Emit(e,1);}
    }
    void Circle(Vector3 p,float radius,float life,Material material,float width)
    {
        var obj=new GameObject("Expanding impact ring");obj.transform.position=p;var lr=obj.AddComponent<LineRenderer>();lr.sharedMaterial=material;lr.useWorldSpace=false;lr.loop=true;lr.positionCount=64;lr.startWidth=lr.endWidth=width;
        for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;lr.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}obj.AddComponent<ExpandingRing>().duration=life;
    }
    void Column(Vector3 p,float height,float width,float life,Material material)
    {
        var obj=new GameObject("Materialization light pillar");obj.transform.position=p;
        var lr=obj.AddComponent<LineRenderer>();lr.sharedMaterial=material;lr.useWorldSpace=false;lr.positionCount=2;
        lr.SetPosition(0,Vector3.up*.05f);lr.SetPosition(1,Vector3.up*height);lr.startWidth=width;lr.endWidth=width*.22f;
        var color=new Gradient();color.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.7f,0),new GradientAlphaKey(0,1)});lr.colorGradient=color;
        obj.AddComponent<GateColumnFade>().duration=life;
    }
    public void Slam(Vector3 p)
    {
        Floor(p,12);Circle(p+Vector3.up*.04f,4.8f,.9f,goldGlow,.19f);Circle(p+Vector3.up*.05f,3.2f,1.2f,goldGlow,.10f);Burst(sparks,p,Vector3.up,150,7);Burst(dust,p,Vector3.up,100,3);
        for(int k=0;k<20;k++)
        {
            var o=new GameObject("Ground crack flash");var line=o.AddComponent<LineRenderer>();line.sharedMaterial=goldGlow;line.positionCount=7;line.startWidth=.055f;line.endWidth=.009f;float a=k*Mathf.PI*2/20;
            for(int j=0;j<7;j++){float r=j*.6f;line.SetPosition(j,p+new Vector3(Mathf.Cos(a+Random.Range(-.1f,.1f))*r,.045f,Mathf.Sin(a+Random.Range(-.1f,.1f))*r));}Destroy(o,1.1f);
        }
    }
    void Update(){foreach(var b in beams)if(b.line.enabled && Time.time>b.until)b.line.enabled=false;}
}

public class RoadConnectionSweep:MonoBehaviour
{
    public Vector3 start;public LineRenderer line;float born;
    void Start(){born=Time.time;}
    void Update()
    {
        float t=(Time.time-born)/.65f;float z=Mathf.Lerp(20,5,Mathf.Clamp01(t));
        float y=Mathf.Lerp(start.y,.06f,Mathf.Clamp01((20-z)/2.5f));
        line.SetPosition(0,new Vector3(start.x-.86f,y,z));line.SetPosition(1,new Vector3(start.x+.86f,y,z));
        line.startColor=line.endColor=new Color(1,1,1,1-t);if(t>=1)Destroy(gameObject);
    }
}

public class GateColumnFade:MonoBehaviour
{
    public float duration;float born;LineRenderer line;MaterialPropertyBlock block;
    void Start(){born=Time.time;line=GetComponent<LineRenderer>();block=new MaterialPropertyBlock();}
    void Update(){float t=(Time.time-born)/duration;transform.localScale=new Vector3(1,Mathf.Lerp(.5f,1,t),1);block.SetColor("_BaseColor",line.sharedMaterial.GetColor("_BaseColor")*new Color(1,1,1,1-t));line.SetPropertyBlock(block);if(t>=1)Destroy(gameObject);}
}

public class ExpandingRing : MonoBehaviour
{
    public float duration=.5f;float born;
    void Start(){born=Time.time;}
    void Update(){float t=(Time.time-born)/duration;transform.localScale=Vector3.one*Mathf.Lerp(.2f,1,t);var line=GetComponent<LineRenderer>();line.startColor=line.endColor=new Color(1,1,1,1-t);if(t>=1)Destroy(gameObject);}
}

public class RagdollExpiry : MonoBehaviour
{
    public float lifetime=2.2f;float born;Renderer[] renderers;MaterialPropertyBlock props;
    void Start(){born=Time.time;renderers=GetComponentsInChildren<Renderer>();props=new MaterialPropertyBlock();}
    void Update(){float age=Time.time-born;if(age<.12f){props.SetFloat("_Flash",Mathf.Clamp01(1-age/.07f));foreach(var r in renderers)r.SetPropertyBlock(props);}if(age>lifetime)Destroy(gameObject);}
}

public class BarContact : MonoBehaviour
{
    public HordeDirector director;public bool falling;float last=-10,lastCrush=-10;
    void OnCollisionEnter(Collision collision)
    {
        Crush(collision);
        if(collision.gameObject.layer!=HordeDirector.GroundLayer || Time.time-last<.18f)return;
        last=Time.time;float speed=collision.relativeVelocity.magnitude;if(speed<.45f)return;
        director.effects.Floor(collision.GetContact(0).point,speed);director.LogEvent("bar_floor",collision.GetContact(0).point,speed);
    }
    void OnCollisionStay(Collision collision){Crush(collision);}
    void Crush(Collision collision)
    {
        if(!falling || collision.gameObject.layer!=HordeDirector.EnemyLayer)return;
        var mob=collision.gameObject.GetComponent<HordeMob>();if(mob==null || !mob.alive)return;
        lastCrush=Time.time;mob.Hit(collision.GetContact(0).point,(Vector3.down+Vector3.forward*.2f).normalized,true);director.LogEvent("bar_crush",collision.GetContact(0).point,1);
    }
}
