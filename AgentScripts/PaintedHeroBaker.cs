using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bakes one illustrated turnaround through a small 2D deformation rig; never edits the gameplay scene.</summary>
public static class PaintedHeroBaker
{
    const string Source = "ArtDirection/HeroPainted2D/turnaround.png";
    const string Output = "Assets/_Project/Art/HeroPainted2D/";
    const int Cell = 256, RenderSize = 512, Cols = 52, Rows = 56;
    const float Height = 1.25f, PixelsPerUnit = 128, PivotY = .23f;
    static readonly string[] Directions = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
    sealed class View
    {
        public string Name;
        public RectInt Region;
        public Vector2 Foot;
        public float Top;
        public View(string name, RectInt region, Vector2 foot, float top) { Name=name;Region=region;Foot=foot;Top=top; }
    }
    sealed class Clip
    {
        public string id; public int frames, fps; public bool loop; public float impactPhase;
        public Clip(string name,int count,int rate,bool loops,float impact=0) { id=name;frames=count;fps=rate;loop=loops;impactPhase=impact; }
    }
    static readonly View[] Views = {
        new View("front",new RectInt(105,32,607,746),new Vector2(337,757),42),
        new View("profile",new RectInt(712,33,621,750),new Vector2(984,763),45),
        new View("back",new RectInt(1418,37,548,739),new Vector2(1608,757),49)
    };
    static readonly Clip[] Clips = {
        new Clip("idle",8,8,true),new Clip("run",12,18,true),new Clip("attack",8,20,false,4f/7),
        new Clip("sweep",10,20,false,5f/9),new Clip("dash",4,16,true),new Clip("hurt",4,16,false),new Clip("down",6,12,false)
    };

    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before baking the painted hero.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory("Verification/HeroPainted2D");
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!source.LoadImage(File.ReadAllBytes(Source)))throw new IOException("Cannot load the painted turnaround.");
        source.filterMode=FilterMode.Bilinear;source.wrapMode=TextureWrapMode.Clamp;
        var scene=EditorSceneManager.NewPreviewScene();
        var root=new GameObject("Painted hero bake · isolated preview");SceneManager.MoveGameObjectToScene(root,scene);
        var cameraNode=new GameObject("2D orthographic export");SceneManager.MoveGameObjectToScene(cameraNode,scene);
        var camera=cameraNode.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;
        camera.orthographic=true;camera.orthographicSize=1;camera.aspect=1;
        camera.transform.position=new Vector3(0,(.5f-PivotY)*2,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.allowHDR=false;camera.allowMSAA=true;
        var high=new RenderTexture(RenderSize,RenderSize,24,RenderTextureFormat.ARGB32){antiAliasing=4};high.Create();
        var small=new RenderTexture(Cell,Cell,0,RenderTextureFormat.ARGB32);small.Create();
        camera.targetTexture=high;
        var material=new Material(Shader.Find("Sprites/Default")){mainTexture=source};
        var mesh=new Mesh();mesh.name="Six-region painted hero rig";
        root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
        var read=new Texture2D(Cell,Cell,TextureFormat.RGBA32,false);
        var previous=RenderTexture.active;
        var checks=new List<object>();int clipped=0,empty=0;long totalBytes=0;
        try
        {
            var triangles=new int[Cols*Rows*6];int ti=0;
            for(int y=0;y<Rows;y++)for(int x=0;x<Cols;x++)
            {
                int a=y*(Cols+1)+x,b=a+1,c=a+Cols+1,d=c+1;
                triangles[ti++]=a;triangles[ti++]=c;triangles[ti++]=b;
                triangles[ti++]=b;triangles[ti++]=c;triangles[ti++]=d;
            }
            foreach(var clip in Clips)
            {
                var atlas=new Texture2D(Cell*clip.frames,Cell*8,TextureFormat.RGBA32,false);
                try
                {
                    for(int direction=0;direction<8;direction++)
                    {
                        var view=Views[direction>=1 && direction<=3 ? 2 : direction==0 || direction==4 ? 1 : 0];
                        bool flip=direction==3 || direction==4 || direction==5;
                        var rest=new Vector3[(Cols+1)*(Rows+1)];var uv=new Vector2[rest.Length];
                        float unit=Height/(view.Foot.y-view.Top);
                        for(int y=0;y<=Rows;y++)for(int x=0;x<=Cols;x++)
                        {
                            int i=y*(Cols+1)+x;
                            float px=view.Region.x+(float)x/Cols*view.Region.width;
                            float py=view.Region.y+(float)y/Rows*view.Region.height;
                            rest[i]=new Vector3((px-view.Foot.x)*unit,(view.Foot.y-py)*unit,0);
                            uv[i]=new Vector2(px/source.width,1-py/source.height);
                        }
                        mesh.Clear();mesh.vertices=rest;mesh.uv=uv;mesh.colors=Enumerable.Repeat(Color.white,rest.Length).ToArray();mesh.triangles=triangles;
                        for(int frame=0;frame<clip.frames;frame++)
                        {
                            float t=(float)frame/(clip.loop?clip.frames:Math.Max(1,clip.frames-1));
                            mesh.vertices=rest.Select(p=>Deform(p,clip.id,t,flip,view.Name=="profile")).ToArray();mesh.RecalculateBounds();
                            camera.Render();Graphics.Blit(high,small);RenderTexture.active=small;
                            read.ReadPixels(new Rect(0,0,Cell,Cell),0,0,false);read.Apply(false,false);
                            var pixels=read.GetPixels32();int count=0;bool border=false;
                            for(int i=0;i<pixels.Length;i++)
                            {
                                var p=pixels[i];
                                if(p.a>12){count++;int x=i%Cell,y=i/Cell;if(x<2 || x>=Cell-2 || y<2 || y>=Cell-2)border=true;}
                                // Sprites/Default renders premultiplied alpha; PNG sprites require straight alpha.
                                if(p.a>0 && p.a<255){p.r=(byte)Math.Min(255,p.r*255/p.a);p.g=(byte)Math.Min(255,p.g*255/p.a);p.b=(byte)Math.Min(255,p.b*255/p.a);pixels[i]=p;}
                            }
                            if(border)clipped++;if(count<100)empty++;
                            atlas.SetPixels32(frame*Cell,(7-direction)*Cell,Cell,Cell,pixels);
                            if(frame==0)checks.Add(new {clip=clip.id,direction,view=view.Name,flip,opaquePixels=count,border});
                        }
                    }
                    atlas.Apply(false,false);byte[] png=atlas.EncodeToPNG();File.WriteAllBytes(Output+clip.id+".png",png);totalBytes+=png.Length;
                }
                finally{UnityEngine.Object.DestroyImmediate(atlas);}
            }
            var manifest=new {version=2,frameSize=Cell,pixelsPerUnit=PixelsPerUnit,pivot=new[]{.5f,PivotY},directions=Directions,clips=Clips,
                source=Source,sourceViews=3,visualFacingDirections=4,renderFrameSize=RenderSize,method="Illustrated 2D mesh animation baked in Unity"};
            File.WriteAllText(Output+"manifest.json",JsonConvert.SerializeObject(manifest,Formatting.Indented)+"\n");
            var report=new {frames=Clips.Sum(c=>c.frames)*8,clipped,empty,pngBytes=totalBytes,checks};
            string json=JsonConvert.SerializeObject(report,Formatting.Indented);
            File.WriteAllText("Verification/HeroPainted2D/bake.json",json+"\n");
            if(clipped>0 || empty>0)throw new InvalidOperationException("Inspect bake.json: clipped="+clipped+", empty="+empty);
            return json;
        }
        finally
        {
            RenderTexture.active=previous;camera.targetTexture=null;high.Release();small.Release();
            foreach(var obj in new UnityEngine.Object[]{read,mesh,material,source,high,small})UnityEngine.Object.DestroyImmediate(obj);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    static Vector3 Deform(Vector3 p,string clip,float t,bool flip,bool profile)
    {
        float wave=Mathf.Sin(t*Mathf.PI*2),step=clip=="run"?1:0;
        float attack=0;
        if(clip=="attack" || clip=="sweep")
        {
            float impact=clip=="attack"?4f/7:5f/9;
            attack=t<.25f?-.35f*t/.25f:t<impact?Mathf.Lerp(-.35f,1,(t-.25f)/(impact-.25f)):Mathf.Lerp(1,0,(t-impact)/(1-impact));
        }
        Vector3 q=p;float weight=0;Vector2 joint=Vector2.zero,offset=Vector2.zero;float angle=0;
        if(p.y>.59f)
        {
            weight=Mathf.SmoothStep(0,1,(p.y-.59f)/.17f);joint=new Vector2(0,.61f);
            angle=clip=="idle"?Mathf.Sin(t*Mathf.PI*2)*.7f:-wave*.55f*step;
        }
        else if(p.y<.37f && p.x>-.28f && p.x<.235f)
        {
            float side=p.x<0?1:-1;
            weight=Mathf.Clamp01((.41f-p.y)/.17f);joint=new Vector2(side>0?-.14f:.13f,.28f);
            offset=new Vector2(wave*side*(profile?.035f:.018f),Mathf.Max(0,wave*side)*.055f)*step;
            angle=wave*side*8*step;
            if(clip=="dash"){offset=new Vector2(side*.045f,side>0?.025f:0);angle=side*12;}
        }
        else if(p.x>.14f && p.y<.56f)
        {
            weight=Mathf.SmoothStep(0,1,(p.x-.14f)/.14f);joint=new Vector2(.21f,.43f);
            angle=-wave*5*step+attack*(clip=="sweep"?98:76);
        }
        else if(p.x<-.13f && p.y<.56f)
        {
            weight=Mathf.SmoothStep(0,1,(-p.x-.13f)/.14f);joint=new Vector2(-.22f,.43f);
            angle=wave*6*step-attack*12;
        }
        q=Vector3.Lerp(p,Rotate(p,joint,angle)+(Vector3)offset,weight);
        float torsoAngle=-attack*7,breath=clip=="idle"?Mathf.Sin(t*Mathf.PI*2)*.006f:0;
        float bob=(1-Mathf.Cos(t*Mathf.PI*4))*.006f*step;
        if(clip=="dash")torsoAngle=profile?-10:-4;
        if(clip=="hurt")torsoAngle=Mathf.Sin(t*Mathf.PI)*9;
        if(clip=="down")torsoAngle=Mathf.Min(1,t*1.5f)*56;
        q=Rotate(q,new Vector2(0,.32f),torsoAngle)+Vector3.up*(breath+bob);
        if(flip)q.x=-q.x;
        return q;
    }

    static Vector3 Rotate(Vector3 p,Vector2 anchor,float degrees)
    {
        float a=degrees*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a),x=p.x-anchor.x,y=p.y-anchor.y;
        return new Vector3(anchor.x+x*c-y*s,anchor.y+x*s+y*c,0);
    }
}
