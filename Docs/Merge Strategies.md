Merge Strategies
===

DbSync supports several different strategies for merging. These control what DbSync should do when it encounter differences, and how it should behave. The default is `MergeWithDelete` which attempts to make the target identical to the source. If you want to specify an different merge strategy you can specify it at either the table or the job level.

Example Format
---

**Supports Export**: Whether you can use this merge strategy while exporting to files

**Roundtrip**: If you perform an import followed by an export and the data files remain identical then this strategy does roundtrips.

**Idempotent**: If it's idempotent then you can run it multiple times and it'll be no different then running it once.

**Description**: A description of what this merge strategy does.

MergeWithDelete
---

Default.

**Supports Export**: Yes

**Roundtrip**: Yes

**Idempotent**: Yes

**Description**: Adds any records not in the target, removes any records not in the source, and updates any records in both that are different.

MergeWithoutDelete
---

**Supports Export**: Yes

**Roundtrip**: No

**Idempotent**: Yes

**Description**: Same as above except it does not delete records that weren't found in the source

AddOnly
---

**Supports Export**: Yes

**Roundtrip**: No

**Idempotent**: Yes

**Description**: This strategy only adds new records. Any existing records are left untouched.

DropReadd
---

**Supports Export**: Yes

**Roundtrip**: Yes

**Idempotent**: No (audit records and triggers may cause database changes)

**Description**: This strategy drops all the target records and then adds all the source records. This strategy is useful when you are having issues with unique key changes

Overwrite
---

**Supports Export**: No (perhaps in the future, but it is a difficult change to implement)

**Roundtrip**: No

**Idempotent**: Yes

**Description**: Adds any new records and updates any existing records with just the field changes. Null fields are ignored. This one is useful when you only care about some of the fields, although be careful as this doesn't allow exporting (even if another strategy is used for export). A better solution may come in the future which involves alternate schemas for tables.