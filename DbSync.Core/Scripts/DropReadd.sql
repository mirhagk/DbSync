﻿DELETE FROM @target

IF OBJECTPROPERTY(OBJECT_ID('@target'), 'TableHasIdentity') = 1
	SET IDENTITY_INSERT @target ON

INSERT INTO @target(@id, @columns)
SELECT @id, @columns FROM @source


IF OBJECTPROPERTY(OBJECT_ID('@target'), 'TableHasIdentity') = 1
	SET IDENTITY_INSERT @target OFF