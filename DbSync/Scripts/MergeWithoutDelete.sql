UPDATE t
SET @columnUpdateList
FROM @target t
INNER JOIN @source s ON t.@id = s.@id


SET IDENTITY_INSERT @target ON

INSERT INTO @target (@id, @columns)
SELECT @id, @columns
FROM @source s
WHERE s.@id NOT IN (SELECT @id FROM @target t)

SET IDENTITY_INSERT @target OFF