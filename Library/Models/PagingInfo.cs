﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Library.Models
{
    public class PagingInfo
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages 
        { 
            get { return TotalItems / ItemsPerPage + (TotalItems % ItemsPerPage == 0 ? 0 : 1); } 
        }
    }
}