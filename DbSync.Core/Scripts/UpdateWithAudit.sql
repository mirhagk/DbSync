UPDATE t
SET @columnUpdateList,
@modifiedUser = SUSER_NAME(),
@modifiedDate = GETDATE()
FROM @target t
INNER JOIN @source s ON t.@id = s.@id