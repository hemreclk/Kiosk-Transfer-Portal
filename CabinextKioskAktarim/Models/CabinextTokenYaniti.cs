using Newtonsoft.Json;

namespace CabinextKioskAktarim.Models
{
    public class CabinextTokenYaniti
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }
    }
}
