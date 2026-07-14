-- Migration 001: Create schemas
-- Bounded-context grouping; also the unit of permission grants for future
-- least-privilege DB principals (e.g. a reporting login granted SELECT on `crm` only).

CREATE SCHEMA tenant AUTHORIZATION dbo;
GO
CREATE SCHEMA [identity] AUTHORIZATION dbo;
GO
CREATE SCHEMA crm AUTHORIZATION dbo;
GO
CREATE SCHEMA audit AUTHORIZATION dbo;
GO
-- Reserved for future phases — created now so object placement never has to be renegotiated:
CREATE SCHEMA billing AUTHORIZATION dbo;
GO
CREATE SCHEMA notification AUTHORIZATION dbo;
GO
CREATE SCHEMA education AUTHORIZATION dbo;
GO
