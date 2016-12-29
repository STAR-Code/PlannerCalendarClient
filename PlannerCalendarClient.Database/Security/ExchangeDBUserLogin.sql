CREATE LOGIN [ExchangeDBUser] 
       WITH PASSWORD=N'adgangskode', 
       DEFAULT_DATABASE=[$(DatabaseName)], 
	   DEFAULT_LANGUAGE=[Dansk], 
	   CHECK_EXPIRATION=OFF, 
	   CHECK_POLICY=OFF

GO

CREATE USER [ExchangeDBUser] FOR LOGIN [ExchangeDBUser] WITH DEFAULT_SCHEMA=dbo
GO
ALTER ROLE [db_datareader] ADD MEMBER [ExchangeDBUser]
GO
ALTER ROLE [db_datawriter] ADD MEMBER [ExchangeDBUser]
GO
