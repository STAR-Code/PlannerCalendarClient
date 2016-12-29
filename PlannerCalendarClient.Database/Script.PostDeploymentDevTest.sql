/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

IF NOT EXISTS(
		SELECT * 
		FROM [PlannerResource]
		)
BEGIN
	-- Add some test mails that are defined on the nnit.com exchange server.
	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  'FC7DC8A9-C6C5-443A-B448-4B905661508D',
			  'noreply-1@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  null )

	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  '997116D7-401A-46C0-B9A6-3BE312789523',
			  'noreply-2@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  null )

	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  'EAA72CFE-70DC-4C2E-9423-EE6E463D7077',
			  'noreply-3@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  null )

	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  '40AEEADC-FA4C-4CA1-9AD4-1043D04D259B',
			  'noreply-4@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  null )

	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  '2D360B87-D108-41BA-9801-EAA0C4ED2105',
			  'noreply-5@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  null )

	INSERT [PlannerResource] ([PlannerResourceID],[MailAddress],[GroupAffinity],[CreatedDate],[UpdatedDate],[DeletedDate])
	VALUES ( 
			  '{0DCB56F1-C34A-4790-ABEE-CAE804038F33}',
			  'noreply-6@star.dk',
			  'GroupA',
			  '2014-01-01 01:30:00',
			  null,
			  '2015-01-01 10:00:00' )
END

IF NOT EXISTS (SELECT TOP 1 1 FROM [dbo].[ServiceUserCredential]) 
BEGIN
	INSERT INTO [dbo].[ServiceUserCredential] ([UserId], [Password])
	VALUES ('', '')
END
