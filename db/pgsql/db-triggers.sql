BEGIN;

/*
 * AccountHistory
 */

CREATE OR REPLACE FUNCTION process_AccountHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."Account" VALUES
            (nextval('"hst_dom"."account_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."Account" VALUES
            (nextval('"hst_dom"."account_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."account_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."Account" VALUES
            (nextval('"hst_dom"."account_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "AccountHistory" ON "dom"."Account";
CREATE TRIGGER "AccountHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."Account"
    FOR EACH ROW EXECUTE PROCEDURE process_AccountHistory();

/*
 * ApplicationHistory
 */

CREATE OR REPLACE FUNCTION process_ApplicationHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."Application" VALUES
            (nextval('"hst_dom"."application_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."Application" VALUES
            (nextval('"hst_dom"."application_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."application_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."Application" VALUES
            (nextval('"hst_dom"."application_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "ApplicationHistory" ON "dom"."Application";
CREATE TRIGGER "ApplicationHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."Application"
    FOR EACH ROW EXECUTE PROCEDURE process_ApplicationHistory();

/*
 * EmailRequestHistory
 */

CREATE OR REPLACE FUNCTION process_EmailRequestHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."EmailRequest" VALUES
            (nextval('"hst_dom"."emailrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."EmailRequest" VALUES
            (nextval('"hst_dom"."emailrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."emailrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."EmailRequest" VALUES
            (nextval('"hst_dom"."emailrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "EmailRequestHistory" ON "dom"."EmailRequest";
CREATE TRIGGER "EmailRequestHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."EmailRequest"
    FOR EACH ROW EXECUTE PROCEDURE process_EmailRequestHistory();

/*
 * MessageHistory
 */

CREATE OR REPLACE FUNCTION process_MessageHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."Message" VALUES
            (nextval('"hst_dom"."message_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."Message" VALUES
            (nextval('"hst_dom"."message_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."message_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."Message" VALUES
            (nextval('"hst_dom"."message_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "MessageHistory" ON "dom"."Message";
CREATE TRIGGER "MessageHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."Message"
    FOR EACH ROW EXECUTE PROCEDURE process_MessageHistory();

/*
 * PermissionHistory
 */

CREATE OR REPLACE FUNCTION process_PermissionHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."Permission" VALUES
            (nextval('"hst_dom"."permission_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."Permission" VALUES
            (nextval('"hst_dom"."permission_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."permission_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."Permission" VALUES
            (nextval('"hst_dom"."permission_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "PermissionHistory" ON "dom"."Permission";
CREATE TRIGGER "PermissionHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."Permission"
    FOR EACH ROW EXECUTE PROCEDURE process_PermissionHistory();

/*
 * PermissionRoleHistory
 */

CREATE OR REPLACE FUNCTION process_PermissionRoleHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."PermissionRole" VALUES
            (nextval('"hst_dom"."permissionrole_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."PermissionRole" VALUES
            (nextval('"hst_dom"."permissionrole_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."permissionrole_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."PermissionRole" VALUES
            (nextval('"hst_dom"."permissionrole_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "PermissionRoleHistory" ON "dom"."PermissionRole";
CREATE TRIGGER "PermissionRoleHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."PermissionRole"
    FOR EACH ROW EXECUTE PROCEDURE process_PermissionRoleHistory();

/*
 * PermissionUserHistory
 */

CREATE OR REPLACE FUNCTION process_PermissionUserHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."PermissionUser" VALUES
            (nextval('"hst_dom"."permissionuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."PermissionUser" VALUES
            (nextval('"hst_dom"."permissionuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."permissionuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."PermissionUser" VALUES
            (nextval('"hst_dom"."permissionuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "PermissionUserHistory" ON "dom"."PermissionUser";
CREATE TRIGGER "PermissionUserHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."PermissionUser"
    FOR EACH ROW EXECUTE PROCEDURE process_PermissionUserHistory();

/*
 * RoleHistory
 */

CREATE OR REPLACE FUNCTION process_RoleHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."Role" VALUES
            (nextval('"hst_dom"."role_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."Role" VALUES
            (nextval('"hst_dom"."role_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."role_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."Role" VALUES
            (nextval('"hst_dom"."role_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "RoleHistory" ON "dom"."Role";
CREATE TRIGGER "RoleHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."Role"
    FOR EACH ROW EXECUTE PROCEDURE process_RoleHistory();

/*
 * RoleUserHistory
 */

CREATE OR REPLACE FUNCTION process_RoleUserHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."RoleUser" VALUES
            (nextval('"hst_dom"."roleuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."RoleUser" VALUES
            (nextval('"hst_dom"."roleuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."roleuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."RoleUser" VALUES
            (nextval('"hst_dom"."roleuser_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "RoleUserHistory" ON "dom"."RoleUser";
CREATE TRIGGER "RoleUserHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."RoleUser"
    FOR EACH ROW EXECUTE PROCEDURE process_RoleUserHistory();

/*
 * SmsRequestHistory
 */

CREATE OR REPLACE FUNCTION process_SmsRequestHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."SmsRequest" VALUES
            (nextval('"hst_dom"."smsrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."SmsRequest" VALUES
            (nextval('"hst_dom"."smsrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."smsrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."SmsRequest" VALUES
            (nextval('"hst_dom"."smsrequest_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "SmsRequestHistory" ON "dom"."SmsRequest";
CREATE TRIGGER "SmsRequestHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."SmsRequest"
    FOR EACH ROW EXECUTE PROCEDURE process_SmsRequestHistory();

/*
 * TokenRegistrationHistory
 */

CREATE OR REPLACE FUNCTION process_TokenRegistrationHistory() RETURNS TRIGGER AS
$$
    BEGIN
        IF (TG_OP = 'DELETE') THEN
            INSERT INTO "hst_dom"."TokenRegistration" VALUES
            (nextval('"hst_dom"."tokenregistration_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 3, FALSE, OLD.*);
            RETURN OLD;
        ELSIF (TG_OP = 'UPDATE') THEN
            INSERT INTO "hst_dom"."TokenRegistration" VALUES
            (nextval('"hst_dom"."tokenregistration_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, FALSE, OLD.*),
            (nextval('"hst_dom"."tokenregistration_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 2, TRUE, NEW.*);
            RETURN NEW;
        ELSIF (TG_OP = 'INSERT') THEN
            INSERT INTO "hst_dom"."TokenRegistration" VALUES
            (nextval('"hst_dom"."tokenregistration_h_id_seq"'), null, txid_current(), pg_backend_pid(), user, timezone('utc', now()), 1, TRUE, NEW.*);
            RETURN NEW;
        END IF;
        RETURN NULL;
    END;
$$
LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TokenRegistrationHistory" ON "dom"."TokenRegistration";
CREATE TRIGGER "TokenRegistrationHistory"
AFTER INSERT OR UPDATE OR DELETE ON "dom"."TokenRegistration"
    FOR EACH ROW EXECUTE PROCEDURE process_TokenRegistrationHistory();

COMMIT;