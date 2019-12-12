BEGIN;
CREATE SCHEMA IF NOT EXISTS "dom";
CREATE SCHEMA IF NOT EXISTS "hst_dom";
CREATE SCHEMA IF NOT EXISTS "log";






CREATE TABLE "dom"."Account"( 
	"Id" int NOT NULL,
	"Email" varchar(128),
	"Phone" numeric(18, 0),
	"Login" varchar(80),
	"Password" varchar(80),
	"Name" varchar(256),
	"Description" varchar,
	"FirstDate" timestamp,
	"LastDate" timestamp,
	"IsLocked" boolean NOT NULL);

CREATE TABLE "dom"."AccountEnvironment"( 
	"EnvironmentId" int NOT NULL,
	"UserId" int NOT NULL);

CREATE TABLE "dom"."Application"( 
	"Id" int NOT NULL,
	"Name" varchar(64) NOT NULL,
	"Description" varchar);

CREATE TABLE "dom"."EmailRequest"( 
	"Id" int NOT NULL,
	"Email" varchar(128) NOT NULL,
	"Password" varchar(80) NOT NULL,
	"ExpiredAt" timestamp(7) NOT NULL,
	"UserId" int,
	"EnvironmentId" int);

CREATE TABLE "dom"."Environment"( 
	"Id" int NOT NULL,
	"Name" varchar(64) NOT NULL,
	"Description" varchar(128),
	"Token" varchar(128) NOT NULL,
	"IsDefault" boolean NOT NULL);

CREATE TABLE "dom"."Message"( 
	"Id" int NOT NULL,
	"Description" varchar,
	"Type" varchar(10),
	"Locale" varchar(20),
	"Name" varchar(256) NOT NULL,
	"Template" varchar NOT NULL,
	"EnvironmentId" int NOT NULL);

CREATE TABLE "dom"."Permission"( 
	"Id" int NOT NULL,
	"ApplicationId" int NOT NULL,
	"Name" varchar(64) NOT NULL,
	"Description" varchar);

CREATE TABLE "dom"."PermissionRole"( 
	"PermissionId" int NOT NULL,
	"RoleId" int NOT NULL);

CREATE TABLE "dom"."PermissionUser"( 
	"PermissionId" int NOT NULL,
	"UserId" int NOT NULL,
	"Since" timestamp(7),
	"Until" timestamp(7));

CREATE TABLE "dom"."Role"( 
	"Id" int NOT NULL,
	"Name" varchar(64) NOT NULL,
	"Description" varchar,
	"IsDefault" boolean NOT NULL);

CREATE TABLE "dom"."RoleUser"( 
	"RoleId" int NOT NULL,
	"UserId" int NOT NULL);

CREATE TABLE "dom"."SmsRequest"( 
	"Id" int NOT NULL,
	"Phone" numeric(18, 0) NOT NULL,
	"Password" varchar(80) NOT NULL,
	"ExpiredAt" timestamp(7) NOT NULL,
	"UserId" int,
	"EnvironmentId" int);

CREATE TABLE "dom"."TokenRegistration"( 
	"Id" int NOT NULL,
	"UserId" int NOT NULL,
	"AccessToken" varchar NOT NULL,
	"IssuedAt" timestamp(7) NOT NULL,
	"ExpiredAt" timestamp(7));

CREATE TABLE "hst_dom"."Account"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Email" varchar(128),
	"Phone" numeric(18, 0),
	"Login" varchar(80),
	"Password" varchar(80),
	"Name" varchar(256),
	"Description" varchar,
	"FirstDate" timestamp,
	"LastDate" timestamp,
	"IsLocked" boolean);

CREATE TABLE "hst_dom"."Application"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Name" varchar(64),
	"Description" varchar);

CREATE TABLE "hst_dom"."EmailRequest"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Email" varchar(128),
	"Password" varchar(80),
	"ExpiredAt" timestamp(7),
	"UserId" int,
	"EnvironmentId" int);

CREATE TABLE "hst_dom"."Message"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Description" varchar,
	"Type" varchar(10),
	"Locale" varchar(20),
	"Name" varchar(256),
	"Template" varchar,
	"EnvironmentId" int);

CREATE TABLE "hst_dom"."Permission"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"ApplicationId" int,
	"Name" varchar(64),
	"Description" varchar);

CREATE TABLE "hst_dom"."PermissionRole"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"PermissionId" int,
	"RoleId" int);

CREATE TABLE "hst_dom"."PermissionUser"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"PermissionId" int,
	"UserId" int,
	"Since" timestamp(7),
	"Until" timestamp(7));

CREATE TABLE "hst_dom"."Role"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Name" varchar(64),
	"Description" varchar,
	"IsDefault" boolean);

CREATE TABLE "hst_dom"."RoleUser"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"RoleId" int,
	"UserId" int);

CREATE TABLE "hst_dom"."SmsRequest"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"Phone" numeric(18, 0),
	"Password" varchar(80),
	"ExpiredAt" timestamp(7),
	"UserId" int,
	"EnvironmentId" int);

CREATE TABLE "hst_dom"."TokenRegistration"( 
	"H_ID" bigint NOT NULL,
	"H_ConnectionID" uuid,
	"H_TransactionID" bigint NOT NULL,
	"H_SessionID" int NOT NULL,
	"H_Login" varchar(128) NOT NULL,
	"H_Time" timestamp(7) NOT NULL,
	"H_OperationType" int NOT NULL,
	"H_IsNew" boolean NOT NULL,
	"Id" int,
	"UserId" int,
	"AccessToken" varchar,
	"IssuedAt" timestamp(7),
	"ExpiredAt" timestamp(7));

CREATE TABLE "log"."Access"( 
	"Id" bigint NOT NULL,
	"Action" varchar NOT NULL,
	"Method" varchar NOT NULL,
	"ClientIp" varchar NOT NULL,
	"ProcessedAt" timestamp NOT NULL,
	"StatusCode" int,
	"UserAgent" varchar NOT NULL,
	"UserId" varchar,
	"Referer" varchar,
	"ProcessingTime" int);

COMMIT;
