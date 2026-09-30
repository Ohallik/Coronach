using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace Lattice.Tests.PlayMode
{
    /// <summary>C6 presentation, measured on the real dialogue panel with every line
    /// the game can say: nothing clips, everything is on screen and readable at
    /// 1280×720, 1920×1080 and 1920×1200, and every speaker shows their own face in
    /// the emotion the line asks for (the portrait set otherwise silently falls back).</summary>
    public sealed class DialogueLayoutTests
    {
        struct Spoken{public string where,speaker,text;public PortraitEmotion emotion;}
        sealed class Choice{public string where;public List<string> labels=new();}
        static readonly (int w,int h)[] Screens={(1280,720),(1920,1080),(1920,1200)};

        [UnitySetUp] public IEnumerator Boot()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");}
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        // The scripts are plain "Speaker: text #emotion:X" lines and "-> option"
        // choices; the compiled project's string table proves nothing was skipped.
        static void Script(List<Spoken> lines,List<Choice> choices)
        {
            foreach(string file in Directory.GetFiles(Path.Combine(Application.dataPath,"_Project/Resources/Dialogue"),"*.yarn"))
            {
                string node=null;bool body=false;Choice choice=null;
                foreach(string raw in File.ReadAllLines(file))
                {
                    string s=raw.Trim();
                    if(s.StartsWith("title:")){node=s.Substring(6).Trim();continue;}
                    if(s=="---"){body=true;choice=null;continue;}
                    if(s=="==="){body=false;continue;}
                    if(!body||s.Length==0||s.StartsWith("<<")||s.StartsWith("//"))continue;
                    string where=Path.GetFileNameWithoutExtension(file)+"/"+node;
                    string text=Regex.Replace(s,@"\s#\S+","").Trim();
                    if(s.StartsWith("->"))
                    {
                        if(choice==null){choice=new Choice{where=where};choices.Add(choice);}
                        choice.labels.Add(Regex.Replace(text.Substring(2),@"<<.*?>>","").Trim());continue;
                    }
                    if(!raw.StartsWith(" ")&&!raw.StartsWith("\t"))choice=null;
                    var m=Regex.Match(text,@"^([A-Za-z][\w ]*):\s*(.+)$");
                    Assert.IsTrue(m.Success,$"{where}: a line with no speaker: {s}");
                    var tag=Regex.Match(s,@"#emotion:(\w+)");
                    lines.Add(new Spoken{where=where,speaker=m.Groups[1].Value,text=m.Groups[2].Value,
                        emotion=tag.Success?System.Enum.Parse<PortraitEmotion>(tag.Groups[1].Value,true):PortraitEmotion.Neutral});
                }
            }
            var project=Resources.Load<YarnProject>("Dialogue/Lattice");
            int strings=project.baseLocalization.GetLineIDs().Count();
            Assert.AreEqual(strings,lines.Count+choices.Sum(c=>c.labels.Count),"the script reader and the compiled project disagree on how many lines exist");
        }
        // The zone runtime builds the dialogue panel; the Decks are where most talk happens.
        static IEnumerator Zone()
        {
            SceneFlow.Current.LoadZone("Hub_Decks");float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);yield return new WaitForSecondsRealtime(.3f);
        }
        static DialoguePanel Panel()
        {var panel=Object.FindFirstObjectByType<DialoguePanel>();Assert.IsNotNull(panel,"no dialogue panel");return panel;}
        // The panel shares the runtime object with other UI; look only inside its own canvas.
        static Canvas Screen(DialoguePanel panel)=>Child<Canvas>(panel,"Dialogue");
        static T Child<T>(Component root,string name)where T:Component
        {var found=root.GetComponentsInChildren<T>(true).FirstOrDefault(c=>c.name==name);Assert.IsNotNull(found,"missing "+name);return found;}
        static bool Fits(TMP_Text text,string value)
        {var rect=((RectTransform)text.transform).rect;var size=text.GetPreferredValues(value,rect.width,0);return size.x<=rect.width+.5f&&size.y<=rect.height+.5f;}

        [UnityTest] public IEnumerator EveryLineAndChoiceFitsItsBox()
        {
            yield return Zone();var lines=new List<Spoken>();var choices=new List<Choice>();Script(lines,choices);
            var panel=Panel();panel.Show("Neve",PortraitEmotion.Neutral,null,"x");yield return null;
            var body=Child<TMP_Text>(Screen(panel),"Body");var speaker=Child<TMP_Text>(Screen(panel),"Speaker");
            // The instrument must be able to fail: an overlong line is measured as overlong.
            Assert.IsFalse(Fits(body,string.Join(" ",Enumerable.Repeat("resonance",60))),"the measure passes anything");
            var failures=new List<string>();
            foreach(var line in lines)
            {
                if(!Fits(body,line.text))failures.Add($"{line.where}: {line.speaker}'s line overflows its box: {line.text}");
                if(!Fits(speaker,line.speaker))failures.Add($"{line.where}: speaker name {line.speaker} overflows");
            }
            foreach(var choice in choices)
            {
                panel.Options(choice.labels,_=>{});yield return null;
                for(int i=0;i<choice.labels.Count;i++)
                {
                    var label=Child<TMP_Text>(Child<Transform>(Screen(panel),"Option_"+i),"Label");
                    if(!Fits(label,choice.labels[i]))failures.Add($"{choice.where}: option overflows its button: {choice.labels[i]}");
                }
                panel.ClearOptions();yield return null;
            }
            panel.Hide();
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        [UnityTest] public IEnumerator TheDialogueSitsOnScreenAndReadsAtEveryCheckedResolution()
        {
            yield return Zone();var lines=new List<Spoken>();var choices=new List<Choice>();Script(lines,choices);
            var panel=Panel();var longest=lines.OrderByDescending(l=>l.text.Length).First();
            panel.Show(longest.speaker,PortraitEmotion.Neutral,PortraitLookup.Get("Neve",BodyForm.Natural,PortraitEmotion.Neutral),longest.text);
            panel.SetVisibleCharacters(int.MaxValue);
            var widest=choices.OrderByDescending(c=>c.labels.Count).First();
            var canvas=Screen(panel);var root=(RectTransform)canvas.transform;
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            // Lay the real canvas out at each screen's scaled size, exactly as its
            // scaler would, by giving it that size in world space.
            canvas.renderMode=RenderMode.WorldSpace;root.localScale=Vector3.one;root.position=Vector3.zero;root.rotation=Quaternion.identity;
            var failures=new List<string>();
            Rect Box(RectTransform rt){var c=new Vector3[4];rt.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
            foreach(var (w,h) in Screens)
            {
                float m=scaler.matchWidthOrHeight;var reference=scaler.referenceResolution;
                float scale=Mathf.Pow(2,Mathf.Lerp(Mathf.Log(w/reference.x,2),Mathf.Log(h/reference.y,2),m));
                root.sizeDelta=new Vector2(w/scale,h/scale);
                foreach(int count in new[]{widest.labels.Count,5})
                {
                    panel.Options(count==5?Enumerable.Range(1,5).Select(i=>"Option "+i).ToList():widest.labels,_=>{});
                    Canvas.ForceUpdateCanvases();yield return null;Canvas.ForceUpdateCanvases();
                    var screen=Box(root);var frame=Box((RectTransform)Child<Transform>(canvas,"DialoguePanel"));var face=Box((RectTransform)Child<Transform>(canvas,"Portrait"));
                    var options=Enumerable.Range(0,count).Select(i=>Box((RectTransform)Child<Transform>(canvas,"Option_"+i))).ToList();
                    bool Inside(Rect r)=>r.xMin>=screen.xMin-.5f&&r.xMax<=screen.xMax+.5f&&r.yMin>=screen.yMin-.5f&&r.yMax<=screen.yMax+.5f;
                    var problems=new List<string>();
                    if(!Inside(frame))problems.Add("the panel runs off screen");
                    if(!Inside(face))problems.Add("the portrait runs off screen");
                    for(int i=0;i<options.Count;i++)
                    {
                        if(!Inside(options[i]))problems.Add($"option {i+1} runs off screen");
                        if(options[i].Overlaps(frame)||options[i].Overlaps(face))problems.Add($"option {i+1} covers the panel");
                    }
                    // Five options is beyond any current node: the geometry check must catch it.
                    if(count==5)Assert.IsNotEmpty(problems,$"{w}x{h}: five stacked options should collide with the panel; the overlap check cannot fail");
                    else failures.AddRange(problems.Select(p=>$"{w}x{h} {widest.where}: {p}"));
                    panel.ClearOptions();yield return null;
                }
                // Rendered glyphs stay clear of the frame's border (about 33 units at the
                // sides and 25 at top and bottom), not merely inside the panel.
                var inner=Box((RectTransform)Child<Transform>(canvas,"DialoguePanel"));
                foreach(var text in Child<Transform>(canvas,"DialoguePanel").GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate();var glyphs=text.textBounds;
                    if(text.name=="Body")Assert.Greater(glyphs.size.x,0,"the body's glyph bounds are empty; the border check cannot fail");
                    if(glyphs.size.x<=0)continue;
                    Vector3 lo=text.transform.TransformPoint(glyphs.min),hi=text.transform.TransformPoint(glyphs.max);
                    if(lo.x<inner.xMin+40||hi.x>inner.xMax-40||lo.y<inner.yMin+30||hi.y>inner.yMax-30)
                        failures.Add($"{w}x{h}: {text.name} runs into the frame border ({lo.x-inner.xMin:0} from the left, {inner.xMax-hi.x:0} from the right, {lo.y-inner.yMin:0} from the bottom, {inner.yMax-hi.y:0} from the top)");
                }
                // Rendered type size: primary text at least 18 px, secondary hints 16 px.
                foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    float px=text.fontSize*scale,floor=text.name=="Continue"?16:18;
                    if(px<floor-.01f)failures.Add($"{w}x{h}: {text.name} renders at {px:0.0} px, under {floor} px");
                }
            }
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;panel.Hide();
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }

        [UnityTest] public IEnumerator EverySpeakerShowsTheirOwnFaceInTheEmotionAsked()
        {
            yield return null;var lines=new List<Spoken>();var choices=new List<Choice>();Script(lines,choices);
            var failures=new List<string>();var owners=new Dictionary<PortraitSet,string>();
            IEnumerable<PortraitSet> Sets(string speaker)
            {
                var character=GameCatalog.Find<CharacterDef>(speaker);
                var natural=character!=null&&character.portraitNatural!=null?character.portraitNatural:Resources.Load<PortraitSet>("Portraits/"+speaker+"_Natural");
                yield return natural;
                // Heroes speak in both bodies, so both sets must answer every emotion.
                if(character!=null&&character.portraitShaped!=null)yield return character.portraitShaped;
            }
            Assert.IsNull(Sets("NobodyAtAll").Single(),"a speaker with no portraits must be found missing");
            foreach(var group in lines.GroupBy(l=>l.speaker))
                foreach(var set in Sets(group.Key))
                {
                    if(set==null){failures.Add($"{group.Key} has no portrait set");continue;}
                    if(owners.TryGetValue(set,out var other)&&other!=group.Key)failures.Add($"{group.Key} speaks with {other}'s face ({set.name})");
                    owners[set]=group.Key;
                    foreach(var line in group.GroupBy(l=>l.emotion).Select(e=>e.First()))
                        if(!set.entries.Any(e=>e.emotion==line.emotion&&e.sprite!=null))failures.Add($"{line.where}: {set.name} has no {line.emotion}, so the line shows another expression");
                }
            Assert.IsEmpty(failures,string.Join("\n",failures.Distinct()));
        }
    }
}
