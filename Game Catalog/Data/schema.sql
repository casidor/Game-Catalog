PRAGMA foreign_keys = ON;

-- Reference tables

CREATE TABLE Genre (
    genre_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    genre_name        TEXT NOT NULL COLLATE NOCASE UNIQUE,
    genre_description TEXT,
    CHECK (length(trim(genre_name)) > 0),
    CHECK (genre_name = trim(genre_name)),
    CHECK (genre_description IS NULL OR length(trim(genre_description)) > 0)
) STRICT;

CREATE TABLE Studio (
    studio_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    studio_name     TEXT NOT NULL COLLATE NOCASE UNIQUE,
    country         TEXT,
    foundation_year INTEGER,
    main_genre_id   INTEGER,
    website         TEXT,
    CHECK (length(trim(studio_name)) > 0),
    CHECK (studio_name = trim(studio_name)),
    CHECK (country IS NULL OR length(trim(country)) > 0),
    CHECK (foundation_year IS NULL OR foundation_year BETWEEN 1800 AND 2100),
    CHECK (website IS NULL OR length(trim(website)) > 0),
    FOREIGN KEY (main_genre_id) REFERENCES Genre(genre_id) ON DELETE RESTRICT ON UPDATE CASCADE
) STRICT;

CREATE TABLE Platform (
    platform_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    platform_name      TEXT NOT NULL COLLATE NOCASE UNIQUE,
    manufacturer       TEXT,
    platform_icon_path TEXT,
    CHECK (length(trim(platform_name)) > 0),
    CHECK (platform_name = trim(platform_name)),
    CHECK (manufacturer IS NULL OR length(trim(manufacturer)) > 0),
    CHECK (platform_icon_path IS NULL OR (platform_icon_path LIKE 'media/_%'
        AND (platform_icon_path LIKE '%.png' OR platform_icon_path LIKE '%.jpg'
          OR platform_icon_path LIKE '%.jpeg' OR platform_icon_path LIKE '%.webp')
        AND instr(platform_icon_path, '..') = 0 AND instr(platform_icon_path, char(92)) = 0))
) STRICT;

CREATE TABLE Tag (
    tag_id   INTEGER PRIMARY KEY AUTOINCREMENT,
    tag_name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    color    TEXT,
    CHECK (length(trim(tag_name)) > 0),
    CHECK (tag_name = trim(tag_name)),
    CHECK (color IS NULL OR color IN (
    'Red', 'Orange', 'Amber', 'Yellow', 'Lime', 'Green', 'Teal', 'Cyan',
    'Sky', 'Blue', 'Indigo', 'Violet', 'Fuchsia', 'Pink', 'Brown', 'Olive', 'Gray'))
) STRICT;

-- Game

CREATE TABLE Game (
    game_id               INTEGER PRIMARY KEY AUTOINCREMENT,
    developer_id          INTEGER NOT NULL,
    parent_game_id        INTEGER,
    game_title            TEXT NOT NULL,
    game_description      TEXT,
    release_year          INTEGER,
    game_status           TEXT NOT NULL,
    size_gb               REAL,
    personal_rating       INTEGER,
    cover_image_path      TEXT,
    background_image_path TEXT,
    executable_path       TEXT,
    install_path          TEXT,
    icon_path             TEXT,
    added_at              TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')),
    is_archived           INTEGER NOT NULL DEFAULT 0,
    CHECK (length(trim(game_title)) > 0),
    CHECK (game_title = trim(game_title)),
    CHECK (game_description IS NULL OR length(trim(game_description)) > 0),
    CHECK (release_year IS NULL OR release_year BETWEEN 1900 AND 2100),
    CHECK (cover_image_path IS NULL OR (cover_image_path LIKE 'media/_%'
        AND (cover_image_path LIKE '%.png' OR cover_image_path LIKE '%.jpg'
          OR cover_image_path LIKE '%.jpeg' OR cover_image_path LIKE '%.webp')
        AND instr(cover_image_path, '..') = 0 AND instr(cover_image_path, char(92)) = 0)),
    CHECK (background_image_path IS NULL OR (background_image_path LIKE 'media/_%'
        AND (background_image_path LIKE '%.png' OR background_image_path LIKE '%.jpg'
          OR background_image_path LIKE '%.jpeg' OR background_image_path LIKE '%.webp')
        AND instr(background_image_path, '..') = 0 AND instr(background_image_path, char(92)) = 0)),
    CHECK (executable_path IS NULL OR length(trim(executable_path)) > 0),
    CHECK (install_path IS NULL OR length(trim(install_path)) > 0),
    CHECK (icon_path IS NULL OR (icon_path LIKE 'media/_%'
        AND (icon_path LIKE '%.png' OR icon_path LIKE '%.jpg'
          OR icon_path LIKE '%.jpeg' OR icon_path LIKE '%.webp')
        AND instr(icon_path, '..') = 0 AND instr(icon_path, char(92)) = 0)),
    CHECK (datetime(added_at) IS added_at),
    CHECK (parent_game_id IS NULL OR parent_game_id != game_id),
    CHECK (game_status IN ('Planned', 'Playing', 'Completed', 'Abandoned')),
    CHECK (personal_rating IS NULL OR personal_rating BETWEEN 1 AND 10),
    CHECK (size_gb IS NULL OR size_gb >= 0),
    CHECK (size_gb IS NULL OR install_path IS NOT NULL OR executable_path IS NOT NULL),
    CHECK (is_archived IN (0, 1)),
    CHECK (parent_game_id IS NULL OR is_archived = 0),
    CHECK (parent_game_id IS NULL
        OR (install_path IS NULL AND executable_path IS NULL AND size_gb IS NULL)),
    FOREIGN KEY (developer_id)   REFERENCES Studio(studio_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (parent_game_id) REFERENCES Game(game_id)     ON DELETE CASCADE  ON UPDATE CASCADE
) STRICT;

-- Link tables

CREATE TABLE GameGenre (
    game_id  INTEGER NOT NULL,
    genre_id INTEGER NOT NULL,
    PRIMARY KEY (game_id, genre_id),
    FOREIGN KEY (game_id)  REFERENCES Game(game_id)   ON DELETE CASCADE  ON UPDATE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES Genre(genre_id) ON DELETE RESTRICT ON UPDATE CASCADE
) STRICT, WITHOUT ROWID;

CREATE TABLE GamePlatform (
    game_id     INTEGER NOT NULL,
    platform_id INTEGER NOT NULL,
    PRIMARY KEY (game_id, platform_id),
    FOREIGN KEY (game_id)     REFERENCES Game(game_id)         ON DELETE CASCADE  ON UPDATE CASCADE,
    FOREIGN KEY (platform_id) REFERENCES Platform(platform_id) ON DELETE RESTRICT ON UPDATE CASCADE
) STRICT, WITHOUT ROWID;

CREATE TABLE GameTag (
    game_id INTEGER NOT NULL,
    tag_id  INTEGER NOT NULL,
    PRIMARY KEY (game_id, tag_id),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (tag_id)  REFERENCES Tag(tag_id)   ON DELETE CASCADE ON UPDATE CASCADE
) STRICT, WITHOUT ROWID;

-- Player activity

CREATE TABLE PlaySession (
    play_session_id  INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id          INTEGER NOT NULL,
    start_time       TEXT NOT NULL,
    end_time         TEXT,
    entry_method     TEXT NOT NULL DEFAULT 'Manual',
    note             TEXT,
    duration_seconds INTEGER GENERATED ALWAYS AS (
        CAST(strftime('%s', end_time) AS INTEGER) - CAST(strftime('%s', start_time) AS INTEGER)
    ) VIRTUAL,
    CHECK (datetime(start_time) IS start_time),
    CHECK (end_time IS NULL OR datetime(end_time) IS end_time),
    CHECK (end_time IS NULL OR end_time > start_time),
    CHECK (end_time IS NULL OR duration_seconds <= 604800),
    CHECK (entry_method IN ('Manual', 'Auto')),
    CHECK (note IS NULL OR length(trim(note)) > 0),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

CREATE TABLE Goal (
    goal_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id          INTEGER NOT NULL,
    goal_name        TEXT NOT NULL,
    goal_description TEXT,
    goal_status      TEXT NOT NULL DEFAULT 'Active',
    priority         INTEGER NOT NULL,
    created_at       TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')),
    due_date         TEXT,
    completed_at     TEXT,
    CHECK (length(trim(goal_name)) > 0),
    CHECK (goal_name = trim(goal_name)),
    CHECK (goal_description IS NULL OR length(trim(goal_description)) > 0),
    CHECK (goal_status IN ('Active', 'Completed', 'Cancelled')),
    CHECK (priority BETWEEN 1 AND 5),
    CHECK (datetime(created_at) IS created_at),
    CHECK (due_date IS NULL OR date(due_date, '+0 days') IS due_date),
    CHECK (due_date IS NULL OR due_date >= date(created_at)),
    CHECK (completed_at IS NULL OR datetime(completed_at) IS completed_at),
    CHECK ((goal_status = 'Completed' AND completed_at IS NOT NULL)
        OR (goal_status <> 'Completed' AND completed_at IS NULL)),
    CHECK (completed_at IS NULL OR completed_at >= created_at),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

-- External services

CREATE TABLE GameExternalId (
    game_external_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id                 INTEGER NOT NULL,
    provider_name           TEXT NOT NULL,
    external_id             TEXT NOT NULL,
    last_synced_at          TEXT,
    page_url                TEXT,
    source_playtime_minutes INTEGER,
    CHECK (provider_name IN ('IGDB', 'Steam')),
    CHECK (length(trim(external_id)) > 0),
    CHECK (external_id = trim(external_id)),
    CHECK (last_synced_at IS NULL OR datetime(last_synced_at) IS last_synced_at),
    CHECK (page_url IS NULL OR length(trim(page_url)) > 0),
    CHECK (source_playtime_minutes IS NULL OR source_playtime_minutes >= 0),
    UNIQUE (game_id, provider_name),
    UNIQUE (provider_name, external_id),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

CREATE TABLE Achievement (
    achievement_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id                 INTEGER NOT NULL,
    steam_achievement_code  TEXT NOT NULL,
    achievement_name        TEXT NOT NULL,
    achievement_description TEXT,
    unlocked_at             TEXT,
    achievement_icon_path   TEXT,
    global_unlock_percent   REAL,
    CHECK (length(trim(steam_achievement_code)) > 0),
    CHECK (steam_achievement_code = trim(steam_achievement_code)),
    CHECK (length(trim(achievement_name)) > 0),
    CHECK (achievement_name = trim(achievement_name)),
    CHECK (achievement_description IS NULL OR length(trim(achievement_description)) > 0),
    CHECK (unlocked_at IS NULL OR datetime(unlocked_at) IS unlocked_at),
    CHECK (achievement_icon_path IS NULL OR (achievement_icon_path LIKE 'media/_%'
        AND (achievement_icon_path LIKE '%.png' OR achievement_icon_path LIKE '%.jpg'
          OR achievement_icon_path LIKE '%.jpeg' OR achievement_icon_path LIKE '%.webp')
        AND instr(achievement_icon_path, '..') = 0 AND instr(achievement_icon_path, char(92)) = 0)),
    CHECK (global_unlock_percent IS NULL OR global_unlock_percent BETWEEN 0 AND 100),
    UNIQUE (game_id, steam_achievement_code),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

-- Indexes

CREATE UNIQUE INDEX ux_game_identity
    ON Game (game_title COLLATE NOCASE, developer_id, COALESCE(release_year, 0));

CREATE UNIQUE INDEX ux_game_install_path ON Game (install_path);

CREATE INDEX ix_studio_main_genre  ON Studio (main_genre_id);
CREATE INDEX ix_game_developer     ON Game (developer_id);
CREATE INDEX ix_game_parent        ON Game (parent_game_id);
CREATE INDEX ix_game_archived      ON Game (is_archived);
CREATE INDEX ix_gamegenre_genre    ON GameGenre (genre_id);
CREATE INDEX ix_gameplatform_plat  ON GamePlatform (platform_id);
CREATE INDEX ix_gametag_tag        ON GameTag (tag_id);
CREATE INDEX ix_session_game_duration ON PlaySession (game_id, duration_seconds);
CREATE INDEX ix_session_game_start    ON PlaySession (game_id, start_time);
CREATE INDEX ix_session_start_time ON PlaySession (start_time);
CREATE INDEX ix_goal_game          ON Goal (game_id);
CREATE INDEX ix_goal_due_date      ON Goal (due_date);
CREATE INDEX ix_achievement_game   ON Achievement (game_id);

-- Triggers

CREATE TRIGGER trg_session_no_overlap_insert
BEFORE INSERT ON PlaySession
BEGIN
    SELECT RAISE(ABORT, 'session_overlap')
    WHERE EXISTS (
        SELECT 1
        FROM PlaySession s
        WHERE s.game_id = NEW.game_id
          AND s.start_time < COALESCE(NEW.end_time, '9999-12-31 23:59:59')
          AND COALESCE(s.end_time, '9999-12-31 23:59:59') > NEW.start_time
    );
END;

CREATE TRIGGER trg_session_no_overlap_update
BEFORE UPDATE OF game_id, start_time, end_time ON PlaySession
BEGIN
    SELECT RAISE(ABORT, 'session_overlap')
    WHERE EXISTS (
        SELECT 1
        FROM PlaySession s
        WHERE s.game_id = NEW.game_id
          AND s.play_session_id <> NEW.play_session_id
          AND s.start_time < COALESCE(NEW.end_time, '9999-12-31 23:59:59')
          AND COALESCE(s.end_time, '9999-12-31 23:59:59') > NEW.start_time
    );
END;

CREATE TRIGGER trg_session_not_in_future_insert
BEFORE INSERT ON PlaySession
WHEN NEW.start_time > strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
  OR NEW.end_time   > strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
BEGIN
    SELECT RAISE(ABORT, 'session_in_future');
END;

CREATE TRIGGER trg_session_not_in_future_update
BEFORE UPDATE OF start_time, end_time ON PlaySession
WHEN NEW.start_time > strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
  OR NEW.end_time   > strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')
BEGIN
    SELECT RAISE(ABORT, 'session_in_future');
END;

CREATE TRIGGER trg_session_game_insert
BEFORE INSERT ON PlaySession
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_session_game_update
BEFORE UPDATE ON PlaySession
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id IN (OLD.game_id, NEW.game_id) AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_goal_game_insert
BEFORE INSERT ON Goal
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_goal_game_update
BEFORE UPDATE ON Goal
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id IN (OLD.game_id, NEW.game_id) AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_achievement_game_insert
BEFORE INSERT ON Achievement
BEGIN
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_achievement_game_update
BEFORE UPDATE OF game_id ON Achievement
BEGIN
    SELECT RAISE(ABORT, 'dlc_no_activity')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_game_dlc_insert
BEFORE INSERT ON Game
WHEN NEW.parent_game_id IS NOT NULL
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.parent_game_id AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_parent_is_dlc')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.parent_game_id AND parent_game_id IS NOT NULL);
END;

CREATE TRIGGER trg_game_dlc_update
BEFORE UPDATE OF parent_game_id ON Game
WHEN NEW.parent_game_id IS NOT NULL
BEGIN
    SELECT RAISE(ABORT, 'game_archived')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.parent_game_id AND is_archived = 1);
    SELECT RAISE(ABORT, 'dlc_parent_is_dlc')
    WHERE EXISTS (SELECT 1 FROM Game WHERE game_id = NEW.parent_game_id AND parent_game_id IS NOT NULL);
    SELECT RAISE(ABORT, 'game_has_dlc')
    WHERE EXISTS (SELECT 1 FROM Game WHERE parent_game_id = NEW.game_id);
    SELECT RAISE(ABORT, 'dlc_has_activity')
    WHERE EXISTS (SELECT 1 FROM PlaySession WHERE game_id = NEW.game_id)
       OR EXISTS (SELECT 1 FROM Goal WHERE game_id = NEW.game_id)
       OR EXISTS (SELECT 1 FROM Achievement WHERE game_id = NEW.game_id);
END;

CREATE TRIGGER trg_game_dlc_of_archived_update
BEFORE UPDATE ON Game
WHEN OLD.parent_game_id IS NOT NULL
 AND EXISTS (SELECT 1 FROM Game WHERE game_id = OLD.parent_game_id AND is_archived = 1)
BEGIN
    SELECT RAISE(ABORT, 'game_archived');
END;

CREATE TRIGGER trg_game_archive_active_session
BEFORE UPDATE OF is_archived ON Game
WHEN NEW.is_archived = 1 AND OLD.is_archived = 0
 AND EXISTS (SELECT 1 FROM PlaySession WHERE game_id = NEW.game_id AND end_time IS NULL)
BEGIN
    SELECT RAISE(ABORT, 'game_has_active_session');
END;

-- Views

CREATE VIEW v_game AS
SELECT g.game_id, g.developer_id, s.studio_name AS developer_name, g.parent_game_id,
       g.game_title, g.game_description, g.release_year, g.game_status, g.size_gb,
       g.personal_rating, g.cover_image_path, g.background_image_path,
       g.executable_path, g.install_path, g.icon_path, g.added_at, g.is_archived,
       COALESCE(t.total_seconds, 0) / 3600.0 AS total_hours,
       CAST(julianday('now', 'localtime') - julianday(g.added_at) AS INTEGER) AS days_in_library
FROM Game g
JOIN Studio s ON s.studio_id = g.developer_id
LEFT JOIN (
    SELECT game_id, SUM(duration_seconds) AS total_seconds
    FROM PlaySession
    GROUP BY game_id
) t ON t.game_id = g.game_id;

CREATE VIEW v_play_session AS
SELECT ps.play_session_id, ps.game_id, g.game_title, g.is_archived, ps.start_time, ps.end_time,
       ps.duration_seconds, ps.entry_method, ps.note
FROM PlaySession ps
JOIN Game g ON g.game_id = ps.game_id;

CREATE VIEW v_goal AS
SELECT gl.goal_id, gl.game_id, g.game_title, g.is_archived,
       gl.goal_name, gl.goal_description, gl.goal_status,
       CASE
           WHEN gl.goal_status = 'Active' AND gl.due_date < date('now', 'localtime') THEN 'Overdue'
           ELSE gl.goal_status
       END AS display_status,
       gl.priority, gl.created_at, gl.due_date, gl.completed_at,
       CASE
           WHEN gl.goal_status = 'Active' AND gl.due_date IS NOT NULL
           THEN MAX(0, CAST(julianday(gl.due_date) - julianday(date('now', 'localtime')) AS INTEGER))
       END AS days_left,
       CASE
           WHEN gl.goal_status = 'Completed'
           THEN CAST(julianday(gl.completed_at) - julianday(gl.created_at) AS INTEGER)
       END AS completion_days
FROM Goal gl
JOIN Game g ON g.game_id = gl.game_id;