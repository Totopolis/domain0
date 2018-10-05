if not exists (select top 1 1 from sys.schemas where name='dom')
	exec sp_executesql N'create schema dom' 
go

if not exists (select top 1 1 from sys.schemas where name='hst_dom')
	exec sp_executesql N'create schema hst_dom' 
go

if not exists (select top 1 1 from sys.schemas where name='log')
	exec sp_executesql N'create schema log' 
go

if object_id('dom.PermissionUser') is not null
	drop table dom.PermissionUser
go
if object_id('dom.RoleUser') is not null
	drop table dom.RoleUser
go
if object_id('dom.PermissionRole') is not null
	drop table dom.PermissionRole
go
if object_id('dom.Role') is not null
	drop table dom.Role
go
if object_id('dom.Permission') is not null
	drop table dom.Permission
go
if object_id('dom.Application') is not null
	drop table dom.Application
go
if object_id('dom.Account') is not null
	drop table dom.Account
go
if object_id('dom.SmsRequest') is not null
	drop table dom.SmsRequest
go
if object_id('dom.EmailRequest') is not null
	drop table dom.EmailRequest
go
if object_id('dom.Message') is not null
	drop table dom.Message
go
if object_id('dom.TokenRegistration') is not null
	drop table dom.TokenRegistration
go
if object_id('log.Access') is not null
	drop table log.Access
go

create table dom.Role (
	Id int not null identity(1,1) constraint PK_dom_Role primary key,
	Name nvarchar(64) not null,
	Description nvarchar(max) null,
	IsDefault bit not null default(0),

	constraint UQ_dom_Role unique(Name)
)
go
create index IX_Role_Name ON dom.Role ([Name])


create table dom.Application (
	Id int not null identity(1,1) constraint PK_dom_Application primary key,
	Name nvarchar(64) not null,
	Description nvarchar(max) null
)
go


create table dom.Permission (
	Id int not null identity(1,1) constraint PK_dom_Permission primary key,
	ApplicationId int not null constraint FK_dom_Permission_ApplicationId foreign key references dom.Application(Id),
	Name nvarchar(64) not null,
	Description nvarchar(max) null

	constraint UQ_dom_Permission unique(Name)
)
go
create index IX_Permission_Name ON dom.Permission ([Name])
go


create table dom.PermissionRole (
	PermissionId int not null constraint FK_dom_PermissionRole_PermissionId foreign key references dom.Permission(Id),
	RoleId int not null constraint FK_dom_PermissionRole_RoleId foreign key references dom.Role(Id)

	constraint PK_dom_PermissionRole primary key(PermissionId, RoleId)
)
go


create table dom.Account (
	[Id] int identity(1,1) not null constraint PK_Account_Id primary key,
	[Email] nvarchar(128) null,
	[Phone] decimal null,
	[Login] nvarchar(80) null,
	[Password] nvarchar(80) null,
	[Name] nvarchar(256) not null,
	[Description] nvarchar(max) null
)
go
create index IX_Account_Phone ON dom.Account ([Phone])
create index IX_Account_Email ON dom.Account ([Email])
create index IX_Account_Login ON dom.Account ([Login])
go


create table dom.RoleUser (
	RoleId int not null constraint FK_dom_RoleUser_RoleId foreign key references dom.Role(Id),
	UserId int not null constraint FK_dom_RoleUser_UserId foreign key references dom.Account(Id)

	constraint PK_dom_RoleUser primary key(RoleId, UserId)
)
go
create index IX_RoleUser_UserId ON dom.RoleUser ([UserId])
go


create table dom.PermissionUser (
	PermissionId int not null,
	UserId int not null,
	Since datetime2 null,
	Until datetime2 null

	constraint PK_dom_PermissionUser primary key(PermissionId, UserId)
)
go


create table dom.Message (
	Id int identity(1,1) not null constraint PK_Message_Id primary key,
	Description nvarchar(max) null,
	Type nvarchar(10) null,
	Locale nvarchar(3) null,
	Name nvarchar(256) not null,
	Template nvarchar(max) not null
)
go

insert into dom.Message 
([Type], [Locale], [Name], [Template])
values
('sms',		'en',		'WelcomeTemplate',		'Hello {0}!'),
('sms',		'en',		'RegisterTemplate',		'Your password is: {0} will valid for {1} min'),
('sms',		'en',		'RequestResetTemplate',	'Your NEW password is: {0} will valid for {1} min'),
('sms',		'ru',		'WelcomeTemplate',		'Добро пожаловать {0}!'),
('sms',		'ru',		'RegisterTemplate',		'Ваш пароль: {0} действителен {1} мин'),
('sms',		'ru',		'RequestResetTemplate',	'Ваш НОВЫЙ пароль: {0} действителен {1} мин'),

('email',	'en',		'WelcomeTemplate',		'Hello {0}!'),
('email',	'en',		'RegisterTemplate',		'Your password is: {0} will valid for {1} min'),
('email',	'en',		'RegisterSubjectTemplate',	'Dear {0}! Welcome to {1}'),
('email',	'en',		'RequestResetTemplate',	'Your NEW password is: {0} will valid for {1} min'),
('email',	'en',		'RequestResetSubjectTemplate',	'{0}.Change password for {1}'),
('email',	'ru',		'WelcomeTemplate',		'Добро пожаловать {0}!'),
('email',	'ru',		'RegisterTemplate',		'Ваш пароль: {0} действителен {1} мин'),
('email',	'ru',		'RegisterSubjectTemplate',	'{0}! Добро пожаловать в {1}'),
('email',	'ru',		'RequestResetTemplate',	'Ваш НОВЫЙ пароль: {0} действителен {1} мин'),
('email',	'ru',		'RequestResetSubjectTemplate',	'{0}. Изменение пароля для {1}')
go
create index IX_Message_Name_Type_Locale ON dom.Message ([Name] asc, [Type] asc, [Locale] asc)
go


create table dom.SmsRequest (
	[Id] int identity(1,1) not null constraint PK_SmsRequest_Id primary key,
	[Phone] decimal not null,
	[Password] nvarchar(80) not null,
	[ExpiredAt] datetime2 not null,
	[UserId] int null
)
go
create index IX_SmsRequest_Phone_ExpiredAt ON dom.SmsRequest ([Phone] ASC, [ExpiredAt] DESC)
go
create index IX_SmsRequest_UserId_ExpiredAt ON dom.SmsRequest ([UserId] ASC, [ExpiredAt] DESC)
go


create table dom.EmailRequest (
	[Id] int identity(1,1) not null constraint PK_EmailRequest_Id primary key,
	[Email] nvarchar(128) not null,
	[Password] nvarchar(80) not null,
	[ExpiredAt] datetime2 not null,
	[UserId] int null
)
go
create index IX_EmailRequest_Email_ExpiredAt ON dom.EmailRequest ([Email] ASC, [ExpiredAt] DESC)
go
create index IX_EmailRequest_UserId_ExpiredAt ON dom.EmailRequest ([UserId] ASC, [ExpiredAt] DESC)
go


create table dom.TokenRegistration (
	[Id] int identity(1,1) not null constraint PK_TokenRegistration_Id primary key,
	[UserId] int not null,
	[AccessToken] nvarchar(max) not null,
	[IssuedAt] datetime2 not null,
	[ExpiredAt] datetime2 null
)
go

/*
insert into [dom].[Application]
([Name], [Description])
values
('Domain0', 'Domain0 auth app')

declare @DomainAppId int = SCOPE_IDENTITY();

insert into [dom].[Permission]
([ApplicationId], [Name], [Description])
values
(@DomainAppId, 'Admin', 'Admin permission')
*/

create table log.Access(
	[Id] bigint identity(1,1) not null constraint PK_log_Access_Id primary key,
	[Action] nvarchar(max) not null,
	[Method] nvarchar(10) not null,
	[ClientIp] nvarchar(32) not null,
	[ProcessedAt] datetime not null,
	[StatusCode] int null,
	[UserAgent] nvarchar(128) not null,
	[UserId] nvarchar(64) null,
    [Referer] nvarchar(max) null,
	[ProcessingTime] int null
)
go

DROP TABLE [hst_dom].[TokenRegistration]
GO

DROP TABLE [hst_dom].[SmsRequest]
GO

DROP TABLE [hst_dom].[RoleUser]
GO

DROP TABLE [hst_dom].[Role]
GO

DROP TABLE [hst_dom].[PermissionUser]
GO

DROP TABLE [hst_dom].[PermissionRole]
GO

DROP TABLE [hst_dom].[Permission]
GO

DROP TABLE [hst_dom].[Message]
GO

DROP TABLE [hst_dom].[EmailRequest]
GO

DROP TABLE [hst_dom].[Application]
GO

DROP TABLE [hst_dom].[Account]
GO

CREATE TABLE [hst_dom].[Account](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Email] [nvarchar](128) NULL,
	[Phone] [decimal](18, 0) NULL,
	[Login] [nvarchar](80) NULL,
	[Password] [nvarchar](80) NULL,
	[Name] [nvarchar](256) NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_Account_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[Application](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Name] [nvarchar](64) NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_Application_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[EmailRequest](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Email] [nvarchar](128) NULL,
	[Password] [nvarchar](80) NULL,
	[ExpiredAt] [datetime2](7) NULL,
	[UserId] [int] NULL,
 CONSTRAINT [PK_EmailRequest_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[Message](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Description] [nvarchar](max) NULL,
	[Type] [nvarchar](10) NULL,
	[Locale] [nvarchar](3) NULL,
	[Name] [nvarchar](256) NULL,
	[Template] [nvarchar](max) NULL,
 CONSTRAINT [PK_Message_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[Permission](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[ApplicationId] [int] NULL,
	[Name] [nvarchar](64) NULL,
	[Description] [nvarchar](max) NULL,
 CONSTRAINT [PK_Permission_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [hst_dom].[PermissionRole](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[PermissionId] [int] NULL,
	[RoleId] [int] NULL,
 CONSTRAINT [PK_PermissionRole_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[PermissionUser](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[PermissionId] [int] NULL,
	[UserId] [int] NULL,
	[Since] [datetime2](7) NULL,
	[Until] [datetime2](7) NULL,
 CONSTRAINT [PK_PermissionUser_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [hst_dom].[Role](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Name] [nvarchar](64) NULL,
	[Description] [nvarchar](max) NULL,
	[IsDefault] [bit] NULL,
 CONSTRAINT [PK_Role_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [hst_dom].[RoleUser](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[RoleId] [int] NULL,
	[UserId] [int] NULL,
 CONSTRAINT [PK_RoleUser_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [hst_dom].[SmsRequest](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[Phone] [decimal](18, 0) NULL,
	[Password] [nvarchar](80) NULL,
	[ExpiredAt] [datetime2](7) NULL,
	[UserId] [int] NULL,
 CONSTRAINT [PK_SmsRequest_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [hst_dom].[TokenRegistration](
	[H_ID] [bigint] IDENTITY(1,1) NOT NULL,
	[H_ConnectionID] [uniqueidentifier] NOT NULL,
	[H_TransactionID] [bigint] NOT NULL,
	[H_SessionID] [int] NOT NULL,
	[H_Login] [nvarchar](128) NOT NULL,
	[H_Time] [datetime2](7) NOT NULL,
	[H_OperationType] [int] NOT NULL,
	[H_IsNew] [bit] NOT NULL,
	[Id] [int] NULL,
	[UserId] [int] NULL,
	[AccessToken] [nvarchar](max) NULL,
	[IssuedAt] [datetime2](7) NULL,
	[ExpiredAt] [datetime2](7) NULL,
 CONSTRAINT [PK_TokenRegistration_History] PRIMARY KEY CLUSTERED 
(
	[H_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TRIGGER [dom].[AccountHistory]
   ON  [dom].[Account]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[Account] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Email]
	,[Phone]
	,[Login]
	,[Password]
	,[Name]
	,[Description])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Email]
	,h.[Phone]
	,h.[Login]
	,h.[Password]
	,h.[Name]
	,h.[Description]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[Account] ENABLE TRIGGER [AccountHistory];
GO
ALTER TABLE [dom].[Account] ENABLE TRIGGER [AccountHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[ApplicationHistory]
   ON  [dom].[Application]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[Application] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Name]
	,[Description])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Name]
	,h.[Description]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[Application] ENABLE TRIGGER [ApplicationHistory];
GO
ALTER TABLE [dom].[Application] ENABLE TRIGGER [ApplicationHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[EmailRequestHistory]
   ON  [dom].[EmailRequest]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[EmailRequest] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Email]
	,[Password]
	,[ExpiredAt]
	,[UserId])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Email]
	,h.[Password]
	,h.[ExpiredAt]
	,h.[UserId]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[EmailRequest] ENABLE TRIGGER [EmailRequestHistory];
GO
ALTER TABLE [dom].[EmailRequest] ENABLE TRIGGER [EmailRequestHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[MessageHistory]
   ON  [dom].[Message]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[Message] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Description]
	,[Type]
	,[Locale]
	,[Name]
	,[Template])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Description]
	,h.[Type]
	,h.[Locale]
	,h.[Name]
	,h.[Template]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[Message] ENABLE TRIGGER [MessageHistory];
GO
ALTER TABLE [dom].[Message] ENABLE TRIGGER [MessageHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[PermissionHistory]
   ON  [dom].[Permission]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[Permission] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[ApplicationId]
	,[Name]
	,[Description])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[ApplicationId]
	,h.[Name]
	,h.[Description]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[Permission] ENABLE TRIGGER [PermissionHistory];
GO
ALTER TABLE [dom].[Permission] ENABLE TRIGGER [PermissionHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[PermissionRoleHistory]
   ON  [dom].[PermissionRole]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[PermissionRole] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[PermissionId]
	,[RoleId])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[PermissionId]
	,h.[RoleId]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.PermissionId, h.RoleId, h.H_IsNew


END;

ALTER TABLE [dom].[PermissionRole] ENABLE TRIGGER [PermissionRoleHistory];
GO
ALTER TABLE [dom].[PermissionRole] ENABLE TRIGGER [PermissionRoleHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[PermissionUserHistory]
   ON  [dom].[PermissionUser]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[PermissionUser] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[PermissionId]
	,[UserId]
	,[Since]
	,[Until])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[PermissionId]
	,h.[UserId]
	,h.[Since]
	,h.[Until]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.PermissionId, h.UserId, h.H_IsNew


END;

ALTER TABLE [dom].[PermissionUser] ENABLE TRIGGER [PermissionUserHistory];
GO
ALTER TABLE [dom].[PermissionUser] ENABLE TRIGGER [PermissionUserHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[RoleHistory]
   ON  [dom].[Role]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[Role] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Name]
	,[Description]
	,[IsDefault])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Name]
	,h.[Description]
	,h.[IsDefault]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[Role] ENABLE TRIGGER [RoleHistory];
GO
ALTER TABLE [dom].[Role] ENABLE TRIGGER [RoleHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[RoleUserHistory]
   ON  [dom].[RoleUser]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[RoleUser] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[RoleId]
	,[UserId])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[RoleId]
	,h.[UserId]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.RoleId, h.UserId, h.H_IsNew


END;

ALTER TABLE [dom].[RoleUser] ENABLE TRIGGER [RoleUserHistory];
GO
ALTER TABLE [dom].[RoleUser] ENABLE TRIGGER [RoleUserHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[SmsRequestHistory]
   ON  [dom].[SmsRequest]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[SmsRequest] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[Phone]
	,[Password]
	,[ExpiredAt]
	,[UserId])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[Phone]
	,h.[Password]
	,h.[ExpiredAt]
	,h.[UserId]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[SmsRequest] ENABLE TRIGGER [SmsRequestHistory];
GO
ALTER TABLE [dom].[SmsRequest] ENABLE TRIGGER [SmsRequestHistory]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TRIGGER [dom].[TokenRegistrationHistory]
   ON  [dom].[TokenRegistration]
   AFTER INSERT, UPDATE, DELETE
AS 
BEGIN

	SET NOCOUNT ON;

declare @delExists bit;
declare @insExists bit;

if exists(select top 1 1 from deleted) set @delExists = 1
if exists(select top 1 1 from inserted) set @insExists = 1

declare @opType int;

if (@delExists = 1)
	if (@insExists = 1)
		set @opType = 2
	else 
		set @opType = 3
else
	if (@insExists = 1)
		set @opType = 1
	else 
		set @opType = 0
		
if (@opType = 0)
	return;	

declare @time datetime2(7) = SYSUTCDATETIME()
declare @connection_id uniqueidentifier = (select connection_id from sys.dm_exec_connections where session_id = @@SPID and parent_connection_id is null)
--declare @transaction_id bigint = (select transaction_id from sys.dm_tran_current_transaction)
declare @transaction_id bigint = (select transaction_id from sys.dm_tran_session_transactions where session_id = @@SPID)
declare @login nvarchar(128) = ORIGINAL_LOGIN()


insert into [hst_dom].[TokenRegistration] ([H_ConnectionID], [H_TransactionID], [H_SessionID], [H_Login], [H_Time], [H_OperationType], [H_IsNew]
-- data columns
	,[Id]
	,[UserId]
	,[AccessToken]
	,[IssuedAt]
	,[ExpiredAt])
select @connection_id, @transaction_id, @@SPID, @login, @time, @opType, h.H_IsNew
-- data columns
	,h.[Id]
	,h.[UserId]
	,h.[AccessToken]
	,h.[IssuedAt]
	,h.[ExpiredAt]
from 
(
	select 0 as H_IsNew, t.* 
	from deleted t
	union all
	select 1 as H_IsNew, t.* 
	from inserted t
) h
order by h.Id, h.H_IsNew


END;

ALTER TABLE [dom].[TokenRegistration] ENABLE TRIGGER [TokenRegistrationHistory];
GO
ALTER TABLE [dom].[TokenRegistration] ENABLE TRIGGER [TokenRegistrationHistory]
GO
