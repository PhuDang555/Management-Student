using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TCS2010PPTG4.Models;

namespace TCS2010PPTG4.Data
{
    public class ApplicationDbContext : IdentityDbContext<CUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Department { get; set; }
        public DbSet<Topic> Topic { get; set; }
        public DbSet<Contribution> Contribution { get; set; }
        public DbSet<SubmittedFile> File { get; set; }
        public DbSet<Comment> Comment { get; set; }
    }
}