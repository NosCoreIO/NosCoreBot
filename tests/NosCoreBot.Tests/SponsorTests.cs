using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NosCoreBot.Services;

namespace NosCoreBot.Tests
{
    [TestClass]
    public class SponsorTests
    {
        private static SponsorEntry Entry(string id, bool isActive, int monthlyCents, int lifetimeCents,
            bool isPublic = true)
        {
            return new SponsorEntry("github", id, id, isPublic, isActive, monthlyCents, lifetimeCents);
        }

        [TestMethod]
        public void ActiveSponsorTotalGrowsWithTheFetchedValue()
        {
            var state = new SponsorState { Lifetime = { ["github:alice"] = 3000 } };

            var entries = SponsorService.ApplyLifetimeFloor(state, new[] { Entry("alice", true, 1000, 9000) });

            Assert.AreEqual(9000, entries[0].LifetimeCents);
            Assert.AreEqual(9000, state.Lifetime["github:alice"]);
        }

        [TestMethod]
        public void ActiveSponsorTotalNeverShrinks()
        {
            var state = new SponsorState { Lifetime = { ["github:alice"] = 9000 } };

            var entries = SponsorService.ApplyLifetimeFloor(state, new[] { Entry("alice", true, 1000, 3000) });

            Assert.AreEqual(9000, entries[0].LifetimeCents);
        }

        [TestMethod]
        public void EndedSponsorshipKeepsTheTotalItStoppedAt()
        {
            var state = new SponsorState { Lifetime = { ["github:bob"] = 2000 } };

            var entries = SponsorService.ApplyLifetimeFloor(state, new[] { Entry("bob", false, 0, 7000) });

            Assert.AreEqual(2000, entries[0].LifetimeCents);
            Assert.AreEqual(2000, state.Lifetime["github:bob"]);
        }

        [TestMethod]
        public void UnknownEndedSponsorshipFallsBackToTheFetchedTotal()
        {
            var state = new SponsorState();

            var entries = SponsorService.ApplyLifetimeFloor(state, new[] { Entry("carol", false, 0, 4200) });

            Assert.AreEqual(4200, entries[0].LifetimeCents);
        }

        [TestMethod]
        public void EntriesAreRankedByTotal()
        {
            var state = new SponsorState();

            var entries = SponsorService.ApplyLifetimeFloor(state, new[]
            {
                Entry("small", true, 300, 600),
                Entry("big", true, 5000, 50000),
                Entry("middle", true, 1000, 12000)
            });

            CollectionAssert.AreEqual(new List<string> { "big", "middle", "small" },
                entries.ConvertAll(entry => entry.Id));
        }

        [TestMethod]
        public void PrivateSponsorsAreAnonymisedInTheLeaderboard()
        {
            var embed = SponsorSyncService.BuildEmbed(new[]
            {
                Entry("public-one", true, 1000, 12000),
                Entry("private-one", true, 5000, 50000, false)
            });

            StringAssert.Contains(embed.Description, "public-one");
            StringAssert.Contains(embed.Description, "Anonymous");
            Assert.IsFalse(embed.Description.Contains("private-one"));
        }

        [TestMethod]
        public void AnEmptyLeaderboardInvitesSponsors()
        {
            var embed = SponsorSyncService.BuildEmbed(new SponsorEntry[0]);

            StringAssert.Contains(embed.Description, "github.com/sponsors");
        }

        [TestMethod]
        public void AmountsAreRenderedAsWholeDollars()
        {
            Assert.AreEqual("$50", SponsorService.Money(5000));
            Assert.AreEqual("$3", SponsorService.Money(300));
        }
    }
}
