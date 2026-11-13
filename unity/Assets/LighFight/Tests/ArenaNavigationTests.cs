using System;
using NUnit.Framework;
using LighFight.Core;

namespace LighFight.Tests
{
    public sealed class ArenaNavigationTests
    {
        [Test]
        public void ArenaRectClampsFighterWithinBounds()
        {
            var rect = new ArenaRect(-5f, 5f, -2f, 2f);
            rect.Clamp(6f, 0f, 0.25f, out float x, out float z);
            Assert.AreEqual(4.75f, x, 0.001f);
            Assert.AreEqual(0f, z, 0.001f);

            rect.Clamp(-6f, -3f, 0.25f, out x, out z);
            Assert.AreEqual(-4.75f, x, 0.001f);
            Assert.AreEqual(-1.75f, z, 0.001f);
        }

        [Test]
        public void ObstacleDetectsFighterRadiusOverlap()
        {
            var obstacle = new ArenaObstacle("Desk", 0f, 2f, 1f, 3f);
            // Inside obstacle
            Assert.IsTrue(obstacle.Overlaps(1f, 2f, 0.25f));
            // Edge overlap via radius
            Assert.IsTrue(obstacle.Overlaps(-0.2f, 2f, 0.25f));
            // Clearly outside
            Assert.IsFalse(obstacle.Overlaps(-0.5f, 2f, 0.25f));
            Assert.IsFalse(obstacle.Overlaps(1f, 3.5f, 0.25f));
        }

        [Test]
        public void WallSlideAllowsSlidingAlongClearAxis()
        {
            // Obstacle covering Z from 1.0 to 3.0, X from 0.0 to 2.0
            var obstacle = new ArenaObstacle("Block", 0f, 2f, 1f, 3f);
            var profile = new ArenaProfile(new ArenaRect(-10f, 10f, -10f, 10f), new[] { obstacle });

            // Fighter at (-0.5, 0), tries to move diagonally towards (0.5, 1.5)
            // Entering obstacle from the bottom-left: Z=1.5 is inside [1, 3], but X=-0.5 with radius is clear of [0, 2]
            profile.ResolveMovement(-0.5f, 0f, 0.5f, 1.5f, 0.25f, out float outX, out float outZ);
            // Should slide along Z or X rather than penetrating
            Assert.IsFalse(profile.IsBlocked(outX, outZ, 0.25f));
        }

        [Test]
        public void MatchWalkIn2DRespectsProfileBoundsAndSeparation()
        {
            var match = new Match(planar: false);
            var profile = ArenaProfile.CreateMeeting();
            match.NavigationProfile = profile;

            // Fighter A walks right and forward
            match.Walk(match.A, 1f, 1f);
            Assert.Greater(match.A.X, -1.4f);
            Assert.Greater(match.A.Z, 0f);

            // Cannot walk through arena back wall (MaxZ is 3.5, radius 0.25 -> max 3.25)
            for (int i = 0; i < 300; i++)
            {
                match.Walk(match.A, 0f, 1f);
            }
            Assert.LessOrEqual(match.A.Z, profile.Bounds.MaxZ - 0.25f + 0.001f);
        }

        [Test]
        public void FightSessionSetsArenaAndPreservesNavigation()
        {
            var session = new FightSession();
            session.SetArena(1); // Kitchen
            Assert.AreEqual(1, session.ArenaIndex);
            Assert.IsNotNull(session.Match.NavigationProfile);
            Assert.AreEqual(3, session.Match.NavigationProfile.Obstacles.Length); // Table, Fridge, CoffeeCabinet
        }

        [Test]
        public void AllFiveArenaProfilesInstantiateWithValidBounds()
        {
            for (int i = 0; i < 5; i++)
            {
                var profile = ArenaProfile.ForArena(i);
                Assert.IsNotNull(profile);
                Assert.Less(profile.Bounds.MinX, profile.Bounds.MaxX);
                Assert.Less(profile.Bounds.MinZ, profile.Bounds.MaxZ);
            }
        }
    }
}
