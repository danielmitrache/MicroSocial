using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroSocial.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoreStringLengthRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Truncate existing data to fit new limits
            migrationBuilder.Sql("UPDATE GroupMessages SET Content = LEFT(Content, 500) WHERE LEN(Content) > 500");
            migrationBuilder.Sql("UPDATE AspNetUsers SET Description = LEFT(Description, 250) WHERE LEN(Description) > 250");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "GroupMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "GroupMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}
