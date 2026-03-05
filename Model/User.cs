using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TaskManagement.Model
{
    public class User
    {
        public int? user_id { get; set; }
        public string? user_name { get; set; }
        public string? user_email { get; set; }
        public string? user_password { get; set; }
        public string? created_at { get; set; }
    }
    [JsonSerializable(typeof(List<User>))]
    internal sealed partial class UserContext : JsonSerializerContext
    {

    }
}