using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mentora.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddFilesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_ApplicationUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_AspNetUsers_ApplicationUserId1",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ApplicationUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ApplicationUserId1",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Bookings");

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CloudinaryPublicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_AspNetUsers_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MenteeId",
                table: "Bookings",
                column: "MenteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MentorId",
                table: "Bookings",
                column: "MentorId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_UploadedById",
                table: "Files",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Mentee_ApplicationUser",
                table: "Bookings",
                column: "MenteeId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Mentor_ApplicationUser",
                table: "Bookings",
                column: "MentorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Mentee_ApplicationUser",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Booking_Mentor_ApplicationUser",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MenteeId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MentorId",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ApplicationUserId",
                table: "Bookings",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ApplicationUserId1",
                table: "Bookings",
                column: "ApplicationUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_ApplicationUserId",
                table: "Bookings",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_AspNetUsers_ApplicationUserId1",
                table: "Bookings",
                column: "ApplicationUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
