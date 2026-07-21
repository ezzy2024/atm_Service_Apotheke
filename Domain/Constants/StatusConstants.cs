namespace ServiceApotheke.API.Domain.Constants
{
    public static class JobApplicationStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Completed = "Completed";
        public const string Invoiced = "Invoiced";
        public const string Rejected = "Rejected";
    }

    public static class TimesheetStatus
    {
        public const string Submitted = "Submitted";
        public const string Approved = "Approved";
        public const string Disputed = "Disputed";
    }

    public static class JobPostStatus
    {
        public const string Active = "Active";
        public const string Open = "Open";
        public const string Filled = "Filled";
        public const string Cancelled = "Cancelled";
    }

    public static class TerminalStatus
    {
        public const string Active = "active";
        public const string Inactive = "inactive";
    }
}
