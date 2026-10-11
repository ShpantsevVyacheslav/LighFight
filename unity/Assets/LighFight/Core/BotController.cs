using System;
using System.Collections.Generic;
namespace LighFight.Core
{
    public sealed class BotController
    {
        public const int ReactionTicks = 14;
        private readonly Random random;
        private readonly int reactionDelay;
        private int reaction=ReactionTicks, attackCooldown;
        private bool guard,crouch;
        private Match previous;
        public BotController(int seed=42,int difficulty=1){random=new Random(seed);reactionDelay=difficulty<=0?24:difficulty>=2?10:ReactionTicks;reaction=reactionDelay;}
        public void Step(FightSession session)
        {
            if(session.Phase!=FightPhase.Fighting || session.Training)return;
            var match=session.Match;var bot=match.B;var target=match.A;
            if(previous!=match){previous=match;reaction=reactionDelay;guard=crouch=false;attackCooldown=0;}
            if(attackCooldown>0)attackCooldown--;
            float distance=Match.Distance(bot,target);
            if(--reaction<=0)
            {
                reaction=reactionDelay;
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
            if(!guard && distance>1.4f)match.Walk(bot,(target.X-bot.X)/distance,(target.Z-bot.Z)/distance);
            else if(!guard && distance<1.05f && attackCooldown>0)match.Walk(bot,(bot.X-target.X)/distance,(bot.Z-target.Z)/distance);
        }
    }
}
