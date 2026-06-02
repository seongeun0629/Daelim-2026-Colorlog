using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Colorlog.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string CreateAt { get; set; }
        public string? ProfileImagePath { get; set; }

        public bool HasProfileImage =>
        !string.IsNullOrEmpty(ProfileImagePath) && File.Exists(ProfileImagePath);
    }
}
