using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WinAIBar.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private static readonly string[] SnapshotIndexColumns = ["Provider", "CapturedAt"];
        private static readonly bool[] SnapshotIndexDescending = [false, true];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Snapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    CapturedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    RawPayload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SnapshotId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Utilization = table.Column<double>(type: "REAL", nullable: false),
                    ResetsAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Used = table.Column<long>(type: "INTEGER", nullable: true),
                    Limit = table.Column<long>(type: "INTEGER", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotas_Snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "Snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotas_SnapshotId",
                table: "Quotas",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshots_Provider_CapturedAt",
                table: "Snapshots",
                columns: SnapshotIndexColumns,
                descending: SnapshotIndexDescending);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotas");

            migrationBuilder.DropTable(
                name: "Snapshots");
        }
    }
}
