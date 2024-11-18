namespace SmartFin.Models.AouthResponse
{
    public class AuthResponse
    {
        public int userId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}