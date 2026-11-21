using System;
using System.Collections.Generic;
using Lattice.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The difficulty's row in the pause menu's Settings and the one question a new game asks, in one place so both say the same three names in the same words. The setting
    /// and its two numbers are <see cref="Difficulty"/>'s; it is kept in the game being played, so there is nothing here to load or save. Nothing here judges a choice: each is
    /// described by what it does to an enemy's numbers, and none of them puts a mark on the HUD.</summary>
    public static class DifficultyUi
    {
        /// <summary>As a list reads them, lightest first. The save's own order starts at Standard (DifficultySetting), which is nobody's to read.</summary>
        public static readonly DifficultySetting[] Order={DifficultySetting.Gentle,DifficultySetting.Standard,DifficultySetting.Demanding};
        /// <summary>The pause row's name: its own, not a number, so the rows under it keep the names they are focused and found by (PauseMenu's Extra).</summary>
        public const string RowName="Difficulty";
        /// <summary>What the pause row says now: the setting and what it does, in the row itself, since the page's note belongs to the sound sliders.</summary>
        public static string Row()=>"Difficulty  ·  "+Says(Difficulty.Current);
        static string Says(DifficultySetting setting)=>Difficulty.Name(setting)+"  ·  "+Difficulty.Says(setting);
        /// <summary>A press lands on the next and the last comes round to the first, as the rows beside it do. The next hit follows it; integrity is taken as an enemy is made, so
        /// whoever is awake keeps theirs and no bar jumps, and a boss still on show before its fight is made again at the new setting (Spawner). It rides in the game from here:
        /// the next save writes it, and a Retry keeps it (DefeatPanel).</summary>
        public static void Next()
        {
            Difficulty.Current=Order[(Array.IndexOf(Order,Difficulty.Current)+1)%Order.Length];
            Debug.Log("DIFFICULTY "+Difficulty.Name(Difficulty.Current));
        }

        // ---------- the question at New Game ----------
        /// <summary>Arguments that put a script at the controls: the replay driver, the smoke runs and the probes. Each of them presses through the title by a fixed count of
        /// buttons, or skips it, and a question in the way would send every one of them somewhere else.</summary>
        static readonly string[] Scripted={"-quality-route","-route","-smoketest","-review","-uismoke","-portraitsmoke","-loadout","-scene","-spawn","-perf","-balance","-look","-creature-defeat","-audio-mix-probe"};
        static bool? asks;
        /// <summary>Whether New Game stops to ask. It does for a person starting the game in a player, and for nobody else: not under the replay driver or any scripted run, not in
        /// batch mode, and not in the editor, where the suites press New Game and expect the game (as SaveSystem.WriteBehind is a player's unless a check turns it on).
        /// Unasked, a new game is Standard, which is what every route and test was written against.</summary>
        public static bool AsksAtNewGame{get=>asks??=!Application.isEditor&&!Application.isBatchMode&&!Array.Exists(Scripted,DevArgs.Has);set=>asks=value;}
        public const string Question="CHOOSE A DIFFICULTY";
        public const string Explains="This sets how much damage enemies deal and how much they can take, and nothing else. You can change it at any time: pause, then Settings.";
        /// <summary>The question, over the title: the three as rows, the cursor on Standard so a press straight through plays the game as it was built, and a way back to the menu.
        /// The panel is the caller's to close.</summary>
        public static GameObject Ask(Transform canvas,Action<DifficultySetting> chosen,Action back)
        {
            var panel=UiKit.Dim(canvas,.96f).gameObject;
            var heading=UiKit.Heading(panel.transform,"DifficultyTitle",Question,45,UiKit.TextColor);UiKit.Rect(heading.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,260),new(1300,80));
            var body=UiKit.Text(panel.transform,"DifficultyBody",Explains,30,UiKit.TextColor);UiKit.Rect(body.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,160),new(1300,100));
            var rows=new List<Selectable>();Selectable standard=null;
            for(int i=0;i<Order.Length;i++)
            {
                var setting=Order[i];
                var row=UiKit.Button(panel.transform,"Difficulty_"+Difficulty.Name(setting),Says(setting),()=>{Debug.Log("DIFFICULTY_CHOSEN "+Difficulty.Name(setting));chosen(setting);});
                // With what it does beside its name a row runs past its 1200 at the button's 34, as a save's row does with its date.
                row.GetComponentInChildren<TMP_Text>().fontSize=30;
                UiKit.Rect(row.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,40-i*90),new(1200,72));rows.Add(row);
                if(setting==DifficultySetting.Standard)standard=row;
            }
            var none=UiKit.Button(panel.transform,"DifficultyBack","Back",back);UiKit.Rect(none.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,-250),new(400,65));rows.Add(none);
            UiKit.LinkVertical(rows.ToArray());EventSystem.current.SetSelectedGameObject(standard.gameObject);
            return panel;
        }
    }
}
