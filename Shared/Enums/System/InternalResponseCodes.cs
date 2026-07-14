namespace Shared.Enums.System;

public enum InternalResponseCodes : byte
{
    OK = 1,
    Created = 2,
    Accepted = 3,
    Found = 4,

    BadRequest = 5,
    Unauthorized = 6,
    Forbidden = 7,
    NotFound = 8,
    Conflict = 9,

    InternalServerError = 10,
    RequestTimeout = 11,
}