using System;

namespace LighFight.Core
{
    public enum Level { High, Mid, Low }
    public enum StrikeOutcome { None, Hit, Blocked, Miss, Interrupted }

    public readonly struct Move
    {
        public readonly string Id;
        public readonly int Startup, Active, Recovery, Damage;
        public readonly float Reach;
        public readonly Level Level;

        public Move(string id, int startup, int active, int recovery, int damage, float reach, Level level)
        {
            Id = id; Startup = startup; Active = active; Recovery = recovery;
            Damage = damage; Reach = reach; Level = level;
        }

        public int Duration => Startup + Active + Recovery;
    }

    public static class Moves
    {
        public static readonly Move[] All = {
            new Move("jab",10,3,15,6,1.5f,Level.High), new Move("cross",14,3,20,10,1.6f,Level.High),
            new Move("hook",18,4,23,13,1.3f,Level.High), new Move("body",16,3,20,10,1.3f,Level.Mid),
            new Move("frontKick",18,4,25,14,2.0f,Level.Mid), new Move("lowKick",17,4,22,10,1.8f,Level.Low),
            new Move("sweep",25,5,32,16,1.9f,Level.Low), new Move("uppercut",22,4,30,19,1.3f,Level.Mid),
            new Move("knee",16,4,23,13,1.1f,Level.Mid), new Move("elbow",15,3,22,12,1.1f,Level.High),
            new Move("spinKick",29,5,34,23,2.1f,Level.High), new Move("jumpKick",30,5,35,24,2.0f,Level.Mid),
            new Move("backfist",16,3,21,12,1.6f,Level.High), new Move("axeKick",31,5,32,25,1.8f,Level.Mid),
            new Move("sideKick",22,4,26,18,2.2f,Level.Mid), new Move("roundhouseKick",24,4,29,20,2.1f,Level.High),
            new Move("hammer",24,4,26,18,1.5f,Level.Mid), new Move("palm",13,3,18,8,1.4f,Level.Mid),
            new Move("heelKick",28,5,30,22,2.0f,Level.High), new Move("switchKick",20,4,25,16,1.9f,Level.Mid),
            new Move("highKick",24,4,29,19,2.1f,Level.High), new Move("shortHook",13,3,22,9,1.15f,Level.High),
            new Move("doublePunch",27,4,31,22,1.55f,Level.Mid), new Move("stepKick",28,4,33,23,2.35f,Level.High),
            new Move("counterstrike",19,3,26,15,1.55f,Level.Mid)
        };
    }

    public sealed class Fighter
    {
        public float X, Z;
        public float FacingX=1,FacingZ;
        public Level LastHitLevel;
        public int Health = 150, Stun, DownTicks;
        public int HitSerial, LastDamage, ReactionTicks, ReactionDuration;
        public bool LastBlocked, LastCounter;
        public bool Guard, Crouch;
        public Move? Attack;
        public StrikeOutcome Outcome;
        public int LastStrikeIndex=-1;
        public int Frame;
        internal bool Connected, CleanHit;
        internal int ChainStepTicks;
        public int ComboCount { get; private set; }
        public bool Chain(int index)
        {
            if (!Attack.HasValue || !CleanHit || ComboCount >= Combos.MaxHits || Stun > 0 || DownTicks > 0 || Health <= 0) return false;
            if(index<0 || index>=Moves.All.Length)return false;
            var move = Attack.Value;
            if (Frame < move.Startup + move.Active || Frame > move.Startup + move.Active + 6) return false;
            bool allowed = Combos.CanFollow(move.Id,index);
            if (!allowed) return false;
            LastStrikeIndex=index;Outcome=StrikeOutcome.None;Attack = Moves.All[index]; Frame = 0; Connected = CleanHit = false; ComboCount++; ChainStepTicks=12; return true;
        }

        public bool Start(int index)
        {
            if (Health <= 0 || index < 0 || index >= Moves.All.Length || Stun > 0 || DownTicks > 0 || Attack.HasValue)
                return false;
            Guard=false;
            LastStrikeIndex=index;Outcome=StrikeOutcome.None;Attack = Moves.All[index]; Frame = 0; Connected = CleanHit = false; ComboCount = 1; ChainStepTicks=0;
            return true;
        }
    }

    public sealed class Match
    {
        public const int TickRate = 60;
        public const float MinimumSeparation=.60f;
        public const int KnockdownTicks = 90, GetUpTicks = 30;
        public readonly Fighter A = new Fighter { X = -1.4f }, B = new Fighter { X = 1.4f, FacingX=-1 };
        public bool Planar { get; }
        public Match(bool planar=false){Planar=planar;}
        private void ConstrainPlane(){
            if(!Planar)return;
            A.Z=B.Z=0;A.FacingZ=B.FacingZ=0;
            A.FacingX=A.FacingX<0?-1:1;B.FacingX=B.FacingX<0?-1:1;
        }
        public int Tick { get; private set; }
        public Action<Match> BeforeContact;
        public Func<Fighter,Fighter,bool> ContactValidator;
        public bool RequirePhysicalContact;
        public Func<Fighter,int,float> EffectiveReach;
        public float Reach(Fighter fighter,int index)=>EffectiveReach!=null?EffectiveReach(fighter,index):Moves.All[index].Reach;
        public bool Finished => A.Health <= 0 || B.Health <= 0;

        private readonly struct Contact
        {
            public readonly bool Hit, Blocked, Counter, Knockdown;
            public readonly int Damage;
            public readonly float Push;
            public readonly Level Level;
            public Contact(bool hit, bool blocked, bool counter, bool knockdown, int damage, float push,Level level)
            { Hit = hit; Blocked = blocked; Counter = counter; Knockdown = knockdown; Damage = damage; Push = push; Level=level; }
        }

        public void Step()
        {
            ConstrainPlane();
            if (Finished) return;
            Tick++;
            BeforeContact?.Invoke(this);
            // Capture both outcomes before mutating either fighter: real simultaneous trades.
            var outgoingA = Resolve(A, B);
            var outgoingB = Resolve(B, A);
            Apply(A, B, outgoingA);
            Apply(B, A, outgoingB);
            Advance(A); Advance(B);
        }

        private Contact Resolve(Fighter attacker, Fighter defender)
        {
            if (!attacker.Attack.HasValue || attacker.Connected || defender.DownTicks > 0) return default;
            var move = attacker.Attack.Value;
            if (attacker.Frame < move.Startup || attacker.Frame >= move.Startup + move.Active ||
                (!RequirePhysicalContact && Distance(attacker,defender) > move.Reach) ||
                (defender.Crouch && move.Level == Level.High)) return default;
            if(RequirePhysicalContact && (ContactValidator==null || !ContactValidator(attacker,defender)))return default;
            bool blocked = defender.Guard && !defender.Attack.HasValue &&
                (move.Level == Level.Low ? defender.Crouch : !defender.Crouch);
            bool counter = !blocked && defender.Attack.HasValue && defender.Frame < defender.Attack.Value.Startup;
            bool knockdown = move.Id == "sweep" || move.Id == "axeKick" || move.Id == "heelKick" || move.Id == "jumpKick";
            return new Contact(true, blocked, counter, knockdown, Combos.Damage(move.Damage + (counter ? move.Damage / 4 : 0),attacker.ComboCount), Math.Sign(defender.X - attacker.X) * .12f,move.Level);
        }

        private static void Apply(Fighter attacker, Fighter defender, Contact contact)
        {
            if (!contact.Hit) return;
            attacker.Connected = true; attacker.CleanHit = !contact.Blocked;attacker.Outcome=contact.Blocked?StrikeOutcome.Blocked:StrikeOutcome.Hit;
            defender.LastHitLevel=contact.Level;
            defender.HitSerial++; defender.LastDamage = contact.Blocked ? 0 : contact.Damage;
            defender.LastBlocked = contact.Blocked; defender.LastCounter = contact.Counter;
            defender.ReactionTicks=0;
            defender.ReactionDuration=contact.Blocked?8:contact.Counter?26:contact.Level==Level.Low?22:contact.Level==Level.Mid?20:18;
            defender.Stun = Math.Max(defender.Stun, defender.ReactionDuration);
            if (contact.Blocked) return;
            defender.Health = Math.Max(0, defender.Health - contact.Damage);
            if (contact.Knockdown) { defender.DownTicks = KnockdownTicks; defender.Stun = KnockdownTicks; }
            float distance=Distance(attacker,defender);
            if(distance>.0001f){defender.X = Math.Max(-5, Math.Min(5, defender.X + (defender.X-attacker.X)/distance*.12f));defender.Z = Math.Max(-1.5f,Math.Min(1.5f,defender.Z+(defender.Z-attacker.Z)/distance*.12f));}
            // A hit cancels startup/recovery. A simultaneous contact was captured above.
            if(defender.Attack.HasValue && defender.Outcome==StrikeOutcome.None)defender.Outcome=StrikeOutcome.Interrupted;
            defender.Attack = null;
            defender.Frame = 0;
        }

        private void Advance(Fighter fighter)
        {
            // A confirmed link adds a small committed step over startup, without homing or crossing bodies.
            if(fighter.ChainStepTicks>0 && fighter.Attack.HasValue && fighter.Frame<fighter.Attack.Value.Startup && fighter.Stun==0)
            {
                fighter.ChainStepTicks--;
                float x=Math.Max(-5,Math.Min(5,fighter.X+fighter.FacingX*.02f)),z=Math.Max(-1.5f,Math.Min(1.5f,fighter.Z+fighter.FacingZ*.02f));
                var other=fighter==A?B:A;
                if(HitGeometry.SegmentDistanceSquared(fighter.X,0,fighter.Z,x,0,z,other.X,0,other.Z,other.X,0,other.Z)>=MinimumSeparation*MinimumSeparation){fighter.X=x;fighter.Z=z;}
            }
            else fighter.ChainStepTicks=0;
            if (fighter.Stun > 0) {fighter.Stun--;fighter.ReactionTicks++;}
            if (fighter.DownTicks > 0) fighter.DownTicks--;
            if(fighter.Attack.HasValue){fighter.Frame++;if(!fighter.Connected && fighter.Frame>=fighter.Attack.Value.Startup+fighter.Attack.Value.Active)fighter.Outcome=StrikeOutcome.Miss;if(fighter.Frame>=fighter.Attack.Value.Duration){fighter.Attack=null;fighter.Frame=0;}}
        }

        public bool Attack(Fighter fighter, int index)
        {
            if(Finished || (fighter!=A && fighter!=B) || index<0 || index>=Moves.All.Length)return false;
            ConstrainPlane();
            if(!fighter.Attack.HasValue){var other=fighter==A?B:A;float distance=Distance(fighter,other);if(distance>.0001f){fighter.FacingX=(other.X-fighter.X)/distance;fighter.FacingZ=(other.Z-fighter.Z)/distance;}}
            return fighter.Chain(index) || fighter.Start(index);
        }

        public static float Distance(Fighter a,Fighter b) => (float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
        public void Walk(Fighter fighter,float direction) => Walk(fighter,direction,0);
        public void Walk(Fighter fighter,float xDirection,float zDirection)
        {
            ConstrainPlane();
            if(Finished || (fighter!=A && fighter!=B) || fighter.Stun>0 || fighter.DownTicks>0 || fighter.Attack.HasValue ||
               float.IsNaN(xDirection)||float.IsInfinity(xDirection)||float.IsNaN(zDirection)||float.IsInfinity(zDirection))return;
            if(Planar)zDirection=0;
            float magnitude=(float)Math.Sqrt(xDirection*xDirection+zDirection*zDirection);if(magnitude>1){xDirection/=magnitude;zDirection/=magnitude;}
            float speed=fighter.Guard||fighter.Crouch?.025f:.045f;
            float x=Math.Max(-5,Math.Min(5,fighter.X+xDirection*speed)),z=Math.Max(-1.5f,Math.Min(1.5f,fighter.Z+zDirection*speed));
            var other=fighter==A?B:A;
            if(HitGeometry.SegmentDistanceSquared(fighter.X,0,fighter.Z,x,0,z,other.X,0,other.Z,other.X,0,other.Z)>=MinimumSeparation*MinimumSeparation){fighter.X=x;fighter.Z=z;}
        }
    }
}
