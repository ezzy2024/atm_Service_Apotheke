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
}
