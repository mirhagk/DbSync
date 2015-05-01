UPDATE t
SET @columnUpdateList,
@modifiedUser = SUSER_NAME(),
@modifiedDate = GETDATE()
FROM @target t
INNER JOIN @source s ON t.@id = s.@id


SET IDENTITY_INSERT @target ON

INSERT INTO @target (@id, @columns, @createdUser, @createdDate, @modifiedUser, @modifiedDate)
SELECT @id, @columns, SUSER_NAME(), GETDATE(), SUSER_NAME(), GETDATE()
FROM @source s
WHERE s.@id NOT IN (SELECT @id FROM @target t)

SET IDENTITY_INSERT @target OFF

DELETE FROM @target
WHERE @target.@id NOT IN (SELECT @id FROM @source)
