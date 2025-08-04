using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace TechnicalSupport.Application.Authorization
{
    public class Operations
    {
        public static OperationAuthorizationRequirement Create =
            new OperationAuthorizationRequirement { Name = nameof(Create) };
        public static OperationAuthorizationRequirement Read =
            new OperationAuthorizationRequirement { Name = nameof(Read) };
        public static OperationAuthorizationRequirement Update =
            new OperationAuthorizationRequirement { Name = nameof(Update) };
        public static OperationAuthorizationRequirement Delete =
            new OperationAuthorizationRequirement { Name = nameof(Delete) };
    }

    public static class TicketOperations
    {
        public static OperationAuthorizationRequirement Create = Operations.Create;
        public static OperationAuthorizationRequirement Read = Operations.Read;
        public static OperationAuthorizationRequirement Update = Operations.Update;
        public static OperationAuthorizationRequirement Delete = Operations.Delete;
        public static OperationAuthorizationRequirement Assign =
            new OperationAuthorizationRequirement { Name = nameof(Assign) };
        public static OperationAuthorizationRequirement ChangeStatus =
            new OperationAuthorizationRequirement { Name = nameof(ChangeStatus) };
        public static OperationAuthorizationRequirement AddComment =
            new OperationAuthorizationRequirement { Name = nameof(AddComment) };
        public static OperationAuthorizationRequirement UploadFile =
            new OperationAuthorizationRequirement { Name = nameof(UploadFile) };
    }

    public static class CommentOperations
    {
        public static OperationAuthorizationRequirement Create = Operations.Create;
        public static OperationAuthorizationRequirement Read = Operations.Read;
        public static OperationAuthorizationRequirement Update = Operations.Update;
        public static OperationAuthorizationRequirement Delete = Operations.Delete;
    }

    public static class UserOperations
    {
        public static OperationAuthorizationRequirement ListAll =
            new OperationAuthorizationRequirement { Name = nameof(ListAll) };
        public static OperationAuthorizationRequirement ReadProfile =
            new OperationAuthorizationRequirement { Name = nameof(ReadProfile) };
        public static OperationAuthorizationRequirement UpdateProfile =
            new OperationAuthorizationRequirement { Name = nameof(UpdateProfile) };
        public static OperationAuthorizationRequirement ChangeRole =
            new OperationAuthorizationRequirement { Name = nameof(ChangeRole) };
        public static OperationAuthorizationRequirement DeleteUser =
            new OperationAuthorizationRequirement { Name = nameof(DeleteUser) };
    }
} 