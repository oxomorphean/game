using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace LinhThu.Prototype.Characters
{
    /// <summary>Body and garments share bones and poses; the caller owns movement, combat time and appearance.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom("LinhThu.Prototype.Combat")]
    public sealed class ModularHeroRig : MonoBehaviour
    {
        public const int BoneCount = 14;
        public ModularHeroDefinition definition;
        public int HairStyle { get; private set; }
        public int CoatStyle { get; private set; }
        public int WeaponStyle { get; private set; }
        public int FacingSign { get; private set; } = 1;
        /// <summary>Whether the trial's outer outfit is selected; otherwise the separate basic outfit is shown.</summary>
        public bool WearingClothes { get; private set; }
        public HeroMotion Motion { get; private set; }
        public float PoseTime { get; private set; }
        public bool Initialized { get; private set; }
        public Transform Bone(int index) => bones[index];
        public SpriteRenderer Part(string id) => pieces.Find(p => p.spec.id == id)?.body;
        public SpriteSkin Skin(string id) => pieces.Find(p => p.spec.id == id)?.skin;

        static readonly string[] Names = { "Body", "Head", "LeftLeg", "RightLeg", "LeftArm", "RightArm", "SwordGrip", "HairTip", "CoatTail", "LeftKnee", "RightKnee", "LeftElbow", "RightElbow", "Eyes" };
        static readonly int[] Parents = { -1, 0, 0, 0, 0, 0, 12, 1, 0, 2, 3, 4, 5, 1 };
        static readonly Vector2[] Anchors = {
            new Vector2(0,.45f), new Vector2(0,.83f), new Vector2(-.10f,.25f), new Vector2(.10f,.25f),
            new Vector2(-.17f,.57f), new Vector2(.17f,.57f), new Vector2(.23f,.34f), new Vector2(0,1.12f),
            new Vector2(0,.32f), new Vector2(-.10f,.13f), new Vector2(.10f,.13f),
            new Vector2(-.195f,.46f), new Vector2(.195f,.46f), new Vector2(0,.83f)
        };
        static readonly Color[] SlashColors = {
            new Color(.88f, 1.00f, .96f, .92f),
            new Color(.22f, .86f, 1.00f, .95f),
            new Color(1.00f, .42f, .18f, .95f),
            new Color(.72f, .38f, 1.00f, .95f),
            new Color(.24f, .94f, .68f, .95f)
        };
        sealed class Piece
        {
            public ModularHeroPart spec;
            public SpriteRenderer body, shadow;
            public SpriteResolver resolver;
            public SpriteSkin skin;
        }
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<Sprite> runtimeSprites = new List<Sprite>();
        Transform[] bones;
        Vector3[] rest;
        SpriteLibraryAsset libraryAsset;
        SortingGroup group;
        MaterialPropertyBlock block;
        Color eyeTint = new Color(.25f,.85f,.70f,1);
        SpriteRenderer slashArcRenderer;
        Texture2D slashArcTexture;

        public void Initialize(ModularHeroDefinition source)
        {
            if (Initialized) throw new InvalidOperationException("Modular hero is already initialized.");
            source.Validate(); definition = source;
            block = new MaterialPropertyBlock();
            var bodyRoot = new GameObject("Body and clothing").transform;
            bodyRoot.SetParent(transform, false);
            group = bodyRoot.gameObject.AddComponent<SortingGroup>();
            bones = new Transform[BoneCount]; rest = new Vector3[BoneCount];
            for (int i = 0; i < BoneCount; i++) bones[i] = new GameObject(Names[i]).transform;
            for (int i = 0; i < BoneCount; i++)
            {
                bones[i].SetParent(Parents[i] < 0 ? transform : bones[Parents[i]], false);
                rest[i] = Parents[i] < 0 ? Anchors[i] : Anchors[i] - Anchors[Parents[i]];
                bones[i].localPosition = rest[i];
            }
            libraryAsset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            libraryAsset.name = "Modular hero instance library";
            var library = bodyRoot.gameObject.AddComponent<SpriteLibrary>();
            foreach (var spec in source.parts)
            {
                var node = new GameObject(spec.id).transform;
                node.SetParent(bodyRoot, false); node.localPosition = spec.anchor;
                node.localScale = Vector3.one * (spec.height * 256 / spec.first.rect.height);
                int variantCount = Mathf.Max(2, spec.VariantCount);
                Sprite first = null;
                for (int v = 0; v < variantCount; v++)
                {
                    Sprite rigged = RigSprite(spec.GetVariant(v), spec, node);
                    libraryAsset.AddCategoryLabel(rigged, spec.id, v.ToString());
                    if (v == 0) first = rigged;
                }
                var renderer = node.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = first; renderer.sharedMaterial = source.bodyMaterial; renderer.sortingOrder = spec.drawOrder;
                var skin = AddSkin(renderer);
                var shadowNode = new GameObject(spec.id + " shadow").transform;
                shadowNode.SetParent(transform, false); shadowNode.localPosition = node.localPosition; shadowNode.localScale = node.localScale;
                var shadow = shadowNode.gameObject.AddComponent<SpriteRenderer>();
                shadow.sprite = first; shadow.sharedMaterial = source.shadowMaterial; shadow.sortingOrder = -29;
                AddSkin(shadow);
                pieces.Add(new Piece { spec = spec, body = renderer, shadow = shadow, skin = skin });
            }
            library.spriteLibraryAsset = libraryAsset;
            foreach (var piece in pieces) piece.resolver = piece.body.gameObject.AddComponent<SpriteResolver>();
            BuildSlashArc2D(bodyRoot, source.bodyMaterial);
            Initialized = true;
            SetAppearance(0,0,false); SetWeapon(0); SamplePose(HeroMotion.Idle,0);
        }

        void BuildSlashArc2D(Transform bodyRoot, Material material)
        {
            const int size = 128;
            slashArcTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Modular 2D Slash Arc",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * .32f) / (size * .58f);
                float dy = (y - size * .48f) / (size * .46f);
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (dx > -.1f && r > .52f && r < .96f && Mathf.Abs(angle) < 76f)
                {
                    float arcFade = 1f - Mathf.Pow(Mathf.Abs(angle) / 76f, 1.8f);
                    float radial = 1f - Mathf.Abs(r - .76f) / .22f;
                    float a = Mathf.Clamp01(arcFade * radial * 1.35f);
                    bool core = r > .72f && r < .85f && Mathf.Abs(angle) < 52f;
                    byte val = (byte)(core ? 255 : 225);
                    pixels[y * size + x] = new Color32(val, val, 255, (byte)(a * 240));
                }
            }
            slashArcTexture.SetPixels32(pixels);
            slashArcTexture.Apply(false, false);
            var arcSprite = Sprite.Create(slashArcTexture, new Rect(0, 0, size, size), new Vector2(.32f, .48f), 100f);
            runtimeSprites.Add(arcSprite);
            var arcNode = new GameObject("SlashArc2D").transform;
            arcNode.SetParent(bodyRoot, false);
            arcNode.localPosition = new Vector3(.18f, .44f, 0);
            slashArcRenderer = arcNode.gameObject.AddComponent<SpriteRenderer>();
            slashArcRenderer.sprite = arcSprite;
            slashArcRenderer.sharedMaterial = material;
            slashArcRenderer.sortingOrder = 65;
            slashArcRenderer.enabled = false;
        }

        SpriteSkin AddSkin(SpriteRenderer renderer)
        {
            var skin = renderer.gameObject.AddComponent<SpriteSkin>();
            skin.autoRebind = false; skin.SetRootBone(bones[0]); skin.SetBoneTransforms(bones);
            skin.alwaysUpdate = true;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 8);
            return skin;
        }

        Sprite RigSprite(Sprite source, ModularHeroPart spec, Transform visual)
        {
            // Weapons preserve their authored blade scale relative to the grip pivot so greatswords and short swords fit the hand accurately.
            float ppu = spec.group == HeroPartGroup.Weapon ? (source == spec.first ? 256f : 185f) : 256 * source.rect.height / spec.first.rect.height;
            var sprite = Sprite.Create(source.texture, source.rect, source.pivot / source.rect.size, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = spec.id + " " + source.name;
            const int columns = 5, rows = 8;
            var vertices = new Vector2[(columns+1)*(rows+1)];
            var triangles = new ushort[columns*rows*6];
            var weights = new NativeArray<BoneWeight>(vertices.Length, Allocator.Temp);
            var bindPoses = new NativeArray<Matrix4x4>(BoneCount, Allocator.Temp);
            try
            {
                for (int y=0; y<=rows; y++) for (int x=0; x<=columns; x++)
                {
                    int index = y*(columns+1)+x;
                    float u=(float)x/columns, v=(float)y/rows;
                    vertices[index] = new Vector2(u*sprite.rect.width,v*sprite.rect.height);
                    bool hair = spec.group == HeroPartGroup.Hair;
                    float tip = spec.tipBlend * Mathf.SmoothStep(0,1,hair ? v : 1-v);
                    weights[index] = new BoneWeight { boneIndex0=spec.bone, weight0=1-tip, boneIndex1=spec.tipBone, weight1=tip };
                }
                int t=0;
                for (int y=0; y<rows; y++) for (int x=0; x<columns; x++)
                {
                    ushort a=(ushort)(y*(columns+1)+x), b=(ushort)(a+1), c=(ushort)(a+columns+1), d=(ushort)(c+1);
                    triangles[t++]=a; triangles[t++]=c; triangles[t++]=b;
                    triangles[t++]=b; triangles[t++]=c; triangles[t++]=d;
                }
                sprite.OverrideGeometry(vertices,triangles);
                var definitions = new SpriteBone[BoneCount];
                for (int i=0; i<BoneCount; i++)
                {
                    definitions[i] = new SpriteBone { name=Names[i], parentId=Parents[i], position=rest[i], rotation=Quaternion.identity, length=.1f };
                    bindPoses[i] = bones[i].worldToLocalMatrix * visual.localToWorldMatrix;
                }
                sprite.SetBones(definitions); sprite.SetBindPoses(bindPoses);
                sprite.SetVertexAttribute(VertexAttribute.BlendWeight,weights);
            }
            finally { weights.Dispose(); bindPoses.Dispose(); }
            runtimeSprites.Add(sprite); return sprite;
        }

        /// <summary>Changes visual parts without resetting animation; clothes selects the outer outfit over the basic outfit.</summary>
        public void SetAppearance(int hair, int coat, bool clothes)
        {
            if (hair < 0 || hair > 1 || coat < 0 || coat > 3) throw new ArgumentOutOfRangeException("Invalid modular appearance index.");
            HairStyle=hair; CoatStyle=coat; WearingClothes=clothes;
            ApplyPieces();
        }

        /// <summary>Swaps the held 2D weapon sprite on the SwordGrip bone without resetting animation or pose.</summary>
        public void SetWeapon(int weapon)
        {
            if (weapon < 0 || weapon > 4) throw new ArgumentOutOfRangeException(nameof(weapon));
            WeaponStyle = weapon;
            ApplyPieces();
        }

        /// <summary>Flips the 2D character horizontally while keeping world-space sun shadows aligned.</summary>
        public void SetFacing(int sign)
        {
            FacingSign = sign < 0 ? -1 : 1;
            transform.localScale = new Vector3(FacingSign, 1, 1);
            UpdateProperties();
        }

        void ApplyPieces()
        {
            foreach (var piece in pieces)
            {
                int maxVariant = Mathf.Max(1, piece.spec.VariantCount) - 1;
                int variant = piece.spec.group == HeroPartGroup.Hair ? Mathf.Clamp(HairStyle, 0, maxVariant)
                    : piece.spec.group == HeroPartGroup.Coat ? Mathf.Clamp(CoatStyle, 0, maxVariant)
                    : piece.spec.group == HeroPartGroup.Weapon ? Mathf.Clamp(WeaponStyle, 0, maxVariant)
                    : 0;
                if (!piece.resolver.SetCategoryAndLabel(piece.spec.id, variant.ToString()))
                    throw new InvalidOperationException("Missing modular variant: " + piece.spec.id);
                piece.shadow.sprite = piece.body.sprite;
                bool clothing = piece.spec.group == HeroPartGroup.Coat || piece.spec.group == HeroPartGroup.Trousers || piece.spec.group == HeroPartGroup.Boots;
                bool visible = piece.spec.group == HeroPartGroup.BasicClothes ? !WearingClothes : !clothing || WearingClothes;
                piece.body.enabled = piece.shadow.enabled = visible;
                // The body stays underneath either outfit; garment shoulders cover the limb joins.
                piece.shadow.enabled &= piece.spec.id != "Eyes";
            }
        }

        public void SetEyeColor(bool amber) { eyeTint = amber ? new Color(1,.66f,.16f,1) : new Color(.25f,.85f,.70f,1); UpdateProperties(); }

        /// <summary>Time and normalized strike phase are supplied by the caller; poses never move the actor root.</summary>
        public void SamplePose(HeroMotion motion, float time, float strikePhase=0)
        {
            if (!Initialized) return;
            Motion=motion; PoseTime=time;
            for (int i=0; i<BoneCount; i++) { bones[i].localPosition=rest[i]; bones[i].localRotation=Quaternion.identity; bones[i].localScale=Vector3.one; }
            float moving=motion==HeroMotion.Run ? 1 : 0, stride=Mathf.Sin(time*10.5f), breath=Mathf.Sin(time*2.8f);
            bones[0].localPosition += Vector3.up * (breath*.004f*(1-moving)+(1-Mathf.Cos(time*21))*.007f*moving);
            bones[0].localRotation=Quaternion.Euler(0,0,-2*moving);
            bones[1].localRotation=Quaternion.Euler(0,0,1.2f*moving+breath*.35f);
            for (int side=0; side<2; side++)
            {
                float step=stride*(side==0 ? 1 : -1), lift=Mathf.Max(0,step)*moving;
                bones[2+side].localPosition += new Vector3(step*.014f, lift*.027f,0)*moving;
                bones[2+side].localRotation=Quaternion.Euler(0,0,step*11*moving);
                bones[9+side].localRotation=Quaternion.Euler(0,0,-step*12*moving);
                bones[4+side].localRotation=Quaternion.Euler(0,0,-step*10*moving);
                bones[11+side].localRotation=Quaternion.Euler(0,0,step*6*moving);
            }
            bones[7].localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*(moving>0 ? 10.5f : 3)-.7f)*(moving>0 ? 3.0f : .8f));
            bones[8].localRotation=Quaternion.Euler(0,0,-stride*5*moving+breath*.7f);
            float blink=Mathf.Repeat(time,3.7f);
            bones[13].localScale=new Vector3(1,blink>3.52f ? Mathf.Max(.08f,Mathf.Abs(blink-3.61f)/.09f) : 1,1);
            if (motion==HeroMotion.Attack || motion==HeroMotion.Sweep)
            {
                float phase=Mathf.Clamp01(strikePhase);
                float angle=phase<.32f ? Mathf.Lerp(0,62,Mathf.SmoothStep(0,1,phase/.32f)) : phase<.48f ? Mathf.Lerp(62,-76,Mathf.SmoothStep(0,1,(phase-.32f)/.16f)) : Mathf.Lerp(-76,0,Mathf.SmoothStep(0,1,(phase-.48f)/.52f));
                bones[5].localRotation=Quaternion.Euler(0,0,angle);
                bones[12].localRotation=Quaternion.Euler(0,0,-angle*.25f);
                bones[6].localRotation=Quaternion.Euler(0,0,angle*.38f);
                bones[0].localRotation=Quaternion.Euler(0,0,-angle*.12f);
                bones[1].localRotation=Quaternion.Euler(0,0,angle*.07f);
                bones[4].localRotation=Quaternion.Euler(0,0,angle*.15f);
                bones[8].localRotation=Quaternion.Euler(0,0,angle*.07f);
                if (slashArcRenderer != null)
                {
                    bool showArc = phase >= .26f && phase <= .62f;
                    slashArcRenderer.enabled = showArc;
                    if (showArc)
                    {
                        float t = Mathf.Clamp01((phase - .26f) / .36f);
                        Color c = SlashColors[Mathf.Clamp(WeaponStyle, 0, SlashColors.Length - 1)];
                        c.a *= Mathf.Sin(t * Mathf.PI);
                        slashArcRenderer.color = c;
                        slashArcRenderer.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(28f, -42f, t));
                        float scale = WeaponStyle == 1 ? 1.18f : WeaponStyle == 0 ? .88f : 1.05f;
                        slashArcRenderer.transform.localScale = Vector3.one * scale;
                    }
                }
            }
            else if (slashArcRenderer != null)
            {
                slashArcRenderer.enabled = false;
            }
            group.sortingOrder=100-Mathf.RoundToInt(transform.position.y*10);
            UpdateProperties();
        }

        void UpdateProperties()
        {
            foreach (var piece in pieces)
            {
                if (piece.spec.id=="Eyes")
                {
                    block.Clear(); block.SetFloat("_DyeEyes",1); block.SetColor("_EyeTint",eyeTint); piece.body.SetPropertyBlock(block);
                }
                block.Clear(); definition.shadowSettings.Apply(block, transform.position, definition.shadowReferenceHeight * Mathf.Abs(transform.lossyScale.y));
                piece.shadow.SetPropertyBlock(block);
            }
        }

        void OnDestroy()
        {
            foreach (var sprite in runtimeSprites) if (sprite != null) Destroy(sprite);
            if (slashArcTexture != null) Destroy(slashArcTexture);
            if (libraryAsset != null) Destroy(libraryAsset);
        }
    }
}
