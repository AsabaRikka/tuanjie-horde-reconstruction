using UnityEngine;
using UnityEngine.Rendering;

public static class HordeTexturedEnvironment
{
    public static void Create(Transform parent=null)
    {
        var root=new GameObject("Textured winter environment").transform;root.SetParent(parent,false);
        var road=HordeMeshes.Mat("Fine asphalt",Color.white);road.SetFloat("_VertexColors",0);road.SetTexture("_SurfaceMap",Resources.Load<Texture2D>("IP/Road_BaseColor"));road.SetFloat("_SurfaceTexture",1);road.SetFloat("_TextureScale",.09f);road.SetFloat("_SurfaceGrain",.018f);
        var forest=HordeMeshes.Mat("Quilted reference forest",Color.white);forest.SetFloat("_VertexColors",0);forest.SetTexture("_SurfaceMap",Resources.Load<Texture2D>("IP/Forest_Quality_BaseColor"));forest.SetFloat("_SurfaceTexture",1);forest.SetFloat("_TextureScale",.018f);forest.SetFloat("_Unlit",.85f);
        var concrete=HordeMeshes.Mat("Frosted parapet",new Color(.37f,.47f,.58f));concrete.SetFloat("_VertexColors",0);
        HordeMeshes.Box("Asphalt deck",new Vector3(0,-.1f,54),new Vector3(18,.18f,180),road,false,root);
        foreach(int side in new[]{-1,1})
        {
            var terrain=HordeMeshes.Box("Forest textured terrain",new Vector3(side*54,-.65f,55),new Vector3(90,.2f,250),forest,false,root);terrain.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            HordeMeshes.Box("Bridge parapet",new Vector3(side*9.20f,.38f,54),new Vector3(.38f,.8f,180),concrete,false,root);
            HordeMeshes.Box("Parapet highlight",new Vector3(side*9.20f,.80f,54),new Vector3(.42f,.09f,180),concrete,false,root);
            HordeMeshes.Box("Hazard rail",new Vector3(side*8.96f,.025f,54),new Vector3(.16f,.03f,180),HordeMeshes.Black,false,root);
            for(int z=-32;z<145;z+=2){var stripe=HordeMeshes.Box("Yellow edge marking",new Vector3(side*8.96f,.047f,z),new Vector3(.17f,.025f,.84f),HordeMeshes.Gold,false,root);stripe.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        }
    }
}
