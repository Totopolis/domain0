if not exists (select top 1 1 from sys.schemas where name='mon')
	exec sp_executesql N'create schema mon' 
go
if object_id('mon.Token') is not null
	drop table mon.Token
go
if object_id('mon.PermissionUser') is not null
	drop table mon.PermissionUser
go
if object_id('mon.RoleUser') is not null
	drop table mon.RoleUser
go
if object_id('mon.Login') is not null
	drop table mon.Login
go
if object_id('mon.PermissionRole') is not null
	drop table mon.PermissionRole
go
if object_id('mon.Role') is not null
	drop table mon.Role
go
if object_id('mon.Permission') is not null
	drop table mon.Permission
go
if object_id('mon.Application') is not null
	drop table mon.Application
go

create table mon.Login (
	Id int not null identity(1,1) constraint PK_mon_Login primary key,
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
create table mon.Role (
	Id int not null identity(1,1) constraint PK_mon_Role primary key,
	Name nvarchar(64) not null,
	Description nvarchar(max) null,
	IsDefault bit not null,

	constraint UQ_mon_Role unique(Name)
)
go
create table mon.Application (
	Id int not null identity(1,1) constraint PK_mon_Application primary key,
	Name nvarchar(64) not null,
	Description nvarchar(max) null
)
go
create table mon.Permission (
	Id int not null identity(1,1) constraint PK_mon_Permission primary key,
	ApplicationId int not null constraint FK_mon_Permission_ApplicationId foreign key references mon.Application(Id),
	Name nvarchar(64) not null,
	Description nvarchar(max) null

	constraint UQ_mon_Permission unique(Name)
)
go
create table mon.PermissionRole (
	PermissionId int not null constraint FK_mon_PermissionRole_PermissionId foreign key references mon.Permission(Id),
	RoleId int not null constraint FK_mon_PermissionRole_RoleId foreign key references mon.Role(Id)

	constraint PK_mon_PermissionRole primary key(PermissionId, RoleId)
)
go
create table mon.RoleUser (
	RoleId int not null constraint FK_mon_RoleUser_RoleId foreign key references mon.Role(Id),
	UserId int not null constraint FK_mon_RoleUser_UserId foreign key references mon.Login(Id)

	constraint PK_mon_RoleUser primary key(RoleId, UserId)
)
go
create table mon.PermissionUser (
	PermissionId int not null,
	UserId int not null,
	Since datetime2 null,
	Until datetime2 null

	constraint PK_mon_PermissionUser primary key(PermissionId, UserId)
)
go
create table mon.Token (
	Id int not null identity(1,1) constraint PK_mon_Token primary key,
	UserId int not null,
	AccessToken varchar(max) not null
)
go
if object_id('mon.Caching') is not null
	drop table mon.Caching
go
create table mon.Caching (
	Id varchar(900) not null constraint PK_mon_Caching primary key,
	Value varbinary(max) not null,
	ExpiresAtTime datetimeoffset(7) not null,
	SlidingExpirationInSeconds bigint null,
	AbsoluteExpiration datetimeoffset(7) null
)
go
if object_id('mon.Message') is not null
	drop table mon.Message
go
create table mon.Message (
	Id int identity(1,1) not null constraint PK_Message_Id primary key,
	Description nvarchar(max) null,
	Locale nvarchar(3) null,
	Name nvarchar(256) not null,
	Template nvarchar(max) not null
)
go