using SQLMigration;

namespace JLPTWordbook.SQL.Migrations;

public class V3__AddExamResults : IScript
{
    public string Name => "AddExamResults";

    public int InstalledRank => 3;

    public string UpSql => @"
CREATE TABLE `exam_session` (
    `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `user_id` VARCHAR(256) NOT NULL,
    `class` VARCHAR(10) NOT NULL,
    `taken_at` DATETIME NOT NULL DEFAULT NOW(),
    `total_count` INT NOT NULL,
    `correct_count` INT NOT NULL,
    INDEX `IDX__exam_session__user_id` (`user_id`),
    INDEX `IDX__exam_session__taken_at` (`taken_at`),
    CONSTRAINT `FK__exam_session__user` FOREIGN KEY (`user_id`) REFERENCES `user`(`id`) ON DELETE CASCADE
);

CREATE TABLE `exam_session_item` (
    `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `session_id` BIGINT NOT NULL,
    `word_index` INT NOT NULL,
    `user_answer` VARCHAR(1024),
    `is_correct` TINYINT(1) NOT NULL,
    INDEX `IDX__exam_session_item__session_id` (`session_id`),
    CONSTRAINT `FK__exam_session_item__session` FOREIGN KEY (`session_id`) REFERENCES `exam_session`(`id`) ON DELETE CASCADE
);";

    public string DownSql => @"
DROP TABLE `exam_session_item`;
DROP TABLE `exam_session`;
";
}
