-- Migration Script: Convert BlockchainData to Raw JSON Storage
-- This script migrates the existing schema to store raw JSON data

-- Step 1: Create a backup table (optional but recommended)
CREATE TABLE IF NOT EXISTS BlockchainData_Backup AS 
SELECT * FROM BlockchainData;

-- Step 2: Create new table with simplified schema
CREATE TABLE BlockchainData_New (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Chain TEXT NOT NULL,
    Network TEXT NOT NULL,
    RawJsonData TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);

-- Step 3: Create index for performance
CREATE INDEX IX_BlockchainData_New_Chain_CreatedAt 
ON BlockchainData_New (Chain, CreatedAt);

-- Step 4: Migrate existing data (if any)
-- Note: This is a placeholder - adjust based on your current data structure
-- If you have existing data, you may need to reconstruct the JSON or clear the table

-- Step 5: Drop old table and rename new table
DROP TABLE BlockchainData;
ALTER TABLE BlockchainData_New RENAME TO BlockchainData;

-- Note: Since the app uses db.Database.EnsureCreated(), 
-- you can simply delete the existing database file and let it recreate
-- with the new schema on next run.
