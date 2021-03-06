IF NOT EXISTS (
		SELECT [Id]
		FROM [ServiceJob]
		WHERE [Class] = 'Rock.Jobs.PostV90DataMigrations'
			AND [Guid] = '3F279016-C7D1-490F-835D-8FFE6D943A32'
		)
BEGIN
	INSERT INTO [ServiceJob] (
		[IsSystem]
		,[IsActive]
		,[Name]
		,[Description]
		,[Class]
		,[CronExpression]
		,[NotificationStatus]
		,[Guid]
		)
	VALUES (
		0
		,1
		,'Runs data updates that need to occur after updating to v9.0'
		,'This job will take care of any data migrations that need to occur after updating to v9.0. After all the operations are done, this job will delete itself.'
		,'Rock.Jobs.PostV90DataMigrations'
		,'0 0 21 1/1 * ? *'
		,1
		,'3F279016-C7D1-490F-835D-8FFE6D943A32'
		);
END
