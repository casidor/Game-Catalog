using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace Game_Catalog.Data.Repositories
{
    /// <summary> Queries about media files referenced by the database. </summary>
    public static class MediaRepository
    {
        /// <summary> Returns every media path stored in the database, without repeats. </summary>
        public static List<string> GetReferencedPaths() =>
            Db.Run("Не вдалося прочитати шляхи до зображень", () =>
            {
                using var connection = Db.Open();
                // UNION removes repeats; a WHERE in each branch drops empty values.
                return connection.Query<string>(@"
                    SELECT cover_image_path FROM Game WHERE cover_image_path IS NOT NULL
                    UNION
                    SELECT background_image_path FROM Game WHERE background_image_path IS NOT NULL
                    UNION
                    SELECT icon_path FROM Game WHERE icon_path IS NOT NULL
                    UNION
                    SELECT platform_icon_path FROM Platform WHERE platform_icon_path IS NOT NULL
                    UNION
                    SELECT achievement_icon_path FROM Achievement WHERE achievement_icon_path IS NOT NULL").ToList();
            });
    }
}
