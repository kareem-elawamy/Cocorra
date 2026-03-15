namespace Cocorra.DAL.AppMetaData
{
    public static class Router
    {
        public const string Root = "Api";
        public const string Version = "V1";
        public const string Rule = Root + "/" + Version + "/";

        public static class AuthenticationRouting
        {
            public const string Prefix = Rule + "Authentication";
            public const string Register = Prefix + "/Register";
            public const string Login = Prefix + "/Login";
            public const string SubmitMbti = Prefix + "/SubmitMbti";
            public const string ForgotPassword = Prefix + "/ForgotPassword";
            public const string UpdateFcmToken = Prefix + "/UpdateFcmToken";
            public const string ConfirmEmail = Prefix + "/ConfirmEmail"; // GET
            public const string ResendOtp = Prefix + "/ResendOtp"; // POST
        }

        public static class AdminRouting
        {
            public const string Prefix = Rule + "Admin";

            public const string GetAll = Prefix + "/Users";               // GET
            public const string GetById = Prefix + "/User/{id}";          // GET
            public const string Update = Prefix + "/User/{id}";           // PUT
            public const string Delete = Prefix + "/User/{id}";           // DELETE           
            public const string ChangeStatus = Prefix + "/User/ChangeStatus/{id}"; // PUT

            public const string ResetPassword = Prefix + "/ResetPassword/{id}"; // POST
            public const string Stats = Prefix + "/Dashboard/Stats";      // GET
        }
        public static class RolesRouting
        {
            public const string Prefix = Rule + "Roles";
            public const string GetRoles = Prefix + "/List";               // GET
            public const string GetRoleById = Prefix + "/{id}";            // GET
            public const string Create = Prefix + "/Create";               // POST
            public const string Update = Prefix + "/Update";               // PUT
            public const string Delete = Prefix + "/Delete/{id}";          // DELETE
            public const string ManageUserRoles = Prefix + "/ManageUser";  // POST
            public const string GetUsersInRole = Prefix + "/Users/{roleName}"; // GET
        }
        public static class RoomRouting
        {
            public const string Prefix = Rule + "Room";
            public const string Create = Prefix + "/Create";
            public const string Join = Prefix + "/Join";
            public const string Approve = Prefix + "/Approve";
            public const string State = Prefix + "/{roomId}/State";
            public const string Feed = Prefix + "Feed";
            public const string toggleReminder = Prefix + "{roomId}/toggle-reminder";
        }
    }
}