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
create table dom.RoleUser (
	RoleId int not null constraint FK_dom_RoleUser_RoleId foreign key references dom.Role(Id),
	UserId int not null constraint FK_dom_RoleUser_UserId foreign key references dom.Login(Id)

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
if object_id('dom.Caching') is not null
	drop table dom.Caching
go
create table dom.Caching (
	Id varchar(900) not null constraint PK_dom_Caching primary key,
	Value varbinary(max) not null,
	ExpiresAtTime datetimeoffset(7) not null,
	SlidingExpirationInSeconds bigint null,
	AbsoluteExpiration datetimeoffset(7) null
)
go
if object_id('dom.Message') is not null
	drop table dom.Message
go
create table dom.Message (
	Id int identity(1,1) not null constraint PK_Message_Id primary key,
	Description nvarchar(max) null,
	Locale nvarchar(3) null,
	Name nvarchar(256) not null,
	Template nvarchar(max) not null
)
go

if object_id('dom.Account') is not null
	drop table dom.Account
go
create table dom.Account (
	[Id] int identity(1,1) not null constraint PK_Account_Id primary key,
	[Phone] decimal null,
	[Login] nvarchar(80) null,
	[Password] nvarchar(80) null,
	[Name] nvarchar(256) not null,
	[Description] nvarchar(max) not null
)
go