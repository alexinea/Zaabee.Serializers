﻿using System;
using System.Text;

namespace Zaabee.Xml
{
    public static class StringExtension
    {
        public static T FromXml<T>(this string str, Encoding encoding = null) =>
            XmlHelper.Deserialize<T>(str, encoding);

        public static object FromXml(this string str, Type type, Encoding encoding = null) =>
            XmlHelper.Deserialize(type, str, encoding);
    }
}