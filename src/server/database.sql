if not exists (select top 1 1 from sys.schemas where name='dom')
	exec sp_executesql N'create schema dom' 
go
if object_id('dom.Token') is not null
	drop table dom.Token
go
if object_id('dom.PermissionUser') is not null
	drop table dom.PermissionUser
go
if object_id('dom.RoleUser') is not null
	drop table dom.RoleUser
go
if object_id('dom.Login') is not null
	drop table dom.Login
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
if object_id('dom.Caching') is not null
	drop table dom.Caching
go
if object_id('dom.Message') is not null
	drop table dom.Message
go
if object_id('dom.TokenRegistration') is not null
	drop table dom.TokenRegistration
go


create table dom.Login (
	Id int not null identity(1,1) constraint PK_dom_Login primary key,
	Phone decimal(14,0) null,
	Login varchar(14) not null,
	Salt varbinary(64) not null,
	Password varchar(64) not null,
	Firstname nvarchar(64) null,
	Secondname nvarchar(64) null,
	Middlename nvarchar(64) null,
	Description nvarchar(max) null
)
go
create table dom.Role (
	Id int not null identity(1,1) constraint PK_dom_Role primary key,
	Name nvarchar(64) not null,
	Description nvarchar(max) null,
	IsDefault bit not null,

	constraint UQ_dom_Role unique(Name)
)
go
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
create table dom.PermissionRole (
	PermissionId int not null constraint FK_dom_PermissionRole_PermissionId foreign key references dom.Permission(Id),
	RoleId int not null constraint FK_dom_PermissionRole_RoleId foreign key references dom.Role(Id)

	constraint PK_dom_PermissionRole primary key(PermissionId, RoleId)
)
go

create table dom.Account (
	[Id] int identity(1,1) not null constraint PK_Account_Id primary key,
	[Phone] decimal null,
	[Login] nvarchar(80) null,
	[Password] nvarchar(80) null,
	[Name] nvarchar(256) not null,
	[Description] nvarchar(max) null
)
go

create table dom.RoleUser (
	RoleId int not null constraint FK_dom_RoleUser_RoleId foreign key references dom.Role(Id),
	UserId int not null constraint FK_dom_RoleUser_UserId foreign key references dom.Account(Id)

	constraint PK_dom_RoleUser primary key(RoleId, UserId)
)
go
create table dom.PermissionUser (
	PermissionId int not null,
	UserId int not null,
	Since datetime2 null,
	Until datetime2 null

	constraint PK_dom_PermissionUser primary key(PermissionId, UserId)
)
go
create table dom.Token (
	Id int not null identity(1,1) constraint PK_dom_Token primary key,
	UserId int not null,
	AccessToken varchar(max) not null
)
go

create table dom.Caching (
	Id varchar(900) not null constraint PK_dom_Caching primary key,
	Value varbinary(max) not null,
	ExpiresAtTime datetimeoffset(7) not null,
	SlidingExpirationInSeconds bigint null,
	AbsoluteExpiration datetimeoffset(7) null
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
(Type, Locale, Name, Template)
values
('sms',		'eng',		'WelcomeTemplate',		'Hello {0}!'),
('sms',		'eng',		'RegisterTemplate',		'Your password is: {0} will valid for {1} min'),
('sms',		'eng',		'RequestResetTemplate',	'Your NEW password is: {0} will valid for {1} min'),
('sms',		'rus',		'WelcomeTemplate',		'Добро пожаловать {0}!'),
('sms',		'rus',		'RegisterTemplate',		'Ваш пароль: {0} действителен {1} мин'),
('sms',		'rus',		'RequestResetTemplate',	'Ваш НОВЫЙ пароль: {0} действителен {1} мин')


create table dom.SmsRequest (
	[Id] int identity(1,1) not null constraint PK_SmsRequest_Id primary key,
	[Phone] decimal not null,
	[Password] nvarchar(80) not null,
	[ExpiredAt] datetime2 not null
)
go

create table dom.TokenRegistration (
	[Id] int identity(1,1) not null constraint PK_TokenRegistration_Id primary key,
	[UserId] int not null,
	[AccessToken] nvarchar(max) not null,
	[IssuedAt] datetime2 not null,
	[ExpiredAt] datetime2 null
)