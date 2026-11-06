using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Hessa Korl, Muster scavenger captain (D202): the first person the party duels rather than a beast. Her moves
    /// read like a swordsman's, after Elden Ring's knights and Ys's rival duels:
    /// <list type="bullet">
    /// <item>Hook Lance, a stepping combo whose last thrust is held a beat longer than the rhythm promises, so dodging
    /// on the beat gets you caught; four thrusts once she is desperate.</item>
    /// <item>Advance, a marked lunge that stops short of a hero standing off, then the lance if that brought her in reach: she walks slower than they jog.</item>
    /// <item>Grapple Chain, a hook thrown down a marked lane: anyone caught is dragged in and slammed; a dodged chain
    /// leaves her overextended and open.</item>
    /// <item>Sidearm, a backstep and three shots, which punishes standing off.</item>
    /// <item>Sweeping Cut, a full turn when both heroes crowd her (phase 2).</item>
    /// <item>Call the Crew, two scavengers at each phase shift (phase 2+).</item>
    /// <item>Lampbreaker, the stolen relay crystal raised high: a wide, slow flash to outrun or roll through (phase 3).</item>
    /// </list>
    /// She yields at the end; she is arrested, not killed.
    /// </summary>
    public sealed partial class BossController
    {
        float HessaRest=>Phase>=3?1.9f:Phase>=2?2.3f:2.8f;
        (string,IEnumerator) ChooseHessa(CombatActor hero)
        {
            var offset=Flat(hero.transform.position-transform.position);float distance=offset.magnitude;
            bool crowded=false;
            if(Phase>=2)
            {
                int near=0;foreach(var member in PartyController.Current.members)if(member.Health.Alive&&Flat(member.transform.position-transform.position).magnitude<4.2f)near++;
                crowded=near>=2;
            }
            if(crowded&&lastMove!="SweepingCut")return("SweepingCut",SweepingCut());
            string move=Pick(("HookLance",distance<8?2.2f:0),("Advance",distance>8?1.6f:0),("GrappleChain",distance>5?1.8f:.4f),("Sidearm",distance>8?1.2f:.5f),
                ("Lampbreaker",Phase>=3?1.4f:0),("SweepingCut",Phase>=2&&distance<4?.8f:0));
            return move switch
            {
                "Advance"=>(move,Advance()),
                "GrappleChain"=>(move,GrappleChain()),
                "Sidearm"=>(move,Sidearm()),
                "Lampbreaker"=>(move,Lampbreaker()),
                "SweepingCut"=>(move,SweepingCut()),
                _=>(move,HookLance(Phase>=3?4:3))
            };
        }

        IEnumerator HookLance(int thrusts)
        {
            var controller=GetComponent<CharacterController>();
            for(int n=0;n<thrusts;n++)
            {
                var hero=PartyController.Current.Active.transform.position;var direction=Toward(hero);transform.rotation=Quaternion.LookRotation(direction);
                bool last=n==thrusts-1;
                // The finisher holds a beat longer than the rhythm promises.
                float tell=last?.85f:n==0?.55f:.36f;
                // Painted from where she will stand when it lands: what the player sees is what is struck.
                var cone=Mark(Telegraph.Cone(transform.position+direction*1.6f,direction,4.4f,last?110:80,tell,last?Telegraph.Heavy:Telegraph.Threat));
                strikeAt=Time.time+tell;
                yield return Windup(tell,last?-14:-7,0,last?.04f:0);
                if(Interrupted)yield break;
                // Step into the thrust.
                for(float t=0;t<.12f&&!Interrupted;){if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;if(controller!=null&&controller.enabled)controller.Move(direction*(1.6f/.12f)*dt);yield return null;}
                Pose(10);
                Strike(cone,last?36:22,DamageType.Kinetic,last?.35f:0,last?2.2f:0);
                AudioManager.PlayAt(last?"impactMetal_000":"laserSmall_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,50,.04f,last?.8f:1.3f);
                yield return Windup(.12f,0);
            }
            Pose(0);Finish(HessaRest);
        }

        IEnumerator GrappleChain()
        {
            var heroActor=PartyController.Current.Active;var direction=Toward(heroActor.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=.75f;
            var lane=Mark(Telegraph.Line(transform.position,direction,14,2.2f,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            yield return Windup(tell,6,0,.03f);
            if(Interrupted)yield break;
            lane.Release();
            var chain=ChainLine(direction);
            CombatActor caught=null;
            foreach(var member in PartyController.Current.members)
                if(member.Health.Alive&&lane.Contains(member.transform.position)&&member.Health.Receive(Packet(14,DamageType.Pulse))>0){caught=member;break;}
            AudioManager.PlayAt("laserSmall_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,50,.04f,.6f);
            if(caught==null)
            {
                // Overextended: a dodged chain is the opening.
                StartCoroutine(ChainFlies(chain,direction,14));health.Expose(1.3f);BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);Debug.Log("HESSA_OPEN");
                Finish(HessaRest+.6f);yield break;
            }
            var body=caught.GetComponent<CharacterController>();
            for(float t=0;t<.3f&&!Interrupted;)
            {
                if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;
                var toward=Flat(transform.position+direction*1.8f-caught.transform.position);
                if(body!=null&&body.enabled&&toward.magnitude>.2f)body.Move(toward.normalized*Mathf.Min(toward.magnitude,28*dt));
                chain.SetPosition(0,ChainHand(direction));chain.SetPosition(1,caught.transform.position+Vector3.up);
                yield return null;
            }
            temporary.Remove(chain.gameObject);Destroy(chain.gameObject);
            if(Interrupted)yield break;
            var slam=Mark(Telegraph.Circle(transform.position+direction*1.6f,2.6f,.45f,Telegraph.Heavy));
            strikeAt=Time.time+.45f;
            yield return Windup(.45f,-12,.1f);
            if(Interrupted)yield break;
            Pose(8);Strike(slam,36,DamageType.Kinetic,.45f,2.4f);Tremor(.25f,.06f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.22f,AudioBus.SFX,50);
            yield return Windup(.2f,0);Pose(0);Finish(HessaRest);
        }

        IEnumerator Sidearm()
        {
            var controller=GetComponent<CharacterController>();var away=-Toward(PartyController.Current.Active.transform.position);
            for(float t=0;t<.25f&&!Interrupted;){if(GameTime.Paused){yield return null;continue;}float dt=Time.deltaTime;t+=dt;if(controller!=null&&controller.enabled)controller.Move(away*(4/.25f)*dt);yield return null;}
            for(int shot=0;shot<3&&!Interrupted;shot++)
            {
                var hero=PartyController.Current.Active.transform.position+Vector3.up;var muzzle=transform.position+Vector3.up*1.5f;
                var aim=Toward(hero);transform.rotation=Quaternion.LookRotation(aim);
                // The aim is fixed when the line appears: step off it.
                var line=Mark(Telegraph.Line(transform.position,aim,18,1.3f,.32f,Telegraph.Threat));
                strikeAt=Time.time+.32f;
                yield return Windup(.32f,-4);
                if(Interrupted)yield break;
                line.Release();
                Projectile.Fire(muzzle,hero-muzzle,Packet(16,DamageType.Plasma),20);
                CombatAudio.HostileShot(transform,muzzle);
            }
            Pose(0);Finish(HessaRest*.8f);
        }

        IEnumerator SweepingCut()
        {
            float tell=.6f;
            var circle=Mark(Telegraph.Circle(transform.position,4.6f,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            yield return Windup(tell,4,0,.05f);
            if(Interrupted)yield break;
            Strike(circle,28,DamageType.Kinetic,.3f,2.8f);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50);
            var rest=transform.rotation;
            for(float t=0;t<.28f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;transform.rotation=rest*Quaternion.Euler(0,360*Mathf.SmoothStep(0,1,t/.28f),0);yield return null;}
            transform.rotation=rest;Pose(0);Finish(HessaRest);
        }

        IEnumerator Lampbreaker()
        {
            // The stolen crystal raised high; a long, readable charge, then a flash across most of the floor.
            float tell=1.5f;
            var flash=Mark(Telegraph.Circle(transform.position,11,tell,Telegraph.Stagger));
            strikeAt=Time.time+tell;
            CombatVfx.Burst(transform.position+Vector3.up*3,new Color(1,.8f,.4f),"shot");
            yield return Windup(tell,-10,.35f,.04f);
            if(Interrupted)yield break;
            Strike(flash,42,DamageType.Beam,.5f,0,Shaded());Tremor(.35f,.08f);
            CombatVfx.Burst(transform.position+Vector3.up*2.5f,new Color(1.6f,1.2f,.6f),"kill");
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.35f,AudioBus.SFX,40,.04f,.55f);
            yield return Windup(.35f,0);Pose(0);Finish(HessaRest+.8f);
        }

        /// <summary>The flash is light: a column between her and a hero takes it, as the room was built to.</summary>
        HashSet<Health> Shaded()
        {
            var eye=transform.position+Vector3.up*2.5f;var shaded=new HashSet<Health>();
            foreach(var member in PartyController.Current.members)if(!CombatCover.Clear(eye,member.transform.position+Vector3.up))shaded.Add(member.Health);
            return shaded;
        }
        IEnumerator Advance()
        {
            var controller=GetComponent<CharacterController>();var target=PartyController.Current.Active.transform.position;
            var direction=Toward(target);transform.rotation=Quaternion.LookRotation(direction);
            float distance=Mathf.Clamp(Flat(target-transform.position).magnitude-3,3,16),tell=.6f,speed=22;
            var lane=Mark(Telegraph.Line(transform.position,direction,distance+1.6f,2.4f,tell,Telegraph.Threat));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-8,0,.03f);
            if(Interrupted)yield break;
            lane.Release();Pose(8);strikeAt=0;
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.16f,AudioBus.SFX,50,.04f,1.3f);
            var hit=new HashSet<Health>();float travelled=0,elapsed=0;Collide(false);
            while(travelled<distance&&elapsed<distance/speed+.3f&&!Interrupted)
            {
                if(GameTime.Paused){yield return null;continue;}
                elapsed+=Time.deltaTime;var before=transform.position;
                if(controller!=null&&controller.enabled)controller.Move(direction*Mathf.Min(speed*Time.deltaTime,distance-travelled));
                travelled+=Flat(transform.position-before).magnitude;
                Contact(hit,transform.position,1.4f,14,.25f,lane);
                yield return null;
            }
            Collide(true);Pose(0);
            if(Interrupted)yield break;
            // A hero who kept running is past the lance, which she wouldn't choose from 8 m either: no combo at nobody, a short breath and she picks again.
            if(Flat(PartyController.Current.Active.transform.position-transform.position).magnitude>=8){Finish(HessaRest*.5f);yield break;}
            yield return HookLance(Phase>=3?4:3);
        }
        static Material chainMaterial;
        Vector3 ChainHand(Vector3 direction)
        {
            var rig=GetComponentInChildren<Animator>();var hand=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.RightHand):null;
            return hand!=null?hand.position:transform.position+Vector3.up*1.3f+direction*.5f;
        }
        /// <summary>The chain itself: a caught hero used to be dragged in by nothing visible.</summary>
        LineRenderer ChainLine(Vector3 direction)
        {
            if(chainMaterial==null){chainMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));chainMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);}
            var go=new GameObject("Grapple chain");temporary.Add(go);
            var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;line.startWidth=.09f;line.endWidth=.13f;
            line.sharedMaterial=chainMaterial;line.startColor=new Color(.8f,.75f,.65f,.9f);line.endColor=new Color(1.3f,.85f,.4f,1);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var hand=ChainHand(direction);line.SetPosition(0,hand);line.SetPosition(1,hand);
            return line;
        }
        /// <summary>A chain that caught nothing still flies the length of its lane, or to the wall, and comes back.</summary>
        IEnumerator ChainFlies(LineRenderer line,Vector3 direction,float length)
        {
            for(float t=0;t<.28f&&line!=null;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                var hand=ChainHand(direction);float reach=length*Mathf.Clamp01(t<.14f?t/.14f:2-t/.14f);
                if(CombatCover.Sweep(hand,hand+direction*Mathf.Max(reach,.01f),.05f,out var wall))reach=wall.distance;
                line.SetPosition(0,hand);line.SetPosition(1,hand+direction*reach);
                yield return null;
            }
            if(line!=null){temporary.Remove(line.gameObject);Destroy(line.gameObject);}
        }
        void CallCrew(int count)
        {
            var definition=GameCatalog.Find<EnemyDef>("MusterScav");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            for(int i=0;i<count&&summoned.Count<3;i++)
            {
                var offset=Quaternion.Euler(0,transform.eulerAngles.y+(i==0?-110:110),0)*Vector3.forward*6f;
                var point=SummonPoint(offset);
                var scav=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(scav);
                CombatVfx.Burst(point+Vector3.up*.8f,new Color(.9f,.35f,.25f),"shot");
            }
            Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
        }
    }
}
