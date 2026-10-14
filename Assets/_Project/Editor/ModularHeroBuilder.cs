using System;
using System.IO;
using System.Linq;
using LinhThu.Prototype;
using LinhThu.Prototype.Characters;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LinhThu.Editor
{
    /// <summary>Authors the separate modular art trial with Unity APIs; existing art sources remain intact.</summary>
    public static class ModularHeroBuilder
    {
        public const string Scene = "Assets/_Project/Content/Scenes/HeroModularTrial.unity";
        public const string DefinitionPath = "Assets/_Project/Content/Characters/ModularHeroFront.asset";
        const string Art = "Assets/_Project/Art/HeroModular2D/";
        sealed class Cut
        {
            public string name;
            public int x, y, width, height;
            public Vector2 pivot;
            public Cut(string name,int x,int y,int width,int height,float px=.5f,float py=.5f)
            { this.name=name;this.x=x;this.y=y;this.width=width;this.height=height;pivot=new Vector2(px,py); }
        }

        [MenuItem("Demo/Art/Create Modular Hero Trial")]
        public static void CreateMenu() => Create();

        public static string Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring the art trial.");
            for (int i=0; i<UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the current scene before creating the modular trial.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var oldParts=Import("parts-front.png",new[] {
                new Cut("Face",20,99,289,248),new Cut("Eyes",337,231,250,98),
                new Cut("Sword",21,956,297,273,.12f,.87f),new Cut("Rear0",363,960,201,272),new Cut("Rear1",681,964,183,183),
                new Cut("Waist",974,1016,246,155),
                new Cut("TrouserL",684,693,182,145,.5f,.98f),new Cut("TrouserR",1009,693,179,145,.5f,.98f),
                new Cut("BootL",684,825,182,114,.5f,.82f),new Cut("BootR",1009,825,179,114,.5f,.82f)
            });
            var body=Import("body-basic-front.png",new[] {
                new Cut("Torso",117,57,306,305),new Cut("ArmL",532,69,245,307,.70f,.88f),new Cut("ArmR",933,69,243,307,.30f,.88f),
                new Cut("Hips",112,464,320,181),new Cut("LegL",546,396,212,299,.70f,.97f),new Cut("LegR",930,397,215,298,.30f,.97f),
                new Cut("BasicShirt",120,709,302,260),new Cut("BasicSleeveL",562,744,203,199,.74f,.86f),new Cut("BasicSleeveR",944,746,201,199,.26f,.86f),
                new Cut("BasicWaist",92,1019,361,71),new Cut("BasicShortL",554,983,222,208,.65f,.90f),new Cut("BasicShortR",943,983,221,210,.35f,.90f)
            });
            var hair=Import("hair-front.png",new[] { new Cut("Hair0",144,47,767,717),new Cut("Hair1",1174,48,721,723) });
            var clothes=Import("clothes-front.png",new[] {
                new Cut("Coat0",52,60,479,438),new Cut("SleeveL0",591,103,300,362,.80f,.94f),new Cut("SleeveR0",1160,103,298,362,.20f,.94f),
                new Cut("Coat1",49,528,481,443),new Cut("SleeveL1",591,576,310,369,.80f,.94f),new Cut("SleeveR1",1147,576,314,369,.20f,.94f)
            });
            var weapons=Import("weapons-front.png",new[] {
                new Cut("Weapon1_AzureGreatsword",24,24,381,344,.151f,.842f),
                new Cut("Weapon2_CrimsonFlame",536,24,357,324,.140f,.859f),
                new Cut("Weapon3_VioletThunder",24,536,368,336,.142f,.857f),
                new Cut("Weapon4_JadeEmerald",536,536,345,315,.139f,.858f)
            });
            var clothesExtra=Import("clothes-extra-front.png",new[] {
                new Cut("Coat2",52,60,479,438),new Cut("SleeveL2",591,103,300,362,.80f,.94f),new Cut("SleeveR2",1160,103,298,362,.20f,.94f),
                new Cut("Coat3",52,528,479,438),new Cut("SleeveL3",591,571,300,362,.80f,.94f),new Cut("SleeveR3",1160,571,298,362,.20f,.94f)
            });
            Sprite Get(Sprite[] sprites,string name) => sprites.Single(s=>s.name==name);
            ModularHeroPart Part(string id,HeroPartGroup group,Sprite first,Sprite second,Vector2 anchor,float height,int bone,int tip,int order,float blend=0,Sprite[] extra=null) =>
                new ModularHeroPart { id=id,group=group,first=first,second=second,extraVariants=extra,anchor=anchor,height=height,bone=bone,tipBone=tip,drawOrder=order,tipBlend=blend };
            var def=AssetDatabase.LoadAssetAtPath<ModularHeroDefinition>(DefinitionPath);
            if (def==null) { def=ScriptableObject.CreateInstance<ModularHeroDefinition>();AssetDatabase.CreateAsset(def,DefinitionPath); }
            def.bodyMaterial=Material("ModularHeroSprite","LinhThu/Modular Hero Sprite");
            def.shadowMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/Art/DaylightShadow.mat");
            def.parts=new[] {
                Part("BaseTorso",HeroPartGroup.Body,Get(body,"Torso"),null,new Vector2(0,.465f),.365f,0,0,25),
                Part("BaseHips",HeroPartGroup.Body,Get(body,"Hips"),null,new Vector2(0,.295f),.13f,0,0,22),
                Part("LeftArm",HeroPartGroup.Body,Get(body,"ArmL"),null,new Vector2(-.17f,.57f),.275f,4,11,23,.72f),
                Part("RightArm",HeroPartGroup.Body,Get(body,"ArmR"),null,new Vector2(.17f,.57f),.275f,5,12,23,.72f),
                Part("LeftLeg",HeroPartGroup.Body,Get(body,"LegL"),null,new Vector2(-.10f,.25f),.23f,2,9,10,.80f),
                Part("RightLeg",HeroPartGroup.Body,Get(body,"LegR"),null,new Vector2(.10f,.25f),.23f,3,10,10,.80f),
                Part("Face",HeroPartGroup.Body,Get(oldParts,"Face"),null,new Vector2(0,.83f),.43f,1,1,50),
                Part("Eyes",HeroPartGroup.Body,Get(oldParts,"Eyes"),null,new Vector2(0,.78f),.135f,13,13,55),
                Part("RearHair",HeroPartGroup.Hair,Get(oldParts,"Rear0"),Get(oldParts,"Rear1"),new Vector2(.10f,1.04f),.24f,1,7,1,.45f),
                Part("FrontHair",HeroPartGroup.Hair,Get(hair,"Hair0"),Get(hair,"Hair1"),new Vector2(0,.99f),.53f,1,7,60,.18f),
                Part("BasicShirt",HeroPartGroup.BasicClothes,Get(body,"BasicShirt"),null,new Vector2(0,.49f),.33f,0,0,38),
                Part("BasicLeftSleeve",HeroPartGroup.BasicClothes,Get(body,"BasicSleeveL"),null,new Vector2(-.17f,.57f),.14f,4,11,35,.65f),
                Part("BasicRightSleeve",HeroPartGroup.BasicClothes,Get(body,"BasicSleeveR"),null,new Vector2(.17f,.57f),.14f,5,12,35,.65f),
                Part("BasicWaist",HeroPartGroup.BasicClothes,Get(body,"BasicWaist"),null,new Vector2(0,.324f),.065f,0,0,29),
                Part("BasicLeftShort",HeroPartGroup.BasicClothes,Get(body,"BasicShortL"),null,new Vector2(-.092f,.30f),.15f,2,9,28,.80f),
                Part("BasicRightShort",HeroPartGroup.BasicClothes,Get(body,"BasicShortR"),null,new Vector2(.092f,.30f),.15f,3,10,28,.80f),
                Part("Coat",HeroPartGroup.Coat,Get(clothes,"Coat0"),Get(clothes,"Coat1"),new Vector2(0,.435f),.43f,0,8,40,.22f,new[] { Get(clothesExtra,"Coat2"),Get(clothesExtra,"Coat3") }),
                Part("LeftSleeve",HeroPartGroup.Coat,Get(clothes,"SleeveL0"),Get(clothes,"SleeveL1"),new Vector2(-.17f,.57f),.23f,4,11,35,.65f,new[] { Get(clothesExtra,"SleeveL2"),Get(clothesExtra,"SleeveL3") }),
                Part("RightSleeve",HeroPartGroup.Coat,Get(clothes,"SleeveR0"),Get(clothes,"SleeveR1"),new Vector2(.17f,.57f),.23f,5,12,35,.65f,new[] { Get(clothesExtra,"SleeveR2"),Get(clothesExtra,"SleeveR3") }),
                Part("TrouserWaist",HeroPartGroup.Trousers,Get(oldParts,"Waist"),null,new Vector2(0,.285f),.18f,0,0,26),
                Part("LeftTrouser",HeroPartGroup.Trousers,Get(oldParts,"TrouserL"),null,new Vector2(-.10f,.25f),.145f,2,9,15,.80f),
                Part("RightTrouser",HeroPartGroup.Trousers,Get(oldParts,"TrouserR"),null,new Vector2(.10f,.25f),.145f,3,10,15,.80f),
                Part("LeftBoot",HeroPartGroup.Boots,Get(oldParts,"BootL"),null,new Vector2(-.10f,.10f),.125f,9,9,18),
                Part("RightBoot",HeroPartGroup.Boots,Get(oldParts,"BootR"),null,new Vector2(.10f,.10f),.125f,10,10,18),
                Part("Sword",HeroPartGroup.Weapon,Get(oldParts,"Sword"),Get(weapons,"Weapon1_AzureGreatsword"),new Vector2(.23f,.34f),.30f,6,6,45,0,new[] { Get(weapons,"Weapon2_CrimsonFlame"),Get(weapons,"Weapon3_VioletThunder"),Get(weapons,"Weapon4_JadeEmerald") })
            };
            def.Validate(); EditorUtility.SetDirty(def); AssetDatabase.SaveAssets();
            try
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Modular trial camera").AddComponent<Camera>();
                camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=2.25f;
                camera.transform.position=new Vector3(.6f,.7f,-10);camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.42f,.60f,.28f);camera.allowHDR=false;camera.allowMSAA=false;
                var mapAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Content/Prefabs/Environment/MeadowMainMap.prefab");
                if (mapAsset==null) throw new InvalidOperationException("Existing meadow map is missing.");
                var map=(GameObject)PrefabUtility.InstantiatePrefab(mapAsset);
                var light=map.GetComponent<DaylightPresentation>() ?? map.AddComponent<DaylightPresentation>();
                light.advanceTime=false;light.startHour=11.5f;
                var hero=new GameObject("Modular hero").AddComponent<ModularHeroRig>();hero.definition=def;
                var trial=new GameObject("Modular Trial").AddComponent<ModularHeroTrial>();trial.hero=hero;trial.view=camera;
                CreateUi(trial);
                var events=new GameObject("Preview input",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                if (!EditorSceneManager.SaveScene(scene,Scene)) throw new IOException("Could not save the modular art trial.");
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (setup.Length>0 && setup.All(s=>!string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            return Scene;
        }

        static Sprite[] Import(string file,Cut[] cuts)
        {
            string path=Art+file;
            if (!File.Exists(path)) throw new FileNotFoundException("Missing modular art source",path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=256;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;
            importer.isReadable=false;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects();
            var rects=cuts.Select(c=> {
                if (c.x<0 || c.y<0 || c.x+c.width>width || c.y+c.height>height) throw new InvalidOperationException("Sprite cut outside source: "+c.name);
                return new SpriteRect { name=c.name,rect=new Rect(c.x,height-c.y-c.height,c.width,c.height),alignment=SpriteAlignment.Custom,pivot=c.pivot,
                    spriteID=old.FirstOrDefault(s=>s.name==c.name)?.spriteID ?? GUID.Generate() };
            }).ToArray();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        static Material Material(string name,string shaderName)
        {
            string path=Art+name+".mat";var shader=Shader.Find(shaderName);
            if (shader==null) throw new InvalidOperationException("Missing shader "+shaderName);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material==null) { material=new Material(shader);AssetDatabase.CreateAsset(material,path); }
            material.shader=shader;EditorUtility.SetDirty(material);return material;
        }

        static void CreateUi(ModularHeroTrial trial)
        {
            var font=Resources.Load<TMP_FontAsset>("Fonts/NotoSans-TMP");
            if (font==null) throw new InvalidOperationException("Preview requires the existing Vietnamese font.");
            var canvas=new GameObject("Character preview controls",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var panel=new GameObject("Appearance",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-20,-24);rect.sizeDelta=new Vector2(310,660);
            panel.GetComponent<Image>().color=new Color(.07f,.14f,.14f,.90f);
            Label(panel.transform,font,"NHÂN VẬT · MẪU NHÌN TRƯỚC",new Vector2(18,-14),16,274);
            trial.status=Label(panel.transform,font,"Đang đứng",new Vector2(18,-42),14,274);
            Button(panel.transform,font,"Đứng",new Vector2(18,-72),86,trial.Stand);
            Button(panel.transform,font,"Chạy",new Vector2(112,-72),86,trial.Run);
            Label(panel.transform,font,"Tóc",new Vector2(18,-114),14,274);
            Button(panel.transform,font,"Tóc 1",new Vector2(18,-140),133,trial.HairOne);
            Button(panel.transform,font,"Tóc 2",new Vector2(159,-140),133,trial.HairTwo);
            Label(panel.transform,font,"Áo",new Vector2(18,-182),14,274);
            Button(panel.transform,font,"Trắng ngà",new Vector2(18,-208),133,trial.IvoryCoat);
            Button(panel.transform,font,"Xanh đen",new Vector2(159,-208),133,trial.NavyCoat);
            Button(panel.transform,font,"Lục ngọc",new Vector2(18,-248),133,trial.JadeCoat);
            Button(panel.transform,font,"Xích kim",new Vector2(159,-248),133,trial.CrimsonCoat);
            Label(panel.transform,font,"Vũ khí (Phím 1-5)",new Vector2(18,-290),14,274);
            Button(panel.transform,font,"Đoản kiếm",new Vector2(18,-316),133,trial.WeaponZero);
            Button(panel.transform,font,"Lam đại kiếm",new Vector2(159,-316),133,trial.WeaponOne);
            Button(panel.transform,font,"Xích viêm",new Vector2(18,-356),133,trial.WeaponTwo);
            Button(panel.transform,font,"Tử lôi",new Vector2(159,-356),133,trial.WeaponThree);
            Button(panel.transform,font,"Bích ngọc",new Vector2(18,-396),274,trial.WeaponFour);
            Button(panel.transform,font,"Đổi màu mắt",new Vector2(18,-444),274,trial.ToggleEyes);
            trial.bodyButtonLabel=Button(panel.transform,font,"Trang phục cơ bản",new Vector2(18,-484),274,trial.ToggleBody);
            Button(panel.transform,font,"Đổi mức zoom",new Vector2(18,-524),274,trial.ToggleZoom);
            Label(canvas.transform,font,"WASD / mũi tên: di chuyển     1-5: đổi vũ khí",new Vector2(22,-626),16,760);
        }

        static TMP_Text Label(Transform parent,TMP_FontAsset font,string value,Vector2 point,float size,float width)
        {
            var node=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI));node.transform.SetParent(parent,false);
            var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=point;rect.sizeDelta=new Vector2(width,28);
            var label=node.GetComponent<TextMeshProUGUI>();label.font=font;label.text=value;label.fontSize=size;label.color=new Color(.95f,.98f,.96f);label.raycastTarget=false;
            return label;
        }

        static TMP_Text Button(Transform parent,TMP_FontAsset font,string value,Vector2 point,float width,UnityEngine.Events.UnityAction action)
        {
            var node=new GameObject(value,typeof(RectTransform),typeof(Image),typeof(UnityEngine.UI.Button));node.transform.SetParent(parent,false);
            var rect=(RectTransform)node.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=point;rect.sizeDelta=new Vector2(width,34);
            node.GetComponent<Image>().color=new Color(.18f,.39f,.35f,1);
            var button=node.GetComponent<UnityEngine.UI.Button>();UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,action);
            var label=Label(node.transform,font,value,Vector2.zero,14,width);
            var labelRect=(RectTransform)label.transform;labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;labelRect.offsetMin=Vector2.zero;labelRect.offsetMax=Vector2.zero;label.alignment=TextAlignmentOptions.Center;
            return label;
        }

        public static string Open() { EditorSceneManager.OpenScene(Scene);return Scene; }

        public static string Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building the modular trial.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.decompressionFallback=false;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL,new[] { GraphicsDeviceType.OpenGLES3 });
            const string output="WebBuildHeroModular";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[] {Scene},target=BuildTarget.WebGL,locationPathName=Path.GetFullPath(output),options=BuildOptions.None });
            var summary=report.summary;
            string json=JsonUtility.ToJson(new BuildEvidence { success=summary.result==BuildResult.Succeeded,seconds=summary.totalTime.TotalSeconds,bytes=summary.totalSize,errors=summary.totalErrors,warnings=summary.totalWarnings },true);
            Directory.CreateDirectory("Verification/HeroModular2D");File.WriteAllText("Verification/HeroModular2D/build.json",json+"\n");
            if (summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Modular hero WebGL build failed.");
            var files=Directory.GetFiles(output+"/Build");string html=File.ReadAllText("Web/hero-modular.html"),version=Guid.NewGuid().ToString("N");
            foreach (var pair in new[] {new[] {"__LOADER__",".loader.js"},new[] {"__DATA__",".data"},new[] {"__FRAMEWORK__",".framework.js"},new[] {"__WASM__",".wasm"}})
                html=html.Replace(pair[0],Path.GetFileName(files.Single(f=>f.EndsWith(pair[1],StringComparison.Ordinal)))+"?v="+version);
            File.WriteAllText(output+"/index.html",html);return json;
        }
        [Serializable] sealed class BuildEvidence { public bool success;public double seconds;public ulong bytes;public int errors,warnings; }
    }
}
