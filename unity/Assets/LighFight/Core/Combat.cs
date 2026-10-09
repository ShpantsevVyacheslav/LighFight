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
            new Move("spinKick",29,5,34,23,2.1f,Level.High), new Move("jumpKick",30,5,35,24,2.0f,Level.Mid)
        };
    }

    public sealed class Fighter
    {
        public float X;
        public int Health = 150, Stun;
        public bool Guard, Crouch;
        public Move? Attack;
        public int Frame;
        internal bool Connected;

        public bool Start(int index)
        {
            if (Health <= 0 || index < 0 || index >= Moves.All.Length || Stun > 0 || Attack.HasValue)
                return false;
            Attack = Moves.All[index]; Frame = 0; Connected = false;
            return true;
        }
    }

    public sealed class Match
    {
        public const int TickRate = 60;
        public readonly Fighter A = new Fighter { X = -1.4f }, B = new Fighter { X = 1.4f };
        public int Tick { get; private set; }
        public bool Finished => A.Health <= 0 || B.Health <= 0;

        private readonly struct Contact
        {
            public readonly bool Hit, Blocked;
            public readonly int Damage;
            public readonly float Push;
            public Contact(bool hit, bool blocked, int damage, float push)
            { Hit = hit; Blocked = blocked; Damage = damage; Push = push; }
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
            if (!attacker.Attack.HasValue || attacker.Connected) return default;
            var move = attacker.Attack.Value;
            if (attacker.Frame < move.Startup || attacker.Frame >= move.Startup + move.Active ||
                Math.Abs(attacker.X - defender.X) > move.Reach ||
                (defender.Crouch && move.Level == Level.High)) return default;
            bool blocked = defender.Guard && !defender.Attack.HasValue &&
                (move.Level == Level.Low ? defender.Crouch : !defender.Crouch);
            return new Contact(true, blocked, move.Damage, Math.Sign(defender.X - attacker.X) * .12f);
        }

        private static void Apply(Fighter attacker, Fighter defender, Contact contact)
        {
            if (!contact.Hit) return;
            attacker.Connected = true;
            defender.Stun = Math.Max(defender.Stun, contact.Blocked ? 8 : 18);
            if (contact.Blocked) return;
            defender.Health = Math.Max(0, defender.Health - contact.Damage);
            defender.X = Math.Max(-5, Math.Min(5, defender.X + contact.Push));
            // A hit cancels startup/recovery. A simultaneous contact was captured above.
            defender.Attack = null;
            defender.Frame = 0;
        }

        private static void Advance(Fighter fighter)
        {
            if (fighter.Stun > 0) fighter.Stun--;
            if (fighter.Attack.HasValue && ++fighter.Frame >= fighter.Attack.Value.Duration)
            { fighter.Attack = null; fighter.Frame = 0; }
        }

        public bool Attack(Fighter fighter, int index)
        {
            return !Finished && (fighter == A || fighter == B) && fighter.Start(index);
        }

        public void Walk(Fighter fighter, float direction)
        {
            if (Finished || (fighter != A && fighter != B) || fighter.Stun > 0 || fighter.Attack.HasValue ||
                float.IsNaN(direction) || float.IsInfinity(direction)) return;
            var other = fighter == A ? B : A;
            var x = Math.Max(-5, Math.Min(5, fighter.X + Math.Max(-1, Math.Min(1, direction)) * .045f));
            if (Math.Abs(x - other.X) >= .75f) fighter.X = x;
        }
    }
}
