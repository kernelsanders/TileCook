﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TileCook.API
{
    public static class ContentType
    {
        public static string GetContentType(string extension)
        {
            string contentType;
            switch (extension)
            {
                case "json":
                    contentType = "application/json";
                    break;
                case "pbf":
                    contentType = "application/x-protobuf";
                    break;
                default:
                    contentType = "text/plain";//MimeMapping.GetMimeMapping("." + extension);
                    break;
            }
            return contentType;
        }
    }
}