using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>How a shared zone check is split so that one zone's fault is that zone's alone (November 12). A check that walked every zone in one case stopped at the
    /// first zone that couldn't load or that logged an error of its own (an unstaged track, a scene not in the build yet): the test framework fails a case on the
    /// frame an error is logged, so every zone after it went unchecked, and what the zones before it had found was never reported either.
    /// <para>So each check runs one case per zone (<c>[ValueSource]</c>, the zone's id in the case's name, which is what <c>focused.ps1 -Zones</c> picks by), and
    /// what truly compares zones with each other (one material for every sign, no two places with the same air, an exit's arrival in the zone it leads to, a count
    /// over the whole world) is a test of its own, named <c>...AcrossZones</c>. That one reads every zone through <see cref="Walk"/>: what each zone's own case
    /// kept this run (<see cref="Keep"/>), and for the rest a visit of the zone there and then, guarded, so a zone that can't be read is left out by name instead of
    /// ending the walk. An error a zone logs is its own cases' to report, not the comparison's.</para></summary>
    public static class ZoneWalk
    {
        static readonly Dictionary<string,object> kept=new();
        static List<string> heard;
        static void Heard(string text,string stack,LogType type)
        {
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)lock(kept)heard?.Add(text.Split('\n')[0]);
        }

        /// <summary>What one zone's visit for a check found, kept for the run so the check across zones doesn't load the zone again. A visit calls this last, once it has
        /// read the whole zone: a visit that stopped part way keeps nothing, and the walk visits that zone itself.</summary>
        public static void Keep(string check,string zone,object found)=>kept[check+"/"+zone]=found;
        /// <summary>What a zone's visit for a check kept: for that zone's own case, straight after the visit.</summary>
        public static T Kept<T>(string check,string zone)=>kept.TryGetValue(check+"/"+zone,out var found)&&found is T read?read:
            throw new System.InvalidOperationException($"fixture: the visit to {zone} for {check} kept nothing");

        /// <summary>Every zone's findings for a check across zones, into <paramref name="found"/> by zone, in the order given. A zone already read by its own case this run
        /// is taken as it was kept; any other is visited here (<paramref name="visit"/> loads it, reads it and calls <see cref="Keep"/>), one after another as the single
        /// case always walked them. A zone that isn't a scene in the build, or whose visit throws (a load that never ends, an assertion in the visit), goes into
        /// <paramref name="unread"/> with the reason, and <paramref name="recover"/> puts the game back (the class's own Cleanup and Boot, and the check's
        /// preconditions) before the next. Errors a zone logs while it is visited don't fail this test: they fail that zone's own case, and are written to this one's
        /// output.</summary>
        public static IEnumerator Walk<T>(string check,IEnumerable<string> zones,Func<string,IEnumerator> visit,Func<IEnumerator> recover,Dictionary<string,T> found,List<string> unread)
        {
            var logged=new List<string>();
            Application.logMessageReceivedThreaded-=Heard;Application.logMessageReceivedThreaded+=Heard;
            try
            {
                foreach(string zone in zones)
                {
                    if(kept.TryGetValue(check+"/"+zone,out var already)&&already is T earlier){found[zone]=earlier;continue;}
                    if(!Application.CanStreamedLevelBeLoaded(zone)){unread.Add($"{zone} (not a scene in the build yet)");continue;}
                    lock(kept)heard=new List<string>();
                    LogAssert.ignoreFailingMessages=true;
                    Exception threw=null;
                    yield return Guarded(visit(zone),e=>threw=e);
                    // Putting the game back unloads what the zone left behind, and whatever that logs is the zone's too.
                    if(threw!=null&&recover!=null)yield return recover();
                    LogAssert.ignoreFailingMessages=false;
                    lock(kept){foreach(string line in heard.Distinct())logged.Add($"{zone}: {line}");heard=null;}
                    if(threw!=null){unread.Add($"{zone} ({threw.Message.Trim().Split('\n')[0]})");continue;}
                    if(kept.TryGetValue(check+"/"+zone,out var now)&&now is T read)found[zone]=read;
                    else unread.Add($"{zone} (its visit kept nothing)");
                }
            }
            finally
            {
                Application.logMessageReceivedThreaded-=Heard;lock(kept)heard=null;
                if(LogScopeOpen())LogAssert.ignoreFailingMessages=false;
            }
            if(unread.Count>0)TestContext.WriteLine($"{check}: not read, each failing its own case: "+string.Join("; ",unread));
            if(logged.Count>0)TestContext.WriteLine($"{check}: errors the zones logged while visited, each its own case's to report:\n"+string.Join("\n",logged.Distinct()));
        }
        static bool LogScopeOpen(){try{_=LogAssert.ignoreFailingMessages;return true;}catch(InvalidOperationException){return false;}}

        /// <summary>For a message across zones: which zones it couldn't count, or nothing.</summary>
        public static string Unread(List<string> unread)=>unread.Count==0?"":"\n(not read here, each failing its own case: "+string.Join("; ",unread)+")";

        /// <summary>Runs a coroutine as Unity would, nested coroutines and all, and hands over what it throws instead of failing the test with it.</summary>
        static IEnumerator Guarded(IEnumerator run,Action<Exception> threw)
        {
            var stack=new Stack<IEnumerator>();stack.Push(run);
            while(stack.Count>0)
            {
                var top=stack.Peek();bool more;
                try{more=top.MoveNext();}
                catch(Exception e){threw(e);yield break;}
                if(!more){stack.Pop();continue;}
                // A nested coroutine, or a CustomYieldInstruction (WaitForSecondsRealtime), is stepped here a frame at a time; anything else (an AsyncOperation, null) is Unity's to wait on.
                if(top.Current is IEnumerator inner){stack.Push(inner);continue;}
                yield return top.Current;
            }
        }
    }
}
