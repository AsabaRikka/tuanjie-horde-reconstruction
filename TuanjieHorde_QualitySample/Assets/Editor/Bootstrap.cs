using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public static class Bootstrap
{
    public static void CreateAndBuild()
    {
        InteractionTests.Run();
        foreach(string path in Directory.GetFiles("Assets/Resources/IP","*.png"))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;
            importer.sRGBTexture=path.Contains("BaseColor");importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Settings");
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/HordeRenderer.asset");
        if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,"Assets/Settings/HordeRenderer.asset");}
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/HordeURP.asset");
        if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,"Assets/Settings/HordeURP.asset");}
        pipeline.msaaSampleCount = 4;
        pipeline.renderScale = 1;
        pipeline.shadowDistance = 65;
        var pipelineSettings=new SerializedObject(pipeline);pipelineSettings.FindProperty("m_MainLightShadowmapResolution").intValue=4096;pipelineSettings.ApplyModifiedProperties();
        pipeline.supportsHDR = true;
        EditorUtility.SetDirty(pipeline);
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var included=graphics.FindProperty("m_AlwaysIncludedShaders");
        for(int i=included.arraySize-1;i>=0;i--){var shader=included.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(shader!=null && shader.name.StartsWith("Universal Render Pipeline/")){included.GetArrayElementAtIndex(i).objectReferenceValue=null;included.DeleteArrayElementAtIndex(i);}}
        foreach(string name in new[]{"Horde/Vertex Lit","Horde/Glow","Horde/Gate Glass","Horde/World Text","Horde/IP Actor","Horde/Contact Shadow","Horde/Radiance"})
        {var shader=Shader.Find(name);if(shader==null)throw new System.Exception("Missing shader "+name);int at=included.arraySize;included.InsertArrayElementAtIndex(at);included.GetArrayElementAtIndex(at).objectReferenceValue=shader;}
        graphics.ApplyModifiedProperties();
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.companyName = "Horde Scene Study";
        PlayerSettings.productName = "守桥尸潮 · Full Quality 04";
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var director=new GameObject("Horde Director").AddComponent<HordeDirector>();
        var models=AssetDatabase.LoadAssetAtPath<HordeModelSet>("Assets/Settings/ModelReplacements.asset");if(models==null){models=ScriptableObject.CreateInstance<HordeModelSet>();AssetDatabase.CreateAsset(models,"Assets/Settings/ModelReplacements.asset");}director.models=models;
        MakePreview();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/BridgeHorde.unity");
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene("Assets/Scenes/BridgeHorde.unity",true)};
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[]{"Assets/Scenes/BridgeHorde.unity"}, locationPathName = "Build/Horde.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        Debug.Log("HORDE_BUILD_RESULT " + report.summary.result);
        if(report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed");
    }
    static void MakePreview()
    {
        Directory.CreateDirectory("Assets/Models");Directory.CreateDirectory("Assets/Materials");HordeMeshes.Init();
        Mesh PersistMesh(string name){string path="Assets/Models/IP_"+name+".asset";var source=name=="environment"?HordeMeshes.Load(name):HordeIP.Mesh(name);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=Object.Instantiate(source);AssetDatabase.CreateAsset(mesh,path);}else EditorUtility.CopySerialized(source,mesh);return mesh;}
        Material PersistMat(string name,Material value){string path="Assets/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=Object.Instantiate(value);AssetDatabase.CreateAsset(m,path);}else EditorUtility.CopySerialized(value,m);return m;}
        var root=new GameObject("Editor Preview").transform;var skin=PersistMat("VertexColours",HordeMeshes.Skin);var steel=PersistMat("YellowSteel",HordeMeshes.Metal);
        HordeMeshes.Model("Bridge and forest",PersistMesh("environment"),Vector3.zero,skin,root);
        var enemy=PersistMesh("enemy_far");
        var combinedPath="Assets/Models/V6_PreviewCrowd_Lite.asset";
        var combined=AssetDatabase.LoadAssetAtPath<Mesh>(combinedPath);
        if(combined==null)
        {
            var instances=new CombineInstance[24*5];
            for(int row=0;row<5;row++)for(int col=0;col<24;col++)instances[row*24+col]=new CombineInstance{mesh=enemy,transform=Matrix4x4.TRS(new Vector3(-6.44f+col*.56f,0,22+row*.8f),Quaternion.Euler(0,180,0),HordeIP.Scale("enemy_far"))};
            combined=new Mesh{name="Combined editor crowd",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};combined.CombineMeshes(instances);AssetDatabase.CreateAsset(combined,combinedPath);
        }
        HordeMeshes.Model("IP crowd preview - one renderer",combined,Vector3.zero,PersistMat("IP_Enemy",HordeIP.Material("enemy")),root);
        var player=HordeMeshes.Model("IP base cannon car",PersistMesh("starter"),new Vector3(0,0,5),PersistMat("IP_Starter",HordeIP.Material("starter")),root);player.transform.localScale=HordeIP.Scale("starter");
        var captive=HordeMeshes.Model("IP upgraded battle car",PersistMesh("heavy"),new Vector3(0,3.279f,19.1f),PersistMat("IP_Heavy",HordeIP.Material("heavy")),root);captive.transform.localScale=Vector3.one*1.3f;
        HordeMeshes.Box("Raised unlock plinth",new Vector3(0,1.6425f,19.1f),new Vector3(3.15f,3.285f,2.55f),steel,false,root);
        foreach(int side in new[]{-1,1})for(int row=0;row<40;row++){string role=side<0?"gate1":"gate99";var gate=HordeMeshes.Model("Imported "+role,PersistMesh(role),new Vector3(side*7.5f,0,22+row*1.9f),PersistMat("V6_"+role,HordeIP.Material(role)),root);gate.transform.rotation=Quaternion.Euler(0,180,0);gate.transform.localScale=new Vector3(2.7f/1.7f,1.1f,1);}
        var giant=HordeMeshes.Model("IP giant boss",PersistMesh("boss"),new Vector3(0,0,64),PersistMat("IP_Boss",HordeIP.Material("boss")),root);giant.transform.localScale=Vector3.one*3.5f;giant.transform.rotation=Quaternion.Euler(0,180,0);
        foreach(int side in new[]{-1,1}){HordeMeshes.Box("Pillar",new Vector3(side*7.5f,2.75f,20),new Vector3(2.7f,5.5f,1.65f),steel,true,root);HordeMeshes.Box("Crossbar",new Vector3(side*4.4f,HordeBarricade.SupportedHeight(0),20),new Vector3(HordeBarricade.Length,HordeBarricade.Thickness,HordeBarricade.Depth),steel,true,root);}
        var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.parent=root;camera.transform.position=new Vector3(0,34,-30);camera.transform.LookAt(new Vector3(0,0,18));camera.fieldOfView=32.45f;camera.backgroundColor=new Color(.37f,.50f,.64f);camera.clearFlags=CameraClearFlags.SolidColor;
        var sun=new GameObject("Preview light").AddComponent<Light>();sun.transform.parent=root;sun.type=LightType.Directional;sun.intensity=1.5f;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.shadows=LightShadows.Soft;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,19),Quaternion.Euler(37,0,0),35);
    }
    public static void OpenForReview()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BridgeHorde.unity");
        EditorApplication.delayCall+=()=>{EditorApplication.isPlaying=true;};
    }
}
