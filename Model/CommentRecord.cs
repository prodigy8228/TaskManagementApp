using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManagement.Model
{

    public class CommentRecord()
    {

        public int? comment_id { get; set; }          // Unique identifier for the comment
        public int? task_id { get; set; }             // ID of the task this comment belongs to
        public int? comment_user_id { get; set; }             // ID of the user who made the comment
        public string? comment_text { get; set; }     // Text content of the comment
        public string? comment_created_at { get; set; }       // Timestamp when the comment was created

        // Optional display property for the commenter's username
        public string? UserName { get; set; }

    }
}
