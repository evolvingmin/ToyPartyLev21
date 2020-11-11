using System;
using UnityEngine;

namespace ToyParty
{
    public static class Define
    {
        public static string Resource_None { get => "NONE"; }
    }

    public static class Result
    {
        [Flags]
        public enum Reason
        {
            OK = 0,
            WARNING_NOT_INITIALIZED = 2,
            WARNING_REDUNDANT_INITIALIZATION = 4,
            WARNING_ALREADY_ASSIGNED = 8,
            WARNING_WRONG_REQUEST = 16,
            WARNING_NOT_SUPPORTED = 32,
            ERROR_DATA_NOT_IN_PROPER_RANGE = 64,
            ERROR_SYSTEM_PARSER_NULL = 128,
            ERROR_DATA_NOT_FOUNDED = 256,
            ERROR_DATA_NOT_CORRECT_FORM = 512,
            ERROR_PARAMETER_NOT_INITIALIZED = 1024,
        }

        public static Reason LogWarning(Reason reason)
        {
            if (reason != Reason.OK)
            {
                Debug.LogWarning(reason);
            }
            return reason;
        }

    }
}
