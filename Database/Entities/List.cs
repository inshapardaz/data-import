﻿using System;

namespace Inshapardaz.DataImport.Database.Entities
{
    public class List
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? ExpireAt { get; set; }
    }
}