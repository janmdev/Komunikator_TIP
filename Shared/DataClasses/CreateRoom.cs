﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DataClasses
{
    public class CreateRoom
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public byte UsersLimit { get; set; }
    }
}
