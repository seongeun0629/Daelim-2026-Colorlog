using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Colorlog.Models
{
    public class ProfileSwitchedMessage: ValueChangedMessage<int>
    {
        public ProfileSwitchedMessage(int userId) : base(userId)
        {
        }
    }
}
