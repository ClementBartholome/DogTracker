using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogTracker.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableNotificationAddColumnIsDone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDone",
                table: "Notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDone",
                table: "Notifications");
        }
    }
}
