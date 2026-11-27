using System.Collections.Generic;
using Lattice.Combat;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The ground camera flies high behind the hero over a town of tall things. Anything tall that comes between it and
    /// the hero opens a hero-sized hole; anything passing right under the lens fades to a ghost as a whole, or a street arch
    /// fills the screen with bricks. The shader's occluder fade does the drawing; the builder enables it on these materials.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class SceneryOcclusion:MonoBehaviour
    {
        const float UnderLens=8,FadeSpeed=6;
        /// <summary>Only what really comes up to the lens fades whole. Low things passing under it (a home, a tower lying on its side) left dotted silhouettes across half the floor,
        /// with their block outlines and their shadows, right where the party walks and fights.</summary>
        const float LensClearance=4;
        /// <summary>What does reach the lens goes altogether, every pass and its shadow: at a full fade the screen door still keeps 3 cells in 16, a huge dotted silhouette over the floor.
        /// Past 1 the shadow goes too. A hole for the hero stops at 1 and keeps the body and the shadow round it. Coming back it eases in over about half a second rather than popping solid.</summary>
        const float Gone=1.25f,ComeBack=.35f;
        /// <summary>The screen door opens all the way, so the hole is a clean window with a soft rim and the hero stands in it whole, not in a bubble of dots. A boss keeps more of its body and sets its own.</summary>
        const float OpenAllTheWay=1;
        static readonly int Depth=Shader.PropertyToID("_OccFadeDepth");
        static readonly int Fade=Shader.PropertyToID("_OccFade"),Ellipse=Shader.PropertyToID("_OccFadeEllipse"),Second=Shader.PropertyToID("_OccFadeEllipse2");
        static readonly Vector4 WholeScreen=new(.5f,.5f,4,4);
        static readonly List<SceneryOcclusion> all=new();
        static int updated=-1;
        static MaterialPropertyBlock block;
        /// <summary>D421: points round a standing hero (height, and across the screen), with a margin past the body, that must all lie behind this one piece for it to count as
        /// hiding the hero whole. A head over its far edge, an arm past its side, and it doesn't: HeroThrough draws the hidden part as a line and the rest shows in colour.</summary>
        static readonly Vector2[] Round={new(0,.1f),new(0,.95f),new(0,2.05f),new(-.42f,.55f),new(.42f,.55f),new(-.42f,1.45f),new(.42f,1.45f)};
        /// <summary>Who a piece's hole is open for: the lead or the partner.</summary>
        const int ForLead=1,ForPartner=2;
        /// <summary>What a piece needs of a hero to judge whether it hides them: the chest, the points round the body, and the hole's ellipse on the screen.</summary>
        struct Sight{public Vector3 chest;public Vector3[] round;public Vector4 ellipse;}
        static Sight leadSight=new(){round=new Vector3[Round.Length]},partnerSight=new(){round=new Vector3[Round.Length]};
        Renderer[] bodies;Bounds bounds;float amount,whole;int holeFor=ForLead;
        /// <summary>A gateway's way through, in its own space. A hero seen through it isn't behind anything: going by the arch's whole box bit a hole in the arch around someone
        /// standing in plain view in its opening.</summary>
        public Bounds opening;
        /// <summary>What's really there, as filled cells in its own space, measured when the zone was built: the box round a fruit tree has nothing in its corners, and a hero beside its foot wore a cloud of dots in a crown that hid nobody. Nothing measured falls back on the box.</summary>
        public byte[] shape;public Vector3 shapeLow;public float shapeCell;public int shapeX,shapeY,shapeZ;
        void Awake()
        {
            bodies=GetComponentsInChildren<Renderer>(true);bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var body in bodies)if(body is MeshRenderer or SkinnedMeshRenderer)bounds.Encapsulate(body.bounds);
        }
        void OnEnable()=>all.Add(this);
        void OnDisable(){all.Remove(this);if(amount>0){amount=0;whole=0;Write(Vector4.zero,Vector4.zero);}}
        void LateUpdate()
        {
            if(updated==Time.frameCount)return;updated=Time.frameCount;
            var camera=Camera.main;var hero=PartyController.Current!=null?PartyController.Current.Active:null;
            if(camera==null||hero==null)return;
            bool active=!hero.flight;
            // D414: no hole for a lead drawn through what hides him (HeroThrough's clean outline, as D395 has it for a boss). The hole was a window into whatever lies under the
            // roof, darker than the roof round it: Taren walking the passage under Harrowgate's gatehouse stood in a dark square hole on the gate roof beside Idra's outline
            // (nov24-whole s50/071). A lead nothing draws through still gets it whenever his chest is hidden. What passes right under the lens still fades whole.
            // D421: but a hero hidden WHOLE by one broad piece, lead or partner, gets the window again. Their outline alone, lying wholly on a lit roof or the top of an arch,
            // read as a ghost walking ON it (nov25-r5-chain s51/218 Taren on Harrowgate's gate roof, s13/007 Sela on the Arch Market's arch, s26/195 on a pillar's ledge):
            // nothing of them showed past the edge to say they were behind it. In the window they stand in their own colours on the floor under it.
            // D432: but never for the partner. Behind scenery she is simply hidden, as anyone is, and nothing draws her through it (HeroThrough); her window of her own (D427,
            // _OccFadeEllipse2) is never opened now but in a test's before (HeroThrough.AsItWas).
            // D438: and none for the lead either. In his window he stood in his own colours in an oval lying on the roof's or the wall's flat top, and three reviewers told
            // what it was still read him as walking on the roof inside a stain (nov26-r7-chain s51/218, s50/058, s58/151); opened in part it was a stippled smudge on the
            // planks (s16/114, s25/012). A hero HeroThrough finds (the mark off a boss, the line by one) is simply hidden by scenery, as anyone is; what passes right under the
            // lens still fades whole. A lead nothing draws through or marks keeps the chest rule, and a test's before (HeroThrough.AsThisMorning) the window.
            bool outlined=BossOcclusion.Outlined(hero),holes=!HeroThrough.Finds(hero);
            var partner=active&&outlined&&HeroThrough.AsItWas?PartnerOf(hero):null;
            var eye=camera.transform.position;
            See(camera,hero,ref leadSight);if(partner!=null)See(camera,partner,ref partnerSight);
            float step=Time.unscaledDeltaTime*FadeSpeed;
            foreach(var scenery in all)scenery.Refresh(active,holes,outlined,partner!=null,eye,step);
        }
        /// <summary>The partner out beside the lead, on her feet and drawn through what hides her (HeroThrough); the window is for her too.</summary>
        static CombatActor PartnerOf(CombatActor lead)
        {
            var members=PartyController.Current.members;if(members==null)return null;
            foreach(var member in members)if(member!=null&&member!=lead&&member.gameObject.activeInHierarchy&&!member.flight&&BossOcclusion.Outlined(member))return member;
            return null;
        }
        static void See(Camera camera,CombatActor hero,ref Sight sight)
        {
            var feet=hero.transform.position;var center=feet+Vector3.up*.95f;var right=camera.transform.right;
            var view=camera.WorldToViewportPoint(center);
            sight.chest=center;
            sight.ellipse=new Vector4(view.x,view.y,Mathf.Abs(camera.WorldToViewportPoint(center+right*.95f).x-view.x),Mathf.Abs(camera.WorldToViewportPoint(center+camera.transform.up*1.45f).y-view.y));
            for(int i=0;i<Round.Length;i++)sight.round[i]=feet+right*Round[i].x+Vector3.up*Round[i].y;
        }
        void Refresh(bool active,bool holes,bool outlined,bool partner,Vector3 eye,float step)
        {
            bool under=active&&bounds.max.y>eye.y-LensClearance&&bounds.SqrDistance(eye)<UnderLens*UnderLens;
            int now=0;
            // One hero-sized window a piece, the lead's first. One wide enough for both opened most of Harrowgate's gatehouse round Taren on its walk while Sela, behind the gate's
            // door (a prop of its own, which never opens), kept her line in the middle of the hole.
            // Nor one for the partner where this piece hides the lead's chest: his colours past its edge say where they both are, and a window beside his line was one piece drawn
            // two ways (the gate roof at s50/071, which read right with both lines).
            // D427: but a partner hidden whole by a piece that holds the lead's window gets a window of her own in it, hero-sized, beside his: her line alone there, on
            // the Walking Canopy's root arch beside Taren in his window, read as her standing on top of the root (nov25-r6-chain s21/295).
            if(active&&holes&&!under)
            {
                if(outlined?HidesWhole(eye,leadSight):HidesChest(eye,leadSight))now=ForLead;
                if(partner&&(now==ForLead||!Behind(eye,leadSight.chest))&&HidesWhole(eye,partnerSight))now|=ForPartner;
            }
            bool between=now!=0;if(between)holeFor=now;
            float target=under||between?1:0;
            if(amount==0&&target==0)return;
            // The mask eases too: swapping from the whole body to the hero's hole mid-fade popped the rest solid in one frame.
            if(amount==0)whole=under?1:0;
            else if(under)whole=Mathf.MoveTowards(whole,1,step);
            else if(between)whole=Mathf.MoveTowards(whole,0,step);
            amount=Mathf.MoveTowards(amount,target,target<amount?step*ComeBack:step);
            // Closing, the window stays on whoever it opened for, while they are still out.
            var ellipse=holeFor==ForPartner&&partner?partnerSight.ellipse:leadSight.ellipse;
            var second=holeFor==(ForLead|ForPartner)&&partner?partnerSight.ellipse:Vector4.zero;
            Write(Vector4.Lerp(ellipse,WholeScreen,whole*whole),second);
        }
        /// <summary>The line to the chest crosses this piece (the rule before D414, kept for a lead nothing draws through).</summary>
        bool HidesChest(Vector3 eye,Sight hero)
        {
            var ray=new Ray(eye,hero.chest-eye);float distance=Vector3.Distance(eye,hero.chest);
            return bounds.IntersectRay(ray,out float entry)&&entry<distance-.15f&&!SeenThroughOpening(eye,hero.chest)&&Crossed(eye,ray.GetPoint(distance-.15f));
        }
        /// <summary>Every point round the hero lies behind this piece, by more than a body is thick (D421).</summary>
        bool HidesWhole(Vector3 eye,Sight hero)
        {
            if(hero.round==null)return false;
            foreach(var point in hero.round)if(!Behind(eye,point))return false;
            return true;
        }
        bool Behind(Vector3 eye,Vector3 point)
        {
            var ray=new Ray(eye,point-eye);float distance=Vector3.Distance(eye,point),near=distance-HeroThrough.Thickness;
            return bounds.IntersectRay(ray,out float entry)&&entry<near&&!SeenThroughOpening(eye,point)&&Crossed(eye,ray.GetPoint(near));
        }
        /// <summary>Does the line to the chest really cross it, or only the box round it? A few lookups, and only for what the box already says is in the way.</summary>
        bool Crossed(Vector3 eye,Vector3 chest)
        {
            if(shape==null||shape.Length==0)return true;
            Vector3 from=transform.InverseTransformPoint(eye)-shapeLow,along=transform.InverseTransformPoint(chest)-shapeLow-from;
            float enter=0,leave=1;
            for(int axis=0;axis<3;axis++)
            {
                float size=shapeCell*(axis==0?shapeX:axis==1?shapeY:shapeZ);
                if(Mathf.Abs(along[axis])<1e-5f){if(from[axis]<0||from[axis]>size)return false;continue;}
                float a=-from[axis]/along[axis],b=(size-from[axis])/along[axis];
                enter=Mathf.Max(enter,Mathf.Min(a,b));leave=Mathf.Min(leave,Mathf.Max(a,b));
                if(enter>leave)return false;
            }
            float stride=shapeCell*.5f/Mathf.Max(along.magnitude,1e-4f);
            for(float t=enter;t<=leave;t+=stride)
            {
                var p=from+along*t;
                int bit=(Mathf.Clamp((int)(p.z/shapeCell),0,shapeZ-1)*shapeY+Mathf.Clamp((int)(p.y/shapeCell),0,shapeY-1))*shapeX+Mathf.Clamp((int)(p.x/shapeCell),0,shapeX-1);
                if((shape[bit>>3]&(1<<(bit&7)))!=0)return true;
            }
            return false;
        }
        bool SeenThroughOpening(Vector3 eye,Vector3 hero)
        {
            if(opening.size.sqrMagnitude<=0)return false;
            Vector3 a=transform.InverseTransformPoint(eye),b=transform.InverseTransformPoint(hero);float depth=b.z-a.z;
            if(Mathf.Abs(depth)<.001f)return false;
            for(int face=0;face<2;face++)
            {
                var at=Vector3.Lerp(a,b,((face==0?opening.min.z:opening.max.z)-a.z)/depth);
                if(Mathf.Abs(at.x-opening.center.x)>opening.extents.x||at.y<opening.min.y||at.y>opening.max.y)return false;
            }
            return true;
        }
        void Write(Vector4 ellipse,Vector4 second)
        {
            block??=new MaterialPropertyBlock();
            foreach(var body in bodies)
            {
                if(body==null||body is not MeshRenderer&&body is not SkinnedMeshRenderer)continue;
                body.GetPropertyBlock(block);block.SetFloat(Fade,amount*Mathf.Lerp(1,Gone,whole));block.SetFloat(Depth,OpenAllTheWay);block.SetVector(Ellipse,ellipse);block.SetVector(Second,second);body.SetPropertyBlock(block);
            }
        }
    }
}
