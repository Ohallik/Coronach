using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.UI
{
    /// <summary>Graphics-only inspection. Teleports to fixed viewpoints; never counts as route evidence. It takes the zones named by <c>-look-zones A,B</c>, in the order given, or with none
    /// every zone in the build: chapter 1's six as they have always been taken, then each other built zone by its chapter and then its id (the build list's own order).</summary>
    public sealed class ZoneLookProbe:MonoBehaviour
    {
        /// <summary>The six the probe was written for and where it stands in each, set by hand. Their stills are compared run against run, so their order, their points and their file names
        /// stay as they are and nothing is added here: any other zone works its three places out from what is in it (<see cref="Places"/>).</summary>
        static readonly string[] First={"Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowApproach","TallowDrift"};
        static readonly Vector3[][] FirstPoints={new[]{new Vector3(-20,1,0),new Vector3(0,1,30),new Vector3(25,1,25)},new[]{new Vector3(-20,0,0),new Vector3(0,0,0),new Vector3(20,0,0)},
            new[]{new Vector3(0,0,18),new Vector3(0,0,75),new Vector3(0,0,164)},new[]{new Vector3(0,1,8),new Vector3(Mathf.Sin(320f/900*Mathf.PI*4)*12,1,320),new Vector3(Mathf.Sin(805f/900*Mathf.PI*4)*12,1,805)},
            new[]{new Vector3(0,1,-8),new Vector3(10,1,6),new Vector3(-10,1,16)},new[]{new Vector3(0,0,-3),new Vector3(-4,0,5),new Vector3(6,0,3)}};
        /// <summary>Two derived places nearer than this are one place: the same still twice.</summary>
        const float Apart=6;
        /// <summary>Which zones, in which order. Named: those, as given. Not named: the six, then every other ZoneDef whose scene this build can load, by chapter (a zone with none last, as the
        /// build list has it) and then id.</summary>
        public static string[] Zones(string named)
        {
            var zones=new List<string>();
            if(!string.IsNullOrWhiteSpace(named))
            {
                foreach(string part in named.Split(',')){string zone=part.Trim();if(zone.Length>0&&!zones.Contains(zone))zones.Add(zone);}
                return zones.ToArray();
            }
            zones.AddRange(First);var later=new List<ZoneDef>();
            foreach(var zone in GameCatalog.All<ZoneDef>())if(zone!=null&&System.Array.IndexOf(First,zone.id)<0&&Application.CanStreamedLevelBeLoaded(zone.id))later.Add(zone);
            later.Sort((a,b)=>Rank(a)!=Rank(b)?Rank(a).CompareTo(Rank(b)):string.CompareOrdinal(a.id,b.id));
            foreach(var zone in later)zones.Add(zone.id);
            return zones.ToArray();
        }
        static int Rank(ZoneDef zone)=>zone.chapter<=0?int.MaxValue:zone.chapter;
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        /// <summary>Three places to stand in a zone nobody has tuned by hand: where the party arrives, then the two spots furthest from it and from each other among those a player is known
        /// to stand at (the other spawns, every prompt, every fight), so the stills take in the zone's far ends and not three corners of its first room. On foot a spot is the walkable point
        /// nearest it; in flight it is on the flight plane, held 10 m short of whatever a prompt belongs to. A zone with nowhere else in it is taken from round its arrival, at the steps
        /// Tallow's two zones use.</summary>
        static Vector3[] Places(Vector3 party)
        {
            var zone=ZoneController.Current;bool flight=zone!=null&&zone.Flight;var arrival=party;
            foreach(var spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))if(spawn.id=="Arrival")arrival=spawn.transform.position;
            if(flight&&zone.definition!=null)arrival.y=zone.definition.flightPlane;
            var spots=new List<Vector3>();
            bool Stand(Vector3 at,float shy,out Vector3 stood)
            {
                stood=at;
                if(flight){stood.y=arrival.y;stood=Vector3.MoveTowards(stood,arrival,shy);return true;}
                if(!NavMesh.SamplePosition(at,out var floor,4,NavMesh.AllAreas))return false;
                stood=floor.position;return true;
            }
            void Add(Vector3 at,float shy){if(Stand(at,shy,out var stood))spots.Add(stood);}
            foreach(var spawn in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))Add(spawn.transform.position,0);
            foreach(var prompt in FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None))Add(prompt.transform.position,10);
            foreach(var fight in FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None))Add(fight.transform.position,0);
            // The same places every run, whatever order the scene hands its objects over in.
            spots.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.z!=b.z?a.z.CompareTo(b.z):a.y.CompareTo(b.y));
            var places=new[]{arrival,arrival,arrival};
            var steps=flight?new[]{new Vector3(10,0,14),new Vector3(-10,0,24)}:new[]{new Vector3(-4,0,8),new Vector3(6,0,6)};
            for(int pick=1;pick<3;pick++)
            {
                float best=Apart;bool found=false;
                foreach(var spot in spots)
                {
                    float apart=float.PositiveInfinity;for(int had=0;had<pick;had++)apart=Mathf.Min(apart,Flat(spot,places[had]));
                    if(apart>best){best=apart;places[pick]=spot;found=true;}
                }
                if(!found&&Stand(arrival+steps[pick-1],0,out var near))places[pick]=near;
            }
            return places;
        }
        /// <summary>Where the second of the party stands at a derived place: two metres to the right if there is floor there (a ship's length in flight), or else with the first.</summary>
        static Vector3 Beside(Vector3 place,int member)
        {
            if(member==0)return place;
            bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            if(flight)return place+Vector3.right*member*HeroCollision.ArrivalSpacing;
            return NavMesh.SamplePosition(place+Vector3.right*member*2,out var floor,1.5f,NavMesh.AllAreas)?floor.position:place;
        }
        IEnumerator Start()
        {
            string folder=DevArgs.Value("-look");Directory.CreateDirectory(folder);
            string[] zones=Zones(DevArgs.Value("-look-zones"));
            // Said before the first still: a zone built since this player was never finishes loading, and the run would time out with nothing to say why.
            foreach(string zone in zones)if(!Application.CanStreamedLevelBeLoaded(zone)){Debug.LogError("FAILED: look: "+zone+" is not a scene in this build. Build the zone, then the dev player.");Application.Quit(1);yield break;}
            Debug.Log("LOOK_ZONES "+string.Join(",",zones));
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            int captured=0;
            for(int z=0;z<zones.Length;z++)
            {
                if(SceneFlow.Current.Zone!=zones[z]){SceneFlow.Current.LoadZone(zones[z]);while(SceneFlow.Current.Loading)yield return null;}
                yield return new WaitForSecondsRealtime(.8f);
                var party=PartyController.Current;foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;member.Health.InvulnerableUntil=float.PositiveInfinity;}
                var rig=FindFirstObjectByType<CameraRig>();var original=rig.profile;var profile=Instantiate(original);
                int tuned=System.Array.IndexOf(First,zones[z]);var points=tuned>=0?FirstPoints[tuned]:Places(party.Active.transform.position);
                for(int i=0;i<3;i++)
                {
                    for(int m=0;m<party.members.Length;m++){var actor=party.members[m];var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=tuned>=0?points[i]+Vector3.right*m*2:Beside(points[i],m);cc.enabled=true;actor.motor.Move(Vector2.zero,false,true);}
                    profile.yaw=original.yaw+(i-1)*12;rig.Apply(party.Active.transform,profile);
                    for(float t=0;t<1.5f;t+=Time.unscaledDeltaTime){foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;yield return null;}
                    string path=Path.Combine(folder,zones[z]+"-"+i+".png");ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.6f);captured++;
                    Debug.Log("LOOK_CAPTURE "+path);
                }
                rig.Apply(party.Active.transform,original);Destroy(profile);
            }
            Debug.Log("ZONE_LOOK_OK count="+captured);Application.Quit();
        }
    }
}
