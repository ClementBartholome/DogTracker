using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogTracker.Migrations
{
    /// <inheritdoc />
    public partial class AlterTableNotificationColumnMessageIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageId",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TreatmentId",
                table: "Notifications",
                column: "TreatmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Treatments_TreatmentId",
                table: "Notifications",
                column: "TreatmentId",
                principalTable: "Treatments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Treatments_TreatmentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TreatmentId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "MessageId",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
