UPDATE t
SET @columnUpdateList
FROM @target t
INNER JOIN @source s ON t.@id = s.@id