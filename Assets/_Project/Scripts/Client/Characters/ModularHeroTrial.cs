using System;
using System.Linq;
using LinhThu.Domain.Navigation;
using LinhThu.Prototype.Preview;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LinhThu.Prototype.Characters
{
    /// <summary>Art-preview controls only; no equipment rules, gameplay session or save access.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom("LinhThu.Prototype.Combat")]
    public sealed class ModularHeroTrial : MonoBehaviour
    {
        public ModularHeroRig hero;
        public Camera view;
        public TMP_Text status, bodyButtonLabel;
        public HeroMotion DemoMotion { get; private set; }
        public bool AmberEyes { get; private set; }
        public bool CloseView { get; private set; } = true;
        public MovementArena Arena => arena;
        MovementArena arena;
        InputAction move;
        float visualTime;
        HeroMotion shownMotion;
        Bounds meadowBounds;

        void Start()
        {
            if (hero==null || view==null || hero.definition==null) throw new InvalidOperationException("Modular preview requires the authored rig and camera.");
            if (!hero.Initialized) hero.Initialize(hero.definition);
            var meadow=FindAnyObjectByType<MeadowGroundSurface>();
            if (meadow==null) throw new InvalidOperationException("Modular preview requires its meadow floor.");
            meadowBounds=meadow.GetComponent<MeshRenderer>().bounds;
            var obstacles=FindObjectsByType<ObstacleAuthoring>().Select(o=>o.Definition()).ToArray();
            arena=new MovementArena(new WorldPoint(8.5f,4.6f),obstacles);
            move=new InputAction("Modular art move",InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            move.Enable(); SetDemo(HeroMotion.Idle);
            Application.runInBackground=true; Application.targetFrameRate=60;
        }

        void Update()
        {
            if (!hero.Initialized) return;
            float dt=Mathf.Min(Time.deltaTime,.05f); visualTime+=dt;
            var kb=Keyboard.current;
            if (kb!=null)
            {
                if (kb.digit1Key.wasPressedThisFrame) ChooseWeapon(0);
                else if (kb.digit2Key.wasPressedThisFrame) ChooseWeapon(1);
                else if (kb.digit3Key.wasPressedThisFrame) ChooseWeapon(2);
                else if (kb.digit4Key.wasPressedThisFrame) ChooseWeapon(3);
                else if (kb.digit5Key.wasPressedThisFrame) ChooseWeapon(4);
            }
            Vector2 axis=Vector2.ClampMagnitude(move.ReadValue<Vector2>(),1);
            Vector3 before=hero.transform.position;
            if (axis.sqrMagnitude>.01f)
            {
                if (axis.x<-.05f) hero.SetFacing(-1);
                else if (axis.x>.05f) hero.SetFacing(1);
                Vector2 delta=axis*2.8f*dt;
                var moved=arena!=null
                    ? arena.Move(new WorldPoint(hero.transform.position.x,hero.transform.position.y),new WorldPoint(delta.x,delta.y),.28f,true)
                    : new WorldPoint(Mathf.Clamp(hero.transform.position.x+delta.x,-8.5f,8.5f),Mathf.Clamp(hero.transform.position.y+delta.y,-4.6f,4.6f));
                hero.transform.position=new Vector3(moved.X,moved.Y,0);
            }
            HeroMotion motion=(hero.transform.position-before).sqrMagnitude>.000001f ? HeroMotion.Run : axis.sqrMagnitude>.01f ? HeroMotion.Idle : DemoMotion;
            hero.SamplePose(motion,visualTime);
            view.orthographicSize=Mathf.Min(CloseView ? 2.25f : 5.2f,meadowBounds.extents.y,meadowBounds.extents.x/view.aspect);
            float halfWidth=view.orthographicSize*view.aspect;
            view.transform.position=new Vector3(
                Mathf.Clamp(hero.transform.position.x+.6f,meadowBounds.min.x+halfWidth,meadowBounds.max.x-halfWidth),
                Mathf.Clamp(hero.transform.position.y+.7f,meadowBounds.min.y+view.orthographicSize,meadowBounds.max.y-view.orthographicSize),-10);
            if (motion!=shownMotion) UpdateLabel(motion);
        }

        public void SetDemo(HeroMotion motion) { DemoMotion=motion==HeroMotion.Run ? HeroMotion.Run : HeroMotion.Idle; UpdateLabel(motion); }
        public void ChooseHair(int style) { hero.SetAppearance(style,hero.CoatStyle,hero.WearingClothes); }
        public void ChooseCoat(int style) { hero.SetAppearance(hero.HairStyle,style,true); UpdateBodyLabel(); }
        public void ChooseWeapon(int index) { hero.SetWeapon(index); }
        public void ToggleBody() { hero.SetAppearance(hero.HairStyle,hero.CoatStyle,!hero.WearingClothes); UpdateBodyLabel(); }
        public void ToggleEyes() { AmberEyes=!AmberEyes; hero.SetEyeColor(AmberEyes); }
        public void ToggleZoom() { CloseView=!CloseView; }
        public void Stand() => SetDemo(HeroMotion.Idle);
        public void Run() => SetDemo(HeroMotion.Run);
        public void HairOne() => ChooseHair(0);
        public void HairTwo() => ChooseHair(1);
        public void IvoryCoat() => ChooseCoat(0);
        public void NavyCoat() => ChooseCoat(1);
        public void JadeCoat() => ChooseCoat(2);
        public void CrimsonCoat() => ChooseCoat(3);
        public void WeaponZero() => ChooseWeapon(0);
        public void WeaponOne() => ChooseWeapon(1);
        public void WeaponTwo() => ChooseWeapon(2);
        public void WeaponThree() => ChooseWeapon(3);
        public void WeaponFour() => ChooseWeapon(4);
        void UpdateBodyLabel() { if (bodyButtonLabel!=null) bodyButtonLabel.text=hero.WearingClothes ? "Trang phục cơ bản" : "Mặc áo ngoài"; }
        void UpdateLabel(HeroMotion motion)
        {
            shownMotion=motion;
            if (status!=null) status.text=motion==HeroMotion.Run ? "Đang chạy" : "Đang đứng";
            UpdateBodyLabel();
        }
        void OnDestroy() { move?.Dispose(); }
    }
}
