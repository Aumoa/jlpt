using SQLMigration;

namespace JLPTWordbook.SQL.Migrations;

public class V1__Init : IScript
{
    public string Name => "Init";

    public int InstalledRank => 1;

    public string UpSql => @"
CREATE TABLE `user` (
	`id` VARCHAR(256) NOT NULL PRIMARY KEY,
    `name_hint` VARCHAR(256),
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `last_logged_at` DATETIME NOT NULL DEFAULT NOW(),
    INDEX `IDX__user__created_at` (`created_at`),
    INDEX `IDX__user__last_logged_at` (`last_logged_at`)
);";

    public string DownSql => @"
DROP TABLE `user`;
";
}
