using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Characters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.PSD;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace LinhThu.Editor
{
    /// <summary>Authors the separate Krita/PSD character using persisted Unity sprite data and keyframed clips.</summary>
    public static class HeroProductionBuilder
    {
        public const string Scene = "Assets/_Project/Content/Scenes/HeroProductionTrial.unity";
        public const string Folder = "Assets/_Project/Content/Characters/HeroProduction2D/";
        public const string Psd = "Assets/_Project/Art/HeroProduction2D/hero-three-quarter.psd";
        public const string Prefab = Folder + "HeroThreeQuarter.prefab";
        const string Evidence = "Verification/HeroProduction2D/";
        const float Ppu = 640;
        static readonly string[] Names = { "Root", "Pelvis", "Chest", "Head", "Hair", "Eyes", "Arm_Back", "Elbow_Back", "Hand_Back", "Arm_Front", "Elbow_Front", "Hand_Front", "Leg_L", "Knee_L", "Foot_L", "Leg_R", "Knee_R", "Foot_R", "Coat_L", "Coat_R" };
        static readonly int[] Parents = { -1, 0, 1, 2, 3, 3, 2, 6, 7, 2, 9, 10, 1, 12, 13, 1, 15, 16, 1, 1 };
        static Vector2[] positions;
        static int Index(string name) => Array.IndexOf(Names, name);

        [MenuItem("Demo/Mỹ thuật/Nhân vật nhiều lớp/Tạo rig và bản thử")]
        public static void CreateMenu() => Prepare();

        public static string Prepare()
        {
            if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play and save scene changes before authoring.");
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Evidence);
            AssetDatabase.Refresh();
            var source = JObject.Parse(File.ReadAllText("ArtDirection/HeroProduction2D/joints.json"));
            positions = Names.Select(n => new Vector2(source["joints"][n][0].Value<float>(), 1024 - source["joints"][n][1].Value<float>())).ToArray();
            var importer = ImportSource();
            var provider = (ISpriteEditorDataProvider)importer;
            provider.InitSpriteEditorDataProvider();
            var characterProvider = provider.GetDataProvider<ICharacterDataProvider>();
            var character = characterProvider.GetCharacterData();
            var rects = provider.GetSpriteRects();
            if (rects.Length != 26 || character.parts.Length != 26)
                throw new InvalidOperationException($"Expected 26 production layers; importer returned {rects.Length} rectangles and {character.parts.Length} character parts.");
            var globalBones = CreateBones(Vector2.zero);
            character.bones = globalBones.ToArray();
            character.pivot = positions[0] / 1024;
            for (int i = 0; i < character.parts.Length; i++)
            {
                var part = character.parts[i]; part.bones = Enumerable.Range(0, Names.Length).ToArray();
                character.parts[i] = part;
            }
            characterProvider.SetCharacterData(character);
            var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
            var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
            foreach (var rect in rects)
            {
                var part = character.parts.Single(p => p.spriteId == rect.spriteID.ToString());
                boneProvider.SetBones(rect.spriteID, CreateBones(part.spritePosition.position));
                AuthorMesh(rect, part.spritePosition, meshProvider);
            }
            provider.Apply(); importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Psd).OfType<Sprite>().ToDictionary(s => s.name);
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>(); library.name = "Hero outfits";
            foreach (var sprite in sprites.Values)
            {
                string[] tokens = sprite.name.Split(new[] { "__" }, StringSplitOptions.None);
                library.AddCategoryLabel(sprite, tokens.Length == 2 ? tokens[1] : sprite.name, tokens.Length == 2 ? tokens[0] : "Base");
            }
            ReplaceAsset(library, Folder + "HeroOutfits.asset");
            library = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(Folder + "HeroOutfits.asset");
            var root = CreateRig(character, rects, sprites, library);
            try
            {
                var animator = root.GetComponent<AuthoredHeroView>().animator;
                var idle = AuthorIdle(animator.transform);
                var run = AuthorRun(animator.transform);
                ReplaceAsset(idle, Folder + "Idle.anim"); ReplaceAsset(run, Folder + "Run.anim");
                animator.runtimeAnimatorController = AuthorController();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            AuthorScene();
            var result = new { source = Psd, prefab = Prefab, scene = Scene, layers = rects.Length,
                bones = Names.Length, sourceWidth = 1024, sourceHeight = 1024, pixelsPerUnit = Ppu,
                method = "Krita shared-canvas raster layers; persisted SpriteSkin mesh/weights; native AnimationClip keyframes", outfits = new[] { "Ivory", "Navy" } };
            File.WriteAllText(Evidence + "authoring.json", JsonConvert.SerializeObject(result, Formatting.Indented) + "\n");
            return JsonConvert.SerializeObject(result);
        }

        static PSDImporter ImportSource()
        {
            if (!File.Exists(Psd)) throw new FileNotFoundException("Export the Krita layered source first.", Psd);
            AssetDatabase.SetImporterOverride<PSDImporter>(Psd);
            var importer = (PSDImporter)AssetImporter.GetAtPath(Psd);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.useMosaicMode = true; importer.useCharacterMode = true; importer.spritePixelsPerUnit = Ppu;
            importer.mipmapEnabled = true; importer.filterMode = FilterMode.Trilinear;
            var serialized = new SerializedObject(importer);
            serialized.FindProperty("m_ImportHiddenLayers").boolValue = true;
            serialized.FindProperty("m_LayerMappingOption").enumValueIndex = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var platform = importer.GetImporterPlatformSettings(BuildTarget.WebGL);
            platform.overridden = true;
            platform.maxTextureSize = 4096; platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetImporterPlatformSettings(platform);
            importer.SaveAndReimport(); return (PSDImporter)AssetImporter.GetAtPath(Psd);
        }

        static List<SpriteBone> CreateBones(Vector2 spriteOrigin)
        {
            var bones = new List<SpriteBone>();
            for (int i = 0; i < Names.Length; i++)
            {
                Vector2 position = Parents[i] < 0 ? positions[i] - spriteOrigin : positions[i] - positions[Parents[i]];
                bones.Add(new SpriteBone { name = Names[i], parentId = Parents[i], position = position, rotation = Quaternion.identity,
                    length = Mathf.Max(8, Enumerable.Range(0, Names.Length).Where(j => Parents[j] == i).Select(j => Vector2.Distance(positions[j], positions[i])).DefaultIfEmpty(12).First()) });
            }
            return bones;
        }

        static string Slot(string name) => name.Contains("__") ? name.Substring(name.IndexOf("__", StringComparison.Ordinal) + 2) : name;

        static void AuthorMesh(SpriteRect rect, RectInt placement, ISpriteMeshDataProvider provider)
        {
            // Rows include the shared anatomical joint heights. Every clothing layer
            // samples the same character coordinates, independent of its crop bounds.
            var xs = Enumerable.Range(0, 9).Select(i => placement.x + placement.width * i / 8f).ToArray();
            var ys = Enumerable.Range(0, 11).Select(i => placement.y + placement.height * i / 10f)
                .Concat(positions.Select(p => p.y).SelectMany(y => new[] { y - 14, y, y + 14 }).Where(y => y > placement.y && y < placement.yMax))
                .Distinct().OrderBy(y => y).ToArray();
            var vertices = new List<Vertex2DMetaData>();
            for (int y = 0; y < ys.Length; y++) for (int x = 0; x < xs.Length; x++)
            {
                var point = new Vector2(xs[x], ys[y]);
                vertices.Add(new Vertex2DMetaData { position = point - placement.position, boneWeight = Weights(Slot(rect.name), point) });
            }
            var triangles = new List<int>(); var edges = new HashSet<Vector2Int>();
            for (int y = 0; y < ys.Length - 1; y++) for (int x = 0; x < xs.Length - 1; x++)
            {
                int a = y * xs.Length + x, b = a + 1, c = a + xs.Length, d = c + 1;
                triangles.AddRange(new[] { a, c, b, b, c, d });
                foreach (var e in new[] { new Vector2Int(a,b), new Vector2Int(a,c), new Vector2Int(b,c), new Vector2Int(b,d), new Vector2Int(c,d) }) edges.Add(e);
            }
            provider.SetVertices(rect.spriteID, vertices.ToArray()); provider.SetIndices(rect.spriteID, triangles.ToArray()); provider.SetEdges(rect.spriteID, edges.ToArray());
        }

        static BoneWeight Weights(string slot, Vector2 point)
        {
            float y = 1024 - point.y;
            if (slot == "Head") return Pair(3, 3, 0);
            if (slot == "Eyes_Mouth") return Pair(5, 5, 0);
            if (slot == "Hair_Front") return Pair(3, 4, 1 - Mathf.SmoothStep(0,1,Mathf.InverseLerp(280,380,y)));
            if (slot == "Body_Torso") return Pair(2, 1, Mathf.SmoothStep(0,1,Mathf.InverseLerp(1024-positions[2].y+30,1024-positions[18].y,y)));
            if (slot == "Coat_L" || slot == "Coat_R")
            {
                int flap = slot == "Coat_L" ? 18 : 19;
                float waist = 1024 - positions[flap].y;
                return Pair(1, flap, Mathf.SmoothStep(0,1,Mathf.InverseLerp(waist,waist+35,y)));
            }
            if (slot.Contains("Front") || slot.Contains("Back"))
            {
                bool front = slot.Contains("Front"); int upper = front ? 9 : 6, lower = front ? 10 : 7;
                float elbow = 1024 - positions[lower].y;
                return Pair(upper, lower, Mathf.SmoothStep(0,1,Mathf.InverseLerp(elbow-14,elbow+14,y)));
            }
            if (slot.StartsWith("Leg_") || slot.StartsWith("Foot_"))
            {
                bool left = slot.EndsWith("L", StringComparison.Ordinal); int upper = left ? 12 : 15, lower = left ? 13 : 16, foot = left ? 14 : 17;
                float knee = 1024 - positions[lower].y, ankle = 1024 - positions[foot].y;
                if (y < ankle - 14) return Pair(upper, lower, Mathf.SmoothStep(0,1,Mathf.InverseLerp(knee-14,knee+14,y)));
                return Pair(lower, foot, Mathf.SmoothStep(0,1,Mathf.InverseLerp(ankle-14,ankle+10,y)));
            }
            throw new InvalidOperationException("No authored weight region for slot " + slot);
        }

        static BoneWeight Pair(int first, int second, float blend) => new BoneWeight { boneIndex0 = first, boneIndex1 = second, weight0 = 1-blend, weight1 = blend };

        static GameObject CreateRig(CharacterData character, SpriteRect[] rects, Dictionary<string,Sprite> sprites, SpriteLibraryAsset library)
        {
            var root = new GameObject("Ngự thú sư · nguồn đã author");
            var view = root.AddComponent<AuthoredHeroView>();
            var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform,false); view.visual = visual.transform;
            var shadowRoot = new GameObject("Shadows"); shadowRoot.transform.SetParent(root.transform,false); view.shadowRoot = shadowRoot.transform;
            view.animator = visual.AddComponent<Animator>(); view.animator.applyRootMotion = false; view.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            view.sorting = visual.AddComponent<SortingGroup>(); view.sorting.sortingOrder = 100;
            visual.AddComponent<SpriteLibrary>().spriteLibraryAsset = library;
            var holder = new GameObject("Bones"); holder.transform.SetParent(visual.transform,false);
            var bones = new Transform[Names.Length];
            for (int i=0;i<bones.Length;i++)
            {
                bones[i] = new GameObject(Names[i]).transform;
                bones[i].SetParent(Parents[i] < 0 ? holder.transform : bones[Parents[i]], false);
                bones[i].localPosition = (positions[i] - (Parents[i] < 0 ? positions[0] : positions[Parents[i]])) / Ppu;
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HeroModular2D/ModularHeroSprite.mat");
            var shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightShadow.mat");
            if (material == null || shadowMaterial == null) throw new InvalidOperationException("Missing existing daylight hero materials.");
            var garments = new List<SpriteResolver>(); var garmentShadows = new List<SpriteRenderer>(); var shadows = new List<SpriteRenderer>();
            foreach (var rect in rects.Where(r => !r.name.StartsWith("Navy__",StringComparison.Ordinal)))
            {
                var part = character.parts.Single(p => p.spriteId == rect.spriteID.ToString());
                var node = new GameObject(rect.name); node.transform.SetParent(visual.transform,false);
                var sprite = sprites[rect.name]; node.transform.localPosition = ((Vector2)part.spritePosition.position + Vector2.Scale(rect.rect.size,rect.pivot) - positions[0]) / Ppu;
                var renderer = node.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
                renderer.sortingOrder = PartOrder(rect.name);
                AddSkin(node,bones);
                string slot = Slot(rect.name);
                if (slot == "Eyes_Mouth") continue;
                var shadowNode = new GameObject(rect.name); shadowNode.transform.SetParent(shadowRoot.transform,false);
                shadowNode.transform.localPosition = node.transform.localPosition;
                var shadow = shadowNode.AddComponent<SpriteRenderer>(); shadow.sprite = sprite; shadow.sharedMaterial = shadowMaterial;
                shadow.sortingOrder = -29; shadow.localBounds = new Bounds(Vector3.zero, Vector3.one * 8); AddSkin(shadowNode,bones); shadows.Add(shadow);
                if (rect.name.Contains("__"))
                {
                    var resolver = node.AddComponent<SpriteResolver>(); resolver.SetCategoryAndLabel(slot,"Ivory");
                    garments.Add(resolver); garmentShadows.Add(shadow);
                }
            }
            view.garmentResolvers = garments.ToArray(); view.garmentShadows = garmentShadows.ToArray(); view.allShadows = shadows.ToArray();
            return root;
        }

        static int PartOrder(string name)
        {
            string slot=Slot(name);
            int order=slot=="Arm_Back" || slot=="Sleeve_Back" ? 10 : slot.StartsWith("Leg") ? 15 : slot.StartsWith("Foot") ? 17
                : slot=="Body_Torso" ? 30 : slot.StartsWith("Coat") ? 35 : slot=="Arm_Front" || slot=="Sleeve_Front" ? 40 : slot=="Head" ? 50 : slot=="Eyes_Mouth" ? 51 : 52;
            return order + (name.Contains("__") ? 1 : 0);
        }

        static void AddSkin(GameObject node, Transform[] bones)
        {
            var skin=node.AddComponent<SpriteSkin>();skin.autoRebind=false;skin.alwaysUpdate=true;
            skin.SetRootBone(bones[0]);skin.SetBoneTransforms(bones);
        }

        static void ReplaceAsset(UnityEngine.Object value, string path)
        {
            var old=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if(old==null) AssetDatabase.CreateAsset(value,path);
            else { EditorUtility.CopySerialized(value,old); EditorUtility.SetDirty(old); UnityEngine.Object.DestroyImmediate(value); }
        }

        static string BonePath(int bone) => Parents[bone] < 0 ? "Bones/"+Names[bone] : BonePath(Parents[bone])+"/"+Names[bone];

        static void Curve(AnimationClip clip,string path,string property,float duration,float[] values, float offset=0)
        {
            var keys=values.Select((v,i)=>new Keyframe(duration*i/(values.Length-1),v+offset)).ToArray();
            var curve=new AnimationCurve(keys);
            for(int i=0;i<keys.Length;i++) { AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto); }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),curve);
        }

        static AnimationClip Loop(string name)
        {
            var clip=new AnimationClip {name=name,frameRate=60};
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=false;
            AnimationUtility.SetAnimationClipSettings(clip,settings); return clip;
        }

        static AnimationClip AuthorIdle(Transform visual)
        {
            var clip=Loop("Idle"); const float duration=2;
            float pelvis=(positions[1].y-positions[0].y)/Ppu;
            Curve(clip,BonePath(1),"m_LocalPosition.y",duration,new[] {0f,.0025f,0f,.0025f,0f},pelvis);
            Curve(clip,BonePath(2),"localEulerAnglesRaw.z",duration,new[] {0f,.6f,0f,-.4f,0f});
            Curve(clip,BonePath(3),"localEulerAnglesRaw.z",duration,new[] {0f,-.5f,0f,.4f,0f});
            Curve(clip,BonePath(4),"localEulerAnglesRaw.z",duration,new[] {0f,1.2f,.2f,-.8f,0f});
            foreach(int arm in new[]{6,9}) Curve(clip,BonePath(arm),"localEulerAnglesRaw.z",duration,new[] {0f,.6f,0f,-.6f,0f});
            foreach(int flap in new[]{18,19}) Curve(clip,BonePath(flap),"localEulerAnglesRaw.z",duration,new[] {0f,.8f,0f,-.7f,0f});
            return clip;
        }

        static AnimationClip AuthorRun(Transform visual)
        {
            var clip=Loop("Run"); const float duration=.5f;
            float[] bob={0,-4,7,10,0,-4,7,10,0};
            Curve(clip,BonePath(1),"m_LocalPosition.y",duration,bob.Select(v=>v/Ppu).ToArray(),(positions[1].y-positions[0].y)/Ppu);
            Curve(clip,BonePath(2),"localEulerAnglesRaw.z",duration,new[] {-1f,-2f,-1f,0f,-1f,0f,1f,0f,-1f});
            Curve(clip,BonePath(3),"localEulerAnglesRaw.z",duration,new[] {1f,2f,1f,0f,1f,0f,-1f,0f,1f});
            Curve(clip,BonePath(4),"localEulerAnglesRaw.z",duration,new[] {0f,2f,3f,1f,0f,-2f,-3f,-1f,0f});
            Curve(clip,BonePath(9),"localEulerAnglesRaw.z",duration,new[] {-17f,-23f,-14f,2f,19f,24f,14f,-3f,-17f});
            Curve(clip,BonePath(10),"localEulerAnglesRaw.z",duration,new[] {-8f,-13f,-19f,-14f,-5f,0f,2f,-2f,-8f});
            Curve(clip,BonePath(6),"localEulerAnglesRaw.z",duration,new[] {18f,23f,14f,-2f,-19f,-24f,-14f,3f,18f});
            Curve(clip,BonePath(7),"localEulerAnglesRaw.z",duration,new[] {3f,0f,-3f,-8f,-14f,-19f,-13f,-8f,3f});
            Curve(clip,BonePath(18),"localEulerAnglesRaw.z",duration,new[] {-1f,2f,5f,3f,0f,-3f,-5f,-2f,-1f});
            Curve(clip,BonePath(19),"localEulerAnglesRaw.z",duration,new[] {1f,-2f,-4f,-3f,0f,3f,4f,2f,1f});
            // Foot targets define the contact/lift poses; offline two-bone solving
            // writes ordinary editable rotation keys. Runtime performs no IK or pose formula.
            float[] stride={58,22,-18,-58,-60,-18,24,62,58}, lift={0,0,0,0,22,55,62,30,0};
            for(int side=0;side<2;side++)
            {
                int hip=side==0?12:15,knee=hip+1,foot=hip+2;
                var hipKeys=new float[9];var kneeKeys=new float[9];var footKeys=new float[9];
                Vector2 first=positions[knee]-positions[hip],second=positions[foot]-positions[knee];
                float len1=first.magnitude,len2=second.magnitude,rest1=Mathf.Atan2(first.y,first.x),rest2=Mathf.Atan2(second.y,second.x);
                for(int i=0;i<9;i++)
                {
                    int phase=side==0?i:i==8?4:(i+4)%8;
                    var target=positions[foot]+new Vector2(stride[phase],lift[phase]);
                    var delta=target-(positions[hip]+Vector2.up*bob[i]);
                    float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(len1-len2)+.001f,len1+len2-.001f);
                    float direction=Mathf.Atan2(delta.y,delta.x),bend=side==0?-1:1;
                    float firstAngle=direction+bend*Mathf.Acos(Mathf.Clamp((len1*len1+distance*distance-len2*len2)/(2*len1*distance),-1,1));
                    Vector2 kneePosition=positions[hip]+Vector2.up*bob[i]+new Vector2(Mathf.Cos(firstAngle),Mathf.Sin(firstAngle))*len1;
                    float secondAngle=Mathf.Atan2(target.y-kneePosition.y,target.x-kneePosition.x);
                    hipKeys[i]=Mathf.DeltaAngle(rest1*Mathf.Rad2Deg,firstAngle*Mathf.Rad2Deg);
                    kneeKeys[i]=Mathf.DeltaAngle((rest2-rest1)*Mathf.Rad2Deg,(secondAngle-firstAngle)*Mathf.Rad2Deg);
                    footKeys[i]=-hipKeys[i]-kneeKeys[i];
                }
                Curve(clip,BonePath(hip),"localEulerAnglesRaw.z",duration,hipKeys);Curve(clip,BonePath(knee),"localEulerAnglesRaw.z",duration,kneeKeys);Curve(clip,BonePath(foot),"localEulerAnglesRaw.z",duration,footKeys);
            }
            return clip;
        }

        static AnimatorController AuthorController()
        {
            string path=Folder+"Hero.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            var idle=machine.AddState("Idle");idle.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Idle.anim");
            var run=machine.AddState("Run");run.motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Run.anim");machine.defaultState=idle;
            var toRun=idle.AddTransition(run);toRun.hasExitTime=false;toRun.duration=.08f;toRun.hasFixedDuration=true;toRun.AddCondition(AnimatorConditionMode.If,0,"Moving");
            var toIdle=run.AddTransition(idle);toIdle.hasExitTime=false;toIdle.duration=.08f;toIdle.hasFixedDuration=true;toIdle.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");
            EditorUtility.SetDirty(controller);return controller;
        }

        static void AuthorScene()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var map=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArtPreviewBuilder.Map))).GetComponent<ModularMapLayout>();
                var camera=new GameObject("Camera xem nguồn đã author").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5.875f;
                camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.35f,.49f,.22f);camera.allowHDR=false;
                var hero=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab))).GetComponent<AuthoredHeroView>();hero.transform.position=new Vector3(-4,-2,0);
                var preview=new GameObject("Duyệt nhân vật nhiều lớp").AddComponent<AuthoredHeroPreview>();preview.hero=hero;preview.map=map;preview.view=camera;
                var daylight=DaylightPresentation.Ensure(preview.gameObject);daylight.advanceTime=false;daylight.startHour=11.5f;
                AuthorUi(preview);
                var events=new GameObject("Input giao diện",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
            }
            finally { if(setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        static void AuthorUi(AuthoredHeroPreview preview)
        {
            var font=Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            var canvas=new GameObject("Điều khiển duyệt",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            var panel=new GameObject("Mẫu 3/4",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-20,-82);rect.sizeDelta=new Vector2(290,245);panel.GetComponent<Image>().color=new Color(.05f,.10f,.13f,.92f);
            Label(panel.transform,font,"NHÂN VẬT · MẪU 3/4",18,14,17);
            preview.status=Label(panel.transform,font,"Đang đứng · Trắng ngà",18,42,13);
            Button(panel.transform,font,"Đứng",18,74,preview.Stand);Button(panel.transform,font,"Xem chạy",152,74,preview.Run);
            Button(panel.transform,font,"Trắng ngà · 1",18,118,preview.Ivory);Button(panel.transform,font,"Xanh đen · 2",152,118,preview.Navy);
            Button(panel.transform,font,"Gần / xa · Z",18,162,preview.Zoom);Button(panel.transform,font,"Dừng hình",152,162,preview.Pause);
            Label(panel.transform,font,"WASD để đi · R về điểm đầu",18,211,12);
        }

        static TMP_Text Label(Transform parent,TMP_FontAsset font,string text,float x,float y,float size)
        {
            var node=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));node.transform.SetParent(parent,false);
            var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(258,26);
            var label=node.GetComponent<TextMeshProUGUI>();label.font=font;label.fontSize=size;label.text=text;label.color=new Color(.92f,.96f,.95f);label.raycastTarget=false;return label;
        }

        static void Button(Transform parent,TMP_FontAsset font,string text,float x,float y,UnityAction action)
        {
            var node=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));node.transform.SetParent(parent,false);
            var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(120,34);
            node.GetComponent<Image>().color=new Color(.19f,.32f,.35f);node.GetComponent<Button>().targetGraphic=node.GetComponent<Image>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(node.GetComponent<Button>().onClick,action);
            var label=Label(node.transform,font,text,0,0,13);label.rectTransform.sizeDelta=new Vector2(120,34);label.alignment=TextAlignmentOptions.Center;
        }

        [MenuItem("Demo/Mỹ thuật/Nhân vật nhiều lớp/Mở bản thử")]
        public static void Open() => EditorSceneManager.OpenScene(Scene);

        [MenuItem("Demo/Mỹ thuật/Nhân vật nhiều lớp/Build Web mẫu")]
        public static void BuildMenu() => Build();

        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before building.");
            const string output="WebBuildHeroProduction";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},target=BuildTarget.WebGL,locationPathName=Path.GetFullPath(output),options=BuildOptions.None});
            var summary=report.summary;
            File.WriteAllText(Evidence+"build.json",JsonConvert.SerializeObject(new {result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,
                messages=report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content).ToArray()},Formatting.Indented)+"\n");
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Authored hero WebGL build failed.");
            var files=Directory.GetFiles(output+"/Build");string html=File.ReadAllText("Web/art-preview.html");
            foreach(var pair in new[]{new[]{"__LOADER__",".loader.js"},new[]{"__DATA__",".data"},new[]{"__FRAMEWORK__",".framework.js"},new[]{"__WASM__",".wasm"}})
                html=html.Replace(pair[0],Path.GetFileName(files.Single(f=>f.EndsWith(pair[1],StringComparison.Ordinal)))+"?v="+Guid.NewGuid().ToString("N"));
            File.WriteAllText(output+"/index.html",html);return "Built separate authored hero preview";
        }
    }
}
