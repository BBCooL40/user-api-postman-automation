-- Inspect the Users table structure in SQLite
PRAGMA table_info('Users');

-- View all stored users
SELECT *
FROM Users;

-- Verify that a specific user exists after POST /api/users
SELECT *
FROM Users
WHERE Email = 'sqlproof.user@test.com';

-- Count matching users; expect 1 after POST and 0 after DELETE
SELECT COUNT(*) AS MatchingUsers
FROM Users
WHERE Email = 'sqlproof.user@test.com';
