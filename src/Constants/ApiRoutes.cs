namespace TaskPilot.Constants;

public static class ApiRoutes
{
    private const string Base = "/api/v1";

    public static class Tasks
    {
        public const string Root = $"{Base}/tasks";
        public const string ById = $"{Base}/tasks/{{id}}";
        public const string Complete = $"{Base}/tasks/{{id}}/complete";
        public const string Stats = $"{Base}/tasks/stats";
    }

    public static class Tags
    {
        public const string Root = $"{Base}/tags";
        public const string ById = $"{Base}/tags/{{id}}";
    }

    public static class ApiKeys
    {
        public const string Root = $"{Base}/apikeys";
        public const string ById = $"{Base}/apikeys/{{id}}";
        public const string Activate = $"{Base}/apikeys/{{id}}/activate";
        public const string Deactivate = $"{Base}/apikeys/{{id}}/deactivate";
    }

    public static class Audit
    {
        public const string Root = $"{Base}/audit";
        public const string Summary = $"{Base}/audit/summary";
    }

    public static class Health
    {
        public const string Live = $"{Base}/health/live";
        public const string Ready = $"{Base}/health/ready";
        public const string Full = $"{Base}/health/full";
        public const string Version = $"{Base}/health/version";
        public const string Assets = $"{Base}/health/assets";
    }
}
