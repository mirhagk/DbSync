DbSync Sync
===

This tool will automatically determine which tables need to be imported or exported. It will require some extra tables in the target database.

> Design Consideration: If the tables don't exist, should it fail? (probably not) Should it simply create those tables and assume you are doing an import? Or should it assume an export? (an import is probably the intended one, but an export is safer when you have source control)

> Design Consideration: Should it support storing the information in xml files instead of db? (Probably not because the files would need to be ignored by source control) 


Besides requiring the configuration tables, it also requires that auditing columns are turned on for the tables (otherwise it has no way to know whether export or import is required)

Table(s)
---

The table structure will look like the following:

Column Name | Data Type | Description
---|----|---
TableName|nvarchar(255) (primary key)|the table that is to be checked for in-syncness
LastSyncTime|datetime|The last time you ran sync on that table
LastSyncFileTime|datetime|The timestamp of the file when you last ran the sync (updated after the last sync)
LastSyncDbAuditTime|datetime|The `MAX(ModifiedDate)` of the table when you last ran the sync (updated after the last sync)

Logic for determining if import/export is required
---

Each table is considered separately.

1. Take `TableTime` and `FileTime` - the `MAX(ModifiedDate)` of the table and the last modified time of the file respectively
2. Use the recorded values in the dbsync config table to determine if either have been modified
3. If neither has been modified then nothing needs to be done with this table
2. If only the database, or only the file has been changed then export or import respectively.
3. Otherwise report a conflict in that table, and continue with the rest of the tables (user must resolve the conflict in order to continue)


> Design Consideration: More complex merging processes may become available in the future. e.g. using source control to determine which records have been altered. Potentially it could still update any records that haven't been modified in the DB and report which records are in conflict.

> Design Consideration: If the user is using source control then the best way to resolve the conflicts would be to export the database records and use the existing source control tools to decipher which ones to keep and which to throw out

> Design Consideration: The tool could import to the database, but keep any modified records. Then it could export those modified records. This could end up resolving most issues, the user would just have to be careful to make sure that their changed records are correct.



