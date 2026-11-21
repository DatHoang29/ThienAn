namespace Shared.DTO.Constants.Application
{
    public static class ApplicationContants
    {

        public enum CacheTech
        {
            Redis,
            Memory
        }

        public static class ResponseContentType
        {
            public const string Json = "application/json";
            public const string MsgPack = "application/x-msgpack";
        }
        public static class MimeTypes
        {
            public const string OpenXml = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        }
        public enum AuditType
        {
            NA = 0,
            LOGIN = 99,
            LOGOUT = 100,
            CREATE = 1,
            UPDATE = 2,
            DELETE = 3,
            CREATE_EDIT = 4,
            NOTE = 5
        }
        public enum PermissionAction
        {
            Add,
            Edit,
            AddOrEdit,
            Delete,
            Read,
            All
        }

        public enum FileUploadType
        {
            Avatar,
            Comment,
            Document,
            Other
        }

        public enum ResponseCode
        {
            Success = 200,
            Created = 201,
            NoContent = 204,
            BadRequest = 400,
            Unauthorized = 401,
            Forbidden = 403,
            NotFound = 404,
            Conflict = 409,
            InternalServerError = 500,
            Logout = 9999
        }


    }
}