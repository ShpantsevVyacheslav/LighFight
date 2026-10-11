using System;

namespace LighFight.Core
{
    public enum Level { High, Mid, Low }

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
        public float X;
        public int Health = 150, Stun, DownTicks;
        public int HitSerial, LastDamage;
        public bool LastBlocked, LastCounter;
        public bool Guard, Crouch;
        public Move? Attack;
        public int Frame;
        internal bool Connected;

        public bool Start(int index)
        {
            if (Health <= 0 || index < 0 || index >= Moves.All.Length || Stun > 0 || DownTicks > 0 || Attack.HasValue)
                return false;
            Attack = Moves.All[index]; Frame = 0; Connected = false;
            return true;
        }
    }

    public sealed class Match
    {
        public const int TickRate = 60;
        public const int KnockdownTicks = 90, GetUpTicks = 30;
        public readonly Fighter A = new Fighter { X = -1.4f }, B = new Fighter { X = 1.4f };
        public int Tick { get; private set; }
        public bool Finished => A.Health <= 0 || B.Health <= 0;

        private readonly struct Contact
        {
            public readonly bool Hit, Blocked, Counter, Knockdown;
            public readonly int Damage;
            public readonly float Push;
            public Contact(bool hit, bool blocked, bool counter, bool knockdown, int damage, float push)
            { Hit = hit; Blocked = blocked; Counter = counter; Knockdown = knockdown; Damage = damage; Push = push; }
        }

        public void Step()
        {
            if (Finished) return;
            Tick++;
            // Capture both outcomes before mutating either fighter: real simultaneous trades.
            var outgoingA = Resolve(A, B);
            var outgoingB = Resolve(B, A);
            Apply(A, B, outgoingA);
            Apply(B, A, outgoingB);
            Advance(A); Advance(B);
        }

        private static Contact Resolve(Fighter attacker, Fighter defender)
        {
            if (!attacker.Attack.HasValue || attacker.Connected || defender.DownTicks > 0) return default;
            var move = attacker.Attack.Value;
            if (attacker.Frame < move.Startup || attacker.Frame >= move.Startup + move.Active ||
                Math.Abs(attacker.X - defender.X) > move.Reach ||
                (defender.Crouch && move.Level == Level.High)) return default;
            bool blocked = defender.Guard && !defender.Attack.HasValue &&
                (move.Level == Level.Low ? defender.Crouch : !defender.Crouch);
            bool counter = !blocked && defender.Attack.HasValue && defender.Frame < defender.Attack.Value.Startup;
            bool knockdown = move.Id == "sweep" || move.Id == "axeKick" || move.Id == "heelKick" || move.Id == "jumpKick";
            return new Contact(true, blocked, counter, knockdown, move.Damage + (counter ? move.Damage / 4 : 0), Math.Sign(defender.X - attacker.X) * .12f);
        }

        private static void Apply(Fighter attacker, Fighter defender, Contact contact)
        {
            if (!contact.Hit) return;
            attacker.Connected = true;
            defender.HitSerial++; defender.LastDamage = contact.Blocked ? 0 : contact.Damage;
            defender.LastBlocked = contact.Blocked; defender.LastCounter = contact.Counter;
            defender.Stun = Math.Max(defender.Stun, contact.Blocked ? 8 : contact.Counter ? 26 : 18);
            if (contact.Blocked) return;
            defender.Health = Math.Max(0, defender.Health - contact.Damage);
            if (contact.Knockdown) { defender.DownTicks = KnockdownTicks; defender.Stun = KnockdownTicks; }
            defender.X = Math.Max(-5, Math.Min(5, defender.X + contact.Push));
            // A hit cancels startup/recovery. A simultaneous contact was captured above.
            defender.Attack = null;
            defender.Frame = 0;
        }

        private static void Advance(Fighter fighter)
        {
            if (fighter.Stun > 0) fighter.Stun--;
            if (fighter.DownTicks > 0) fighter.DownTicks--;
            if (fighter.Attack.HasValue && ++fighter.Frame >= fighter.Attack.Value.Duration)
            { fighter.Attack = null; fighter.Frame = 0; }
        }

        public bool Attack(Fighter fighter, int index)
        {
            return !Finished && (fighter == A || fighter == B) && fighter.Start(index);
        }

        public void Walk(Fighter fighter, float direction)
        {
            if (Finished || (fighter != A && fighter != B) || fighter.Stun > 0 || fighter.DownTicks > 0 || fighter.Attack.HasValue ||
                float.IsNaN(direction) || float.IsInfinity(direction)) return;
            var other = fighter == A ? B : A;
            var x = Math.Max(-5, Math.Min(5, fighter.X + Math.Max(-1, Math.Min(1, direction)) * (fighter.Guard || fighter.Crouch ? .025f : .045f)));
            if (Math.Abs(x - other.X) >= .75f) fighter.X = x;
        }
    }
}
