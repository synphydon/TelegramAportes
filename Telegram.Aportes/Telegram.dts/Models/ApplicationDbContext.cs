using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Telegram.dts.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<aporte> aportes { get; set; }

    public virtual DbSet<mensajes_sistema> mensajes_sistemas { get; set; }

    public virtual DbSet<multimedium> multimedia { get; set; }

    public virtual DbSet<usuario> usuarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql(Clases.General.Conexion, Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.45-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<aporte>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PRIMARY");

            entity.HasIndex(e => e.telegram_id, "Segundo");

            entity.HasIndex(e => new { e.telegram_id, e.fecha }, "cuarto");

            entity.HasIndex(e => new { e.estado, e.fecha }, "quinto");

            entity.HasIndex(e => new { e.id, e.fecha }, "tercero");

            entity.Property(e => e.fecha).HasColumnType("datetime");
        });

        modelBuilder.Entity<mensajes_sistema>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PRIMARY");

            entity.ToTable("mensajes_sistema");

            entity.HasIndex(e => new { e.clave, e.baja }, "secundario").IsUnique();

            entity.Property(e => e.clave).HasMaxLength(45);
            entity.Property(e => e.idioma)
                .HasMaxLength(5)
                .HasDefaultValueSql("'es-es'");
        });

        modelBuilder.Entity<multimedium>(entity =>
        {
            entity.HasKey(e => new { e.aporte_id, e.orden })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.HasIndex(e => e.aporte_id, "segunda");
        });

        modelBuilder.Entity<usuario>(entity =>
        {
            entity.HasKey(e => e.id).HasName("PRIMARY");

            entity.HasIndex(e => e.telegram_id, "primaria").IsUnique();

            entity.Property(e => e.fecha_ultimo_aporte).HasColumnType("datetime");
            entity.Property(e => e.nombre_usuario).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
