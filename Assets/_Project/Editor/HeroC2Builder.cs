using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NguThu.Client.Preview;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NguThu.Editor
{
    /// <summary>Builds only the C2 art trial from Krita's full-canvas part exports.</summary>
    public static class HeroC2Builder
    {
        const string Source = "ArtDirection/CharacterProduction/experiments/C2/";
        const string Art = "Assets/_Project/Art/HeroC2/";
        const string Content = "Assets/_Project/Content/Characters/HeroC2/";
        const float Ppu = 256;
        static readonly Dictionary<string, Vector2> Bind = new Dictionary<string, Vector2>();
        static readonly Dictionary<string, string> Parents = new Dictionary<string, string>();

        [MenuItem("Demo/Character C2/Open separate trial")]
        public static void OpenTrial()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before changing scenes.");
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Content + "HeroC2Trial.unity");
        }

        [MenuItem("Demo/Character C2/Check appearance swaps in Play mode")]
        public static void CheckPlayMode()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Open the C2 trial and enter Play mode first.");
            var view = UnityEngine.Object.FindFirstObjectByType<HeroC2Preview>();
            if (view == null) throw new InvalidOperationException("C2 trial is not open.");
            view.SetRunning(true);
            view.animator.Play("Run", 0, .17f); view.animator.Update(0);
            var checks = new JArray();
            var rootPosition = view.transform.position;
            foreach (var outfit in new[]{"classic","jade","basic"})
            foreach (var hair in new[]{"silver","ink"})
            foreach (var eyes in new[]{"jade","amber"})
            {
                view.animator.Update(.08f);
                var before = view.animator.GetCurrentAnimatorStateInfo(0);
                var matrices = view.visual.GetComponentsInChildren<Transform>(true).Select(x=>x.localToWorldMatrix).ToArray();
                view.SetAppearance(outfit, hair, eyes);
                var after = view.animator.GetCurrentAnimatorStateInfo(0);
                if (before.fullPathHash != after.fullPathHash || before.normalizedTime != after.normalizedTime)
                    throw new Exception("Appearance restarted animation.");
                if (!matrices.SequenceEqual(view.visual.GetComponentsInChildren<Transform>(true).Select(x=>x.localToWorldMatrix)))
                    throw new Exception("Appearance moved a joint.");
                view.animator.Update(.02f);
                if (view.animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= after.normalizedTime)
                    throw new Exception("Run stopped advancing after appearance swap.");
                if (view.transform.position != rootPosition) throw new Exception("Root moved during appearance swap.");
                checks.Add(new JObject{["outfit"]=outfit,["hair"]=hair,["eyes"]=eyes,["phase_before"]=before.normalizedTime,["phase_after"]=after.normalizedTime,["continued_running"]=true});
            }
            view.SetAppearance("classic","silver","jade");
            File.WriteAllText(Source+"unity-play-mode-checks.json",new JObject{["play_mode"]=true,["method"]="Unity Animator.Update with controlled deltas during Play mode",["swaps"]=checks}.ToString()+"\n");
            Debug.Log("C2 Play mode: all 12 appearance combinations preserve pose and animation phase; Run continues.");
        }

        [MenuItem("Demo/Character C2/Build and verify separate trial")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before building the C2 art trial.");
            var openTrial = SceneManager.GetSceneByPath(Content + "HeroC2Trial.unity");
            bool reopenTrial = openTrial.IsValid() && openTrial.isLoaded;
            if (reopenTrial)
            {
                if (SceneManager.sceneCount != 1)
                    throw new InvalidOperationException("Open the C2 trial on its own before rebuilding; other open scenes must be preserved.");
                if (openTrial.isDirty) throw new InvalidOperationException("Save the C2 trial before rebuilding it.");
            }
            var activeScene = SceneManager.GetActiveScene();
            bool useSingleScene = reopenTrial || (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(activeScene.path)
                && !activeScene.isDirty && activeScene.GetRootGameObjects().Length == 0);
            Directory.CreateDirectory(Art); Directory.CreateDirectory(Content);
            var data = JObject.Parse(File.ReadAllText(Source + "manifest.json"));
            Bind.Clear(); Parents.Clear();
            foreach (var row in (JArray)data["bones"])
            {
                string n = (string)row["name"];
                Bind[n] = new Vector2((float)row["position"][0], (float)row["position"][1]);
                Parents[n] = (string)row["parent"];
            }
            foreach (var layer in (JArray)data["layers"])
            {
                string name = (string)layer["name"];
                File.Copy(Source + "export/" + name + ".png", Art + name + ".png", true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var layer in (JArray)data["layers"])
            {
                string name = (string)layer["name"];
                var importer = (TextureImporter)AssetImporter.GetAtPath(Art + name + ".png");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Ppu;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                var pivot = Bind[(string)layer["bone"]];
                settings.spritePivot = new Vector2(pivot.x / 1024f, 1 - pivot.y / 1024f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = true; importer.filterMode = FilterMode.Trilinear;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, useSingleScene ? NewSceneMode.Single : NewSceneMode.Additive);
            var root = new GameObject("Hero C2 · art trial"); SceneManager.MoveGameObjectToScene(root, scene);
            var view = root.AddComponent<HeroC2Preview>();
            var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false); view.visual = visual;
            var bones = new Dictionary<string, Transform>();
            foreach (var row in (JArray)data["bones"])
            {
                string n = (string)row["name"], parent = Parents[n];
                var bone = new GameObject(n).transform;
                bone.SetParent(parent == null ? visual : bones[parent], false);
                Vector2 delta = parent == null ? Vector2.zero : Bind[n] - Bind[parent];
                bone.localPosition = new Vector3(delta.x / Ppu, -delta.y / Ppu, 0);
                bones[n] = bone;
            }
            var parts = new List<HeroC2Preview.Part>();
            foreach (var layer in (JArray)data["layers"])
            {
                string name = (string)layer["name"], bone = (string)layer["bone"];
                var go = new GameObject(name); go.transform.SetParent(bones[bone], false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
                sr.sortingOrder = (int)layer["order"];
                parts.Add(new HeroC2Preview.Part { renderer=sr, category=(string)layer["category"], variant=(string)layer["variant"], hiddenBy=((JArray)layer["hidden_by"]).Select(x=>(string)x).ToArray() });
            }
            view.parts = parts.ToArray(); view.animator = visual.gameObject.AddComponent<Animator>();
            foreach (var item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 31;
            var idle = AuthorClip("Idle", bones, visual, false);
            var run = AuthorClip("Run", bones, visual, true);
            SaveAsset(idle, Content + "Idle.anim"); SaveAsset(run, Content + "Run.anim");
            var controller = new AnimatorController();
            SaveAsset(controller, Content + "HeroC2.controller");
            controller.AddLayer("Base"); controller.AddParameter("Running", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle"); idleState.motion = idle; machine.defaultState = idleState;
            var runState = machine.AddState("Run"); runState.motion = run;
            var toRun=idleState.AddTransition(runState);toRun.hasExitTime=false;toRun.duration=.12f;toRun.AddCondition(AnimatorConditionMode.If,0,"Running");
            var toIdle=runState.AddTransition(idleState);toIdle.hasExitTime=false;toIdle.duration=.12f;toIdle.AddCondition(AnimatorConditionMode.IfNot,0,"Running");
            view.animator.runtimeAnimatorController=controller;view.ApplyAppearance();
            PrefabUtility.SaveAsPrefabAsset(root, Content + "HeroC2.prefab");
            var cameraObject=new GameObject("C2 Camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.06f;
            camera.cullingMask=1<<31;
            camera.transform.position=new Vector3(0,1.6f,-10);camera.backgroundColor=new Color(.88f,.91f,.90f);camera.clearFlags=CameraClearFlags.SolidColor;
            // Dedicated scene; preserve the original scene and build settings.
            EditorSceneManager.SaveScene(scene,Content+"HeroC2Trial.unity");
            AssetDatabase.SaveAssets();
            Verify(root,view,visual,run,idle,camera);
            if (useSingleScene) EditorSceneManager.OpenScene(Content + "HeroC2Trial.unity");
            else EditorSceneManager.CloseScene(scene,true);
            File.WriteAllText(Source+"unity-build-complete.txt",DateTime.UtcNow.ToString("O")+"\n");
            Debug.Log("C2 trial built: 52 shared-canvas parts, Idle/Run clips, appearance and render checks.");
        }

        static void SaveAsset(UnityEngine.Object asset,string path)
        {
            var old=AssetDatabase.LoadMainAssetAtPath(path);
            if(old!=null)AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset,path);
        }

        static AnimationClip AuthorClip(string name,Dictionary<string,Transform> bones,Transform visual,bool run)
        {
            var clip=new AnimationClip {name=name,frameRate=30};
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            float duration=run?.8f:2.4f;
            var curves=new Dictionary<string,List<Keyframe>>();
            Action<string,string,float,float> add=(bone,prop,t,value)=>{string key=bone+"|"+prop;if(!curves.ContainsKey(key))curves[key]=new List<Keyframe>();curves[key].Add(new Keyframe(t,value));};
            for(int i=0;i<=32;i++)
            {
                float t=duration*i/32f,phase=i/32f,a=phase*Mathf.PI*2;
                float bob=run?12+3*Mathf.Cos(a*2):-2+2*Mathf.Cos(a);
                var local=bones["pelvis"].localPosition;
                add("pelvis","m_LocalPosition.x",t,local.x+(run?6:0)/Ppu);
                add("pelvis","m_LocalPosition.y",t,local.y-bob/Ppu);
                add("chest","localEulerAnglesRaw.z",t,run?-3.2f+1.5f*Mathf.Sin(a*2):.7f*Mathf.Sin(a));
                add("head","localEulerAnglesRaw.z",t,run?2.1f-1.1f*Mathf.Sin(a*2):-.5f*Mathf.Sin(a));
                add("hair","localEulerAnglesRaw.z",t,run?2.4f*Mathf.Sin(a-.6f):.8f*Mathf.Sin(a-.6f));
                foreach(string side in new[]{"front","back"})
                {
                    float p=(phase+(side=="back"?.5f:0))%1,sa=Mathf.Sin(p*Mathf.PI*2);
                    float lift=run?Mathf.Sin(Mathf.Clamp01((p-.55f)/.45f)*Mathf.PI)*28:0;
                    float travel=run?(p<.55f?Mathf.Lerp(22,-22,p/.55f):Mathf.Lerp(-22,22,(p-.55f)/.45f)):0;
                    string thigh="thigh_"+side,shin="shin_"+side,foot="foot_"+side;
                    var hip=Bind[thigh]+new Vector2(run?6:0,bob);
                    var ankle=Bind[foot]+new Vector2(travel,-lift);
                    float upperAngle,lowerAngle;SolveLeg(thigh,shin,foot,hip,ankle,out upperAngle,out lowerAngle);
                    add(thigh,"localEulerAnglesRaw.z",t,upperAngle);
                    add(shin,"localEulerAnglesRaw.z",t,lowerAngle);
                    float footTilt=run&&p>.55f?-12*Mathf.Sin((p-.55f)/.45f*Mathf.PI):0;
                    add(foot,"localEulerAnglesRaw.z",t,-upperAngle-lowerAngle+footTilt);
                    add("upper_"+side,"localEulerAnglesRaw.z",t,run?sa*16:Mathf.Sin(a)*.6f);
                    add("fore_"+side,"localEulerAnglesRaw.z",t,run?-7-Mathf.Max(0,sa)*14:0);
                    add("hand_"+side,"localEulerAnglesRaw.z",t,run?sa*-3:0);
                    add("coat_"+side,"localEulerAnglesRaw.z",t,run?sa*-6:Mathf.Sin(a)*.6f);
                }
            }
            foreach(var row in curves)
            {
                var key=row.Key.Split('|');var curve=new AnimationCurve(row.Value.ToArray());
                for(int k=0;k<curve.length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bones[key[0]],visual),typeof(Transform),key[1]),curve);
            }
            return clip;
        }

        static void SolveLeg(string upper,string lower,string foot,Vector2 hip,Vector2 target,out float top,out float bottom)
        {
            Vector2 u=Bind[lower]-Bind[upper],v=Bind[foot]-Bind[lower],d=target-hip;
            float l=u.magnitude,m=v.magnitude,dist=Mathf.Clamp(d.magnitude,Mathf.Abs(l-m)+.2f,l+m-.2f);
            float theta=Mathf.Atan2(d.y,d.x);
            float offset=Mathf.Acos(Mathf.Clamp((l*l+dist*dist-m*m)/(2*l*dist),-1,1));
            float sign=Mathf.Sign(u.x*v.y-u.y*v.x);
            float first=theta-sign*offset;
            Vector2 knee=hip+new Vector2(Mathf.Cos(first),Mathf.Sin(first))*l;
            float second=Mathf.Atan2(target.y-knee.y,target.x-knee.x);
            top=-Mathf.DeltaAngle(Mathf.Atan2(u.y,u.x)*Mathf.Rad2Deg,first*Mathf.Rad2Deg);
            bottom=-Mathf.DeltaAngle(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg,second*Mathf.Rad2Deg)-top;
        }

        static void Verify(GameObject root,HeroC2Preview view,Transform visual,AnimationClip run,AnimationClip idle,Camera camera)
        {
            Directory.CreateDirectory(Source+"unity-frames");
            var rt=new RenderTexture(768,896,24);camera.targetTexture=rt;
            var report=new JObject();report["parts"]=view.parts.Length;report["bones"]=Bind.Count;
            report["method"]="Unity rigid cutout skeleton; native editable AnimationClips; no SpriteSkin deformation";
            var poses=new JArray();
            foreach(var outfit in new[]{"classic","jade","basic"})foreach(var hair in new[]{"silver","ink"})
            {
                view.SetAppearance(outfit,hair,"jade");
                for(int i=0;i<24;i++)
                {
                    run.SampleAnimation(visual.gameObject,run.length*i/24f);
                    var before=visual.GetComponentsInChildren<Transform>(true).Select(x=>x.localToWorldMatrix).ToArray();
                    view.SetAppearance(outfit,hair,"amber");view.SetAppearance(outfit,hair,"jade");
                    var after=visual.GetComponentsInChildren<Transform>(true).Select(x=>x.localToWorldMatrix).ToArray();
                    if(!before.SequenceEqual(after))throw new Exception("Appearance modified bone transforms");
                    if(root.transform.position!=Vector3.zero)throw new Exception("Root drift");
                    Capture(camera,rt,Source+"unity-frames/"+outfit+"-"+hair+"-"+i.ToString("D2")+".png");
                    poses.Add(new JObject{["outfit"]=outfit,["hair"]=hair,["phase"]=i/24f,["swap_preserves_pose"]=true});
                }
            }
            view.SetAppearance("classic","silver","jade");idle.SampleAnimation(visual.gameObject,0);
            Capture(camera,rt,Source+"unity-idle.png");
            visual.localScale=new Vector3(-1,1,1);run.SampleAnimation(visual.gameObject,run.length*.25f);
            Capture(camera,rt,Source+"unity-flipped.png");
            report["pose_samples"]=poses;report["clips"]=new JArray(idle.name,run.name);
            report["shared_source_size"]=1024;report["main_player_replaced"]=false;
            File.WriteAllText(Source+"unity-checks.json",report.ToString()+"\n");
            camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        }

        static void Capture(Camera camera,RenderTexture target,string file)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
        }
    }
}
