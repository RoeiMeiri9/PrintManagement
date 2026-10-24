using PrintManagement.Models.Filter;
using System;
using System.Collections.Generic;

namespace PrintManagement.Pages.CustomFilters.Helpers;

public static class InitialFilters
{
    public static readonly GenericFilterDefinition[] PrinterDefaults =
    [
        new GenericFilterDefinition
        {
            Name = "Printers Not Ready",
            Criteria = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "QueueStatus",
                    new FilterRule
                    {
                        Condition = FilterCondition.NotEquals,
                        Value = "Ready"
                    }
                }
            },
            Description = "Shows printers that are offline, in error, or otherwise not ready."
        },
        new GenericFilterDefinition
        {
            Name = "Printers With Jobs",
            Criteria = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "JobsInQueue",
                    new FilterRule
                    {
                        Condition = FilterCondition.GreaterThan,
                        Value = "0"
                    }
                }
            },
            Description = "Shows printers that have one or more jobs waiting in the queue."
        }
    ];
}