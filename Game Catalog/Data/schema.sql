PRAGMA foreign_keys = ON;

CREATE TABLE Studio (
    studio_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    name            TEXT NOT NULL COLLATE NOCASE UNIQUE,
    country         TEXT,
    foundation_year INTEGER,
    main_genre      TEXT,
    website         TEXT,
    CHECK (length(trim(name)) > 0)
) STRICT;

CREATE TABLE Game (
    game_id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    developer_id            INTEGER NOT NULL,
    parent_game_id          INTEGER,
    title                   TEXT NOT NULL,
    genre                   TEXT,
    platform                TEXT,
    description             TEXT,
    release_year            INTEGER,
    status                  TEXT NOT NULL DEFAULT 'Planned',
    size_gb                 REAL,
    personal_rating         INTEGER NOT NULL DEFAULT 5,
    cover_image_path        TEXT,
    background_image_path   TEXT,
    executable_path         TEXT,
    icon_path               TEXT,
    added_at                TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%S', 'now')),
    is_archived             INTEGER NOT NULL DEFAULT 0,
    CHECK (length(trim(title)) > 0),
    CHECK (parent_game_id IS NULL OR parent_game_id != game_id),
    CHECK (status IN ('Planned', 'Playing', 'Completed', 'Abandoned')),
    CHECK (personal_rating BETWEEN 1 AND 10),
    CHECK (size_gb IS NULL OR size_gb >= 0),
    CHECK (is_archived IN (0, 1)),
    FOREIGN KEY (developer_id)   REFERENCES Studio(studio_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (parent_game_id) REFERENCES Game(game_id)     ON DELETE CASCADE  ON UPDATE CASCADE
) STRICT;

CREATE TABLE PlaySession (
    play_session_id INTEGER PRIMARY KEY AUTOINCREMENT,
    game_id         INTEGER NOT NULL,
    start_time      TEXT NOT NULL,
    end_time        TEXT,
    entry_method    TEXT NOT NULL DEFAULT 'Manual',
    note            TEXT,
    CHECK (end_time IS NULL OR end_time >= start_time),
    CHECK (entry_method IN ('Manual', 'Auto')),
    FOREIGN KEY (game_id) REFERENCES Game(game_id) ON DELETE CASCADE ON UPDATE CASCADE
) STRICT;

CREATE UNIQUE INDEX ux_game_identity
    ON Game (title COLLATE NOCASE, developer_id, COALESCE(release_year, 0));

CREATE INDEX ix_game_developer     ON Game (developer_id);
CREATE INDEX ix_game_parent        ON Game (parent_game_id);
CREATE INDEX ix_session_game       ON PlaySession (game_id);
CREATE INDEX ix_session_start_time ON PlaySession (start_time);