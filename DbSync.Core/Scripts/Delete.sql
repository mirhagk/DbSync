DELETE FROM @target
WHERE @target.@id NOT IN (SELECT @id FROM @source)