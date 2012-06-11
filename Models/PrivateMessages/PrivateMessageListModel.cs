﻿using System.Collections.Generic;
using Nop.Web.Models.Common;

namespace Nop.Web.Models.PrivateMessages
{
    public class PrivateMessageListModel
    {
        public IList<PrivateMessageModel> Messages { get; set; }
        public PagerModel PagerModel { get; set; }
    }
}