using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Core
{
    /// <summary>The game's mix in one place (round ten, November 27). Nathan heard captured play and no sound effects: every one-shot was
    /// mixed some 17 dB under the music (the palette made at -10 dBFS peak and played at .16 to .26, through a listener on the lens 17 to 45 m
    /// off). Every one-shot clip under Resources/Audio/Palette and Resources/Audio/SFX now sits at one reference on disk
    /// (<see cref="ReferencePeakDb"/>, <c>tools/audio_reference.py</c>), and this table says, for each family of clip, what peak a call at
    /// that family's usual volume (its nominal) reaches the listener at with every slider at 1. The volume a call site passes is a trim
    /// against that nominal (a .24 armoured hit is .8 dB over a .22 soft one), so the whole mix is changed here. Bands, by kind, at the
    /// listener, a channel: an impact that tells the player something -9 to -5 dBFS peak, a motion -14 to -10, a step -20 to -16, a menu's move about
    /// -16 and its confirm, cancel and error about -12, a boss's or the world's cue by what it is (-16 to -6); the music's RMS -26 to -23; a
    /// zone's bed RMS -32 to -28. AudioMixTests holds them.</summary>
    public static class CueLevels
    {
        public enum Kind{Impact,Motion,Step,UiMove,UiConfirm,Cue}
        public readonly struct Family
        {
            public readonly string name;public readonly Kind kind;
            /// <summary>The call volume the family is usually played at, and the peak (dBFS at the listener) such a call reaches.</summary>
            public readonly float nominal,peakDb;
            /// <summary>The clip's peak on disk before round ten, for <see cref="AsRoundNine"/> and the loops kept at their old level.</summary>
            public readonly float oldPeakDb;
            public Family(string name,Kind kind,float nominal,float peakDb,float oldPeakDb){this.name=name;this.kind=kind;this.nominal=nominal;this.peakDb=peakDb;this.oldPeakDb=oldPeakDb;}
            public float Gain=>Mathf.Pow(10,(peakDb-ReferencePeakDb-PanLawDb)/20)/nominal;
        }
        /// <summary>Every one-shot clip's sample peak on disk; every one-shot is mono.</summary>
        public const float ReferencePeakDb=-1;
        /// <summary>What Unity gives each channel of a mono voice at the centre, flat or spatial: read from the listener's own output in a
        /// test mirror (AudioMixTests.TheEngineSumsWhatTheSheetReckons: a hit reckoned at -25.1 without it was heard at -28.1).</summary>
        public const float PanLawDb=-3.01f;
        /// <summary>No single cue reaches the listener over this peak, whatever its call asks: four of them over the music still leave the master its headroom.</summary>
        public const float CeilingPeakDb=-4;
        /// <summary>The state before round ten, for the tests that show it failing: the old files' levels, gain 1, the listener on the lens and the beeping room hum.
        /// laserSmall_000 was a stereo file at -5.65 a channel, heard without the mono pan law: as heard, a mono file at -2.65.</summary>
        public static bool AsRoundNine;
        const float Palette=-10;
        public static readonly Family[] Families=
        {
            // What a blow did: the palette's impacts (CombatAudio.Impact, Impact.cs's accents) and the bodies that fall.
            new("hit_soft",Kind.Impact,.22f,-8,Palette),new("hit_armor",Kind.Impact,.22f,-8,Palette),new("thump",Kind.Impact,.2f,-8,Palette),
            new("crit",Kind.Impact,.18f,-8,Palette),new("break",Kind.Impact,.24f,-7,Palette),new("kill",Kind.Impact,.2f,-7,Palette),
            new("guard_block",Kind.Impact,.2f,-8,Palette),new("hull_hit",Kind.Impact,.22f,-8,Palette),new("deflect",Kind.Impact,.26f,-7,Palette),
            new("death_creature",Kind.Impact,.24f,-8,Palette),new("death_mech",Kind.Impact,.26f,-8,Palette),new("hero_down",Kind.Impact,.3f,-7,Palette),
            new("boss_down",Kind.Impact,.3f,-6,Palette),
            // A body moving: swings, shots, dodges, the flash move and the tag-in.
            new("swing",Kind.Motion,.2f,-12,Palette),new("needle_taren",Kind.Motion,.18f,-12,Palette),new("needle_sela",Kind.Motion,.18f,-12,Palette),
            new("dodge",Kind.Motion,.18f,-12,Palette),new("flash_move",Kind.Motion,.16f,-12,Palette),new("swap_strike",Kind.Motion,.2f,-11,Palette),
            new("laserSmall_000",Kind.Motion,.16f,-13,-2.65f),new("thrusterFire_000",Kind.Motion,.16f,-12,-.8f),
            // Feet: GroundFeet's gain runs .15 to .22 with the pace.
            new("step_metal",Kind.Step,.18f,-18,Palette),new("step_rock",Kind.Step,.18f,-18,Palette),new("step_soil",Kind.Step,.18f,-18,Palette),
            // Menus (UiSounds) and the dialogue's advance.
            new("ui_move",Kind.UiMove,.28f,-16,Palette),new("dialogue_tick",Kind.UiMove,.3f,-16,Palette),
            new("ui_confirm",Kind.UiConfirm,.38f,-12,Palette),new("ui_cancel",Kind.UiConfirm,.38f,-12,Palette),new("ui_error",Kind.UiConfirm,.4f,-12,Palette),
            // Round ten's two new ones (tools/prepare_audio_palette.py --set world): something taken into the bag, and a save by hand.
            new("pickup",Kind.UiMove,.3f,-16,ReferencePeakDb),new("saved",Kind.UiConfirm,.3f,-12,ReferencePeakDb),
            // The rest: a boss's own cues, the world's machines, a skill's body, a level gained. Each of these clips is played by many
            // callers at their own trims (forceField_000 by ninety); the nominal is the commonest of them.
            new("hero_revive",Kind.Cue,.26f,-9,Palette),new("level_up",Kind.Cue,.22f,-9,Palette),
            new("forceField_000",Kind.Cue,.2f,-10,-.9f),new("explosionCrunch_000",Kind.Cue,.2f,-9,-1),new("impactMetal_000",Kind.Cue,.2f,-9,-.9f),
            new("doorOpen_000",Kind.Cue,.3f,-11,-.9f),new("spaceEngineLow_000",Kind.Cue,.3f,-12,-1),new("computerNoise_000",Kind.Cue,.3f,-14,-1),
            new("water_rush",Kind.Cue,.3f,-12,-3.1f),new("water_splash",Kind.Cue,.3f,-11,-1.6f),new("root_groan",Kind.Cue,.3f,-11,-1.9f),
            new("joint_hiss",Kind.Cue,.14f,-15,-12.1f),new("joint_release",Kind.Cue,.4f,-10,-6),new("district_lift",Kind.Cue,.3f,-14,-8),
            new("school_bell",Kind.Cue,.3f,-16,-14),
        };
        static Dictionary<string,Family> byName;
        static Dictionary<string,Family> ByName
        {get{if(byName==null){byName=new();foreach(var family in Families)byName[family.name]=family;}return byName;}}
        /// <summary>A clip's family: its own name, or its name without a variation's _0 to _9.</summary>
        public static bool TryFamily(string key,out Family family)
        {
            family=default;if(string.IsNullOrEmpty(key))return false;
            if(ByName.TryGetValue(key,out family))return true;
            int cut=key.Length-2;return cut>0&&key[cut]=='_'&&char.IsDigit(key[cut+1])&&ByName.TryGetValue(key.Substring(0,cut),out family);
        }
        public static float Ceiling=>Mathf.Min(1,Mathf.Pow(10,(CeilingPeakDb-ReferencePeakDb-PanLawDb)/20));
        /// <summary>The source volume a one-shot call plays at. A clip with no family plays at its call's volume, as before; the tests reject one.</summary>
        public static float Volume(string key,float call)
        {
            if(!TryFamily(key,out var family))return Mathf.Clamp01(call);
            if(AsRoundNine)return Mathf.Clamp01(call*Mathf.Pow(10,(family.oldPeakDb-ReferencePeakDb)/20));
            return Mathf.Clamp(call*family.Gain,0,Ceiling);
        }
        /// <summary>The call volume that a one-shot's source volume was played for (below the ceiling): what the tests that compare callers' trims read.</summary>
        public static float Trim(string key,float sourceVolume)
        {
            if(!TryFamily(key,out var family))return sourceVolume;
            return sourceVolume/(AsRoundNine?Mathf.Pow(10,(family.oldPeakDb-ReferencePeakDb)/20):family.Gain);
        }
        /// <summary>A loop (an engine under a lift, water rising, a chain running) plays at its caller's level against the clip as it was before
        /// round ten: the loops already sat where a player hears the machine under the fight, and only the files moved to the reference.</summary>
        public static float LoopVolume(string key,float call)
        {
            if(!TryFamily(key,out var family))return Mathf.Clamp01(call);
            return Mathf.Clamp01(call*Mathf.Pow(10,(family.oldPeakDb-ReferencePeakDb)/20));
        }
        /// <summary>The bed a place plays when its ZoneDef names none, and its level: shaped noise with nothing pitched or repeating in it
        /// (tools/room_tone_ambience.py), at about -30 dBFS RMS a channel. Before round ten this was Kenney's computerNoise_000, five seconds
        /// of beeps on a loop, and spaceEngineLow_000, at .055.</summary>
        public const string RoomTone="AMB_RoomTone",FlightDrone="AMB_FlightDrone";
        public const float RoomToneVolume=.33f,FlightDroneVolume=.29f,OldHumVolume=.055f;
        /// <summary>A zone's bed, by its RMS a channel: under the music by six to eight dB and over nothing else.</summary>
        public const float BedLowDb=-32,BedHighDb=-28;
        /// <summary>Thirty-nine of the 45 places' beds reached the listener at -31.6 to -28.9 dBFS RMS a channel (each set in its builder at
        /// about -33 for the clip mixed to one channel); six sat under the band, and their ZoneDefs are chapter 1's and 2's, built by builders
        /// nobody reruns. Each plays at its ZoneDef's volume times this: the Braided River's water -34.2 and the Walking Canopy's root hall
        /// -34.5 (+4.2 and +4.5 dB), Sorrel's desert wind -33.1, the gorge's ash wind -32.4 and Lanternwick's town -32.3, and Hushwell's
        /// drips -33.4 at .9, lengthened and raised 4 dB in the file by tools/drip_bed.py, with no trim.</summary>
        static readonly Dictionary<string,float> BedTrims=new()
        {{"AMB_BraidedRiver",1.62f},{"AMB_RootHall",1.68f},{"AMB_DesertWind",1.43f},{"AMB_AshWind",1.32f},{"AMB_City",1.3f}};
        public static float BedVolume(AudioClip bed,float zoneVolume)=>
            AsRoundNine||bed==null||!BedTrims.TryGetValue(bed.name,out float trim)?Mathf.Clamp01(zoneVolume):Mathf.Clamp01(zoneVolume*trim);
        /// <summary>The music's RMS a channel on disk, the median of its 27 tracks (-16.8 to -21.3; streamed, so measured by
        /// `tools/audio_reference.py --music`, not read back here), and its band at the listener. The brief's -24 to -21 (about -22) was read
        /// through a downmix to one channel, 2.2 dB hot on this music: a channel of it reaches the listener at -24.3, and that is left as
        /// Nathan made it, with a hit some 16 dB over it.</summary>
        public const float MusicFileRmsDb=-18.6f,MusicLowDb=-26,MusicHighDb=-23;
    }
}
