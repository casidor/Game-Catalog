PRAGMA foreign_keys = ON;

-- Reference tables

CREATE TABLE Genre (
    genre_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    genre_name        TEXT NOT NULL COLLATE NOCASE UNIQUE,
    genre_description TEXT,
    CHECK (length(trim(genre_name)) > 0)
) STRICT;

CREATE TABLE Studio (
    studio_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    studio_name     TEXT NOT NULL COLLATE NOCASE UNIQUE,
    country         TEXT,
    foundation_year INTEGER,
    main_genre      INTEGER,
    website         TEXT,
    CHECK (length(trim(studio_name)) > 0),
    FOREIGN KEY (main_genre) REFERENCES Genre(genre_id) ON DELETE RESTRICT ON UPDATE CASCADE
) STRICT;

CREATE TABLE Platform (
    platform_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    platform_name      TEXT NOT NULL COLLATE NOCASE UNIQUE,
    manufacturer       TEXT,
    platform_icon_path TEXT,
    CHECK (length(trim(platform_name)) > 0)
) STRICT;

CREATE TABLE Tag (
    tag_id   INTEGER PRIMARY KEY AUTOINCREMENT,
    tag_name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    color    TEXT,
    CHECK (length(trim(tag_name)) > 0),
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
    CHECK (parent_game_id IS NULL OR parent_game_id != game_id),
    CHECK (game_status IN ('Planned', 'Playing', 'Completed', 'Abandoned')),
    CHECK (personal_rating IS NULL OR personal_rating BETWEEN 1 AND 10),
    CHECK (size_gb IS NULL OR size_gb >= 0),
    CHECK (size_gb IS NULL OR install_path IS NOT NULL),
    CHECK (is_archived IN (0, 1)),
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
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

CREATE TABLE Goal (
    goal_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id          INTEGER NOT NULL,
    goal_name        TEXT NOT NULL,
    goal_description TEXT,
    goal_status      TEXT NOT NULL,
    priority         INTEGER NOT NULL,
    created_at       TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime')),
    due_date         TEXT,
    completed_at     TEXT,
    CHECK (length(trim(goal_name)) > 0),
    CHECK (goal_status IN ('Active', 'Completed', 'Cancelled')),
    CHECK (priority BETWEEN 1 AND 5),
    CHECK (completed_at IS NULL OR goal_status = 'Completed'),
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
    CHECK (global_unlock_percent IS NULL OR global_unlock_percent BETWEEN 0 AND 100),
    UNIQUE (game_id, steam_achievement_code),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

-- Indexes

CREATE UNIQUE INDEX ux_game_identity
    ON Game (game_title COLLATE NOCASE, developer_id, COALESCE(release_year, 0));

CREATE INDEX ix_studio_main_genre  ON Studio (main_genre);
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
SELECT ps.play_session_id, ps.game_id, g.game_title, ps.start_time, ps.end_time,
       ps.duration_seconds, ps.entry_method, ps.note
FROM PlaySession ps
JOIN Game g ON g.game_id = ps.game_id;