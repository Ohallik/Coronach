using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A vessel held out of its lane by knots of weed (chapter 3's seed barges). Each knot is an anchored enemy standing at one of this barge's roots; when one dies it says what it
    /// let go of (<see cref="EnemyAnchored.Freed"/>), and the barge answers for its own: that root's cable goes, and when the last has gone the barge is free. It sets its flag,
    /// reports one counted interaction to the quests (a flag set from code moves no quest step until the next load), has the active hero say so, and drifts back along an authored
    /// path to lie in the lane as cover. A save made after that finds it already there; one made with a knot or two cut finds it held by all of them again, as their fight is.
    /// </summary>
    public sealed class SnaggedBarge:MonoBehaviour
    {
        /// <summary>The knots' encounter (<c>clear.id</c>): read once as the zone loads, for a save that cleared it and never saw the barge let go.</summary>
        public string knotsEncounter;
        public string freedFlag,report="SeedBarge",bark="barge",knotId="SnagvineNode";
        /// <summary>Where it goes once free, in the world, from where it lies snagged: the last point is where it stays.</summary>
        public Vector3[] drift=System.Array.Empty<Vector3>();
        public float driftSeconds=10;
        /// <summary>How far it has swung round by the end, in degrees.</summary>
        public float turn;
        /// <summary>Where each knot holds it, and the cable drawn from there; the same order.</summary>
        public Transform[] roots=System.Array.Empty<Transform>();
        public GameObject[] cables=System.Array.Empty<GameObject>();
        /// <summary>Shown only while it is held (the weed's own mass), and only once it is free (its running lights).</summary>
        public GameObject whileSnagged,whenFree;
        /// <summary>How near a root a knot has to have held to be this barge's: the knots stand on their roots, and the next barge is hundreds of metres off.</summary>
        const float RootReach=3;
        GameState owner;bool free,underWay;float gone;Vector3 from;Quaternion heading;bool[] cut;
        public bool Free=>free;
        /// <summary>How many of its knots have let go.</summary>
        public int Cut{get{int n=0;if(cut!=null)foreach(bool c in cut)if(c)n++;return n;}}
        void OnEnable()=>EnemyAnchored.Freed+=OnFreed;
        void OnDisable(){EnemyAnchored.Freed-=OnFreed;FlightMotor.UnderWay.Remove(transform);}
        void Start()
        {
            from=transform.position;heading=transform.rotation;cut=new bool[roots.Length];
            var services=GameServices.Current;if(services==null)return;
            owner=services.State;
            if(services.Flags.GetBool(freedFlag)){free=true;gone=1;Place(1);}
            // Cleared and saved in the frame before the barge answered: it is free, and has been for a while.
            else if(!string.IsNullOrEmpty(knotsEncounter)&&services.Flags.GetBool("clear."+knotsEncounter)){Release(services,false);gone=1;Place(1);}
            Show();
        }
        void OnFreed(EnemyBrain knot,Vector3 held)
        {
            var services=GameServices.Current;
            if(free||cut==null||services==null||!ReferenceEquals(owner,services.State)||knot==null||knot.definition==null||knot.definition.id!=knotId)return;
            int nearest=-1;float best=RootReach*RootReach;
            for(int i=0;i<roots.Length;i++)
            {
                if(roots[i]==null||cut[i])continue;
                var offset=roots[i].position-held;offset.y=0;if(offset.sqrMagnitude<best){best=offset.sqrMagnitude;nearest=i;}
            }
            if(nearest<0)return;
            cut[nearest]=true;if(nearest<cables.Length&&cables[nearest]!=null)cables[nearest].SetActive(false);
            Debug.Log("BARGE_KNOT_CUT "+freedFlag+" "+Cut+"/"+roots.Length);
            if(Cut>=roots.Length)Release(services,true);
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null||!ReferenceEquals(owner,services.State))return;
            if(free&&gone<1&&!GameTime.Paused){gone=Mathf.Min(1,gone+Time.deltaTime/Mathf.Max(.1f,driftSeconds));Place(Mathf.SmoothStep(0,1,gone));}
            // Come to rest, it is a hull like any other again.
            if(gone>=1&&underWay){underWay=false;FlightMotor.UnderWay.Remove(transform);}
        }
        void Release(GameServices services,bool said)
        {
            free=true;services.Flags.SetBool(freedFlag,true);Debug.Log("BARGE_FREED "+freedFlag);
            // While it drifts, what it comes against is shoved and not charged for it (FlightMotor.UnderWay): the ship that cut its last knot is where it starts from.
            if(said&&!underWay){underWay=true;FlightMotor.UnderWay.Add(transform);}
            if(!string.IsNullOrEmpty(report))RpgServices.Quests.Report(ObjectiveKind.Interact,report);
            if(said)
            {
                var party=PartyController.Current;if(party!=null&&party.Active!=null&&!string.IsNullOrEmpty(bark))BarkService.Play(party.Active.character,bark,true);
                AudioManager.PlayAt("doorOpen_000",transform,transform.position,.2f,AudioBus.SFX,90);
            }
            Show();
        }
        void Show()
        {
            if(whileSnagged!=null&&whileSnagged.activeSelf==free)whileSnagged.SetActive(!free);
            if(whenFree!=null&&whenFree.activeSelf!=free)whenFree.SetActive(free);
            if(free)foreach(var cable in cables)if(cable!=null)cable.SetActive(false);
        }
        void Place(float t)
        {
            if(drift==null||drift.Length==0)return;
            // Along the path by distance, so it neither rushes a long leg nor crawls a short one.
            float total=0;var last=from;foreach(var p in drift){total+=Vector3.Distance(last,p);last=p;}
            float want=total*t;last=from;var at=drift[drift.Length-1];
            foreach(var p in drift)
            {
                float leg=Vector3.Distance(last,p);
                if(want<=leg||leg<=0){at=leg>0?Vector3.Lerp(last,p,want/leg):p;break;}
                want-=leg;last=p;
            }
            transform.SetPositionAndRotation(at,heading*Quaternion.Euler(0,turn*t,0));
        }
    }
}
