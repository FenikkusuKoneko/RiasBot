using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

namespace Rias.Migrations
{
    public partial class CharacterSearchVector : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "custom_characters",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name", "description" });

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "characters",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name", "description" });

            migrationBuilder.CreateIndex(
                name: "ix_custom_characters_search_vector",
                table: "custom_characters",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_characters_search_vector",
                table: "characters",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_custom_characters_search_vector",
                table: "custom_characters");

            migrationBuilder.DropIndex(
                name: "ix_characters_search_vector",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "custom_characters");

            migrationBuilder.DropColumn(
                name: "description",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "characters");
        }
    }
}
