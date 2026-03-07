using SQLMigration;

namespace JLPTWordbook.SQL.Migrations;

public class V2__AddMemorizedWords : IScript
{
    public string Name => "AddMemorizedWords";

    public int InstalledRank => 2;

    public string UpSql => @"
CREATE TABLE `memorized_word` (
    `user_id` VARCHAR(256) NOT NULL,
    `class` VARCHAR(10) NOT NULL,
    `word_index` INT NOT NULL,
    `memorized_at` DATETIME NOT NULL DEFAULT NOW(),
    PRIMARY KEY (`user_id`, `class`, `word_index`),
    INDEX `IDX__memorized_word__user_class` (`user_id`, `class`),
    CONSTRAINT `FK__memorized_word__user` FOREIGN KEY (`user_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
);";

    public string DownSql => @"
DROP TABLE `memorized_word`;
";
}
