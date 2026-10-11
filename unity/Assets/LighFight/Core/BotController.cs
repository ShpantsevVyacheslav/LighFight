using System;
using System.Collections.Generic;
namespace LighFight.Core
{
    public sealed class BotController
    {
        public const int ReactionTicks = 14;
        private readonly Random random;
        private int reaction=ReactionTicks, attackCooldown;
        private bool guard,crouch;
        private Match previous;
        public BotController(int seed=42){random=new Random(seed);}
        public void Step(FightSession session)
        {
            if(session.Phase!=FightPhase.Fighting || session.Training)return;
            var match=session.Match;var bot=match.B;var target=match.A;
            if(previous!=match){previous=match;reaction=ReactionTicks;guard=crouch=false;attackCooldown=0;}
            if(attackCooldown>0)attackCooldown--;
            float distance=Math.Abs(bot.X-target.X);
            if(--reaction<=0)
            {
                reaction=ReactionTicks;
                guard=target.Attack.HasValue && target.Frame < target.Attack.Value.Startup + target.Attack.Value.Active && random.NextDouble()<.8;
                crouch=guard && target.Attack.Value.Level==Level.Low;
                if(!guard && target.Attack.HasValue && target.Frame >= target.Attack.Value.Startup + target.Attack.Value.Active) attackCooldown=0;
                if(!guard && attackCooldown<=0 && bot.Stun==0 && bot.DownTicks==0 && !bot.Attack.HasValue)
                {
                    var candidates=new List<int>();
                    for(int i=0;i<Moves.All.Length;i++)
                        if(Moves.All[i].Reach>=distance && !(target.Crouch && Moves.All[i].Level==Level.High))candidates.Add(i);
                    if(candidates.Count>0)
                    {session.QueueAttack(false,candidates[random.Next(candidates.Count)]);attackCooldown=38+random.Next(20);}
                }
            }
            bot.Guard=guard;bot.Crouch=crouch;
            if(!guard && distance>1.4f)match.Walk(bot,Math.Sign(target.X-bot.X));
            else if(!guard && distance<1.05f && attackCooldown>0)match.Walk(bot,Math.Sign(bot.X-target.X));
        }
    }
}
