using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    public sealed class RuntimeStrideTests
    {
        sealed class Travel : IMotor
        {
            public Vector3 velocity;
            public Vector3 Facing=>Vector3.forward;
            public Vector3 Velocity=>velocity;
            public void Move(Vector2 input,bool boost,bool brake){}
            public void Dash(Vector3 direction,float distance){}
        }
        // Independent rendered-mesh probe. It never reads GroundFeet's contact
        // anchor or the profile's marker offsets. BakeMesh also works without
        // retaining every imported mesh's CPU vertex buffer in the player.
        sealed class SoleProbe
        {
            sealed class Part
            {
                public SkinnedMeshRenderer renderer;
                public Mesh baked=new Mesh();
                public readonly List<Vector3> vertices=new();
                public int[] indices;
            }
            readonly List<Part> parts=new();
            public SoleProbe(Animator animator,bool left)
            {
                var foot=animator.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                var other=animator.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};renderer.BakeMesh(part.baked);parts.Add(part);
                    part.baked.GetVertices(part.vertices);var vertices=part.vertices;
                    for(int i=0;i<vertices.Count;i++)
                    {
                        Vector3 point=renderer.transform.TransformPoint(vertices[i]);
                        if(Vector3.Distance(point,foot.position)<.48f&&Vector3.Dot(point-(foot.position+other.position)*.5f,foot.position-other.position)>0)
                            candidates.Add((part,i,point));
                    }
                }
                Assert.Greater(candidates.Count,0,"visible foot region missing");
                float bottom=candidates.Min(c=>c.point.y);
                var sole=candidates.Where(c=>c.point.y<bottom+.025f).ToArray();
                float back=sole.Min(c=>c.point.z),front=sole.Max(c=>c.point.z);
                Assert.Greater(front-back,.08f,"bind-pose foot must have a visible sole");
                foreach(var part in parts)part.indices=sole.Where(c=>c.part==part&&c.point.z>front-(front-back)*.25f).Select(c=>c.index).ToArray();
            }
            public Vector3 Point()
            {
                Vector3 sum=Vector3.zero;int count=0;
                foreach(var part in parts)
                {
                    if(part.indices.Length==0)continue;
                    part.renderer.BakeMesh(part.baked);part.baked.GetVertices(part.vertices);var vertices=part.vertices;
                    foreach(int i in part.indices){sum+=part.renderer.transform.TransformPoint(vertices[i]);count++;}
                }
                Assert.Greater(count,0);return sum/count;
            }
            public void Dispose(){foreach(var part in parts)Object.Destroy(part.baked);}
        }
        [UnityTest] public IEnumerator VisibleSolesHoldDuringRuntimeLocomotionAndPause()
        {
            GameTime.Reset();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(100,-.5f,100);floor.transform.localScale=new Vector3(400,1,400);
            Physics.SyncTransforms();
            try
            {
                foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
                foreach(float direction in new[]{0f,90,180})
                {
                    var root=new GameObject("runtime stride "+hero,typeof(Health),typeof(CombatActor));root.transform.position=new Vector3(100,0,100);
                    var actor=root.GetComponent<CombatActor>();actor.character=hero;
                    var travel=Quaternion.Euler(0,direction,0)*Vector3.forward;float speed=shaped?6.7f:2.6f;
                    actor.motor=new Travel{velocity=travel*speed};
                    var definition=GameCatalog.Find<CharacterDef>(hero);
                    var body=Object.Instantiate(shaped?definition.shaped:definition.natural,root.transform);
                    var animator=body.GetComponentInChildren<Animator>();var driver=body.GetComponent<GeneratedAnimator>();
                    var left=new SoleProbe(animator,true);var right=new SoleProbe(animator,false);
                    try
                    {
                        var points=new[]{new List<Vector3>(),new List<Vector3>()};int contacts=0;float drift=0,penetration=0;
                        float started=Time.unscaledTime;
                        while(Time.unscaledTime-started<3.5f)
                        {
                            if(Time.unscaledTime-started>.6f&&!animator.IsInTransition(0))
                            {
                                float phase=Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                                for(int side=0;side<2;side++)
                                {
                                    float p=Mathf.Repeat(phase-side*.5f,1);bool stance=shaped?p>=.08f&&p<=.16f:p>=.2f&&p<=.4f;
                                    var list=points[side];var point=side==0?left.Point():right.Point();
                                    if(stance){list.Add(point);penetration=Mathf.Max(penetration,-point.y);}
                                    else if(list.Count>0)
                                    {
                                        foreach(var a in list)foreach(var b in list)drift=Mathf.Max(drift,new Vector2(a.x-b.x,a.z-b.z).magnitude);
                                        if(list.Count>=2)contacts++;list.Clear();
                                    }
                                }
                            }
                            root.transform.position+=travel*speed*Time.unscaledDeltaTime;
                            yield return null;
                        }
                        string context=hero+(shaped?" Shaped":" Natural")+" direction "+direction;
                        Assert.GreaterOrEqual(contacts,4,context+" must include repeated actual stance intervals");
                        Assert.LessOrEqual(drift,.05f,context+" rendered planted sole drift");
                        Assert.LessOrEqual(penetration,.03f,context+" rendered sole penetration");
                        Debug.Log($"RUNTIME_STRIDE_CASE {context} contacts={contacts} drift={drift:F6} penetration={penetration:F6}");
                        GameTime.Paused=true;yield return null;
                        Vector3 beforeLeft=left.Point(),beforeRight=right.Point();float beforePhase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                        yield return new WaitForSecondsRealtime(.2f);
                        Assert.Less(Vector3.Distance(beforeLeft,left.Point()),.003f,context+" left foot moves during pause");
                        Assert.Less(Vector3.Distance(beforeRight,right.Point()),.003f,context+" right foot moves during pause");
                        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(beforePhase).Within(.001f));
                        GameTime.Paused=false;
                    }
                    finally{GameTime.Reset();left.Dispose();right.Dispose();Object.Destroy(root);}
                    yield return null;
                }
            }
            finally{GameTime.Reset();Object.Destroy(floor);}
        }
    }
}
