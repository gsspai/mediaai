namespace App.Api.Utilities.Roles
{
    /// <summary>
    /// Centralizes role definitions and provides an authoritative list of valid system roles for role-based access control.
    /// Use AllRoles for validation/selection within access control logic.
    /// Business roles mirror 「产品文档 · 用户与角色」; keep them in sync with react-app/src/utils/roles.ts.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Represents administrative users with the highest level of system permissions.
        /// </summary>
        public const string Admin = "admin";

        /// <summary>
        /// Business development: owns deals and client communication.
        /// </summary>
        public const string Sales = "sales";

        /// <summary>
        /// Finance: reviews quotes and confirms revenue-side figures.
        /// </summary>
        public const string Finance = "finance";

        /// <summary>
        /// Planning: owns the content calendar and topic selection.
        /// </summary>
        public const string Planner = "planner";

        /// <summary>
        /// Operations: publishing, throttling triage and platform connections.
        /// </summary>
        public const string Operations = "operations";

        /// <summary>
        /// Editing: production and delivery of content assets.
        /// </summary>
        public const string Editor = "editor";

        /// <summary>
        /// Analytics: owns metric definitions and report views.
        /// </summary>
        public const string Analyst = "analyst";

        /// <summary>
        /// Represents a regular user with standard permissions.
        /// </summary>
        public const string User = "user";

        /// <summary>
        /// Represents any authenticated user account for feature gating.
        /// </summary>
        public const string Authenticated = "authenticated";

        /// <summary>
        /// Roles that may read/write business data (deals, plans, connections, metrics).
        /// Accounts holding none of these are treated as read-only visitors.
        /// </summary>
        public static readonly string[] BusinessRoles = { Admin, Sales, Finance, Planner, Operations, Editor, Analyst };

        /// <summary>
        /// Aggregate array containing all defined roles, suitable for role validation/enumeration.
        /// </summary>
        public static readonly string[] AllRoles = { Admin, Sales, Finance, Planner, Operations, Editor, Analyst, User, Authenticated };

        /// <summary>
        /// Returns true when the given role is one of the business roles.
        /// </summary>
        public static bool IsBusinessRole(string role) => Array.IndexOf(BusinessRoles, role) >= 0;
    }
}
