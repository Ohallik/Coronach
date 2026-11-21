using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    /// <summary>
    /// Many Hands' city (ManyHandsBuilder.Hands) and what arrives in it after chapter 5 (Hands.Arrivals) must not need each other while their statics are set up.
    /// Each later chapter's staging reads the arrivals' places before anything else of the city; when Arrivals' setup read the city's and the city's took in the
    /// arrivals' views, whichever was read second saw the other half-made: reading Arrivals first threw, reading Hands first read a place as zero.
    /// Both are editor code (Lattice.Editor, which this assembly doesn't reference), so they are reached by name.
    /// </summary>
    public class ManyHandsArrivalsInitTests
    {
        const BindingFlags Statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        static Type Find(string name)=>Type.GetType(name+", Lattice.Editor")??throw new InvalidOperationException("fixture: no "+name+" in Lattice.Editor");
        static object Read(Type type,string member)
        {
            var field=type.GetField(member,Statics);if(field!=null)return field.GetValue(null);
            var property=type.GetProperty(member,Statics);if(property!=null)return property.GetValue(null);
            throw new InvalidOperationException($"fixture: {type.Name} has no static {member}");
        }
        static IEnumerable<string> Names(Array views)=>views.Cast<object>().Select(v=>(string)v.GetType().GetField("name").GetValue(v));

        /// <summary>The members of <paramref name="of"/> a type initialiser reads or calls: each field, method or constructor token in its IL that resolves to one.</summary>
        static string[] Reads(ConstructorInfo init,Type of)
        {
            var il=init?.GetMethodBody()?.GetILAsByteArray();if(il==null)return new string[0];
            var found=new List<string>();
            for(int i=0;i+4<il.Length;i++)
            {
                byte op=il[i];
                // ldsfld, ldsflda, stsfld, call, callvirt, newobj: each followed by a four-byte metadata token.
                if(op!=0x7E&&op!=0x7F&&op!=0x80&&op!=0x28&&op!=0x6F&&op!=0x73)continue;
                int token=BitConverter.ToInt32(il,i+1);int table=(int)((uint)token>>24);
                if(table!=0x04&&table!=0x06&&table!=0x0A&&table!=0x2B)continue;
                MemberInfo member;try{member=init.Module.ResolveMember(token);}catch(Exception){continue;}
                if(member!=null&&member.DeclaringType==of)found.Add(member.Name);
            }
            return found.Distinct().ToArray();
        }

        [Test] public void TheArrivalsCanBeReadBeforeTheCityAndNeitherNeedsTheOtherWhileSettingUp()
        {
            var city=Find("Lattice.EditorTools.ManyHandsBuilder+Hands");var arrivals=Find("Lattice.EditorTools.ManyHandsBuilder+Hands+Arrivals");
            // The arrivals first, as a chapter's staging reads them: a place, then their views.
            Assert.AreNotEqual(Vector2.zero,(Vector2)Read(arrivals,"PadCutter"),"Idra's cutter's place read as zero: Arrivals was set up while Hands was half-made");
            Assert.AreNotEqual(Vector2.zero,(Vector2)Read(arrivals,"TovraAt"),"Tovra's place read as zero");
            var theirs=(Array)Read(arrivals,"Views");Assert.IsNotNull(theirs,"Arrivals.Views");Assert.Greater(theirs.Length,0,"Arrivals.Views is empty");
            // Then the city, whose views take the arrivals' in after its own.
            var all=(Array)Read(city,"Views");Assert.IsNotNull(all,"Hands.Views");
            var names=Names(all).ToList();
            Assert.Contains("arrival",names,"the city's own views aren't in Hands.Views");
            foreach(var name in Names(theirs))Assert.Contains(name,names,$"the arrivals' view {name} isn't in Hands.Views, so LookDev never renders it");

            // Whatever this domain happened to read first, neither type's setup reads the other: that is what makes the order not matter.
            Assert.IsEmpty(Reads(city.TypeInitializer,arrivals),"Hands' static setup reads Arrivals (make it a lazy read, as Hands.Views is)");
            Assert.IsEmpty(Reads(arrivals.TypeInitializer,city),"Arrivals' static setup reads Hands (literals only there, or a lazy read)");
            // And the check can fail: LookDev's setup does read the city's views.
            Assert.IsNotEmpty(Reads(Find("Lattice.EditorTools.LookDev").TypeInitializer,city),"fixture: the setup check finds nothing even where a read is known (LookDev's table reads Hands.Views)");
        }
    }
}
