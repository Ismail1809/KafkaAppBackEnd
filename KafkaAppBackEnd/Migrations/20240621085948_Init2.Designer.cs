﻿// <auto-generated />
using System;
using KafkaAppBackEnd.DbContent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KafkaAppBackEnd.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240621085948_Init2")]
    partial class Init2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("KafkaAppBackEnd.Models.Connection", b =>
                {
                    b.Property<int>("ConnectionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ConnectionId"));

                    b.Property<string>("BootStrapServer")
                        .HasColumnType("text");

                    b.Property<string>("ConnectionName")
                        .HasColumnType("text");

                    b.Property<bool?>("IsConnected")
                        .HasColumnType("boolean");

                    b.HasKey("ConnectionId");

                    b.ToTable("Connections");
                });
#pragma warning restore 612, 618
        }
    }
}
