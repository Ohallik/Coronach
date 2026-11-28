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
    /// zone's bed RMS -40 to -36 since round eleven (-32 to -28 in round ten masked the music), the fallback beds -46. AudioMixTests holds them.</summary>
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
            // Round eleven's three (Kenney's Sci-Fi Sounds, staged at the reference): the fabrication bench opening and closing, at a menu's
            // confirm; a warp's crossing, heard through the load, at a world cue's level.
            new("doorOpen_001",Kind.UiConfirm,.3f,-12,ReferencePeakDb),new("doorClose_001",Kind.UiConfirm,.3f,-12,ReferencePeakDb),
            new("forceField_003",Kind.Cue,.3f,-10,ReferencePeakDb),
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
        /// <summary>The bed a place plays when its ZoneDef names none, and its level. Before round ten this was Kenney's computerNoise_000,
        /// five seconds of beeps on a loop, and spaceEngineLow_000, at .055. Round ten made them shaped noise at about -30 dBFS RMS a channel,
        /// with a duct's hiss and a faint air in them; Nathan heard "constant background kind of air noise" that made the music seem muffled.
        /// Round eleven (tools/room_tone_ambience.py): a hull's low hum and a far drive, nothing over about 800 Hz, at about -46 dBFS RMS a
        /// channel (the files at -20.4 and -18.4): sixteen decibels under round ten, felt more than heard.</summary>
        public const string RoomTone="AMB_RoomTone",FlightDrone="AMB_FlightDrone";
        public const float OldHumVolume=.055f;
        public static float RoomToneVolume=>BedsAsRoundTen?.33f:.053f;
        public static float FlightDroneVolume=>BedsAsRoundTen?.29f:.042f;
        /// <summary>The fallback beds' level, RMS a channel at the ears, and the frequency nothing of theirs reaches over.</summary>
        public const float FallbackBedDb=-46,FallbackTopHz=800;
        /// <summary>A zone's bed, by its RMS a channel before its own cut (<see cref="BedCutDb"/>): round ten put them at -32 to -28, six
        /// decibels under the music, and they masked it; round eleven brings every one down eight, to -40 to -36.</summary>
        public const float BedLowDb=-40,BedHighDb=-36;
        /// <summary>Round eleven's one trim for every bed AudioManager plays and every bed a builder placed in a scene (the Many Hands
        /// forge's hammering and its herd): eight decibels down. Turn the whole of the background here.</summary>
        public const float BedTrimDb=-8;
        /// <summary>The music's presence, where a bed must not cover it: 1 to 6 kHz. The music's own 1-6 kHz part reaches the ears at
        /// -36.9 to -30.3 dBFS RMS a channel (27 tracks, median -32.5). No bed puts more than <see cref="BedPresenceShare"/> of its energy
        /// there at more than <see cref="BedPresenceDb"/> (11 dB under the quietest track's, 15 under the median), and no bed peaks at the
        /// ears over <see cref="BedPeakDb"/>, the quietest thing the player does (a step, -20 to -16).</summary>
        public const float PresenceLowHz=1000,PresenceHighHz=6000,BedPresenceShare=.1f,BedPresenceDb=-48,BedPeakDb=-20;
        /// <summary>The few beds whose energy is in the music's band (AudioMixTests' sheet; the round's sheet in its decision), cut further
        /// than the rest, in dB: Hushwell's drips put 40% of their energy at 1-6 kHz and peaked at -1.3 dBFS at the ears, over every effect;
        /// the Oriel skyfields' 16%, the Continuance's 21%, the Frozen Host's 17% and the Braided River's 14% stood at -36.9 to -38.7 there.
        /// The Many Hands forge's hammering (placed at the anvil, 45% at 1-6 kHz) comes down with them. The Spore Veil's, the Glass Choir's
        /// and the Separation Works' tones are narrow and under 1% of their energy at 1-6 kHz (-56 to -73 there after the trim): no cut.</summary>
        static readonly Dictionary<string,float> BedCutsDb=new()
        {{"AMB_Drips",-11.5f},{"AMB_OrielSkyfields",-4f},{"AMB_Continuance",-3.5f},{"AMB_FrozenHost",-3f},{"AMB_BraidedRiver",-2f},{"AMB_HandsForge",-6.5f}};
        public static float BedCutDb(string bed)=>!string.IsNullOrEmpty(bed)&&BedCutsDb.TryGetValue(bed,out float cut)?cut:0;
        /// <summary>The beds as round ten left them (this morning's levels, before the trim and the cuts), for the tests that show them failing.
        /// The fallback beds' files are round eleven's either way: round ten's are kept outside the tree.</summary>
        public static bool BedsAsRoundTen;
        /// <summary>For a test's before only: the three places round ten left silent (the bench opening and closing, a warp, a boss walking
        /// out) play nothing, as they did.</summary>
        public static bool CuesAsRoundTen;
        static float Round11(string bed)=>BedsAsRoundTen?1:Mathf.Pow(10,(BedTrimDb+BedCutDb(bed))/20);
        /// <summary>A source a builder placed in a scene on the ambience bus (a looping bed at a spot), at the volume its builder gave it:
        /// AudioManager trims it as it trims a zone's bed, when the zone starts, with no rebuild.</summary>
        public static float PlacedVolume(AudioClip clip,float made)=>AsRoundNine?made:Mathf.Clamp01(made*Round11(clip!=null?clip.name:null));
        /// <summary>Thirty-nine of the 45 places' beds reached the listener at -31.6 to -28.9 dBFS RMS a channel (each set in its builder at
        /// about -33 for the clip mixed to one channel); six sat under the band, and their ZoneDefs are chapter 1's and 2's, built by builders
        /// nobody reruns. Each plays at its ZoneDef's volume times this: the Braided River's water -34.2 and the Walking Canopy's root hall
        /// -34.5 (+4.2 and +4.5 dB), Sorrel's desert wind -33.1, the gorge's ash wind -32.4 and Lanternwick's town -32.3, and Hushwell's
        /// drips -33.4 at .9, lengthened and raised 4 dB in the file by tools/drip_bed.py, with no trim.</summary>
        static readonly Dictionary<string,float> BedTrims=new()
        {{"AMB_BraidedRiver",1.62f},{"AMB_RootHall",1.68f},{"AMB_DesertWind",1.43f},{"AMB_AshWind",1.32f},{"AMB_City",1.3f}};
        public static float BedVolume(AudioClip bed,float zoneVolume)
        {
            if(AsRoundNine||bed==null)return Mathf.Clamp01(zoneVolume);
            float raised=BedTrims.TryGetValue(bed.name,out float trim)?zoneVolume*trim:zoneVolume;
            return Mathf.Clamp01(raised*Round11(bed.name));
        }
        /// <summary>The music's RMS a channel on disk, the median of its 27 tracks (-16.8 to -21.3; streamed, so measured by
        /// `tools/audio_reference.py --music`, not read back here), and its band at the listener. The brief's -24 to -21 (about -22) was read
        /// through a downmix to one channel, 2.2 dB hot on this music: a channel of it reaches the listener at -24.3, and that is left as
        /// Nathan made it, with a hit some 16 dB over it.</summary>
        public const float MusicFileRmsDb=-18.6f,MusicLowDb=-26,MusicHighDb=-23;
    }
}
