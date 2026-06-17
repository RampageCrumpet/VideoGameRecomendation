using GameRecommendation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace GameRecommendation.TestUtilities
{
    public class TestRecommendationDbContext : RecommendationDbContext
    {
        public TestRecommendationDbContext(DbContextOptions<TestRecommendationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseCollation(null);
        }
    }
}
