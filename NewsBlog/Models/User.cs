using Microsoft.AspNetCore.Identity;

namespace NewsBlog.Models
{
    public class User:IdentityUser
    {
        //Auther
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        //Relation
        public List<Post>? Posts { get; set; }
    }
}
