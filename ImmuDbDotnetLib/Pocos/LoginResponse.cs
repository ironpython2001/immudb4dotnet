namespace ImmuDbDotnetLib.Pocos
{
    public class LoginResponse
    {
       public Status Status
        {
            get;
            set;
        }
    }
    public class Status
    {
        public bool IsSuccess
        {
            get; set;
        }
        public string Detail
        {
            get; set;
        }
    }
    public class DatabaseListResponse
    {
        public string Detail
        {
            get; set;
        }
    }
}
