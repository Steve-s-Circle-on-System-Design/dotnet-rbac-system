using System;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RbacSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    email = table.Column<string>(type: "citext", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "user"),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "pending_verification"),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    profile_picture = table.Column<string>(type: "text", nullable: true),
                    provider = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    provider_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    token_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_failed_login_attempts", "failed_login_attempts >= 0");
                    table.CheckConstraint("ck_users_token_version", "token_version >= 0");
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    resource = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    details = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    recipient = table.Column<string>(type: "citext", maxLength: 255, nullable: false),
                    subject = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    template = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    provider_message_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    delivery_metadata = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    clicked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bounced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    file_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    cloudinary_public_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    secure_url = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                    table.CheckConstraint("ck_files_file_size", "file_size >= 0");
                    table.CheckConstraint("ck_files_height", "height IS NULL OR height > 0");
                    table.CheckConstraint("ck_files_width", "width IS NULL OR width > 0");
                    table.ForeignKey(
                        name: "FK_files_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    email = table.Column<string>(type: "citext", maxLength: 255, nullable: false),
                    code_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    purpose = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    resend_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_verifications", x => x.id);
                    table.CheckConstraint("ck_otp_verifications_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_otp_verifications_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_otp_verifications_resend_count", "resend_count >= 0");
                    table.ForeignKey(
                        name: "FK_otp_verifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "password_resets",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_resets", x => x.id);
                    table.CheckConstraint("ck_password_resets_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "FK_password_resets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    token_family = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    rotated_from_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoke_reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("ck_refresh_tokens_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_refresh_tokens_rotation", "rotated_from_id IS NULL OR rotated_from_id <> id");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_refresh_tokens_rotated_from_id",
                        column: x => x.rotated_from_id,
                        principalTable: "refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_resource",
                table: "audit_logs",
                columns: new[] { "resource", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_created_at",
                table: "email_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_provider_message_id",
                table: "email_logs",
                column: "provider_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_status",
                table: "email_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_email_logs_user_id",
                table: "email_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_uploaded_at",
                table: "files",
                column: "uploaded_at");

            migrationBuilder.CreateIndex(
                name: "ix_files_user_id",
                table: "files",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_files_cloudinary_public_id",
                table: "files",
                column: "cloudinary_public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_otp_verifications_active",
                table: "otp_verifications",
                columns: new[] { "email", "purpose" },
                filter: "used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_otp_verifications_expires_at",
                table: "otp_verifications",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_otp_verifications_lookup",
                table: "otp_verifications",
                columns: new[] { "email", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_otp_verifications_user_id",
                table: "otp_verifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_resets_active",
                table: "password_resets",
                columns: new[] { "user_id", "expires_at" },
                filter: "used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_password_resets_expires_at",
                table: "password_resets",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ux_password_resets_token_hash",
                table: "password_resets",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_rotated_from_id",
                table: "refresh_tokens",
                column: "rotated_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_family",
                table: "refresh_tokens",
                column: "token_family");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_deleted_at",
                table: "users",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_provider_identity",
                table: "users",
                columns: new[] { "provider", "provider_id" },
                unique: true,
                filter: "provider IS NOT NULL AND provider_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "otp_verifications");

            migrationBuilder.DropTable(
                name: "password_resets");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
