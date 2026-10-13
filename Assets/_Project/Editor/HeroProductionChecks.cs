using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Prototype.Characters;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace LinhThu.Editor
{
    /// <summary>Checks persisted rig data, reimport identity and actual deformed poses of the separate trial.</summary>
    public static class HeroProductionChecks
    {
        const string Evidence = "Verification/HeroProduction2D/";
        static IEnumerator running;
        static readonly List<string> passed = new List<string>();

        static void Check(string name, bool valid)
        {
            if (!valid) throw new InvalidOperationException(name);
            passed.Add(name);
        }

        static string Fingerprint()
        {
            var provider=(ISpriteEditorDataProvider)AssetImporter.GetAtPath(HeroProductionBuilder.Psd);
            provider.InitSpriteEditorDataProvider();
            var mesh=provider.GetDataProvider<ISpriteMeshDataProvider>();
            var bones=provider.GetDataProvider<ISpriteBoneDataProvider>();
            return JsonConvert.SerializeObject(provider.GetSpriteRects().OrderBy(r=>r.name).Select(r=>new {
                r.name, id=r.spriteID.ToString(), x=r.rect.x,y=r.rect.y,w=r.rect.width,h=r.rect.height,
                px=r.pivot.x,py=r.pivot.y,
                bones=bones.GetBones(r.spriteID).Select(b=>new {b.name,b.parentId,x=b.position.x,y=b.position.y,b.length}),
                vertices=mesh.GetVertices(r.spriteID).Select(v=>new {x=v.position.x,y=v.position.y,v.boneWeight}),
                triangles=mesh.GetIndices(r.spriteID)
            }));
        }

        public static string Assets()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before reimport checks.");
            passed.Clear();
            var provider=(ISpriteEditorDataProvider)AssetImporter.GetAtPath(HeroProductionBuilder.Psd);
            provider.InitSpriteEditorDataProvider();
            var character=provider.GetDataProvider<ICharacterDataProvider>().GetCharacterData();
            var rects=provider.GetSpriteRects();
            Check("26 named layers and 20 saved character bones",rects.Length==26&&character.bones.Length==20&&rects.Select(r=>r.name).Distinct().Count()==26);
            var bones=provider.GetDataProvider<ISpriteBoneDataProvider>();
            var mesh=provider.GetDataProvider<ISpriteMeshDataProvider>();
            foreach(var rect in rects)
            {
                Check("Saved bones and normalized weights: "+rect.name,bones.GetBones(rect.spriteID).Count==20&&
                    mesh.GetVertices(rect.spriteID).All(v=>Mathf.Abs(v.boneWeight.weight0+v.boneWeight.weight1+v.boneWeight.weight2+v.boneWeight.weight3-1)<.0001f));
                if(!rect.name.StartsWith("Ivory__",StringComparison.Ordinal)) continue;
                var other=rects.Single(r=>r.name==rect.name.Replace("Ivory__","Navy__"));
                var a=character.parts.Single(p=>p.spriteId==rect.spriteID.ToString());
                var b=character.parts.Single(p=>p.spriteId==other.spriteID.ToString());
                Check("Registered outfit pair: "+rect.name,a.spritePosition==b.spritePosition&&rect.pivot==other.pivot&&
                    JsonConvert.SerializeObject(mesh.GetVertices(rect.spriteID).Select(v=>v.boneWeight))==JsonConvert.SerializeObject(mesh.GetVertices(other.spriteID).Select(v=>v.boneWeight)));
            }
            foreach(string name in new[]{"Idle","Run"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroProductionBuilder.Folder+name+".anim");
                var bindings=AnimationUtility.GetCurveBindings(clip);
                Check("Looping keyed clip with fixed root: "+name,clip.isLooping&&bindings.Length>=8&&bindings.All(b=>b.path!="Bones/Root"));
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HeroProductionBuilder.Prefab);
            Check("Saved prefab references native rig and nine resolvers",prefab.GetComponent<AuthoredHeroView>().garmentResolvers.Length==9&&prefab.GetComponentsInChildren<SpriteSkin>().Length==33);
            string before=Fingerprint();
            AssetDatabase.ImportAsset(HeroProductionBuilder.Psd,ImportAssetOptions.ForceUpdate);
            Check("PSD reimport preserves sprite IDs, joints, pivots, mesh and weights",before==Fingerprint());
            var result=JsonConvert.SerializeObject(new {passed=passed.Count,checks=passed.ToArray()},Formatting.Indented);
            File.WriteAllText(Evidence+"asset-checks.json",result+"\n");return result;
        }

        public static string BeginRuntime()
        {
            if(!EditorApplication.isPlaying||running!=null)throw new InvalidOperationException("Start the trial in Play mode first.");
            running=Inspect();EditorApplication.update+=Tick;return "Capturing both outfits across eight running poses";
        }

        static void Tick()
        {
            bool finished=false;
            try { finished=!running.MoveNext(); }
            catch(Exception error)
            {
                finished=true;
                File.WriteAllText(Evidence+"runtime-checks.json",JsonConvert.SerializeObject(new {success=false,error=error.ToString()},Formatting.Indented)+"\n");
                Debug.LogException(error);
            }
            finally
            {
                if(finished)
                {
                    EditorApplication.update-=Tick;
                    var routine=running;running=null;
                    (routine as IDisposable)?.Dispose();
                }
            }
        }

        static IEnumerator Inspect()
        {
            passed.Clear();
            var preview=UnityEngine.Object.FindAnyObjectByType<AuthoredHeroPreview>();
            var hero=preview.hero;
            var animator=hero.animator;
            bool wasEnabled=preview.enabled,wasMoving=hero.Moving;
            float speed=animator.speed,zoom=preview.view.orthographicSize;
            Vector3 cameraPosition=preview.view.transform.position;
            string outfitBefore=hero.Outfit;int facingBefore=hero.FacingSign;
            var stateBefore=animator.GetCurrentAnimatorStateInfo(0);
            try
            {
                preview.enabled=false;animator.speed=0;hero.SetMoving(true);
                Vector3 root=hero.transform.position;
                preview.view.orthographicSize=2.15f;
                preview.view.transform.position=new Vector3(root.x+.5f,root.y+.5f,-10);
                var sampled=new List<object>();Vector3[] first=null;
                foreach(string outfit in new[]{"Ivory","Navy"})
                {
                    hero.SetOutfit(outfit);
                    for(int pose=0;pose<8;pose++)
                    {
                        hero.animator.Play("Run",0,pose/8f);hero.animator.Update(0);
                        yield return null;yield return null;yield return null;
                        var skins=hero.GetComponentsInChildren<SpriteSkin>();
                        Check("All skins deform: "+outfit+"/"+pose,skins.All(s=>s.HasCurrentDeformedVertices()));
                        var leg=hero.visual.Find("Ivory__Leg_L").GetComponent<SpriteSkin>();
                        var vertices=leg.GetDeformedVertexPositionData().ToArray();
                        if(first==null)first=vertices;
                        if(outfit=="Ivory"&&pose==4)Check("Run changes actual leg mesh",vertices.Zip(first,(a,b)=>(a-b).sqrMagnitude).Max()>.0001f);
                        Check("Animation preserves ground root: "+outfit+"/"+pose,(hero.transform.position-root).sqrMagnitude<.000001f);
                        Capture(preview.view,outfit.ToLowerInvariant()+"-run-"+pose);
                        sampled.Add(new {outfit,pose,phase=hero.animator.GetCurrentAnimatorStateInfo(0).normalizedTime});
                    }
                }
                var state=hero.animator.GetCurrentAnimatorStateInfo(0);
                hero.SetOutfit("Ivory");
                var swapped=hero.animator.GetCurrentAnimatorStateInfo(0);
                Check("Outfit swap preserves Animator state and phase",state.fullPathHash==swapped.fullPathHash&&state.normalizedTime==swapped.normalizedTime);
                hero.gameObject.SetActive(false);hero.gameObject.SetActive(true);hero.SetMoving(true);
                animator.speed=1;animator.Update(.2f);animator.Update(.2f);animator.speed=0;
                Check("Resumed movement restores Run after hero reactivation",animator.GetBool("Moving")&&animator.GetCurrentAnimatorStateInfo(0).IsName("Run"));
                hero.SetFacing(-1);yield return null;yield return null;Capture(preview.view,"mirrored-run");
                var sun=Shader.GetGlobalVector("_SunShadow");hero.SetFacing(1);
                Check("Mirroring keeps sunlight world direction",sun==Shader.GetGlobalVector("_SunShadow"));
                hero.SetMoving(false);hero.animator.Play("Idle",0,0);hero.animator.Update(0);yield return null;yield return null;
                Capture(preview.view,"idle-close");preview.view.orthographicSize=5.875f;Capture(preview.view,"idle-game-size");
                var block=new MaterialPropertyBlock();hero.allShadows[0].GetPropertyBlock(block);
                var origin=block.GetVector("_GroundOrigin");
                Check("Shadow origin follows actor foot",Vector2.Distance(new Vector2(origin.x,origin.y),root)<.0001f);
                var result=new {success=true,passed=passed.Count,checks=passed.ToArray(),samples=sampled.ToArray(),note="Camera captures exclude overlay UI; aesthetic review is separate."};
                File.WriteAllText(Evidence+"runtime-checks.json",JsonConvert.SerializeObject(result,Formatting.Indented)+"\n");
            }
            finally
            {
                if(animator!=null)animator.speed=speed;
                if(preview!=null)
                {
                    preview.enabled=wasEnabled;
                    if(preview.view!=null)
                    {
                        preview.view.orthographicSize=zoom;
                        preview.view.transform.position=cameraPosition;
                    }
                }
                if(hero!=null&&animator!=null)
                {
                    hero.SetMoving(wasMoving);hero.SetOutfit(outfitBefore);hero.SetFacing(facingBefore);
                    animator.Play(stateBefore.fullPathHash,0,stateBefore.normalizedTime);animator.Update(0);
                }
            }
        }

        static void Capture(Camera camera,string name)
        {
            var previous=camera.targetTexture;var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
            var texture=new Texture2D(1280,720,TextureFormat.RGBA32,false);
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                File.WriteAllBytes(Evidence+name+".png",texture.EncodeToPNG());
            }
            finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
