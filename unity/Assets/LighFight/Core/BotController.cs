using System;
namespace LighFight.Core
{
    public sealed class BotController
    {
        public const int ReactionTicks=14;
        private readonly Random random;
        private readonly int reactionDelay;
        private readonly int[] candidates=new int[Moves.All.Length];
        private int reaction,attackCooldown,sideTicks;
        private float sideDirection;
        private bool guard,crouch;
        private Match previous;
        public BotController(int seed=42,int difficulty=1){random=new Random(seed);reactionDelay=difficulty<=0?24:difficulty>=2?10:ReactionTicks;reaction=reactionDelay;}
        public void Step(FightSession session)
        {
            if(session.Phase!=FightPhase.Fighting || session.Training)return;
            var match=session.Match;var bot=match.B;var target=match.A;
            if(previous!=match){previous=match;reaction=reactionDelay;guard=crouch=false;attackCooldown=sideTicks=0;}
            if(attackCooldown>0)attackCooldown--;
            float distance=Match.Distance(bot,target);
            if(--reaction<=0)
            {
                reaction=reactionDelay;
                bool threatened=target.Attack.HasValue && distance<2.6f && target.Frame<target.Attack.Value.Startup+target.Attack.Value.Active;
                bool dodge=threatened && target.Frame<target.Attack.Value.Startup && random.NextDouble()<.30;
                guard=threatened && !dodge && random.NextDouble()<.8;
                crouch=guard && target.Attack.Value.Level==Level.Low;
                if(dodge){sideTicks=12;sideDirection=bot.Z>.9f?-1:bot.Z<-.9f?1:random.Next(2)==0?-1:1;}
                bool whiff=target.Attack.HasValue && !target.Connected && target.Frame>=target.Attack.Value.Startup+target.Attack.Value.Active;
                if(whiff)attackCooldown=0;
                if(!guard && sideTicks==0 && bot.Stun==0 && bot.DownTicks==0)
                {
                    if(bot.Attack.HasValue && bot.CleanHit && bot.Frame>=bot.Attack.Value.Startup+bot.Attack.Value.Active)
                    {
                        for(int i=0;i<Moves.All.Length;i++)if(Combos.CanFollow(bot.Attack.Value.Id,i)){session.QueueAttack(false,i);break;}
                    }
                    else if(attackCooldown<=0 && !bot.Attack.HasValue)
                    {
                        int count=0;
                        for(int i=0;i<Moves.All.Length;i++)if(match.Reach(bot,i)>=distance && !(target.Crouch && Moves.All[i].Level==Level.High) && (!whiff || Moves.All[i].Startup<=18))candidates[count++]=i;
                        if(count>0){session.QueueAttack(false,candidates[random.Next(count)]);attackCooldown=38+random.Next(20);}
                        else if(distance<2.4f && random.NextDouble()<.4){sideTicks=10;sideDirection=bot.Z>0?-1:1;}
                    }
                }
            }
            bot.Guard=guard;bot.Crouch=crouch;
            if(sideTicks>0){sideTicks--;if(distance>.001f){if(match.Planar)match.Walk(bot,Math.Sign(bot.X-target.X));else match.Walk(bot,-(target.Z-bot.Z)/distance*sideDirection,(target.X-bot.X)/distance*sideDirection);}}
            else if(!guard && distance>.8f)match.Walk(bot,(target.X-bot.X)/distance,(target.Z-bot.Z)/distance);
            else if(!guard && distance<.65f && distance>.001f && attackCooldown>0)match.Walk(bot,(bot.X-target.X)/distance,(bot.Z-target.Z)/distance);
        }
    }
}
