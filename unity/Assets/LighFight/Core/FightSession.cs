namespace LighFight.Core
{
    public enum FightPhase { Fighting, RoundOver, Complete }

    // All timing belongs to simulation ticks, including the input buffer and round clock.
    public sealed class FightSession
    {
        public const int RoundTicks = 60 * Match.TickRate;
        public const int BreakTicks = 90;
        public const int BufferTicks = 8;
        public Match Match { get; private set; } = new Match();
        public FightPhase Phase { get; private set; }
        public int RemainingTicks { get; private set; } = RoundTicks;
        public int WinsA { get; private set; }
        public int WinsB { get; private set; }
        public int Round { get; private set; } = 1;
        public int RoundWinner { get; private set; } = -1;
        public int Winner => Phase != FightPhase.Complete ? -1 : WinsA > WinsB ? 0 : 1;
        private int breakRemaining;
        private int attackA = -1, attackB = -1, lifeA, lifeB;

        public bool QueueAttack(bool playerA, int index)
        {
            if (Phase != FightPhase.Fighting || index < 0 || index >= Moves.All.Length) return false;
            if (playerA) { attackA = index; lifeA = BufferTicks; }
            else { attackB = index; lifeB = BufferTicks; }
            return true;
        }

        private void Consume(Fighter fighter, ref int attack, ref int life)
        {
            if (attack < 0) return;
            if (Match.Attack(fighter, attack) || --life <= 0) { attack = -1; life = 0; }
        }

        public void Step()
        {
            if (Phase == FightPhase.Complete) return;
            if (Phase == FightPhase.RoundOver)
            {
                if (--breakRemaining <= 0)
                {
                    Match = new Match(); RemainingTicks = RoundTicks;
                    Phase = FightPhase.Fighting; Round++; RoundWinner = -1;
                }
                return;
            }
            Consume(Match.A, ref attackA, ref lifeA);
            Consume(Match.B, ref attackB, ref lifeB);
            Match.Step();
            RemainingTicks--;
            if (!Match.Finished && RemainingTicks > 0) return;
            RoundWinner = Match.A.Health == Match.B.Health ? -1 : Match.A.Health > Match.B.Health ? 0 : 1;
            if (RoundWinner == 0) WinsA++;
            if (RoundWinner == 1) WinsB++;
            attackA = attackB = -1; lifeA = lifeB = 0;
            Phase = WinsA >= 2 || WinsB >= 2 ? FightPhase.Complete : FightPhase.RoundOver;
            breakRemaining = BreakTicks;
        }
    }
}
