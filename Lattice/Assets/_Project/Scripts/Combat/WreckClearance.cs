using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// D407, D413: nobody is left inside a beaten boss. A small body that falls is thrown clear of him (DefeatPresentation.FallClear, D386); a boss's body stays where it ends (shut
    /// down, destroyed, kneeling, yielding, or reeling before it leaves), and cannot be. For a while after the end, a hero of its own form (lead or partner, on foot or in flight)
    /// found in what it draws is slid out of it by his own controller, the shortest way out that is open: a wall stops him, and on foot he only goes over floor at his own height,
    /// so never off a deck or a shelf. It is added to his own moving, never put in its place: the stick still moves him, and nothing holds him once he is clear.
    /// D413: D407 measured its capsule, and what is drawn reaches further: Taren stood in Dessik Varn's beaten arm (nov24-whole s30/045), Sela's ship under the Hulljack's head
    /// (s29/043, cleared at 4.24 m of its middle), Idra half in the skiff (s52/171) and in Vessa's pole (s48/229), Taren against the Floodcrown's head (s21/288, cleared at 2.50).
    /// Now what it draws is read from its posed skin (and, for a part that can't be read or isn't a skin, the box it is drawn in, turned with it), every quarter second, as the
    /// camera sees it from above: anything of it at a hero's feet or over him on the plan is in him. A crate it ended on (Dessik's cargo, pulled against his frame) is slid off too.
    /// </summary>
    public sealed class WreckClearance:MonoBehaviour
    {
        /// <summary>How long after the end a hero found in it begins to be eased (on the heroes' clock, which the kill's stop holds: the stop, the card, the first lines, and a lead
        /// walking on into it meanwhile, as the replay's did into the Floodcrown a second after), how long one easing may run, the time the slide takes and its least pace, and how
        /// far past touching what it draws he is left.</summary>
        public const float Window=6,MostEase=1.5f,Over=.3f,LeastPace=3,Margin=.15f;
        /// <summary>On foot: how far above or below the body's root he may stand and still be in it (higher, he stands on it: the Foundry Saint's back), and the most the floor may
        /// fall away under the next step of the slide.</summary>
        const float SameFloor=1.2f,MostDrop=.4f;
        /// <summary>What it draws is kept a point per <see cref="Grain"/> of the plan (the highest of it there), each standing for that much round it; a box is sampled every
        /// <see cref="BoxStep"/> over its faces. What lies lower than <see cref="Above"/> over a hero's feet (a ring on the floor, a shadow) is under him, not in him.</summary>
        const float Grain=.1f,BoxStep=.2f,Above=.15f,ReadEvery=.25f,Near=3,Probe=.3f,TowardItsMiddle=.25f;
        const int MostSamples=60,Bearings=16;
        CharacterController capsule;Health body;float began,readAt=float.NegativeInfinity;Bounds whole;bool any;
        readonly List<Vector3> points=new();readonly Dictionary<Vector2Int,int> cells=new();readonly List<Vector3> skin=new();
        readonly List<Vector2> spans=new();
        readonly Dictionary<Component,(float pace,float since,Vector3 way)> easing=new();
        /// <summary>Begun on a boss's end (BossController), for a body with a capsule.</summary>
        public static void Begin(GameObject boss)
        {
            if(boss==null||!boss.TryGetComponent<CharacterController>(out _)||boss.GetComponent<WreckClearance>()!=null)return;
            // D418: not the freed Cantor. It is no wreck: it turns and flies off at once (CantorRelease), the companion's own guide keeps her clear of it, and the lead is the
            // player's to fly. Begun on it, it slid a lead the release tests hold still 0.36 m (RecordedReleaseSpacingTests, suite nov25a).
            if(boss.TryGetComponent<SerpentSegments>(out _))return;
            boss.AddComponent<WreckClearance>();
        }
        void Awake(){capsule=GetComponent<CharacterController>();body=GetComponent<Health>();began=GameTime.Now;}
        string Id=>body!=null?body.id:name;
        void Update()
        {
            float now=GameTime.Now-began,dt=GameTime.Delta;
            if(now>Window+MostEase){Destroy(this);return;}
            var party=PartyController.Current;if(party==null||party.members==null||capsule==null||dt<=0)return;
            if(GameTime.Now-readAt>=ReadEvery&&(!any||easing.Count>0||now<=Window&&Nearby(party)))Read();
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.gameObject.activeInHierarchy||hero.Health==null||!hero.Health.Alive||!hero.TryGetComponent<CharacterController>(out var own)||!own.enabled)continue;
                if(body!=null&&EnemyBrain.OtherForm(hero,body))continue;
                var at=hero.transform.position;if(!hero.flight&&Mathf.Abs(at.y-transform.position.y)>SameFloor)continue;
                float r=(hero.flight?Mathf.Max(own.radius,HeroCollision.HullRadius(hero.character)):own.radius)+Margin;
                bool known=easing.TryGetValue(hero,out var ease);
                if(!Inside(at,r,at.y))
                {
                    if(known){easing.Remove(hero);Debug.Log($"WRECK_CLEAR {Id} {hero.character} {Flat(at-Middle).magnitude:0.00} m from its middle");}
                    continue;
                }
                if(!known)
                {
                    if(now>Window)continue;
                    var way=Plan(at,r,at.y,s=>Shut(hero,own,s.way,s.step),out float depth);
                    ease=(Mathf.Max(LeastPace,depth/Over),now,way);easing[hero]=ease;
                    Debug.Log($"WRECK_EASE {Id} {hero.character} {depth:0.00} m to clear at {Flat(at-Middle).magnitude:0.00} m from its middle");
                }
                else if(now-ease.since>MostEase)continue;
                float step=ease.pace*dt;
                if(Shut(hero,own,ease.way,step))
                {
                    var way=Plan(at,r,at.y,s=>Shut(hero,own,s.way,s.step),out _);if(Shut(hero,own,way,step))continue;
                    ease.way=way;easing[hero]=ease;
                }
                HeroCollision.ThroughParty(own);own.Move(ease.way*step);
            }
            Crates(party,now,dt);
        }
        // ---------- what it draws ----------
        Vector3 Middle=>transform.TransformPoint(capsule.center);
        float Reach{get{var scale=transform.lossyScale;return capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));}}
        float Top=>Middle.y+capsule.height*.5f*Mathf.Abs(transform.lossyScale.y);
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        bool Nearby(PartyController party)
        {
            if(!any)return true;var box=whole;box.Expand(new Vector3(Near*2,1000,Near*2));
            foreach(var hero in party.members)if(hero!=null&&box.Contains(hero.transform.position))return true;
            foreach(var crate in LooseCargo.All)if(crate!=null&&box.Contains(crate.transform.position))return true;
            return false;
        }
        static bool Counts(Renderer drawn)
        {
            if(!(drawn is MeshRenderer||drawn is SkinnedMeshRenderer)||!drawn.enabled||!drawn.gameObject.activeInHierarchy||drawn.name=="Telegraph")return false;
            if(drawn.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)return false;
            var look=drawn.sharedMaterial;return look==null||look.shader==null||!look.shader.name.Contains("TextMeshPro");
        }
        /// <summary>Its posed body as the player draws it this frame: a readable skin point by point (a generated rig by its bones, another baked), and every other part (a mesh
        /// a build can't read, a held pole, a hull) by the box it is drawn in, turned with it: the same in the editor and the player.</summary>
        void Read()
        {
            points.Clear();cells.Clear();any=false;readAt=GameTime.Now;
            foreach(var drawn in GetComponentsInChildren<Renderer>())
            {
                if(!Counts(drawn))continue;
                if(any)whole.Encapsulate(drawn.bounds);else{whole=drawn.bounds;any=true;}
                if(drawn is SkinnedMeshRenderer posed)
                {
                    if(posed.sharedMesh!=null&&posed.sharedMesh.isReadable)
                    {
                        try{foreach(var p in GeneratedGeometry.WorldSkinPoints(posed))Mark(p);continue;}
                        catch(System.InvalidOperationException){}
                        var baked=new Mesh();posed.BakeMesh(baked,true);var pose=Matrix4x4.TRS(posed.transform.position,posed.transform.rotation,Vector3.one);
                        baked.GetVertices(skin);foreach(var p in skin)Mark(pose.MultiplyPoint3x4(p));Destroy(baked);continue;
                    }
                    Sample((posed.rootBone!=null?posed.rootBone:posed.transform).localToWorldMatrix,posed.localBounds);
                }
                else if(drawn.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null)Sample(drawn.transform.localToWorldMatrix,filter.sharedMesh.bounds);
            }
        }
        void Mark(Vector3 p)
        {
            var key=new Vector2Int(Mathf.FloorToInt(p.x/Grain),Mathf.FloorToInt(p.z/Grain));
            if(cells.TryGetValue(key,out int i)){if(p.y>points[i].y)points[i]=new Vector3(points[i].x,p.y,points[i].z);}
            else{cells[key]=points.Count;points.Add(p);}
        }
        /// <summary>A box's six faces, a point every <see cref="BoxStep"/> of the world: what it covers on the plan, from above.</summary>
        void Sample(Matrix4x4 into,Bounds box)
        {
            var lo=box.min;var size=box.size;
            int Count(int axis)=>Mathf.Clamp(Mathf.CeilToInt(size[axis]*((Vector3)into.GetColumn(axis)).magnitude/BoxStep),1,MostSamples);
            int nx=Count(0),ny=Count(1),nz=Count(2);
            for(int face=0;face<2;face++)
            {
                for(int a=0;a<=ny;a++)for(int b=0;b<=nz;b++)Mark(into.MultiplyPoint3x4(lo+new Vector3(size.x*face,size.y*a/ny,size.z*b/nz)));
                for(int a=0;a<=nx;a++)for(int b=0;b<=nz;b++)Mark(into.MultiplyPoint3x4(lo+new Vector3(size.x*a/nx,size.y*face,size.z*b/nz)));
                for(int a=0;a<=nx;a++)for(int b=0;b<=ny;b++)Mark(into.MultiplyPoint3x4(lo+new Vector3(size.x*a/nx,size.y*b/ny,size.z*face)));
            }
        }
        /// <summary>Is any of it, or its capsule, within <paramref name="r"/> of a point on the plan, at or over <paramref name="feet"/>.</summary>
        bool Inside(Vector3 at,float r,float feet)
        {
            float low=feet+Above;
            if(Top>=low&&Flat(at-Middle).magnitude<Reach+r)return true;
            float rr=(r+Grain)*(r+Grain);
            foreach(var p in points){if(p.y<low)continue;float dx=p.x-at.x,dz=p.z-at.z;if(dx*dx+dz*dz<rr)return true;}
            return false;
        }
        /// <summary>How far along a way on the plan a circle of <paramref name="r"/> must go from <paramref name="at"/> to be out of it.</summary>
        float Out(Vector3 at,float r,float feet,Vector3 way)
        {
            float low=feet+Above;spans.Clear();
            void Span(float x,float z,float reach)
            {
                float qx=x-at.x,qz=z-at.z,along=qx*way.x+qz*way.z,across=qx*qx+qz*qz-along*along,rr=reach*reach;
                if(across>=rr)return;float half=Mathf.Sqrt(rr-across);if(along+half<=0)return;spans.Add(new Vector2(along-half,along+half));
            }
            var middle=Middle;if(Top>=low)Span(middle.x,middle.z,Reach+r);
            foreach(var p in points)if(p.y>=low)Span(p.x,p.z,r+Grain);
            spans.Sort((a,b)=>a.x.CompareTo(b.x));
            float s=0;foreach(var span in spans){if(span.x>s)break;s=Mathf.Max(s,span.y);}
            return s;
        }
        /// <summary>The shortest way out that is open (straight out from its middle on a tie): the ways round from straight out to either side, each as far as it takes to be out of
        /// it. Never back across its middle: a hero slid through the wreck to its far side (the deck test's first run: out past the Marshal's middle, not along the deck).</summary>
        Vector3 Plan(Vector3 at,float r,float feet,System.Func<(Vector3 way,float step),bool> shut,out float depth)
        {
            var radial=Flat(at-Middle);radial=radial.sqrMagnitude>.0025f?radial.normalized:Vector3.back;
            var ways=new List<(float score,float depth,Vector3 way)>(Bearings);
            for(int i=0;i<Bearings;i++)
            {
                float angle=Mathf.DeltaAngle(0,i*360f/Bearings);if(Mathf.Abs(angle)>90.01f)continue;var way=Quaternion.Euler(0,angle,0)*radial;float d=Out(at,r,feet,way);
                ways.Add((d+TowardItsMiddle*Mathf.Abs(angle)/180,d,way));
            }
            ways.Sort((a,b)=>a.score.CompareTo(b.score));
            foreach(var (_,d,way) in ways)if(!shut((way,Mathf.Min(Mathf.Max(d,.05f),Probe)))){depth=d;return way;}
            depth=ways[0].depth;return ways[0].way;
        }
        /// <summary>Whether something solid stands within the step that way or (on foot) there is no floor at his height under it.</summary>
        bool Shut(CombatActor hero,CharacterController own,Vector3 toward,float step)
        {
            var at=hero.transform.position;var centre=at+own.center;float r=own.radius*.9f,half=Mathf.Max(0,own.height*.5f-own.radius);
            var low=centre-Vector3.up*(half-(hero.flight?0:own.stepOffset*.5f));var high=centre+Vector3.up*half;
            foreach(var hit in Physics.CapsuleCastAll(low,high,r,toward,step+.05f,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.distance<=0||hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<CombatActor>()!=null)continue;
                return true;
            }
            return !hero.flight&&!Floored(at+toward*(step+own.radius*.5f),at.y);
        }
        static bool Floored(Vector3 at,float height)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(at.x,height+1,at.z),Vector3.down,1+MostDrop+.05f,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponentInParent<CombatActor>()!=null||hit.collider.GetComponentInParent<EnemyBrain>()!=null)continue;
                if(hit.point.y>=height-MostDrop)return true;
            }
            return false;
        }
        // ---------- a crate it ended on ----------
        /// <summary>Loose cargo standing in it (Dessik Varn's, pulled against his frame by his Magnet Pulse, his legs drawn through it as he went down: s30/045) is slid out the same
        /// way, not solid while it goes, and set down where it is out of him and of the heroes; a crate a move still carries is the move's.</summary>
        void Crates(PartyController party,float now,float dt)
        {
            for(int i=LooseCargo.All.Count-1;i>=0;i--)
            {
                var crate=i<LooseCargo.All.Count?LooseCargo.All[i]:null;if(crate==null||!crate.isActiveAndEnabled)continue;
                bool known=easing.TryGetValue(crate,out var ease);if(!known&&crate.Moving)continue;
                var middle=crate.Middle;float r=crate.Half*1.2f+Margin,feet=transform.position.y;
                if(!known&&Mathf.Abs(middle.y-crate.Half-feet)>SameFloor)continue;
                if(!Inside(middle,r,feet)&&!(known&&OnAHero(party,middle,r)))
                {
                    if(known){easing.Remove(crate);crate.Place(crate.transform.position);Debug.Log($"WRECK_CLEAR {Id} crate {Flat(middle-Middle).magnitude:0.00} m from its middle");}
                    continue;
                }
                if(!known)
                {
                    if(now>Window)continue;
                    crate.Lift();var way=Plan(middle,r,feet,s=>CombatCover.Sweep(middle,middle+s.way*(s.step+crate.Half),crate.Half*.8f,out _),out float depth);
                    ease=(Mathf.Max(LeastPace,depth/Over),now,way);easing[crate]=ease;
                    Debug.Log($"WRECK_EASE {Id} crate {depth:0.00} m to clear at {Flat(middle-Middle).magnitude:0.00} m from its middle");
                }
                float step=ease.pace*dt;
                if(now-ease.since>MostEase||CombatCover.Sweep(middle,middle+ease.way*(step+crate.Half),crate.Half*.8f,out _))
                {
                    easing.Remove(crate);crate.Place(crate.transform.position);Debug.Log($"WRECK_STOP {Id} crate {Flat(middle-Middle).magnitude:0.00} m from its middle");
                    continue;
                }
                crate.Hold(crate.transform.position+ease.way*step);
            }
        }
        static bool OnAHero(PartyController party,Vector3 at,float r)
        {
            foreach(var hero in party.members)
            {
                if(hero==null||hero.flight||!hero.TryGetComponent<CharacterController>(out var own))continue;
                if(Flat(hero.transform.position-at).magnitude<r+own.radius)return true;
            }
            return false;
        }
    }
}
