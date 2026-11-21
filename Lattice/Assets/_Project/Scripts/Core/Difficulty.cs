namespace Lattice.Core
{
    /// <summary>How hard enemies hit and how much they take. Saved in the game as its number, so the order is append-only and Standard stays 0: a save from before the setting
    /// has no value, and reads as the game it was played as.</summary>
    public enum DifficultySetting{Standard,Gentle,Demanding}
    /// <summary>The one setting about the fights, and the two numbers it changes: what an enemy's or a boss's hit takes off a hero, and the integrity they start with. Nothing else
    /// reads it: tells, windows, XP and loot are the same on all three. It lives in the game being played (<see cref="GameState.difficulty"/>) and nowhere else, on purpose:
    /// nothing static and nothing in the player's preferences holds it, so a new game, a fixture's state and a replay all start at Standard whatever was last chosen on this machine.
    /// Standard multiplies nothing: its numbers are the definitions' own, to the bit.</summary>
    public static class Difficulty
    {
        /// <summary>The setting was changed in the game being played. For what was made before its fight and is still waiting for it (a Spawner's bodies on show): nothing that
        /// is awake listens, because a living bar never moves. A game loaded or begun is not a change: its zone makes everything anew.</summary>
        public static event System.Action Changed;
        /// <summary>The setting of the game being played; Standard when there is none.</summary>
        public static DifficultySetting Current
        {
            get{var services=GameServices.Current;return services!=null&&services.State!=null?services.State.difficulty:DifficultySetting.Standard;}
            set{var services=GameServices.Current;if(services==null||services.State==null||services.State.difficulty==value)return;services.State.difficulty=value;Changed?.Invoke();}
        }
        /// <summary>A value this build doesn't know (a later build's save) plays as Standard.</summary>
        public static float DamageOf(DifficultySetting setting)=>setting switch{DifficultySetting.Gentle=>.6f,DifficultySetting.Demanding=>1.3f,_=>1};
        public static float IntegrityOf(DifficultySetting setting)=>setting switch{DifficultySetting.Gentle=>.8f,DifficultySetting.Demanding=>1.15f,_=>1};
        /// <summary>What a hit an enemy lands on the party comes to. Asked as the hit lands (Health.Receive), so a change is felt on the very next one, a shot already in the air included.</summary>
        public static float EnemyDamage(float amount){float by=DamageOf(Current);return by==1?amount:amount*by;}
        /// <summary>What an enemy's or a boss's integrity is as it is made (ActorFactory.Enemy). A body awake keeps what it was made with: a bar never jumps under a change.
        /// One still on show before its fight has no bar yet, and is made again (Spawner).</summary>
        public static float EnemyIntegrity(float designed){float by=IntegrityOf(Current);return by==1?designed:designed*by;}
        public static string Name(DifficultySetting setting)=>setting switch{DifficultySetting.Gentle=>"Gentle",DifficultySetting.Demanding=>"Demanding",_=>"Standard"};
        /// <summary>What a setting does, as its own numbers: none of them is called easy or hard, and none has a mark on the HUD.</summary>
        public static string Says(DifficultySetting setting)=>$"enemies deal {DamageOf(setting)*100:0}% damage and have {IntegrityOf(setting)*100:0}% Integrity";
        /// <summary>A setting by its name, as a replay route writes it. No name at all is Standard; a name that isn't one of the three is refused, never guessed at.</summary>
        public static bool TryNamed(string name,out DifficultySetting setting)
        {
            setting=DifficultySetting.Standard;
            if(string.IsNullOrEmpty(name)||name=="Standard")return true;
            if(name=="Gentle"){setting=DifficultySetting.Gentle;return true;}
            if(name=="Demanding"){setting=DifficultySetting.Demanding;return true;}
            return false;
        }
    }
}
