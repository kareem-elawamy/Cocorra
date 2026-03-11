using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.DTOS.FriendDto
{
    public class UserSearchDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        // حالة الصداقة (عشان فلاتر يعرف يعرض زرار Add ولا Pending ولا Friends)
        public string FriendshipStatus { get; set; } = "None";
    }
}
