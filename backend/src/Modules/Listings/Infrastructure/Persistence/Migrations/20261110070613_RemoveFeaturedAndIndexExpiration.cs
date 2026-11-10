using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeMatch.Modules.Listings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFeaturedAndIndexExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve the legacy public view while removing its featured column dependency.
            migrationBuilder.Sql("""
                DO $$
                DECLARE definition text;
                BEGIN
                    IF to_regclass('public.vw_public_listings') IS NOT NULL THEN
                        definition := pg_get_viewdef('public.vw_public_listings'::regclass, true);
                        IF definition !~ '(l[.])?is_featured[[:space:]]*,' THEN
                            RAISE EXCEPTION 'Unexpected vw_public_listings definition; cannot remove is_featured safely';
                        END IF;
                        DROP VIEW public.vw_public_listings;
                        EXECUTE 'CREATE VIEW public.vw_public_listings AS '
                            || regexp_replace(definition, '(l[.])?is_featured[[:space:]]*,', '');
                    END IF;
                END $$;
                """);

            migrationBuilder.DropColumn(
                name: "is_featured",
                schema: "listings",
                table: "listings");

            migrationBuilder.CreateIndex(
                name: "ix_listings_published_expires_at",
                schema: "listings",
                table: "listings",
                column: "expires_at",
                filter: "status = 'PUBLISHED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_listings_published_expires_at",
                schema: "listings",
                table: "listings");

            migrationBuilder.AddColumn<bool>(
                name: "is_featured",
                schema: "listings",
                table: "listings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                DO $$
                DECLARE definition text;
                BEGIN
                    IF to_regclass('public.vw_public_listings') IS NOT NULL THEN
                        definition := pg_get_viewdef('public.vw_public_listings'::regclass, true);
                        IF definition !~ '(l[.])?view_count[[:space:]]*,' THEN
                            RAISE EXCEPTION 'Unexpected vw_public_listings definition; cannot restore is_featured safely';
                        END IF;
                        DROP VIEW public.vw_public_listings;
                        EXECUTE 'CREATE VIEW public.vw_public_listings AS '
                            || regexp_replace(definition, '((l[.])?view_count[[:space:]]*,)', '\1 \2is_featured,');
                    END IF;
                END $$;
                """);
        }
    }
}
