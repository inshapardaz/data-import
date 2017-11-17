using Inshapardaz.DataImport.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Inshapardaz.DataImport.Database
{
    public class DatabaseContext : DbContext, IDatabaseContext
    {
        public DatabaseContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Dictionary> Dictionary { get; set; }
        public virtual DbSet<Meaning> Meaning { get; set; }
        public virtual DbSet<Translation> Translation { get; set; }
        public virtual DbSet<Word> Word { get; set; }
        public virtual DbSet<WordRelation> WordRelation { get; set; }
        public virtual DbSet<DictionaryDownload> DictionaryDownload { get; set; }
        public virtual DbSet<File> File { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Inshapardaz");

            modelBuilder.Entity<Dictionary>(entity =>
            {
                entity.ToTable("Dictionary", "Inshapardaz");

                entity.Property(e => e.Name).HasMaxLength(255);

                entity.Property(e => e.UserId).HasMaxLength(50);
            });

            modelBuilder.Entity<Meaning>(entity =>
            {
                entity.ToTable("Meaning", "Inshapardaz");

                entity.HasOne(d => d.Word)
                    .WithMany(p => p.Meaning)
                    .HasForeignKey(d => d.WordId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Meaning_WordDetail");
            });

            modelBuilder.Entity<Translation>(entity =>
            {
                entity.ToTable("Translation", "Inshapardaz");

                entity.HasOne(d => d.Word)
                    .WithMany(p => p.Translation)
                    .HasForeignKey(d => d.WordId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Translation_WordDetail");
            });

            modelBuilder.Entity<Word>(entity =>
            {
                entity.ToTable("Word", "Inshapardaz");

                entity.Property(e => e.DictionaryId).HasDefaultValueSql("1");

                entity.HasOne(d => d.Dictionary)
                    .WithMany(p => p.Word)
                    .HasForeignKey(d => d.DictionaryId)
                    .HasConstraintName("FK_Word_Dictionary");
            });
            
            modelBuilder.Entity<WordRelation>(entity =>
            {
                entity.ToTable("WordRelation", "Inshapardaz");

                entity.HasOne(d => d.RelatedWord)
                    .WithMany(p => p.WordRelationRelatedWord)
                    .HasForeignKey(d => d.RelatedWordId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_WordRelation_RelatedWord");

                entity.HasOne(d => d.SourceWord)
                    .WithMany(p => p.WordRelationSourceWord)
                    .HasForeignKey(d => d.SourceWordId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_WordRelation_SourceWord");
            });

            modelBuilder.Entity<File>(entity => { entity.ToTable("File", "Inshapardaz"); });

            modelBuilder.Entity<DictionaryDownload>(entity =>
            {
                entity.ToTable("DictionaryDownload", "Inshapardaz");
                entity.HasOne(d => d.Dictionary)
                    .WithMany(d => d.Downloads);
                entity.HasOne(d => d.File);
            });
        }
    }
}