using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "ccloglogin",
                schema: "dbo",
                columns: table => new
                {
                    LogId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_id = table.Column<int>(type: "int", nullable: false),
                    Extension = table.Column<short>(type: "smallint", nullable: false),
                    TipoMov = table.Column<byte>(type: "tinyint", nullable: false),
                    fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ccloglogin", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "ccRIACat_Areas",
                schema: "dbo",
                columns: table => new
                {
                    IDArea = table.Column<int>(type: "int", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ccRIACat_Areas", x => new { x.IDArea, x.AreaName });
                });

            migrationBuilder.CreateTable(
                name: "ccUsers",
                schema: "dbo",
                columns: table => new
                {
                    User_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IDArea = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ccUsers", x => x.User_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ccloglogin_User_id_fecha",
                schema: "dbo",
                table: "ccloglogin",
                columns: new[] { "User_id", "fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ccloglogin",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ccRIACat_Areas",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ccUsers",
                schema: "dbo");
        }
    }
}
